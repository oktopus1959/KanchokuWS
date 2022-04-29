using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KanchokuWS.OverlappingKeyStroke.DeterminerLib;
using Utils;

namespace KanchokuWS.OverlappingKeyStroke
{
    class DeterminerImpl
    {
        private static Logger logger = Logger.GetLogger(true);

        // 同時打鍵保持リスト
        private List<Stroke> strokeList = new List<Stroke>();

        /// <summary>
        /// 初期化と同時打鍵組合せ辞書の読み込み
        /// </summary>
        /// <param name="tableFile">主テーブルファイル名</param>
        /// <param name="tableFile2">副テーブルファイル名</param>
        public void Initialize(string tableFile, string tableFile2)
        {
            KeyCombinationPool.Initialize();
            Clear();

            var parser = new TableFileParser(KeyCombinationPool.Singleton1);
            parser.ParseTable(tableFile);

            if (tableFile2._notEmpty()) {
                var parser2 = new TableFileParser(KeyCombinationPool.Singleton2);
                parser2.ParseTable(tableFile2);
            }
        }

        /// <summary>
        /// 選択されたテーブルファイルに合わせて、KeyComboPoolを入れ替える
        /// </summary>
        public void ExchangeKeyCombinationPool()
        {
            KeyCombinationPool.ExchangeCurrentPool();
        }

        public bool IsEnabled => KeyCombinationPool.CurrentPool.Count > 0;

        /// <summary>
        /// 同時打鍵リストをクリアする
        /// </summary>
        public void Clear()
        {
            strokeList.Clear();
        }

        /// <summary>
        /// キーの押下<br/>押下されたキーをキューに積むだけ。同時打鍵などの判定はキーの解放時に行う。
        /// </summary>
        /// <param name="decKey">押下されたキーのデコーダコード</param>
        /// <returns>同時打鍵の可能性があるなら true を返す<br/>無効なら false を返す</returns>
        public bool KeyDown(int decKey)
        {
            var dtNow = DateTime.Now;
            logger.DebugH(() => $"ENTER: Add new stroke: dt={dtNow.ToString("HH:mm:ss.fff")}, decKey={decKey}");
            bool flag = false;
            var stroke = new Stroke(decKey, dtNow);
            if (strokeList._notEmpty() || !(KeyCombinationPool.CurrentPool.GetEntry(stroke)?.IsTerminal ?? true)) {
                flag = true;
                strokeList.Add(stroke);
            }
            logger.DebugH(() => $"LEAVE: {flag}: strokeList={strokeList.Select(x => x.NormalKeyCode.ToString())._join(":")}");
            return flag;
        }

        /// <summary>
        /// キーの解放
        /// </summary>
        /// <param name="decKey">解放されたキーのデコーダコード</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダコード列を返す。<br/>確定しなければ null を返す</returns>
        public List<int> KeyUp(int decKey)
        {
            int normalKey = decKey % DecoderKeys.NORMAL_DECKEY_NUM;     // 検索のためにキーを正規化しておく
            var dtNow = DateTime.Now;
            logger.DebugH(() => $"ENTER: dt={dtNow.ToString("HH:mm:ss.fff")}, decKey={decKey}");
            List<int> result = null;

            if (strokeList._isEmpty()) {
                logger.DebugH(() => $"LEAVE-0: result=(null), strokeList is empty");
                return result;
            }

            if (strokeList.Count == 1) {
                // ストロークキューに１打鍵だけ積んであった場合
                if (strokeList[0].NormalKeyCode == normalKey) {
                    if (!strokeList[0].IsShifted) result = Helper.MakeList(strokeList[0].DecoderKeyCode);
                    strokeList.Clear();
                }
                logger.DebugH(() => $"LEAVE-1: result={decKey}, strokeList={strokeList.Select(x => x.NormalKeyCode.ToString())._join(":")}");
                return result;
            }

            // ストロークキューに2打鍵以上積んであった場合
            // まず同じキーコードのものを探す
            int upKeyIdx = findSameKey(normalKey);
            logger.DebugH(() => $"Stroke {upKeyIdx} up");
            if (upKeyIdx >= 0) {
                int overlapLen = 0;
                if (upKeyIdx == 0 && !strokeList[0].IsShifted) {
                    // 第１打鍵と同じキーが解放され、かつ第１打鍵がシフトまだシフトされていない場合⇒同時打鍵になる範囲を求める
                    for (int i = 1; i < strokeList.Count; ++i) {
                        double ms1 = strokeList[0].TimeSpanMs(strokeList[i]);
                        double ms2 = strokeList[i].TimeSpanMs(dtNow);
                        double rate = (ms2 / (ms1 + ms2)) * 100.0;
                        logger.DebugH(() => $"ms1={ms1:f2}, ms2={ms2:f2}, ovlRate={rate:f1}, threshold={Settings.OverlappingKeyTimeRate}");
                        if (ms1 <= Settings.OverlappingMaxAllowedLeadTimeMs && (rate >= Settings.OverlappingKeyTimeRate || ms2 >= Settings.OverlappingKeyTimeMs)) {
                            overlapLen = i + 1;
                            break;
                        }
                    }
                } else {
                    // 第１打鍵がシフト済み⇒ここまでの全打鍵を同時打鍵候補とみなす
                    overlapLen = strokeList.Count;
                }
                // シフト済み、または同時打鍵候補あり⇒長いほうからチェックして最長の同時打鍵列を求める
                while (overlapLen > 1) {
                    var keyList = KeyCombinationPool.CurrentPool.GetEntry(strokeList, overlapLen)?.DecoderKeyList;
                    if (keyList != null) {
                        // 同時打鍵が見つかった
                        logger.DebugH($"PATH-1: Overlap candidates found");
                        result = new List<int>(keyList.KeyList);
                        break;
                    }
                    --overlapLen;
                }
                if (result == null) {
                    // 同時打鍵が見つからなかったのでシフト以外の個別キーを返す
                    logger.DebugH($"PATH-2: Overlap candidates not found");
                    result = new List<int>();
                    overlapLen = 0;
                }
                // 同時打鍵の後に解放キーがあるなら、そこまでを出力に加える
                for (int i = overlapLen; i <= upKeyIdx; ++i) {
                    if (!strokeList[i].IsShifted) {
                        result.Add(strokeList[i].DecoderKeyCode);
                    }
                }
                // UPされたキーとシフトされないキーを除去
                for (int i = upKeyIdx._max(overlapLen - 1); i >= 0; --i) {
                    if (i == upKeyIdx || !strokeList[i].IsShiftable) {
                        strokeList.RemoveAt(i);
                    } else {
                        strokeList[i].SetShifted();
                    }
                }
                logger.DebugH(() => $"LEAVE-1: result={result?.Select(x => x.ToString())._join(":") ?? "null"}, strokeList={strokeList.Select(x => x.NormalKeyCode.ToString())._join(":")}");
                return result;
            }

            // Downされたキーが見つからなかった
            logger.DebugH(() => $"LEAVE: result=(null), strokeList={strokeList.Select(x => x.NormalKeyCode.ToString())._join(":")}");
            return null;
        }

        private int findSameKey(int normKey)
        {
            for (int idx = strokeList.Count - 1; idx >= 0; --idx) {
                if (strokeList[idx].NormalKeyCode == normKey) return idx;
            }
            return -1;
        }
    }
}

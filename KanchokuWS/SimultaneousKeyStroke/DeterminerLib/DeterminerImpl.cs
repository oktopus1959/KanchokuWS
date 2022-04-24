using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KanchokuWS.SimultaneousKeyStroke.DeterminerLib;
using Utils;

namespace KanchokuWS.SimultaneousKeyStroke
{
    class DeterminerImpl
    {
        // 同時打鍵組合せ
        private Dictionary<string, KeyCombination> keyComboDict;

        // 同時打鍵シフトキー
        private ShiftKeyPriority shiftKeys;

        // 同時打鍵保持リスト
        private List<Stroke> strokeList = new List<Stroke>();

        /// <summary>
        /// 初期化と同時打鍵組合せ辞書の読み込み
        /// </summary>
        /// <param name="tableFile"></param>
        public void Initialize(string tableFile)
        {
            var parser = new TableFileParser();
            keyComboDict = parser.ParseTable(tableFile);
            shiftKeys = parser.SimultaneousShiftKeys;
            Clear();
        }

        /// <summary>
        /// 同時打鍵リストをクリアする
        /// </summary>
        public void Clear()
        {
            strokeList.Clear();
        }

        /// <summary>
        /// キーの押下
        /// </summary>
        /// <param name="keyInfo">押下されたキーの情報</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダキー列を返す。<br/>確定しなければ null を返す</returns>
        public DecoderKeyCodeList KeyDown(KeyCodeInfo keyInfo)
        {
            int keyCode = keyInfo.DecKey;
            var dtNow = DateTime.Now;
            var primKey = KeyCombinationHelper.MakePrimaryKey(strokeList, keyCode);
            var keyCombo = keyComboDict._safeGet(primKey);
            if (keyCombo != null && !keyCombo.IsTerminal) {
                strokeList.Add(new Stroke(keyCode, shiftKeys.GetShiftPriority(keyCode), dtNow));
                return null;
            }
            if (keyCombo == null) {
                // 新しい keyInfo を加えた組合せは存在しないので、これは捨てて、前回までのキーコンボを使う
                keyCombo = keyComboDict._safeGet(KeyCombinationHelper.MakePrimaryKey(strokeList));
            }
            // 前回までのキーのうち、シフト以外のものを削除
            int i = 0;
            while (i < strokeList.Count) {
                if (shiftKeys.GetShiftPriority(strokeList[i].KeyCode) > 0) {
                    strokeList.RemoveAt(i);
                } else {
                    ++i;
                }
            }
            return keyCombo?.DecoderKeyList;
        }

        /// <summary>
        /// キーの解放
        /// </summary>
        /// <param name="keyInfo">解放されたキーの情報</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダキー列を返す。<br/>確定しなければ null を返す</returns>
        public DecoderKeyCodeList KeyUp(KeyCodeInfo keyInfo)
        {
            int keyCode = keyInfo.DecKey;
            var dtNow = DateTime.Now;
            DecoderKeyCodeList result = null;

            if (strokeList._isEmpty()) return result;

            if (strokeList.Count == 1) {
                if (strokeList[0].KeyCode != keyCode) return null;
                result = new DecoderKeyCodeList(strokeList);
                strokeList.Clear();
                return result;
            }

            if (strokeList.Count == 2) {
                double ms1 = strokeList[0].TimeSpanMs(strokeList[1]);
                double ms2 = strokeList[1].TimeSpanMs(dtNow);
                if (strokeList[0].KeyCode == keyCode) {
                    // 第１打鍵と同じキーが解放された場合
                    if ((ms2 / (ms1+ms2)) >= Settings.SimultaneousKeyOverwrapTimeRate) {
                        result = keyComboDict._safeGet(KeyCombinationHelper.MakePrimaryKey(strokeList))?.DecoderKeyList;
                    }
                    if (result == null) {
                        result = new DecoderKeyCodeList(strokeList[0].KeyCode);
                    }
                    strokeList.RemoveAt(0);
                    return result;
                }
                if (strokeList[1].KeyCode == keyCode) {
                    // 第2打鍵と同じキーが解放された場合
                    result = keyComboDict._safeGet(KeyCombinationHelper.MakePrimaryKey(strokeList))?.DecoderKeyList;
                    if (result == null) {
                        result = new DecoderKeyCodeList(strokeList[1].KeyCode);
                    }
                    strokeList.RemoveAt(1);
                    return result;
                }
                return null;
            }
            return null;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace KanchokuWS.CombinationKeyStroke.DeterminerLib
{
    class StrokeList
    {
        private static Logger logger = Logger.GetLogger(true);

        /// <summary>
        /// 前回のKeyUp時から持ち越された、同時打鍵用のストロークリスト
        /// </summary>
        private List<Stroke> comboList = new List<Stroke>();

        /// <summary>
        /// キー押下によって追加されたストロークリスト
        /// </summary>
        private List<Stroke> strokeList = new List<Stroke>();

        public List<Stroke> GetList()
        {
            return strokeList;
        }

        public List<List<Stroke>> GetSubLists()
        {
            var result = new List<List<Stroke>>();
            gatherSubList(strokeList, result);
            return result;
        }

        private void gatherSubList(List<Stroke> list, List<List<Stroke>> result)
        {
            if (list.Count > 0) {
                result.Add(list);
                if (list.Count > 1) {
                    for (int i = list.Count - 1; i >= 0; --i) {
                        var subList = list.Take(i).ToList();
                        subList.AddRange(list.Skip(i + 1));
                        gatherSubList(subList, result);
                    }
                }
            } else {
                result.Add(new List<Stroke>());
            }
        }

        public int Count => comboList.Count + strokeList.Count;

        public void Clear()
        {
            comboList.Clear();
            strokeList.Clear();
        }

        public bool IsEmpty()
        {
            return strokeList.Count == 0;
        }

        public Stroke First => strokeList._isEmpty() ? null : strokeList[0];

        public Stroke Last => strokeList._isEmpty() ? null : strokeList.Last();

        public Stroke At(int pos)
        {
            return (pos >= 0 && pos < strokeList.Count) ? strokeList[pos] : null;
        }

        public void RemoveAt(int pos)
        {
            if (pos >= 0 && pos < strokeList.Count) strokeList.RemoveAt(pos);
        }

        public Stroke FindSameStroke(int moduloKey)
        {
            int idx = FindSameIndex(moduloKey);
            return idx >= 0 ? strokeList[idx] : null;
        }

        public int FindSameIndex(int moduloKey)
        {
            return findSameIndex(strokeList, moduloKey);
        }

        private int findSameIndex(List<Stroke> list, int moduloKey)
        {
            for (int idx = list.Count - 1; idx >= 0; --idx) {
                if (list[idx].ModuloKeyCode == moduloKey) return idx;
            }
            return -1;
        }

        public Stroke DetectKeyRepeat(Stroke s)
        {
            return s != null ? DetectKeyRepeat(s.DecoderKeyCode) : null;
        }

        public Stroke DetectKeyRepeat(int decKey)
        {
            return (Last?.IsSameKey(decKey) ?? false) ? Last : null;
        }

        public void Add(Stroke s)
        {
            strokeList.Add(s);
        }

        public List<int> GetKeyCombination(int decKey, DateTime dtNow)
        {
            int moduloKey = Stroke.ModuloizeKey(decKey);     // 検索のためにキーを正規化しておく
            logger.DebugH(() => $"ENTER: dt={dtNow.ToString("HH:mm:ss.fff")}, decKey={decKey}, modulo={moduloKey}");

            List<int> result = null;

            int upComboIdx = findSameIndex(comboList, moduloKey);
            if (strokeList._notEmpty()) {
                result = new List<int>();
                int upKeyIdx = findSameIndex(strokeList, moduloKey);
                logger.DebugH(() => $"upComboIdx={upComboIdx}, upKeyIdx={upKeyIdx}");

                int startPos = 0;
                int overlapLen = 0;
                var preShifts = new StrokeList();

                var subComboLists = new List<List<Stroke>>();
                gatherSubList(comboList, subComboLists);

                while (startPos < strokeList.Count) {
                    bool bFound = false;
                    foreach (var subList in subComboLists) {
                        overlapLen = strokeList.Count - startPos;
                        int minLen = subList.Count > 0 ? 1 : 2;
                        logger.DebugH(() => $"minLen={minLen}, overlapLen={overlapLen}");
                        while (overlapLen >= minLen) {
                            var iter = strokeList.Skip(startPos).Take(overlapLen);
                            var list = new List<Stroke>(subList);
                            list.AddRange(iter);
                            logger.DebugH(() => $"PATH-1: list={list._toString()}");
                            var keyList = KeyCombinationPool.CurrentPool.GetEntry(list, null)?.ComboShiftedDecoderKeyList;
                            logger.DebugH(() => $"PATH-1: keyList={(keyList._isEmpty() ? "(empty)" : keyList.KeyString())}");
                            if (keyList._notEmpty() && isCombinationTiming(upKeyIdx, startPos, overlapLen, dtNow)) {
                                // 同時打鍵が見つかった(かつ、同時打鍵の条件を満たしている)ので、それを出力する
                                logger.DebugH(() => $"PATH-1: FOUND: Overlap candidates found: startPos={startPos}, overlapLen={overlapLen}");
                                result.AddRange(keyList.KeyList);
                                startPos += overlapLen;
                                bFound = true;
                                // 同時打鍵に使用したキーを使い回すかあるいは破棄するか
                                if (keyList.IsOneshotShift) {
                                    // Oneshotなら使い回さず、今回かぎりとする
                                    logger.DebugH(() => $"PATH-1: OneshotShift");
                                } else if (subComboLists.Count <= 1 && subComboLists._getFirst()._isEmpty()) {
                                    // 持ち越された同時打鍵キーリストが空なので、今回の同時打鍵に使用したキーを使い回す
                                    logger.DebugH(() => $"PATH-1: Reuse temporary combination");
                                    foreach (var s in iter) s.SetShifted();
                                    subComboLists.Clear();
                                    gatherSubList(list, subComboLists);
                                }
                                break;
                            }
                            --overlapLen;
                        }
                        if (bFound) break;  // 見つかった
                    }
                    if (!bFound) {
                        // 見つからなかったら、それを出力し、1つずらして、ループする
                        logger.DebugH(() => $"ADD: startPos={startPos}, keyCode={strokeList[startPos].DecoderKeyCode}");
                        result.Add(strokeList[startPos].DecoderKeyCode);
                        ++startPos;
                    }
                    logger.DebugH(() => $"startPos={startPos}, overlapLen={overlapLen}");
                }

                // UPされたキー以外を comboList に移動する
                if (upKeyIdx >= 0) strokeList.RemoveAt(upKeyIdx);
                comboList.AddRange(strokeList.Where(x => !x.IsShifted));
                strokeList.Clear();
            }
            if (upComboIdx >= 0) comboList.RemoveAt(upComboIdx);

            logger.DebugH(() => $"LEAVE: result={result?._keyString() ?? "null"}, {ToDebugString()}");
            return result;
        }

        // タイミングによる同時打鍵判定関数
        bool isCombinationTiming(int upKeyIdx, int startPos, int overlapLen, DateTime dtNow)
        {
            logger.DebugH(() => $"comboList.Count={comboList.Count}");
            if (comboList.Count > 0) return true;

            int checkPos = startPos + overlapLen - 1;

            logger.DebugH(() => $"upKeyIdx={upKeyIdx} >= startPos={startPos} + overlapLen={overlapLen} - 1 ? {upKeyIdx >= checkPos} ");
            if (upKeyIdx >= checkPos) return true;      // チェック対象の末尾キーが最初にUPされた

            //logger.DebugH(() => $"strokeList[{startPos}].IsShiftableSpaceKey={strokeList[startPos].IsShiftableSpaceKey}");
            //if (strokeList[startPos].IsShiftableSpaceKey) return true;     // 先頭キーがシフト可能なスペースキーだった
            logger.DebugH(() => $"strokeList[{startPos}].IsShifted={strokeList[startPos].IsShifted}");
            if (strokeList[startPos].IsShifted) return true;     // 先頭キーがシフト済みキーだった

            // タイミングチェック
            double ms1 = strokeList[startPos].TimeSpanMs(strokeList[checkPos]);
            double ms2 = strokeList[checkPos].TimeSpanMs(dtNow);
            double rate = (ms2 / (ms1 + ms2)) * 100.0;
            logger.DebugH(() => $"ms1={ms1:f2}, ms2={ms2:f2}, ovlRate={rate:f1}, threshold={Settings.CombinationKeyTimeRate}");
            return (Settings.CombinationMaxAllowedLeadTimeMs <= 0 || ms1 <= Settings.CombinationMaxAllowedLeadTimeMs)
                && (rate >= Settings.CombinationKeyTimeRate || ms2 >= Settings.CombinationKeyTimeMs);
        }

        public string ToDebugString()
        {
            return $"comboList={comboList._toString()}, strokeList={strokeList._toString()}";
        }
    }

    static class StrokeListExtension
    {
        public static bool _isEmpty(this StrokeList list)
        {
            return list == null || list.IsEmpty();
        }

        public static bool _notEmpty(this StrokeList list)
        {
            return !list._isEmpty();
        }

        public static string _toString(this List<Stroke> list)
        {
            return list?.Select(x => x.ModuloKeyCode.ToString())._join(":") ?? "";
        }
    }
}

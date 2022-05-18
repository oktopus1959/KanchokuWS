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
        /// キー押下によって追加された未処理のストロークリスト
        /// </summary>
        private List<Stroke> unprocList = new List<Stroke>();

        /// <summary>
        /// 与えられたリストの部分リストからなる集合(リスト)を返す
        /// </summary>
        /// <param name="list"></param>
        /// <param name="result"></param>
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

        public int Count => comboList.Count + unprocList.Count;

        public void Clear()
        {
            comboList.Clear();
            unprocList.Clear();
        }

        public bool IsEmpty()
        {
            return Count == 0;
        }

        /// <summary>
        /// 処理済み同時打鍵の個数
        /// </summary>
        /// <returns></returns>
        public bool IsComboListEmpty()
        {
            return comboList.Count == 0;
        }

        /// <summary>
        /// 未処理打鍵の個数
        /// </summary>
        /// <returns></returns>
        public bool IsUnprocListEmpty()
        {
            return unprocList.Count == 0;
        }

        public Stroke First => unprocList._getFirst();

        public Stroke Last => unprocList._getLast();

        public Stroke FindSameStroke(int decKey)
        {
            int idx = FindSameIndex(decKey);
            return idx >= 0 ? unprocList[idx] : null;
        }

        public int FindSameIndex(int decKey)
        {
            return findSameIndex(unprocList, decKey);
        }

        private int findSameIndex(List<Stroke> list, int decKey)
        {
            for (int idx = list.Count - 1; idx >= 0; --idx) {
                if (list[idx].IsSameKey(decKey)) return idx;
            }
            return -1;
        }

        public bool DetectKeyRepeat(Stroke s)
        {
            return s != null ? DetectKeyRepeat(s.OrigDecoderKey) : false;
        }

        public bool DetectKeyRepeat(int decKey)
        {
            var s = unprocList._getLast();
            if (s != null) {
                if (s.IsSameKey(decKey)) {
                    s.SetCombined();
                    if (unprocList._safeCount() == 1) {
                        comboList.Add(s);
                        unprocList.Clear();
                    }
                    return true;
                }
            } else {
                s = comboList._getLast();
                if (s != null && s.IsSameKey(decKey)) {
                    return true;
                }
            }
            return false;
        }

        public void Add(Stroke s)
        {
            unprocList.Add(s);
        }

        // 押下の場合
        public List<int> GetKeyCombinationWhenKeyDown(int decKey, DateTime dtNow)
        {
            // 連続シフトでなければ何も返さない
            if (KeyCombinationPool.CurrentPool.ContainsUnorderedShiftKey) return null;

            // 連続シフトの場合は、同時打鍵キーの数は最大2とする
            List<int> getAndCheckCombo(List<Stroke> list)
            {
                var keyCombo = KeyCombinationPool.CurrentPool.GetEntry(list);
                logger.DebugH(() => $"combo={(keyCombo == null ? "(none)" : "FOUND")}, decKeyList={(keyCombo == null ? "(none)" : keyCombo.DecKeysDebugString())}, comboKeyList={(keyCombo == null ? "(none)" : keyCombo.ComboKeysDebugString())}");
                if (keyCombo?.DecKeyList != null) {
                    logger.DebugH("COMBO CHECK PASSED");
                    return new List<int>(keyCombo.DecKeyList);
                }
                return null;
            }

            List<int> result = null;
            if (comboList._isEmpty() && unprocList.Count == 2 && unprocList.Last().IsSameKey(decKey)) {
                // 連続シフトでの最初の同時打鍵のケース
                logger.DebugH("Try first successive combo");
                result = getAndCheckCombo(unprocList);
                if (result != null) {
                    // 同時打鍵に使用したキーを使い回す
                    comboList.Add(unprocList[0]);
                    comboList[0].SetCombined();
                    unprocList.Clear();
                }
            }
            else if (comboList.Count == 1 && unprocList.Count == 1 && unprocList[0].IsSameKey(decKey) && Settings.CombinationKeyTimeMs <= 0) {
                // 連続シフトでの2文字目以降のケース
                logger.DebugH("Try second or later successive combo");
                result = getAndCheckCombo(Helper.MakeList(comboList[0], unprocList[0]));
                unprocList.RemoveAt(0); // コンボがなくてもキーを削除しておく(たとえば月光でDを長押ししてKを押したような場合は、何も出力せず、Kも除去する)
            } else {
                logger.DebugH("Combo check will be done at key release");
            }
            return result;
        }

        // 解放の場合
        public List<int> GetKeyCombinationWhenKeyUp(int decKey, DateTime dtNow)
        {
            logger.DebugH(() => $"ENTER: decKey={decKey}, dt={dtNow.ToString("HH:mm:ss.fff")}");

            List<int> result = null;

            int upComboIdx = findSameIndex(comboList, decKey);
            if (unprocList._notEmpty()) {
                int upKeyIdx = findSameIndex(unprocList, decKey);
                logger.DebugH(() => $"upComboIdx={upComboIdx}, upKeyIdx={upKeyIdx}");
                if (upComboIdx >= 0 || upKeyIdx >= 0) {
                    result = new List<int>();
                    int startPos = 0;
                    int overlapLen = 0;

                    // 持ち越したキーリストの部分リストからなる集合(リスト)
                    var subComboLists = new List<List<Stroke>>();
                    gatherSubList(comboList, subComboLists);

                    bool bSecondComboCheck = comboList._notEmpty();
                    while (startPos < unprocList.Count) {
                        bool bFound = false;
                        foreach (var subList in subComboLists) {
                            overlapLen = unprocList.Count - startPos;
                            int minLen = subList._isEmpty() ? 2 : 1;
                            logger.DebugH(() => $"subList.Count={subList.Count}, minLen={minLen}, overlapLen={overlapLen}");
                            while (overlapLen >= minLen) {
                                var list = makeComboChallengeList(subList, startPos, overlapLen);
                                logger.DebugH(() => $"SEARCH: searchKey={list._toString()}");
                                //var keyList = KeyCombinationPool.CurrentPool.GetEntry(list)?.ComboShiftedDecoderKeyList;
                                var keyCombo = KeyCombinationPool.CurrentPool.GetEntry(list);
                                logger.DebugH(() => $"RESULT: combo={(keyCombo == null ? "(none)" : "FOUND")}, decKeyList={(keyCombo == null ? "(none)" : keyCombo.DecKeysDebugString())}, comboKeyList={(keyCombo == null ? "(none)" : keyCombo.ComboKeysDebugString())}");
                                if (keyCombo?.DecKeyList != null && isCombinationTiming(true, upKeyIdx, startPos, overlapLen, dtNow, bSecondComboCheck)) {
                                    // 同時打鍵が見つかった(かつ、同時打鍵の条件を満たしている)ので、それを出力する
                                    bSecondComboCheck = true;
                                    logger.DebugH(() => $"COMBO CHECK PASSED: Overlap candidates found: startPos={startPos}, overlapLen={overlapLen}");
                                    result.AddRange(keyCombo.DecKeyList);
                                    // 同時打鍵に使用したキーを使い回すかあるいは破棄するか
                                    if (keyCombo.IsOneshotShift) {
                                        // Oneshotなら使い回さず、今回かぎりとする
                                        logger.DebugH(() => $"OneshotShift");
                                    } else {
                                        logger.DebugH(() => $"Move to next combination: startPos={startPos}, overlapLen={overlapLen}");
                                        foreach (var s in getRange(startPos, overlapLen)) s.SetCombined();
                                        if (subComboLists.Count <= 1 && subComboLists._getFirst()._isEmpty()) {
                                            // 持ち越された同時打鍵キーリストが空なので、今回の同時打鍵に使用したキーを使い回す
                                            logger.DebugH(() => $"Reuse temporary combination");
                                            subComboLists.Clear();
                                            gatherSubList(list, subComboLists);
                                        }
                                    }
                                    startPos += overlapLen;
                                    bFound = true;
                                    break;
                                }
                                --overlapLen;
                            }
                            if (bFound) break;  // 見つかった
                        }
                        if (!bFound) {
                            // 見つからなかったら、それを出力し、1つずらして、ループする
                            logger.DebugH(() => $"ADD: startPos={startPos}, keyCode={unprocList[startPos].OrigDecoderKey}");
                            result.Add(unprocList[startPos].OrigDecoderKey);
                            ++startPos;
                        }
                        logger.DebugH(() => $"startPos={startPos}, overlapLen={overlapLen}");
                    }

                    // UPされたキー以外を comboList に移動する
                    if (upKeyIdx >= 0) unprocList.RemoveAt(upKeyIdx);
                    comboList.AddRange(unprocList.Where(x => x.IsCombined));
                    unprocList.Clear();
                }
            }
            if (upComboIdx >= 0) comboList.RemoveAt(upComboIdx);

            logger.DebugH(() => $"LEAVE: result={result?._keyString() ?? "null"}, {ToDebugString()}");
            return result;
        }

        /// <summary>同時打鍵のチャレンジ列を作成する</summary>
        /// <returns></returns>
        private List<Stroke> makeComboChallengeList(List<Stroke> strokes, int startPos, int overlapLen)
        {
            var list = new List<Stroke>(strokes);
            list.AddRange(getRange(startPos, overlapLen));
            if (list.Count >= 3) {
                // 3個以上のキーを含むならば、スペースのような weakShift を削除する
                for (int i = 0; i < list.Count; ++i) {
                    if (list[i].ModuloDecKey == DecoderKeys.STROKE_SPACE_DECKEY) {
                        logger.DebugH(() => $"DELETE weakShift at {i}");
                        list.RemoveAt(i);
                        break;
                    }
                }
            }
            return list;
        }

        private IEnumerable<Stroke> getRange(int startPos, int overlapLen)
        {
            return unprocList.Skip(startPos).Take(overlapLen);
        }

        // タイミングによる同時打鍵判定関数
        private bool isCombinationTiming(bool bUp, int upKeyIdx, int startPos, int overlapLen, DateTime dtNow, bool bSecondComboCheck)
        {
            logger.DebugH(() => $"comboList.Count={comboList.Count}, SecondCheck={bSecondComboCheck}");
            //if (comboList.Count > 0) return true;

            int checkPos = startPos + overlapLen - 1;

            logger.DebugH(() => $"CHECK1: upKeyIdx={upKeyIdx},startPos={startPos}, overlapLen={overlapLen}: {upKeyIdx >= checkPos}");
            if ((bUp || !bSecondComboCheck) && upKeyIdx >= checkPos) return true;      // チェック対象の末尾キーが最初にUPされた

            logger.DebugH(() => $"CHECK2: strokeList[{startPos}].IsShifted={unprocList[startPos].IsCombined}");
            if (!bSecondComboCheck && unprocList[startPos].IsCombined) return true;     // 先頭キーがシフト済みキーだった
            logger.DebugH(() => $"CHECK3: strokeList[{startPos}].IsShiftableSpaceKey={unprocList[startPos].IsShiftableSpaceKey}");
            if (unprocList[startPos].IsShiftableSpaceKey) return true;     // 先頭キーがシフト可能なスペースキーだった⇒スペースキーならタイミングは考慮せず無条件

            // タイミングチェック(1文字目ならリードタイムをチェック; 2文字目以降の場合は、対象キーダウンからシフトキーアップまでの時間によって判定)
            double ms1 = unprocList[startPos].TimeSpanMs(unprocList[checkPos]);
            double ms2 = unprocList[checkPos].TimeSpanMs(dtNow);
            logger.DebugH(() => $"ms1={ms1:f2}ms, threshold={Settings.CombinationMaxAllowedLeadTimeMs}ms, ms2={ms2:f2}ms, threshold={Settings.CombinationKeyTimeMs}ms");
            return (!bSecondComboCheck && ms1 <= Settings.CombinationMaxAllowedLeadTimeMs) || (bSecondComboCheck && ms2 >= Settings.CombinationKeyTimeMs);
        }

        public string ToDebugString()
        {
            return $"comboList={comboList._toString()}, unprocList={unprocList._toString()}";
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
            return list?.Select(x => x.OrigDecoderKey.ToString())._join(":") ?? "";
        }
    }
}

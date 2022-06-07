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
        /// キー押下によって追加された未処理のストロークリスト
        /// </summary>
        private List<Stroke> unprocList = new List<Stroke>();

        /// <summary>
        /// 同時打鍵検索によって処理されたが、次の同時打鍵検索にも使用されうるストロークリスト
        /// </summary>
        private List<Stroke> comboList = new List<Stroke>();

        /// <summary>
        /// 与えられたリストの部分リストからなる集合(リスト)を返す
        /// </summary>
        /// <param name="list"></param>
        /// <param name="result"></param>
        private List<List<Stroke>> gatherSubList(List<Stroke> list)
        {
            List<List<Stroke>> result = new List<List<Stroke>>();
            gatherSubList(comboList, result);
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

        public bool IsComboListEmpty => comboList.Count == 0;

        public bool IsUnprocListEmpty => unprocList.Count == 0;

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

        private int findAndMarkUpKey(List<Stroke> list, int decKey)
        {
            int idx = findSameIndex(list, decKey);
            list._getNth(idx)?.SetUpKey();
            return idx;
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

        public bool IsSuccessiveShift2ndKey()
        {
            logger.DebugH(() =>  $"ContainsSuccessiveShiftKey={KeyCombinationPool.CurrentPool.ContainsSuccessiveShiftKey}, comboList.Count={comboList.Count}, unprocList.Count={unprocList.Count}");
            return KeyCombinationPool.CurrentPool.ContainsSuccessiveShiftKey &&
                (comboList.Count >= 1 && unprocList.Count == 1 ||
                 comboList.Count == 0 && unprocList.Count >= 3);
        }

        // 押下の場合
        public List<int> GetKeyCombinationWhenKeyDown(int decKey)
        {
            // 同時打鍵シフトでなければ何も返さない
            if (!KeyCombinationPool.CurrentPool.ContainsComboShiftKey) {
                logger.DebugH("No combo shift key");
                return null;
            }

            // 連続シフトの場合は、同時打鍵キーの数は最大2とする
            List<int> getAndCheckCombo(List<Stroke> list)
            {
                var keyCombo = KeyCombinationPool.CurrentPool.GetEntry(list);
                logger.DebugH(() => $"combo={(keyCombo == null ? "(none)" : "FOUND")}, decKeyList={(keyCombo == null ? "(none)" : keyCombo.DecKeysDebugString())}, comboKeyList={(keyCombo == null ? "(none)" : keyCombo.ComboKeysDebugString())}");
                if (keyCombo != null && keyCombo.DecKeyList != null && keyCombo.IsTerminal) {
                    logger.DebugH("COMBO CHECK PASSED");
                    return new List<int>(keyCombo.DecKeyList);
                }
                return null;
            }

            List<int> result = null;
            if (comboList._isEmpty() && unprocList.Count == 2 && unprocList.Last().IsSameKey(decKey)) {
                // 最初の同時打鍵のケース
                logger.DebugH("Try first successive combo");
                if (unprocList[0].IsPrefixShift || unprocList[1].TimeSpanMs(unprocList[0]) <= Settings.CombinationKeyMaxAllowedLeadTimeMs) {
                    // 前置シフトか、第2打鍵までの時間が閾値以下の場合
                    result = getAndCheckCombo(unprocList);
                    if (result != null) {
                        if (KeyCombinationPool.CurrentPool.ContainsSuccessiveShiftKey) {
                            // 連続シフトの場合は、同時打鍵に使用したキーを使い回す
                            comboList.Add(unprocList[0].IsComboShift ? unprocList[0] : unprocList[1]);
                            comboList[0].SetCombined();
                        }
                        unprocList.Clear();
                    }
                }
            } else if (Settings.CombinationKeyMinOverlappingTimeMs <= 0) {
                // 2文字目以降も即時判定の場合
                if (comboList.Count >= 1 && unprocList.Count == 1 && unprocList[0].IsSameKey(decKey)) {
                    // 2文字目以降のケース
                    logger.DebugH("Try second or later successive combo");
                    result = getAndCheckCombo(Helper.MakeList(comboList, unprocList[0]));
                    unprocList.RemoveAt(0); // コンボがなくてもキーを削除しておく(たとえば月光でDを長押ししてKを押したような場合は、何も出力せず、Kも除去する)
                }
            } else {
                logger.DebugH("Combo check will be done at key release");
            }
            return result;
        }

        // 同時打鍵のタイミングチェックをやった結果、満たされなかった
        bool isTimingCheckBroken = false;

        // 解放の場合
        public List<int> GetKeyCombinationWhenKeyUp(int decKey, DateTime dtNow)
        {
            logger.DebugH(() => $"ENTER: decKey={decKey}, dt={dtNow.ToString("HH:mm:ss.fff")}");

            List<int> result = null;

            try {
                int upComboIdx = findAndMarkUpKey(comboList, decKey);

                if (unprocList._notEmpty()) {
                    int upKeyIdx = findAndMarkUpKey(unprocList, decKey);
                    logger.DebugH(() => $"upComboIdx={upComboIdx}, upKeyIdx={upKeyIdx}");

                    if (upComboIdx >= 0 || upKeyIdx >= 0) {
                        //Stroke upStroke = upComboIdx >= 0 ? comboList[upComboIdx] : unprocList[upKeyIdx];

                        // 処理対象リスト
                        List<Stroke> hotList = unprocList;
                        if (upComboIdx < 0 && upKeyIdx >= 0 && upKeyIdx < unprocList.Count && !unprocList[upKeyIdx].IsComboShift) {
                            // 未処理リストの方は、同時打鍵シフトキー以外が解放された場合は、そのキーまでしかチェックしない
                            hotList = unprocList.Take(upKeyIdx + 1).ToList();
                        }
                        unprocList = unprocList.Skip(hotList.Count).ToList();

                        result = new List<int>();

                        bool bSecondComboCheck = comboList._notEmpty();
                        logger.DebugH(() => $"START while: {ToDebugString()}, dtNow={dtNow}, bSecondComboCheck={bSecondComboCheck}");

                        while (hotList._notEmpty()) {
                            //同時打鍵を見つける
                            isTimingCheckBroken = false;
                            int overlapLen = findCombo(result, hotList, dtNow, bSecondComboCheck);
                            if (overlapLen > 0) {
                                // 見つかった
                                logger.DebugH($"COMBO FOUND");
                                bSecondComboCheck = true;
                            } else {
                                // 見つからなかったら、1つずらして、ループする
                                logger.DebugH($"COMBO NOT found");
                                var s = hotList[0];
                                if (s != null) {
                                    if (!s.IsComboShift || (s.IsUpKey && s.IsSingleHittable)) {
                                        // 自身がシフトキーでないか、UPされた単打可能キーの場合
                                        logger.DebugH(() => $"NormalKey or SingleHittable: comboList.Count={comboList._safeCount()}, isTimingCheckBroken={isTimingCheckBroken}");
                                        if (bSecondComboCheck || comboList._isEmpty() || isTimingCheckBroken) {
                                            // 2文字目以降か、comboKey が無いか、同時打鍵候補はあったが単打キーとの打鍵間隔の条件にマッチしなかった
                                            logger.DebugH(() => $"ADD: keyCode={hotList[0].OrigDecoderKey}");
                                            result.Add(s.OrigDecoderKey);
                                        }
                                    } else {
                                        logger.DebugH(() => $"ShiftKey");
                                        // 自身がシフトキーで
                                        if (comboList._isEmpty()) {
                                            // 持ち越されたシフトキーがまだ無い場合に限り
                                            if (hotList.Count > 1) {
                                                // 後続する未処理キーがある場合は、先頭キー(シフトキーのはず)を持ち越す
                                                copyToComboList(hotList, 1);
                                            }
                                        }
                                    }
                                }
                                overlapLen = 1;
                            }
                            hotList = hotList.Skip(overlapLen).ToList();
                            logger.DebugH(() => $"TRY NEXT: result={result._keyString()}, hotList={hotList._toString()}");
                        } // while(hotList)

                        logger.DebugH(() => $"END while: {ToDebugString()}");
                    }
                }

                // comboList のうちでUPされたものや同時打鍵のOneshotに使われたものや削除対象のものを削除する
                for (int i = comboList.Count - 1; i >= 0; --i) {
                    if (comboList[i].IsUpKey || comboList[i].ToBeRemoved) {
                        comboList.RemoveAt(i);
                    }
                }
                logger.DebugH(() => $"CLEANUP: UpKey or Oneshot in comboList Removed: {ToDebugString()}");

                // 6個以上の打鍵が残っていたら警告をログ出力する
                if (Count > 5) {
                    logger.Warn($"strokeList.Count={Count}");
                    if (Count > 10) {
                        logger.Warn($"Clear strokeList");
                        Clear();
                    }
                }

            } catch (Exception ex) {
                logger.Error(ex._getErrorMsg());
                Clear();
            }

            logger.DebugH(() => $"LEAVE: result={result?._keyString() ?? "null"}, {ToDebugString()}");
            return result;
        }

        /// <summary>同時打鍵を見つける<br/>見つかったら、処理された打鍵数を返す。見つからなかったら0を返す</summary>
        private int findCombo(List<int> result, List<Stroke> hotList, DateTime dtNow, bool bSecondComboCheck)
        {
            logger.DebugH(() => $"ENTER: hotList={hotList._toString()}, bSecondComboCheck={bSecondComboCheck}");

            // 持ち越したキーリストの部分リストからなる集合(リスト)
            List<List<Stroke>> subComboLists = gatherSubList(comboList);

            int overlapLen = hotList.Count;
            while (overlapLen >= 1) {
                logger.DebugH(() => $"WHILE: overlapLen={overlapLen}");
                foreach (var subList in subComboLists) {
                    int minLen = subList._isEmpty() ? 2 : 1;    // subList(comboListの部分列)が空なら、hotListのほうから2つ以上必要
                    logger.DebugH(() => $"FOREACH: subList={subList._toString()}, minLen={minLen}");
                    if (overlapLen < minLen) break;

                    var challengeList = makeComboChallengeList(subList, hotList.Take(overlapLen));
                    logger.DebugH(() => $"COMBO SEARCH: searchKey={challengeList._toString()}");

                    var keyCombo = KeyCombinationPool.CurrentPool.GetEntry(challengeList);
                    logger.DebugH(() => $"COMBO RESULT: combo={(keyCombo == null ? "(none)" : "FOUND")}, decKeyList={(keyCombo == null ? "(none)" : keyCombo.DecKeysDebugString())}, comboKeyList={(keyCombo == null ? "(none)" : keyCombo.ComboKeysDebugString())}");

                    if (keyCombo?.DecKeyList != null) {
                        Stroke tailKey = hotList[overlapLen - 1];
                        logger.DebugH(() => $"CHECK1: {tailKey.IsUpKey && tailKey.IsComboShift}: hotList[tailPos={overlapLen - 1}].IsUpKey && !IsComboShift");
                        logger.DebugH(() => $"CHECK2: {hotList[0].IsShiftableSpaceKey}: hotList[0].IsShiftableSpaceKey");
                        if (tailKey.IsUpKey && !tailKey.IsComboShift || // CHECK1: 対象リストの末尾キーが先にUPされた
                            hotList[0].IsShiftableSpaceKey ||           // CHECK2: 先頭キーがシフト可能なスペースキーだった⇒スペースキーならタイミングは考慮せず無条件
                            isCombinationTiming(challengeList, tailKey, dtNow, bSecondComboCheck)) {
                            // 同時打鍵が見つかった(かつ、同時打鍵の条件を満たしている)ので、それを出力する
                            logger.DebugH(() => $"COMBO CHECK PASSED: Overlap candidates found: overlapLen={overlapLen}, list={challengeList._toString()}");
                            result.AddRange(keyCombo.DecKeyList);
                            // 同時打鍵に使用したキーを使い回すかあるいは破棄するか
                            if (keyCombo.IsOneshotShift) {
                                // Oneshotなら使い回さず、今回かぎりとする
                                logger.DebugH(() => $"OneshotShift");
                            } else {
                                // Oneshot以外は使い回す
                                logger.DebugH(() => $"Move to next combination: overlapLen={overlapLen}");
                                copyToComboList(hotList, overlapLen);
                            }
                            // 見つかった
                            logger.DebugH(() => $"LEAVE: overlapLen={overlapLen}");
                            return overlapLen;
                        }
                    }
                }
                --overlapLen;
            }
            // 見つからなかった
            logger.DebugH(() => $"LEAVE: overlapLen=0");
            return 0;
        }

        private void copyToComboList(List<Stroke> list, int len)
        {
            foreach (var s in list.Take(len)) s.SetCombined();
            comboList.AddRange(list.Take(len));
        }

        /// <summary>同時打鍵のチャレンジ列を作成する</summary>
        /// <returns></returns>
        private List<Stroke> makeComboChallengeList(List<Stroke> strokes, IEnumerable<Stroke> addList)
        {
            var list = new List<Stroke>(strokes);
            list.AddRange(addList);
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

        // タイミングによる同時打鍵判定関数
        private bool isCombinationTiming(List<Stroke> list, Stroke tailStk, DateTime dtNow, bool bSecondComboCheck)
        {
            logger.DebugH(() => $"list={list._toString()}, tailStk={tailStk.DebugString()}");
            if (list._isEmpty()) return false;

            // タイミングチェック(1文字目ならリードタイムをチェック; 2文字目以降の場合は、対象キーダウンからシフトキーアップまでの時間によって判定)
            bool result = false;
            if (!bSecondComboCheck) {
                double ms1 = list[0].TimeSpanMs(tailStk);
                result = ms1 <= Settings.CombinationKeyMaxAllowedLeadTimeMs;
                logger.DebugH(() => $"RESULT1={result}: !bSecondComboCheck && ms1={ms1:f2}ms <= threshold={Settings.CombinationKeyMaxAllowedLeadTimeMs}ms");
            } else {
                result = list.Any(x => !x.IsUpKey && x.IsComboShift);   // まだUPされていないシフトキーがあるか
                if (result) {
                    logger.DebugH(() => $"RESULT2={result}: bSecondComboCheck && ALIVE SHIFT Key found");
                } else {
                    double ms2 = tailStk.TimeSpanMs(dtNow);
                    result = ms2 >= Settings.CombinationKeyMinOverlappingTimeMs;
                    logger.DebugH(() => $"RESULT2={result}: bSecondComboCheck && ms2={ms2:f2}ms >= threshold={Settings.CombinationKeyMinOverlappingTimeMs}ms");
                }
            }
            if (!result) isTimingCheckBroken = true;
            return result;
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

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
            if (list == null) {
                gatherSubList(new List<Stroke>(), result);
            } else {
                gatherSubList(list, result);
            }
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
            // 同時打鍵可でなければ何も返さない⇒同時打鍵判定をしない
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

        // 同時打鍵の組合せが見つかった(同時打鍵として判定されたかは不明)
        bool bComboFound = false;

        class KeyComboRule
        {
            // 0; D/C, 1:YES, -1:NO
            public bool bComboFound;
            public int kComboListEmpty;
            public int k1stSingle;
            public int k1stShift;
            public int k2ndSingle;
            public int k2ndShift;
            //public int kNo3rdKey;
            //public int kShiftKeyUp;
            public int outputLen;
            public int discardLen;
            public bool bMoveShift;

            //public KeyComboRule(bool bComboFound, int kComboEmpty, int k1stSingle, int k1stShift, int k2ndSingle, int k2ndShift, int kNo3rdKey, int kShiftKeyUp,
            //    int outLen, int discLen = -1, bool bMoveShift = true)
            //{
            //    this.bComboFound = bComboFound;
            //    this.kComboListEmpty = kComboEmpty;
            //    this.k1stSingle = k1stSingle;
            //    this.k1stShift = k1stShift;
            //    this.k2ndSingle = k2ndSingle;
            //    this.k2ndShift = k2ndShift;
            //    this.kNo3rdKey = kNo3rdKey;
            //    this.kShiftKeyUp = kShiftKeyUp;
            //    this.outputLen = outLen;
            //    this.discardLen = discLen >= 0 ? discLen : outLen > 0 ? outLen : 1;
            //    this.bMoveShift = bMoveShift;
            //}

            public KeyComboRule(bool bComboFound, int kComboEmpty, int k1stSingle, int k1stShift, int k2ndSingle, int k2ndShift,
                int outLen, int discLen = -1, bool bMoveShift = true)
            {
                this.bComboFound = bComboFound;
                this.kComboListEmpty = kComboEmpty;
                this.k1stSingle = k1stSingle;
                this.k1stShift = k1stShift;
                this.k2ndSingle = k2ndSingle;
                this.k2ndShift = k2ndShift;
                //this.kNo3rdKey = kNo3rdKey;
                //this.kShiftKeyUp = kShiftKeyUp;
                this.outputLen = outLen;
                this.discardLen = discLen >= 0 ? discLen : outLen > 0 ? outLen : 1;
                this.bMoveShift = bMoveShift;
            }
        }

        List<KeyComboRule> keyComboRules = new List<KeyComboRule>() {
            new KeyComboRule(true, 0, 1, 0, 0, 0, 1),   // COMBO有, ComboList/DC, 第1:単/DC, 第2:DC/DC ⇒ 1出, 1棄, MS
            //new KeyComboRule(true, 0, -1, 1, 0, 0, 0, 0, 0, 1, false),  // COMBO有, ComboList有, 第1:非/シ, 第2:DC/DC ⇒ 0出, 1棄, NS
            new KeyComboRule(false, 1, 1, 0, 1, 0, 2),  // COMBO無, ComboList無, 第1:単/DC, 第2:単/DC ⇒ 2出, 2棄, MS (薙刀「あい」「かい」「ある」「かる」)
            new KeyComboRule(false, -1, 1, 0, 0, 0, 1),   // COMBO無, ComboList有, 第1:単/DC, 第2:DC/DC ⇒ 1出, 1棄, MS (薙刀「ぶき」)
            //new KeyComboRule(false, -1, 0, 0, 0, 0, 0),   // COMBO無, ComboList有, 第1:DC/DC, 第2:DC/DC ⇒ 0出, 1棄, MS (薙刀「ある」「かる」)
            //new KeyComboRule(false, 1, 0, 0, 0, 0, 1),   // COMBO無, ComboList無, 第1:DC/シ, 第2:DC/DC, 第3:有 ⇒ 0出, 1棄, MS (薙刀「(かるすへ)⇒ずべ」
        };

        // 順次打鍵の場合に、シフトキーがUPされたら、一時的に同時打鍵検索をストップする
        bool bTemporaryComboDisabled = false;

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
                        result = new List<int>();

                        bool bSecondComboCheck = comboList._notEmpty();
                        logger.DebugH(() => $"START while: {ToDebugString()}, bSecondComboCheck={bSecondComboCheck}");

                        while (unprocList._notEmpty()) {
                            //同時打鍵を見つける
                            bComboFound = false;
                            // 持ち越したキーリストの部分リストからなる集合(リスト)
                            logger.DebugH(() => $"bTemporaryComboDisabled={bTemporaryComboDisabled}");
                            List<List<Stroke>> subComboLists = gatherSubList(bTemporaryComboDisabled ? null : comboList);
                            //List<List<Stroke>> subComboLists = gatherSubList(comboList);

                            int overlapLen = findCombo(result, subComboLists, unprocList, dtNow, bSecondComboCheck);
                            if (overlapLen > 0) {
                                // 見つかった
                                logger.DebugH($"COMBO FOUND");
                                bSecondComboCheck = true;
                            } else {
                                // 見つからなかった
                                logger.DebugH($"COMBO NOT found or TIMING CHECK FAILED");
                                var s = unprocList[0];
                                logger.DebugH(() => $"RULE TRY: comboList.Count={comboList.Count}, unprocList.Count={unprocList.Count}, ComboFound={bComboFound}, s.IsComboShift={s.IsComboShift}, s.IsSingle={s.IsSingleHittable}");
                                int outputLen = 0;
                                int discardLen = 1;
                                int copyShiftLen = 1;
                                if (comboList._isEmpty() && unprocList.Count == 1) {
                                    logger.DebugH($"NO SHIFT and SELF UP KEY and NO OTHER KEY");
                                    if (s.IsSingleHittable || s.IsSequentialShift) {
                                        logger.DebugH($"Single Hittable or SequentialShift");
                                        outputLen = 1;
                                    } else {
                                        logger.DebugH($"ABANDONED-1");
                                    }
                                } else {
                                    var t = unprocList._getNth(1);
                                    logger.DebugH(() => $"bComboFound={bComboFound}, ComboListEmpty={comboList._isEmpty()}, 1stSingle={s.IsSingleHittable}, 1stShift={s.IsComboShift}, 2ndSingle={t?.IsSingleHittable}, 2ndShift={t?.IsComboShift}, isShiftUP={comboList._notEmpty() && comboList.Any(x => x.IsUpKey)}");
                                    int n = 1;
                                    foreach (var rule in keyComboRules) {
                                        if (rule.bComboFound == bComboFound
                                            && (rule.kComboListEmpty == 0 || (rule.kComboListEmpty == 1) == comboList._isEmpty())
                                            && (rule.k1stSingle == 0 || (rule.k1stSingle == 1) == s.IsSingleHittable)
                                            && (rule.k1stShift == 0 || (rule.k1stShift == 1) == s.IsComboShift)
                                            && (rule.k2ndSingle == 0 || (rule.k2ndSingle == 1) == (t != null && t.IsSingleHittable))
                                            && (rule.k2ndShift == 0 || (rule.k2ndShift == 1) == (t != null && t.IsComboShift))
                                            //(rule.kShiftKeyUp == 0 || (rule.kShiftKeyUp == 1) == (comboList._notEmpty() && comboList.Any(x => x.IsUpKey))))
                                        ) {
                                            outputLen = rule.outputLen;
                                            discardLen = rule.discardLen._lowLimit(1);
                                            if (rule.bMoveShift) copyShiftLen = discardLen;
                                            logger.DebugH(() => $"RULE({n}) Applied: outputLen={outputLen}, discardLen={discardLen}, copyShiftLen={copyShiftLen}");
                                            break;
                                        }
                                        ++n;
                                    }
                                }
                                for (int i = 0; i < outputLen && i < unprocList.Count; ++i) {
                                    result.Add(unprocList[i].OrigDecoderKey);
                                }
                                if (copyShiftLen > 0) {
                                    copyToComboList(unprocList, copyShiftLen);
                                }
                                overlapLen = discardLen;
                            }
                            unprocList = unprocList.Skip(overlapLen).ToList();
                            logger.DebugH(() => $"TRY NEXT: result={result._keyString()}, {ToDebugString()}");
                        } // while(unprocList)

                        logger.DebugH(() => $"END while: {ToDebugString()}");
                    }
                }

                bool bSomeShiftKeyUp = comboList.Any(x => x.IsUpKey);

                // comboList のうちでUPされたものや同時打鍵のOneshotに使われたものや削除対象のものを削除する
                for (int i = comboList.Count - 1; i >= 0; --i) {
                    if (comboList[i].IsUpKey || comboList[i].ToBeRemoved) {
                        comboList.RemoveAt(i);
                    }
                }
                bTemporaryComboDisabled = KeyCombinationPool.CurrentPool.IsPrefixedSequentialShift && comboList._notEmpty() && bSomeShiftKeyUp;
                logger.DebugH(() => $"CLEANUP: UpKey or Oneshot in comboList Removed: bTemporaryComboDisabled={bTemporaryComboDisabled}, {ToDebugString()}");

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
        private int findCombo(List<int> result, List<List<Stroke>> subComboLists, List<Stroke> hotList, DateTime dtNow, bool bSecondComboCheck)
        {
            logger.DebugH(() => $"ENTER: hotList={hotList._toString()}, bSecondComboCheck={bSecondComboCheck}");

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
                    logger.DebugH(() => $"COMBO RESULT: keyCombo.decKeyList={(keyCombo == null ? "(none)" : keyCombo.DecKeysDebugString())}, comboKeyList={(keyCombo == null ? "(none)" : keyCombo.ComboKeysDebugString())}");

                    if (keyCombo?.DecKeyList != null) {
                        bComboFound = true; // 同時打鍵の組合せが見つかった
                        Stroke tailKey = hotList[overlapLen - 1];
                        logger.DebugH(() => $"CHECK0: {!hotList[0].IsSingleHittable}: hotList[0] NOT SINGLE");
                        logger.DebugH(() => $"CHECK1: {tailKey.IsUpKey && hotList[0].IsComboShift && tailKey.IsSingleHittable}: hotList[0].IsComboShift={hotList[0].IsComboShift} and hotList[tailPos={overlapLen - 1}].IsUpKey && IsSingle");
                        logger.DebugH(() => $"CHECK2: {hotList[0].IsShiftableSpaceKey}: hotList[0].IsShiftableSpaceKey");
                        if (tailKey.IsUpKey && tailKey.IsSingleHittable && hotList[0].IsComboShift || // CHECK1: 対象リストの末尾キーが先にUPされた
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
            foreach (var s in list.Take(len)) {
                if (s.IsComboShift) {
                    s.SetCombined();
                    comboList.Add(s);
                }
            }
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
                if (!KeyCombinationPool.CurrentPool.ContainsUnorderedShiftKey) {
                    // 相互シフトを含まないならば、1文字目の時間制約は気にしない
                    result = true;
                    logger.DebugH(() => $"RESULT1={result}: !bSecondComboCheck && !ContainsUnorderedShiftKey={!KeyCombinationPool.CurrentPool.ContainsUnorderedShiftKey}");
                } else {
                    double ms1 = list[0].TimeSpanMs(tailStk);
                    result = ms1 <= Settings.CombinationKeyMaxAllowedLeadTimeMs;
                    logger.DebugH(() => $"RESULT1={result}: !bSecondComboCheck && ms1={ms1:f2}ms <= threshold={Settings.CombinationKeyMaxAllowedLeadTimeMs}ms");
                }
            } else {
                result = list.Any(x => x.OrigDecoderKey != tailStk.OrigDecoderKey && !x.IsUpKey && x.IsComboShift);   // まだUPされていないシフトキーがあるか
                if (result) {
                    logger.DebugH(() => $"RESULT2={result}: bSecondComboCheck && ALIVE SHIFT Key found");
                } else {
                    double ms2 = tailStk.TimeSpanMs(dtNow);
                    result = ms2 >= Settings.CombinationKeyMinOverlappingTimeMs;
                    logger.DebugH(() => $"RESULT2={result}: bSecondComboCheck && ms2={ms2:f2}ms >= threshold={Settings.CombinationKeyMinOverlappingTimeMs}ms");
                }
            }
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

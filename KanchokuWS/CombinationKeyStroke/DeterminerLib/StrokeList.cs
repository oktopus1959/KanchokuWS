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

        public int ComboListCount => comboList.Count;
        public int UnprocListCount => unprocList.Count;
        public int Count => comboList.Count + unprocList.Count;

        public void ClearComboList()
        {
            comboList.Clear();
        }

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

        public bool IsSuccessiveShift2ndOr3rdKey()
        {
            bool result = KeyCombinationPool.CurrentPool.ContainsSuccessiveShiftKey &&
                (comboList.Count >= 1 && unprocList.Count == 1 ||
                 comboList.Count == 0 && unprocList.Count >= 3);
            logger.DebugH(() =>  $"Result={result}; ContainsSuccessiveShiftKey={KeyCombinationPool.CurrentPool.ContainsSuccessiveShiftKey}, comboList.Count={comboList.Count}, unprocList.Count={unprocList.Count}");
            return result;
        }

        public bool IsSuccessiveShift3rdOrLaterKey()
        {
            bool result = KeyCombinationPool.CurrentPool.ContainsSuccessiveShiftKey && Count >= 3;
            logger.DebugH(() =>  $"Result={result}; ContainsSuccessiveShiftKey={KeyCombinationPool.CurrentPool.ContainsSuccessiveShiftKey}, comboList.Count={comboList.Count}, unprocList.Count={unprocList.Count}");
            return result;
        }

        // 押下の場合
        public List<int> GetKeyCombinationWhenKeyDown(out bool bTimer, out bool bUnconditional)
        {
            bTimer = false;
            bUnconditional = false;

            // 同時打鍵可でなければ何も返さない⇒同時打鍵判定をしない
            if (!KeyCombinationPool.CurrentPool.ContainsComboShiftKey) {
                logger.DebugH("No combo shift key");
                return null;
            }

            // 1つは非単打キーがある
            bool anyNotSingleHittable()
            {
                return unprocList.Any(x => !x.IsSingleHittable);
            }

            // 連続シフトの場合は、同時打鍵キーの数は最大2とする
            (List<int>, bool) getAndCheckCombo(List<Stroke> list)
            {
                var keyCombo = KeyCombinationPool.CurrentPool.GetEntry(list);
                logger.DebugH(() =>
                    $"combo={(keyCombo == null ? "(none)" : "FOUND")}, decKeyList={(keyCombo == null ? "(none)" : keyCombo.DecKeysDebugString())}, " +
                    $"Terminal={keyCombo?.IsTerminal ?? false}, comboKeyList={(keyCombo == null ? "(none)" : keyCombo.ComboKeysDebugString())}");
                if (keyCombo != null && keyCombo.DecKeyList != null && keyCombo.IsTerminal) {
                    logger.DebugH("COMBO CHECK PASSED");
                    return (new List<int>(keyCombo.DecKeyList), true);
                }
                logger.DebugH("COMBO CHECK FAILED");
                return (null, keyCombo != null);
            }

            List<int> result = null;
            bool bKeyComboFound = false;
            if (comboList._isEmpty() && unprocList.Count == 2) {
                // 最初の同時打鍵のケース
                logger.DebugH("Try first successive combo");
                (result, bKeyComboFound) = getAndCheckCombo(unprocList);
                if (result != null) {
                    // 同時打鍵候補があった
                    if (unprocList[0].IsPrefixShift ||
                        (((unprocList[0].IsComboShift && unprocList[1].TimeSpanMs(unprocList[0]) <= Settings.CombinationKeyMaxAllowedLeadTimeMs) ||
                          (!unprocList[0].IsComboShift && unprocList[1].TimeSpanMs(unprocList[0]) <= Settings.ComboKeyMaxAllowedPostfixTimeMs)) &&
                        (Settings.CombinationKeyMinTimeOnlyAfterSecond || Settings.CombinationKeyMinOverlappingTimeMs <= 0 || anyNotSingleHittable()))) {
                        // 前置シフトであるか、または第2打鍵までの時間が閾値以下で即時判定あるいは親指シフトのように非単打キーを含む場合
                        if (KeyCombinationPool.CurrentPool.ContainsSuccessiveShiftKey) {
                            // 連続シフトの場合は、同時打鍵に使用したキーを使い回す
                            comboList.Add(unprocList[0].IsComboShift ? unprocList[0] : unprocList[1]);
                            comboList[0].SetCombined();
                        }
                        unprocList.Clear();
                    } else {
                        // どちらも単打を含むため、未確定の場合は、タイマーを有効にする
                        result = null;
                        bTimer = true;
                    }
                }
            } else if (Settings.CombinationKeyMinOverlappingTimeMs <= 0) {
                // 2文字目以降または3キー以上の同時押し状態で、即時判定の場合
                logger.DebugH("Imediate Combo check");
                if (comboList.Count >= 1 && unprocList.Count == 1) {
                    // 2文字目以降のケース
                    logger.DebugH(() => $"Try second or later successive combo: bTemporaryComboDisabled={bTemporaryComboDisabled}");
                    if (bTemporaryComboDisabled) {
                        // 連続シフト版「月光」などで、先行してShiftキーが送出され、一時的に同時打鍵がOFFになっている場合
                        (result, bKeyComboFound) = getAndCheckCombo(Helper.MakeList(unprocList[0]));
                        bTemporaryComboDisabled = false;
                    } else {
                        (result, bKeyComboFound) = getAndCheckCombo(Helper.MakeList(comboList, unprocList[0]));
                    }
                    logger.DebugH(() => $"result={(result != null ? "FOUND" : "(empty)")}, bKeyComboFound={bKeyComboFound}");
                    if (result != null || !bKeyComboFound) {
                        // コンボがなくてもキーを削除しておく(たとえば月光でDを長押ししてKを押したような場合は、何も出力せず、Kも除去する)
                        // ただし、薙刀式で k→j→w とほぼ同時に押したときに w が除去されるのはまずいので、コンボが見つかったら、削除はしない
                        logger.DebugH("Remove first unproc key");
                        unprocList.RemoveAt(0);
                    }
                } else {
                    // 
                    logger.DebugH("First successive combo and unproc key num >= 3. Combo check will be done at key release");
                }
            } else {
                logger.DebugH("Combo check will be done at key release");
            }

            bUnconditional = DecoderKeys.IsEisuComboDeckey(result._getFirst());
            return result;
        }

        // 同時打鍵の組合せが見つかった(同時打鍵として判定されたかは不明)
        //bool bComboFound = false;

        class KeyComboRule
        {
            // 0; D/C, 1:YES, -1:NO
            public int timingFailure;       // 0:Combo有、D/C, 1:Combo有、1文字目チェックNG, 2:Combo有、2文字目チェックNG, -1: Combo無し
            public int anyComboKeyUp;       // 0:D/C, 1:Up有、-1:Up無
            public int kComboListEmpty;
            public int k1stSingle;          // 0:D/C, 1:単打,         -1:非単打
            public int k1stShift;           // 0:D/C, 1:同時, 2:順次, -1:非シフト
            public int k2ndSingle;          // 0:D/C, 1:単打,         -1:非単打
            public int k2ndShift;           // 0:D/C, 1:同時, 2:順次, -1:非シフト
            public int outputLen;
            public int discardLen;
            //public bool bMoveShift;

            public KeyComboRule(int timingFailure, int anyKeyUp, int kComboEmpty, int k1stSingle, int k1stShift, int k2ndSingle, int k2ndShift,
                int outLen, int discLen = -1/*, bool bMoveShift = true*/)
            {
                this.timingFailure = timingFailure;
                this.anyComboKeyUp = anyKeyUp;
                this.kComboListEmpty = kComboEmpty;
                this.k1stSingle = k1stSingle;
                this.k1stShift = k1stShift;
                this.k2ndSingle = k2ndSingle;
                this.k2ndShift = k2ndShift;
                this.outputLen = outLen;
                this.discardLen = discLen >= 0 ? discLen : outLen > 0 ? outLen : 1;
                //this.bMoveShift = bMoveShift;
            }

            public bool Apply(int timingFail, bool keyUp, List<Stroke> comboList, Stroke s, Stroke t)
            {
                return (timingFailure == timingFail || timingFailure == 0 && timingFail > 0)
                    && (anyComboKeyUp == 0 || (anyComboKeyUp == 1) == keyUp)
                    && (kComboListEmpty == 0 || (kComboListEmpty == 1) == comboList._isEmpty())
                    && (k1stSingle == 0 || (k1stSingle == 1) == s.IsSingleHittable)
                    && (k1stShift == 0 || (k1stShift == 1) == s.IsComboShift || (k1stShift == 2) == s.IsSequentialShift)
                    && (k2ndSingle == 0 || (k2ndSingle == 1) == (t != null && t.IsSingleHittable))
                    && (k2ndShift == 0 || (k2ndShift == 1) == (t != null && t.IsComboShift) || (k2ndShift == 2) == (t != null && t.IsSequentialShift))
                    ;
            }
        }

        List<KeyComboRule> keyComboRules = new List<KeyComboRule>() {
            new KeyComboRule(1, 0, 0, 1, 0, 0, 0, 1),           // COMBO有:NG1, UP:DC, ComboList/DC, 第1:単/DC, 第2:DC/DC ⇒ 1出, 1棄, MS
            new KeyComboRule(2, 1, 0, 0, 0, 0, 0, 1),           // COMBO有:NG2, UP:有, ComboList/DC, 第1:DC/DC, 第2:DC/DC ⇒ 1出, 1棄, MS
            new KeyComboRule(2, -1, 0, 1, 0, 0, 0, 0, 0),       // COMBO有:NG2, UP:無, ComboList/DC, 第1:単/DC, 第2:DC/DC ⇒ 0出, 0棄, MS
            //new KeyComboRule(true, 0, 0, -1, 1, 0, 0, 0, 0, 0, 1, false),  // COMBO有, ComboList有, 第1:非/シ, 第2:DC/DC ⇒ 0出, 1棄, NS
            //new KeyComboRule(false, 0, 1, 1, 0, 1, 0, 2),      // COMBO:無, UP:DC, ComboList:無, 第1:単/DC, 第2:単/DC ⇒ 2出, 2棄, MS (薙刀「あい」「かい」「ある」「かる」)
            //new KeyComboRule(false, 0, 1, 1, 0, 0, 0, 1),      // COMBO:無, UP:DC, ComboList:空, 第1:単/DC, 第2:DC/DC ⇒ 1出, 1棄, MS (のに「OKA⇒ため」)
            //new KeyComboRule(false, 0, -1, 1, 0, 0, 0, 1),     // COMBO:無, UP:DC, ComboList:有, 第1:単/DC, 第2:DC/DC ⇒ 1出, 1棄, MS (薙刀「ぶき」)
            new KeyComboRule(-1, 0, 0, 1, 0, 0, 0, 1),      // COMBO:無, UP:DC, ComboList:DC, 第1:単/DC, 第2:DC/DC ⇒ 1出, 1棄, MS (薙刀「ぶき」)
            new KeyComboRule(-1, 0, 0, 0, 2, 0, 0, 1),      // COMBO:無, UP:DC, ComboList:DC, 第1:DC/順, 第2:DC/DC ⇒ 1出, 1棄, MS (のに「KDkFdf⇒にょ」)
            //new KeyComboRule(false, 0, -1, 0, 0, 0, 0, 0),   // COMBO:無, ComboList:有, 第1:DC/DC, 第2:DC/DC ⇒ 0出, 1棄, MS (薙刀「ある」「かる」)
            //new KeyComboRule(false, 0, 1, 0, 0, 0, 0, 1),   // COMBO無, ComboList無, 第1:DC/シ, 第2:DC/DC, 第3:有 ⇒ 0出, 1棄, MS (薙刀「(かるすへ)⇒ずべ」
        };

        // 順次打鍵の場合に、シフトキーがUPされたら、一時的に同時打鍵検索をストップする
        bool bTemporaryComboDisabled = false;

        // 解放の場合
        public List<int> GetKeyCombinationWhenKeyUp(int decKey, DateTime dtNow, bool bDecoderOn, out bool bUnconditional)
        {
            logger.DebugH(() => $"ENTER: decKey={decKey}, dt={dtNow.ToString("HH:mm:ss.fff")}");

            List<int> result = null;
            bUnconditional = false;

            try {
                bool bPrevSequential = bTemporaryComboDisabled;

                int upComboIdx = findAndMarkUpKey(comboList, decKey);

                if (unprocList._notEmpty()) {
                    int upKeyIdx = findAndMarkUpKey(unprocList, decKey);
                    logger.DebugH(() => $"upComboIdx={upComboIdx}, upKeyIdx={upKeyIdx}");

                    if (upComboIdx >= 0 || upKeyIdx >= 0) {
                        result = new List<int>();

                        bool bSecondComboCheck = comboList._notEmpty();
                        logger.DebugH(() => $"START while: {ToDebugString()}, bSecondComboCheck={bSecondComboCheck}");

                        while (unprocList._notEmpty()) {
                            // 持ち越したキーリストの部分リストからなる集合(リスト)
                            logger.DebugH(() => $"bPrevSequential={bPrevSequential}");
                            //List<List<Stroke>> subComboLists = gatherSubList(bPrevSequential ? null : comboList);
                            List<List<Stroke>> subComboLists = gatherSubList(comboList);
                            //List<List<Stroke>> subComboLists = gatherSubList(comboList);

                            int outputLen = 0;
                            int discardLen = 1;
                            int copyShiftLen = 1;

                            if (comboList._isEmpty() && unprocList.Count == 1) {
                                logger.DebugH($"NO COMBO SHIFT and JUST 1 UNPROC KEY");
                                var s = unprocList[0];
                                if (s.IsUpKey || !s.IsComboShift) {
                                    logger.DebugH($"JUST 1 UNPROC KEY is UP KEY");
                                    if (s.IsSingleHittable || s.IsSequentialShift) {
                                        logger.DebugH($"Single Hittable or SequentialShift");
                                        outputLen = 1;
                                    } else {
                                        logger.DebugH($"ABANDONED-1");
                                    }
                                } else {
                                    // UPされていないシフトキーがある。多分、最初のループで処理されずに残ったものがRETRYで対象となった。
                                    // 次のUPのときに処理するのでこのまま残す。以前はこれをここで出力していたので、余分な出力となっていた。
                                    // 薙刀式での K→J→W で J の処理はそれ以降に任せるということ。
                                    logger.DebugH($"JUST 1 UNPROC KEY is NOT UP KEY. Maybe RETRY and it's SHIFT KEY. BREAK.");
                                    break;
                                }
                            } else if (bPrevSequential) {
                                logger.DebugH(() => $"PrevSequential={bPrevSequential}");
                                var keyCombo = findComboAny(subComboLists, unprocList);
                                if (keyCombo != null) {
                                    logger.DebugH(() => $"COMBO FOUND (PrevSequential)");
                                    outputLen = 1;
                                    if (keyCombo.IsTerminal) {
                                        // ここで順次シフトは終わり
                                        bPrevSequential = false;
                                        logger.DebugH(() => $"COMBO IS TERMINAL. PrevSequential={bPrevSequential}");
                                    }
                                } else {
                                    logger.DebugH($"NO SEQUENTIAL COMBO. ABANDONED-2");
                                }
                            } else {
                                //同時打鍵を見つける
                                int timingFailure = -1;
                                int overlapLen = findCombo(result, subComboLists, unprocList, dtNow, bSecondComboCheck, bDecoderOn, out timingFailure);
                                if (overlapLen > 0) {
                                    // 見つかった
                                    logger.DebugH($"COMBO FOUND");
                                    bSecondComboCheck = true;
                                    outputLen = copyShiftLen = 0;  // 既に findCombo() の中でやっている
                                    discardLen = overlapLen;
                                } else {
                                    // 見つからなかった
                                    bool bComboFound = timingFailure >= 0;
                                    logger.DebugH(() => bComboFound ? (bPrevSequential ? "COMBO FOUND but PREV SEQUENTIAL" : $"COMBO FOUND but TIMING CHECK FAILED: {timingFailure}") : "COMBO NOT FOUND");
                                    bool bSomeKeyUp = unprocList.Any(x => x.IsUpKey);
                                    var s = unprocList[0];
                                    logger.DebugH(() => $"comboList.Count={comboList.Count}, unprocList.Count={unprocList.Count}, ComboFound={bComboFound}, s.IsComboShift={s.IsComboShift}, s.IsSingle={s.IsSingleHittable}");
                                    var t = unprocList._getNth(1);
                                    logger.DebugH(() => $"RULE TRY: bComboFound={bComboFound}, someKeyUp={bSomeKeyUp}, ComboListEmpty={comboList._isEmpty()}, 1stSingle={s.IsSingleHittable}, 1stShift={s.IsComboShift}, 2ndSingle={t?.IsSingleHittable}, 2ndShift={t?.IsComboShift}, isShiftUP={comboList._notEmpty() && comboList.Any(x => x.IsUpKey)}");
                                    int n = 1;
                                    foreach (var rule in keyComboRules) {
                                        if (rule.Apply(timingFailure, bSomeKeyUp, comboList, s, t)) {
                                            outputLen = rule.outputLen;
                                            discardLen = rule.discardLen;
                                            //if (rule.bMoveShift) copyShiftLen = discardLen;
                                            copyShiftLen = discardLen;
                                            //if (rule.k1stShift == 2) bTemporaryComboDisabled = true;    // 順次打鍵なら次は一時的に同時打鍵判定をやめる
                                            bPrevSequential = (rule.k1stShift == 2);
                                            logger.DebugH(() => $"RULE({n}) APPLIED: outputLen={outputLen}, discardLen={discardLen}, copyShiftLen={copyShiftLen}, bPrevSequential={bPrevSequential}");
                                            break;
                                        }
                                        ++n;
                                    }
                                    if (n > keyComboRules.Count) {
                                        logger.DebugH("NO RULE APPLIED");
                                        bPrevSequential = false;
                                    }
                                }
                            }
                            logger.DebugH(() => $"outputLen={outputLen}, copyShiftLen={copyShiftLen}, discardLen={discardLen}");
                            for (int i = 0; i < outputLen && i < unprocList.Count; ++i) {
                                // Upされていない連続シフトキーは出力しない ⇒ これをやると薙刀式で JE と打ったのが同時打鍵と判定されなかったときに E(て)が出力されなくなるので、やめる
                                //if (unprocList[i].IsUpKey || !unprocList[i].IsSuccessiveShift) {
                                    result.Add(unprocList[i].OrigDecoderKey);
                                //}
                                // 同時打鍵でなく出力されたキーは comboList には移さない
                                unprocList[i].SetToBeRemoved();
                                logger.DebugH(() => $"ADD: result={result._keyString()}");
                            }
                            if (copyShiftLen > 0) {
                                // true: 連続シフトキーのみ、comboListに移す
                                copyToComboList(unprocList, copyShiftLen/*, true*/);
                                logger.DebugH(() => $"copyToComboList={copyShiftLen}, successiveOnly: {ToDebugString()}");
                            }
                            if (discardLen > 0) {
                                unprocList = unprocList.Skip(discardLen).ToList();
                                logger.DebugH(() => $"discardLen={discardLen}: {ToDebugString()}");
                            } else {
                                // 強制的に終了する
                                break;
                            }
                            if (unprocList._notEmpty() && Settings.AbandonUsedKeysWhenSpecialComboShiftDown && unprocList[0].IsSpaceOrFuncComboShift) {
                                // Spaceまたは機能キーのシフトキーがきたら、使い終わったキーを破棄する
                                logger.DebugH(() => $"Abandon Used Keys When Special Combo Shift Down");
                                comboList.Clear();
                            }
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
                bTemporaryComboDisabled = comboList._notEmpty() && (bPrevSequential || KeyCombinationPool.CurrentPool.IsPrefixedOrSequentialShift && bSomeShiftKeyUp);
                logger.DebugH(() => $"CLEANUP: UpKey or Oneshot in comboList Removed: bTemporaryComboDisabled={bTemporaryComboDisabled}, {ToDebugString()}");

                // 指定個数以上の打鍵が残っていたら警告をログ出力する
                if (Count >= Settings.WarnThresholdKeyQueueCount) {
                    logger.Warn($"strokeList.Count={Count}");
                    if (Count >= Settings.WarnThresholdKeyQueueCount + 5) {
                        // さらにそれを5個以上、上回っていたら、安全のためキューをクリアしておく
                        logger.Warn($"Clear strokeList");
                        Clear();
                    }
                }

            } catch (Exception ex) {
                logger.Error(ex._getErrorMsg());
                Clear();
            }

            bUnconditional = DecoderKeys.IsEisuComboDeckey(result._getFirst());
            logger.DebugH(() => $"LEAVE: result={result?._keyString() ?? "null"}, {ToDebugString()}");
            return result;
        }

        /// <summary>同時打鍵を見つける<br/>見つかったら、処理された打鍵数を返す。見つからなかったら0を返す</summary>
        private int findCombo(List<int> result, List<List<Stroke>> subComboLists, List<Stroke> hotList, DateTime dtNow, bool bSecondComboCheck, bool bDecoderOn, out int timingFailure)
        {
            logger.DebugH(() => $"ENTER: hotList={hotList._toString()}, bSecondComboCheck={bSecondComboCheck}");

            int timingResult = -1;

            int findFunc()
            {
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
                            logger.DebugH(() => $"COMBO RESULT: keyCombo.decKeyList={(keyCombo == null ? "(none)" : keyCombo.DecKeysDebugString())}, HasString={keyCombo?.HasString ?? false}, comboKeyList={(keyCombo == null ? "(none)" : keyCombo.ComboKeysDebugString())}");

                        if (keyCombo != null && keyCombo.DecKeyList != null && keyCombo.HasString) {
                            //bComboFound = true; // 同時打鍵の組合せが見つかった
                            timingResult = 0;  // 同時打鍵の組合せが見つかった
                            Stroke tailKey = hotList[overlapLen - 1];
                            bool isTailKeyUp = hotList.Skip(overlapLen - 1).Any(x => x.IsUpKey);    // 末尾キー以降のキーがUPされた
                            logger.DebugH(() => $"CHECK1: {isTailKeyUp && tailKey.IsSingleHittable}: tailPos={overlapLen - 1}: isTailKeyUp && tailKey.IsSingleHittable");
                            logger.DebugH(() => $"CHECK2: {hotList[0].IsShiftableSpaceKey}: hotList[0].IsShiftableSpaceKey");
                            logger.DebugH(() => $"CHECK3: {Settings.ThreeKeysComboUnconditional && keyCombo.DecKeyList._safeCount() >= 3 && !Settings.SequentialPriorityWordKeyStringSet.Contains(challengeList._toString())}: challengeList={challengeList._toString()}");
                            if (isTailKeyUp && tailKey.IsSingleHittable ||  // CHECK1: 対象リストの末尾キーが単打可能キーであり先にUPされた
                                hotList[0].IsShiftableSpaceKey ||           // CHECK2: 先頭キーがシフト可能なスペースキーだった⇒スペースキーならタイミングは考慮せず無条件
                                (Settings.ThreeKeysComboUnconditional && keyCombo.DecKeyList._safeCount() >= 3 && !Settings.SequentialPriorityWordKeyStringSet.Contains(challengeList._toString())) ||
                                                                            // CHECK3: 3打鍵以上の同時打鍵で、順次優先でなければタイミングチェックをやらない
                                (timingResult = isCombinationTiming(challengeList, tailKey, dtNow, bSecondComboCheck)) == 0)
                                                                            // CHECK1～CHECK3をすり抜けたらタイミングチェックをやる
                            {
                                // 同時打鍵が見つかった(かつ、同時打鍵の条件を満たしている)ので、それを出力する
                                logger.DebugH(() => $"COMBO CHECK PASSED: Overlap candidates found: overlapLen={overlapLen}, list={challengeList._toString()}");
                                result.AddRange(keyCombo.DecKeyList);
                                // 同時打鍵に使用したキーを使い回すかあるいは破棄するか
                                if (keyCombo.IsOneshotShift) {
                                    // Oneshotなら使い回さず、今回かぎりとする
                                    logger.DebugH(() => $"OneshotShift");
                                } else {
                                    // Oneshot以外のシフトキーは使い回す
                                    logger.DebugH(() => $"Move to next combination: overlapLen={overlapLen}");
                                    copyToComboList(hotList, overlapLen/*, false*/);
                                    // 連続シフトキー以外はコピーしないようにした (薙刀式で J,W,Pが3打同時でなく、J,Wだけの同時と判定されたときに、Jだけをコピーするため)
                                }
                                // 見つかった
                                return overlapLen;
                            }
                        }
                    }
                    // 見つからなかった
                    --overlapLen;
                }
                return overlapLen;
            }

            int resultLen = findFunc();

            timingFailure = timingResult;

            logger.DebugH(() => $"LEAVE: {(resultLen == 0 ? "NOT ": "")}FOUND: result={result._keyString()}, timingFailure={timingResult}, overlapLen={resultLen}: {ToDebugString()}");
            return resultLen;
        }

        private KeyCombination findComboAny(List<List<Stroke>> subComboLists, List<Stroke> hotList)
        {
            logger.DebugH(() => $"ENTER: hotList={hotList._toString()}");

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
                    if (keyCombo != null) return keyCombo;
                }
                --overlapLen;
            }
            // 見つからなかった
            logger.DebugH(() => $"LEAVE: overlapLen=0");
            return null;
        }

        //private void copyToComboList(List<Stroke> list, int len, bool bSuccessiveShiftOnly)
        //{
        //    foreach (var s in list.Take(len)) {
        //        if (!s.ToBeRemoved && (s.IsSuccessiveShift || (!bSuccessiveShiftOnly && !s.IsUpKey))) {
        //            // !bSuccessiveShiftOnly なら非連続シフトキーでもKeyUpされていなければ comboListに移す
        //            if (s.IsSuccessiveShift) s.SetCombined();
        //            comboList.Add(s);
        //        }
        //    }
        //}

        /// <summary>削除対象でない連続シフトキーを</summary>
        private void copyToComboList(List<Stroke> list, int len)
        {
            foreach (var s in list.Take(len)) {
                if (!s.ToBeRemoved && s.IsSuccessiveShift) {
                    if (s.IsSuccessiveShift) s.SetCombined();
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
            //ちょっと意図が不明なので、コメントアウトしておく
            //たとえば、Spaceキーを同時打鍵のシフトキーとしている場合は、下記処理があると3打鍵以上の同時打鍵ができなくなる
            //if (list.Count >= 3) {
            //    // 3個以上のキーを含むならば、スペースのような weakShift を削除する
            //    for (int i = 0; i < list.Count; ++i) {
            //        if (list[i].ModuloDecKey == DecoderKeys.STROKE_SPACE_DECKEY) {
            //            logger.DebugH(() => $"DELETE weakShift at {i}");
            //            list.RemoveAt(i);
            //            break;
            //        }
            //    }
            //}
            return list;
        }

        // タイミングによる同時打鍵判定関数
        // result: 0: 判定OK, 1:1文字目チェックNG, 2:2文字目チェックNG
        private int isCombinationTiming(List<Stroke> list, Stroke tailStk, DateTime dtNow, bool bSecondComboCheck)
        {
            logger.DebugH(() => $"list={list._toString()}, tailStk={tailStk.DebugString()}, bSecondComboCheck={bSecondComboCheck}");
            if (list._isEmpty()) return -1;

            int result = 0;
            if (!bSecondComboCheck) {
                // 1文字目ならリードタイムをチェック
                if (!KeyCombinationPool.CurrentPool.ContainsUnorderedShiftKey) {
                    // 相互シフトを含まないならば、1文字目の時間制約は気にしない
                    result = 0;
                    logger.DebugH(() => $"RESULT1={result == 0}: !bSecondComboCheck (True) && !ContainsUnorderedShiftKey={!KeyCombinationPool.CurrentPool.ContainsUnorderedShiftKey}");
                } else {
                    double ms1 = list[0].TimeSpanMs(tailStk);
                    result =
                        (list[0].IsComboShift && ms1 <= Settings.CombinationKeyMaxAllowedLeadTimeMs) ||
                        (!list[0].IsComboShift && ms1 <= Settings.ComboKeyMaxAllowedPostfixTimeMs)
                        ? 0 : 1;
                    logger.DebugH(() =>
                        $"RESULT1={result == 0}: !bSecondComboCheck (True) && ms1={ms1:f2}ms <= " +
                        $"threshold={Settings.CombinationKeyMaxAllowedLeadTimeMs}ms/{Settings.ComboKeyMaxAllowedPostfixTimeMs}ms (Timing={result})");
                }
            }
            if (bSecondComboCheck || (result == 0 && !Settings.CombinationKeyMinTimeOnlyAfterSecond)) {
                // 2文字目であるか、または、1文字目のリードタイムチェックをパスし、かつ、1文字目でも重複時間チェックが必要
                result = list.Any(x => x.OrigDecoderKey != tailStk.OrigDecoderKey && !x.IsUpKey && x.IsJustComboShift) ? 0 : 2;   // まだUPされていない非単打シフトキーがあるか
                if (result == 0) {
                    // 非単打シフトキーがまだ解放されずに残っていたら同時打鍵と判定する
                    logger.DebugH(() => $"RESULT2={result == 0}: bSecondComboCheck && ALIVE SHIFT Key found");
                } else {
                    // シフトキーが解放されている(または単打可能キーのみである)ので、最後のキー押下時刻との差分を求め、タイミング判定する
                    double ms2 = tailStk.TimeSpanMs(dtNow);
                    result = ms2 >= Settings.CombinationKeyMinOverlappingTimeMs ? 0 : 2;
                    logger.DebugH(() => $"RESULT2={result == 0}: ms2={ms2:f2}ms >= threshold={Settings.CombinationKeyMinOverlappingTimeMs}ms (Timing={result})");
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

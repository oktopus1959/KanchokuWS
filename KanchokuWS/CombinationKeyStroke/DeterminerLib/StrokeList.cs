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
        private static Logger logger = Logger.GetLogger();

        /// <summary>
        /// 押下中のキーのストロークリスト
        /// </summary>
        private List<Stroke> downKeyList = new List<Stroke>();

        /// <summary>
        /// キー押下によって追加された未処理のストロークリスト
        /// </summary>
        private List<Stroke> unprocList = new List<Stroke>();

        /// <summary>
        /// 同時打鍵検索によって処理されたが、次の同時打鍵検索にも使用されうるストロークリスト
        /// </summary>
        private List<Stroke> comboList = new List<Stroke>();

        /// <summary>前回のComboシフトキーが解放された時刻をシフトキーとともに記憶しておく</summary>
        class ComboShiftUpTimeInfo
        {
            public int PrevShiftKey { get; set; } = 0;
            public DateTime UpDt { get; set; }
            public bool ComboPressed { get; set; } = false;

            public void Initialize()
            {
                PrevShiftKey = 0;
                UpDt = DateTime.MinValue;
                ComboPressed = false;
                logger.InfoH(() => $"LEAVE: PrevShiftKey={PrevShiftKey}, UpDt={UpDt}, ComboPressed={ComboPressed}");
            }

            public void SetValues(int shiftDeckey, DateTime upDt)
            {
                PrevShiftKey = shiftDeckey;
                UpDt = upDt;
                ComboPressed = false;
                logger.InfoH(() => $"LEAVE: PrevShiftKey={PrevShiftKey}, UpDt={UpDt}, ComboPressed={ComboPressed}");
            }

            public void CheckComboKeyDown(int deckey)
            {
                logger.InfoH(() => $"ENTER: PrevShiftKey={PrevShiftKey}, deckey={deckey}, ComboPressed={ComboPressed}");
                if (PrevShiftKey != deckey) {
                    if (ComboPressed) {
                        logger.InfoH(() => $"ComboPressed: DO Initialize");
                        Initialize();
                    } else {
                        logger.InfoH(() => $"transit to ComboPressed");
                        ComboPressed = true;
                    }
                }
                logger.InfoH(() => $"LEAVE: PrevShiftKey={PrevShiftKey}, deckey={deckey}, ComboPressed={ComboPressed}");
            }

            public DateTime GetPrevComboShiftKeyUpDt(int shiftDeckey)
            {
                return DecoderKeys.IsSpaceOrFuncKey(shiftDeckey) && PrevShiftKey == shiftDeckey ? UpDt : DateTime.MinValue;
            }
        }

        // TODO: このあたりは不要のはず。後で削除する
        ///// <summary>
        ///// 前回のComboシフトキーが解放された時刻をシフトキーごとに保存するマップ<br/>
        ///// シフトキーの解放後、後置シフトを無効にする時間を計測する起点となる
        ///// </summary>
        //private Dictionary<int, DateTime> prevComboShiftKeyUpDtMap = new Dictionary<int, DateTime>();

        /// <summary>
        /// 前回のComboシフトキーが解放された時刻をシフトキーとともに記憶しておく<br/>
        /// シフトキーの解放後、後置シフトを無効にする時間を計測する起点となる
        /// </summary>
        private ComboShiftUpTimeInfo comboShiftUpTimeInfo = new ComboShiftUpTimeInfo();

        //public DateTime GetPrevComboShiftKeyUpDt(int deckey)
        //{
        //    return prevComboShiftKeyUpDtMap._safeGet(deckey, DateTime.MinValue);
        //}

        /// <summary>直前のShiftキーからの経過時間を取得<br/>shiftStrokeはSpaceまたは機能キー</summary>
        public double GetElapsedTimeFromPrevShiftKeyUp(Stroke stroke1, Stroke shiftStroke)
        {
            return stroke1.TimeSpanMs(comboShiftUpTimeInfo.GetPrevComboShiftKeyUpDt(shiftStroke.OrigDecoderKey));
        }

        public void SetPrevComboShiftKeyUpDt(int deckey, DateTime dt)
        {
            //prevComboShiftKeyUpDtMap[deckey] = dt;
            comboShiftUpTimeInfo.SetValues(deckey, dt);
        }

        public void ClearPrevComboShiftKeyUpDt(int deckey)
        {
            //prevComboShiftKeyUpDtMap[deckey] = DateTime.MinValue;
            comboShiftUpTimeInfo.Initialize();
        }

        public void CheckComboShiftKeyUpDt(int deckey)
        {
            comboShiftUpTimeInfo.CheckComboKeyDown(deckey);
        }

        public bool IsComboBlocker()
        {
            var list = new List<Stroke>(comboList)._addRange(unprocList);
            logger.InfoH(() => $"ENTER: list={list._toString()}");
            bool result = KeyCombinationPool.CurrentPool.GetEntry(list)?.IsComboBlocked == true;
            logger.InfoH(() => $"LEAVE: comboBlocker={result}");
            return result;
        }

        /// <summary>
        /// 与えられたリストの部分リストからなる集合(リスト)を返す(空リストも含む)
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
                        // i番目を削除した削除したリスト
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

        public void ClearUnprocList()
        {
            logger.InfoH(() => $"CALLED");
            unprocList.Clear();
        }

        public void ClearComboList()
        {
            logger.InfoH(() => $"CALLED");
            comboList.Clear();
        }

        public void Clear()
        {
            logger.InfoH(() => $"CALLED");
            downKeyList.Clear();
            comboList.Clear();
            unprocList.Clear();
        }

        public bool IsEmpty()
        {
            return Count == 0;
        }

        public bool IsComboListEmpty => comboList.Count == 0;

        public bool IsUnprocListEmpty => unprocList.Count == 0;

        public Stroke FirstUnprocKey => unprocList._getFirst();

        public Stroke SecondUnprocKey => unprocList._getSecond();

        public Stroke GetNthUnprocKey(int n) { return unprocList._getNth(n); }

        public Stroke LastUnprocKey => unprocList._getLast();

        public bool IsDownKeyListEmpty => downKeyList.Count == 0;

        public KeyCombination GetKeyCombo()
        {
            return KeyCombinationPool.CurrentPool.GetEntry(unprocList);
        }

        public KeyCombination GetKeyComboMutual()
        {
            return KeyCombinationPool.CurrentPool.GetEntry(unprocList, false);
        }

        public bool IsTerminalCombo()
        {
            return ComboListCount == 0 && (GetKeyCombo()?.IsTerminal ?? false);
        }

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
            downKeyList.Add(s);
            unprocList.Add(s);
        }

        public void RemoveUpKey(int decKey)
        {
            for (int i = 0; i < downKeyList.Count; i++) {
                if (downKeyList[i].OrigDecoderKey == decKey) {
                    downKeyList.RemoveAt(i);
                    break;
                }
            }
        }

        public bool IsSuccessiveShift2ndOr3rdKey()
        {
            bool result = KeyCombinationPool.CurrentPool.ContainsSuccessiveShiftKey &&
                (comboList.Count >= 1 && unprocList.Count == 1 ||
                 comboList.Count == 0 && unprocList.Count >= 3);
            logger.InfoH(() =>  $"Result={result}; ContainsSuccessiveShiftKey={KeyCombinationPool.CurrentPool.ContainsSuccessiveShiftKey}, comboList.Count={comboList.Count}, unprocList.Count={unprocList.Count}");
            return result;
        }

        public bool IsSuccessiveShift3rdOrLaterKey()
        {
            bool result = KeyCombinationPool.CurrentPool.ContainsSuccessiveShiftKey && Count >= 3;
            logger.InfoH(() =>  $"Result={result}; ContainsSuccessiveShiftKey={KeyCombinationPool.CurrentPool.ContainsSuccessiveShiftKey}, comboList.Count={comboList.Count}, unprocList.Count={unprocList.Count}");
            return result;
        }

        // 押下の場合
        public List<int> GetKeyCombinationWhenKeyDown(out bool bTimer, out bool bUnconditional)
        {
            bTimer = false;
            bUnconditional = false;

            // 同時打鍵可でなければ何も返さない⇒同時打鍵判定をしない
            if (!KeyCombinationPool.CurrentPool.ContainsComboShiftKey) {
                logger.InfoH("No combo shift key");
                return null;
            }

            // 1つは非単打キーがある
            bool anyNotSingleHittable()
            {
                return unprocList.Any(x => !x.HasStringOrSingleHittable);
            }

            KeyCombination keyCombo = null;

            // 連続シフトの場合は、同時打鍵キーの数は最大2とする
            List<int> getAndCheckCombo(List<Stroke> list)
            {
                logger.InfoH(() => $"getAndCheckCombo: list={list._toString()}");
                keyCombo = KeyCombinationPool.CurrentPool.GetEntry(list);
                logger.InfoH(() =>
                    $"combo={(keyCombo == null ? "(none)" : "FOUND")}, decKeyList={(keyCombo == null ? "(none)" : keyCombo.DecKeysDebugString())}, " +
                    $"Terminal={keyCombo?.IsTerminal ?? false}, isComboBlocked={keyCombo?.IsComboBlocked ?? false}, isStackLike={keyCombo?.IsStackLikeCombo ?? false}" +
                    $"OnlyCharKeysComboShouldBeCoveringCombo={Settings.OnlyCharKeysComboShouldBeCoveringCombo}, ContainsTwoCharacterKeys={keyCombo?.ContainsTwoCharacterKeys ?? false}" +
                    $"comboKeyList={(keyCombo == null ? "(none)" : keyCombo.ComboKeysString())}");
                if (keyCombo != null && keyCombo.DecKeyList != null && (keyCombo.IsTerminal || keyCombo.IsComboBlocked) &&
                    !((Settings.OnlyCharKeysComboShouldBeCoveringCombo || keyCombo.IsStackLikeCombo) && keyCombo.ContainsTwoCharacterKeys)) {
                    logger.InfoH("COMBO CHECK PASSED");
                    return new List<int>(keyCombo.DecKeyList);
                }
                logger.InfoH("KeyDown COMBO CHECK FAILED");
                return null;
            }

            logger.InfoH(() => $"ENTER: IsTemporaryComboDisabled={IsTemporaryComboDisabled}");

            List<int> result = null;
            //if (comboList._isEmpty() && unprocList.Count == 2)
            if (Count == 2 && unprocList.Count >= 1) {
                // 2キーの同時打鍵のケースの場合のみを扱う
                logger.InfoH("Try 2 keys combo");
                // comboList.Count == 0 なら unprocList.Count == 2 である
                bool isStrk1Unproc = comboList._isEmpty();
                var strk1 = isStrk1Unproc ? unprocList[0] : comboList[0];
                var strk2 = isStrk1Unproc ? unprocList[1] : unprocList[0];
                result = getAndCheckCombo(Helper.MakeList(strk1, strk2));
                if (result != null) {
                    // 同時打鍵候補があった
                    double shiftTimeSpan = strk2.TimeSpanMs(strk1);
                    double shiftUpElapse = GetElapsedTimeFromPrevShiftKeyUp(strk1, strk2);
                    bool stroke1ShiftCond() => strk1.IsComboShift && (!isStrk1Unproc || shiftTimeSpan <= Settings.CombinationKeyMaxAllowedLeadTimeMs);
                    bool stroke2ShiftCond() => strk2.IsComboShift &&
                           (Settings.ComboDisableIntervalTimeMs <= 0 || shiftUpElapse >= Settings.ComboDisableIntervalTimeMs) &&
                           shiftTimeSpan <= Settings.ComboKeyMaxAllowedPostfixTimeMs;
                    if (Logger.IsInfoEnabled) {
                        logger.InfoH("KeyDown COMBO FOUND:");
                        logger.InfoH(() => $"stroke-1: {(isStrk1Unproc ? "unprocList[0]" : "comboList[0]")}, {(strk1.IsComboShift ? "IS" : "NOT")} ComboShift");
                        logger.InfoH(() => $"stroke-2: {(isStrk1Unproc ? "unprocList[1]" : "unprocList[0]")}, {(strk2.IsComboShift ? "IS" : "NOT")} ComboShift");
                        logger.InfoH(() => $"!isStrk1Unproc({!isStrk1Unproc}) || shiftTimeSpan={(int)shiftTimeSpan:f1}ms <= maxAllowedTime={Settings.CombinationKeyMaxAllowedLeadTimeMs}");
                        logger.InfoH(() => $"shiftUpElapse={shiftUpElapse:f1}ms >= ComboDisableIntervalTimeMs={Settings.ComboDisableIntervalTimeMs}");
                        logger.InfoH(() => $"shiftTimeSpan={(int)shiftTimeSpan:f1}ms <= ComboKeyMaxAllowedPostfixTimeMs={Settings.ComboKeyMaxAllowedPostfixTimeMs}");
                        logger.InfoH(() => $"IsComboBlocked => {keyCombo.IsComboBlocked} || \n" +
                            $"(stroke-1.IsPrefixShift({strk1.IsPrefixShift}) && (comboList._isEmpty={isStrk1Unproc} || " +
                                $"(ComboKeyMinOverlappingTimeMs({Settings.CombinationKeyMinOverlappingTimeMs}) <= 0)({Settings.CombinationKeyMinOverlappingTimeMs <= 0}))) => " +
                                $"{strk1.IsPrefixShift && (isStrk1Unproc || Settings.CombinationKeyMinOverlappingTimeMs <= 0)} || \n" +
                            $"((stroke1Cond({stroke1ShiftCond()}) || stroke2Cond({stroke2ShiftCond()})):{stroke1ShiftCond() || stroke2ShiftCond()} && \n" +
                            $"    (ComboKeyMinTimeOnlyAfterSecond({Settings.CombinationKeyMinTimeOnlyAfterSecond}) || " +
                                $"ComboKeyMinOverlappingTimeMs({Settings.CombinationKeyMinOverlappingTimeMs}) <= 0({Settings.CombinationKeyMinOverlappingTimeMs <= 0}) || " +
                                $"anyNotSingleHittable({anyNotSingleHittable()}))) => " +
                                $"{(stroke1ShiftCond() || stroke2ShiftCond()) && (Settings.CombinationKeyMinTimeOnlyAfterSecond || Settings.CombinationKeyMinOverlappingTimeMs <= 0 || anyNotSingleHittable())}");
                    }
                    if (keyCombo.IsComboBlocked || (strk1.IsPrefixShift && (isStrk1Unproc || Settings.CombinationKeyMinOverlappingTimeMs <= 0)) ||
                        // (ComboBlockerの場合) または (前置シフトの1文字目であるか、2文字目でも重複時間判定がない場合)、
                        (!strk1.IsPrefixShift && isStrk1Unproc && (stroke1ShiftCond() || stroke2ShiftCond()) &&
                         (Settings.CombinationKeyMinTimeOnlyAfterSecond || Settings.CombinationKeyMinOverlappingTimeMs <= 0 || anyNotSingleHittable()))) {
                        // または (前置シフトでない1文字目であり、1文字目には第2打鍵までの時間制約を適用しないか、第2打鍵までの時間が閾値以下で即時判定あるいは親指シフトのように非単打キーを含む場合)
                        if (isStrk1Unproc && KeyCombinationPool.CurrentPool.ContainsSuccessiveShiftKey) {
                            // 連続シフトの場合は、同時打鍵に使用したキーを使い回す
                            comboList.Add(strk1.IsComboShift ? strk1 : strk2);
                            comboList[0].SetCombined();
                        }
                        unprocList.Clear();
                        // 同時打鍵列の処理をした場合は、これ以降順次打鍵になるか否かをチェックしておく
                        IsTemporaryComboDisabled = keyCombo.IsComboBlocked;
                        logger.InfoH(() => $"KeyDown COMBO result={result._keyString()}, IsTemporaryComboDisabled={IsTemporaryComboDisabled}");
                    } else {
                        // どちらも単打を含むため、未確定の場合は、タイマーを有効にする
                        result = null;
                        bTimer = true;
                        logger.InfoH("KeyDown COMBO Undetermined. Return NULL result");
                    }
                } else if (keyCombo == null && unprocList[0].HasString /*HasDecKeyList*/) {
                    logger.InfoH("combo NOT found. Return first key as is");
                    // 同時打鍵候補がないので、最初のキーをそのまま返す
                    result = Helper.MakeList(unprocList[0].OrigDecoderKey);
                    unprocList.RemoveAt(0); // 最初のキーを削除
                    if (unprocList._notEmpty()) {
                        // 2番目のキーのチェック
                        if (unprocList[0].IsJustSingleHit) {
                            logger.InfoH("second key is just SingleHit. Return second key as is");
                            result.Add(unprocList[0].OrigDecoderKey);
                            unprocList.RemoveAt(0);
                        } else if (unprocList[0].HasStringOrSingleHittable && unprocList[0].HasString /*unprocList[0].HasDecKeyList*/) {
                            logger.InfoH("second key is SingleHittable and HasString. Enable timer");
                            bTimer = true;
                        }
                    }
                }
            } else if (Settings.CombinationKeyMinOverlappingTimeMs <= 0) {
                // 2文字目以降または3キー以上の同時押し状態で、即時判定の場合
                logger.InfoH("Imediate Combo check");
                if (comboList.Count >= 1 && unprocList.Count == 1) {
                    // 2文字目以降のケース
                    logger.InfoH(() => $"Try second or later successive combo: bTemporaryComboDisabled={IsTemporaryComboDisabled}");
                    if (IsTemporaryComboDisabled) {
                        // 連続シフト版「月光」などで、先行してShiftキーが送出され、一時的に同時打鍵がOFFになっている場合
                        result = getAndCheckCombo(Helper.MakeList(unprocList[0]));
                        IsTemporaryComboDisabled = false;
                    } else {
                        result = getAndCheckCombo(Helper.MakeList(comboList, unprocList[0]));
                    }
                    logger.InfoH(() => $"result={(result != null ? "FOUND" : "(empty)")}, keyCombo={keyCombo.DecKeysDebugString()}");
                    if (result != null || keyCombo == null) {
                        // コンボがなくてもキーを削除しておく(たとえば月光でDを長押ししてKを押したような場合は、何も出力せず、Kも除去する)
                        // ただし、薙刀式で k→j→w とほぼ同時に押したときに w が除去されるのはまずいので、コンボが見つかったら、削除はしない
                        logger.InfoH("Remove first unproc key");
                        unprocList.RemoveAt(0);
                    }
                } else {
                    // 
                    logger.InfoH("First successive combo and unproc key num >= 3. Combo check will be done at key release");
                }
            } else {
                logger.InfoH("Combo check will be done at key release");
            }

            bUnconditional = DecoderKeys.IsEisuComboDeckey(result._getFirst());

            logger.InfoH(() => $"LEAVE: IsTemporaryComboDisabled={IsTemporaryComboDisabled}");
            return result;
        }

        /// <summary>ComboBlockerなどによって一時的に同時打鍵を無効化して順次打鍵になっているか</summary>
        public bool IsTemporaryComboDisabled { get; set; }  = false;

        // 解放の場合
        public List<int> GetKeyCombinationWhenKeyUp(int decKey, DateTime dtNow, bool bDecoderOn, out bool bUnconditional)
        {
            logger.InfoH(() => $"ENTER: decKey={decKey}, IsTemporaryComboDisabled={IsTemporaryComboDisabled}, dt={dtNow.ToString("HH:mm:ss.fff")}");

            List<int> result = new List<int>();
            bUnconditional = false;

            try {
                bool bTempComboDisabled = IsTemporaryComboDisabled;

                if (comboList._isEmpty() && unprocList.Count > 2 && unprocList[0].IsUpKey && unprocList[1].IsSpaceOrFuncComboShift) {
                    // "S SPC s X spc" または "S SPC s X x" のような状況。S を単打として出力する。
                    logger.InfoH(() => $"Output first Character stroke: {unprocList[0].OrigDecoderKey}");
                    result.Add(unprocList[0].OrigDecoderKey);
                    unprocList = unprocList.Skip(1).ToList();
                }
                int upComboIdx = findAndMarkUpKey(comboList, decKey);

                if (unprocList._notEmpty()) {
                    int upKeyIdx = findAndMarkUpKey(unprocList, decKey);
                    int upKeyIdxFromTail = upKeyIdx >= 0 ? unprocList._safeCount() - upKeyIdx - 1 : -1;
                    logger.InfoH(() => $"upComboIdx={upComboIdx}, upKeyIdx={upKeyIdx}, upKeyIdxFromTail={upKeyIdxFromTail}");

                    if (upComboIdx >= 0 || upKeyIdx >= 0) {
                        bool bSecondComboCheck = comboList._notEmpty();
                        logger.InfoH(() => $"START while: {ToDebugString()}, bSecondComboCheck={bSecondComboCheck}");

                        HashSet<string> challengedSet = new HashSet<string>();

                        while (unprocList._notEmpty() /*&& (upKeyIdxFromTail < 0 || unprocList.Count > upKeyIdxFromTail)*/) {
                            // 持ち越したキーリストの部分リストからなる集合(リスト)
                            logger.InfoH(() => $"bTempComboDisabled={bTempComboDisabled}");
                            //List<List<Stroke>> subComboLists = gatherSubList(bTempComboDisabled ? null : comboList);

                            bool bForceOutput = false;
                            int outputLen = 0;
                            int discardLen = 1;
                            int copyShiftLen = 1;

                            if (comboList._isEmpty() && unprocList.Count == 1) {
                                logger.InfoH($"NO COMBO SHIFT and JUST 1 UNPROC KEY");
                                var s = unprocList[0];
                                logger.InfoH(() => $"unprocList.First={s.DebugString()}");
                                if (s.IsUpKey /*|| !s.IsComboShift*/) {
                                    logger.InfoH($"JUST 1 UNPROC KEY is UP KEY");
                                    if (s.IsSingleHittable || s.IsSequentialShift) {
                                        // 単打可能または順次シフトだった
                                        logger.InfoH(() => $"IsSingleHittable={s.IsSingleHittable} or SequentialShift={s.IsSequentialShift}");
                                        outputLen = 1;
                                    } else {
                                        logger.InfoH(() => $"ABANDONED-1: IsSingleHittable={s.IsSingleHittable} and SequentialShift={s.IsSequentialShift}");
                                    }
                                } else if (!s.IsComboShift) {
                                    logger.InfoH($"JUST 1 UNPROC KEY is NOT UP KEY and NOT SHIFT KEY. BREAK.");
                                    break;
                                } else {
                                    // UPされていないシフトキーがある。多分、最初のループで処理されずに残ったものがRETRYで対象となった。
                                    // 次のUPのときに処理するのでこのまま残す。以前はこれをここで出力していたので、余分な出力となっていた。
                                    // 薙刀式での K→J→W で J の処理はそれ以降に任せるということ。
                                    logger.InfoH($"JUST 1 UNPROC KEY is NOT UP KEY. Maybe RETRY and it's SHIFT KEY. BREAK.");
                                    break;
                                }
                            } else if (bTempComboDisabled) {
                                // 同時打鍵が一時的に無効化されているので、順次打鍵として扱う
                                logger.InfoH(() => $"bTempComboDisabled={bTempComboDisabled}");
                                bForceOutput = true;
                                outputLen = 1;
                                copyShiftLen = 0;
                                discardLen = 1;
                                logger.InfoH(() => $"ADD: result={result._keyString()}");
                            } else {
                                //同時打鍵を見つける
                                List<List<Stroke>> subComboLists = gatherSubList(comboList);
                                int overlapLen = findCombo(result, challengedSet, subComboLists, unprocList, upKeyIdxFromTail, dtNow, bSecondComboCheck, bDecoderOn, out bTempComboDisabled);
                                if (overlapLen > 0) {
                                    // 見つかった
                                    logger.InfoH(() => $"COMBO FOUND: bTempComboDisabled={bTempComboDisabled}");
                                    bSecondComboCheck = true;
                                    outputLen = copyShiftLen = 0;  // 既に findCombo() の中でやっている
                                    discardLen = overlapLen;
                                } else {
                                    // 見つからなかった
                                    //logger.InfoH($"COMBO NOT FOUND");
                                    if (comboList._isEmpty() && unprocList.Count == 2 && upKeyIdx == 0 && unprocList[1].IsSpaceOrFuncComboShift) {
                                        // 未処理状態で、1打鍵めがUpされ、2打鍵めがSpaceなどのシフトキーだったら、ペンディングとする
                                        // "S SPC s" のような状況。 SPCが S を修飾するか否かは次の打鍵のタイミング次第。
                                        // "S SPC s spc" なら SPC は S を後置修飾する。 
                                        logger.InfoH(() => $"Pending post shift: first={unprocList[0].OrigDecoderKey}, second={unprocList[1].OrigDecoderKey}");
                                        break;
                                    }
                                    if (upKeyIdxFromTail >= 0 && unprocList.Count > upKeyIdxFromTail) {
                                        outputLen = discardLen = unprocList.Count - upKeyIdxFromTail;
                                        copyShiftLen = outputLen - 1;
                                        logger.InfoH(() => $"COMBO NOT FOUND: outputLen={outputLen}, copyShiftLen={copyShiftLen}");
                                    } else {
                                        logger.InfoH(() => $"COMBO NOT FOUND: break; upKeyIdxFromTail={upKeyIdxFromTail}, unprocList.Count={unprocList.Count}");
                                        break;
                                    }
                                }
                            }
                            logger.InfoH(() => $"outputLen={outputLen}, copyShiftLen={copyShiftLen}, discardLen={discardLen}");
                            for (int i = 0; i < outputLen && i < unprocList.Count; ++i) {
                                // Upされていない連続シフトキーは出力しない ⇒ と思ったが、これをやると薙刀式で JE と打ったのが同時打鍵と判定されなかったときに E(て)が出力されなくなるので、やめる
                                //if (unprocList[i].IsUpKey || !unprocList[i].IsSuccessiveShift)
                                var s = unprocList[i];
                                // 強制出力か文字を持つか単打可能か順次シフトキーの場合だけ、出力する
                                if (bForceOutput || s.HasDecKeyList || s.HasStringOrSingleHittable || s.IsSequentialShift) {
                                    result.Add(s.OrigDecoderKey);
                                    logger.InfoH(() => $"ADD: result={result._keyString()}");
                                }
                            }
                            if (copyShiftLen > 0) {
                                // true: 連続シフトキーのみ、comboListに移す
                                copyToComboList(unprocList, copyShiftLen/*, true*/);
                                logger.InfoH(() => $"copyToComboList={copyShiftLen}, successiveOnly: {ToDebugString()}");
                            }
                            if (discardLen > 0) {
                                unprocList = unprocList.Skip(discardLen).ToList();
                                logger.InfoH(() => $"discardLen={discardLen}: {ToDebugString()}");
                            } else {
                                // 強制的に終了する
                                break;
                            }
                            if (unprocList._notEmpty() && Settings.AbandonUsedKeysWhenSpecialComboShiftDown && unprocList[0].IsSpaceOrFuncComboShift) {
                                // Spaceまたは機能キーのシフトキーがきたら、使い終わったキーを破棄する
                                logger.InfoH(() => $"Abandon Used Keys When Special Combo Shift Down");
                                comboList.Clear();
                            }
                            logger.InfoH(() => $"TRY NEXT: result={result._keyString()}, {ToDebugString()}");
                        } // while(unprocList)

                        logger.InfoH(() => $"END while: {ToDebugString()}");
                    }
                }

                bool bSomeShiftKeyUp = comboList.Any(x => x.IsUpKey);

                // comboList のうちでUPされたものや同時打鍵のOneshotに使われたものや削除対象のものを削除する
                for (int i = comboList.Count - 1; i >= 0; --i) {
                    if (comboList[i].IsUpKey || comboList[i].ToBeRemoved) {
                        comboList.RemoveAt(i);
                    }
                }
                //bTemporaryUnconditional = comboList._notEmpty() && (bTempUnconditional || KeyCombinationPool.CurrentPool.IsPrefixedOrSequentialShift && bSomeShiftKeyUp);
                //IsTemporaryComboDisabled = comboList._notEmpty() && bTempComboDisabled;
                IsTemporaryComboDisabled = bTempComboDisabled;
                logger.InfoH(() => $"CLEANUP: UpKey or Oneshot in comboList Removed: bTemporaryComboDisabled={IsTemporaryComboDisabled}, {ToDebugString()}");

                // 指定個数以上の打鍵が残っていたら警告をログ出力する
                if (Count >= Settings.WarnThresholdKeyQueueCount) {
                    logger.WarnH($"strokeList.Count={Count}");
                    if (Count >= Settings.WarnThresholdKeyQueueCount + 5) {
                        // さらにそれを5個以上、上回っていたら、安全のためキューをクリアしておく
                        logger.WarnH($"Clear strokeList");
                        Clear();
                    }
                }

            } catch (Exception ex) {
                logger.Error(ex._getErrorMsg());
                Clear();
            }

            bUnconditional = DecoderKeys.IsEisuComboDeckey(result._getFirst());
            logger.InfoH(() => $"LEAVE: result={result?._keyString() ?? "null"}, IsTemporaryComboDisabled={IsTemporaryComboDisabled}, {ToDebugString()}");
            return result;
        }

        /// <summary>同時打鍵を見つける<br/>見つかったら、処理された打鍵数を返す。見つからなかったら0を返す</summary>
        private int findCombo(List<int> result, HashSet<string> challengedSet, List<List<Stroke>> subComboLists, List<Stroke> unprocList,
            int upKeyIdxFromTail, DateTime dtNow, bool bSecondComboCheck, bool bDecoderOn, out bool bComboBlocked)
        {
            logger.InfoH(() => $"ENTER: unprocList={unprocList._toString()}, upKeyIdxFromTail={upKeyIdxFromTail}, bSecondComboCheck={bSecondComboCheck}");

            int timingResult = -1;
            bool comboBlocked = false;

            int findFunc()
            {
                int overlapLen = unprocList.Count;
                int upKeyIdx = upKeyIdxFromTail >= 0 ? unprocList.Count - upKeyIdxFromTail - 1 : -1;
                while (overlapLen >= 1) {
                    logger.InfoH(() => $"WHILE: overlapLen={overlapLen}");
                    foreach (var subList in subComboLists) {
                        int minLen = subList._isEmpty() ? 2 : 1;    // subList(comboListの部分列)が空なら、hotListのほうから2つ以上必要
                        logger.InfoH(() => $"FOREACH: subList={subList._toString()}, minLen={minLen}");
                        if (overlapLen < minLen) break;

                        var challengeList = makeComboChallengeList(subList, unprocList.Take(overlapLen));
                        var challengeStr = challengeList._toString();
                        logger.InfoH(() => $"COMBO SEARCH: challengeList={challengeStr}");
                        if (challengedSet.Contains(challengeList._toString())) {
                            logger.InfoH(() => $"challengeList={challengeStr} ALREADY TRIED, SKIP");
                            continue;
                        }
                        challengedSet.Add(challengeStr);

                        //bool isTailKeyUp = unprocList.Skip(overlapLen - 1).Any(x => x.IsUpKey);    // 末尾キー以降のキーがUPされた
                        bool isTailKeyUp = upKeyIdx >= 0 && upKeyIdx >= overlapLen - 1;    // 末尾キー以降のキーがUPされた
                        logger.InfoH(() => $"isTailKeyUp={isTailKeyUp} upKeyIdx={upKeyIdx} overlapLen={overlapLen}");

                        var keyCombo = KeyCombinationPool.CurrentPool.GetEntry(challengeList, isTailKeyUp);
                        logger.InfoH(() => $"COMBO CANDIDATE {(keyCombo == null ? "NOT " : "")}FOUND");

                        if (keyCombo != null) {
                            // 同時打鍵の組合せが見つかった(有効なキーコード列を持たない部分Comboかもしれないが)
                            bool bEffectiveComboFound = keyCombo.DecKeyList != null && (keyCombo.HasDecoderOutput || keyCombo.IsComboBlocked) &&
                                (!((Settings.OnlyCharKeysComboShouldBeCoveringCombo && keyCombo.ContainsTwoCharacterKeys) || keyCombo.IsStackLikeCombo) || isTailKeyUp);
                            if (Logger.IsInfoEnabled) {
                                logger.InfoH(() => $"EFFECTIVE COMBO: {bEffectiveComboFound}: comboKeyList={(keyCombo == null ? "(none)" : keyCombo.ComboKeysString())}");
                                logger.InfoH(() => $"    keyCombo.decKeyList={(keyCombo == null ? "(none)" : keyCombo.DecKeysDebugString())} && " +
                                    $" (HasDecoderOutput={keyCombo.HasDecoderOutput} || IsComboBlocked={keyCombo.IsComboBlocked}) ");
                                logger.InfoH(() => $" && !((OnlyCharKeysComboShouldBeCoveringCombo={!Settings.OnlyCharKeysComboShouldBeCoveringCombo} || " +
                                    $"IsStackLikeCombo={keyCombo.IsStackLikeCombo}) && " +
                                    $"ContainsTwoCharacterKeys={!keyCombo.ContainsTwoCharacterKeys}) || isTailKeyUp={isTailKeyUp})");
                            }
                            if (bEffectiveComboFound) {
                                // 有効な同時打鍵の組合せが見つかった
                                timingResult = 0;
                                comboBlocked = keyCombo.IsComboBlocked;     // 同時打鍵の一時無効化か
                                Stroke tailKey = unprocList[overlapLen - 1];
                                if (Logger.IsInfoEnabled) {
                                    logger.InfoH(() =>
                                        //$"CHECK1: {isTailKeyUp && (comboBlocked || challengeList[0].IsShiftableSpaceKey || (tailKey.HasStringOrSingleHittable && !tailKey.IsShiftableSpaceKey))}: " +
                                        //$"tailPos={overlapLen - 1}: " +
                                        //$"isTailKeyUp({isTailKeyUp}) && " +
                                        //$"(comboBlocked({comboBlocked}) || challengeList[0].IsShiftableSpaceKey={challengeList[0].IsShiftableSpaceKey} || " +
                                        //$"(tailKey.HasStringOrSingleHittable({tailKey.HasStringOrSingleHittable}) && !tailKey.IsShiftableSpaceKey({!tailKey.IsShiftableSpaceKey})))");
                                        $"CHECK1: {isTailKeyUp}: isTailKeyUp({isTailKeyUp})");
                                    logger.InfoH(() => $"CHECK2: {challengeList.Count < 3 && unprocList[0].IsShiftableSpaceKey}: " +
                                        $"challengeList.Count({challengeList.Count}) < 3 ({challengeList.Count < 3}) && unprocList[0].IsShiftableSpaceKey({unprocList[0].IsShiftableSpaceKey})");
                                    logger.InfoH(() => "CHECK3: " +
                                        $"{Settings.ThreeKeysComboUnconditional && keyCombo.DecKeyList._safeCount() >= 3 && !isListContaindInSequentialPriorityWordKeySet(challengeList)}: " +
                                        $"ThreeKeysComboUnconditional({Settings.ThreeKeysComboUnconditional}) && " +
                                        $"keyCombo.DecKeyList.Count({keyCombo.DecKeyList._safeCount()}) >= 3 ({keyCombo.DecKeyList._safeCount() >= 3}) && " +
                                        $"!isListContaindInSequentialPriorityWordKeySet({challengeList._toString()})({!isListContaindInSequentialPriorityWordKeySet(challengeList)})" +
                                        $": challengeList={challengeList._toString()}");
                                }
                                //if ((isTailKeyUp && (comboBlocked || challengeList[0].IsShiftableSpaceKey || (tailKey.HasStringOrSingleHittable && !tailKey.IsShiftableSpaceKey))) ||
                                //      // CHECK1: 対象リストの末尾キーが先にUPされており、同時打鍵の一時無効化か、先頭キーがシフト可能スペースキーか、末尾キーが単打可能キーだった
                                if (isTailKeyUp ||
                                      // CHECK1: 対象リストの末尾キーが先にUPされていた(NICOLAで 「J<200>NFER<30>nfer<30>j」は200msの間が空いてもNFERが有効になって「ど」になってしまうことに注意)
                                    challengeList.Count < 3 && unprocList[0].IsShiftableSpaceKey ||
                                      // CHECK2: チャレンジリストの長さが2以下で、先頭キーがシフト可能なスペースキーだった
                                      // ⇒連続シフトでない、最初のスペースキーとの同時打鍵ならタイミングは考慮せず無条件
                                    (Settings.ThreeKeysComboUnconditional && keyCombo.DecKeyList._safeCount() >= 3 && !isListContaindInSequentialPriorityWordKeySet(challengeList)) ||
                                      // CHECK3: 3打鍵以上の同時打鍵で、順次優先でなければタイミングチェックをやらない
                                    (timingResult = isCombinationTiming(keyCombo, challengeList, tailKey, dtNow, bSecondComboCheck)) == 0)
                                      // CHECK1～CHECK3をすり抜けたらタイミングチェックをやる
                                {
                                    // 同時打鍵が見つかった(かつ、同時打鍵の条件を満たしている)ので、それを出力する
                                    logger.InfoH(() => $"COMBO CHECK PASSED: Overlap candidates found: overlapLen={overlapLen}, list={challengeList._toString()}");
                                    result.AddRange(keyCombo.DecKeyList);
                                    // 同時打鍵に使用したキーを使い回すかあるいは破棄するか
                                    if (keyCombo.IsOneshotShift) {
                                        // Oneshotなら使い回さず、今回かぎりとする
                                        logger.InfoH(() => $"OneshotShift");
                                    } else {
                                        // Oneshot以外のシフトキーは使い回す
                                        logger.InfoH(() => $"Move to next combination: overlapLen={overlapLen}");
                                        copyToComboList(unprocList, overlapLen/*, false*/);
                                        // 連続シフトキー以外はコピーしないようにした (薙刀式で J,W,Pが3打同時でなく、J,Wだけの同時と判定されたときに、Jだけをコピーするため)
                                    }
                                    // 見つかった
                                    return overlapLen;
                                } else {
                                    logger.InfoH(() => $"COMBO EFFECTIVE but COMBO CHECK FAILED; next");
                                }
                            } else if (upKeyIdx < 0) {
                                // どの未処理キーも解放されていないので、次のアクションを待つ
                                logger.InfoH(() => $"COMBO NOT EFFECTIVE but NO unprocKey Up; return 0");
                                return 0;
                            } else {
                                logger.InfoH(() => $"COMBO NOT EFFECTIVE; next");
                            }
                        }
                    }
                    // 見つからなかった
                    --overlapLen;
                }
                return overlapLen;
            }

            int resultLen = findFunc();

            bComboBlocked = comboBlocked;

            logger.InfoH(() => $"LEAVE: {(resultLen == 0 ? "NOT ": "")}FOUND: result={result._keyString()}, overlapLen={resultLen}: {ToDebugString()}");
            return resultLen;
        }

        /// <summary>順次打鍵のほうを優先させる打鍵列か</summary>
        private bool isListContaindInSequentialPriorityWordKeySet(List<Stroke> list)
        {
            int listLen = list._safeCount();
            if (listLen < 2) return true;

            var set = Settings.SequentialPriorityWordKeyStringSet;
            var headSet = Settings.ThreeKeysComboPriorityHeadKeyStringSet;
            var tailSet = Settings.ThreeKeysComboPriorityTailKeyStringSet;
            var head1 = list.Take(1)._toString();
            var head2 = list.Take(2)._toString();
            var tail1 = list.Skip(listLen - 1)._toString();
            var tail2 = list.Skip(listLen - 2)._toString();
            if (headSet._notEmpty()) {
                // 同時打鍵のほうを優先させる打鍵列か
                //if (!headSet.Contains(head1) && !headSet.Contains(head2)) return true;
                if (headSet.Contains(head1) || headSet.Contains(head2)) return false;
            }
            if (tailSet._notEmpty()) {
                // 同時打鍵のほうを優先させる打鍵列か
                //if (!tailSet.Contains(tail1) && !tailSet.Contains(tail2)) return true;
                if (tailSet.Contains(tail1) || tailSet.Contains(tail2)) return false;
            }
            return set.Contains(list._toString()) || set.Contains($"{head1}:*") || set.Contains($"{head2}:*") || set.Contains($"*:{tail1}") || set.Contains($"*:{tail2}");
        }

        /// <summary>
        /// 削除対象でない連続シフトキーを comboList に移動する<br/>
        /// 非文字キー(Spaceなど)を優先する(SP+J→や、SP+X→ほ、J+X→ぽ、のとき、J+SP+Xで「やぽ」にならないようにする
        /// ただし、comboList に入るキー(連続シフトにかかわるキー)は2キーまでとする
        /// </summary>
        private void copyToComboList(List<Stroke> list, int len)
        {
            logger.InfoH(() => $"ENTER: list={list._toString()}, len={len}; comboList={comboList._toString()}");
            int movedLen = comboList.Count;
            if (movedLen < 2) {
                foreach (var s in list.Take(len)) {
                    logger.InfoH(() => $"key={s.OrigDecoderKey}:{!s.ToBeRemoved && s.IsSuccessiveShift} :!s.ToBeRemoved={!s.ToBeRemoved} && s.IsSuccessiveShift={s.IsSuccessiveShift}");
                    if (!s.ToBeRemoved && s.IsSuccessiveShift) {
                        s.SetCombined();
                        if (s.IsSpaceOrFuncComboShift) {
                            comboList.Insert(0, s);
                        } else {
                            comboList.Add(s);
                        }
                        ++movedLen;
                        if (movedLen >= 2) break;
                    }
                }
            }
            logger.InfoH(() => $"LEAVE: comboList={comboList._toString()}");
        }

        /// <summary>同時打鍵のチャレンジ列を作成する</summary>
        /// <returns></returns>
        private List<Stroke> makeComboChallengeList(List<Stroke> strokes, IEnumerable<Stroke> addList)
        {
            var list = new List<Stroke>(strokes);
            list.AddRange(addList);
            return list;
        }

        // タイミングによる同時打鍵判定関数(ここでは isTailKeyUp == False として扱う)
        // result: 0: 判定OK, 1:1文字目チェックNG, 2:2文字目チェックNG
        private int isCombinationTiming(KeyCombination keyCombo, List<Stroke> list, Stroke tailStk, DateTime dtNow, bool bSecondComboCheck)
        {
            logger.InfoH(() => $"unordered={keyCombo.IsUnordered}, list={list._toString()}, tailStk={tailStk.DebugString()}, bSecondComboCheck={bSecondComboCheck}");
            if (list._isEmpty()) return -1;

            var strk1st = list[0];
            var strk2nd = list[1];
            //bool isSpaceOrFunc = strk1st.IsSpaceOrFunc || strk2nd.IsSpaceOrFunc || tailStk.IsSpaceOrFunc;
            int result = 0;
            if (!bSecondComboCheck) {
                // 1文字目ならリードタイムをチェック
                if (!keyCombo.IsUnordered) {
                    // 相互シフトでなければ、1文字目の時間制約は気にしない
                    result = 0;
                    logger.InfoH(() => $"RESULT1={result == 0}: !bSecondComboCheck (True) && !IsUnorderd={!keyCombo.IsUnordered}");
                } else {
                    int maxLeadTime = Settings.CombinationKeyMaxAllowedLeadTimeMs;
                    //double ms1 = strk1st.TimeSpanMs(tailStk);
                    //double elapsedTimeFromPrevShiftKeyUp = GetElapsedTimeFromShiftKeyUp(strk1st, tailStk);
                    double ms1 = strk1st.TimeSpanMs(strk2nd);
                    double elapsedTimeFromPrevShiftKeyUp = GetElapsedTimeFromPrevShiftKeyUp(strk1st, strk2nd);
                    bool isComboDisableInterval() => Settings.ComboDisableIntervalTimeMs > 0 && elapsedTimeFromPrevShiftKeyUp <= Settings.ComboDisableIntervalTimeMs;
                    result =
                        list.Count >= 4 ||      // 4キー以上の同時打鍵ならリードタイムの時間制約は無視する(第1、第2打鍵にシフトキーがくるとは限らないため)
                        (strk1st.IsSpaceOrFuncComboShift  && list.Count > 2) ||
                        (strk2nd.IsMainComboShift && !isComboDisableInterval() && ms1 <= maxLeadTime && ms1 <= Settings.ComboKeyMaxAllowedPostfixTimeMs) ||
                        (!strk2nd.IsSpaceOrFuncComboShift && ms1 <= maxLeadTime && (strk1st.IsComboShift || strk2nd.IsComboShift && ms1 <= Settings.ComboKeyMaxAllowedPostfixTimeMs))
                        ? 0 : 1;
                    if (Logger.IsInfoEnabled) {
                        logger.InfoH(() => $"ComboDisableIntervalTimeMs={Settings.ComboDisableIntervalTimeMs}, ElapsedTimeFromShiftKeyUp={elapsedTimeFromPrevShiftKeyUp:f1}");
                        logger.InfoH(() => $"RESULT1={result == 0}: result={result}");
                        logger.InfoH(() => $"{list.Count >= 4}: list.Count({list.Count})>=4");
                        logger.InfoH(() => $"{strk1st.IsSpaceOrFuncComboShift  && list.Count > 2}: strk1st.IsSpaceOrFuncComboShift({strk1st.IsSpaceOrFuncComboShift}) && list.Count({list.Count})>=2({list.Count >= 2})");
                        logger.InfoH(() => $"{strk2nd.IsSpaceOrFuncComboShift && !isComboDisableInterval() && ms1 <= maxLeadTime && ms1 <= Settings.ComboKeyMaxAllowedPostfixTimeMs}: " +
                            $"strk2nd.IsSpaceOrFuncComboShift({strk2nd.IsSpaceOrFuncComboShift}) && " +
                            $"!isComboDisableInterval()({!isComboDisableInterval() }) && " +
                            $"ms1({ms1}) <= maxLeadTime({maxLeadTime}) ({ms1 <= maxLeadTime}) && " +
                            $"ms1({ms1}) <= Settings.ComboKeyMaxAllowedPostfixTimeMs({Settings.ComboKeyMaxAllowedPostfixTimeMs}) ({ms1 <= Settings.ComboKeyMaxAllowedPostfixTimeMs})");
                        logger.InfoH(() => $"{!strk2nd.IsSpaceOrFuncComboShift && ms1 <= maxLeadTime && (strk1st.IsComboShift || strk2nd.IsComboShift && ms1 <= Settings.ComboKeyMaxAllowedPostfixTimeMs)}: " +
                            $"!strk2nd.IsSpaceOrFuncComboShift({!strk2nd.IsSpaceOrFuncComboShift}) && " +
                            $"ms1({ms1}) <= maxLeadTime({maxLeadTime}) ({ms1 <= maxLeadTime}) && " +
                            $"(strk1st.IsComboShift({strk1st.IsComboShift}) || " +
                            $"strk2nd.IsComboShift({strk2nd.IsComboShift}) && " + $"ms1({ms1}) <= Settings.ComboKeyMaxAllowedPostfixTimeMs({Settings.ComboKeyMaxAllowedPostfixTimeMs})");
                    }
                }
            }
            if (result == 0) {
                logger.InfoH(() => $"bSecondComboCheck={bSecondComboCheck}, list={list._toString()}, CombinationKeyMinTimeOnlyAfterSecond={Settings.CombinationKeyMinTimeOnlyAfterSecond}");
            }
            if (bSecondComboCheck || (result == 0 && (list._safeCount() > 2 || !Settings.CombinationKeyMinTimeOnlyAfterSecond))) {
                // 2文字目であるか、または、1文字目のリードタイムチェックをパスし、かつ、3キー同時または1文字目でも重複時間チェックが必要
                // ここでは tailKey より前のキーがUPされたものとして扱う。ただし、list中にUPされたキーがあるとは限らない)
                // シフトキーが解放されている(または単打可能キーのみである)ので、最後のキー押下時刻との差分を求め、タイミング判定する

                // 文字キー同士の同時打鍵の場合は、それ用の閾値が用意されていればそれを使う。その場合、第1打鍵と第2打鍵の押下間隔分を閾値に上乗せずる。
                // (つまり、第1打鍵と第2打鍵の間をすばやく押下したほうが同時打鍵と判定されやすくなるということ)
                int minTimeCharKeys = (Settings.CharKeyComboMinOverlappingTime > 0 && !strk1st.IsSpaceOrFunc && !strk2nd.IsSpaceOrFunc)
                    ? Settings.CharKeyComboMinOverlappingTime + (int)strk1st.TimeSpanMs(strk2nd) : 0;
                logger.InfoH(() => $"minTimeCharKeys={minTimeCharKeys}: CharKeyComboMinOverlappingTime={Settings.CharKeyComboMinOverlappingTime}, " +
                                    $"!strk1st.IsSpaceOrFunc={!strk1st.IsSpaceOrFunc}, !strk2nd.IsSpaceOrFunc={!strk2nd.IsSpaceOrFunc}");
                double ms2 = tailStk.TimeSpanMs(dtNow);
                logger.InfoH(() => $"CombinationKeyMinOverlappingTimeMs={Settings.CombinationKeyMinOverlappingTimeMs}, " +
                                    $"CombinationKeyMinOverlappingTimeMs3={Settings.CombinationKeyMinOverlappingTimeMs3}, " +
                                    $"CombinationKeyMinOverlappingTimeForSecond={Settings.CombinationKeyMinOverlappingTimeForSecond}");
                int minTime =
                    list._safeCount() >= 3 && list._startsWithAny(Settings.HeadComboKeysListForZeroOverlappingTime) ? 0 :
                    Settings.CombinationKeyMinOverlappingTimeMs3 > Settings.CombinationKeyMinOverlappingTimeMs && list._safeCount() >= 3 ? Settings.CombinationKeyMinOverlappingTimeMs3 :
                    //Settings.CombinationKeyMinOverlappingTimeMs2 > 0 && !isSpaceOrFunc ? Settings.CombinationKeyMinOverlappingTimeMs2 :
                    minTimeCharKeys > 0 ? minTimeCharKeys :
                    bSecondComboCheck && Settings.CombinationKeyMinOverlappingTimeForSecond > Settings.CombinationKeyMinOverlappingTimeMs ? Settings.CombinationKeyMinOverlappingTimeForSecond :
                    Settings.CombinationKeyMinOverlappingTimeMs;

                result = ms2 >= minTime ? 0 : bSecondComboCheck ? 2 : 1;
                logger.InfoH(() => $"RESULT2={result == 0}: ms2={ms2:f2}ms >= minOverlappingTime={minTime}ms (Timing={result})");
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

        public static bool _startsWithAny(this IEnumerable<Stroke> list, IEnumerable<List<int>> keyList)
        {
            foreach (var keys in keyList) {
                if (keys.All(x => list.Take(keys.Count).Any(s => s.ModuloDecKey == x))) return true;
            }
            return false;
        }

        public static string _toString(this IEnumerable<Stroke> list)
        {
            return list?.Select(x => x.OrigDecoderKey.ToString())._join(":") ?? "";
        }
    }
}

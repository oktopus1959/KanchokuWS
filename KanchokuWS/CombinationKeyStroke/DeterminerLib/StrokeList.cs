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
                logger.DebugH(() => $"LEAVE: PrevShiftKey={PrevShiftKey}, UpDt={UpDt}, ComboPressed={ComboPressed}");
            }

            public void SetValues(int shiftDeckey, DateTime upDt)
            {
                PrevShiftKey = shiftDeckey;
                UpDt = upDt;
                ComboPressed = false;
                logger.DebugH(() => $"LEAVE: PrevShiftKey={PrevShiftKey}, UpDt={UpDt}, ComboPressed={ComboPressed}");
            }

            public void CheckComboKeyDown(int deckey)
            {
                logger.DebugH(() => $"ENTER: PrevShiftKey={PrevShiftKey}, deckey={deckey}, ComboPressed={ComboPressed}");
                if (PrevShiftKey != deckey) {
                    if (ComboPressed) {
                        logger.DebugH(() => $"ComboPressed: DO Initialize");
                        Initialize();
                    } else {
                        logger.DebugH(() => $"transit to ComboPressed");
                        ComboPressed = true;
                    }
                }
                logger.DebugH(() => $"LEAVE: PrevShiftKey={PrevShiftKey}, deckey={deckey}, ComboPressed={ComboPressed}");
            }

            public DateTime GetPrevComboShiftKeyUpDt(int shiftDeckey)
            {
                return PrevShiftKey == shiftDeckey ? UpDt : DateTime.MinValue;
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
            logger.DebugH(() => $"ENTER: list={list._toString()}");
            bool result = KeyCombinationPool.CurrentPool.GetEntry(list)?.IsComboBlocked == true;
            logger.DebugH(() => $"LEAVE: comboBlocker={result}");
            return result;
        }

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

        public KeyCombination GetKeyCombo()
        {
            return KeyCombinationPool.CurrentPool.GetEntry(unprocList);
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
                return unprocList.Any(x => !x.HasStringOrSingleHittable);
            }

            KeyCombination keyCombo = null;

            // 連続シフトの場合は、同時打鍵キーの数は最大2とする
            List<int> getAndCheckCombo(List<Stroke> list)
            {
                logger.DebugH(() => $"getAndCheckCombo: list={list._toString()}");
                keyCombo = KeyCombinationPool.CurrentPool.GetEntry(list);
                logger.DebugH(() =>
                    $"combo={(keyCombo == null ? "(none)" : "FOUND")}, decKeyList={(keyCombo == null ? "(none)" : keyCombo.DecKeysDebugString())}, " +
                    $"Terminal={keyCombo?.IsTerminal ?? false}, isComboBlocked={keyCombo?.IsComboBlocked ?? false}, " +
                    $"OnlyCharKeysComboShouldBeCoveringCombo={Settings.OnlyCharKeysComboShouldBeCoveringCombo}, ContainsTwoCharacterKeys={keyCombo?.ContainsTwoCharacterKeys ?? false}" +
                    $"comboKeyList={(keyCombo == null ? "(none)" : keyCombo.ComboKeysDebugString())}");
                if (keyCombo != null && keyCombo.DecKeyList != null && (keyCombo.IsTerminal || keyCombo.IsComboBlocked) &&
                    (!Settings.OnlyCharKeysComboShouldBeCoveringCombo || !keyCombo.ContainsTwoCharacterKeys)) {
                    logger.DebugH("COMBO CHECK PASSED");
                    return new List<int>(keyCombo.DecKeyList);
                }
                logger.DebugH("KeyDown COMBO CHECK FAILED");
                return null;
            }

            logger.DebugH(() => $"ENTER: IsTemporaryComboDisabled={IsTemporaryComboDisabled}");

            List<int> result = null;
            //if (comboList._isEmpty() && unprocList.Count == 2)
            if (Count == 2 && unprocList.Count >= 1) {
                // 2キーの同時打鍵のケースの場合のみを扱う
                logger.DebugH("Try 2 keys combo");
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
                    if (Logger.IsInfoHEnabled) {
                        logger.DebugH("KeyDown COMBO FOUND:");
                        logger.DebugH(() => $"stroke-1: {(isStrk1Unproc ? "unprocList[0]" : "comboList[0]")}, {(strk1.IsComboShift ? "IS" : "NOT")} ComboShift");
                        logger.DebugH(() => $"stroke-2: {(isStrk1Unproc ? "unprocList[1]" : "unprocList[0]")}, {(strk2.IsComboShift ? "IS" : "NOT")} ComboShift");
                        logger.DebugH(() => $"!isStrk1Unproc({!isStrk1Unproc}) || shiftTimeSpan={(int)shiftTimeSpan:f1}ms <= maxAllowedTime={Settings.CombinationKeyMaxAllowedLeadTimeMs}");
                        logger.DebugH(() => $"shiftUpElapse={shiftUpElapse:f1}ms >= ComboDisableIntervalTimeMs={Settings.ComboDisableIntervalTimeMs}");
                        logger.DebugH(() => $"shiftTimeSpan={(int)shiftTimeSpan:f1}ms <= ComboKeyMaxAllowedPostfixTimeMs={Settings.ComboKeyMaxAllowedPostfixTimeMs}");
                        logger.DebugH(() => $"IsComboBlocked => {keyCombo.IsComboBlocked} || \n" +
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
                        logger.DebugH(() => $"KeyDown COMBO result={result._keyString()}, IsTemporaryComboDisabled={IsTemporaryComboDisabled}");
                    } else {
                        // どちらも単打を含むため、未確定の場合は、タイマーを有効にする
                        result = null;
                        bTimer = true;
                        logger.DebugH("KeyDown COMBO Undetermined. Return NULL result");
                    }
                //} else if (keyCombo == null && unprocList[0].HasDecKeyList) {
                } else if (keyCombo == null && unprocList[0].HasString) {
                    logger.DebugH("combo NOT found. Return first key as is");
                    // 同時打鍵候補がないので、最初のキーをそのまま返す
                    result = Helper.MakeList(unprocList[0].OrigDecoderKey);
                    unprocList.RemoveAt(0); // 最初のキーを削除
                    if (unprocList._notEmpty()) {
                        // 2番目のキーのチェック
                        if (unprocList[0].IsJustSingleHit) {
                            logger.DebugH("second key is just SingleHit. Return second key as is");
                            result.Add(unprocList[0].OrigDecoderKey);
                            unprocList.RemoveAt(0);
                        } else if (unprocList[0].HasStringOrSingleHittable && unprocList[0].HasString /*unprocList[0].HasDecKeyList*/) {
                            logger.DebugH("second key is SingleHittable and HasString. Enable timer");
                            bTimer = true;
                        }
                    }
                }
            } else if (Settings.CombinationKeyMinOverlappingTimeMs <= 0) {
                // 2文字目以降または3キー以上の同時押し状態で、即時判定の場合
                logger.DebugH("Imediate Combo check");
                if (comboList.Count >= 1 && unprocList.Count == 1) {
                    // 2文字目以降のケース
                    logger.DebugH(() => $"Try second or later successive combo: bTemporaryComboDisabled={IsTemporaryComboDisabled}");
                    if (IsTemporaryComboDisabled) {
                        // 連続シフト版「月光」などで、先行してShiftキーが送出され、一時的に同時打鍵がOFFになっている場合
                        result = getAndCheckCombo(Helper.MakeList(unprocList[0]));
                        IsTemporaryComboDisabled = false;
                    } else {
                        result = getAndCheckCombo(Helper.MakeList(comboList, unprocList[0]));
                    }
                    logger.DebugH(() => $"result={(result != null ? "FOUND" : "(empty)")}, keyCombo={keyCombo.DecKeysDebugString()}");
                    if (result != null || keyCombo == null) {
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

            logger.DebugH(() => $"LEAVE: IsTemporaryComboDisabled={IsTemporaryComboDisabled}");
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
                    && (k1stSingle == 0 || (k1stSingle == 1) == s.HasStringOrSingleHittable)
                    && (k1stShift == 0 || (k1stShift == 1) == s.IsComboShift || (k1stShift == 2) == s.IsSequentialShift)
                    && (k2ndSingle == 0 || (k2ndSingle == 1) == (t != null && t.HasStringOrSingleHittable))
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

        /// <summary>ComboBlockerなどによって一時的に同時打鍵を無効化して順次打鍵になっているか</summary>
        public bool IsTemporaryComboDisabled { get; set; }  = false;

        private const int COVERING_COMBO_FAILED = -2;

        // 解放の場合
        public List<int> GetKeyCombinationWhenKeyUp(int decKey, DateTime dtNow, bool bDecoderOn, out bool bUnconditional)
        {
            logger.DebugH(() => $"ENTER: decKey={decKey}, IsTemporaryComboDisabled={IsTemporaryComboDisabled}, dt={dtNow.ToString("HH:mm:ss.fff")}");

            List<int> result = null;
            bUnconditional = false;

            try {
                //bool bTempUnconditional = false;
                bool bTempComboDisabled = IsTemporaryComboDisabled;

                int upComboIdx = findAndMarkUpKey(comboList, decKey);

                if (unprocList._notEmpty()) {
                    int upKeyIdx = findAndMarkUpKey(unprocList, decKey);
                    int upKeyIdxFromTail = upKeyIdx >= 0 ? unprocList._safeCount() - upKeyIdx - 1 : -1;
                    logger.DebugH(() => $"upComboIdx={upComboIdx}, upKeyIdx={upKeyIdx}, upKeyIdxFromTail={upKeyIdxFromTail}");

                    if (upComboIdx >= 0 || upKeyIdx >= 0) {
                        result = new List<int>();

                        bool bSecondComboCheck = comboList._notEmpty();
                        logger.DebugH(() => $"START while: {ToDebugString()}, bSecondComboCheck={bSecondComboCheck}");

                        HashSet<string> challengedSet = new HashSet<string>();

                        while (unprocList._notEmpty() /*&& (upKeyIdxFromTail < 0 || unprocList.Count > upKeyIdxFromTail)*/) {
                            // 持ち越したキーリストの部分リストからなる集合(リスト)
                            logger.DebugH(() => $"bTempComboDisabled={bTempComboDisabled}");
                            //List<List<Stroke>> subComboLists = gatherSubList(bTempComboDisabled ? null : comboList);

                            bool bForceOutput = false;
                            bool bPrefixShiftMaybe = false;
                            int outputLen = 0;
                            int discardLen = 1;
                            int copyShiftLen = 1;

                            if (comboList._isEmpty() && unprocList.Count == 1) {
                                logger.DebugH($"NO COMBO SHIFT and JUST 1 UNPROC KEY");
                                var s = unprocList[0];
                                logger.DebugH(() => $"unprocList.First={s.DebugString()}");
                                if (s.IsUpKey || !s.IsComboShift) {
                                    logger.DebugH($"JUST 1 UNPROC KEY is UP KEY");
                                    if (s.IsSingleHittable || s.IsSequentialShift) {
                                        // 単打可能または順次シフトだった
                                        logger.DebugH(() => $"IsSingleHittable={s.IsSingleHittable} or SequentialShift={s.IsSequentialShift}");
                                        outputLen = 1;
                                    } else {
                                        logger.DebugH(() => $"ABANDONED-1: IsSingleHittable={s.IsSingleHittable} and SequentialShift={s.IsSequentialShift}");
                                    }
                                } else {
                                    // UPされていないシフトキーがある。多分、最初のループで処理されずに残ったものがRETRYで対象となった。
                                    // 次のUPのときに処理するのでこのまま残す。以前はこれをここで出力していたので、余分な出力となっていた。
                                    // 薙刀式での K→J→W で J の処理はそれ以降に任せるということ。
                                    logger.DebugH($"JUST 1 UNPROC KEY is NOT UP KEY. Maybe RETRY and it's SHIFT KEY. BREAK.");
                                    break;
                                }
                            //} else if (bTempUnconditional) {
                            //    logger.DebugH(() => $"TempUnconditional={bTempUnconditional}");
                            //    var keyCombo = findComboAny(subComboLists, unprocList);
                            //    if (keyCombo != null) {
                            //        logger.DebugH(() => $"COMBO FOUND (TempUnconditional={bTempUnconditional})");
                            //        outputLen = 1;
                            //        if (keyCombo.IsTerminal) {
                            //            // ここで無条件Comboは終わり
                            //            bTempUnconditional = false;
                            //            logger.DebugH(() => $"COMBO IS TERMINAL. TempUnconditional={bTempUnconditional}");
                            //        }
                            //    } else {
                            //        logger.DebugH($"NO SEQUENTIAL COMBO. ABANDONED-2");
                            //    }
                            } else if (bTempComboDisabled) {
                                // 同時打鍵が一時的に無効化されているので、順次打鍵として扱う
                                logger.DebugH(() => $"bTempComboDisabled={bTempComboDisabled}");
                                //result.Add(unprocList[0].OrigDecoderKey);
                                //unprocList[0].SetToBeRemoved();
                                bForceOutput = true;
                                outputLen = 1;
                                copyShiftLen = 0;
                                discardLen = 1;
                                logger.DebugH(() => $"ADD: result={result._keyString()}");
                            } else {
                                //同時打鍵を見つける
                                List<List<Stroke>> subComboLists = gatherSubList(comboList);
                                int timingFailure = -1;
                                int overlapLen = findCombo(result, challengedSet, subComboLists, unprocList, upKeyIdxFromTail, dtNow, bSecondComboCheck, bDecoderOn, out timingFailure, out bTempComboDisabled);
                                if (overlapLen > 0) {
                                    // 見つかった
                                    logger.DebugH(() => $"COMBO FOUND: bTempComboDisabled={bTempComboDisabled}");
                                    bSecondComboCheck = true;
                                    outputLen = copyShiftLen = 0;  // 既に findCombo() の中でやっている
                                    discardLen = overlapLen;
                                } else if (timingFailure == COVERING_COMBO_FAILED) {
                                    logger.DebugH($"COMBO FOUND but COVERING_COMBO_FAILED");
                                    if (upKeyIdxFromTail >= 0 && unprocList.Count > upKeyIdxFromTail) {
                                        outputLen = discardLen = unprocList.Count - upKeyIdxFromTail;
                                        copyShiftLen = 0;
                                        logger.DebugH(() => $"COVERING_COMBO_FAILED: outputLen={outputLen}");
                                    } else {
                                        logger.DebugH(() => $"COVERING_COMBO_FAILED: break; upKeyIdxFromTail={upKeyIdxFromTail}, unprocList.Count={unprocList.Count}");
                                        break;
                                    }
                                } else {
                                    // 見つからなかった
                                    bool bComboFound = timingFailure >= 0;
                                    logger.DebugH(() => bComboFound ? $"COMBO FOUND but TIMING CHECK FAILED: {timingFailure}" : "COMBO NOT FOUND");
                                    bool bSomeKeyUp = unprocList.Any(x => x.IsUpKey);
                                    var s = unprocList[0];
                                    logger.DebugH(() => $"comboList.Count={comboList.Count}, unprocList.Count={unprocList.Count}, ComboFound={bComboFound}, s={s.DebugString()}");
                                    var t = unprocList._getNth(1);
                                    logger.DebugH(() => $"RULE TRY: bComboFound={bComboFound}, someKeyUp={bSomeKeyUp}, ComboListEmpty={comboList._isEmpty()}, " +
                                        $"2nd={t?.DebugString() ?? "none"}, 2ndShift={t?.IsComboShift}, isShiftUP={comboList._notEmpty() && comboList.Any(x => x.IsUpKey)}");
                                    int n = 1;
                                    foreach (var rule in keyComboRules) {
                                        if (rule.Apply(timingFailure, bSomeKeyUp, comboList, s, t)) {
                                            // どれかのルールにヒットした
                                            outputLen = rule.outputLen;
                                            discardLen = rule.discardLen;
                                            if (outputLen > 0) bSecondComboCheck = true;
                                            //if (rule.bMoveShift) copyShiftLen = discardLen;
                                            copyShiftLen = discardLen;
                                            //if (rule.k1stShift == 2) {
                                            //    // 前置連続シフト(or順次打鍵)の場合
                                            //    bTempUnconditional = outputLen == 0;        // 
                                            //    bTempComboDisabled = !bTempUnconditional;
                                            //} else {
                                            //    bTempUnconditional = false;
                                            //    bTempComboDisabled = false;
                                            //}
                                            bPrefixShiftMaybe = (rule.k1stShift == 2);      // 連続シフト打鍵の可能性あり
                                                                                            // この場合、連続シフト版月光で DKkIid と打鍵したとき、DK でこのルールが適用され、
                                                                                            // D を残すことで次の I にシフトがかかり、「よ」が出るようになる
                                            logger.DebugH(() => $"RULE({n}) APPLIED: outputLen={outputLen}, discardLen={discardLen}, copyShiftLen={copyShiftLen}");
                                            break;
                                        }
                                        ++n;
                                    }
                                    if (n > keyComboRules.Count) {
                                        logger.DebugH("NO RULE APPLIED");
                                        //bTempUnconditional = false;
                                    }
                                }
                            }
                            logger.DebugH(() => $"outputLen={outputLen}, copyShiftLen={copyShiftLen}, discardLen={discardLen}");
                            for (int i = 0; i < outputLen && i < unprocList.Count; ++i) {
                                // Upされていない連続シフトキーは出力しない ⇒ と思ったが、これをやると薙刀式で JE と打ったのが同時打鍵と判定されなかったときに E(て)が出力されなくなるので、やめる
                                //if (unprocList[i].IsUpKey || !unprocList[i].IsSuccessiveShift)
                                var s = unprocList[i];
                                // 強制出力か文字を持つか単打可能か順次シフトキーの場合だけ、出力する
                                if (bForceOutput || s.HasDecKeyList || s.HasStringOrSingleHittable || s.IsSequentialShift) {
                                    result.Add(s.OrigDecoderKey);
                                    // 連続シフト打鍵の可能性がない場合、出力されたキーは comboList には移さない
                                    if (!bPrefixShiftMaybe) s.SetToBeRemoved();
                                    logger.DebugH(() => $"ADD: result={result._keyString()}");
                                }
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
                //bTemporaryUnconditional = comboList._notEmpty() && (bTempUnconditional || KeyCombinationPool.CurrentPool.IsPrefixedOrSequentialShift && bSomeShiftKeyUp);
                IsTemporaryComboDisabled = comboList._notEmpty() && bTempComboDisabled;
                logger.DebugH(() => $"CLEANUP: UpKey or Oneshot in comboList Removed: bTemporaryComboDisabled={IsTemporaryComboDisabled}, {ToDebugString()}");

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
            logger.DebugH(() => $"LEAVE: result={result?._keyString() ?? "null"}, IsTemporaryComboDisabled={IsTemporaryComboDisabled}, {ToDebugString()}");
            return result;
        }

        /// <summary>同時打鍵を見つける<br/>見つかったら、処理された打鍵数を返す。見つからなかったら0を返す</summary>
        private int findCombo(List<int> result, HashSet<string> challengedSet, List<List<Stroke>> subComboLists, List<Stroke> unprocList,
            int upKeyIdxFromTail, DateTime dtNow, bool bSecondComboCheck, bool bDecoderOn, out int timingFailure, out bool bComboBlocked)
        {
            logger.DebugH(() => $"ENTER: unprocList={unprocList._toString()}, upKeyIdxFromTail={upKeyIdxFromTail}, bSecondComboCheck={bSecondComboCheck}");

            int timingResult = -1;
            bool comboBlocked = false;

            int findFunc()
            {
                int overlapLen = unprocList.Count;
                int upKeyIdx = upKeyIdxFromTail >= 0 ? unprocList.Count - upKeyIdxFromTail - 1 : -1;
                while (overlapLen >= 1) {
                    logger.DebugH(() => $"WHILE: overlapLen={overlapLen}");
                    foreach (var subList in subComboLists) {
                        int minLen = subList._isEmpty() ? 2 : 1;    // subList(comboListの部分列)が空なら、hotListのほうから2つ以上必要
                        logger.DebugH(() => $"FOREACH: subList={subList._toString()}, minLen={minLen}");
                        if (overlapLen < minLen) break;

                        var challengeList = makeComboChallengeList(subList, unprocList.Take(overlapLen));
                        var challengeStr = challengeList._toString();
                        logger.DebugH(() => $"COMBO SEARCH: challengeList={challengeStr}");
                        if (challengedSet.Contains(challengeList._toString())) {
                            logger.DebugH(() => $"challengeList={challengeStr} ALREADY TRIED, SKIP");
                            continue;
                        }
                        challengedSet.Add(challengeStr);

                        var keyCombo = KeyCombinationPool.CurrentPool.GetEntry(challengeList);
                        logger.DebugH(() => $"COMBO RESULT: keyCombo.decKeyList={(keyCombo == null ? "(none)" : keyCombo.DecKeysDebugString())}, " +
                            $"HasDecoderOutput={keyCombo?.HasDecoderOutput ?? false}, comboKeyList={(keyCombo == null ? "(none)" : keyCombo.ComboKeysDebugString())}");

                        if (keyCombo != null && keyCombo.DecKeyList != null && (keyCombo.HasDecoderOutput || keyCombo.IsComboBlocked)) {
                            //bComboFound = true; // 同時打鍵の組合せが見つかった
                            //bool isTailKeyUp = unprocList.Skip(overlapLen - 1).Any(x => x.IsUpKey);    // 末尾キー以降のキーがUPされた
                            bool isTailKeyUp = upKeyIdx >= 0 && upKeyIdx >= overlapLen - 1;    // 末尾キー以降のキーがUPされた
                            bool bCoveringComboCheckPassed = !Settings.OnlyCharKeysComboShouldBeCoveringCombo || !keyCombo.ContainsTwoCharacterKeys || isTailKeyUp;
                            if (Logger.IsInfoHEnabled) {
                                logger.DebugH(() => $"COVERING_COMBO_CHECK_PASSED: {bCoveringComboCheckPassed}: " +
                                    $"!OnlyCharKeysComboShouldBeCoveringCombo={!Settings.OnlyCharKeysComboShouldBeCoveringCombo} || " +
                                    $"!ContainsTwoCharacterKeys={!keyCombo.ContainsTwoCharacterKeys} || isTailKeyUp={isTailKeyUp}");
                            }
                            if (bCoveringComboCheckPassed) {
                                timingResult = 0;  // 同時打鍵の組合せが見つかった
                                comboBlocked = keyCombo.IsComboBlocked;     // 同時打鍵の一時無効化か
                                Stroke tailKey = unprocList[overlapLen - 1];
                                if (Logger.IsInfoHEnabled) {
                                    logger.DebugH(() =>
                                        $"CHECK1: {isTailKeyUp && (comboBlocked || challengeList[0].IsShiftableSpaceKey || (tailKey.HasStringOrSingleHittable && !tailKey.IsShiftableSpaceKey))}: " +
                                        $"tailPos={overlapLen - 1}: " +
                                        $"isTailKeyUp({isTailKeyUp}) && " +
                                        $"(comboBlocked({comboBlocked}) || challengeList[0].IsShiftableSpaceKey={challengeList[0].IsShiftableSpaceKey}" +
                                        $"(tailKey.HasStringOrSingleHittable({tailKey.HasStringOrSingleHittable}) && !tailKey.IsShiftableSpaceKey({!tailKey.IsShiftableSpaceKey})))");
                                    logger.DebugH(() => $"CHECK2: {challengeList.Count < 3 && unprocList[0].IsShiftableSpaceKey}: " +
                                        $"challengeList.Count({challengeList.Count}) < 3 ({challengeList.Count < 3}) && unprocList[0].IsShiftableSpaceKey({unprocList[0].IsShiftableSpaceKey})");
                                    logger.DebugH(() => "CHECK3: " +
                                        $"{Settings.ThreeKeysComboUnconditional && keyCombo.DecKeyList._safeCount() >= 3 && !isListContaindInSequentialPriorityWordKeySet(challengeList)}: " +
                                        $"ThreeKeysComboUnconditional({Settings.ThreeKeysComboUnconditional}) && " +
                                        $"keyCombo.DecKeyList.Count({keyCombo.DecKeyList._safeCount()}) >= 3 ({keyCombo.DecKeyList._safeCount() >= 3}) && " +
                                        $"!isListContaindInSequentialPriorityWordKeySet({challengeList._toString()})({!isListContaindInSequentialPriorityWordKeySet(challengeList)})" +
                                        $": challengeList={challengeList._toString()}");
                                }
                                if ((isTailKeyUp && (comboBlocked || challengeList[0].IsShiftableSpaceKey || (tailKey.HasStringOrSingleHittable && !tailKey.IsShiftableSpaceKey))) ||
                                        // CHECK1: 対象リストの末尾キーが先にUPされており、同時打鍵の一時無効化か、先頭キーがシフト可能スペースキーか、末尾キーが単打可能キーだった
                                    challengeList.Count < 3 && unprocList[0].IsShiftableSpaceKey ||
                                        // CHECK2: チャレンジリストの長さが2以下で、先頭キーがシフト可能なスペースキーだった
                                        // ⇒連続シフトでない、最初のスペースキーとの同時打鍵ならタイミングは考慮せず無条件
                                    (Settings.ThreeKeysComboUnconditional && keyCombo.DecKeyList._safeCount() >= 3 && !isListContaindInSequentialPriorityWordKeySet(challengeList)) ||
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
                                        copyToComboList(unprocList, overlapLen/*, false*/);
                                        // 連続シフトキー以外はコピーしないようにした (薙刀式で J,W,Pが3打同時でなく、J,Wだけの同時と判定されたときに、Jだけをコピーするため)
                                    }
                                    // 見つかった
                                    return overlapLen;
                                }
                            } else {
                                timingResult = COVERING_COMBO_FAILED;
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
            bComboBlocked = comboBlocked;

            logger.DebugH(() => $"LEAVE: {(resultLen == 0 ? "NOT ": "")}FOUND: result={result._keyString()}, timingFailure={timingResult}, overlapLen={resultLen}: {ToDebugString()}");
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

        private KeyCombination findComboAny(List<List<Stroke>> subComboLists, List<Stroke> unprocList)
        {
            logger.DebugH(() => $"ENTER: unprocList={unprocList._toString()}");

            int overlapLen = unprocList.Count;
            while (overlapLen >= 1) {
                logger.DebugH(() => $"WHILE: overlapLen={overlapLen}");
                foreach (var subList in subComboLists) {
                    int minLen = subList._isEmpty() ? 2 : 1;    // subList(comboListの部分列)が空なら、hotListのほうから2つ以上必要
                    logger.DebugH(() => $"FOREACH: subList={subList._toString()}, minLen={minLen}");
                    if (overlapLen < minLen) break;

                    var challengeList = makeComboChallengeList(subList, unprocList.Take(overlapLen));
                    logger.DebugH(() => $"COMBO SEARCH: challengeList={challengeList._toString()}");

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

        /// <summary>
        /// 削除対象でない連続シフトキーを comboList に移動する<br/>
        /// 2キー以上なら、UPしたシフトキーは移動しない
        /// (「のにいると」で SP E O I e o i sp のケースで、E を combo に移動してしまうと 「E O → しょ」が有効になってしまうため)
        /// 3キー以上なら、連続シフトキー以外も移動する(薙刀式の「ぎゃぎょ」(J W H h O o w j)のようなケース)
        /// ただし、comboList に入るキー(連続シフトにかかわるキー)は2キーまでとする
        /// </summary>
        private void copyToComboList(List<Stroke> list, int len)
        {
            logger.DebugH(() => $"CALLED: list={list._toString()}, len={len}");
            int movedLen = comboList.Count;
            if (movedLen < 2) {
                foreach (var s in list.Take(len)) {
                    if (!s.ToBeRemoved && ((s.IsSuccessiveShift && ((movedLen == 0 && len == 1) || !s.IsUpKey)) || (!s.IsUpKey && len >= 3 && movedLen < len - 1))) {
                        if (s.IsSuccessiveShift) s.SetCombined();
                        comboList.Add(s);
                        ++movedLen;
                        if (movedLen >= 2) return;
                    }
                }
                if (movedLen == 0 && len >= 2) {
                    // 1つも移動できなかったら、UPしたシフトキーを移動する
                    foreach (var s in list.Take(len)) {
                        if (!s.ToBeRemoved && s.IsSuccessiveShift) {
                            if (s.IsSuccessiveShift) s.SetCombined();
                            comboList.Add(s);
                            break;
                        }
                    }
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

        // タイミングによる同時打鍵判定関数(ここでは isTailKeyUp == False として扱う)
        // result: 0: 判定OK, 1:1文字目チェックNG, 2:2文字目チェックNG
        private int isCombinationTiming(List<Stroke> list, Stroke tailStk, DateTime dtNow, bool bSecondComboCheck)
        {
            logger.DebugH(() => $"list={list._toString()}, tailStk={tailStk.DebugString()}, bSecondComboCheck={bSecondComboCheck}");
            if (list._isEmpty()) return -1;

            var strk1st = list[0];
            var strk2nd = list[1];
            bool isSpaceOrFunc = strk1st.IsSpaceOrFunc || strk2nd.IsSpaceOrFunc || tailStk.IsSpaceOrFunc;
            int result = 0;
            if (!bSecondComboCheck) {
                // 1文字目ならリードタイムをチェック
                if (!KeyCombinationPool.CurrentPool.ContainsUnorderedShiftKey) {
                    // 相互シフトを含まないならば、1文字目の時間制約は気にしない
                    result = 0;
                    logger.DebugH(() => $"RESULT1={result == 0}: !bSecondComboCheck (True) && !ContainsUnorderedShiftKey={!KeyCombinationPool.CurrentPool.ContainsUnorderedShiftKey}");
                } else {
                    int maxLeadTime = Settings.CombinationKeyMaxAllowedLeadTimeMs;
                    //double ms1 = strk1st.TimeSpanMs(tailStk);
                    //double elapsedTimeFromPrevShiftKeyUp = GetElapsedTimeFromShiftKeyUp(strk1st, tailStk);
                    double ms1 = strk1st.TimeSpanMs(strk2nd);
                    double elapsedTimeFromPrevShiftKeyUp = GetElapsedTimeFromPrevShiftKeyUp(strk1st, strk2nd);
                    bool isComboDisableInterval() => Settings.ComboDisableIntervalTimeMs > 0 && elapsedTimeFromPrevShiftKeyUp <= Settings.ComboDisableIntervalTimeMs;
                    result =
                        list.Count >= 4 ||      // 4キー以上の同時打鍵ならリードタイムの時間制約は無視する(第1、第2打鍵にシフトキーがくるとは限らないため)
                        //(strk1st.IsComboShift && !tailStk.IsComboShift && ms1 <= maxTime) ||
                        //(tailStk.IsComboShift && !isComboDisableInterval() && ms1 <= Settings.ComboKeyMaxAllowedPostfixTimeMs)
                        (strk1st.IsComboShift && !strk2nd.IsComboShift && ms1 <= maxLeadTime) ||
                        (strk2nd.IsComboShift && !isComboDisableInterval() && ms1 <= Settings.ComboKeyMaxAllowedPostfixTimeMs)
                        ? 0 : 1;
                    if (Logger.IsInfoHEnabled) {
                        logger.DebugH(() => $"isSpaceOrFunc={isSpaceOrFunc}, CombinationKeyMaxAllowedLeadTimeMs={Settings.CombinationKeyMaxAllowedLeadTimeMs}");
                        logger.DebugH(() => $"ComboDisableIntervalTimeMs={Settings.ComboDisableIntervalTimeMs}, ElapsedTimeFromShiftKeyUp={elapsedTimeFromPrevShiftKeyUp:f1}");
                        //logger.DebugH(() => $"strk1st.IsComboShift={strk1st.IsComboShift} && !tailStk.IsComboShift={!tailStk.IsComboShift} && (ms1({ms1}) <= maxTime({maxTime}))={ms1 <= maxTime}");
                        logger.DebugH(() => $"strk1st.IsComboShift={strk1st.IsComboShift} && !strk2nd.IsComboShift={!strk2nd.IsComboShift} && (ms1({ms1}) <= maxTime({maxLeadTime}))={ms1 <= maxLeadTime}");
                        //logger.DebugH(() => $"tailStk.IsComboShift={tailStk.IsComboShift} && !isComboDisableInterval={!isComboDisableInterval()} && " +
                        logger.DebugH(() => $"strk2nd.IsComboShift={strk2nd.IsComboShift} && !isComboDisableInterval={!isComboDisableInterval()} && " +
                            $"(ms1({ms1}) <= MaxAllowedTimeToPostShiftKey({Settings.ComboKeyMaxAllowedPostfixTimeMs}))={ms1 <= Settings.ComboKeyMaxAllowedPostfixTimeMs}");
                        logger.DebugH(() => $"RESULT1={result == 0}: !bSecondComboCheck (True) && " +
                            $"!isComboDisableInterval={(strk1st.IsComboShift ? "D/C" : (!isComboDisableInterval()).ToString())} && ms1={ms1:f2}ms <= " +
                            $"maxAllowedTime(Lead/Post)={maxLeadTime}ms/{Settings.ComboKeyMaxAllowedPostfixTimeMs}ms (result={result})");
                    }
                }
            }
            if (result == 0) {
                logger.DebugH(() => $"bSecondComboCheck={bSecondComboCheck}, list={list._toString()}, CombinationKeyMinTimeOnlyAfterSecond={Settings.CombinationKeyMinTimeOnlyAfterSecond}");
            }
            if (bSecondComboCheck || (result == 0 && (list._safeCount() > 2 || !Settings.CombinationKeyMinTimeOnlyAfterSecond))) {
                // 2文字目であるか、または、1文字目のリードタイムチェックをパスし、かつ、3キー同時または1文字目でも重複時間チェックが必要
                // ここでは tailKey より前のキーがUPされたものとして扱う。ただし、list中にUPされたキーがあるとは限らない)
                if (list.All(x => !x.IsUpKey)) {
                    // list中のキーがすべて解放されずに残っていたら同時打鍵とは判定しない
                    result = bSecondComboCheck ? 2 : 1;
                    logger.DebugH(() => $"RESULT2=False: All keys in list are ALIVE (Timing={result})");
                } else {
                    // シフトキーが解放されている(または単打可能キーのみである)ので、最後のキー押下時刻との差分を求め、タイミング判定する
                    double ms2 = tailStk.TimeSpanMs(dtNow);
                    int minTime = 
                        //Settings.CombinationKeyMinOverlappingTimeMs3 > 0 && list._safeCount() >= 3 ? Settings.CombinationKeyMinOverlappingTimeMs3 :
                        //Settings.CombinationKeyMinOverlappingTimeMs2 > 0 && !isSpaceOrFunc ? Settings.CombinationKeyMinOverlappingTimeMs2 :
                        Settings.CombinationKeyMinOverlappingTimeMs;
                    result = ms2 >= minTime ? 0 : bSecondComboCheck ? 2 : 1;
                    logger.DebugH(() => $"RESULT2={result == 0}: ms2={ms2:f2}ms >= minOverlappingTime={minTime}ms (Timing={result})");
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

        public static string _toString(this IEnumerable<Stroke> list)
        {
            return list?.Select(x => x.OrigDecoderKey.ToString())._join(":") ?? "";
        }
    }
}

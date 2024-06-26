﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KanchokuWS.TableParser;
using KanchokuWS.CombinationKeyStroke.DeterminerLib;
using Utils;

namespace KanchokuWS.CombinationKeyStroke
{
    public class KeyHandlerResult
    {
        public List<int> list;
        public bool bUncoditional;
    }

    /// <summary>
    /// キー入力の時系列に対して、同時打鍵などの判定を行って、出力文字列を決定する
    /// </summary>
    class Determiner : IDisposable
    {
        private static Logger logger = Logger.GetLogger(true);

        // FrmKanchoku
        FrmKanchoku frmMain;

        // タイマー
        System.Timers.Timer timerA;
        System.Timers.Timer timerB;
        System.Timers.Timer timerC;

        int decKeyForTimerA = -1;
        int decKeyForTimerB = -1;
        int decKeyForTimerC = -1;

        bool isDecoderOnA = false;
        bool isDecoderOnB = false;
        bool isDecoderOnC = false;

        private void timerA_elapsed(Object sender, System.Timers.ElapsedEventArgs e)
        {
            logger.InfoH(() => $"TIMER-A ELAPSED: decKey={decKeyForTimerA}");
            timerA?.Stop();
            if (decKeyForTimerA >= 0) {
                int dk = decKeyForTimerA;
                decKeyForTimerA = -1;
                KeyUp(dk, isDecoderOnA, true);
            }
        }

        private void timerB_elapsed(Object sender, System.Timers.ElapsedEventArgs e)
        {
            logger.InfoH(() => $"TIMER-B ELAPSED: decKey={decKeyForTimerB}");
            timerB?.Stop();
            if (decKeyForTimerB >= 0) {
                int dk = decKeyForTimerB;
                decKeyForTimerB = -1;
                KeyUp(dk, isDecoderOnB, true);
            }
        }

        private void timerC_elapsed(Object sender, System.Timers.ElapsedEventArgs e)
        {
            logger.InfoH(() => $"TIMER-C ELAPSED: decKey={decKeyForTimerC}");
            timerC?.Stop();
            if (decKeyForTimerC >= 0) {
                int dk = decKeyForTimerC;
                decKeyForTimerC = -1;
                KeyUp(dk, isDecoderOnC, true);
            }
        }

        public Action<List<int>, bool> KeyProcHandler { get; set; }

        public void InitializeTimer(FrmKanchoku frm)
        {
            logger.InfoH(() => $"CALLED");
            frmMain = frm;
            timerA = new System.Timers.Timer();
            timerB = new System.Timers.Timer();
            timerC = new System.Timers.Timer();
            timerA.SynchronizingObject = frm;
            timerB.SynchronizingObject = frm;
            timerC.SynchronizingObject = frm;
            timerA.Elapsed += timerA_elapsed;
            timerB.Elapsed += timerB_elapsed;
            timerC.Elapsed += timerC_elapsed;
        }

        void startTimer(int ms, int decKey, bool bDecoderOn)
        {
            logger.InfoH(() => $"CALLED: ms={ms}, decKey={decKey}");
            if (ms <= 0) return;

            if (timerA != null && !timerA.Enabled) {
                decKeyForTimerA = decKey;
                isDecoderOnA = bDecoderOn;
                timerA.Interval = ms;
                timerA.Start();
                logger.InfoH(() => $"TIMER-A STARTED");
            } else if (timerB != null && !timerB.Enabled) {
                decKeyForTimerB = decKey;
                isDecoderOnB = bDecoderOn;
                timerB.Interval = ms;
                timerB.Start();
                logger.InfoH(() => $"TIMER-B STARTED");
            } else if (timerC != null && !timerC.Enabled) {
                decKeyForTimerC = decKey;
                isDecoderOnC = bDecoderOn;
                timerC.Interval = ms;
                timerC.Start();
                logger.InfoH(() => $"TIMER-C STARTED");
            }
        }

        // 同時打鍵保持リスト
        private StrokeList strokeList = new StrokeList();

        // 前置書き換えキーの打鍵時刻
        private DateTime preRewriteDt = DateTime.MinValue;

        private bool isPreRewriteTarget = false;

        // 前キー
        //private int prevDownDecKey = -1;

        private void cancelPreRewriteTime()
        {
            logger.InfoH($"CALLED");
            preRewriteDt = DateTime.MinValue;
            frmMain?.ExecCmdDecoder("cancelPreRewrite", null);
        }

        private void checkPreRewriteTime(int dk)
        {
            logger.InfoH(() => $"CALLED: preRewriteDt={preRewriteDt}, isPreRewriteTarget={isPreRewriteTarget}");
            if (preRewriteDt._isValid()) {
                int delayTime = isPreRewriteTarget ? Settings.PreRewriteAllowedDelayTimeMs : Settings.PreRewriteAllowedDelayTimeMs2;
                double elapsedTime = (HRDateTime.Now - preRewriteDt).TotalMilliseconds;
                logger.InfoH(() => $"delayTime={delayTime}, elapsedTime={elapsedTime:f3}");
                if (delayTime > 0 && elapsedTime > delayTime) {
                    cancelPreRewriteTime();
                } else {
                    logger.InfoH($"DO NOTHING");
                }
            }
        }

        public static bool IsTailPreRewriteChar(string str)
        {
            return str._notEmpty() && (Settings.PreRewriteTargetChars.Contains('*') || Settings.PreRewriteTargetChars.Contains(str.Last()));
        }

        public void SetPreRewriteTime(string outStr)
        {
            logger.InfoH(() => $"ENTER: outStr={outStr}");
            if (outStr._isEmpty() /*|| outStr.Last()._isAscii()*/) {        // _isAscii()の場合も書き換えをキャンセルしてしまうと、ローマ字配列に影響が出る
                if (preRewriteDt._isValid()) {
                    cancelPreRewriteTime();
                }
            } else {
                isPreRewriteTarget = IsTailPreRewriteChar(outStr);
                preRewriteDt = HRDateTime.Now;
            }
            logger.InfoH(() => $"LEAVE: isPreRewriteTarget={isPreRewriteTarget}, preRewriteDt={preRewriteDt}");
        }

        public void Dispose()
        {
            timerA?.Dispose();
            timerA = null;
            timerB?.Dispose();
            timerB = null;
            timerC?.Dispose();
            timerC = null;
        }

        /// <summary>
        /// 初期化とテーブルファイルの読み込み、同時打鍵組合せ辞書の作成
        /// </summary>
        /// <param name="tableFile">主テーブルファイル名</param>
        /// <param name="tableFile2">副テーブルファイル名</param>
        /// <param name="tableFile3">第3テーブルファイル名</param>
        public void Initialize(string tableFile, string tableFile2, string tableFile3, bool bTest = false)
        {
            logger.Info("ENTER");
            Settings.ClearSpecificDecoderSettings();
            KeyCombinationPool.Initialize();
            Clear();

            new TableFileParser().ParseTableFile(tableFile, "tmp/tableFile1.tbl", KeyCombinationPool.SingletonK1, KeyCombinationPool.SingletonA1, 1, bTest);

            if (tableFile2._notEmpty()) {
                new TableFileParser().ParseTableFile(tableFile2, "tmp/tableFile2.tbl", KeyCombinationPool.SingletonK2, KeyCombinationPool.SingletonA2, 2, bTest);
            }

            if (tableFile3._notEmpty()) {
                new TableFileParser().ParseTableFile(tableFile3, "tmp/tableFile3.tbl", KeyCombinationPool.SingletonK3, KeyCombinationPool.SingletonA3, 3, bTest);
            }
            logger.Info("LEAVE");
        }

        ///// <summary>
        ///// 選択されたテーブルファイルに合わせて、漢直用KeyComboPoolを入れ替える
        ///// </summary>
        //public static void SelectKanchokuKeyCombinationPool(int tableNum, bool bDecoderOn)
        //{
        //    KeyCombinationPool.ChangeCurrentPoolBySelectedTable(tableNum, bDecoderOn);
        //}

        //public static void UsePrimaryPool(bool bDecoderOn)
        //{
        //    KeyCombinationPool.UsePrimaryPool(bDecoderOn);
        //}

        //public static void UseSecondaryPool(bool bDecoderOn)
        //{
        //    KeyCombinationPool.UseSecondaryPool(bDecoderOn);
        //}

        //public static bool IsEnabled => KeyCombinationPool.CurrentPool.Enabled;

        /// <summary>
        /// 同時打鍵リストをクリアする
        /// </summary>
        public void Clear()
        {
            logger.InfoH(() => $"CALLED");
            strokeList.Clear();
        }

        /// <summary>
        /// 未処理の同時打鍵リストをクリアする
        /// </summary>
        public void ClearUnprocList()
        {
            logger.InfoH(() => $"CALLED");
            strokeList.ClearUnprocList();
        }

        private Queue<Func<KeyHandlerResult>> procQueue = new Queue<Func<KeyHandlerResult>>();

        private bool bHandling = false;

        public void HandleQueue()
        {
            if (bHandling) return;

            bHandling = true;
            try {
                while (procQueue.Count > 0) {
                    var result = procQueue.Dequeue().Invoke();
                    KeyProcHandler?.Invoke(result.list, result.bUncoditional);
                }
            } catch (Exception ex) {
                logger.Error(ex._getErrorMsg());
            } finally {
                bHandling = false;
            }
        }

        // これまで押下されたキーの総数
        int totalKeyDownCount = 0;

        // 直前のキー(オートリピートの判定に用いる)
        int lastRepeatedDecKey = -1;

        bool bAutoRepeated = false;

        /// <summary>
        /// キーの押下<br/>押下されたキーをキューに積み、可能であれば同時打鍵判定も行う
        /// </summary>
        /// <param name="decKey">押下されたキーのデコーダコード</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダコード列を返す。<br/>確定しなければ null を返す</returns>
        public void KeyDown(int decKey, bool bDecoderOn, int keyDownCount, Action<List<int>> handleComboKeyRepeat)
        {
            DateTime dtNow = HRDateTime.Now;
            frmMain?.WriteStrokeLog(decKey, dtNow, true, strokeList.IsEmpty());

            totalKeyDownCount = keyDownCount;
            procQueue.Enqueue(() => keyDown(decKey, dtNow, bDecoderOn, handleComboKeyRepeat));
            HandleQueue();
        }

        public KeyHandlerResult keyDown(int decKey, DateTime dt, bool bDecoderOn, Action<List<int>> handleComboKeyRepeat)
        {
            logger.InfoH(() => $"\nENTER: decKey={decKey}, lastRepeatedDecKey={lastRepeatedDecKey}, strokeList=[{strokeList.ToDebugString()}], " +
                $"IsTemporaryComboDisabled={strokeList.IsTemporaryComboDisabled}, IsDecoderWaitingFirstStroke={frmMain?.IsDecoderWaitingFirstStroke()}");

            if (frmMain.IsDecoderWaitingFirstStroke()) {
                strokeList.IsTemporaryComboDisabled = false;
                logger.InfoH(() => $"IsTemporaryComboDisabled={strokeList.IsTemporaryComboDisabled}");
            }

            checkPreRewriteTime(decKey);

            strokeList.CheckComboShiftKeyUpDt(decKey);

            List<int> result = null;
            bool bUnconditional = false;

            try {
                bool bWaitSecondStroke = frmMain != null && !frmMain.IsDecoderWaitingFirstStroke();
                if (!bWaitSecondStroke) frmMain.IsWaitingSecondStrokeLocked = false;

                var stroke = new Stroke(decKey, bDecoderOn, dt);
                var combo = KeyCombinationPool.CurrentPool.GetEntry(stroke);

                logger.InfoH(() => $"combo={(combo == null ? "null" : combo.DecKeysDebugString())}, DecoderOn={bDecoderOn}, WaitSecondStroke={bWaitSecondStroke}");

                if (lastRepeatedDecKey == decKey && /*strokeList.IsEmpty() && (combo == null || combo.IsSubKey) &&*/ bDecoderOn && bWaitSecondStroke) {
                    // 第2打鍵待ちでオートリピートされた場合、同時打鍵か否かに関係なく、キーを無視して第2打鍵待ち状態を継続する
                    logger.InfoH("IGNORE auto repeat key");
                    bAutoRepeated = true;
                    frmMain.IsWaitingSecondStrokeLocked = true;
                } else {
                    if (lastRepeatedDecKey != decKey) bAutoRepeated = false;
                    lastRepeatedDecKey = decKey;
                    if (frmMain.DecoderOutput.IsDecoderEisuMode()) {
                        // デコーダが英数モードだったので、そのまま返す
                        logger.InfoH("decoder is EISU mode");
                        result = Helper.MakeList(decKey);
                    } else if (combo?.IsTerminal == true && KeyCombinationPool.CurrentPool.IsRepeatableKey(decKey)) {
                        // 終端、かつキーリピートが可能なキーだった(BackSpaceとか)ので、それを返す
                        logger.InfoH("terminal and repeatable key");
                        result = Helper.MakeList(decKey);
                    } else {
                        logger.InfoH(() => stroke.DebugString());

                        // キーリピートのチェック
                        if (strokeList.DetectKeyRepeat(stroke)) {
                            // キーリピートが発生した場合
                            // キーリピート時は、リピートの終わりに1回だけ KeyUp が発生するので、そこで strokeListのUplistがクリアされる
                            logger.InfoH("key repeatable detected");
                            bAutoRepeated = true;
                            if (!bDecoderOn) {
                                // DecoderがOFFのときはキーリピート扱いとする
                                logger.InfoH("Decoder OFF, so repeat key");
                                result = Helper.MakeList(decKey);
                            } else if (KeyCombinationPool.CurrentPool.IsRepeatableKey(decKey)) {
                                // キーリピートが可能なキー
                                logger.InfoH("non terminal and repeatable key");
                                result = Helper.MakeList(decKey);
                            } else if ((stroke.IsComboShift || strokeList.Count == 2 && strokeList.First.IsComboShift) && handleComboKeyRepeat != null) {
                                // 同時打鍵シフトキーの場合は、リピートハンドラを呼び出すだけで、キーリピートは行わない(つまりシフト扱い)
                                List<int> list = new List<int>();
                                if (strokeList.Count == 1) {
                                    logger.InfoH(() => $"Call ComboKeyRepeat Handler: {stroke.ComboShiftDecKey}");
                                    list.Add(stroke.ComboShiftDecKey);
                                } else if (strokeList.Count == 2) {
                                    var keyCombo = strokeList.GetKeyCombo();
                                    if (keyCombo != null) {
                                        logger.InfoH(() => $"Call ComboKeyRepeat Handler: {keyCombo.DecKeysDebugString()}");
                                        if (keyCombo.DecKeyList._safeCount() >= 2) {
                                            list.Add(keyCombo.DecKeyList[0]);
                                            list.Add(KeyCombination.MakeNonTerminalDuplicatableComboKey(keyCombo.DecKeyList[1]));
                                        }
                                    }
                                }
                                handleComboKeyRepeat(list);
                            } else {
                                // キーリピートが不可なキーは無視
                                logger.InfoH("Key repeat ignored");
                            }
                        } else {
                            // キーリピートではない通常の押下の場合は、同時打鍵判定を行う
                            //bool isStrokeListEmpty = strokeList.IsEmpty();
                            if (Settings.AbandonUsedKeysWhenSpecialComboShiftDown && DecoderKeys.IsSpaceOrFuncKey(decKey) && stroke.IsComboShift) {
                                // Spaceまたは機能キーのシフトキーがきたら、使い終わったキーを破棄する
                                logger.InfoH("Abandon Used Keys When Special Combo Shift Down");
                                strokeList.ClearComboList();
                            }
                            logger.InfoH(() => $"combo: {(combo == null ? "null" : "FOUND")}, IsTerminal={combo?.IsTerminal ?? true}, " +
                                $"StrokeList.Count={strokeList.Count}, bWaitSecondStroke={bWaitSecondStroke}, IsTemporaryComboDisabled={strokeList.IsTemporaryComboDisabled}, ");
                            if ((!bWaitSecondStroke || !strokeList.IsTemporaryComboDisabled) && ((combo != null && !combo.IsTerminal) || !strokeList.IsEmpty())) {
                                // 第1打鍵待ちか、同時打鍵が有効であって、
                                // 押下されたのは同時打鍵に使われる可能性のあるキーだった、あるいは同時打鍵シフト後の第2打鍵だった
                                // 打鍵リストに追加して同時打鍵判定を行う
                                strokeList.Add(stroke);
                                if (strokeList.Count == 1) {
                                    // 第1打鍵の場合
                                    if (!stroke.IsComboShift) {
                                        logger.InfoH(() => $"UseCombinationKeyTimer1={Settings.UseCombinationKeyTimer1}");
                                        // 非同時打鍵キーの第1打鍵ならタイマーを起動する
                                        if (Settings.UseCombinationKeyTimer1) {
                                            startTimer(Settings.CombinationKeyMaxAllowedLeadTimeMs, Stroke.ModuloizeKey(decKey), bDecoderOn);
                                        }
                                    }
                                } else if (frmMain?.IsDecoderWaitingFirstStroke() == true && strokeList.IsComboBlocker()) {
                                    // ComboBlocker だったらここで KeyUp を実行
                                    logger.InfoH(() => $"Call keyUp()");
                                    return keyUp(decKey, dt, false, bDecoderOn);
                                } else {
                                    // 第2打鍵以降の場合は、同時打鍵チェック
                                    logger.InfoH(() => $"Check key combo: strokeList={strokeList.ToDebugString()}");
                                    bool bTimer = false;
                                    result = strokeList.GetKeyCombinationWhenKeyDown(out bTimer, out bUnconditional);
                                    if (result._isEmpty()) {
                                        logger.InfoH($"result is EMPTY: bTimer={bTimer}");
                                        if (bTimer || strokeList.Count == 2 /* strokeList.IsSuccessiveShift3rdOrLaterKey() /*strokeList.IsSuccessiveShift2ndOr3rdKey()*/) {
                                            logger.InfoH(() => $"UseCombinationKeyTimer2={Settings.UseCombinationKeyTimer2}, " +
                                                $"NotSpaceNorFuncKey={!DecoderKeys.IsSpaceOrFuncKey(decKey)}, IsTerminalCombo()={strokeList.IsTerminalCombo()}");
                                            // タイマーが有効であるか、または同時打鍵シフトの2打鍵めの非シフト文字キーであって、同時打鍵組合せが終端文字であり、
                                            // かつ、先頭が文字キーでないか文字キーのみの同時打鍵組合せの場合が被覆Comboではない、だったらタイマーを起動する
                                            if (Settings.UseCombinationKeyTimer2 && !stroke.IsComboShift && !DecoderKeys.IsSpaceOrFuncKey(decKey) && strokeList.IsTerminalCombo() &&
                                                (DecoderKeys.IsSpaceOrFuncKey(strokeList.First.OrigDecoderKey) || !Settings.OnlyCharKeysComboShouldBeCoveringCombo)) {
                                                startTimer(Settings.CombinationKeyMinOverlappingTimeMs, Stroke.ModuloizeKey(decKey), bDecoderOn);
                                            }
                                        }
                                    } else if (bTimer && strokeList.Count == 1) {
                                        // 先頭のキーが result に追い出されて、今回のキーだけが残った
                                        logger.InfoH("Timer on");
                                        if (Settings.UseCombinationKeyTimer2 && !stroke.IsComboShift && !DecoderKeys.IsSpaceOrFuncKey(decKey)) {
                                            startTimer(Settings.CombinationKeyMinOverlappingTimeMs, Stroke.ModuloizeKey(decKey), bDecoderOn);
                                        }
                                    }
                                    // 一時的な同時打鍵無効化になったら、チェックポイントの保存
                                    //saveCheckPointDeckeyCount();
                                }
                            } else {
                                // 同時打鍵には使われないキーなので、そのまま返す
                                logger.InfoH("Return ASIS");
                                result = Helper.MakeList(decKey);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex._getErrorMsg());
                strokeList.Clear();
            }

            checkResultAgainstDecoderState(result);
            logger.InfoH(() =>
                $"LEAVE: result={result._keyString()._orElse("empty")}, {strokeList.ToDebugString()}, " +
                $"IsTemporaryComboDisabled={strokeList.IsTemporaryComboDisabled}, lastRepeatedDecKey={lastRepeatedDecKey}, autoRepeated={bAutoRepeated}");

            //if (result._notEmpty()) {
            //    setPreRewriteTime(result.Last());
            //}

            if (result._safeCount() == 1 && KeyCombinationPool.IsComboShift(decKey)) {
                // 結果が単打だったら prevComboShiftKeyUpDt をクリアしておく
                logger.InfoH("SINGLE HIT: CLEAR prevComboShiftKeyUpDt");
                strokeList.ClearPrevComboShiftKeyUpDt(decKey);
            }

            return new KeyHandlerResult() { list = result, bUncoditional = bUnconditional };
        }

        /// <summary>
        /// キーの解放。同時打鍵判定も行う。
        /// </summary>
        /// <param name="decKey">解放されたキーのデコーダコード</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダコード列を返す。<br/>確定しなければ null を返す</returns>
        public void KeyUp(int decKey, bool bDecoderOn, bool bTimer = false)
        {
            DateTime dtNow = HRDateTime.Now;

            logger.Info(() =>
                $"\nCALLED: decKey={decKey}, DecoderOn={bDecoderOn}, bTimer={bTimer}, lastRepeatedDecKey={lastRepeatedDecKey}, " +
                $"bAutoRepeated={bAutoRepeated}, lastRepeatedDecKey={lastRepeatedDecKey}, strokeList={strokeList.ToDebugString()}");
            bool bSameLastKey = !strokeList.IsUnprocListEmpty && strokeList.Last.OrigDecoderKey == decKey;
            bool bComboShiftKeyRepeated = bDecoderOn && bAutoRepeated && KeyCombinationPool.IsComboShift(decKey) && lastRepeatedDecKey == decKey && strokeList.DetectKeyRepeat(decKey);

            bAutoRepeated = false;

            if (bTimer && bSameLastKey) {
                // 漢直配列で第1打鍵がスペースキーとの同時打鍵の場合に、第1打鍵を長押しして第2打鍵待ちの状態でロックしようとすることがあるので、
                // 押下中のキーと同じキーがタイマーによってKeyUpされたときは、キーリピート状態に移行する
                lastRepeatedDecKey = decKey;
                bAutoRepeated = true;
                bComboShiftKeyRepeated = false;
            } else if (!bTimer) {
                lastRepeatedDecKey = -1;
            }

            logger.InfoH(() =>
                $"decKey={decKey}, lastRepeatedDecKey={lastRepeatedDecKey}, ComboShiftKeyRepeated={bComboShiftKeyRepeated}");
            if (bComboShiftKeyRepeated) {
                // ComboShiftキーがリピートされている状態だったら、それを無視
                logger.InfoH("REPEATED COMBO SHIFT KEY IGNORED");
                strokeList.Clear();
            } else if (!bTimer || bSameLastKey) {    // タイマーの場合は、最後に押下されたキーと一致しているか
                frmMain?.WriteStrokeLog(decKey, dtNow, false, false, bTimer);
                procQueue.Enqueue(() => keyUp(decKey, dtNow, bTimer, bDecoderOn));
                HandleQueue();
            } else if (bTimer) {
                logger.InfoH("TIMER IGNORED");
                frmMain?.WriteStrokeLog(-1, dtNow, false, false, true);
            }
        }

        public KeyHandlerResult keyUp(int decKey, DateTime dt, bool bTimer, bool bDecoderOn)
        {
            logger.InfoH(() => $"\nENTER: decKey={decKey}, IsComboShift={KeyCombinationPool.IsComboShift(decKey)}");

            checkPreRewriteTime(decKey);

            // 第1打鍵待ちに戻ったら、一時的な同時打鍵無効化をキャンセルする
            //checkStrokeCountReset();

            bool bUnconditional = false;
            var result = strokeList.GetKeyCombinationWhenKeyUp(decKey, dt, bDecoderOn, out bUnconditional);

            // 一時的な同時打鍵無効化になったら、チェックポイントの保存
            //saveCheckPointDeckeyCount();

            checkResultAgainstDecoderState(result);

            logger.InfoH(() => $"LEAVE: result={result._keyString()._orElse("empty")}, {strokeList.ToDebugString()}");

            if (!bTimer && strokeList.IsEmpty()) frmMain?.FlushStrokeLog();

            //if (result._notEmpty() && bDecoderOn) {
            //    setPreRewriteTime(result.Last());
            //}

            if (KeyCombinationPool.IsSpaceOrFuncComboShift(decKey)) {
                if (result._safeCount() == 1) {
                    // 結果が単打だったら prevComboShiftKeyUpDt をクリアしておく
                    logger.InfoH("SINGLE HIT: CLEAR prevComboShiftKeyUpDt");
                    strokeList.ClearPrevComboShiftKeyUpDt(decKey);
                } else {
                    // ShiftキーがUPされ結果が単打でない場合は、後置シフトを無効にする時間を計測する起点をセット
                    logger.InfoH("SHIFT KEY UP: SET prevComboShiftKeyUpDt");
                    strokeList.SetPrevComboShiftKeyUpDt(decKey, dt);
                }
            }

            return new KeyHandlerResult() { list = result, bUncoditional = bUnconditional };
        }

        //int checkPointKeyDownCount = -1;

        // 一時的な同時打鍵無効化のためのチェックポイントの保存
        //private void saveCheckPointDeckeyCount()
        //{
        //    logger.InfoH(() => $"ENTER: IsTemporaryComboDisabled={strokeList.IsTemporaryComboDisabled}, " +
        //        $"checkPointKeyDownCount={checkPointKeyDownCount}, totalKeyDownCount={totalKeyDownCount}");
        //    if (strokeList.IsTemporaryComboDisabled) {
        //        if (checkPointKeyDownCount < 0) checkPointKeyDownCount = totalKeyDownCount;
        //    } else {
        //        checkPointKeyDownCount = -1;
        //    }
        //    logger.InfoH(() => $"LEAVE: IsTemporaryComboDisabled={strokeList.IsTemporaryComboDisabled}, checkPointKeyDownCount={checkPointKeyDownCount}");
        //}

        // 第1打鍵待ちに戻ったら、一時的な同時打鍵無効化を解除する
        //private void checkStrokeCountReset()
        //{
        //    logger.InfoH(() => $"ENTER: IsTemporaryComboDisabled={strokeList.IsTemporaryComboDisabled}, " +
        //        $"checkPointKeyDownCount={checkPointKeyDownCount}, totalKeyDownCount={totalKeyDownCount}, IsDecoderWaitingFirstStroke={frmMain.IsDecoderWaitingFirstStroke()}");
        //    //if (checkPointKeyDownCount >= 0 && totalKeyDownCount > checkPointKeyDownCount + 1 && frmMain.IsDecoderWaitingFirstStroke()) {
        //    if (frmMain.IsDecoderWaitingFirstStroke()) {
        //        strokeList.IsTemporaryComboDisabled = false;
        //        checkPointKeyDownCount = -1;
        //    }
        //    logger.InfoH(() => $"LEAVE: IsTemporaryComboDisabled={strokeList.IsTemporaryComboDisabled}, checkPointKeyDownCount={checkPointKeyDownCount}");
        //}

        private void checkResultAgainstDecoderState(List<int> result)
        {
            if (result._safeCount() > 1 && frmMain != null && !frmMain.IsDecoderWaitingFirstStroke()) {
                // After 2 stroke or later
                logger.InfoH("Decoder waiting 2nd or later stroke");
                if (result._getFirst() > DecoderKeys.COMBO_DECKEY_START) {
                    result.RemoveAt(0);
                    for (int i = 0; i < result.Count; ++i) {
                        result[i] %= DecoderKeys.PLANE_DECKEY_NUM;
                    }
                }
            }
        }

        /// <summary>
        /// Singleton オブジェクトを返す
        /// </summary>
        public static Determiner Singleton { get; private set; } = new Determiner();
    }
}

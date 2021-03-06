using System;
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
    enum TimerKind
    {
        None,
        FirstStroke,
        JustTwoComboKey,
        SecondOrLaterChar
    }

    /// <summary>
    /// キー入力の時系列に対して、同時打鍵などの判定を行って、出力文字列を決定する
    /// </summary>
    class Determiner : IDisposable
    {
        private static Logger logger = Logger.GetLogger();

        // FrmKanchoku
        FrmKanchoku frmMain;

        // タイマー
        System.Timers.Timer timerA;
        System.Timers.Timer timerB;

        int decKeyForTimerA = -1;
        int decKeyForTimerB = -1;

        TimerKind kindForTimerA = TimerKind.None;
        TimerKind kindForTimerB = TimerKind.None;

        private void timerA_elapsed(Object sender, System.Timers.ElapsedEventArgs e)
        {
            logger.DebugH(() => $"TIMER-A ELAPSED: decKeyForTimerA={decKeyForTimerA}");
            timerA?.Stop();
            if (decKeyForTimerA >= 0) {
                int dk = decKeyForTimerA;
                decKeyForTimerA = -1;
                KeyUp(dk, kindForTimerA);
            }
        }

        private void timerB_elapsed(Object sender, System.Timers.ElapsedEventArgs e)
        {
            logger.DebugH(() => $"TIMER-B ELAPSED: decKeyForTimerB={decKeyForTimerB}");
            timerB?.Stop();
            if (decKeyForTimerB >= 0) {
                int dk = decKeyForTimerB;
                decKeyForTimerB = -1;
                KeyUp(dk, kindForTimerB);
            }
        }

        public Action<List<int>> KeyProcHandler { get; set; }

        public void InitializeTimer(FrmKanchoku frm)
        {
            logger.DebugH(() => $"CALLED");
            frmMain = frm;
            timerA = new System.Timers.Timer();
            timerB = new System.Timers.Timer();
            timerA.SynchronizingObject = frm;
            timerB.SynchronizingObject = frm;
            timerA.Elapsed += timerA_elapsed;
            timerB.Elapsed += timerB_elapsed;
        }

        void startTimer(int ms, int decKey, TimerKind kind)
        {
            logger.DebugH(() => $"CALLED: ms={ms}, decKey={decKey}");
            if (ms <= 0) return;

            if (timerA != null && !timerA.Enabled) {
                decKeyForTimerA = decKey;
                kindForTimerA = kind;
                timerA.Interval = ms;
                timerA.Start();
                logger.DebugH(() => $"TIMER1 STARTED");
            } else if (timerB != null && !timerB.Enabled) {
                decKeyForTimerB = decKey;
                kindForTimerB = kind;
                timerB.Interval = ms;
                timerB.Start();
                logger.DebugH(() => $"TIMER2 STARTED");
            }
        }

        // 同時打鍵保持リスト
        private StrokeList strokeList = new StrokeList();

        // 前置書き換えキーの打鍵時刻
        private DateTime preRewriteDt = DateTime.MinValue;

        // 前キー
        private int prevDownDecKey = -1;

        private void checkPreRewriteTime(int dk)
        {
            if (/*Stroke.ModuloizeKey(prevDecKey) != Stroke.ModuloizeKey(dk) && */ //たぶんこの処理は不要(同じキーの連打は時間チェックをしない)。後で削除
                preRewriteDt._isValid() &&
                Settings.PreRewriteAllowedDelayTimeMs > 0 &&
                (DateTime.Now - preRewriteDt).TotalMilliseconds > Settings.PreRewriteAllowedDelayTimeMs)
            {
                logger.DebugH($"CALL cancelPreRewrite");
                frmMain?.ExecCmdDecoder("cancelPreRewrite", null);
            }
        }

        private void setPreRewriteTime(int dk)
        {
            if (KeyCombinationPool.CurrentPool.IsPreRewriteKey(dk)) {
                logger.DebugH($"set PreRewrite DateTime");
                preRewriteDt = DateTime.Now;
            } else {
                preRewriteDt = DateTime.MinValue;
            }
        }

        public void Dispose()
        {
            timerA?.Dispose();
            timerA = null;
            timerB?.Dispose();
            timerB = null;
        }

        /// <summary>
        /// 初期化と同時打鍵組合せ辞書の読み込み
        /// </summary>
        /// <param name="tableFile">主テーブルファイル名</param>
        /// <param name="tableFile2">副テーブルファイル名</param>
        public void Initialize(string tableFile, string tableFile2)
        {
            Settings.ClearSpecificDecoderSettings();
            KeyCombinationPool.Initialize();
            Clear();

            new TableFileParser().ParseTableFile(tableFile, "tmp/tableFile1.tbl", KeyCombinationPool.Singleton1, true);

            if (tableFile2._notEmpty()) {
                new TableFileParser().ParseTableFile(tableFile2, "tmp/tableFile2.tbl", KeyCombinationPool.Singleton2, false);
            }
        }

        /// <summary>
        /// 選択されたテーブルファイルに合わせて、KeyComboPoolを入れ替える
        /// </summary>
        public void ExchangeKeyCombinationPool()
        {
            KeyCombinationPool.ExchangeCurrentPool();
        }

        public void UsePrimaryPool()
        {
            KeyCombinationPool.UsePrimaryPool();
        }

        public void UseSecondaryPool()
        {
            KeyCombinationPool.UseSecondaryPool();
        }

        public bool IsEnabled => KeyCombinationPool.CurrentPool.Enabled;

        /// <summary>
        /// 同時打鍵リストをクリアする
        /// </summary>
        public void Clear()
        {
            strokeList.Clear();
        }

        private Queue<Func<List<int>>> procQueue = new Queue<Func<List<int>>>();

        private bool bHandling = false;

        public void HandleQueue()
        {
            if (bHandling) return;

            bHandling = true;
            try {
                while (procQueue.Count > 0) {
                    var list = procQueue.Dequeue().Invoke();
                    KeyProcHandler?.Invoke(list);
                }
            } catch (Exception ex) {
                logger.Error(ex._getErrorMsg());
            } finally {
                bHandling = false;
            }
        }

        /// <summary>
        /// キーの押下<br/>押下されたキーをキューに積み、可能であれば同時打鍵判定も行う
        /// </summary>
        /// <param name="decKey">押下されたキーのデコーダコード</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダコード列を返す。<br/>確定しなければ null を返す</returns>
        public void KeyDown(int decKey, Action<int> handleComboKeyRepeat)
        {
            DateTime dtNow = DateTime.Now;
            frmMain?.WriteStrokeLog(decKey, dtNow, true, strokeList.IsEmpty());

            procQueue.Enqueue(() => keyDown(decKey, dtNow, handleComboKeyRepeat));
            HandleQueue();
        }

        public List<int> keyDown(int decKey, DateTime dt, Action<int> handleComboKeyRepeat)
        {
            logger.DebugH(() => $"\nENTER: decKey={decKey}");

            checkPreRewriteTime(decKey);
            prevDownDecKey = decKey;

            List<int> result = null;

            try {
                var stroke = new Stroke(decKey, dt);
                var combo = KeyCombinationPool.CurrentPool.GetEntry(stroke);
                if (combo?.IsTerminal == true && KeyCombinationPool.CurrentPool.IsRepeatableKey(decKey)) {
                    // 終端、かつキーリピートが可能なキーだった(BacSpaceとか)ので、それを返す
                    logger.DebugH("terminal and repeatable key");
                    result = Helper.MakeList(decKey);
                } else {
                    logger.DebugH(() => stroke.DebugString());

                    // キーリピートのチェック
                    if (strokeList.DetectKeyRepeat(stroke)) {
                        // キーリピートが発生した場合
                        if (stroke.IsComboShift && handleComboKeyRepeat != null) {
                            // 同時打鍵シフトキーの場合は、リピートハンドラを呼び出す
                            handleComboKeyRepeat(stroke.ComboShiftDecKey);
                        } else if (KeyCombinationPool.CurrentPool.IsRepeatableKey(decKey)) {
                            // キーリピートが可能なキー
                            logger.DebugH("non terminal and repeatable key");
                            result = Helper.MakeList(decKey);
                        } else {
                            // キーリピートが不可なキーは無視
                            logger.DebugH("Key repeat ignored");
                        }
                    } else {
                        // キーリピートではない通常の押下の場合は、同時打鍵判定を行う
                        //bool isStrokeListEmpty = strokeList.IsEmpty();
                        logger.DebugH(() => $"combo: {(combo == null ? "null" : "FOUND")}, IsTerminal={combo?.IsTerminal ?? true}, isStrokeListEmpty={strokeList.IsEmpty()}");
                        if ((combo != null && !combo.IsTerminal) || !strokeList.IsEmpty()) {
                            // 押下されたのは同時打鍵に使われる可能性のあるキーだった、あるいは同時打鍵シフト後の第2打鍵だったので、打鍵リストに追加して同時打鍵判定を行う
                            strokeList.Add(stroke);
                            if (strokeList.Count == 1) {
                                // 第1打鍵の場合
                                if (!stroke.IsComboShift) {
                                    logger.DebugH(() => $"UseCombinationKeyTimer1={Settings.UseCombinationKeyTimer1}");
                                    // 非同時打鍵キーの第1打鍵ならタイマーを起動する
                                    if (Settings.UseCombinationKeyTimer1) startTimer(Settings.CombinationKeyMaxAllowedLeadTimeMs, Stroke.ModuloizeKey(decKey), TimerKind.FirstStroke);
                                }
                            } else {
                                // 第2打鍵以降の場合は、同時打鍵チェック
                                bool bTimer = false;
                                result = strokeList.GetKeyCombinationWhenKeyDown(out bTimer);
                                if (result._isEmpty()) {
                                    if (bTimer || strokeList.IsSuccessiveShift2ndKey()) {
                                        logger.DebugH(() => $"UseCombinationKeyTimer2={Settings.UseCombinationKeyTimer2}, TimerKind={(bTimer ? TimerKind.JustTwoComboKey : TimerKind.SecondOrLaterChar)}");
                                        // タイマーが有効であるか、または同時打鍵シフト後の第2打鍵であって同時打鍵が未判定だったらタイマーを起動する
                                        if (Settings.UseCombinationKeyTimer2) {
                                            startTimer(Settings.CombinationKeyMinOverlappingTimeMs, Stroke.ModuloizeKey(decKey),
                                                bTimer ? TimerKind.JustTwoComboKey : TimerKind.SecondOrLaterChar);
                                        }
                                    }
                                }
                            }
                        } else {
                            // 同時打鍵には使われないキーなので、そのまま返す
                            logger.DebugH("Return ASIS");
                            result = Helper.MakeList(decKey);
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex._getErrorMsg());
                strokeList.Clear();
            }

            logger.DebugH(() => $"LEAVE: result={result._keyString()._orElse("empty")}, {strokeList.ToDebugString()}");

            if (result._notEmpty()) {
                setPreRewriteTime(result.Last());
            }

            return result;
        }

        /// <summary>
        /// キーの解放。同時打鍵判定も行う。
        /// </summary>
        /// <param name="decKey">解放されたキーのデコーダコード</param>
        /// <param name="timerKind">1 .. forFirstCharbyTimer, 2..forSecondCharByTimer</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダコード列を返す。<br/>確定しなければ null を返す</returns>
        public void KeyUp(int decKey, TimerKind timerKind = 0)
        {
            DateTime dtNow = DateTime.Now;
            logger.DebugH(() => $"\ndecKey={decKey}, forChar={timerKind}, {strokeList.ToDebugString()}");
            bool bTimer = timerKind != TimerKind.None;
            if (!bTimer || timerKind == TimerKind.JustTwoComboKey ||
                (timerKind == TimerKind.FirstStroke && strokeList.IsComboListEmpty && strokeList.UnprocListCount == 1) ||   // タイマーによる1文字目キーUPのとき
                (timerKind == TimerKind.SecondOrLaterChar && strokeList.UnprocListCount == 1))                              // タイマーによる２文字目キーUPのとき
            {
                frmMain?.WriteStrokeLog(decKey, dtNow, false, false, bTimer);
                procQueue.Enqueue(() => keyUp(decKey, dtNow, bTimer));
                HandleQueue();
            } else if (bTimer) {
                logger.DebugH(() => $"TIMER IGNORED");
                frmMain?.WriteStrokeLog(-1, dtNow, false, false, true);
            }
        }

        public List<int> keyUp(int decKey, DateTime dt, bool bTimer)
        {
            logger.DebugH(() => $"\nENTER: decKey={decKey}");

            checkPreRewriteTime(decKey);

            var result = strokeList.GetKeyCombinationWhenKeyUp(decKey, dt);

            logger.DebugH(() => $"LEAVE: result={result._keyString()._orElse("empty")}, {strokeList.ToDebugString()}");

            if (!bTimer && strokeList.IsEmpty()) frmMain?.FlushStrokeLog();

            if (result._notEmpty()) {
                setPreRewriteTime(result.Last());
            }

            return result;
        }

        /// <summary>
        /// Singleton オブジェクトを返す
        /// </summary>
        public static Determiner Singleton { get; private set; } = new Determiner();
    }
}

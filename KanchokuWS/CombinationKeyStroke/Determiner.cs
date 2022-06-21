using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KanchokuWS.CombinationKeyStroke.DeterminerLib;
using Utils;

namespace KanchokuWS.CombinationKeyStroke
{
    /// <summary>
    /// キー入力の時系列に対して、同時打鍵などの判定を行って、出力文字列を決定する
    /// </summary>
    class Determiner : IDisposable
    {
        private static Logger logger = Logger.GetLogger(true);

        // FrmKanchoku
        FrmKanchoku frmMain;

        // タイマー
        System.Timers.Timer timer1;
        System.Timers.Timer timer2;

        int decKeyForTimer1 = -1;
        int decKeyForTimer2 = -1;

        private void timer1_elapsed(Object sender, System.Timers.ElapsedEventArgs e)
        {
            logger.DebugH(() => $"TIMER1 ELAPSED: decKeyForTimer1={decKeyForTimer1}");
            timer1?.Stop();
            if (decKeyForTimer1 >= 0) {
                int dk = decKeyForTimer1;
                decKeyForTimer1 = -1;
                KeyUp(dk);
            }
        }

        private void timer2_elapsed(Object sender, System.Timers.ElapsedEventArgs e)
        {
            logger.DebugH(() => $"TIMER2 ELAPSED: decKeyForTimer2={decKeyForTimer2}");
            timer2?.Stop();
            if (decKeyForTimer2 >= 0) {
                int dk = decKeyForTimer2;
                decKeyForTimer2 = -1;
                KeyUp(dk);
            }
        }

        public Action<List<int>> KeyProcHandler { get; set; }

        public void InitializeTimer(FrmKanchoku frm)
        {
            logger.DebugH(() => $"CALLED");
            frmMain = frm;
            timer1 = new System.Timers.Timer();
            timer2 = new System.Timers.Timer();
            timer1.SynchronizingObject = frm;
            timer2.SynchronizingObject = frm;
            timer1.Elapsed += timer1_elapsed;
            timer2.Elapsed += timer2_elapsed;
        }

        void startTimer(int ms, int decKey)
        {
            logger.DebugH(() => $"CALLED: ms={ms}, decKey={decKey}");
            if (timer1 != null && !timer1.Enabled) {
                decKeyForTimer1 = decKey;
                timer1.Interval = ms;
                timer1.Start();
                logger.DebugH(() => $"TIMER1 STARTED");
            } else if (timer2 != null && !timer2.Enabled) {
                decKeyForTimer2 = decKey;
                timer2.Interval = ms;
                timer2.Start();
                logger.DebugH(() => $"TIMER2 STARTED");
            }
        }

        // 同時打鍵保持リスト
        private StrokeList strokeList = new StrokeList();

        // 前置書き換えキーの打鍵時刻
        private DateTime preRewriteDt = DateTime.MinValue;

        // 前キー
        private int prevDecKey = -1;

        private void checkPreRewriteTime(int dk)
        {
            if (Stroke.ModuloizeKey(prevDecKey) != Stroke.ModuloizeKey(dk) && 
                preRewriteDt._isValid() &&
                Settings.PreRewriteAllowedDelayTimeMs > 0 &&
                (DateTime.Now - preRewriteDt).TotalMilliseconds > Settings.PreRewriteAllowedDelayTimeMs) {
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
            timer1?.Dispose();
            timer1 = null;
            timer2?.Dispose();
            timer2 = null;
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

            var parser = new TableFileParser(KeyCombinationPool.Singleton1, true);
            parser.ParseTable(tableFile, "tmp/tableFile1.tbl");

            if (tableFile2._notEmpty()) {
                var parser2 = new TableFileParser(KeyCombinationPool.Singleton2, false);
                parser2.ParseTable(tableFile2, "tmp/tableFile2.tbl");
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
            procQueue.Enqueue(() => keyDown(decKey, handleComboKeyRepeat));
            HandleQueue();
        }

        public List<int> keyDown(int decKey, Action<int> handleComboKeyRepeat)
        {
            logger.DebugH(() => $"\nENTER: decKey={decKey}");

            checkPreRewriteTime(decKey);
            prevDecKey = decKey;

            List<int> result = null;

            try {
                if (KeyCombinationPool.CurrentPool.IsRepeatableKey(decKey)) {
                    // キーリピートが可能なキーだった(BacSpaceとか)ので、それを返す
                    logger.DebugH("repeatable key");
                    result = Helper.MakeList(decKey);
                } else {
                    var stroke = new Stroke(decKey, DateTime.Now);
                    logger.DebugH(() => stroke.DebugString());

                    // キーリピートのチェック
                    if (strokeList.DetectKeyRepeat(stroke)) {
                        // キーリピートが発生した場合
                        // キーリピートが不可なキーは無視
                        logger.DebugH("Key repeat ignored");
                        // 同時打鍵シフトキーの場合は、リピートハンドラを呼び出す
                        if (stroke.IsComboShift && handleComboKeyRepeat != null) handleComboKeyRepeat(stroke.ComboShiftDecKey);
                    } else {
                        // キーリピートではない通常の押下の場合は、同時打鍵判定を行う
                        var combo = KeyCombinationPool.CurrentPool.GetEntry(stroke);
                        bool isStrokeListEmpty = strokeList.IsEmpty();
                        logger.DebugH(() => $"combo: {(combo == null ? "null" : "FOUND")}, IsTerminal={combo?.IsTerminal ?? true}, isStrokeListEmpty={isStrokeListEmpty}");
                        if ((combo != null && !combo.IsTerminal) || !isStrokeListEmpty) {
                            // 押下されたのは同時打鍵に使われる可能性のあるキーだった、あるいは同時打鍵シフト後の第2打鍵だったので、キューに追加して同時打鍵判定を行う
                            strokeList.Add(stroke);
                            result = strokeList.GetKeyCombinationWhenKeyDown(decKey);
                            if (result._isEmpty()) {
                                if (isStrokeListEmpty) {
                                    if (!stroke.IsComboShift) {
                                        logger.DebugH($"UseCombinationKeyTimer1={Settings.UseCombinationKeyTimer1}");
                                        // 非同時打鍵キーの第1打鍵であり、未確定だったらタイマーを起動する
                                        if (Settings.UseCombinationKeyTimer1) startTimer(Settings.CombinationKeyMaxAllowedLeadTimeMs, Stroke.ModuloizeKey(decKey));
                                    }
                                } else {
                                    if (strokeList.IsSuccessiveShift2ndKey()) {
                                        logger.DebugH($"UseCombinationKeyTimer2={Settings.UseCombinationKeyTimer2}");
                                        // 同時打鍵シフト後の第2打鍵であり、同時打鍵が未判定だったらタイマーを起動する
                                        if (Settings.UseCombinationKeyTimer2) startTimer(Settings.CombinationKeyMinOverlappingTimeMs, Stroke.ModuloizeKey(decKey));
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

            logger.DebugH(() => $"LEAVE: result={result._keyString()._orElse("empty")}: {strokeList.ToDebugString()}");

            if (result._notEmpty()) {
                setPreRewriteTime(result.Last());
            }

            return result;
        }

        /// <summary>
        /// キーの解放。同時打鍵判定も行う。
        /// </summary>
        /// <param name="decKey">解放されたキーのデコーダコード</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダコード列を返す。<br/>確定しなければ null を返す</returns>
        public void KeyUp(int decKey)
        {
            procQueue.Enqueue(() => keyUp(decKey));
            HandleQueue();
        }

        public List<int> keyUp(int decKey)
        {
            logger.DebugH(() => $"\nENTER: decKey={decKey}");

            checkPreRewriteTime(decKey);

            var result = strokeList.GetKeyCombinationWhenKeyUp(decKey, DateTime.Now);

            logger.DebugH(() => $"LEAVE: result={result._keyString()._orElse("empty")}: {strokeList.ToDebugString()}");

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

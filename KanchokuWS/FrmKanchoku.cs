using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using KanchokuWS.Domain;
using KanchokuWS.Handler;
using KanchokuWS.Gui;
using KanchokuWS.TableParser;
using Utils;

namespace KanchokuWS
{
    public partial class FrmKanchoku : Form
    {
        private static Logger logger = Logger.GetLogger();

        //------------------------------------------------------------------
        [DllImport("user32.dll")]
        private static extern void ShowWindow(IntPtr hWnd, int nCmdShow);

        // ウィンドウをアクティブにせずに表示する
        private const int SW_SHOWNA = 8;

        //------------------------------------------------------------------
        // GUI側の処理
        //------------------------------------------------------------------
        /// <summary> 仮想鍵盤フォーム </summary>
        private FrmVirtualKeyboard frmVkb;

        public bool IsVkbHiddenTemporay = false;

        public void ShowFrmVkb()
        {
            frmVkb.ShowNonActive();   // NonActive
        }

        /// <summary> 漢直モードマーカーフォーム </summary>
        private FrmModeMarker frmMode;

        public void ShowFrmMode()
        {
            ShowWindow(frmMode.Handle, SW_SHOWNA);   // NonActive
        }

        private DlgStrokeLog dlgStrokeLog = null;

        private DateTime strokeLogLastDt;

        private StringBuilder sbStrokeLog = new StringBuilder();

        public void ShowDlgStrokeLog(Form frmFocus, int left, int top)
        {
            logger.DebugH("ENTER");
            // 打鍵ログ表示ダイアログの作成
            if (dlgStrokeLog != null) {
                dlgStrokeLog._showTopMost();
            } else {
                dlgStrokeLog = new DlgStrokeLog(NotifyToCloseDlgStrokeLog, frmFocus);
                dlgStrokeLog.Show();
                MoveWindow(dlgStrokeLog.Handle, left, top, dlgStrokeLog.Width, dlgStrokeLog.Height, true);
                sbStrokeLog.Clear();
            }
            logger.DebugH("LEAVE");
        }

        public void CloseDlgStrokeLog()
        {
            logger.DebugH("CALLED");
            dlgStrokeLog?.Close();
        }

        public void NotifyToCloseDlgStrokeLog()
        {
            logger.DebugH("CALLED");
            dlgStrokeLog = null;
        }

        public void WriteStrokeLog(int decKey, DateTime dt, bool bDown, bool bFirst, bool bTimer = false)
        {
            if (IsDecoderActive && dlgStrokeLog != null) {
                char faceCh = bTimer && decKey < 0 ? '\0' : DecoderKeyVsChar.GetFaceCharFromDecKey(decKey)._gtZeroOr('?');
                if (bDown && faceCh >= 'a' && faceCh <= 'z') faceCh = (char)(faceCh - 0x20);
                string msg = $"{(bTimer ? "*Up " : bDown ? "Down" : "Up  ")} | " + (faceCh != '\0' ? $"'{faceCh}'" : "N/A");
                logger.DebugH(() => $"WriteStrokeLog: {msg}");
                appenStrokeLog(msg, dt, bFirst);
            }
        }

        public void WriteStrokeLog(string str)
        {
            if (IsDecoderActive && dlgStrokeLog != null) {
                string msg = $"Out  | '{str}'";
                logger.DebugH(() => $"WriteStrokeLog: {msg}");
                appenStrokeLog(msg, DateTime.Now, false);
                //if (CombinationKeyStroke.Determiner.Singleton.IsStrokeListEmpty()) FlushStrokeLog();
            }
        }

        private void appenStrokeLog(string msg, DateTime dt, bool bFirst)
        {
            string sDiff = "    --";
            int diffMs = strokeLogLastDt._isValid() ? (int)Math.Round((dt - strokeLogLastDt).TotalMilliseconds) : 100000;
            if (bFirst || diffMs >= 1000) {
                sbStrokeLog.Append($"--- time --- | diff ms\r\n");
                //if (diffMs >= 60000) diffMs = 0;
            } else {
                sDiff = $"{diffMs,6:#,0}";
            }
            sbStrokeLog.Append($"{dt.ToString("HH:mm:ss.fff")} | {sDiff} | {msg}\r\n");
            strokeLogLastDt = dt;
        }

        public void FlushStrokeLog()
        {
            if (IsDecoderActive && dlgStrokeLog != null) {
                dlgStrokeLog?.WriteLog(sbStrokeLog.ToString());
            }
            sbStrokeLog.Clear();
        }

        private void flushStrokeLogByTimer()
        {
            if (sbStrokeLog.Length > 0 && IsDecoderActive && dlgStrokeLog != null && DateTime.Now > strokeLogLastDt.AddMilliseconds(300))
                FlushStrokeLog();
        }

        //------------------------------------------------------------------
        //private string CharCountFile;

        private const int timerInterval = 100;

        private KeyboardEventHandler keDispatcher { get; set; }

        /// <summary>
        /// 複合コマンド文字列
        /// </summary>
        private string complexCommandStr = null;

        //------------------------------------------------------------------
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FrmKanchoku(KeyboardEventHandler dispatcher)
        {
            keDispatcher = dispatcher;

            // スクリーン情報の取得
            ScreenInfo.CreateSingleton();

            showSplash();

            InitializeComponent();

            FormBorderStyle = FormBorderStyle.None;
            Width = 1;
            Height = 1;
            WindowState = FormWindowState.Minimized;
            Opacity = 0;

            DpiChanged += dpiChangedHandler;
        }

        private void dpiChangedHandler(object sender, DpiChangedEventArgs e)
        {
            logger.InfoH(() => $"CALLED: new dpi={e.DeviceDpiNew}");
        }

        //------------------------------------------------------------------
        /// <summary>
        /// Form のロード
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FrmKanchoku_Load(object sender, EventArgs e)
        {
            logger.WriteInfo($"\n\n==== KANCHOKU WS START (LogLevel={Logger.LogLevel}) ====");

            IMEHandler.MainWnd = this.Handle;

            // kanchoku.user.ini が存在しなければ、初期状態で作成しておく
            if (!UserKanchokuIni.Singleton.IsIniFileExist) {
                logger.WriteInfo("kanchoku.user.ini not found. Create.");
                UserKanchokuIni.Singleton.SetInt("logLevel", Logger.LogLevelWarn);
            }
            // デバッグ用設定の読み込み
            Settings.ReadIniFileForDebug();

            // DeterminerのタイマーのSynchronizingObjectを自身に設定しておく
            CombinationKeyStroke.Determiner.Singleton.InitializeTimer(this);

            // 各種サンプルから本番ファイルをコピー(もし無ければ)
            copySampleFiles();

            // 仮想鍵盤フォームの作成
            frmVkb = new FrmVirtualKeyboard(this);
            frmVkb.Opacity = 0;     // 最初は完全透明にして目に見えないようにする
            frmVkb.Show();

            // 漢直モード表示フォームの作成
            frmMode = new FrmModeMarker(this, frmVkb);
            frmMode.Show();
            frmMode.Hide();

            // アクティブなウィンドウのハンドラ作成
            ActiveWindowHandler.CreateSingleton();

            // SendInputハンドラの作成
            SendInputHandler.CreateSingleton();

            // 初期化
            VKeyComboRepository.Initialize();
            ExtraModifiers.Initialize();
            DlgModConversion.Initialize();

            // キーボードファイルの読み込み
            bool resultOK = readKeyboardFile();

            // 設定ファイルの読み込み
            Settings.ReadIniFile();

            if (!resultOK) return;

            // 各種定義ファイルの読み込み
            ReadDefFiles();

            // 漢直初期化処理
            await initializeKanchoku();

            // デバッグ用テーブルファイルの出力
            if (Settings.OutputDebugTableFiles) dumpDebugTableFiles();

            //Text = "漢直窓S";

            // 仮想鍵盤フォームを隠す
            frmVkb.Hide();
            frmVkb.Opacity = 100;

            if (frmSplash != null) frmSplash.IsKanchokuReady = true;

            // 辞書保存チャレンジ開始時刻の設定
            reinitializeSaveDictsChallengeDt();

            // タイマー開始
            timer1.Interval = timerInterval;
            timer1.Start();
            logger.Info("Timer Started");

            // キーボードイベントのディスパッチ開始
            initializeKeyboardEventDispatcher();
        }

        // 各種サンプルから本番ファイルをコピー(もし無ければ)
        private void copySampleFiles()
        {
            void copySampleFile(string filename)
            {
                var rootDir = KanchokuIni.Singleton.KanchokuDir;
                var sampleFilename = filename._safeReplace(".txt", ".sample.txt");
                var sampleFilePath = rootDir._joinPath(sampleFilename);
                var prodFilePath = rootDir._joinPath(filename);
                if (Helper.FileExists(sampleFilePath) && !Helper.FileExists(prodFilePath)) {
                    logger.WriteInfo($"COPY {sampleFilename} to {filename}.");
                    Helper.CopyFile(sampleFilePath, prodFilePath);
                }
            }

            copySampleFile("easy_chars.txt");
            copySampleFile("stroke-help.txt");
            copySampleFile("mod-conversion.txt");
        }

        /// <summary> キーボードファイルの読み込み (成功したら true, 失敗したら false を返す) </summary>
        private bool readKeyboardFile()
        {
            if (!Domain.VKeyVsDecoderKey.ReadKeyboardFile()) {
                // キーボードファイルを読み込めなかったので終了する
                logger.Error($"CLOSE: Can't read keyboard file");
                //this.Close();
                frmSplash?.Fallback();
                return false;
            }
            return true;
        }

        /// <summary> 各種定義ファイルの読み込み </summary>
        public void ReadDefFiles()
        {
            logger.InfoH("ENTER");

            logConstant();

            // テーブルファイルの先読み（各種制約の設定）
            if (Settings.TableFile._notEmpty()) {
                new TableFileParser().ReadDirectives(Settings.TableFile, true);
            }
            if (Settings.TableFile2._notEmpty()) {
                new TableFileParser().ReadDirectives(Settings.TableFile2, false);
            }

            // 文字定義ファイルの読み込み
            Domain.DecoderKeyVsChar.ReadCharsDefFile();

            // 追加の修飾キー定義ファイルの読み込み
            if (Settings.ExtraModifiersEnabled && Settings.ModConversionFile._notEmpty()) {
                complexCommandStr = ExtraModifiers.ReadExtraModConversionFile(Settings.ModConversionFile);
            }

            // 設定ダイアログで割り当てたSandSシフト面による上書き
            ShiftPlane.AssignSanSPlane();

            // 漢字読みファイルの読み込み
            if (Settings.KanjiYomiFile._notEmpty()) {
                KanjiYomiTable.ReadKanjiYomiFile(Settings.KanjiYomiFile);
            }

            // 同時打鍵設定の読み込み
            CombinationKeyStroke.Determiner.Singleton.Initialize(Settings.TableFile, Settings.TableFile2, Settings.TableFile3);

            logger.InfoH("LEAVE");
        }

        private void logConstant()
        {
            logger.InfoH(() => $"TOTAL_SHIFT_DECKEY_NUM={DecoderKeys.TOTAL_SHIFT_DECKEY_NUM}");
            logger.InfoH(() => $"FUNC_DECKEY_START={DecoderKeys.FUNC_DECKEY_START}");
            logger.InfoH(() => $"STROKE_DECKEY_END={DecoderKeys.STROKE_DECKEY_END}");
            logger.InfoH(() => $"COMBO_DECKEY_START={DecoderKeys.COMBO_DECKEY_START}");
            logger.InfoH(() => $"COMBO_EX_DECKEY_START={DecoderKeys.COMBO_EX_DECKEY_START}");
            logger.InfoH(() => $"EISU_COMBO_DECKEY_START={DecoderKeys.EISU_COMBO_DECKEY_START}");
            logger.InfoH(() => $"EISU_COMBO_EX_DECKEY_START={DecoderKeys.EISU_COMBO_EX_DECKEY_START}");
            logger.InfoH(() => $"CTRL_DECKEY_START={DecoderKeys.CTRL_DECKEY_START}");
            logger.InfoH(() => $"CTRL_FUNC_DECKEY_START={DecoderKeys.CTRL_FUNC_DECKEY_START}");
            logger.InfoH(() => $"TOTAL_DECKEY_NUM={DecoderKeys.TOTAL_DECKEY_NUM}");
            logger.InfoH(() => $"UNCONDITIONAL_DECKEY_OFFSET={DecoderKeys.UNCONDITIONAL_DECKEY_OFFSET}");
            logger.InfoH(() => $"UNCONDITIONAL_DECKEY_END={DecoderKeys.UNCONDITIONAL_DECKEY_END}");
            logger.InfoH(() => $"CTRL_SCR_LOCK_DECKEY={DecoderKeys.CTRL_SCR_LOCK_DECKEY}");
            logger.InfoH(() => $"SPECIAL_DECKEY_ID_BASE={DecoderKeys.SPECIAL_DECKEY_ID_BASE}");
        }

        private void updateStrokeNodesByComplexCommands()
        {
            if (complexCommandStr._notEmpty()) {
                // 複合コマンド定義があるので、デコーダに送る
                ExecCmdDecoder("updateStrokeNodes", complexCommandStr);
                complexCommandStr = null;
            }
        }

        /// <summary> 各種定義ファイルの読み込み </summary>
        public void ReloadDefFiles()
        {
            logger.InfoH("ENTER");

            // 各種定義ファイルの読み込み
            ReadDefFiles();

            // デコーダの再設定(ここでストローク木も構築される)
            ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            updateStrokeNodesByComplexCommands();

            // 初期打鍵表(下端機能キー以外は空白)の作成
            MakeInitialVkbTable();

            // 打鍵テーブルの作成
            if (Settings.StrokeHelpFile._notEmpty()) {
                frmVkb?.MakeStrokeTables(Settings.StrokeHelpFile);
            }

            logger.InfoH("LEAVE");
        }

        //------------------------------------------------------------------
        /// <summary>
        /// Form のクローズ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrmKanchoku_FormClosing(object sender, FormClosingEventArgs e)
        {
            logger.InfoH("\nCALLED");
            if (frmSplash != null) {
                closeSplash();
                logger.Debug("Splash Closed");
            }

            // タイマー終了
            timer1.Stop();
            logger.Info("Timer Stopped");

            //// 出力文字カウントをファイルに出力
            //ActiveWindowHandler.Singleton.WriteCharCountFile(CharCountFile);

            // この後は各種終了処理
            //DecKeyHandler.Destroy();
            ActiveWindowHandler.DisposeSingleton();
            SendInputHandler.DisposeSingleton();
            finalizeDecoder();
            frmMode?.Close();
            frmVkb?.Close();

            // 再起動
            if (bRestart) {
                logger.InfoH("RESTART");
                MultiAppChecker.Release();
                //Helper.WaitMilliSeconds(1000);
                logger.InfoH("Start another process...\n");
                Logger.Close();
                Helper.StartProcess(SystemHelper.GetExePath(), null);
            }

            // 各種Timer処理が終了するのを待つ
            Helper.WaitMilliSeconds(200);
            Logger.EnableInfoH();
            logger.WriteInfo("==== KANCHOKU WS TERMINATED ====\n");
        }

        private bool bRestart = false;
        private bool bNoSaveDicts = false;

        //------------------------------------------------------------------
        // 終了
        public void Terminate()
        {
            logger.InfoH("CALLED");
            if (frmSplash != null) {
                closeSplash();
                logger.Info("Splash Closed");
            }

            DeactivateDecoderWithModifiersOff();

            logger.InfoH($"ConfirmOnClose={Settings.ConfirmOnClose}");
            if (!Settings.ConfirmOnClose || SystemHelper.OKCancelDialog("漢直窓Sを終了します。\r\nよろしいですか。")) {
                logger.InfoH("CLOSING...");
                Close();
            }
        }

        //------------------------------------------------------------------
        // 再起動
        public void Restart(bool bNoSave)
        {
            logger.InfoH($"CALLED: bNoSave={bNoSave}");
            if (frmSplash != null) {
                closeSplash();
                logger.Info("Splash Closed");
            }

            DeactivateDecoderWithModifiersOff();

            logger.InfoH($"bNoSave={bNoSave}, ConfirmOnRestart={Settings.ConfirmOnRestart}");
            var msg = bNoSave ?
                "漢直窓Sを再起動します。\r\nデコーダが保持している辞書内容はファイルに保存されません。\r\nよろしいですか。" :
                "漢直窓Sを再起動します。\r\nデコーダが保持している辞書内容をファイルに書き出すので、\r\nユーザーが直接辞書ファイルに加えた変更は失われます。\r\nよろしいですか。";
            if (!Settings.ConfirmOnRestart || SystemHelper.OKCancelDialog(msg)) {
                logger.InfoH("RESTARTING...");
                bRestart = true;
                bNoSaveDicts = bNoSave;
                Close();
            }
        }

        //------------------------------------------------------------------
        FrmSplash frmSplash = null;

        /// <summary>
        /// splash の表示
        /// </summary>
        private void showSplash()
        {
            // ここではまだ Settings の初期化を行っていないので、自分で kanchoku.ini の読み込みをやる必要がある
            int sec = Settings.GetString("splashWindowShowDuration")._parseInt(60)._lowLimit(0);
            if (sec > 0) {
                var frm = new FrmSplash(sec);
                frm.Show();
                frmSplash = frm;
                Helper.WaitMilliSeconds(50);        // 直ちに表示されるようにするために、DoEvents を呼び出してちょっと待つ
            }
        }

        BoolObject syncSplash = new BoolObject();

        private void closeSplash()
        {
            logger.InfoH("CALLED");
            if (frmSplash != null) {
                frmSplash.TopMost = false;
                frmSplash.Hide();
                if (syncSplash.BusyCheck()) return;
                using (syncSplash) {
                    if (frmSplash != null) {
                        frmSplash.IsKanchokuTerminated = true;
                        logger.InfoH("CLOSED");
                    }
                    frmSplash = null;
                }
            }
        }

        //------------------------------------------------------------------
        // Win32 呼び出し
        [DllImport("User32.dll")]
        private static extern int PostMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);

        //------------------------------------------------------------------
        // Decoder 呼び出し
        //------------------------------------------------------------------
        /// <summary> Decoder の生成 </summary>
        /// <param name="logLevel"></param>
        /// <returns>生成されたDecoderインスタンスのポインタ</returns>
        [DllImport("kw-uni.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr CreateDecoder(int logLevel);

        /// <summary> Decoder の初期化 </summary>
        /// <param name="decoder"></param>
        /// <param name="decParams">初期化用パラメータ</param>
        [DllImport("kw-uni.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int InitializeDecoder(IntPtr decoder, IntPtr decParams);

        /// <summary> Decoder の状態のリセット </summary>
        /// <param name="decoder"></param>
        [DllImport("kw-uni.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void ResetDecoder(IntPtr decoder);

        /// <summary> Decoder の扱う辞書の保存 </summary>
        /// <param name="decoder"></param>
        [DllImport("kw-uni.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void SaveDictsDecoder(IntPtr decoder);

        /// <summary> Decoder の終了 </summary>
        /// <param name="decoder"></param>
        [DllImport("kw-uni.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void FinalizeDecoder(IntPtr decoder);

        /// <summary> Decoder に外字テーブルの作成を要求する </summary>
        /// <param name="decoder"></param>
        [DllImport("kw-uni.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void MakeInitialVkbTableDecoder(IntPtr decoder, ref DecoderOutParams table);

        /// <summary> Decoder へ入力DECKEYキーを送信する </summary>
        /// <param name="decoder"></param>
        [DllImport("kw-uni.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void HandleDeckeyDecoder(IntPtr decoder, int keyId, uint targetChar, int inputFlags, ref DecoderOutParams outParams);

        /// <summary> Decoder へ各種データを送信する </summary>
        [DllImport("kw-uni.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int ExecCmdDecoder(IntPtr decoder, IntPtr decParam, ref DecoderOutParams outParams);

        //------------------------------------------------------------------
        /// <summary> デコーダのハンドル(ポインタ) </summary>
        private IntPtr decoderPtr;

        /// <summary> デコーダからの出力情報を保持する構造体インスタンス </summary>
        DecoderOutParams decoderOutput;

        public DecoderOutParams DecoderOutput => decoderOutput;

        /// <summary>仮想鍵盤を移動させないか(仮想鍵盤自身がアクティブになっているなど)</summary>
        public bool IsVirtualKeyboardFreezed => decoderOutput.IsVirtualKeyboardFreezed();

        private void getCenterString()
        {
            int len = decoderOutput.centerString._strlen();
            string tail = "";
            if (len > 11) {
                len = 10;
                tail = "…";
            }
            CommonState.CenterString = new string(decoderOutput.centerString, 0, len) + tail;
            logger.Info(() => $"center={CommonState.CenterString}, mode.Face={frmMode.FaceString}");

            // TODO: 外出しする
            if (decoderOutput.IsZenkakuModeMarkerShow()) {
                frmMode.SetZenkakuMode();
            } else if (decoderOutput.IsZenkakuModeMarkerClear()) {
                frmMode.SetKanjiMode();
            } else if (Settings.EffectiveKanjiModeMarkerShowIntervalSec == 0) {
                // モード標識を常時表示なら、2ストローク待ちか否かを通知する
                frmMode.SetWait2ndStrokeMode(decoderOutput.IsWaiting2ndStroke());
            }
        }

        //------------------------------------------------------------------
        private const int IN_OUT_DATA_SIZE = 2048;

        /// <summary> Decoder コマンド呼び出し用の構造体 </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DecoderCommandParams
        {
            // 種々の受け渡しデータ
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = IN_OUT_DATA_SIZE)]
            public char[] inOutData;
        }

        /// <summary> デコーダにコマンドを送信する(エラーなら false を返す)</summary> 
        public bool ExecCmdDecoder(string cmd, string data, bool bInit = false)
        {
            logger.InfoH(() => $"ENTER: cmd={cmd}, bInit={bInit}, dataLen={data._safeLength()}, data={data}");
            bool resultFlag = true;
            if (decoderPtr != IntPtr.Zero) {
                var prm = new DecoderCommandParams() {
                    //hWnd = this.Handle,
                    inOutData = new char[IN_OUT_DATA_SIZE],
                };
                if (!bInit) {
                    data = data._notEmpty() ? $"{cmd}\t{data}" : cmd;
                }
                Array.Copy(data._toCharArray(), prm.inOutData, data.Length._highLimit(prm.inOutData.Length - 1));
                // アンマネージド構造体のメモリ確保
                int size = Marshal.SizeOf(typeof(DecoderCommandParams));
                System.IntPtr cmdParamsPtr = Marshal.AllocCoTaskMem(size);
                // マネージド構造体をアンマネージドにコピーする
                Marshal.StructureToPtr(prm, cmdParamsPtr, false);
                int result = 0;
                if (bInit) {
                    // 初期化呼び出し
                    result = InitializeDecoder(decoderPtr, cmdParamsPtr);
                } else {
                    result = ExecCmdDecoder(decoderPtr, cmdParamsPtr, ref decoderOutput);
                }
                prm = (DecoderCommandParams)Marshal.PtrToStructure(cmdParamsPtr, prm.GetType());

                // アンマネージドのメモリを解放
                Marshal.FreeCoTaskMem(cmdParamsPtr);

                if (result >= 1) {
                    if (frmSplash != null) {
                        //frmSplash._invoke(() => frmSplash.Fallback());
                        this._invoke(() => this.closeSplash());
                    }
                    var errMsg = prm.inOutData._toString();
                    if (result == 1) {
                        logger.Warn(errMsg);
                        SystemHelper.ShowWarningMessageBox(errMsg);
                    } else {
                        logger.Error(errMsg);
                        SystemHelper.ShowErrorMessageBox(errMsg);
                        resultFlag = false;
                    }
                } else if (result < 0) {
                    var errMsg = "Some error occured when Decoder called";
                    logger.Warn(errMsg);
                    SystemHelper.ShowWarningMessageBox(errMsg);
                }
            }
            logger.Info(() => $"LEAVE: resultFlag={resultFlag}");
            return resultFlag;
        }

        /// <summary>デコーダの関数を呼び出す。正常に終了したら、戻値である faceStringsを返す </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public char[] CallDecoderFunc(string func, string param = null)
        {
            if (ExecCmdDecoder(func, param))
                return decoderOutput.faceStrings;
            else
                return null;
        }

        //------------------------------------------------------------------
        [DllImport("user32.dll")]
        private static extern ushort GetAsyncKeyState(uint vkey);

        //------------------------------------------------------------------
        public int DeckeyTotalCount { get; private set; } = 0;

        public bool IsWaiting2ndStroke => decoderOutput.IsWaiting2ndStroke();

        private int prevRawDeckey = -1;
        private int rawDeckeyRepeatCount = 0;

        private int prevDeckey = -1;
        private DateTime prevDecDt;

        //private int prevDateDeckeyCount = 0;
        private int dateStrDeckeyCount = 0;
        private int dayOffset = 0;
        private int prevDateStrLength = 0;

        /// <summary> Decoder の ON/OFF 状態 </summary>
        public bool IsDecoderActive { get; private set; } = false;

        private bool isDecoderActivated()
        {
            return IsDecoderActive;
        }

        public bool IsDecoderWaitingFirstStroke()
        {
            return decoderOutput.IsWaitingFirstStroke();
        }

        public bool IsVkbShown => Settings.VirtualKeyboardShowStrokeCountEffective > 0 && Settings.VirtualKeyboardShowStrokeCountEffective <= decoderOutput.GetStrokeCount() + 1;

        /// <summary>
        /// KeyboardHookから呼び出されるキーボードダウンイベントハンドラ<br/>
        /// キーを処理した場合は true を返す。システムにキーの処理をさせる場合は false を返す。
        /// </summary>
        /// <param name="vkey"></param>
        /// <param name="extraInfo"></param>
        /// <returns></returns>
        public bool OnKeyboardDownHandler(int vkey, int extraInfo)
        {
            logger.DebugH(() => $"CALLED: vkey={vkey}, extraInfo={extraInfo}");
            return IsDecoderActive;
        }

        /// <summary>
        /// KeyboardHookから呼び出されるキーボードアップイベントハンドラ<br/>
        /// キーを処理した場合は true を返す。システムにキーの処理をさせる場合は false を返す。
        /// </summary>
        /// <param name="vkey"></param>
        /// <param name="extraInfo"></param>
        /// <returns></returns>
        public bool OnKeyboardUpHandler(int vkey, int extraInfo)
        {
            logger.DebugH(() => $"CALLED: vkey={vkey}, extraInfo={extraInfo}");
            return IsDecoderActive;
        }

        private void initializeKeyboardEventDispatcher()
        {
            logger.InfoH("ENTER");

            keDispatcher.SetInvokeHandlerToDeterminer();

            keDispatcher.OnKeyDown = OnKeyboardDownHandler;
            keDispatcher.OnKeyUp = OnKeyboardUpHandler;
            keDispatcher.ToggleDecoder = ToggleDecoder;
            keDispatcher.ActivateDecoder = ActivateDecoder;
            keDispatcher.DeactivateDecoder = DeactivateDecoderWithModifiersOff;
            keDispatcher.IsDecoderActivated = isDecoderActivated;
            keDispatcher.IsDecoderWaitingFirstStroke = IsDecoderWaitingFirstStroke;
            keDispatcher.SetSandSShiftedOneshot = setSandSShiftedOneshot;
            keDispatcher.FuncDispatcher = FuncDispatcher;
            keDispatcher.SendInputVkeyWithMod = SendInputVkeyWithMod;
            keDispatcher.InvokeDecoderUnconditionally = InvokeDecoderUnconditionally;
            keDispatcher.SetStrokeHelpShiftPlane = SetStrokeHelpShiftPlane;
            keDispatcher.SetNextStrokeHelpDecKey = SetNextStrokeHelpDecKey;
            keDispatcher.WriteStrokeLog = WriteStrokeLog;
            //keDispatcher.RotateReverseStrokeHelp = rotateReverseStrokeHelp;
            //keDispatcher.RotateDateString = rotateDateString;
            //keDispatcher.RotateReverseDateString = rotateReverseDateString;
            //keDispatcher.InvokeDecoder = InvokeDecoder;

            // キーボードイベントのディスパッチ開始
            keDispatcher.InstallKeyboardHook();
            logger.InfoH("LEAVE");
        }

        /// <summary>無条件にデコーダを呼び出す</summary>
        private bool InvokeDecoderUnconditionally(int deckey, uint mod)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"CALLED: deckey={deckey:x}H({deckey}), mod={mod}H({mod})");
            if (IsDecoderActive)
                handleKeyDecoder(deckey, mod);
            else
                handleKeyDecoderDirectly(deckey, mod);
            return true;
        }

        /// <summary>ストロークヘルプを表示するシフト面を設定する</summary>
        /// <param name="shiftPlane"></param>
        private void SetStrokeHelpShiftPlane(int shiftPlane)
        {
            logger.InfoH(() => $"CALLED: shiftPlane={shiftPlane}, IsDecoderActive={IsDecoderActive}");
            if (IsDecoderActive) {
                if (shiftPlane >= 0 && shiftPlane < ShiftPlane.ShiftPlane_NUM) {
                    frmVkb.StrokeHelpShiftPlane = shiftPlane;
                    if (frmVkb.IsCurrentNormalVkb) frmVkb.DrawInitialVkb();
                }
            }
        }

        /// <summary>
        /// 指定キーに対する次打鍵テーブルの作成
        /// </summary>
        /// <param name="decKey"></param>
        private void SetNextStrokeHelpDecKey(List<int> decKeys)
        {
            logger.InfoH(() => $"CALLED: decKeys={decKeys._keyString("empty")}");
            frmVkb.DecKeysForNextTableStrokeHelp.Clear();
            if (decKeys._notEmpty()) {
                foreach (var dc in decKeys) {
                    if (dc < DecoderKeys.EISU_COMBO_DECKEY_END) {
                        frmVkb.DecKeysForNextTableStrokeHelp.Add(dc);
                    }
                }
            }
            frmVkb.DrawInitialVkb();
        }

        private int prevFuncDeckey = 0;
        private int prevFuncTotalCount = 0;

        /// <summary>
        /// UI側のハンドラー
        /// </summary>
        /// <param name="deckey"></param>
        /// <returns></returns>
        private bool FuncDispatcher(int deckey, int normalDeckey, uint mod)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"CALLED: deckey={deckey:x}H({deckey}), normalDeckey={normalDeckey:x}H({normalDeckey}), mod={mod:x}({mod})");
            bool bPrevDtUpdate = false;
            int prevDeckey = prevFuncDeckey;
            prevFuncDeckey = deckey;
            int prevCount = prevFuncTotalCount;
            prevFuncTotalCount = DeckeyTotalCount;
            try {
                if (deckey == DecoderKeys.DATE_STRING_ROTATION_DECKEY) {
                    return !isActiveWinExcel() && rotateDateString(1);
                } else if (deckey == DecoderKeys.DATE_STRING_UNROTATION_DECKEY) {
                    return !isActiveWinExcel() && rotateDateString(-1);
                } else if (IsDecoderActive) {
                    renewSaveDictsPlannedDt();
                    switch (deckey) {
                        case DecoderKeys.VKB_SHOW_HIDE_DECKEY:
                            if (IsVkbHiddenTemporay) {
                                IsVkbHiddenTemporay = false;
                                ShowFrmVkb();
                            } else {
                                IsVkbHiddenTemporay = true;
                                frmVkb.Hide();
                            }
                            return true;
                        case DecoderKeys.STROKE_HELP_ROTATION_DECKEY:
                            return rotateStrokeHelp(1);
                        case DecoderKeys.STROKE_HELP_UNROTATION_DECKEY:
                            return rotateStrokeHelp(-1);
                        case DecoderKeys.DATE_STRING_ROTATION_DECKEY:
                            return !isActiveWinExcel() && rotateDateString(1);
                        case DecoderKeys.DATE_STRING_UNROTATION_DECKEY:
                            return !isActiveWinExcel() && rotateDateString(-1);
                        case DecoderKeys.STROKE_HELP_DECKEY:
                            if (prevDeckey != deckey || prevCount + 1 < DeckeyTotalCount) {
                                ShowStrokeHelp(null);
                            } else {
                                ShowBushuCompHelp();
                            }
                            return true;
                        case DecoderKeys.BUSHU_COMP_HELP_DECKEY:
                            ShowBushuCompHelp();
                            return true;
                        case DecoderKeys.TOGGLE_ROMAN_STROKE_GUIDE_DECKEY:
                            if (IsDecoderActive) {
                                rotateStrokeHelp(0);
                                bRomanStrokeGuideMode = !bRomanStrokeGuideMode && !bRomanMode;
                                drawRomanOrHiraganaMode(bRomanStrokeGuideMode, false);
                            }
                            return true;
                        case DecoderKeys.TOGGLE_UPPER_ROMAN_STROKE_GUIDE_DECKEY:
                            if (IsDecoderActive) {
                                rotateStrokeHelp(0);
                                bUpperRomanStrokeGuideMode = !bUpperRomanStrokeGuideMode && !bRomanMode;
                                if (!bRomanMode) drawRomanOrHiraganaMode(bUpperRomanStrokeGuideMode, false);
                            }
                            return true;
                        case DecoderKeys.TOGGLE_HIRAGANA_STROKE_GUIDE_DECKEY:
                            if (IsDecoderActive) {
                                rotateStrokeHelp(0);
                                bHiraganaStrokeGuideMode = !bHiraganaStrokeGuideMode;
                                if (bHiraganaStrokeGuideMode) {
                                    InvokeDecoder(DecoderKeys.FULL_ESCAPE_DECKEY, 0);   // やっぱり出力文字列をクリアしておく必要あり
                                                                                        //ExecCmdDecoder("setHiraganaBlocker", null);       // こっちだと、以前のひらがなが出力文字列に残ったりして、それを拾ってしまう
                                } else {
                                    //HandleDeckeyDecoder(decoderPtr, DecoderKeys.FULL_ESCAPE_DECKEY, 0, 0, ref decoderOutput); // こっちだと、見えなくなるだけで、ひらがな列が残ってしまう
                                    ExecCmdDecoder("clearTailHiraganaStr", null);   // 物理的に読みのひらがな列を削除しておく必要あり
                                }
                                drawRomanOrHiraganaMode(false, bHiraganaStrokeGuideMode);
                            }
                            return true;
                        case DecoderKeys.EXCHANGE_CODE_TABLE_DECKEY:
                            logger.InfoH("EXCHANGE_CODE_TABLE");
                            ExchangeCodeTable();
                            return true;
                        case DecoderKeys.EXCHANGE_CODE_TABLE2_DECKEY:
                            logger.InfoH("EXCHANGE_CODE_TABLE2");
                            ExchangeCodeTable(true);
                            return true;
                        case DecoderKeys.SELECT_CODE_TABLE1_DECKEY:
                            logger.InfoH("SELECT_CODE_TABLE1_DECKEY");
                            SelectCodeTable(1, false);
                            return true;
                        case DecoderKeys.SELECT_CODE_TABLE2_DECKEY:
                            logger.InfoH("SELECT_CODE_TABLE2_DECKEY");
                            SelectCodeTable(2, false);
                            return true;
                        case DecoderKeys.SELECT_CODE_TABLE3_DECKEY:
                            logger.InfoH("SELECT_CODE_TABLE3_DECKEY");
                            SelectCodeTable(3, false);
                            return true;
                        case DecoderKeys.TOGGLE_KATAKANA_CONVERSION1_DECKEY:
                            logger.InfoH("TOGGLE_KATAKANA_CONVERSION1_DECKEY");
                            SelectCodeTable(1, true);
                            return true;
                        case DecoderKeys.TOGGLE_KATAKANA_CONVERSION2_DECKEY:
                            logger.InfoH("TOGGLE_KATAKANA_CONVERSION2_DECKEY");
                            SelectCodeTable(2, true);
                            return true;
                        case DecoderKeys.KANA_TRAINING_TOGGLE_DECKEY:
                            logger.InfoH("KANA_TRAINING_TOGGLE");
                            KanaTrainingModeToggle();
                            return true;
                        case DecoderKeys.PSEUDO_SPACE_DECKEY:
                            logger.InfoH(() => $"PSEUDO_SPACE_DECKEY: strokeCount={decoderOutput.GetStrokeCount()}");
                            deckey = DecoderKeys.STROKE_SPACE_DECKEY;
                            if (IsDecoderActive && decoderOutput.GetStrokeCount() >= 1) {
                                // 第2打鍵待ちなら、スペースを出力
                                InvokeDecoder(deckey, 0);
                            }
                            return true;
                        case DecoderKeys.POST_NORMAL_SHIFT_DECKEY:
                        case DecoderKeys.POST_PLANE_A_SHIFT_DECKEY:
                        case DecoderKeys.POST_PLANE_B_SHIFT_DECKEY:
                        case DecoderKeys.POST_PLANE_C_SHIFT_DECKEY:
                        case DecoderKeys.POST_PLANE_D_SHIFT_DECKEY:
                        case DecoderKeys.POST_PLANE_E_SHIFT_DECKEY:
                        case DecoderKeys.POST_PLANE_F_SHIFT_DECKEY:
                            logger.InfoH(() => $"POST_PLANE_X_SHIFT_DECKEY=POST_NORMAL_SHIFT_DECKEY+{deckey - DecoderKeys.POST_NORMAL_SHIFT_DECKEY}, strokeCount={decoderOutput.GetStrokeCount()}");
                            if (IsDecoderActive && decoderOutput.GetStrokeCount() >= 1) {
                                // 第2打鍵待ちなら、いったんBSを出力してからシフトされたコードを出力
                                InvokeDecoder(DecoderKeys.BS_DECKEY, 0);
                                deckey = (prevDeckey % DecoderKeys.PLANE_DECKEY_NUM) + (deckey - DecoderKeys.POST_NORMAL_SHIFT_DECKEY + 1) * DecoderKeys.PLANE_DECKEY_NUM;
                                InvokeDecoder(deckey, 0);
                            }
                            return true;
                        case DecoderKeys.COPY_SELECTION_AND_SEND_TO_DICTIONARY_DECKEY:
                            logger.InfoH(() => $"COPY_SELECTION_AND_SEND_TO_DICTIONARY:{deckey}");
                            copySelectionAndSendToDictionary();
                            return true;
                        case DecoderKeys.CLEAR_STROKE_DECKEY:
                            logger.InfoH(() => $"CLEAR_STROKE_DECKEY:{deckey}");
                            sendClearStrokeToDecoder();
                            return true;
                        case DecoderKeys.TOGGLE_BLOCKER_DECKEY:
                            logger.InfoH(() => $"TOGGLE_BLOCKER_DECKEY:{deckey}");
                            sendDeckeyToDecoder(deckey);
                            return true;

                        case DecoderKeys.DIRECT_SPACE_DECKEY:
                            logger.InfoH(() => $"DIRECT_SPACE_DECKEY:{deckey}, mode={mod:x}H");
                            return sendVkeyFromDeckey(DecoderKeys.STROKE_SPACE_DECKEY, mod);

                        case DecoderKeys.UNDEFINED_DECKEY:
                            // 主に英数モードから抜けるために使う
                            logger.InfoH(() => $"UNDEFINED_DECKEY:{deckey}");
                            sendDeckeyToDecoder(deckey);
                            if (normalDeckey >= 0 && normalDeckey < DecoderKeys.NORMAL_DECKEY_NUM) {
                                return sendVkeyFromDeckey(normalDeckey, mod);
                            } else {
                                return false;
                            }

                        default:
                            bPrevDtUpdate = true;
                            if (IsDecoderActive && (deckey < DecoderKeys.DECKEY_CTRL_A || deckey > DecoderKeys.DECKEY_CTRL_Z)) {
                                return InvokeDecoder(deckey, mod);
                            } else {
                                return sendVkeyFromDeckey(deckey, mod);
                            }
                    }
                } else {
                    // Decoder Inactive
                    if (Settings.LoggingDecKeyInfo) logger.InfoH("Decoder Inactive");
                    bPrevDtUpdate = true;
                    if (deckey >= 0 && deckey != DecoderKeys.UNDEFINED_DECKEY) {
                        return sendVkeyFromDeckey(deckey, mod);
                    } else if (normalDeckey >= 0) {
                        return sendVkeyFromDeckey(normalDeckey, mod);
                    }
                    return false;
                }
            } finally {
                prevDeckey = deckey;
                if (bPrevDtUpdate) prevDecDt = DateTime.Now;
            }
        }

        /// <summary>漢直コードテーブルの入れ替え</summary>
        public void ExchangeCodeTable(bool bSecond = false)
        {
            logger.InfoH("CALLED");
            if (IsDecoderActive && Settings.TableFile2._notEmpty() /*&& DecoderOutput.IsWaitingFirstStroke()*/) {
                ExecCmdDecoder("isKatakanaMode", null);  // カタカナモードか
                bool isKatakana = (decoderOutput.resultFlags & ResultFlags.CurrentModeIsKatakana) != 0;
                logger.InfoH(() => $"isKatakana={isKatakana}, resultFlags={decoderOutput.resultFlags:x}");
                ExecCmdDecoder("commitHistory", null);                  // 履歴のコミットと初期化
                //InvokeDecoder(DecoderKeys.FULL_ESCAPE_DECKEY, 0);
                //InvokeDecoder(DecoderKeys.SOFT_ESCAPE_DECKEY, 0);
                InvokeDecoder(DecoderKeys.COMMIT_STATE_DECKEY, 0);      // これで各種モードがクリアされる
                InvokeDecoder(DecoderKeys.COMMIT_STATE_DECKEY, 0);      // 念のため2回呼ぶ
                if (!isKatakana) {
                    // カタカナモードでなければ、テーブルの入れ替えを行う
                    if (bSecond && decoderOutput.strokeTableNum == 3) {
                        changeCodeTableAndCombinationPool("useCodeTable2");     // コードテーブル2に入れ替え
                    } else {
                        changeCodeTableAndCombinationPool("exchangeCodeTable");     // 漢直コードテーブルの入れ替え
                    }
                    frmVkb.DrawVirtualKeyboardChars();
                    frmMode.SetKanjiMode();
                }
            }
        }

        /// <summary>漢直コードテーブルの選択</summary>
        public void SelectCodeTable(int n, bool toggleKatakana)
        {
            logger.InfoH($"CALLED: n={n}");
            if (IsDecoderActive && (Settings.TableFile2._notEmpty() || Settings.TableFile3._notEmpty()) /*&& DecoderOutput.IsWaitingFirstStroke()*/) {
                //ExecCmdDecoder("isKatakanaMode", null);  // カタカナモードか
                //bool isKatakana = (decoderOutput.resultFlags & ResultFlags.CurrentModeIsKatakana) != 0;
                ExecCmdDecoder("commitHistory", null);                  // 履歴のコミットと初期化
                //InvokeDecoder(DecoderKeys.SOFT_ESCAPE_DECKEY, 0);       // これで各種モードがクリアされる
                //InvokeDecoder(DecoderKeys.SOFT_ESCAPE_DECKEY, 0);       // 念のため2回呼ぶ
                InvokeDecoder(DecoderKeys.COMMIT_STATE_DECKEY, 0);      // これで各種モードがクリアされる
                InvokeDecoder(DecoderKeys.COMMIT_STATE_DECKEY, 0);      // 念のため2回呼ぶ
                //if (toggleKatakana && !isKatakana) InvokeDecoder(DecoderKeys.TOGGLE_KATAKANA_CONVERSION_DECKEY, 0);
                if (n == 1 && Settings.TableFile._notEmpty()) {
                    changeCodeTableAndCombinationPool("useCodeTable1");     // コードテーブル1に入れ替え
                } else if (n == 2 && Settings.TableFile2._notEmpty()) {
                    changeCodeTableAndCombinationPool("useCodeTable2");     // コードテーブル2に入れ替え
                } else if (n == 3 && Settings.TableFile3._notEmpty()) {
                    changeCodeTableAndCombinationPool("useCodeTable3");     // コードテーブル3に入れ替え
                }
                frmVkb.DrawVirtualKeyboardChars();
                frmMode.SetKanjiMode();
            }
        }

        private void changeCodeTableAndCombinationPool(string cmd)
        {
            ExecCmdDecoder(cmd, null);  // コードテーブルの切り替え
            CombinationKeyStroke.DeterminerLib.KeyCombinationPool.ChangeCurrentPoolBySelectedTable(decoderOutput.strokeTableNum, true);  // KeyCombinationPoolの入れ替え
        }

        /// <summary>SandS状態が一時的なシフト状態か</summary>
        public bool IsSandSShiftedOneshot { get; private set; } = false;

        /// <summary>SandS状態を一時的なシフト状態にする</summary>
        private void setSandSShiftedOneshot()
        {
            logger.InfoH("CALLED");
            if (IsDecoderActive) {
                // 中央鍵盤色を、第2テーブル選択状態の色にする
                IsSandSShiftedOneshot = true;
                frmVkb.DrawVirtualKeyboardChars();
                IsSandSShiftedOneshot = false;
            }
        }

        private bool rotateStrokeHelp()
        {
            return rotateStrokeHelp(1);
        }

        private bool rotateReverseStrokeHelp()
        {
            return rotateStrokeHelp(-1);
        }

        private bool rotateStrokeHelp(int direction)
        {
            logger.InfoH(() => $"CALLED: IsDecoderActive={IsDecoderActive}");
            if (IsDecoderActive) {
                // 入力標識の消去
                frmMode.Vanish();
                // 仮想鍵盤のヘルプ表示の切り替え(モード標識表示時なら一時的に仮想鍵盤表示)
                int effectiveCnt = Settings.VirtualKeyboardShowStrokeCountEffective;
                Settings.VirtualKeyboardShowStrokeCountTemp = 1;
                frmVkb.RotateStrokeTable(effectiveCnt != 1 ? 0 : direction);
                return true;
            }
            return false;
        }

        private bool isCtrlKeyConversionEffectiveWindow()
        {
            string activeWinClassName = ActiveWindowHandler.Singleton.ActiveWinClassName._toLower();
            bool contained = activeWinClassName._notEmpty()
                && Settings.CtrlKeyTargetClassNamesHash._notEmpty()
                && Settings.CtrlKeyTargetClassNamesHash.Any(name => name._notEmpty() && activeWinClassName.StartsWith(name));
            bool ctrlTarget = !(Settings.UseClassNameListAsInclusion ^ contained);
            if (Settings.LoggingDecKeyInfo && Logger.IsInfoEnabled) {
                logger.InfoH($"ctrlTarget={ctrlTarget} (=!({Settings.UseClassNameListAsInclusion} (Inclusion) XOR {contained} (ContainedInList)");
            }
            return ctrlTarget;
        }

        private bool isActiveWinExcel()
        {
            return ActiveWindowHandler.Singleton.ActiveWinClassName._startsWith("EXCEL");
        }

        private bool rotateDateString(int direction)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"CALLED: direction={direction}");
            bool bFirst = (prevDeckey != DecoderKeys.DATE_STRING_ROTATION_DECKEY && prevDeckey != DecoderKeys.DATE_STRING_UNROTATION_DECKEY);
            if (bFirst) {
                if (Settings.LoggingDecKeyInfo) logger.InfoH($"bFirst={bFirst}");
                dateStrDeckeyCount = 0;     // 0 は初期状態
                prevDateStrLength = 0;
                dayOffset = 0;
            }
            dateStrDeckeyCount += direction;
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"LEAVE: new deckey={DecoderKeys.DATE_STRING_ROTATION_DECKEY:x}, dateStrDeckeyCount={dateStrDeckeyCount}, prevDateStrLength={prevDateStrLength}, dayOffset={dayOffset}");
            outputTodayDate();
            return true;
        }

        private void copySelectionAndSendToDictionary()
        {
            try {
                // Ctrl-C を送る
                SendInputHandler.Singleton.SendVKeyCombo(VKeyComboRepository.CtrlC_VKeyCombo.modifier, VKeyComboRepository.CtrlC_VKeyCombo.vkey, 1);
                Helper.WaitMilliSeconds(100);
                if (Clipboard.ContainsText()) {
                    //文字列データがあるときはこれを取得する
                    //取得できないときは空の文字列（String.Empty）を返す
                    SendToDictionary(Clipboard.GetText());
                }
            } catch (Exception e) {
                logger.Error(e._getErrorMsg());
            }
        }

        /// <summary>文字列をデコーダの辞書に送って登録する </summary>
        /// <param name="str"></param>
        public void SendToDictionary(string str)
        {
            str = str._strip()._reReplace("  +", " ");
            if (str._notEmpty()) {
                if (str.Length == 1 || (str.Length == 2 && str._isSurrogatePair())) {
                    ShowStrokeHelp(str);
                } else if (str.Length == 4 && str[2] == '=') {
                    ExecCmdDecoder("addAutoBushuEntry", str._safeSubstring(3,1) + str._safeSubstring(0,2));
                } else if (str[1] == '=') {
                    ExecCmdDecoder("mergeBushuAssocEntry", str);
                } else if (str._reMatch("^[^ ]+ /")) {
                    ExecCmdDecoder("addMazegakiEntry", str);
                } else {
                    //ExecCmdDecoder("addHistEntry", str._safeSubstring(0, Settings.HistMaxLength));
                    AddHistEntry(str);
                }
            }
        }

        /// <summary>文字列を履歴登録する(ただし、20文字以下で空白を含まない。変換形登録なら、変換部は32文字以下)</summary>
        /// <param name="str"></param>
        public void AddHistEntry(string str)
        {
            var items = str._split2('|');
            int len1 = items._getFirst()._safeLength();
            int len2 = items._getSecond()._safeLength();
            const int keyMaxLen = 32;
            const int xferMaxLen = 64;
            logger.InfoH(() => $"len1={len1}, len2={len2}, items[0]={items._getFirst()._safeSubstring(0, keyMaxLen)}, items[1]={items._getSecond()._safeSubstring(0, 64)}");
            if (len1 > 0 && len1 <= keyMaxLen && len2 <= 64 && items._getFirst()._safeIndexOf(' ') < 0) {
                ExecCmdDecoder("addHistEntry", str);
            } else {
                logger.Warn($"key length({len1}) is greater than {keyMaxLen} or xfer length ({len2}) is greater than {xferMaxLen}: {str}");
            }
        }

        // 開発者用の設定がONになっているとき、漢直モードのON/OFFを10回繰り返したら警告を出す
        private int devFlagsOnWarningCount = 0;

        private void ToggleDecoder()
        {
            ToggleActiveState(true);
        }

        // アクティブと非アクティブを切り替える
        public void ToggleActiveState(bool bRevertCtrl = false)
        {
            logger.InfoH(() => $"ENTER");
            if (!IsDecoderActive) {
                ActivateDecoder();
            } else {
                var keyState = SendInputHandler.GetCtrlKeyState(true);
                DeactivateDecoder(!bRevertCtrl);
                if (bRevertCtrl) SendInputHandler.Singleton.RevertCtrlKey(keyState);
            }
            logger.InfoH("LEAVE");
        }

        private void ActivateDecoder()
        {
            logger.InfoH(() => $"\nENTER");
            if (IsDecoderActive) {
                logger.InfoH("Decoder already activated");
                ExecCmdDecoder("commitHistory", null);                  // 履歴のコミットと初期化
                //InvokeDecoder(DecoderKeys.SOFT_ESCAPE_DECKEY, 0);       // これで各種モードがクリアされる
                //InvokeDecoder(DecoderKeys.SOFT_ESCAPE_DECKEY, 0);       // 念のため2回呼ぶ
                InvokeDecoder(DecoderKeys.COMMIT_STATE_DECKEY, 0);      // これで各種モードがクリアされる
                InvokeDecoder(DecoderKeys.COMMIT_STATE_DECKEY, 0);      // 念のため2回呼ぶ
                frmVkb.DrawVirtualKeyboardChars();
                logger.InfoH("LEAVE");
                return;
            }
            IsDecoderActive = true;
            var ctrlKeyState = SendInputHandler.GetCtrlKeyState();
            if (Settings.SelectedTableActivatedWithCtrl > 0 && Settings.SelectedTableActivatedWithCtrl <= 2 && ctrlKeyState.AnyKeyDown) {
                changeCodeTableAndCombinationPool($"useCodeTable{Settings.SelectedTableActivatedWithCtrl}");     // 指定のコードテーブルを選択
            } else {
                if (decoderOutput.strokeTableNum == 3) {
                    changeCodeTableAndCombinationPool("useCodeTable1");     // コードテーブル1に入れ替え
                } else {
                    CombinationKeyStroke.DeterminerLib.KeyCombinationPool.ChangeCurrentPoolByDecoderMode(IsDecoderActive);  // 前回の漢直用Poolに切り替え
                }
            }
            try {
                prevDeckey = -1;
                if (frmSplash != null) closeSplash();
                if (decoderPtr != IntPtr.Zero) ResetDecoder(decoderPtr);
                CombinationKeyStroke.Determiner.Singleton.Clear();     // 同時打鍵キューのクリア
                decoderOutput.layout = 0;   // None にリセットしておく。これをやらないと仮想鍵盤モードを切り替えたときに以前の履歴選択状態が残ったりする
                CommonState.CenterString = "";
                Settings.VirtualKeyboardShowStrokeCountTemp = 0;
                bHiraganaStrokeGuideMode = false;
                bRomanStrokeGuideMode = false;
                IsVkbHiddenTemporay = false;
                frmVkb.StrokeHelpShiftPlane = 0;
                frmVkb.DecKeysForNextTableStrokeHelp.Clear();
                frmVkb.DrawInitialVkb();
                //Text = "漢直窓S - ON";
                notifyIcon1.Icon = Properties.Resources.kanmini1;
                // 仮想鍵盤を移動させる
                MoveFormVirtualKeyboard();
                Hide();
                frmMode.Hide();
                // ウィンドウが移動する時間をかせいでから画面を表示する ⇒ これは不要か？とりあえず待ち時間を短くしておく(50ms⇒20ms)(2021/8/2)
                Helper.WaitMilliSeconds(20);
                frmMode.SetKanjiMode();
                if (Settings.VirtualKeyboardShowStrokeCount == 1) {
                    sendClearStrokeToDecoder(); // デコーダを第1打鍵待ちに戻しておく
                    frmVkb.SetTopText(ActiveWindowHandler.Singleton.ActiveWinClassName);
                    ShowFrmVkb();       // Show NonActive
                }
                if (Settings.IsAnyDevFlagEnabled) {
                    if (devFlagsOnWarningCount <= 0) {
                        devFlagsOnWarningCount = 2;
                    } else {
                        --devFlagsOnWarningCount;
                        if (devFlagsOnWarningCount == 0) {
                            string msg = "";
                            if (Logger.LogLevel > 2) {
                                msg = "ログレベルを確認してください。";
                            } else {
                                msg = "開発者用の設定が有効になっています。";
                            }
                            //SystemHelper.ShowWarningMessageBox(msg);
                            frmVkb.SetTopText(msg);
                        }
                    }
                }
            } finally {
            }
            logger.InfoH("LEAVE");
        }

        // デコーダをOFFにする
        public void DeactivateDecoder(bool bModifiersOff = true)
        {
            logger.InfoH(() => $"\nENTER");
            IsDecoderActive = false;
            CombinationKeyStroke.DeterminerLib.KeyCombinationPool.ChangeCurrentPoolByDecoderMode(IsDecoderActive);
            if (decoderPtr != IntPtr.Zero) {
                handleKeyDecoder(DecoderKeys.DEACTIVE_DECKEY, 0);   // DecoderOff の処理をやる
                if (bModifiersOff) SendInputHandler.Singleton.UpCtrlAndShftKeys();                  // CtrlとShiftキーをUP状態に戻す
                frmVkb.Hide();
                frmMode.Hide();
                notifyIcon1.Icon = Properties.Resources.kanmini0;
                frmMode.SetKanjiMode();
                if (Settings.VirtualKeyboardShowStrokeCount != 1) {
                    frmMode.SetAlphaMode();
                }
            }
            logger.InfoH("LEAVE");
        }

        // デコーダをOFFにする
        public void DeactivateDecoderWithModifiersOff()
        {
            DeactivateDecoder(true);
        }

        /// <summary>仮想鍵盤の表示位置を移動する</summary>
        public void MoveFormVirtualKeyboard()
        {
            logger.Info("CALLED");
            moveVkbWindow(false, true, true);
        }

        /// <summary> ウィンドウを移動さ出ない微少変動量 </summary>
        private const int NO_MOVE_OFFSET = 10;

        /// <summary> 仮想鍵盤ウィンドウの ClassName の末尾のハッシュ部分 </summary>
        private string dlgVkbClassNameHash;

        private bool bFirstMove = true;

        private Rectangle prevCaretPos;

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        /// <summary>
        /// 仮想鍵盤(またはモード標識)をカレットの近くに移動する (仮想鍵盤自身がアクティブの場合は移動しない)<br/>
        /// これが呼ばれるのはデコーダがONのときだけ
        /// </summary>
        private void moveVkbWindow(bool bDiffWin, bool bMoveMandatory, bool bLog)
        {
            if (ActiveWindowHandler.Singleton == null) return;  // まだ Singleton が生成される前に呼び出される可能性あり

            var activeWinClassName = ActiveWindowHandler.Singleton.ActiveWinClassName;
            var activeWinSettings = Settings.GetWinClassSettings(activeWinClassName);
            if (bLog || bFirstMove) {
                logger.Info($"CALLED: diffWin={bDiffWin}, mandatory={bMoveMandatory}, firstMove={bFirstMove}");
                ActiveWindowHandler.Singleton.LoggingCaretInfo(activeWinSettings);
            }

            if (Settings.VirtualKeyboardPosFixedTemporarily) return;    // 一時的に固定されている

            if (dlgVkbClassNameHash._isEmpty()) {
                dlgVkbClassNameHash = ActiveWindowHandler.GetWindowClassName(frmVkb.Handle)._safeSubstring(-16);
                logger.Info(() => $"Vkb ClassName Hash={dlgVkbClassNameHash}");
            }

            var activeWinCaretPos = ActiveWindowHandler.Singleton.ActiveWinCaretPos;
            bool bFixedPosWinClass = Settings.IsFixedPosWinClass(activeWinClassName);

            bool isValidCaretShape()
            {
                bool result = activeWinCaretPos.Width > 0 || activeWinCaretPos.Height > 0;
                if (bLog && !result) logger.Info("INVALID caret shape");
                return result;
            }

            bool isValidCaretPos()
            {
                return
                    (Math.Abs(activeWinCaretPos.X) >= NO_MOVE_OFFSET || Math.Abs(activeWinCaretPos.Y) >= NO_MOVE_OFFSET) &&
                    (Math.Abs(activeWinCaretPos.X - prevCaretPos.X) >= NO_MOVE_OFFSET || Math.Abs(activeWinCaretPos.Y - prevCaretPos.Y) >= NO_MOVE_OFFSET);
            }

            if (bFirstMove || (!this.IsVirtualKeyboardFreezed && !activeWinClassName.EndsWith(dlgVkbClassNameHash) && activeWinClassName._ne("SysShadow"))) {
                if (isValidCaretShape() && (bFirstMove || bMoveMandatory || (isValidCaretPos() && ActiveWindowHandler.Singleton.IsInValidCaretMargin(activeWinSettings)))) {
                    int xOffset = (activeWinSettings?.CaretOffset)._getNth(0, Settings.VirtualKeyboardOffsetX);
                    int yOffset = (activeWinSettings?.CaretOffset)._getNth(1, Settings.VirtualKeyboardOffsetY);
                    int xFixed = (activeWinSettings?.VkbFixedPos)._getNth(0, -1)._geZeroOr(Settings.VirtualKeyboardFixedPosX);
                    int yFixed = (activeWinSettings?.VkbFixedPos)._getNth(1, -1)._geZeroOr(Settings.VirtualKeyboardFixedPosY);
                    if (xFixed < 0 && bFixedPosWinClass) xFixed = Math.Abs(Settings.VirtualKeyboardFixedPosX);
                    if (yFixed < 0 && bFixedPosWinClass) yFixed = Math.Abs(Settings.VirtualKeyboardFixedPosY);
                    //double dpiRatio = 1.0; //FrmVkb.GetDeviceDpiRatio();
                    if (bLog || bFirstMove) logger.InfoH($"CaretPos.X={activeWinCaretPos.X}, CaretPos.Y={activeWinCaretPos.Y}, xOffset={xOffset}, yOffset={yOffset}, xFixed={xFixed}, yFixed={yFixed}");
                    if (activeWinCaretPos.X >= 0) {
                        int cX = activeWinCaretPos.X;
                        int cY = activeWinCaretPos.Y;
                        int cW = activeWinCaretPos.Width;
                        int cH = activeWinCaretPos.Height;
                        if (bLog) {
                            logger.InfoH($"MOVE: X={cX}, Y={cY}, W={cW}, H={cH}, OX={xOffset}, OY={yOffset}");
                            if (Settings.LoggingActiveWindowInfo) {
                                var dpis = ScreenInfo.Singleton.ScreenDpi.Select(x => $"{x}")._join(", ");
                                frmVkb.SetTopText($"DR={dpis}, CX={cX},CY={cY},CW={cW},CH={cH},OX={xOffset},OY={yOffset}");
                            }
                        }
                        Action<Form> moveAction = (Form frm) => {
                            int fX = 0;
                            int fY = 0;
                            int fW = frm.Size.Width;
                            int fH = frm.Size.Height;
                            if (xFixed >= 0 && yFixed >= 0) {
                                fX = xFixed;
                                fY = yFixed;
                            } else {
                                fX = cX + (xOffset >= 0 ? cW : -fW) + xOffset ;
                                if (fX < 0) fX = cX + cW + Math.Abs(xOffset);
                                fY = cY + (yOffset >= 0 ? cH : -fH) + yOffset;
                                if (fY < 0) fY = cY + cH + Math.Abs(yOffset);
                                int fRight = fX + fW;
                                int fBottom = fY + fH;
                                Rectangle rect = ScreenInfo.Singleton.GetScreenContaining(cX, cY);
                                if (fRight >= rect.X + rect.Width) fX = cX - fW - Math.Abs(xOffset);
                                if (fBottom >= rect.Y + rect.Height) fY = cY - fH - Math.Abs(yOffset);
                            }
                            MoveWindow(frm.Handle, fX, fY, fW, fH, true);
                        };
                        // 仮想鍵盤の移動
                        moveAction(frmVkb);

                        // 入力モード標識の移動
                        moveAction(frmMode);
                        if (bDiffWin && !this.IsVkbShown) {
                            // 異なるウィンドウに移動したら入力モード標識を表示する
                            frmMode.ShowImmediately();
                        }
                        prevCaretPos = activeWinCaretPos;
                    }
                }
                bFirstMove = false;
            } else {
                logger.Debug(() => $"ActiveWinClassName={activeWinClassName}, VkbClassName={dlgVkbClassNameHash}");
            }
        }

        /// <summary>ストロークヘルプ</summary>
        public void ShowStrokeHelp(string w)
        {
            logger.InfoH($"CALLED: w={w}");
            if (IsDecoderActive) {
                // 指定文字(空なら最後に入力された文字)のストロークヘルプを取得
                ExecCmdDecoder("showStrokeHelp", w);
                // 仮想キーボードにヘルプや文字候補を表示
                getCenterString();
                frmVkb.DrawVirtualKeyboardChars();
            }
        }

        /// <summary>部首合成ヘルプ</summary>
        public void ShowBushuCompHelp()
        {
            logger.InfoH("CALLED");
            if (IsDecoderActive) {
                // 中央鍵盤文字(空なら最後に入力された文字)のストロークヘルプを取得
                ExecCmdDecoder("showBushuCompHelp", CommonState.CenterString);
                // 仮想キーボードにヘルプや文字候補を表示
                getCenterString();
                frmVkb.DrawVirtualKeyboardChars();
            }
        }

        /// <summary> 仮想鍵盤のストローク表を切り替える </summary>
        /// <param name="delta"></param>
        public void RotateStrokeTable(int delta)
        {
            logger.InfoH(() => $"CALLED: delta={delta}");
            if (IsDecoderActive) {
                if (delta == 0) delta = 1;
                frmVkb.RotateStrokeTable(delta);
            }
        }

        /// <summary> 辞書ファイルなどの保存 </summary>
        public void SaveAllFiles()
        {
            //// 出力文字カウントをファイルに出力
            //ActiveWindowHandler.Singleton.WriteCharCountFile(CharCountFile);
            // デコーダが使用する辞書ファイルの保存
            ExecCmdDecoder("saveDictFiles", null);
        }

        private void  dumpDebugTableFiles()
        {
            ExecCmdDecoder("SaveDumpTable", null);    // tmp/dump-table[123].txt (Decoder が実際に保持しているテーブルの内容をダンプしたもの)

            KanchokuWS.CombinationKeyStroke.DeterminerLib.KeyCombinationPool.SingletonK1?.DebugPrintFile("tmp/key-combinationK1.txt");
            KanchokuWS.CombinationKeyStroke.DeterminerLib.KeyCombinationPool.SingletonA1?.DebugPrintFile("tmp/key-combinationA1.txt");
            KanchokuWS.CombinationKeyStroke.DeterminerLib.KeyCombinationPool.SingletonK2?.DebugPrintFile("tmp/key-combinationK2.txt");
            KanchokuWS.CombinationKeyStroke.DeterminerLib.KeyCombinationPool.SingletonA2?.DebugPrintFile("tmp/key-combinationA2.txt");
            KanchokuWS.CombinationKeyStroke.DeterminerLib.KeyCombinationPool.SingletonK3?.DebugPrintFile("tmp/key-combinationK3.txt");
            KanchokuWS.CombinationKeyStroke.DeterminerLib.KeyCombinationPool.SingletonA3?.DebugPrintFile("tmp/key-combinationA3.txt");
        }

        //------------------------------------------------------------------
        // 各種下請け処理
        //------------------------------------------------------------------
        private async Task initializeKanchoku()
        {
            logger.Info("ENTER");

            //Settings.ReadIniFile();
            //frmVkb.SetNormalCellBackColors();

            //// 文字出力カウントファイルの読み込み
            //CharCountFile = KanchokuIni.Singleton.MakeFullPath("char_count.txt");
            //ActiveWindowHandler.Singleton.ReadCharCountFile(CharCountFile);

            // デコーダの初期化
            if (await initializeDecoder()) {
                // 初期打鍵表(下端機能キー以外は空白)の作成
                MakeInitialVkbTable();
                // 打鍵テーブルの作成
                frmVkb.MakeStrokeTables(Settings.StrokeHelpFile);
                // DecKeyハンドラの初期化
                //DecKeyHandler.Initialize(this.Handle);
            }

            logger.Info("LEAVE");
        }

        // デコーダの初期化
        private async Task<bool> initializeDecoder()
        {
            return await Task.Run(() => {
                logger.InfoH("ENTER");
                decoderPtr = CreateDecoder(Logger.LogLevel);         // UI側のLogLevelに合わせる

                //ExecCmdDecoder(null, KanchokuIni.Singleton.IniFilePath, true);
                if (!ExecCmdDecoder(null, Settings.SerializedDecoderSettings, true)) {
                    logger.Error($"CLOSE: Decoder initialize error");
                    //PostMessage(this.Handle, WM_Defs.WM_CLOSE, 0, 0);
                    //this._invoke(() => this.Close());
                    return false;
                }

                // キー入力時のデコーダから出力情報を保持する構造体インスタンスを確保
                decoderOutput = new DecoderOutParams();

                updateStrokeNodesByComplexCommands();

                logger.InfoH("LEAVE");
                return true;
            });
        }

        // デコーダの終了
        private void finalizeDecoder()
        {
            logger.InfoH("ENTER");
            if (decoderPtr != IntPtr.Zero) {
                if (!bNoSaveDicts) {
                    logger.Info("CALL SaveDictsDecoder");
                    SaveDictsDecoder(decoderPtr);
                }
                logger.Info("CALL FinalizeDecoder");
                FinalizeDecoder(decoderPtr);
                decoderPtr = IntPtr.Zero;
            }

            logger.InfoH("LEAVE");
        }

        /// <summary>初期打鍵表(下端機能キー以外は空白)の作成</summary>
        public void MakeInitialVkbTable()
        {
            logger.Info("ENTER");
            if (decoderPtr != IntPtr.Zero) {
                // 初期打鍵テーブルの取得
                MakeInitialVkbTableDecoder(decoderPtr, ref decoderOutput);
                frmVkb.CopyInitialVkbTable(decoderOutput.faceStrings);
            }
            logger.Info("LEAVE");
        }

        ///// <summary>ひらがな・カタカナ打鍵テーブルの作成</summary>
        //private void makeKanaStrokeTable()
        //{
        //    logger.Info("ENTER");
        //    if (decoderPtr != IntPtr.Zero) {
        //        // ひらがな打鍵テーブルの取得
        //        ExecCmdDecoder("makeHiraganaTable", null);
        //        frmVkb.CopyHiraganaVkbTable(decoderOutput.faceStrings);
        //        // カタカナ打鍵テーブルの取得
        //        ExecCmdDecoder("makeKatakanaTable", null);
        //        frmVkb.CopyKatakanaVkbTable(decoderOutput.faceStrings);
        //    }
        //    logger.Info("LEAVE");
        //}

        /// <summary>
        /// デコーダ呼び出し
        /// </summary>
        /// <param name="deckey"></param>
        /// <returns></returns>
        private bool InvokeDecoder(int deckey, uint mod)
        {
            if (IsDecoderActive) {
                ++DeckeyTotalCount;
                logger.InfoH(() => $"\nRECEIVED deckey={(deckey < DecoderKeys.SPECIAL_DECKEY_ID_BASE ? $"{deckey}" : $"{deckey:x}H")}, totalCount={DeckeyTotalCount}");

                // DecKey 無限ループの検出
                if (Settings.DeckeyInfiniteLoopDetectCount > 0) {
                    if (deckey == prevRawDeckey) {
                        ++rawDeckeyRepeatCount;
                        if ((rawDeckeyRepeatCount % 100) == 0) logger.InfoH(() => $"rawDeckeyRepeatCount={rawDeckeyRepeatCount}");
                        if (rawDeckeyRepeatCount > Settings.DeckeyInfiniteLoopDetectCount) {
                            logger.Error($"rawDeckeyRepeatCount exceeded threshold: deckey={deckey:x}H({deckey}), count={rawDeckeyRepeatCount}, threshold={Settings.DeckeyInfiniteLoopDetectCount}");
                            logger.Warn("Force close");
                            this.Close();
                        }
                    } else {
                        prevRawDeckey = deckey;
                        rawDeckeyRepeatCount = 0;
                    }
                }

                // 前回がCtrlキー修飾されたDecKeyで、その処理終了時刻の5ミリ秒以内に次のキーがきたら、それを無視する。
                // そうしないと、キー入力が滞留して、CtrlキーのプログラムによるUP/DOWN処理とユーザー操作によるそれとがコンフリクトする可能性が高まる
                if (prevDeckey >= DecoderKeys.CTRL_DECKEY_START && prevDecDt.AddMilliseconds(5) >= DateTime.Now) {
                    logger.InfoH("SKIP");
                    return false;
                }

                // Ctrl-J と Ctrl-M
                if ((Settings.UseCtrlJasEnter && VKeyComboRepository.GetCtrlDecKeyOf("J") == deckey) /*|| (Settings.UseCtrlMasEnter && VKeyComboRepository.GetCtrlDecKeyOf("M") == deckey)*/) {
                    deckey = DecoderKeys.ENTER_DECKEY;
                }

                // ActivateDecoderの処理中ではない
                // 入力標識の消去
                frmMode.Vanish();
                // 通常のストロークキーまたは機能キー(BSとか矢印キーとかCttrl-Hとか)
                bool flag = handleKeyDecoder(deckey, mod);
                logger.InfoH(() => $"LEAVE: {flag}");
                return flag;
            }
            return false;
        }

        private bool bHiraganaStrokeGuideMode = false;

        private bool bRomanStrokeGuideMode = false;
        private bool bUpperRomanStrokeGuideMode = false;
        private bool bRomanMode = false;

        private string[] candidateCharStrs = null;

        //private static char[] emptyCharArray = new char[0];

        private char[] candidateChars = null;

        private uint targetChar = 0;

        /// <summary>直前は複数打鍵文字の始まりだったか </summary>
        private bool bPrevMultiStrokeChar = false;

        private void getTargetChar(int deckey)
        {
            if (targetChar == 0 && candidateCharStrs != null) {
                var s = candidateCharStrs._getNth(deckey);
                logger.Info(() => $"targetChar={s}");
                if (s._notEmpty()) {
                    targetChar = s[0];
                    if (s.Length > 1) targetChar = targetChar << 16 + s[1];
                    // 第2打鍵待ちでロックする(勝手に第2打鍵待ちをキャンセルしない)
                    IsWaitingSecondStrokeLocked = true;
                    // 単打で入力ガイドが終了する場合もあるので先にローマ字列をクリアしておく
                    ExecCmdDecoder("clearTailRomanStr", null);
                }
            }
        }

        private int makeInputFlags(bool romanStrokeGuideMode, bool upperRomanStrokeGuideMode)
        {
            int result = 0;
            if (romanStrokeGuideMode) result |= DecoderInputFlags.DecodeKeyboardChar;
            if (upperRomanStrokeGuideMode) result |= DecoderInputFlags.UpperRomanGuideMode;
            return result;
        }

        /// <summary>
        /// デコーダの呼び出し
        /// </summary>
        private bool handleKeyDecoder(int deckey, uint mod)
        {
            logger.InfoH(() => $"ENTER: deckey={deckey:x}H({deckey}), mod={mod:x}");

            getTargetChar(deckey);
            logger.InfoH(() => $"targetChar={targetChar}, bRomanStrokeGuideMode={bRomanStrokeGuideMode}, bUpperRomanStrokeGuideMode={bUpperRomanStrokeGuideMode}");

            // デコーダの呼び出し
            HandleDeckeyDecoder(decoderPtr, deckey, targetChar, makeInputFlags(bRomanStrokeGuideMode, bUpperRomanStrokeGuideMode), ref decoderOutput);

            logger.InfoH(() =>
                $"HandleDeckeyDecoder: RESULT: table#={decoderOutput.strokeTableNum}, strokeDepth={decoderOutput.GetStrokeCount()}, layout={decoderOutput.layout}, " +
                $"numBS={decoderOutput.numBackSpaces}, resultFlags={decoderOutput.resultFlags:x}H, output={decoderOutput.outString._toString()}, topString={decoderOutput.topString._toString()}, " +
                $"IsDeckeyToVkey={decoderOutput.IsDeckeyToVkey()}, nextStrokeDeckey={decoderOutput.nextStrokeDeckey}");

            // 第1打鍵待ち状態になったら、一時的な仮想鍵盤表示カウントをリセットする
            //if (decoderOutput.GetStrokeCount() < 1) Settings.VirtualKeyboardShowStrokeCountTemp = 0;

            // 第2打鍵待ち状態になったら、第2打鍵待ちになった時刻をセットする
            if (decoderOutput.GetStrokeCount() > 0) dtWaitSecondStroke = DateTime.Now;

            // 中央鍵盤文字列の取得
            getCenterString();

            bool sendKeyFlag = true;

            // ローマ字?
            bool isUpperAlphabet(char ch) => (ch >= 'A' && ch <= 'Z');
            bool isAlphabet(char ch) => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
            bool isHiragana(char ch) => (ch >= 'ぁ' && ch <= 'ん');
            string getTailRomanStr()
            {
                int pos = 0;
                for (; pos < decoderOutput.topString.Length; ++pos) {
                    if (decoderOutput.topString[pos] == '\0') break;
                }
                int tailPos = pos;
                for (;  pos > 0; --pos) {
                    if (!isAlphabet(decoderOutput.topString[pos - 1])) break;
                }
                return new string(decoderOutput.topString.Skip(pos).Take(tailPos - pos).ToArray());
            }
            string getTailHiraganaStr()
            {
                int pos = 0;
                for (; pos < decoderOutput.topString.Length; ++pos) {
                    if (decoderOutput.topString[pos] == '\0') break;
                }
                int tailPos = pos;
                for (;  pos > 0; --pos) {
                    if (!isHiragana(decoderOutput.topString[pos - 1])) break;
                }
                return new string(decoderOutput.topString.Skip(pos).Take(tailPos - pos).ToArray());
            }
            logger.Info(() =>
                    $"RomanStrokeGuide={bRomanStrokeGuideMode}, UpperRomanStrokeGuide={bUpperRomanStrokeGuideMode}, romanMode={bRomanMode}, "
                    + $"HiraganaStrokeGuide={bHiraganaStrokeGuideMode}, "
                    + $"Settings.UpperRomanStrokeGuide={Settings.UpperRomanStrokeGuide}, numBS={decoderOutput.numBackSpaces}, "
                    + $"output={decoderOutput.outString._toString()}, deckey={deckey}, prevMultiChar={bPrevMultiStrokeChar}");

            if (bRomanStrokeGuideMode ||
                (bRomanMode && decoderOutput.numBackSpaces > 0) ||
                ((Settings.UpperRomanStrokeGuide || bUpperRomanStrokeGuideMode) &&
                 decoderOutput.numBackSpaces == 0 &&
                 isUpperAlphabet(decoderOutput.outString[0]) && decoderOutput.outString[1] == 0)) {
                // ローマ字読み入力モード
                var romanStr = getTailRomanStr();
                logger.Info(() => $"romanStr={romanStr}");
                CommonState.CenterString = "ローマ字";
                candidateChars = KanjiYomiTable.GetCandidatesFromRoman(romanStr);
                candidateCharStrs = frmVkb.DrawStrokeHelp(candidateChars);
                frmVkb.SetTopText(decoderOutput.topString);
                targetChar = 0;
                bRomanMode = true;
            } else if (bHiraganaStrokeGuideMode) {
                // ひらがな読み入力モード
                CommonState.CenterString = "ひらがな";
                candidateChars = KanjiYomiTable.GetCandidates(getTailHiraganaStr());
                candidateCharStrs = frmVkb.DrawStrokeHelp(candidateChars);
                frmVkb.SetTopText(decoderOutput.topString);
                targetChar = 0;
            } else {
                if (decoderOutput.GetStrokeCount() > 0) {
                    logger.DebugH("PATH-1");
                    // 第2打鍵以降の待ちで、何かVkey出力がある場合は、打鍵クリア
                    if (decoderOutput.IsDeckeyToVkey()) {
                        logger.DebugH(() => $"send CLEAR_STROKE_DECKEY");
                        //HandleDeckeyDecoder(decoderPtr, DecoderKeys.CLEAR_STROKE_DECKEY, 0, 0, ref decoderOutput);
                        sendClearStrokeToDecoder();
                    }
                    if (decoderOutput.numBackSpaces > 0) {
                        SendInputHandler.Singleton.SendStringViaClipboardIfNeeded(null, decoderOutput.numBackSpaces, true);
                    }
                }
                if (decoderOutput.GetStrokeCount() < 1) {
                    logger.DebugH("PATH-2");
                    // 第1打鍵待ちになった時のみ
                    // 一時的な仮想鍵盤表示カウントをリセットする
                    Settings.VirtualKeyboardShowStrokeCountTemp = 0;

                    // 他のVKey送出(もしあれば)
                    if (decoderOutput.IsDeckeyToVkey()) {
                        logger.DebugH("PATH-3");
                        sendKeyFlag = sendVkeyFromDeckey(deckey, mod);
                        //nPreKeys += 1;
                    }

                    candidateCharStrs = null;
                    candidateChars = null;
                    targetChar = 0;

                    // BSと文字送出(もしあれば)
                    var outString = decoderOutput.outString;
                    int outLen = outString._strlen();
                    if (outLen >= 0) {
                        logger.DebugH("PATH-4");
                        // 送出文字列中に特殊機能キー(tabやleftArrowなど)が含まれているか
                        bool bFuncVkeyContained = isFuncVkeyContained(outString, outLen);
                        bool bPreRewriteTarget = isTailPreRewriteChar(outString, outLen);
                        int numBS = decoderOutput.numBackSpaces;
                        int leadLen = calcSameLeadingLen(outString, outLen, numBS);
                        var outStr = leadLen > 0 ? outString.Skip(leadLen).ToArray() : outString;
                        /*if (Settings.LoggingDecKeyInfo)*/ logger.InfoH(() => $"outString={outString._toString()}, numBS={numBS}, leadLen={leadLen}, outStr={outStr._toString()}");
                        WriteStrokeLog(outStr._toString());
                        SendInputHandler.Singleton.SendStringViaClipboardIfNeeded(outStr, numBS - leadLen, bFuncVkeyContained);
                        if (bFuncVkeyContained) {
                            logger.DebugH("PATH-5");
                            // 送出文字列中に特殊機能キー(tabやleftArrowなど)が含まれている場合は、 FULL_ESCAPE を実行してミニバッファをクリアしておく
                            HandleDeckeyDecoder(decoderPtr, DecoderKeys.FULL_ESCAPE_DECKEY, 0, 0, ref decoderOutput);
                        }
                        // 前置書き換え対象文字なら、許容時間をセットする
                        CombinationKeyStroke.Determiner.Singleton.SetPreRewriteTime(bPreRewriteTarget);
                    }
                }
                logger.DebugH("PATH-6");

                // 仮想キーボードにヘルプや文字候補を表示
                frmVkb.DrawVirtualKeyboardChars(Settings.ShowLastStrokeByDiffBackColor && !bPrevMultiStrokeChar ? unshiftDeckey(deckey) : -1);

                bPrevMultiStrokeChar = decoderOutput.outString[0] == 0 && isNormalDeckey(deckey);

                if (bRomanMode || bUpperRomanStrokeGuideMode) {
                    logger.DebugH("PATH-7");
                    bRomanMode = false;
                    bUpperRomanStrokeGuideMode = false;
                    ExecCmdDecoder("clearTailRomanStr", null);
                }
            }

            logger.InfoH(() => $"LEAVE: sendKeyFlag={sendKeyFlag}");

            return sendKeyFlag;
        }

        /// <summary>現出力文字列と同じ先頭部の長さを計算</summary>
        /// <returns></returns>
        private int calcSameLeadingLen(char[] outString, int outLen, int numBS)
        {
            if (numBS <= 0) return 0;

            var topString = frmVkb.TopText;
            int topLen = topString._safeLength();
            if (topLen > 0 && topString.Last() == '|') {
                // 末尾がブロッカーフラグだったので、それを削除しておく
                --topLen;
                topString = topString.Substring(0, topLen);
            }
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"ENTER: topString={topString}, topLen={topLen}, outString={outString._toString()}, outLen={outLen}, numBS={numBS}");

            if (topLen <= 0) return 0;

            int topPos = topLen - numBS;
            if (topPos < 0) return 0;

            if (Settings.LoggingDecKeyInfo) logger.InfoH($"topLen={topLen}, topPos={topPos}, outLen={outLen}");
            int i = 0;
            while (topPos + i < topLen && i < outLen && topString[topPos + i] == outString[i]) {
                ++i;
            }
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"LEAVE: LeadingLen={i}");
            return i;
        }

        public string CallDecoderWithKey(int deckey, uint mod, out int numBS)
        {
            logger.InfoH(() => $"ENTER: deckey={deckey:x}H({deckey}), mod={mod:x}, " +
                $"targetChar={targetChar}, bRomanStrokeGuideMode={bRomanStrokeGuideMode}, bUpperRomanStrokeGuideMode={bUpperRomanStrokeGuideMode}");

            // デコーダの呼び出し
            HandleDeckeyDecoder(decoderPtr, deckey, targetChar, makeInputFlags(bRomanStrokeGuideMode, bUpperRomanStrokeGuideMode), ref decoderOutput);

            logger.InfoH(() =>
                $"HandleDeckeyDecoder: RESULT: table#={decoderOutput.strokeTableNum}, strokeDepth={decoderOutput.GetStrokeCount()}, layout={decoderOutput.layout}, " +
                $"numBS={decoderOutput.numBackSpaces}, resultFlags={decoderOutput.resultFlags:x}H, output={decoderOutput.outString._toString()}, topString={decoderOutput.topString._toString()}, " +
                $"IsDeckeyToVkey={decoderOutput.IsDeckeyToVkey()}, nextStrokeDeckey={decoderOutput.nextStrokeDeckey}");

            bool bPreRewriteTarget = isTailPreRewriteChar(decoderOutput.outString);
            // 前置書き換え対象文字なら、許容時間をセットする
            CombinationKeyStroke.Determiner.Singleton.SetPreRewriteTime(bPreRewriteTarget);

            var result = decoderOutput.outString._toString();
            numBS = decoderOutput.numBackSpaces;
            WriteStrokeLog(result);
            logger.InfoH(() => $"LEAVE: result={result}, numBS={decoderOutput.numBackSpaces}, usedTable={decoderOutput.strokeTableNum}, strokeCount={decoderOutput.GetStrokeCount()}");
            return result;
        }

        /// <summary>
        /// デコーダの直接呼び出し
        /// </summary>
        private void handleKeyDecoderDirectly(int deckey, uint mod)
        {
            logger.InfoH(() => $"ENTER: deckey={deckey:x}H({deckey}), mod={mod:x}, " +
                $"targetChar={targetChar}, bRomanStrokeGuideMode={bRomanStrokeGuideMode}, bUpperRomanStrokeGuideMode={bUpperRomanStrokeGuideMode}");

            // デコーダの呼び出し
            HandleDeckeyDecoder(decoderPtr, deckey, targetChar, makeInputFlags(bRomanStrokeGuideMode, bUpperRomanStrokeGuideMode), ref decoderOutput);

            // 送出文字列中に特殊機能キー(tabやleftArrowなど)が含まれているか
            bool bFuncVkeyContained = isFuncVkeyContained(decoderOutput.outString);
            bool bPreRewriteTarget = isTailPreRewriteChar(decoderOutput.outString);
            // BSと文字送出(もしあれば)
            SendInputHandler.Singleton.SendStringViaClipboardIfNeeded(decoderOutput.outString, decoderOutput.numBackSpaces, bFuncVkeyContained);
            if (bFuncVkeyContained) {
                // 送出文字列中に特殊機能キー(tabやleftArrowなど)が含まれている場合は、 FULL_ESCAPE を実行してミニバッファをクリアしておく
                HandleDeckeyDecoder(decoderPtr, DecoderKeys.FULL_ESCAPE_DECKEY, 0, 0, ref decoderOutput);
            }
            // 前置書き換え対象文字なら、許容時間をセットする
            CombinationKeyStroke.Determiner.Singleton.SetPreRewriteTime(bPreRewriteTarget);
        }

        /// <summary>
        /// デコーダにCLEAR_STROKEを送りつける
        /// </summary>
        private void sendClearStrokeToDecoder()
        {
            logger.InfoH(() => $"CALLED");
            sendDeckeyToDecoder(DecoderKeys.CLEAR_STROKE_DECKEY);
        }

        /// <summary>
        /// デコーダにキーを送りつける
        /// </summary>
        private void sendDeckeyToDecoder(int deckey)
        {
            logger.InfoH(() => $"CLLED: deckey={deckey:x}H({deckey})");
            HandleDeckeyDecoder(decoderPtr, deckey, 0, 0, ref decoderOutput);
            if (IsDecoderActive) {
                // 中央鍵盤文字列の取得
                getCenterString();
                // 仮想キーボードにヘルプや文字候補を表示
                frmVkb.DrawVirtualKeyboardChars();
            }
        }

        private bool isFuncVkeyContained(char[] str, int len = -1)
        {
            if (len < 0) len = str._strlen();
            int pos = str._findIndex(0, len, '!');
            while (pos >= 0 && pos + 1 < str.Length) {
                if (str[pos + 1] == '{') return true;
                pos = str._findIndex(pos + 1, len, '!');
            }
            return false;
        }

        private bool isTailPreRewriteChar(char[] str, int len = -1)
        {
            if (len < 0) len = str._strlen();
            return len > 0 && (Settings.PreRewriteTargetChars.Contains('*') || Settings.PreRewriteTargetChars.Contains(str[len - 1]));
        }

        private bool isNormalDeckey(int deckey)
        {
            return deckey >= DecoderKeys.NORMAL_DECKEY_START && deckey < DecoderKeys.PLANE_DECKEY_NUM;
        }

        private int unshiftDeckey(int deckey)
        {
            return deckey < DecoderKeys.TOTAL_SHIFT_DECKEY_END ? deckey % DecoderKeys.PLANE_DECKEY_NUM : deckey;
        }

        private void drawRomanOrHiraganaMode(bool bRoman, bool bHiragana)
        {
            // ローマ字またはひらがな読みモード
            CommonState.CenterString = bRoman ? "ローマ字" : bHiragana ? "ひらがな" : "";
            candidateCharStrs = frmVkb.DrawStrokeHelp(candidateChars);
            if (bRomanMode && !bRoman) {
                ExecCmdDecoder("clearTailRomanStr", null);
            }
            bRomanMode = bRoman;
        }

        private bool sendVkeyFromDeckey(int deckey, uint mod)
        {
            var ctrlKeyState = SendInputHandler.GetCtrlKeyState();
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"ENTER: deckey={deckey:x}H({deckey}), mod={mod:x}({mod}), leftCtrl={ctrlKeyState.LeftKeyDown}, rightCtrl={ctrlKeyState.RightKeyDown}");
            if ((!ctrlKeyState.LeftKeyDown && !ctrlKeyState.RightKeyDown)                                       // Ctrlキーが押されていないか、
                || isCtrlKeyConversionEffectiveWindow()                                                         // Ctrl修飾を受け付けるWindowClassか
                //|| deckey < DecoderKeys.STROKE_DECKEY_END                                                     // 通常のストロークキーは通す
                || deckey < DecoderKeys.NORMAL_DECKEY_NUM                                                       // 通常のストロークキーは通す
                || deckey >= DecoderKeys.CTRL_DECKEY_START && deckey < DecoderKeys.CTRL_DECKEY_END              // Ctrl-A～Ctrl-Z は通す
                || deckey >= DecoderKeys.CTRL_SHIFT_DECKEY_START && deckey < DecoderKeys.CTRL_SHIFT_DECKEY_END  // Ctrl-Shift-A～Ctrl-Shift-Z は通す
                ) {

                if (Settings.LoggingDecKeyInfo) logger.InfoH($"TARGET WINDOW");

                if (Settings.ShortcutKeyConversionEnabled || (mod & KeyModifiers.MOD_ALT) == 0) {
                    // CtrlやAltなどのショートカットキーの変換をやるか、または Altキーが押されていなかった
                    var dkChar = Domain.DecoderKeyVsChar.GetArrangedCharFromDecKey(deckey);
                    if (dkChar != '\0') {
                        var vk = CharVsVKey.GetVKeyFromFaceChar(dkChar);
                        if (vk != 0) {
                            if (vk >= 0x100) {
                                vk -= 0x100;
                                mod |= KeyModifiers.MOD_SHIFT;
                            }
                            if (Settings.LoggingDecKeyInfo) logger.InfoH($"SendVKeyCombo: {mod:x}H({mod}), {vk:x}H({vk})");
                            SendInputHandler.Singleton.SendVKeyCombo(mod, vk, 1);
                            if (Settings.LoggingDecKeyInfo) logger.InfoH($"LEAVE: TRUE");
                            return true;
                        }
                    }
                }
                var combo = VKeyComboRepository.GetVKeyComboFromDecKey(deckey);
                if (combo != null) {
                    if (Settings.LoggingDecKeyInfo) {
                        logger.InfoH($"SEND: combo.modifier={combo.Value.modifier:x}H({combo.Value.modifier}), "
                            + $"combo.vkey={combo.Value.vkey:x}H({combo.Value.vkey}), "
                            + $"ctrl={(combo.Value.modifier & KeyModifiers.MOD_CONTROL) != 0}, "
                            + $"mod={mod:x}H({mod})");
                    }
                    //if (deckey < DecoderKeys.FUNCTIONAL_DECKEY_ID_BASE) {
                    //    SendInputHandler.Singleton.SendVirtualKey(combo.Value.vkey, 1);
                    //} else {
                    //    SendInputHandler.Singleton.SendVKeyCombo(combo.Value, 1);
                    //}
                    SendInputHandler.Singleton.SendVKeyCombo((combo.Value.modifier != 0 ? combo.Value.modifier : mod), combo.Value.vkey, 1);
                    if (Settings.LoggingDecKeyInfo) logger.InfoH($"LEAVE: TRUE");
                    return true;
                } else {
                    if (Settings.LoggingDecKeyInfo) logger.InfoH($"NO VKEY COMBO for deckey={deckey:x}H({deckey})");
                }
            }
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"LEAVE: FALSE");
            return false;
        }

        /// <summary>修飾キー付きvkeyをSendInputする</summary>
        private bool SendInputVkeyWithMod(uint mod, uint vkey)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"CALLED: mod={mod}H({mod}), vkey={vkey}H({vkey})");
            SendInputHandler.Singleton.SendVKeyCombo(mod, vkey, 1);
            return true;
        }

        /// <summary> 今日の日付文字列を出力する </summary>
        private void outputTodayDate()
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"CALLED");
            var items = Settings.DateStringFormat._split('|');
            if (items._isEmpty()) return;
            if (dateStrDeckeyCount < 0)
                dateStrDeckeyCount += items.Length + 1;
            else
                dateStrDeckeyCount %= items.Length + 1;
            var dtStr = "";
            if (dateStrDeckeyCount > 0) {
                var dtNow = DateTime.Now.AddDays(dayOffset);
                var fmt = items._getNth(dateStrDeckeyCount - 1)._strip();
                if (Settings.LoggingDecKeyInfo) logger.InfoH($"count={dateStrDeckeyCount}, fmt={fmt}");
                if (fmt._isEmpty()) return;

                int diffYear = 0;
                if (fmt._safeIndexOf("r") >= 0) {
                    // 令和
                    diffYear = 2018;
                    fmt = fmt._reReplace("r+", "y");
                }
                dtStr = dtNow.AddYears(-diffYear).ToString(fmt);
            }
            SendInputHandler.Singleton.SendStringViaClipboardIfNeeded(dtStr.ToCharArray(), prevDateStrLength);
            prevDateStrLength = dtStr.Length;
        }

        //------------------------------------------------------------------
        // イベントハンドラ
        //------------------------------------------------------------------
        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            logger.Info("CALLED");
            if (((MouseEventArgs)e).Button == MouseButtons.Left) {
                ToggleActiveState();
            }
        }

        private void Exit_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 終了
            logger.Info("CALLED");
            Terminate();
        }

        private void Settings_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logger.Info("CALLED");
            openSettingsDialog();
        }

        private void openSettingsDialog(bool bInitial = false)
        {
            if (frmSplash != null) closeSplash();
            if (!DlgSettings.BringTopMostShownDlg()) {
                var dlg = new DlgSettings(this, frmVkb, frmMode);
                if (bInitial) dlg.ShowInitialMessage();
                dlg.ShowDialog();
                bool bRestart = dlg.RestartRequired;
                bool bNoSave = dlg.NoSave;
                dlg.Dispose();
                logger.InfoH(() => $"bRestart={bRestart}, bNoSave={bNoSave}");
                if (bRestart) Restart(bNoSave);
            }
        }

        private void Cancel_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private bool initialSettingsDialogOpened = false;

        // アクティブウィンドウの情報を取得する間隔(ミリ秒)
        private int activeWinInfoCount = Settings.GetActiveWindowInfoIntervalMillisec / timerInterval;

        // 辞書保存チャレンジ開始時刻
        private static DateTime saveDictsChallengeDt = DateTime.MaxValue;

        // 辞書保存チャレンジ開始時刻の再初期化
        private void reinitializeSaveDictsChallengeDt()
        {
            if (Settings.SaveDictsIntervalTime > 0) {
                saveDictsChallengeDt = DateTime.Now.AddMinutes(Settings.SaveDictsIntervalTime);
                saveDictsPlannedDt = saveDictsChallengeDt.AddMinutes(Settings.SaveDictsCalmTime);
            } else {
                saveDictsChallengeDt = DateTime.MaxValue;
                saveDictsPlannedDt = DateTime.MaxValue;
            }
            logger.DebugH($"saveDictsChallengeDt={saveDictsChallengeDt}, saveDictsPlannedDt={saveDictsPlannedDt}");
        }

        // 辞書保存予定時刻
        private static DateTime saveDictsPlannedDt = DateTime.MaxValue;

        private void renewSaveDictsPlannedDt()
        {
            var dtNow = DateTime.Now;
            if (dtNow >= saveDictsChallengeDt) {
                saveDictsPlannedDt = dtNow.AddMinutes(Settings.SaveDictsCalmTime);
                logger.DebugH($"saveDictsPlannedDt={saveDictsPlannedDt}");
            }
        }

        // 第2打鍵待ちになった時刻
        private DateTime dtWaitSecondStroke = DateTime.MaxValue;

        /// <summary>第2打鍵待ちでロックされている(勝手に第2打鍵待ちをキャンセルしない)</summary>
        public bool IsWaitingSecondStrokeLocked { get; set; } = false;

        /// <summary>
        /// メインタイマー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (--activeWinInfoCount <= 0) {
                if (!initialSettingsDialogOpened && frmSplash?.SettingsDialogOpenFlag == true) {
                    // スプラッシュウィンドウから設定ダイアログの初期起動を指示された
                    initialSettingsDialogOpened = true; // 今回限りの起動とする
                    openSettingsDialog(true);
                }
                ActiveWindowHandler.Singleton.GetActiveWindowInfo(moveVkbWindow, frmVkb);
                activeWinInfoCount = Settings.GetActiveWindowInfoIntervalMillisec / timerInterval;
            }

            if (IMEHandler.GetImeStateChanged(frmVkb) && Settings.ImeCooperationEnabled) {
                if (IMEHandler.ImeEnabled) {
                    ActivateDecoder();
                } else {
                    DeactivateDecoder(false);
                }
            }

            CombinationKeyStroke.Determiner.Singleton.HandleQueue();

            // 第2打鍵待ちの場合は、それをキャンセルする
            if (IsDecoderActive && decoderOutput.GetStrokeCount() > 0 && Settings.CancelSecondStrokeMillisec > 0) {
                if (dtWaitSecondStroke._isValid() && dtWaitSecondStroke <= DateTime.Now.AddMilliseconds(-Settings.CancelSecondStrokeMillisec)) {
                    if (!IsWaitingSecondStrokeLocked) {
                        dtWaitSecondStroke = DateTime.MaxValue;
                        sendClearStrokeToDecoder();
                    }
                }
            }

            if (DateTime.Now >= saveDictsPlannedDt || (IsDecoderActive && DateTime.Now >= saveDictsChallengeDt)) {
                reinitializeSaveDictsChallengeDt();
                if (decoderPtr != IntPtr.Zero) {
                    logger.InfoH("CALL SaveDictsDecoder");
                    SaveDictsDecoder(decoderPtr);
                }
            }

            flushStrokeLogByTimer();
        }

        // 辞書内容を保存して再起動
        private void RestartWithSave_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logger.Debug("ENTER");
            Restart(false);
            logger.Debug("LEAVE");
        }

        // 辞書内容を破棄して再起動
        private void RestartWithDiscard_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logger.Debug("ENTER");
            Restart(true);
            logger.Debug("LEAVE");
        }

        // 部首合成辞書をリロードする
        public void ReloadBushuDic()
        {
            if (SystemHelper.OKCancelDialog("部首合成辞書ファイルを再読み込みします。\r\n現在デコーダが保持している部首合成辞書の内容は失われます。\r\nよろしいですか。")) {
                ExecCmdDecoder("readBushuDic", null);
            }
        }

        // Wikipedia交ぜ書き辞書を読み込む
        public void ReadMazegakiWikipediaDic()
        {
            if (SystemHelper.OKCancelDialog("Wikipedia交ぜ書き辞書ファイルを読み込みます。\r\n10秒以上かかる場合がありますが、「完了」が表示されるまでは漢直操作をしないでください。\r\nよろしいですか。")) {
                ExecCmdDecoder("readMazegakiDic", "kwmaze.wiki.txt");
                SystemHelper.ShowInfoMessageBox("Wkipedia交ぜ書き辞書の読み込みを完了しました。");
            }
        }

        // 設定ファイルと各種定義ファイルをリロードする
        public void ReloadSettingsAndDefFiles()
        {
            logger.InfoH("ENTER");

            // キーボードハンドラの再初期化
            keDispatcher.Reinitialize();

            // 初期化
            VKeyComboRepository.Initialize();
            ExtraModifiers.Initialize();
            DlgModConversion.Initialize();

            // キーボードファイルの読み込み
            bool resultOK = readKeyboardFile();

            // 設定ファイルの読み込み
            Settings.ReadIniFile();

            if (!resultOK) return;

            // 各種定義ファイルの読み込み
            ReloadDefFiles();

            // デバッグ用テーブルファイルの出力
            if (Settings.OutputDebugTableFiles) dumpDebugTableFiles();

            // 辞書保存チャレンジ開始時刻の再初期化
            reinitializeSaveDictsChallengeDt();

            logger.InfoH("LEAVE");
        }

        public void KanaTrainingModeToggle()
        {
            logger.Info("CALLED");
            if (IsDecoderActive) {
                Settings.KanaTrainingMode = !Settings.KanaTrainingMode;
                if (Settings.KanaTrainingMode) {
                    ExecCmdDecoder("setKanaTrainingMode", "true");
                    ExecCmdDecoder("setAutoHistSearchEnabled", "false");
                } else {
                    ExecCmdDecoder("setKanaTrainingMode", "false");
                    if (Settings.AutoHistSearchEnabled) ExecCmdDecoder("setAutoHistSearchEnabled", "true");
                }
                frmVkb.DrawInitialVkb();
            }
        }

        private void ReadBushuDic_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logger.Info("CALLED");
            ReloadBushuDic();
        }

        private void ReadMazeWikipediaDic_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logger.Info("CALLED");
            ReadMazegakiWikipediaDic();
        }

        private void ExchangeTable_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logger.Info("CALLED");
            ExchangeCodeTable();
        }

        private void ReloadSettings_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logger.Info("CALLED");
            ReloadSettingsAndDefFiles();
        }

        private void KanaTrainingMode_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logger.Info("CALLED");
            KanaTrainingModeToggle();
        }

        /// <summary> 漢直WSの一時停止/再開 </summary>
        private void Stop_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.DecoderStopped = !Settings.DecoderStopped;
            Stop_ToolStripMenuItem.Text = Settings.DecoderStopped ? "再開" : "一時停止";
        }
    }

}

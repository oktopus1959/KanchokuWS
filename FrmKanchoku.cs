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
using Utils;

namespace KanchokuWS
{
    public partial class FrmKanchoku : Form
    {
        private static Logger logger = Logger.GetLogger();

        //------------------------------------------------------------------
        // GUI側の処理
        //------------------------------------------------------------------
        /// <summary> 仮想鍵盤フォーム </summary>
        private FrmVirtualKeyboard frmVkb;

        public void ShowFrmVkb()
        {
            ShowWindow(frmVkb.Handle, SW_SHOWNA);   // NonActive
        }

        /// <summary> 漢直モードマーカーフォーム </summary>
        private FrmModeMarker frmMode;

        public void ShowFrmMode()
        {
            //string center = CommonState.CenterString._notEmpty() ? CommonState.CenterString.Substring(0, 1) : "漢";
            //logger.Debug(() => $"center={center}, mode.face={frmMode.FaceString}");
            //frmMode.FaceString = center;
            ShowWindow(frmMode.Handle, SW_SHOWNA);   // NonActive
        }

        /// <summary> アクティブなウィンドウに関する処理 </summary>
        private ActiveWindowHandler actWinHandler;

        //private string CharCountFile;

        private const int timerInterval = 100;

        private KeyboardEventDispatcher keDispatcher { get; set; }

        //------------------------------------------------------------------
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FrmKanchoku(KeyboardEventDispatcher dispatcher)
        {
            keDispatcher = dispatcher;

            // スクリーン情報の取得
            ScreenInfo.GetScreenInfo();

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
            logger.WriteLog("INFO", $"\n\n==== KANCHOKU WS START (LogLevel={Logger.LogLevel}) ====");

            // キーボードファイルの読み込み
            if (!readKeyboardFile()) return;

            // 設定ファイルの読み込み
            Settings.ReadIniFile();

            // 各種定義ファイルの読み込み
            ReadDefFiles();

            // 仮想鍵盤フォームの作成
            frmVkb = new FrmVirtualKeyboard(this);
            frmVkb.Opacity = 0;
            frmVkb.Show();

            // 漢直モード表示フォームの作成
            frmMode = new FrmModeMarker(this, frmVkb);
            frmMode.Show();
            frmMode.Hide();

            // 漢直初期化処理
            await initializeKanchoku();

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

        /// <summary> キーボードファイルの読み込み (成功したら true, 失敗したら false を返す) </summary>
        private bool readKeyboardFile()
        {
            if (!VirtualKeys.ReadKeyboardFile()) {
                // キーボードファイルを読み込めなかったので終了する
                logger.Error($"CLOSE: Can't read keyboard file");
                //DecKeyHandler.Destroy();
                //PostMessage(this.Handle, WM_Defs.WM_CLOSE, 0, 0);
                this.Close();
                return false;
            }
            return true;
        }

        /// <summary> 各種定義ファイルの読み込み </summary>
        public void ReadDefFiles()
        {
            // 文字定義ファイルの読み込み
            //if (Settings.CharsDefFile._notEmpty()) {
            //    DecoderKeyToChar.ReadCharsDefFile(Settings.CharsDefFile);
            //}

            // 追加の修飾キー定義ファイルの読み込み
            if (Settings.ExtraModifiersEnabled && Settings.ModConversionFile._notEmpty()) {
                VirtualKeys.ReadExtraModConversionFile(Settings.ModConversionFile);
            }

            // 漢字読みファイルの読み込み
            if (Settings.KanjiYomiFile._notEmpty()) {
                KanjiYomiTable.ReadKanjiYomiFile(Settings.KanjiYomiFile);
            }
        }

        /// <summary> 各種定義ファイルの読み込み </summary>
        public void ReloadDefFiles()
        {
            // 各種定義ファイルの読み込み
            ReadDefFiles();

            ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);

            // 初期打鍵表(下端機能キー以外は空白)の作成
            MakeInitialVkbTable();

            // 打鍵テーブルの作成
            if (Settings.StrokeHelpFile._notEmpty()) {
                frmVkb.MakeStrokeTables(Settings.StrokeHelpFile);
            }
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
            //actWinHandler.WriteCharCountFile(CharCountFile);

            // この後は各種終了処理
            //DecKeyHandler.Destroy();
            actWinHandler?.Dispose();
            finalizeDecoder();
            frmMode?.Close();
            frmVkb?.Close();

            // 再起動
            if (bRestart) {
                logger.InfoH("RESTART");
                MultiAppChecker.Release();
                Logger.Close();
                Helper.StartProcess(SystemHelper.GetExePath(), null);
            }

            // 各種Timer処理が終了するのを待つ
            Helper.WaitMilliSeconds(200);
            Logger.EnableInfoH();
            logger.WriteLog("INFO", "==== KANCHOKU WS TERMINATED ====\n");
        }

        private bool bRestart = false;
        private bool bNoSaveDicts = false;

        //------------------------------------------------------------------
        // 終了
        public void Terminate()
        {
            if (frmSplash != null) {
                closeSplash();
                logger.Debug("Splash Closed");
            }
            DeactivateDecoder();
            logger.Debug("Decoder OFF");
            if (!Settings.ConfirmOnClose || SystemHelper.OKCancelDialog("漢直窓Sを終了します。\r\nよろしいですか。")) {
                Close();
            }
        }

        //------------------------------------------------------------------
        // 再起動
        public void Restart(bool bNoSave)
        {
            if (frmSplash != null) {
                closeSplash();
                logger.Debug("Splash Closed");
            }
            DeactivateDecoder();
            logger.Debug("Decoder OFF");
            var msg = bNoSave ?
                "漢直窓Sを再起動します。\r\nデコーダが保持している辞書内容はファイルに保存されません。\r\nよろしいですか。" :
                "漢直窓Sを再起動します。\r\nデコーダが保持している辞書内容をファイルに書き出すので、\r\nユーザーが直接辞書ファイルに加えた変更は失われます。\r\nよろしいですか。";
            if (!Settings.ConfirmOnRestart || SystemHelper.OKCancelDialog(msg)) {
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
            logger.Info("CALLED");
            if (frmSplash != null) {
                if (syncSplash.BusyCheck()) return;
                using (syncSplash) {
                    if (frmSplash != null) {
                        frmSplash.IsKanchokuTerminated = true;
                        logger.Info("CLOSED");
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

        [DllImport("user32.dll")]
        private static extern void ShowWindow(IntPtr hWnd, int nCmdShow);

        // ウィンドウをアクティブにせずに表示する
        private const int SW_SHOWNA = 8;

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
        static extern void HandleDeckeyDecoder(IntPtr decoder, int keyId, uint targetChar, bool decodeKeyboardChar, ref DecoderOutParams outParams);

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
            int len = decoderOutput.centerString._findIndex(c => c == '\0');
            if (len < 0) len = decoderOutput.centerString.Length;
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
            logger.Info(() => $"ENTER: cmd={cmd}, data={data}, bInit={bInit}");
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

                if (result == 1) {
                    var errMsg = new string(prm.inOutData, 0, prm.inOutData._findIndex(c => c == '\0')._geZeroOr(prm.inOutData.Length));
                    logger.Warn(errMsg);
                    SystemHelper.ShowWarningMessageBox(errMsg);
                } else if (result > 1) {
                    var errMsg = new string(prm.inOutData, 0, prm.inOutData._findIndex(c => c == '\0')._geZeroOr(prm.inOutData.Length));
                    logger.Error(errMsg);
                    SystemHelper.ShowErrorMessageBox(errMsg);
                    return false;
                } else if (result < 0) {
                    var errMsg = "Some error occured when Decoder called";
                    logger.Warn(errMsg);
                    SystemHelper.ShowWarningMessageBox(errMsg);
                }
            }
            logger.Info("LEAVE");
            return true;
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
        private int deckeyTotalCount = 0;

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

            keDispatcher.OnKeyDown = OnKeyboardDownHandler;
            keDispatcher.OnKeyUp = OnKeyboardUpHandler;
            keDispatcher.ToggleDecoder = ToggleDecoder;
            keDispatcher.ActivateDecoder = ActivateDecoder;
            keDispatcher.DeactivateDecoder = DeactivateDecoder;
            keDispatcher.IsDecoderActivated = isDecoderActivated;
            keDispatcher.FuncDispatcher = FuncDispatcher;
            keDispatcher.SendInputVkeyWithMod = SendInputVkeyWithMod;
            //keDispatcher.RotateReverseStrokeHelp = rotateReverseStrokeHelp;
            //keDispatcher.RotateDateString = rotateDateString;
            //keDispatcher.RotateReverseDateString = rotateReverseDateString;
            //keDispatcher.InvokeDecoder = InvokeDecoder;

            // キーボードイベントのディスパッチ開始
            keDispatcher.InstallKeyboardHook();
            logger.InfoH("LEAVE");
        }

        /// <summary>
        /// UI側のハンドラー
        /// </summary>
        /// <param name="deckey"></param>
        /// <returns></returns>
        private bool FuncDispatcher(int deckey, uint mod)
        {
            bool bPrevDtUpdate = false;
            try {
                if (IsDecoderActive) {
                    renewSaveDictsPlannedDt();
                }
                switch (deckey) {
                    case DecoderKeys.STROKE_HELP_ROTATION_DECKEY:
                        return rotateStrokeHelp(1);
                    case DecoderKeys.STROKE_HELP_UNROTATION_DECKEY:
                        return rotateStrokeHelp(-1);
                    case DecoderKeys.DATE_STRING_ROTATION_DECKEY:
                        return !isActiveWinExcel() && rotateDateString(1);
                    case DecoderKeys.DATE_STRING_UNROTATION_DECKEY:
                        return !isActiveWinExcel() && rotateDateString(-1);
                    case DecoderKeys.BUSHU_COMP_HELP:
                        ShowBushuCompHelp();
                        return true;
                    case DecoderKeys.TOGGLE_ROMAN_STROKE_GUIDE:
                        if (IsDecoderActive) {
                            rotateStrokeHelp(0);
                            bRomanStrokeGuideMode = !bRomanStrokeGuideMode && !bRomanMode;
                            drawRomanOrHiraganaMode(bRomanStrokeGuideMode, false);
                        }
                        return true;
                    case DecoderKeys.TOGGLE_UPPER_ROMAN_STROKE_GUIDE:
                        if (IsDecoderActive) {
                            rotateStrokeHelp(0);
                            bUpperRomanStrokeGuideMode = !bUpperRomanStrokeGuideMode && !bRomanMode;
                            if (!bRomanMode) drawRomanOrHiraganaMode(bUpperRomanStrokeGuideMode, false);
                        }
                        return true;
                    case DecoderKeys.TOGGLE_HIRAGANA_STROKE_GUIDE:
                        if (IsDecoderActive) {
                            rotateStrokeHelp(0);
                            bHiraganaStrokeGuideMode = !bHiraganaStrokeGuideMode;
                            if (bHiraganaStrokeGuideMode) {
                                InvokeDecoder(DecoderKeys.FULL_ESCAPE_DECKEY, 0);   // やっぱり出力文字列をクリアしておく必要あり
                                //ExecCmdDecoder("setHiraganaBlocker", null);       // こっちだと、以前のひらがなが出力文字列に残ったりして、それを拾ってしまう
                            } else {
                                //HandleDeckeyDecoder(decoderPtr, DecoderKeys.FULL_ESCAPE_DECKEY, 0, false, ref decoderOutput); // こっちだと、見えなくなるだけで、ひらがな列が残ってしまう
                                ExecCmdDecoder("clearTailHiraganaStr", null);   // 物理的に読みのひらがな列を削除しておく必要あり
                            }
                            drawRomanOrHiraganaMode(false, bHiraganaStrokeGuideMode);
                        }
                        return true;
                    case DecoderKeys.EXCHANGE_CODE_TABLE_DECKEY:
                        logger.Info("EXCHANGE_CODE_TABLE");
                        if (IsDecoderActive && DecoderOutput.IsWaitingFirstStroke()) {
                            ExecCmdDecoder("exchangeCodeTable", null);  // 漢直コードテーブルの入れ替え
                            frmVkb.DrawVirtualKeyboardChars();
                        }
                        return true;
                    case DecoderKeys.PSEUDO_SPACE_DECKEY:
                        logger.Info(() => $"PSEUDO_SPACE_DECKEY: strokeCount={decoderOutput.GetStrokeCount()}");
                        deckey = DecoderKeys.STROKE_SPACE_DECKEY;
                        if (IsDecoderActive && decoderOutput.GetStrokeCount() >= 1) {
                            // 第2打鍵待ちなら、スペースを出力
                            InvokeDecoder(deckey, 0);
                        }
                        return true;
                    case DecoderKeys.POST_NORMAL_SHIFT_DECKEY:
                    case DecoderKeys.POST_PLANE_A_SHIFT_DECKEY:
                    case DecoderKeys.POST_PLANE_B_SHIFT_DECKEY:
                        logger.Info(() => $"POST_PLANE_X_SHIFT_DECKEY:{deckey}, strokeCount={decoderOutput.GetStrokeCount()}");
                        if (IsDecoderActive && decoderOutput.GetStrokeCount() >= 1) {
                            // 第2打鍵待ちなら、いったんBSを出力してからシフトされたコードを出力
                            InvokeDecoder(DecoderKeys.BS_DECKEY, 0);
                            deckey = (prevDeckey % DecoderKeys.NORMAL_DECKEY_NUM) + (deckey - DecoderKeys.POST_NORMAL_SHIFT_DECKEY + 1) * DecoderKeys.SHIFT_DECKEY_NUM;
                            InvokeDecoder(deckey, 0);
                        }
                        return true;
                    case DecoderKeys.COPY_SELECTION_AND_SEND_TO_DICTIONARY_DECKEY:
                        logger.Info(() => $"COPY_SELECTION_AND_SEND_TO_DICTIONARY:{deckey}");
                        copySelectionAndSendToDictionary();
                        return true;
                    default:
                        bPrevDtUpdate = true;
                        if (IsDecoderActive && (deckey < DecoderKeys.DECKEY_CTRL_A || deckey > DecoderKeys.DECKEY_CTRL_Z)) {
                            return InvokeDecoder(deckey, mod);
                        } else {
                            return sendVkeyFromDeckey(deckey, mod);
                        }
                }
            } finally {
                prevDeckey = deckey;
                if (bPrevDtUpdate) prevDecDt = DateTime.Now;
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
            string activeWinClassName = actWinHandler.ActiveWinClassName._toLower();
            bool contained = activeWinClassName._notEmpty()
                && Settings.CtrlKeyTargetClassNames._notEmpty()
                && Settings.CtrlKeyTargetClassNames.Any(name => name._notEmpty() && activeWinClassName.StartsWith(name));
            bool ctrlTarget = !(Settings.UseClassNameListAsInclusion ^ contained);
            if (Settings.LoggingDecKeyInfo && Logger.IsInfoEnabled) {
                logger.InfoH($"ctrlTarget={ctrlTarget} (=!({Settings.UseClassNameListAsInclusion} (Inclusion) XOR {contained} (ContainedInList)");
            }
            return ctrlTarget;
        }

        private bool isActiveWinExcel()
        {
            return actWinHandler.ActiveWinClassName._startsWith("EXCEL");
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
                actWinHandler.SendVKeyCombo(VirtualKeys.CtrlC_VKeyCombo.modifier, VirtualKeys.CtrlC_VKeyCombo.vkey, 1);
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
                    ExecCmdDecoder("addHistEntry", str);
                }
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
                bool leftCtrl, rightCtrl;
                actWinHandler.GetCtrlKeyState(out leftCtrl, out rightCtrl);
                DeactivateDecoder();
                if (bRevertCtrl) actWinHandler.RevertUpCtrlKey(leftCtrl, rightCtrl);
            }
            logger.InfoH("LEAVE");
        }

        private void ActivateDecoder()
        {
            logger.InfoH(() => $"\nENTER");
            IsDecoderActive = true;
            try {
                prevDeckey = -1;
                if (frmSplash != null) closeSplash();
                if (decoderPtr != IntPtr.Zero) ResetDecoder(decoderPtr);
                decoderOutput.layout = 0;   // None にリセットしておく。これをやらないと仮想鍵盤モードを切り替えたときに以前の履歴選択状態が残ったりする
                CommonState.CenterString = "";
                Settings.VirtualKeyboardShowStrokeCountTemp = 0;
                bHiraganaStrokeGuideMode = false;
                bRomanStrokeGuideMode = false;
                frmVkb.DrawInitailVkb();
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
                    frmVkb.SetTopText(actWinHandler.ActiveWinClassName);
                    ShowFrmVkb();       // Show NonActive
                                        //} else if (Settings.ModeMarkerShowIntervalSec > 0) {
                                        //    ShowFrmMode();      // Show NonActive
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
        public void DeactivateDecoder()
        {
            logger.InfoH(() => $"\nENTER");
            IsDecoderActive = false;
            handleKeyDecoder(DecoderKeys.DEACTIVE_DECKEY, 0);   // DecoderOff の処理をやる
            actWinHandler.UpCtrlAndShftKeys();                  // CtrlとShiftキーをUP状態に戻す
            frmVkb.Hide();
            frmMode.Hide();
            notifyIcon1.Icon = Properties.Resources.kanmini0;
            frmMode.SetKanjiMode();
            if (Settings.VirtualKeyboardShowStrokeCount != 1) {
                frmMode.SetAlphaMode();
            }
            logger.InfoH("LEAVE");
        }

        /// <summary>仮想鍵盤の表示位置を移動する</summary>
        public void MoveFormVirtualKeyboard()
        {
            logger.InfoH("CALLED");
            actWinHandler?.MoveWindow();
        }

        public void ShowStrokeHelp(string w)
        {
            ExecCmdDecoder("showStrokeHelp", w);
            // 仮想キーボードにヘルプや文字候補を表示
            getCenterString();
            frmVkb.DrawVirtualKeyboardChars();
        }

        public void ShowBushuCompHelp()
        {
            ExecCmdDecoder("showBushuCompHelp", CommonState.CenterString);
            // 仮想キーボードにヘルプや文字候補を表示
            getCenterString();
            frmVkb.DrawVirtualKeyboardChars();
        }

        /// <summary> 仮想鍵盤のストローク表を切り替える </summary>
        /// <param name="delta"></param>
        public void RotateStrokeTable(int delta)
        {
            logger.InfoH(() => $"CALLED: delta={delta}");
            if (delta == 0) delta = 1;
            frmVkb.RotateStrokeTable(delta);
        }

        /// <summary> 辞書ファイルなどの保存 </summary>
        public void SaveAllFiles()
        {
            //// 出力文字カウントをファイルに出力
            //actWinHandler.WriteCharCountFile(CharCountFile);
            // デコーダが使用する辞書ファイルの保存
            ExecCmdDecoder("saveDictFiles", null);
        }

        //------------------------------------------------------------------
        // 各種下請け処理
        //------------------------------------------------------------------
        private async Task initializeKanchoku()
        {
            logger.Info("ENTER");

            // アクティブなウィンドウのハンドラ作成
            actWinHandler = new ActiveWindowHandler(this, frmVkb, frmMode);

            //Settings.ReadIniFile();
            //frmVkb.SetNormalCellBackColors();

            //// 文字出力カウントファイルの読み込み
            //CharCountFile = KanchokuIni.Singleton.MakeFullPath("char_count.txt");
            //actWinHandler.ReadCharCountFile(CharCountFile);

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
                logger.Info("ENTER");
                decoderPtr = CreateDecoder(Logger.LogLevel);         // UI側のLogLevelに合わせる

                //ExecCmdDecoder(null, KanchokuIni.Singleton.IniFilePath, true);
                if (!ExecCmdDecoder(null, Settings.SerializedDecoderSettings, true)) {
                    logger.Error($"CLOSE: Decoder initialize error");
                    //PostMessage(this.Handle, WM_Defs.WM_CLOSE, 0, 0);
                    this._invoke(() => this.Close());
                    return false;
                }

                // キー入力時のデコーダから出力情報を保持する構造体インスタンスを確保
                decoderOutput = new DecoderOutParams();

                logger.Info("LEAVE");
                return true;
            });
        }

        // デコーダの終了
        private void finalizeDecoder()
        {
            logger.Info("ENTER");
            if (decoderPtr != IntPtr.Zero) {
                if (!bNoSaveDicts) {
                    logger.Info("CALL SaveDictsDecoder");
                    SaveDictsDecoder(decoderPtr);
                }
                logger.Info("CALL FinalizeDecoder");
                FinalizeDecoder(decoderPtr);
                decoderPtr = IntPtr.Zero;
            }

            logger.Info("LEAVE");
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
                ++deckeyTotalCount;
                logger.InfoH(() => $"\nRECEIVED deckey={(deckey < DecoderKeys.SPECIAL_DECKEY_ID_BASE ? $"{deckey}" : $"{deckey:x}H")}, totalCount={deckeyTotalCount}");

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
                if ((Settings.UseCtrlJasEnter && VirtualKeys.GetCtrlDecKeyOf("J") == deckey) /*|| (Settings.UseCtrlMasEnter && VirtualKeys.GetCtrlDecKeyOf("M") == deckey)*/) {
                    deckey = DecoderKeys.ENTER_DECKEY;
                }

                // ActivateDecoderの処理中ではない
                // 入力標識の消去
                frmMode.Vanish();
                // 通常のストロークキーまたは機能キー(BSとか矢印キーとかCttrl-Hとか)
                bool flag = handleKeyDecoder(deckey, mod);
                logger.InfoH($"LEAVE");
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
                }
            }
        }

        /// <summary>
        /// デコーダの呼び出し
        /// </summary>
        private bool handleKeyDecoder(int deckey, uint mod)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"ENTER: deckey={deckey:x}H({deckey}), mod={mod:x}");

            getTargetChar(deckey);

            // デコーダの呼び出し
            HandleDeckeyDecoder(decoderPtr, deckey, targetChar, bRomanStrokeGuideMode, ref decoderOutput);

            logger.Info(() =>
                $"HandleDeckeyDecoder: RESULT: layout={decoderOutput.layout}, numBS={decoderOutput.numBackSpaces}, resultFlags={decoderOutput.resultFlags:x}H, " +
                $"output={decoderOutput.outString._toString()}, IsDeckeyToVkey={decoderOutput.IsDeckeyToVkey()}, nextStrokeDeckey={decoderOutput.nextStrokeDeckey}");

            // 第1打鍵待ち状態になったら、一時的な仮想鍵盤表示カウントをリセットする
            //if (decoderOutput.GetStrokeCount() < 1) Settings.VirtualKeyboardShowStrokeCountTemp = 0;

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
                ((Settings.UpperRomanStrokeGuide || bUpperRomanStrokeGuideMode) && decoderOutput.numBackSpaces == 0 && isUpperAlphabet(decoderOutput.outString[0]) && decoderOutput.outString[1] == 0)) {
                // ローマ字読みモード
                var romanStr = getTailRomanStr();
                logger.Info(() => $"romanStr={romanStr}");
                CommonState.CenterString = "ローマ字";
                candidateChars = KanjiYomiTable.GetCandidatesFromRoman(romanStr);
                candidateCharStrs = frmVkb.DrawStrokeHelp(candidateChars);
                frmVkb.SetTopText(decoderOutput.topString);
                targetChar = 0;
                bRomanMode = true;
            } else if (bHiraganaStrokeGuideMode) {
                CommonState.CenterString = "ひらがな";
                candidateChars = KanjiYomiTable.GetCandidates(getTailHiraganaStr());
                candidateCharStrs = frmVkb.DrawStrokeHelp(candidateChars);
                frmVkb.SetTopText(decoderOutput.topString);
                targetChar = 0;
            } else {
                if (decoderOutput.GetStrokeCount() < 1) {
                    // 第1打鍵待ちになった時のみ
                    // 一時的な仮想鍵盤表示カウントをリセットする
                    Settings.VirtualKeyboardShowStrokeCountTemp = 0;

                    // 他のVKey送出(もしあれば)
                    if (decoderOutput.IsDeckeyToVkey()) {
                        sendKeyFlag = sendVkeyFromDeckey(deckey, mod);
                        //nPreKeys += 1;
                    }

                    candidateCharStrs = null;
                    candidateChars = null;
                    targetChar = 0;

                    // 送出文字列中に特殊機能キー(tabやleftArrowなど)が含まれているか
                    bool bFuncVkeyContained = decoderOutput.outString._toString().IndexOf("!{") >= 0;
                    // BSと文字送出(もしあれば)
                    actWinHandler.SendStringViaClipboardIfNeeded(decoderOutput.outString, decoderOutput.numBackSpaces, bFuncVkeyContained);
                    if (bFuncVkeyContained) {
                        // 送出文字列中に特殊機能キー(tabやleftArrowなど)が含まれている場合は、 FULL_ESCAPE を実行してミニバッファをクリアしておく
                        HandleDeckeyDecoder(decoderPtr, DecoderKeys.FULL_ESCAPE_DECKEY, 0, false, ref decoderOutput);
                    }
                }

                // 仮想キーボードにヘルプや文字候補を表示
                frmVkb.DrawVirtualKeyboardChars(Settings.ShowLastStrokeByDiffBackColor && !bPrevMultiStrokeChar ? unshiftDeckey(deckey) : -1);

                bPrevMultiStrokeChar = decoderOutput.outString[0] == 0 && isNormalDeckey(deckey);

                if (bRomanMode || bUpperRomanStrokeGuideMode) {
                    bRomanMode = false;
                    bUpperRomanStrokeGuideMode = false;
                    ExecCmdDecoder("clearTailRomanStr", null);
                }
            }

            if (Settings.LoggingDecKeyInfo) logger.InfoH($"LEAVE");

            return sendKeyFlag;
        }

        private bool isNormalDeckey(int deckey)
        {
            return deckey >= DecoderKeys.NORMAL_DECKEY_START && deckey < DecoderKeys.NORMAL_DECKEY_END;
        }

        private int unshiftDeckey(int deckey)
        {
            return deckey < DecoderKeys.SHIFT_B_DECKEY_END ? deckey % DecoderKeys.SHIFT_DECKEY_NUM : deckey;
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
            if (isCtrlKeyConversionEffectiveWindow()
                || deckey < DecoderKeys.FUNC_DECKEY_START
                || deckey >= DecoderKeys.FUNC_DECKEY_END && deckey < DecoderKeys.CTRL_FUNC_DECKEY_START
                || deckey >= DecoderKeys.CTRL_FUNC_DECKEY_END && deckey < DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_START
                || deckey >= DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_END) {

                if (Settings.LoggingDecKeyInfo) logger.Info($"TARGET WINDOW: deckey={deckey:x}H({deckey})");
                var combo = VirtualKeys.GetVKeyComboFromDecKey(deckey);
                if (combo != null) {
                    if (Settings.LoggingDecKeyInfo) {
                        logger.Info($"SEND: combo.modifier={combo.Value.modifier:x}H({combo.Value.modifier}), "
                            + $"combo.vkey={combo.Value.vkey:x}H({combo.Value.vkey}), "
                            + $"ctrl={(combo.Value.modifier & KeyModifiers.MOD_CONTROL) != 0}, "
                            + $"mod={mod:x}H({mod})");
                    }
                    //if (deckey < DecoderKeys.FUNCTIONAL_DECKEY_ID_BASE) {
                    //    actWinHandler.SendVirtualKey(combo.Value.vkey, 1);
                    //} else {
                    //    actWinHandler.SendVKeyCombo(combo.Value, 1);
                    //}
                    actWinHandler.SendVKeyCombo((combo.Value.modifier != 0 ? combo.Value.modifier : mod), combo.Value.vkey, 1);
                    return true;
                }
            }
            return false;
        }

        /// <summary>修飾キー付きvkeyをSendInputする</summary>
        private bool SendInputVkeyWithMod(uint mod, uint vkey)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"CALLED: mod={mod}H({mod}), vkey={vkey}H({vkey})");
            actWinHandler.SendVKeyCombo(mod, vkey, 1);
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
            actWinHandler.SendStringViaClipboardIfNeeded(dtStr.ToCharArray(), prevDateStrLength);
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
            logger.Debug("ENTER");
            Terminate();
            logger.Debug("LEAVE");
        }

        private void Settings_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
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

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (--activeWinInfoCount <= 0) {
                if (!initialSettingsDialogOpened && frmSplash?.SettingsDialogOpenFlag == true) {
                    // スプラッシュウィンドウから設定ダイアログの初期起動を指示された
                    initialSettingsDialogOpened = true; // 今回限りの起動とする
                    openSettingsDialog(true);
                }
                actWinHandler?.GetActiveWindowInfo();
                activeWinInfoCount = Settings.GetActiveWindowInfoIntervalMillisec / timerInterval;
            }

            if (DateTime.Now >= saveDictsPlannedDt || (IsDecoderActive && DateTime.Now >= saveDictsChallengeDt)) {
                reinitializeSaveDictsChallengeDt();
                if (decoderPtr != IntPtr.Zero) {
                    logger.InfoH("CALL SaveDictsDecoder");
                    SaveDictsDecoder(decoderPtr);
                }
            }
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
            // キーボードハンドラの再初期化
            keDispatcher.Reinitialize();
            // 初期化
            VirtualKeys.Initialize();
            // キーボードファイルの読み込み
            if (!readKeyboardFile()) return;
            // 設定ファイルの読み込み
            Settings.ReadIniFile();
            // 各種定義ファイルの読み込み
            ReloadDefFiles();
            //ExecCmdDecoder("reloadSettings", Settings.SerializedDecoderSettings);
            reinitializeSaveDictsChallengeDt();
        }

        private void ReadBushuDic_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadBushuDic();
        }

        private void ReadMazeWikipediaDic_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReadMazegakiWikipediaDic();
        }

        private void ReloadSettings_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadSettingsAndDefFiles();
        }
    }

}

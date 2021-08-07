﻿using System;
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
        private async void DlgKanchoku_Load(object sender, EventArgs e)
        {
            logger.WriteLog("INFO", $"\n\n==== KANCHOKU WS START (LogLevel={Logger.LogLevel}) ====");

            // キーボードファイルの読み込み
            if (!VirtualKeys.ReadKeyboardFile()) {
                // キーボードファイルを読み込めなかったので終了する
                logger.Error($"CLOSE: Can't read keyboard file");
                //DecKeyHandler.Destroy();
                //PostMessage(this.Handle, WM_Defs.WM_CLOSE, 0, 0);
                this.Close();
                return;
            }

            // 設定ファイルの読み込み
            Settings.ReadIniFile();

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

            // タイマー開始
            timer1.Interval = timerInterval;
            timer1.Start();
            logger.Info("Timer Started");

            // キーボードイベントのディスパッチ開始
            initializeKeyboardEventDispatcher();
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
            actWinHandler.Dispose();
            finalizeDecoder();
            frmMode.Close();
            frmVkb.Close();

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
        static extern void HandleDeckeyDecoder(IntPtr decoder, int keyId, ref DecoderOutParams outParams);

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
            if (decoderOutput.IsZenkakuModeMarkerShow()) {
                frmMode.SetZenkakuMode();
            } else if (decoderOutput.IsZenkakuModeMarkerClear()) {
                frmMode.SetKanjiMode();
            } else if (Settings.KanjiModeMarkerShowIntervalSec == 0) {
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

        private bool BackspaceBlockerSent = false;

        /// <summary> Decoder の ON/OFF 状態 </summary>
        public bool IsDecoderActive { get; private set; } = false;

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
            keDispatcher.FuncDispatcher = FuncDispatcher;
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
        private bool FuncDispatcher(int deckey)
        {
            try {
                switch (deckey) {
                    case DecoderKeys.STROKE_HELP_ROTATION_DECKEY:
                        return rotateStrokeHelp(1);
                    case DecoderKeys.STROKE_HELP_UNROTATION_DECKEY:
                        return rotateStrokeHelp(-1);
                    case DecoderKeys.DATE_STRING_ROTATION_DECKEY:
                        return rotateDateString(1);
                    case DecoderKeys.DATE_STRING_UNROTATION_DECKEY:
                        return rotateDateString(-1);
                    default:
                        return InvokeDecoder(deckey);
                }
            } finally {
                prevDeckey = deckey;
                prevDecDt = DateTime.Now;
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

        ///// <summary>
        ///// WndProc のオーバーライド<br/>
        ///// </summary>
        ///// <param name="m"></param>
        ////protected override void WndProc(ref Message m)
        //private void wndProc(ref Message m)
        //{
        //    int msg = m.Msg;
        //    //if (Logger.IsTraceEnabled) {
        //    //    logger.Trace($"Msg={msg:x}H, wParam={(int)m.WParam:x}H({(int)m.WParam:x}H)");
        //    //}
        //    if (msg == -1) {
        //        int deckey = (int)m.WParam;
        //        try {
        //            if (deckey >= 0 && deckey < DecoderKeys.GLOBAL_DECKEY_ID_BASE) {
        //                // deckey の変換
        //                deckey = convertSpecificDeckey(deckey);
        //                if (deckey >= 0) {
        //                    if (deckey == DecoderKeys.DATE_STRING_ROTATION_DECKEY || deckey == DecoderKeys.DATE_STRING_UNROTATION_DECKEY) {
        //                        // Ctrl+; -- 日付の出力
        //                        postTodayDate(deckey);
        //                    } else if (IsDecoderActive) {
        //                        // Decoder ON
        //                        // 入力標識の消去
        //                        frmMode.Vanish();
        //                        // 通常のストロークキーまたは機能キー(BSとか矢印キーとかCttrl-Hとか)
        //                        handleKeyDecoder(deckey);
        //                    } else {
        //                        // Decoder OFF
        //                        if (deckey == DecoderKeys.FULL_ESCAPE_DECKEY) {
        //                            // ここではとくに何もしない(この後 prevDeckey が FULL_ESCAPE_DECKEY になることで、DATE_STRING などの処理は初期化されるため)
        //                        } else {
        //                            switch (deckey) {
        //                                case DecoderKeys.DECKEY_B: actWinHandler.SendVirtualKey((uint)Keys.B, 1); break;
        //                                case DecoderKeys.DECKEY_F: actWinHandler.SendVirtualKey((uint)Keys.F, 1); break;
        //                                case DecoderKeys.DECKEY_H: actWinHandler.SendVirtualKey((uint)Keys.H, 1); break;
        //                                case DecoderKeys.DECKEY_N: actWinHandler.SendVirtualKey((uint)Keys.N, 1); break;
        //                                case DecoderKeys.DECKEY_P: actWinHandler.SendVirtualKey((uint)Keys.P, 1); break;
        //                                default: postVkeyFromDeckey(deckey); break;
        //                            }
        //                        }
        //                    }
        //                    if (Settings.DelayAfterProcessDeckey) {
        //                        //Task.Delay(1000).Wait();
        //                        Helper.WaitMilliSeconds(1000);
        //                        logger.InfoH("OK");
        //                    }
        //                }
        //            } else {
        //                switch (deckey) {
        //                    case DecoderKeys.ACTIVE_DECKEY:
        //                    case DecoderKeys.ACTIVE2_DECKEY:
        //                    case DecoderKeys.DEACTIVE_DECKEY:
        //                    case DecoderKeys.DEACTIVE2_DECKEY:
        //                        ToggleActiveState();
        //                        break;
        //                    case DecoderKeys.DATE_STRING_ROTATION_DECKEY:
        //                    case DecoderKeys.DATE_STRING_UNROTATION_DECKEY:
        //                        // Ctrl+; -- 日付の出力
        //                        postTodayDate(deckey);
        //                        break;
        //                    case DecoderKeys.STROKE_HELP_ROTATION_DECKEY:
        //                    case DecoderKeys.STROKE_HELP_UNROTATION_DECKEY:
        //                        // 入力標識の消去
        //                        frmMode.Vanish();
        //                        // 仮想鍵盤のヘルプ表示の切り替え(モード標識表示時なら一時的に仮想鍵盤表示)
        //                        int effectiveCnt = Settings.VirtualKeyboardShowStrokeCountEffective;
        //                        Settings.VirtualKeyboardShowStrokeCountTemp = 1;
        //                        frmVkb.RotateStrokeTable(effectiveCnt != 1 ? 0 : deckey == DecoderKeys.STROKE_HELP_ROTATION_DECKEY ? 1 : -1);
        //                        break;
        //                }
        //            }
        //        } finally {
        //        }
        //        prevProcEndDt = DateTime.Now;
        //    }
        //}

        private int convertSpecificDeckey(int deckey)
        {
            string activeWinClassName = actWinHandler.ActiveWinClassName._toLower();
            bool contained = activeWinClassName._notEmpty() && Settings.CtrlKeyTargetClassNames._notEmpty() && Settings.CtrlKeyTargetClassNames.Any(name => name._notEmpty() && activeWinClassName.StartsWith(name));
            bool ctrlTarget = !(Settings.UseClassNameListAsInclusion ^ contained);

            bool leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
            bool rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;

            bool leftCtrlOn = leftCtrl && Settings.UseLeftControlToConversion;
            bool rightCtrlOn = rightCtrl && Settings.UseRightControlToConversion;
            bool ctrlKeyOn = leftCtrlOn || rightCtrlOn;
            bool ctrlKeyAndTargetOn = ctrlTarget && ctrlKeyOn;

            bool bCtrlHConvert = ctrlKeyAndTargetOn && Settings.ConvertCtrlHtoBackSpaceEffective;
            bool bCtrlBFNPConvert = ctrlKeyAndTargetOn && Settings.ConvertCtrlBFNPtoArrowKeyEffective;
            bool bCtrlAConvert = ctrlKeyAndTargetOn && Settings.ConvertCtrlAtoHomeEffective;
            bool bCtrlDConvert = ctrlKeyAndTargetOn && Settings.ConvertCtrlDtoDeleteEffective;
            bool bCtrlEConvert = ctrlKeyAndTargetOn && Settings.ConvertCtrlEtoEndEffective;
            bool bCtrlGConvert = ctrlKeyAndTargetOn && Settings.ConvertCtrlGtoEscape;
            bool bCtrlJConvert = ctrlKeyAndTargetOn && Settings.UseCtrlJasEnter;
            bool bCtrlMConvert = ctrlKeyAndTargetOn && Settings.UseCtrlMasEnter;
            bool bCtrlSemiColonConvert = ctrlKeyOn && Settings.ConvertCtrlSemiColonToDateEffective;

            if (Settings.LoggingDecKeyInfo && Logger.IsInfoEnabled) {
                logger.InfoH($"activeWindowClassName={activeWinClassName}");
                logger.InfoH($"CtrlKeyTargetClassNames={Settings.CtrlKeyTargetClassNames._join(",")}");
                logger.InfoH($"ctrlTarget={ctrlTarget} (=!({Settings.UseClassNameListAsInclusion}(Inclusion) XOR {contained} (ContainedInList)");
                logger.InfoH($"leftCtrlOn={leftCtrlOn} (={leftCtrl}(leftCtrl) AND {Settings.UseLeftControlToConversion}(UseLeftCtrl))");
                logger.InfoH($"rightCtrlOn={rightCtrlOn} (={rightCtrl}(rightCtrl) AND {Settings.UseRightControlToConversion}(UseRightCtrl))");
                logger.InfoH($"ctrlKeyOn={ctrlKeyOn}");
                logger.InfoH($"ctrlKeyAndTargetOn={ctrlKeyAndTargetOn} (={ctrlTarget}(ctrlTarget) AND {ctrlKeyOn}(ctrlKeyOn))");
                logger.InfoH($"ctrlH={bCtrlHConvert} (ConvertCtrlHtoBackSpace={Settings.ConvertCtrlHtoBackSpaceEffective})");
                logger.InfoH($"ctrlBFNP={bCtrlBFNPConvert} (ConvertCtrlBFNPtoArrowKey={Settings.ConvertCtrlBFNPtoArrowKeyEffective})");
                logger.InfoH($"ctrlA={bCtrlAConvert} (ConvertCtrlAtoHome={Settings.ConvertCtrlAtoHomeEffective})");
                logger.InfoH($"ctrlD={bCtrlDConvert} (ConvertCtrlDtoDelete={Settings.ConvertCtrlDtoDeleteEffective})");
                logger.InfoH($"ctrlE={bCtrlEConvert} (ConvertCtrlEtoEnd={Settings.ConvertCtrlEtoEndEffective})");
                logger.InfoH($"ctrlG={bCtrlGConvert} (ConvertCtrlGtoEscape={Settings.ConvertCtrlGtoEscape})");
                logger.InfoH($"ctrlJ={bCtrlJConvert} (ConvertCtrlJtoEscape={Settings.UseCtrlJasEnter})");
                logger.InfoH($"ctrlM={bCtrlMConvert} (ConvertCtrlMtoEscape={Settings.UseCtrlMasEnter})");
                logger.InfoH($"ctrlSemi={bCtrlSemiColonConvert} (ConvertCtrlEtoEnd={Settings.ConvertCtrlEtoEndEffective})");
            }

            switch (deckey) {
                case DecoderKeys.CTRL_H_DECKEY:
                    if (bCtrlHConvert) deckey = DecoderKeys.BS_DECKEY;
                    break;
                case DecoderKeys.CTRL_B_DECKEY:
                    if (bCtrlBFNPConvert) deckey = DecoderKeys.LEFT_ARROW_DECKEY;
                    break;
                case DecoderKeys.CTRL_F_DECKEY:
                    if (bCtrlBFNPConvert) deckey = DecoderKeys.RIGHT_ARROW_DECKEY;
                    break;
                case DecoderKeys.CTRL_N_DECKEY:
                    if (bCtrlBFNPConvert) deckey = DecoderKeys.DOWN_ARROW_DECKEY;
                    break;
                case DecoderKeys.CTRL_P_DECKEY:
                    if (bCtrlBFNPConvert) deckey = DecoderKeys.UP_ARROW_DECKEY;
                    break;
                case DecoderKeys.CTRL_A_DECKEY:
                    if (bCtrlAConvert) deckey = DecoderKeys.HOME_DECKEY;
                    break;
                case DecoderKeys.CTRL_D_DECKEY:
                    if (bCtrlDConvert) deckey = DecoderKeys.DEL_DECKEY;
                    break;
                case DecoderKeys.CTRL_E_DECKEY:
                    if (bCtrlEConvert) deckey = DecoderKeys.END_DECKEY;
                    break;
                //case DecoderKeys.CTRL_G_DECKEY:
                //    if (bCtrlGConvert) deckey = DecoderKeys.FULL_ESCAPE_DECKEY;
                //    break;
                //case DecoderKeys.CTRL_SHIFT_G_DECKEY:
                //    if (bCtrlGConvert) deckey = DecoderKeys.UNBLOCK_DECKEY;
                //    break;
                case DecoderKeys.CTRL_J_DECKEY:
                    if (bCtrlJConvert) deckey = DecoderKeys.ENTER_DECKEY;
                    break;
                case DecoderKeys.CTRL_M_DECKEY:
                    if (bCtrlMConvert) deckey = DecoderKeys.ENTER_DECKEY;
                    break;
                //case DecoderKeys.CTRL_SEMICOLON_DECKEY:         // 日付フォーマットの切り替え
                //case DecoderKeys.CTRL_SHIFT_SEMICOLON_DECKEY:   // 日付フォーマットの切り替え(2つめのフォーマットから)
                //case DecoderKeys.CTRL_COLON_DECKEY:             // 日付のインクリメント
                //case DecoderKeys.CTRL_SHIFT_COLON_DECKEY:       // 日付のデクリメント
                //    if (bCtrlSemiColonConvert) {
                //        deckey = convertDateStringDeckey(deckey);
                //        if (Settings.LoggingDecKeyInfo) logger.InfoH($"Convert Deckey-Ctrl-(Shift)-(Semi)Colorn to Deckey{deckey:x}");
                //    }
                //    break;
            }
            // 連続的に Ctrlキー(Ctrl-Hなど)または特殊キー(BSなど)(これは AutoDecKey などにより Ctrl-H がBSに変換されていることを想定)が送りつけられている時に
            if ((prevDeckey >= DecoderKeys.CTRL_FUNC_DECKEY_ID_BASE) && DateTime.Now < prevDecDt.AddMilliseconds(Settings.KeyRepeatDetectMillisec)) {
                switch (deckey) {
                    case DecoderKeys.DECKEY_H:
                    case DecoderKeys.BS_DECKEY:
                        if (deckey != DecoderKeys.BS_DECKEY) {
                            if (Settings.LoggingDecKeyInfo) logger.InfoH($"Convert Deckey-H or Ctrl-H to Deckey-BS");
                            if (bCtrlHConvert) deckey = DecoderKeys.BS_DECKEY;
                        }
                        if (!BackspaceBlockerSent) {
                            // Backspace だったら、デコーダに対して BackspaceBlocker を送る
                            if (Settings.LoggingDecKeyInfo) logger.InfoH("setBackspaceBlocker");
                            ExecCmdDecoder("setBackspaceBlocker", null);
                            BackspaceBlockerSent = true;
                        }
                        break;
                    case DecoderKeys.DECKEY_B: // B
                        if (Settings.LoggingDecKeyInfo) logger.InfoH($"Convert Deckey-B or Ctrl-B to Deckey-LeftArrow");
                        if (bCtrlBFNPConvert) deckey = DecoderKeys.LEFT_ARROW_DECKEY;
                        break;
                    case DecoderKeys.DECKEY_F: // F
                        if (Settings.LoggingDecKeyInfo) logger.InfoH($"Convert Deckey-F or Ctrl-F to Deckey-RightArrow");
                        if (bCtrlBFNPConvert) deckey = DecoderKeys.RIGHT_ARROW_DECKEY;
                        break;
                    case DecoderKeys.DECKEY_N: // N
                        if (Settings.LoggingDecKeyInfo) logger.InfoH($"Convert Deckey-N or Ctrl-N to Deckey-DownArrow");
                        if (bCtrlBFNPConvert) deckey = DecoderKeys.DOWN_ARROW_DECKEY;
                        break;
                    case DecoderKeys.DECKEY_P: // P
                        if (Settings.LoggingDecKeyInfo) logger.InfoH($"Convert Deckey-P or Ctrl-P to Deckey-UpArrow");
                        if (bCtrlBFNPConvert) deckey = DecoderKeys.UP_ARROW_DECKEY;
                        break;
                    case DecoderKeys.DECKEY_A: // A
                        if (Settings.LoggingDecKeyInfo) logger.InfoH($"Convert Deckey-A or Ctrl-A to Deckey-Home");
                        if (bCtrlAConvert) deckey = DecoderKeys.HOME_DECKEY;
                        break;
                    case DecoderKeys.DECKEY_D: // D
                        if (Settings.LoggingDecKeyInfo) logger.InfoH($"Convert Deckey-D or Ctrl-D to Deckey-Delete");
                        if (bCtrlDConvert) deckey = DecoderKeys.DEL_DECKEY;
                        break;
                    case DecoderKeys.DECKEY_E: // E
                        if (Settings.LoggingDecKeyInfo) logger.InfoH($"Convert Deckey-E or Ctrl-E to Deckey-End");
                        if (bCtrlEConvert) deckey = DecoderKeys.END_DECKEY;
                        break;
                    default:
                        if (deckey < DecoderKeys.CTRL_FUNC_DECKEY_ID_BASE) {
                            // その他のストロークキーは、AHKなどの変換間違いの可能性が高いので無視する
                            if (Settings.LoggingDecKeyInfo) logger.InfoH($"IGNORE: {(deckey < DecoderKeys.CTRL_FUNC_DECKEY_ID_BASE ? $"{deckey}" : $"{deckey:x}H")}");
                            deckey = -1;
                        }
                        // コントロールキーや特殊キーはそのままデコーダに送る(矢印キーなどは候補選択に使うはずなので)
                        break;
                }
            } else {
                BackspaceBlockerSent = false;
            }

            prevDeckey = deckey;
            prevDecDt = DateTime.Now;
            return deckey;
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

        // 開発者用の設定がONになっているとき、漢直モードのON/OFFを10回繰り返したら警告を出す
        private int devFlagsOnWarningCount = 0;

        private void ToggleDecoder()
        {
            ToggleActiveState();
        }

        // アクティブと非アクティブを切り替える
        public void ToggleActiveState(bool bForceOff = false)
        {
            logger.InfoH(() => $"ENTER");
            if (!bForceOff && !IsDecoderActive) {
                ActivateDecoder();
            } else {
                DeactivateDecoder();
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
                frmVkb.DrawInitailVkb();
                //DecKeyHandler.RegisterDeactivateDecoderKeys();
                //DecKeyHandler.RegisterDecoderSpecialDecoderKeys();
                //DecKeyHandler.RegisterDecoderStrokeDecoderKeys();
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
                                msg = "ログレベルが INFOH 以上になっています。";
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
            handleKeyDecoder(DecoderKeys.ACTIVE_DECKEY);   // DecoderOff の処理をやる
            frmVkb.Hide();
            frmMode.Hide();
            notifyIcon1.Icon = Properties.Resources.kanmini0;
            //DecKeyHandler.UnregisterCandSelectDecoderKeys();        // 候補選択中に漢直モードをOFFにする可能性があるため
            handleArrowKeys(true);                          // 候補選択中に漢直モードをOFFにする可能性があるため、強制的に Unreg しておく
                                                            //DecKeyHandler.UnregisterDecoderSpecialDecoderKeys();
                                                            //DecKeyHandler.UnregisterDecoderStrokeDecoderKeys();
                                                            //DecKeyHandler.RegisterActivateDecoderKeys();
                                                            //Text = "漢直窓S - OFF";
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

        /// <summary> 仮想鍵盤のストローク表を切り替える </summary>
        /// <param name="delta"></param>
        public void RotateStrokeTable(int delta)
        {
            logger.InfoH(() => $"CALLED: delta={delta}");
            if (delta == 0) delta = 1;
            frmVkb.RotateStrokeTable(delta);
        }

        public void ReregisterSpecialGlobalDeckeys()
        {
            logger.InfoH("CALLED");
            DeactivateDecoder();
            //DecKeyHandler.UnregisterSpecialGlobalDecoderKeys();
            //DecKeyHandler.RegisterSpecialGlobalDecoderKeys();
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
                makeInitialVkbTable();
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
        private void makeInitialVkbTable()
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
        private bool InvokeDecoder(int deckey)
        {
            if (IsDecoderActive) {
                ++deckeyTotalCount;
                logger.InfoH(() => $"\nRECEIVED deckey={(deckey < DecoderKeys.CTRL_FUNC_DECKEY_ID_BASE ? $"{deckey}" : $"{deckey:x}H")}, totalCount={deckeyTotalCount}");

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
                if (prevDeckey >= DecoderKeys.CTRL_FUNC_DECKEY_ID_BASE && prevDecDt.AddMilliseconds(5) >= DateTime.Now) {
                    logger.InfoH("SKIP");
                    return false;
                }

                // ActivateDecoderの処理中ではない
                // 入力標識の消去
                frmMode.Vanish();
                // 通常のストロークキーまたは機能キー(BSとか矢印キーとかCttrl-Hとか)
                handleKeyDecoder(deckey);
                logger.InfoH($"LEAVE");
                return true;
            }
            return false;
        }

        /// <summary>
        /// デコーダの呼び出し
        /// </summary>
        private void handleKeyDecoder(int deckey)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"ENTER: deckey={deckey:x}H({deckey})");

            // デコーダの呼び出し
            HandleDeckeyDecoder(decoderPtr, deckey, ref decoderOutput);

            logger.Info(() => $"layout={decoderOutput.layout}, numBS={decoderOutput.numBackSpaces}, resultFlags={decoderOutput.resultFlags:x}H, output={decoderOutput.outString._toString()}");

            // 第1打鍵待ち状態になったら、一時的な仮想鍵盤表示カウントをリセットする
            if (decoderOutput.GetStrokeCount() < 1) Settings.VirtualKeyboardShowStrokeCountTemp = 0;

            // 中央鍵盤文字列の取得
            getCenterString();

            // 他のVKey送出(もしあれば)
            if (decoderOutput.IsDeckeyToVkey()) {
                postVkeyFromDeckey(deckey);
                //nPreKeys += 1;
            }

            // BSと文字送出(もしあれば)
           actWinHandler.SendStringViaClipboardIfNeeded(decoderOutput.outString, decoderOutput.numBackSpaces);

            // 仮想キーボードにヘルプや文字候補を表示
            frmVkb.DrawVirtualKeyboardChars();

            // 候補選択が必要なら矢印キーをホットキーにする
            handleArrowKeys();

            if (Settings.LoggingDecKeyInfo) logger.InfoH($"LEAVE");
        }

        private void postVkeyFromDeckey(int deckey)
        {
            var combo = VirtualKeys.GetVKeyComboFromDecKey(deckey);
            if (combo != null) {
                if (deckey < DecoderKeys.FUNCTIONAL_DECKEY_ID_BASE) {
                    actWinHandler.SendVirtualKey(combo.Value.vkey, 1);
                } else {
                    actWinHandler.SendVirtualKeys(combo.Value, 1);
                }
            }
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

        // 候補選択のための矢印キーをホットキーにする
        private bool AreArrowKeysDecKey = false;

        // UIスレッドから呼び出す必要あり
        private void handleArrowKeys(bool bForceUnreg = false)
        {
            bool regFlag = false;
            bool unregFlag = false;

            if (bForceUnreg) {
                unregFlag = true;
            } else if (decoderOutput.IsArrowKeysRequired()) {
                regFlag = !AreArrowKeysDecKey;
            } else {
                unregFlag = AreArrowKeysDecKey;
            }

            if (regFlag) {
                //DecKeyHandler.RegisterCandSelectDecoderKeys();
                AreArrowKeysDecKey = true;
                logger.Info("Register Arrow Keys");
            } else if (unregFlag) {
                //DecKeyHandler.UnregisterCandSelectDecoderKeys();
                AreArrowKeysDecKey = false;
                logger.Info("Unregister Arrow Keys");
            }
        }

        //------------------------------------------------------------------
        // イベントハンドラ
        //------------------------------------------------------------------
        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            logger.Info("CALLED");
            if (((MouseEventArgs)e).Button == MouseButtons.Right) {
                contextMenuStrip1.Show(Cursor.Position);
            } else {
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

        private int activeWinInfoCount = Settings.GetActiveWindowInfoIntervalMillisec / timerInterval;

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

        private void ReadBushuDic_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadBushuDic();
        }

        private void ReadMazeWikipediaDic_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReadMazegakiWikipediaDic();
        }
    }

}

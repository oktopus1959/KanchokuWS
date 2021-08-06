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

        //------------------------------------------------------------------
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FrmKanchoku()
        {
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

            KeyboardHookHandler.InstallKeyboardHook(this);

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
            HotKeyHandler.Destroy();
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

        /// <summary> Decoder へ入力HOTKEYキーを送信する </summary>
        /// <param name="decoder"></param>
        [DllImport("kw-uni.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void HandleHotkeyDecoder(IntPtr decoder, int keyId, ref DecoderOutParams outParams);

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
        private int hotkeyTotalCount = 0;

        private int prevRawHotkey = -1;
        private int rawHotkeyRepeatCount = 0;

        private int prevHotkey = -1;
        private DateTime prevHotDt;

        //private int prevDateHotkeyCount = 0;
        private int dateStrHotkeyCount = 0;
        private int dayOffset = 0;
        private int prevDateStrLength = 0;

        private bool BackspaceBlockerSent = false;

        private DateTime prevProcEndDt;

        /// <summary> Decoder の ON/OFF 状態 </summary>
        public bool IsDecoderActive { get; private set; } = false;

        /// <summary> Decoder処理中に受信した WM_HOTKEY を無視するためのフラグ </summary>
        private bool busyFlag = false;

        /// <summary> busy時に受信したホットキー </summary>
        private int busyHotkey = -1;

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
            return false;
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
            return false;
        }

        /// <summary>
        /// WndProc のオーバーライド<br/>
        ///  - WM_HOTKEY などを処理する
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            int msg = m.Msg;
            //if (Logger.IsTraceEnabled) {
            //    logger.Trace($"Msg={msg:x}H, wParam={(int)m.WParam:x}H({(int)m.WParam:x}H)");
            //}
            if (msg == WM_Defs.WM_HOTKEY) {
                int hotkey = (int)m.WParam;
                ++hotkeyTotalCount;
                logger.InfoH(() => $"\nRECEIVED hotkey={(hotkey < HotKeys.CTRL_FUNC_HOTKEY_ID_BASE ? $"{hotkey}" : $"{hotkey:x}H")}, totalCount={hotkeyTotalCount}");

                // HotKey 無限ループの検出
                if (Settings.HotkeyInfiniteLoopDetectCount > 0) {
                    if (hotkey == prevRawHotkey) {
                        ++rawHotkeyRepeatCount;
                        if ((rawHotkeyRepeatCount % 100) == 0) logger.InfoH(() => $"rawHotkeyRepeatCount={rawHotkeyRepeatCount}");
                        if (rawHotkeyRepeatCount > Settings.HotkeyInfiniteLoopDetectCount) {
                            logger.Error($"rawHotkeyRepeatCount exceeded threshold: hotkey={hotkey:x}H({hotkey}), count={rawHotkeyRepeatCount}, threshold={Settings.HotkeyInfiniteLoopDetectCount}");
                            logger.Warn("Force close");
                            this.Close();
                        }
                    } else {
                        prevRawHotkey = hotkey;
                        rawHotkeyRepeatCount = 0;
                    }
                }

                // 前回がCtrlキー修飾されたHotKeyで、その処理終了時刻の5ミリ秒以内に次のキーがきたら、それを無視する。
                // そうしないと、キー入力が滞留して、CtrlキーのプログラムによるUP/DOWN処理とユーザー操作によるそれとがコンフリクトする可能性が高まる
                if (prevHotkey >= HotKeys.CTRL_FUNC_HOTKEY_ID_BASE && prevProcEndDt.AddMilliseconds(5) >= DateTime.Now) {
                    logger.InfoH("SKIP");
                    return;
                }

                if (!busyFlag) {
                    busyFlag = true;
                    while (busyFlag) {
                        // ホットキーの処理中ではない
                        try {
                            if (hotkey >= 0 && hotkey < HotKeys.GLOBAL_HOTKEY_ID_BASE) {
                                // hotkey の変換
                                hotkey = convertSpecificHotkey(hotkey);
                                if (hotkey >= 0) {
                                    if (hotkey == HotKeys.DATE_STRING_HOTKEY1 || hotkey == HotKeys.DATE_STRING_HOTKEY2) {
                                        // Ctrl+; -- 日付の出力
                                        postTodayDate(hotkey);
                                    } else if (IsDecoderActive) {
                                        // Decoder ON
                                        // 入力標識の消去
                                        frmMode.Vanish();
                                        // 通常のストロークキーまたは機能キー(BSとか矢印キーとかCttrl-Hとか)
                                        handleKeyDecoder(hotkey);
                                    } else {
                                        // Decoder OFF
                                        if (hotkey == HotKeys.FULL_ESCAPE_HOTKEY) {
                                            // ここではとくに何もしない(この後 prevHotkey が FULL_ESCAPE_HOTKEY になることで、DATE_STRING などの処理は初期化されるため)
                                        } else {
                                            switch (hotkey) {
                                                case HotKeys.HOTKEY_B: actWinHandler.SendVirtualKey((uint)Keys.B, 1); break;
                                                case HotKeys.HOTKEY_F: actWinHandler.SendVirtualKey((uint)Keys.F, 1); break;
                                                case HotKeys.HOTKEY_H: actWinHandler.SendVirtualKey((uint)Keys.H, 1); break;
                                                case HotKeys.HOTKEY_N: actWinHandler.SendVirtualKey((uint)Keys.N, 1); break;
                                                case HotKeys.HOTKEY_P: actWinHandler.SendVirtualKey((uint)Keys.P, 1); break;
                                                default: postVkeyFromHotkey(hotkey); break;
                                            }
                                        }
                                    }
                                    if (Settings.DelayAfterProcessHotkey) {
                                        //Task.Delay(1000).Wait();
                                        Helper.WaitMilliSeconds(1000);
                                        logger.InfoH("OK");
                                    }
                                }
                            } else {
                                switch (hotkey) {
                                    case HotKeys.ACTIVE_HOTKEY:
                                    case HotKeys.ACTIVE2_HOTKEY:
                                    case HotKeys.INACTIVE_HOTKEY:
                                    case HotKeys.INACTIVE2_HOTKEY:
                                        ToggleActiveState();
                                        break;
                                    case HotKeys.DATE_STRING_HOTKEY1:
                                    case HotKeys.DATE_STRING_HOTKEY2:
                                        // Ctrl+; -- 日付の出力
                                        postTodayDate(hotkey);
                                        break;
                                    case HotKeys.STROKE_HELP_ROTATION_HOTKEY:
                                    case HotKeys.STROKE_HELP_UNROTATION_HOTKEY:
                                        // 入力標識の消去
                                        frmMode.Vanish();
                                        // 仮想鍵盤のヘルプ表示の切り替え(モード標識表示時なら一時的に仮想鍵盤表示)
                                        int effectiveCnt = Settings.VirtualKeyboardShowStrokeCountEffective;
                                        Settings.VirtualKeyboardShowStrokeCountTemp = 1;
                                        frmVkb.RotateStrokeTable(effectiveCnt != 1 ? 0 : hotkey == HotKeys.STROKE_HELP_ROTATION_HOTKEY ? 1 : -1);
                                        break;
                                }
                            }
                        } finally {
                            if (busyHotkey >= 0) {
                                hotkey = busyHotkey;
                                busyHotkey = -1;
                                logger.InfoH(() => $"Handle busyHotkey={hotkey}");
                            } else {
                                busyFlag = false;
                            }
                        }
                        prevProcEndDt = DateTime.Now;
                    }
                } else {
                    // ホットキーの処理中なので、スキップする
                    logger.InfoH(() => $"HOTKEY BUSY: hotkey={hotkey}");
                    busyHotkey = hotkey;
                }
                logger.InfoH($"LEAVE");
            }
        }

        private int convertSpecificHotkey(int hotkey)
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

            if (Settings.LoggingHotKeyInfo && Logger.IsInfoEnabled) {
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

            switch (hotkey) {
                case HotKeys.CTRL_H_HOTKEY:
                    if (bCtrlHConvert) hotkey = HotKeys.BS_HOTKEY;
                    break;
                case HotKeys.CTRL_B_HOTKEY:
                    if (bCtrlBFNPConvert) hotkey = HotKeys.LEFT_ARROW_HOTKEY;
                    break;
                case HotKeys.CTRL_F_HOTKEY:
                    if (bCtrlBFNPConvert) hotkey = HotKeys.RIGHT_ARROW_HOTKEY;
                    break;
                case HotKeys.CTRL_N_HOTKEY:
                    if (bCtrlBFNPConvert) hotkey = HotKeys.DOWN_ARROW_HOTKEY;
                    break;
                case HotKeys.CTRL_P_HOTKEY:
                    if (bCtrlBFNPConvert) hotkey = HotKeys.UP_ARROW_HOTKEY;
                    break;
                case HotKeys.CTRL_A_HOTKEY:
                    if (bCtrlAConvert) hotkey = HotKeys.HOME_HOTKEY;
                    break;
                case HotKeys.CTRL_D_HOTKEY:
                    if (bCtrlDConvert) hotkey = HotKeys.DEL_HOTKEY;
                    break;
                case HotKeys.CTRL_E_HOTKEY:
                    if (bCtrlEConvert) hotkey = HotKeys.END_HOTKEY;
                    break;
                //case HotKeys.CTRL_G_HOTKEY:
                //    if (bCtrlGConvert) hotkey = HotKeys.FULL_ESCAPE_HOTKEY;
                //    break;
                //case HotKeys.CTRL_SHIFT_G_HOTKEY:
                //    if (bCtrlGConvert) hotkey = HotKeys.UNBLOCK_HOTKEY;
                //    break;
                case HotKeys.CTRL_J_HOTKEY:
                    if (bCtrlJConvert) hotkey = HotKeys.ENTER_HOTKEY;
                    break;
                case HotKeys.CTRL_M_HOTKEY:
                    if (bCtrlMConvert) hotkey = HotKeys.ENTER_HOTKEY;
                    break;
                case HotKeys.CTRL_SEMICOLON_HOTKEY:         // 日付フォーマットの切り替え
                case HotKeys.CTRL_SHIFT_SEMICOLON_HOTKEY:   // 日付フォーマットの切り替え(2つめのフォーマットから)
                case HotKeys.CTRL_COLON_HOTKEY:             // 日付のインクリメント
                case HotKeys.CTRL_SHIFT_COLON_HOTKEY:       // 日付のデクリメント
                    if (bCtrlSemiColonConvert) {
                        hotkey = convertDateStringHotkey(hotkey);
                        if (Settings.LoggingHotKeyInfo) logger.InfoH($"Convert Hotkey-Ctrl-(Shift)-(Semi)Colorn to Hotkey{hotkey:x}");
                    }
                    break;
            }
            // 連続的に Ctrlキー(Ctrl-Hなど)または特殊キー(BSなど)(これは AutoHotKey などにより Ctrl-H がBSに変換されていることを想定)が送りつけられている時に
            if ((prevHotkey >= HotKeys.CTRL_FUNC_HOTKEY_ID_BASE) && DateTime.Now < prevHotDt.AddMilliseconds(Settings.KeyRepeatDetectMillisec)) {
                switch (hotkey) {
                    case HotKeys.HOTKEY_H:
                    case HotKeys.BS_HOTKEY:
                        if (hotkey != HotKeys.BS_HOTKEY) {
                            if (Settings.LoggingHotKeyInfo) logger.InfoH($"Convert Hotkey-H or Ctrl-H to Hotkey-BS");
                            if (bCtrlHConvert) hotkey = HotKeys.BS_HOTKEY;
                        }
                        if (!BackspaceBlockerSent) {
                            // Backspace だったら、デコーダに対して BackspaceBlocker を送る
                            if (Settings.LoggingHotKeyInfo) logger.InfoH("setBackspaceBlocker");
                            ExecCmdDecoder("setBackspaceBlocker", null);
                            BackspaceBlockerSent = true;
                        }
                        break;
                    case HotKeys.HOTKEY_B: // B
                        if (Settings.LoggingHotKeyInfo) logger.InfoH($"Convert Hotkey-B or Ctrl-B to Hotkey-LeftArrow");
                        if (bCtrlBFNPConvert) hotkey = HotKeys.LEFT_ARROW_HOTKEY;
                        break;
                    case HotKeys.HOTKEY_F: // F
                        if (Settings.LoggingHotKeyInfo) logger.InfoH($"Convert Hotkey-F or Ctrl-F to Hotkey-RightArrow");
                        if (bCtrlBFNPConvert) hotkey = HotKeys.RIGHT_ARROW_HOTKEY;
                        break;
                    case HotKeys.HOTKEY_N: // N
                        if (Settings.LoggingHotKeyInfo) logger.InfoH($"Convert Hotkey-N or Ctrl-N to Hotkey-DownArrow");
                        if (bCtrlBFNPConvert) hotkey = HotKeys.DOWN_ARROW_HOTKEY;
                        break;
                    case HotKeys.HOTKEY_P: // P
                        if (Settings.LoggingHotKeyInfo) logger.InfoH($"Convert Hotkey-P or Ctrl-P to Hotkey-UpArrow");
                        if (bCtrlBFNPConvert) hotkey = HotKeys.UP_ARROW_HOTKEY;
                        break;
                    case HotKeys.HOTKEY_A: // A
                        if (Settings.LoggingHotKeyInfo) logger.InfoH($"Convert Hotkey-A or Ctrl-A to Hotkey-Home");
                        if (bCtrlAConvert) hotkey = HotKeys.HOME_HOTKEY;
                        break;
                    case HotKeys.HOTKEY_D: // D
                        if (Settings.LoggingHotKeyInfo) logger.InfoH($"Convert Hotkey-D or Ctrl-D to Hotkey-Delete");
                        if (bCtrlDConvert) hotkey = HotKeys.DEL_HOTKEY;
                        break;
                    case HotKeys.HOTKEY_E: // E
                        if (Settings.LoggingHotKeyInfo) logger.InfoH($"Convert Hotkey-E or Ctrl-E to Hotkey-End");
                        if (bCtrlEConvert) hotkey = HotKeys.END_HOTKEY;
                        break;
                    default:
                        if (hotkey < HotKeys.CTRL_FUNC_HOTKEY_ID_BASE) {
                            // その他のストロークキーは、AHKなどの変換間違いの可能性が高いので無視する
                            if (Settings.LoggingHotKeyInfo) logger.InfoH($"IGNORE: {(hotkey < HotKeys.CTRL_FUNC_HOTKEY_ID_BASE ? $"{hotkey}" : $"{hotkey:x}H")}");
                            hotkey = -1;
                        }
                        // コントロールキーや特殊キーはそのままデコーダに送る(矢印キーなどは候補選択に使うはずなので)
                        break;
                }
            } else {
                BackspaceBlockerSent = false;
            }

            prevHotkey = hotkey;
            prevHotDt = DateTime.Now;
            return hotkey;
        }

        private int convertDateStringHotkey(int hotkey)
        {
            if (Settings.LoggingHotKeyInfo) logger.InfoH($"CALLED: hotkey={hotkey:x}");
            bool bFirst = prevHotkey != HotKeys.DATE_STRING_HOTKEY1 || DateTime.Now > prevHotDt.AddSeconds(3);
            if (bFirst) {
                if (Settings.LoggingHotKeyInfo) logger.InfoH($"bFirst={bFirst}");
                dateStrHotkeyCount = 0;     // 0 は初期状態
                prevDateStrLength = 0;
                dayOffset = 0;
            }
            switch (hotkey) {
                case HotKeys.CTRL_SEMICOLON_HOTKEY:         // 日付フォーマットの切り替え
                    if (Settings.LoggingHotKeyInfo) logger.InfoH($"CTRL_SEMICOLON_HOTKEY");
                    // 次の日付フォーマット
                    ++dateStrHotkeyCount;
                    break;
                case HotKeys.CTRL_SHIFT_SEMICOLON_HOTKEY:   // 日付フォーマットの切り替え(最後のフォーマットから)
                    if (Settings.LoggingHotKeyInfo) logger.InfoH($"CTRL_SHIFT_SEMICOLON_HOTKEY");
                    // 前の日付フォーマット
                    --dateStrHotkeyCount;
                    break;
                case HotKeys.CTRL_COLON_HOTKEY:             // 日付のインクリメント
                    if (Settings.LoggingHotKeyInfo) logger.InfoH($"CTRL_COLON_HOTKEY");
                    // 日付のインクリメント
                    ++dayOffset;
                    break;
                case HotKeys.CTRL_SHIFT_COLON_HOTKEY:       // 日付のデクリメント
                    if (Settings.LoggingHotKeyInfo) logger.InfoH($"CTRL_SHIFT_COLON_HOTKEY");
                    // 日付のデクリメント
                    --dayOffset;
                    break;
            }
            if (Settings.LoggingHotKeyInfo) logger.InfoH($"LEAVE: new hotkey={HotKeys.DATE_STRING_HOTKEY1:x}, dateStrHotkeyCount={dateStrHotkeyCount}, prevDateStrLength={prevDateStrLength}, dayOffset={dayOffset}");
            return HotKeys.DATE_STRING_HOTKEY1;
        }

        // 開発者用の設定がONになっているとき、漢直モードのON/OFFを10回繰り返したら警告を出す
        private int devFlagsOnWarningCount = 0;

        // アクティブと非アクティブを切り替える
        public void ToggleActiveState(bool bForceOff = false)
        {
            IsDecoderActive = bForceOff ? false : !IsDecoderActive;
            logger.InfoH(() => $"\nENTER: Now Decoder is {(IsDecoderActive ? "ACTIVE" : "INACTIVE")}");
            if (IsDecoderActive) {
                if (frmSplash != null) closeSplash();
                if (decoderPtr != IntPtr.Zero) ResetDecoder(decoderPtr);
                decoderOutput.layout = 0;   // None にリセットしておく。これをやらないと仮想鍵盤モードを切り替えたときに以前の履歴選択状態が残ったりする
                CommonState.CenterString = "";
                Settings.VirtualKeyboardShowStrokeCountTemp = 0;
                frmVkb.DrawInitailVkb();
                HotKeyHandler.RegisterDeactivateHotKeys();
                HotKeyHandler.RegisterDecoderSpecialHotKeys();
                HotKeyHandler.RegisterDecoderStrokeHotKeys();
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
            } else {
                handleKeyDecoder(HotKeys.ACTIVE_HOTKEY);   // DecoderOff の処理をやる
                frmVkb.Hide();
                frmMode.Hide();
                notifyIcon1.Icon = Properties.Resources.kanmini0;
                //HotKeyHandler.UnregisterCandSelectHotKeys();        // 候補選択中に漢直モードをOFFにする可能性があるため
                handleArrowKeys(true);                          // 候補選択中に漢直モードをOFFにする可能性があるため、強制的に Unreg しておく
                HotKeyHandler.UnregisterDecoderSpecialHotKeys();
                HotKeyHandler.UnregisterDecoderStrokeHotKeys();
                HotKeyHandler.RegisterActivateHotKeys();
                //Text = "漢直窓S - OFF";
                frmMode.SetKanjiMode();
                if (Settings.VirtualKeyboardShowStrokeCount != 1) {
                    frmMode.SetAlphaMode();
                }
            }
            logger.InfoH("LEAVE");
        }

        /// <summary>仮想鍵盤の表示位置を移動する</summary>
        public void MoveFormVirtualKeyboard()
        {
            logger.InfoH("CALLED");
            actWinHandler?.MoveWindow();
        }

        // デコーダをOFFにする
        public void DeactivateDecoder()
        {
            ToggleActiveState(true);
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

        public void ReregisterSpecialGlobalHotkeys()
        {
            logger.InfoH("CALLED");
            DeactivateDecoder();
            HotKeyHandler.UnregisterSpecialGlobalHotKeys();
            HotKeyHandler.RegisterSpecialGlobalHotKeys();
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

            // キーボードファイルを読み込む
            if (readKeyboardFile()) {
                // デコーダの初期化
                if (await initializeDecoder()) {
                    // 初期打鍵表(下端機能キー以外は空白)の作成
                    makeInitialVkbTable();
                    // 打鍵テーブルの作成
                    frmVkb.MakeStrokeTables(Settings.StrokeHelpFile);
                    // HotKeyハンドラの初期化
                    HotKeyHandler.Initialize(this.Handle);
                }
            }
            logger.Info("LEAVE");
        }

        private bool readKeyboardFile()
        {
            logger.Info("ENTER");
            // キーボードファイルを読み込む
            if (!VirtualKeys.ReadKeyboardFile()) {
                // 読み込めなかったので終了する
                logger.Error($"CLOSE: Can't read keyboard file");
                //HotKeyHandler.Destroy();
                //PostMessage(this.Handle, WM_Defs.WM_CLOSE, 0, 0);
                this.Close();
                return false;
            }
            logger.Info("LEAVE");
            return true;
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
        /// デコーダの呼び出し
        /// </summary>
        private void handleKeyDecoder(int hotkey)
        {
            if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"ENTER: hotkey={hotkey:x}H({hotkey})");

            // デコーダの呼び出し
            HandleHotkeyDecoder(decoderPtr, hotkey, ref decoderOutput);

            logger.Info(() => $"layout={decoderOutput.layout}, numBS={decoderOutput.numBackSpaces}, resultFlags={decoderOutput.resultFlags:x}H, output={decoderOutput.outString._toString()}");

            // 第1打鍵待ち状態になったら、一時的な仮想鍵盤表示カウントをリセットする
            if (decoderOutput.GetStrokeCount() < 1) Settings.VirtualKeyboardShowStrokeCountTemp = 0;

            // 中央鍵盤文字列の取得
            getCenterString();

            // 他のVKey送出(もしあれば)
            if (decoderOutput.IsHotkeyToVkey()) {
                postVkeyFromHotkey(hotkey);
                //nPreKeys += 1;
            }

            // BSと文字送出(もしあれば)
           actWinHandler.SendStringViaClipboardIfNeeded(decoderOutput.outString, decoderOutput.numBackSpaces);

            // 仮想キーボードにヘルプや文字候補を表示
            frmVkb.DrawVirtualKeyboardChars();

            // 候補選択が必要なら矢印キーをホットキーにする
            handleArrowKeys();

            if (Settings.LoggingHotKeyInfo) logger.InfoH($"LEAVE");
        }

        private void postVkeyFromHotkey(int hotkey)
        {
            var combo = VirtualKeys.GetVKeyComboFromHotKey(hotkey);
            if (combo != null) {
                if (hotkey < HotKeys.FUNCTIONAL_HOTKEY_ID_BASE) {
                    actWinHandler.SendVirtualKey(combo.Value.vkey, 1);
                } else {
                    actWinHandler.SendVirtualKeys(combo.Value, 1);
                }
            }
        }

        /// <summary> 今日の日付文字列を出力する </summary>
        private void postTodayDate(int hotkey)
        {
            if (Settings.LoggingHotKeyInfo) logger.InfoH($"CALLED: hotkey={hotkey:x}");
            var items = Settings.DateStringFormat._split('|');
            if (items._isEmpty()) return;
            if (dateStrHotkeyCount < 0)
                dateStrHotkeyCount += items.Length + 1;
            else
                dateStrHotkeyCount %= items.Length + 1;
            var dtStr = "";
            if (dateStrHotkeyCount > 0) {
                var dtNow = DateTime.Now.AddDays(dayOffset);
                var fmt = items._getNth(dateStrHotkeyCount - 1)._strip();
                if (Settings.LoggingHotKeyInfo) logger.InfoH($"count={dateStrHotkeyCount}, fmt={fmt}");
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
        private bool AreArrowKeysHotKey = false;

        // UIスレッドから呼び出す必要あり
        private void handleArrowKeys(bool bForceUnreg = false)
        {
            bool regFlag = false;
            bool unregFlag = false;

            if (bForceUnreg) {
                unregFlag = true;
            } else if (decoderOutput.IsArrowKeysRequired()) {
                regFlag = !AreArrowKeysHotKey;
            } else {
                unregFlag = AreArrowKeysHotKey;
            }

            if (regFlag) {
                HotKeyHandler.RegisterCandSelectHotKeys();
                AreArrowKeysHotKey = true;
                logger.Info("Register Arrow Keys");
            } else if (unregFlag) {
                HotKeyHandler.UnregisterCandSelectHotKeys();
                AreArrowKeysHotKey = false;
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

    public static class KeyboardHookHandler
    {
        private static Logger logger = Logger.GetLogger();

        private static FrmKanchoku frmMain { get; set; }

        public static void InstallKeyboardHook(FrmKanchoku frm)
        {
            frmMain = frm;
            KeyboardHook.OnKeyDownEvent = onKeyboardDownHandler;
            KeyboardHook.OnKeyUpEvent = onKeyboardUpHandler;
            KeyboardHook.Hook();
            logger.InfoH($"LEAVE");
        }

        public static void ReleaseKeyboardHook()
        {
            KeyboardHook.UnHook();
            logger.InfoH($"LEAVE");
        }

        private static bool onKeyboardDownHandler(int vkey, int extraInfo)
        {
            return frmMain.OnKeyboardDownHandler(vkey, extraInfo);
        }

        private static bool onKeyboardUpHandler(int vkey, int extraInfo)
        {
            return frmMain.OnKeyboardUpHandler(vkey, extraInfo);
        }
    }
}

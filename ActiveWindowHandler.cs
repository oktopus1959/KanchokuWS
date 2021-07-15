using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using Utils;

namespace KanchokuWS
{
    public class ActiveWindowHandler : IDisposable
    {
        private static Logger logger = Logger.GetLogger();

        public FrmKanchoku FrmMain { get; set; }
        public FrmVirtualKeyboard FrmVkb { get; set; }
        public FrmModeMarker FrmMode { get; set; }

        /// <summary> アクティブ(フォーカスを持つ)ウィンドウの ClassName </summary>
        public string ActiveWinClassName { get; private set; } = "";

        /// <summary> アクティブ(フォーカスを持つ)ウィンドウのハンドル </summary>
        private IntPtr ActiveWinHandle = IntPtr.Zero;

        /// <summary> アクティブ(フォーカスを持つ)ウィンドウのカレット位置 </summary>
        private Rectangle ActiveWinCaretPos;

        /// <summary> アクティブ(フォーカスを持つ)ウィンドウの固有の設定 </summary>
        private Settings.WindowsClassSettings ActiveWinSettings;

        /// <summary> 仮想鍵盤ウィンドウの ClassName の末尾のハッシュ部分 </summary>
        private string DlgVkbClassNameHash;

        public ActiveWindowHandler(FrmKanchoku frmMain, FrmVirtualKeyboard frmVkb, FrmModeMarker frmMode)
        {
            FrmMain = frmMain;
            FrmVkb = frmVkb;
            FrmMode = frmMode;
            DlgVkbClassNameHash = getWindowClassName(FrmVkb.Handle)._safeSubstring(-16);
            logger.Info(() => $"Vkb ClassName Hash={DlgVkbClassNameHash}");
        }

        private bool bDisposed = false;

        public void Dispose()
        {
            if (!bDisposed) {
                bDisposed = true;
                logger.Info("Disposed");
            }
        }

        // cf. https://tomosoft.jp/design/?p=6624
        //COPYDATASTRUCT構造体 
        public struct COPYDATASTRUCT
        {
            public Int32 dwData;      //送信する32ビット値
            public Int32 cbData;   //lpDataのバイト数
            public string lpData;   //送信するデータへのポインタ(0も可能)
        }

        //[DllImport("User32.dll", EntryPoint = "PostMessageW")]
        //private static extern Int32 PostMessageW(IntPtr hWnd, uint Msg, uint wParam, ref COPYDATASTRUCT lParam);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 PostMessageW(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

        [DllImport("user32.dll")]
        private static extern ushort GetAsyncKeyState(uint vkey);

        [DllImport("user32.dll")]
        private static extern uint keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", EntryPoint = "MapVirtualKeyA")]
        private extern static uint MapVirtualKey(uint wCode, uint wMapType);

        private const uint MAPVK_VK_TO_VSC = 0;
        private const uint KEYEVENTF_KEYDOWN = 0;
        private const uint KEYEVENTF_EXTENDEDKEY = 1;
        private const uint KEYEVENTF_KEYUP = 2;

        private void keybdEvent(uint vkey, uint eventFlags, bool bExt = false)
        {
            // これは不要のようだ
            //uint sc = MapVirtualKey(Vkeys.LCONTROL, MAPVK_VK_TO_VSC);
            uint sc = 0;

            if (bExt) eventFlags |= KEYEVENTF_EXTENDEDKEY;
            keybd_event((byte)vkey, (byte)sc, eventFlags, UIntPtr.Zero);
        }

        private void keybd_up(uint vkey, bool bExt = false)
        {
            keybdEvent(vkey, KEYEVENTF_KEYUP, bExt);
        }

        private void keybd_down(uint vkey, bool bExt = false)
        {
            keybdEvent(vkey, KEYEVENTF_KEYDOWN, bExt);
        }

        private DateTime lastOutputDt;

        /// <summary>
        /// Ctrlキーの事前上げ下げ
        /// </summary>
        /// <param name="leftCtrl"></param>
        /// <param name="rightCtrl"></param>
        private void upDownCtrlKey(bool bUp, out bool leftCtrl, out bool rightCtrl)
        {
            bool loggingFlag = Settings.LoggingHotKeyInfo;
            int waitUpMs = (ActiveWinSettings?.CtrlUpWaitMillisec)._value(-1)._geZeroOr(Settings.CtrlKeyUpGuardMillisec);
            leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
            rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;
            if (bUp && (leftCtrl || rightCtrl)) {
                // 両方一諸に上げるようにした
                keybd_up(VirtualKeys.CONTROL);              // extended を付けないと LCONTROL 扱いのようだ
                keybd_up(VirtualKeys.CONTROL, true);        // extended を付けると RCONTROL 扱いのようだ
                if (loggingFlag) logger.InfoH($"Ctrl Up and wait {waitUpMs} millisec");
                if (waitUpMs > 0) {
                    Task.Delay(waitUpMs).Wait();            // やはりこれが無いと Ctrlが有効なままBSが渡ったりする
                }
            } else if (!bUp && !(leftCtrl || rightCtrl)) {
                keybd_down(VirtualKeys.CONTROL);              // extended を付けないと LCONTROL 扱いのようだ
                keybd_down(VirtualKeys.CONTROL, true);        // extended を付けると RCONTROL 扱いのようだ
                if (loggingFlag) logger.InfoH($"Ctrl Down and wait {waitUpMs} millisec");
                if (waitUpMs > 0) {
                    Task.Delay(waitUpMs).Wait();            // やはりこれが無いと Ctrlが有効なままBSが渡ったりする
                }
            }
        }

        /// <summary>
        /// Ctrlキーの状態を戻す
        /// </summary>
        /// <param name="prevLeftCtrl"></param>
        /// <param name="prevRightCtrl"></param>
        private void revertCtrlKey(bool prevLeftCtrl, bool prevRightCtrl)
        {
            bool loggingFlag = Settings.LoggingHotKeyInfo;
            int waitDownMs = (ActiveWinSettings?.CtrlDownWaitMillisec)._value(-1)._geZeroOr(Settings.CtrlKeyDownGuardMillisec);

            bool leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
            if (prevLeftCtrl && !leftCtrl) {
                if (loggingFlag) logger.InfoH($"LeftCtrl Down after waiting {waitDownMs} millisec");
                if (waitDownMs > 0) {
                    Task.Delay(waitDownMs).Wait();
                }
                keybd_down(VirtualKeys.CONTROL);            // extended を付けないと LCONTROL 扱いのようだ
            } else if (!prevLeftCtrl && leftCtrl) {
                if (loggingFlag) logger.InfoH($"LeftCtrl Up after waiting {waitDownMs} millisec");
                if (waitDownMs > 0) {
                    Task.Delay(waitDownMs).Wait();
                }
                keybd_up(VirtualKeys.CONTROL);            // extended を付けないと LCONTROL 扱いのようだ
            }

            bool rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;
            if (prevRightCtrl && !rightCtrl) {
                if (loggingFlag) logger.InfoH($"RightCtrl Down after waiting {waitDownMs} millisec");
                if (waitDownMs > 0) {
                    Task.Delay(waitDownMs).Wait();
                }
                keybd_down(VirtualKeys.CONTROL, true);      // extended を付けると RCONTROL 扱いのようだ
            } else if (!prevRightCtrl && rightCtrl) {
                if (loggingFlag) logger.InfoH($"RightCtrl Up after waiting {waitDownMs} millisec");
                if (waitDownMs > 0) {
                    Task.Delay(waitDownMs).Wait();
                }
                keybd_up(VirtualKeys.CONTROL, true);      // extended を付けると RCONTROL 扱いのようだ
            }
        }

        /// <summary>出力文字カウント辞書</summary>
        public Dictionary<char, int> CharCountDic { get; private set; } = new Dictionary<char, int>();

        public void ReadCharCountFile(string filename)
        {
            logger.InfoH(() => $"CALLED: filename={filename}");
            if (Helper.FileExists(filename)) {
                try {
                    foreach (var line in System.IO.File.ReadAllLines(filename)) {
                        var items = line.Trim()._split('\t');
                        if (items._length() == 2 && items[0]._notEmpty() && items[1]._notEmpty()) {
                            CharCountDic[items[0][0]] = items[1]._parseInt();
                        }
                    }
                } catch (Exception e) {
                    logger.Error($"Cannot read file: {filename}: {e.Message}");
                }
            }
        }

        //public void WriteCharCountFile(string filename)
        //{
        //    logger.InfoH(() => $"CALLED: filename={filename}");
        //    if (filename._notEmpty()) {
        //        try {
        //            System.IO.File.WriteAllText(filename,
        //                CharCountDic.OrderByDescending(p => p.Value).Select(p => $"{p.Key}\t{p.Value}")._join("\n"));
        //        } catch (Exception e) {
        //            logger.Error($"Cannot write file: {filename}: {e.Message}");
        //        }
        //    }
        //}

        /// <summary>
        /// アクティブウィンドウに文字を送出する
        /// </summary>
        /// <param name="ch"></param>
        public void PostChar(char ch)
        {
            PostMessageW(ActiveWinHandle, WM_Defs.WM_CHAR, ch, 1);
            //PostMessageW(ActiveWinHandle, WM_Defs.WM_IME_CHAR, ch, 1);
            //if (ch >= 0x100) CharCountDic[ch] = CharCountDic._safeGet(ch) + 1;
        }

        /// <summary>
        /// アクティブウィンドウに仮想キーを送出する<br/>
        /// bCheckCtrl == true なら、 Ctrlキーが立っている場合は、何もせず戻る
        /// </summary>
        /// <param name="n">キーダウンの数</param>
        public void PostVirtualKey(uint vkey, int n, bool bCheckCtrl)
        {
            if (Settings.LoggingHotKeyInfo) logger.InfoH($"vkey={vkey:x}H, n={n}");

            if (bCheckCtrl) {
                if ((GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0) {
                    if (Settings.LoggingHotKeyInfo) logger.InfoH("LeftCtrl ON; return");
                    return;
                }
                if ((GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0) {
                    if (Settings.LoggingHotKeyInfo) logger.InfoH("RightCtrl ON; return");
                    return;
                }
            }

            // まずホットキーの解除
            int hotkey = VirtualKeys.GetHotKeyFromCombo(0, vkey);
            int ctrlHotkey = VirtualKeys.GetHotKeyFromCombo(KeyModifiers.MOD_CONTROL, vkey);
            HotKeyHandler.UnregisterHotKeyTemporary(hotkey);
            HotKeyHandler.UnregisterHotKeyTemporary(ctrlHotkey);
            // キーの Down/Up
            for (int i = 0; i < n; ++i) {
                keybd_down(vkey);
                keybd_up(vkey);
            }
            // ホットキーの再登録
            HotKeyHandler.ResumeHotKey(hotkey);
            HotKeyHandler.ResumeHotKey(ctrlHotkey);

            // Enterキーだったら、すぐに仮想鍵盤を移動するように MinValue とする
            lastOutputDt = vkey == (uint)Keys.Enter ? DateTime.MinValue : DateTime.Now;
        }

        BoolObject syncPostVkey = new BoolObject();

        /// <summary>
        /// アクティブウィンドウにVkeyを送出する
        /// </summary>
        /// <param name="n">vkeyの数</param>
        public void PostVirtualKeys(VKeyCombo combo, int n)
        {
            bool loggingFlag = Settings.LoggingHotKeyInfo;
            if (loggingFlag) logger.InfoH($"CALLED: combo=({combo.modifier:x}H:{combo.vkey:x}H), numKeys={n}");
            if (syncPostVkey.BusyCheck()) {
                if (loggingFlag) logger.InfoH($"IGNORED: numKeys={n}");
                return;
            }
            using (syncPostVkey) {
                lock (syncPostVkey) {
                    bool leftCtrl = false, rightCtrl = false;
                    bool bUp = (combo.modifier & KeyModifiers.MOD_CONTROL) == 0;
                    upDownCtrlKey(bUp, out leftCtrl, out rightCtrl);

                    PostVirtualKey(combo.vkey, n, false);

                    revertCtrlKey(leftCtrl, rightCtrl);
                }
            }
        }

        /// <summary>
        /// アクティブウィンドウにBSを送出する
        /// </summary>
        /// <param name="n">BSの数</param>
        public void PostBackSpaces(int n)
        {
            if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"ActiveWinHandle={(int)ActiveWinHandle:x}H, n={n}");
            if (n > 0) PostVirtualKeys(new VKeyCombo(0, (uint)Keys.Back), n);
        }

        /// <summary>
        /// アクティブウィンドウに文字列を送出する<br/>
        /// 文字送出前に nPreKeys * PreWmCharGuardMillisec だけ、wait を入れる。<br/>
        /// 必要ならクリップボード経由で送り付ける
        /// </summary>
        public void PostStringViaClipboardIfNeeded(char[] str, int nPreKeys)
        {
            if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"ActiveWinHandle={(int)ActiveWinHandle:x}H, WM_CHAR={WM_Defs.WM_CHAR:x}H, str=\"{str._toString()}\"");

            if (ActiveWinHandle != IntPtr.Zero && str.Length > 0 && str[0] != 0) {
                // Backspace の送出があった場合、すぐに PostMessage を呼ぶと、Backspace との順序が入れ替わることがあるっぽい？ので、少し wait を入れてみる
                // オリジナルの漢直Winでは、waitではなくWaitForInputIdle()を呼んでいた。
                // しかしオリジナル漢直では文字送出とBackspaceの入れ替わりがあったことを考えると、やはり一定時間 wait すべきではないかと考える。
                if (nPreKeys > 0 && Settings.PreWmCharGuardMillisec > 0) {
                    int waitMs = (int)(Math.Pow(nPreKeys, Settings.ReductionExponet._lowLimit(0.5)) * Settings.PreWmCharGuardMillisec);
                    if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"Wait {waitMs} ms: PreWmCharGuardMillisec={Settings.PreWmCharGuardMillisec}, nPreKeys={nPreKeys}, reductionExp={Settings.ReductionExponet}");
                    //Task.Delay(Settings.PreWmCharGuardMillisec * nPreKeys).Wait(); // ⇒こちらだと自身の設定ダイアログなどに向けた出力処理でBSの処理が後回しになってしまうようだ
                    //Helper.WaitMilliSeconds(Settings.PreWmCharGuardMillisec * nPreKeys);
                    Helper.WaitMilliSeconds(waitMs);
                }

                int len = str._findIndex(x => x == 0);
                if (Settings.MinLeghthViaClipboard <= 0 || len < Settings.MinLeghthViaClipboard) {
                    // 自前で送出
                    bool leftCtrl, rightCtrl;
                    upDownCtrlKey(true, out leftCtrl, out rightCtrl);

                    foreach (char ch in str) {
                        if (ch == 0) break;
                        PostChar(ch);
                    }

                    revertCtrlKey(leftCtrl, rightCtrl);
                } else {
                    // クリップボードにコピー
                    Clipboard.SetText(new string(str, 0, len));
                    // Ctrl-V を送る (PostVirtualKeys の中でも upDownCtrlKey/revertCtrlKey をやっている)
                    PostVirtualKeys(VirtualKeys.GetVKeyComboFromHotKey(HotKeys.CTRL_V_HOTKEY).Value, 1);
                }

                lastOutputDt = DateTime.Now;
            }
        }

        //-----------------------------------------------------------------------------------------------------
        private SyncBool syncBS = new SyncBool();

        //// cf. https://stackoverflow.com/questions/4372055/detect-active-window-changed-using-c-sharp-without-polling
        //// cf. https://stackoverflow.com/questions/6548470/getfocus-win32api-help
        //delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        //WinEventDelegate dele = null;

        //[DllImport("user32.dll")]
        //private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        //private const uint WINEVENT_OUTOFCONTEXT = 0;
        //private const uint EVENT_SYSTEM_FOREGROUND = 3;

        //[DllImport("user32.dll")]
        //static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        //[DllImport("kernel32.dll")]
        //static extern uint GetCurrentThreadId();

        //[DllImport("user32.dll")]
        //static extern uint GetWindowThreadProcessId(IntPtr hWnd, int ProcessId);

        //[DllImport("user32.dll")]
        //static extern IntPtr GetForegroundWindow();

        //[DllImport("user32.dll")]
        //private static extern IntPtr GetFocus();

        //[DllImport("user32.dll")]
        //private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);

        //[DllImport("user32.dll")]
        //private static extern bool GetCaretPos(out Point lpPoint);

        //[DllImport("user32.dll")]
        //private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        GUIThreadInfo guiThreadInfo = new GUIThreadInfo();

        /// <summary>
        /// アクティブウィンドウハンドルの取得
        /// </summary>
        private void GetActiveWindowHandle(bool bLog = true)
        {
            if (bLog) logger.InfoH("ENTER");

            IntPtr fgHan = IntPtr.Zero;
            IntPtr focusHan = IntPtr.Zero;

            // すぐにハンドルを取りに行くと失敗することがある。
            // Focus を持つウィンドウハンドルが 0 以外になるまで、N msecのwaitを入れながら最大3回試行する
            for (int count = 0; count < 3; ++count) {
                // とりあえず20msec待つ
                //Task.Delay(20).Wait();
                Helper.WaitMilliSeconds(20);    // UIスレッドのタイマを使用するようにしたせいか、単なる Task.Delayだと処理が固まることがある気がする

                // 最前面ウィンドウの情報を取得
                guiThreadInfo.GetForegroundWinInfo();

                fgHan = guiThreadInfo.hwndForeground;
                ActiveWinClassName = guiThreadInfo.className;
                focusHan = guiThreadInfo.hwndFocus;
                if (bLog) logger.InfoH(() => $"fgHan={(int)fgHan:x}H, focusHan={(int)focusHan:x}H");

                guiThreadInfo.GetScreenCaretPos(ref ActiveWinCaretPos);
                if (bLog) logger.InfoH(() => $"WndClass={ActiveWinClassName}: focus caret pos=({ActiveWinCaretPos.X}, {ActiveWinCaretPos.Y})");

                if (focusHan != IntPtr.Zero || ActiveWinClassName._equalsTo("ConsoleWindowClass")) break;  // CMD Prompt の場合は Focus が取れないっぽい?
                if (bLog || Logger.IsInfoEnabled) logger.Warn($"RETRY: count={count + 1}");
            }

            if (focusHan == IntPtr.Zero) {
                if (bLog || Logger.IsInfoEnabled) logger.Warn("Can't get window handle with focus");
            }
            ActiveWinHandle = (focusHan == IntPtr.Zero) ? fgHan : focusHan;

            if (bLog) logger.InfoH(() => $"LEAVE: ActiveWinHandle={(int)ActiveWinHandle:x}H");
        }

        private string getWindowClassName(IntPtr hwnd)
        {
            const int nChars = 1024;
            StringBuilder Buff = new StringBuilder(nChars);
            GetClassName(hwnd, Buff, nChars);
            return Buff.ToString();
        }

        private Rectangle prevCaretPos;

        /// <summary> ウィンドウを移動さ出ない微少変動量 </summary>
        private const int NoMoveOffset = 10;

        private List<Rectangle> screenRects = new List<Rectangle>();

        public void GetScreenInfo()
        {
            screenRects = Screen.AllScreens.Select(s => new Rectangle(s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height)).ToList();
            if (Settings.LoggingActiveWindowInfo) {
                int i = 0;
                foreach (var r in screenRects) {
                    logger.InfoH($"Screen {i}: X={r.X}, Y={r.Y}, W={r.Width}, H={r.Height}");
                }
            }
        }

        private DateTime prevLogDt1;

        private void loggingCaretInfo()
        {
            if (prevLogDt1.AddSeconds(3) < DateTime.Now) {
                prevLogDt1 = DateTime.Now;
                var caretMargin = ActiveWinSettings?.ValidCaretMargin;
                if (caretMargin != null) {
                    GUIThreadInfo.RECT rect;
                    guiThreadInfo.GetForegroundWindowRect(out rect);
                    logger.InfoH($"caretPos=(X:{ActiveWinCaretPos.X}, Y:{ActiveWinCaretPos.Y}), " +
                        $"validCaretMargin=({caretMargin.Select(m => m.ToString())._join(",")}), " +
                        $"WinRect=(L:{rect.iLeft}, T:{rect.iTop}, R:{rect.iRight}, B:{rect.iBottom}), " +
                        $"validWinRect=(L:{rect.iLeft + caretMargin._getNth(2)}, " +
                        $"T:{rect.iTop + caretMargin._getNth(0)}, " +
                        $"R:{rect.iRight - caretMargin._getNth(3)}, " +
                        $"B:{rect.iBottom - caretMargin._getNth(1)})");
                }
                var caretOffset = ActiveWinSettings?.CaretOffset;
                if (caretOffset != null) {
                    logger.InfoH($"caretOffset=({caretOffset.Select(m => m.ToString())._join(",")})");
                }
            }
        }

        /// <summary> 指定されたマージンの内側にカレットがあるか</summary>
        /// <returns></returns>
        private bool isInValidCaretMargin(Settings.WindowsClassSettings settings)
        {
            var caretMargin = settings?.ValidCaretMargin;
            if (caretMargin == null) return true;

            GUIThreadInfo.RECT rect;
            guiThreadInfo.GetForegroundWindowRect(out rect);
            return rect.iTop + caretMargin._getNth(0) <= ActiveWinCaretPos.Y
                && ActiveWinCaretPos.Y <= rect.iBottom - caretMargin._getNth(1)
                && rect.iLeft + caretMargin._getNth(2) <= ActiveWinCaretPos.X
                && ActiveWinCaretPos.X <= rect.iRight - caretMargin._getNth(3);
        }

        /// <summary>
        /// 仮想鍵盤をカレットの近くに移動する<br/>
        /// </summary>
        public void MoveWindow()
        {
            moveWindow(false, true, true);
        }

        /// <summary>
        /// 仮想鍵盤をカレットの近くに移動する (仮想鍵盤自身がアクティブの場合は移動しない)<br/>
        /// これが呼ばれるのはデコーダがONのときだけ
        /// </summary>
        private void moveWindow(bool bDiffWin, bool bMoveMandatory, bool bLog)
        {
            ActiveWinSettings = Settings.GetWinClassSettings(ActiveWinClassName);
            if (bLog) {
                logger.InfoH($"CALLED: diffWin={bDiffWin}, mandatory={bMoveMandatory}");
                loggingCaretInfo();
            }

            if (!FrmMain.IsVirtualKeyboardFreezed && !ActiveWinClassName.EndsWith(DlgVkbClassNameHash) && ActiveWinClassName._ne("SysShadow")) {
                if (bMoveMandatory ||
                    ((Math.Abs(ActiveWinCaretPos.X) >= NoMoveOffset || Math.Abs(ActiveWinCaretPos.Y) >= NoMoveOffset) &&
                     (Math.Abs(ActiveWinCaretPos.X - prevCaretPos.X) >= NoMoveOffset || Math.Abs(ActiveWinCaretPos.Y - prevCaretPos.Y) >= NoMoveOffset) &&
                     isInValidCaretMargin(ActiveWinSettings))
                   ) {
                    int xOffset = (ActiveWinSettings?.CaretOffset)._getNth(0, Settings.VirtualKeyboardOffsetX);
                    int yOffset = (ActiveWinSettings?.CaretOffset)._getNth(1, Settings.VirtualKeyboardOffsetY);
                    if (bLog) logger.InfoH($"CaretPos.X={ActiveWinCaretPos.X}, CaretPos.Y={ActiveWinCaretPos.Y}, xOffset={xOffset}, yOffset={yOffset}");
                    if (ActiveWinCaretPos.X >= 0) {
                        if (bLog) {
                            logger.InfoH($"MOVE: X={ActiveWinCaretPos.X}, Y={ActiveWinCaretPos.Y}, W={ActiveWinCaretPos.Width}, H={ActiveWinCaretPos.Height}, OX={xOffset}, OY={yOffset}");
                            int sw = screenRects._notEmpty() ? screenRects[0].Width : 0;
                            int sh = screenRects._notEmpty() ? screenRects[0].Height : 0;
                            FrmVkb.SetTopText($"SW={sw},SH={sh},CX={ActiveWinCaretPos.X},CY={ActiveWinCaretPos.Y},CW={ActiveWinCaretPos.Width},CH={ActiveWinCaretPos.Height},OX={xOffset},OY={yOffset}");
                        }
                        Action<Form> moveAction = (Form frm) => {
                            int cX = ActiveWinCaretPos.X;
                            int cY = ActiveWinCaretPos.Y;
                            int cBottom = cY + ActiveWinCaretPos.Height;
                            int fX = cX + xOffset;
                            int fY = cBottom + yOffset;
                            int fW = frm.Size.Width;
                            int fH = frm.Size.Height;
                            int fRight = fX + fW;
                            int fBottom = fY + fH;
                            int sRight = 0;
                            int sBottom = 0;
                            int sRightMax = 0;
                            int sBottomMax = 0;
                            foreach (var r in screenRects) {
                                sRight = r.X + r.Width;
                                sBottom = r.Y + r.Height;
                                if (cX < sRight && cBottom < sBottom) {
                                    // このスクリーンに納まっていた
                                    break;
                                }
                                // 納まらなかったときは最大スクリーンに設定しておく
                                if (sRightMax < sRight) sRightMax = sRight;
                                if (sBottomMax < sBottom) sBottomMax = sBottom;
                                sRight = sRightMax;
                                sBottom = sBottomMax;
                            }
                            if (fRight >= sRight) fX = cX - fW - xOffset;
                            if (fBottom >= sBottom) fY = cY - fH - yOffset;
                            MoveWindow(frm.Handle, fX, fY, fW, fH, true);
                        };
                        // 仮想鍵盤の移動
                        moveAction(FrmVkb);

                        // 入力モード標識の移動
                        moveAction(FrmMode);
                        if (bDiffWin && !FrmMain.IsVkbShown) {
                            // 異なるウィンドウに移動したら入力モード標識を表示する
                            FrmMode.ShowImmediately();
                        }
                        prevCaretPos = ActiveWinCaretPos;
                    }
                }
            } else {
                logger.Debug(() => $"ActiveWinClassName={ActiveWinClassName}, VkbClassName={DlgVkbClassNameHash}");
            }
        }

        // 同じスレッドで再入するのを防ぐ
        private BoolObject syncObj = new BoolObject();

        public enum MoveWinType
        {
            Freeze = 0,
            MoveIfAny = 1,
            MoveMandatory = 2,
        }

        public void GetActiveWindowInfo()
        {
            GetActiveWindowInfo(MoveWinType.MoveIfAny, Settings.LoggingActiveWindowInfo /*&& Logger.IsDebugEnabled*/);
        }

        public void GetActiveWindowInfo(MoveWinType moveWin, bool bLog = true)
        {
            if (bLog) logger.InfoH($"ENTER: moveWin={moveWin}");

            // 同一スレッドでの再入を防ぐ
            if (syncObj.BusyCheck()) {
                if (Logger.IsInfoEnabled && !ActiveWinClassName._endsWith(DlgVkbClassNameHash)) {
                    logger.InfoH("LEAVE: In Progress");
                }
                return;
            }

            bool bOK = false;
            bool bDiffWin = false;
            using (syncObj) {   // 抜けたときにビジー状態が解除される
                try {
                    string prevClassName = ActiveWinClassName;
                    GetActiveWindowHandle(bLog);
                    bOK = true;
                    bDiffWin = ActiveWinClassName._ne(prevClassName);
                    if (bDiffWin && !ActiveWinClassName._endsWith(DlgVkbClassNameHash)) {
                        // 直前のものとクラス名が異なっていれば、それを仮想鍵盤上部に表示する (ただし、仮想鍵盤自身を除く)
                        FrmVkb.SetTopText(ActiveWinClassName);
                    }
                } catch (Exception e) {
                    logger.Error($"{e.Message}\n{e.StackTrace}");
                }
            }
            if (bOK && moveWin != MoveWinType.Freeze) {
                // 強制移動でない場合は、頻繁に移動しないように、最後のキー出力が終わってNms経過したらウィンドウを移動する
                bool bMandatory = moveWin == MoveWinType.MoveMandatory;
                if (bMandatory || DateTime.Now >= lastOutputDt.AddMilliseconds(Settings.VirtualKeyboardMoveGuardMillisec))
                    moveWindow(bDiffWin, bMandatory, bLog);
            }
            if (bLog) logger.InfoH(() => $"LEAVE: ActiveWinClassName={ActiveWinClassName}");
        }

    }
}

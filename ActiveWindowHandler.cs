﻿using System;
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

        public const int MyMagicNumber = 1959;

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

        // cf. http://pgcenter.web.fc2.com/contents/csharp_sendinput.html
        // cf. https://www.pinvoke.net/default.aspx/user32.sendinput

        // マウスイベント(mouse_eventの引数と同様のデータ)
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public int dwExtraInfo;
        };

        // キーボードイベント(keybd_eventの引数と同様のデータ)
        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public int dwFlags;
            public int time;
            public int dwExtraInfo;
        };

        // ハードウェアイベント
        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public int uMsg;
            public ushort wParamL;
            public ushort wParamH;
        };

        // 各種イベント(SendInputの引数データ)
        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT
        {
            [FieldOffset(0)] public int type;
            [FieldOffset(4)] public MOUSEINPUT mi;
            [FieldOffset(4)] public KEYBDINPUT ki;
            [FieldOffset(4)] public HARDWAREINPUT hi;
        };

        //// キー操作、マウス操作をシミュレート(擬似的に操作する)
        //[DllImport("user32.dll")]
        //private extern static void SendInput(
        //    int nInputs, ref INPUT pInputs, int cbsize);

        /// <summary>
        /// Synthesizes keystrokes, mouse motions, and button clicks.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs,
           [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs,
           int cbSize);

        // 仮想キーコードをスキャンコードに変換
        [DllImport("user32.dll", EntryPoint = "MapVirtualKeyA")]
        private extern static int MapVirtualKey(
            int wCode, int wMapType);

        private const int INPUT_MOUSE = 0;                  // マウスイベント
        private const int INPUT_KEYBOARD = 1;               // キーボードイベント
        private const int INPUT_HARDWARE = 2;               // ハードウェアイベント

        private const int MOUSEEVENTF_MOVE = 0x1;           // マウスを移動する
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;    // 絶対座標指定
        private const int MOUSEEVENTF_LEFTDOWN = 0x2;       // 左　ボタンを押す
        private const int MOUSEEVENTF_LEFTUP = 0x4;         // 左　ボタンを離す
        private const int MOUSEEVENTF_RIGHTDOWN = 0x8;      // 右　ボタンを押す
        private const int MOUSEEVENTF_RIGHTUP = 0x10;       // 右　ボタンを離す
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x20;    // 中央ボタンを押す
        private const int MOUSEEVENTF_MIDDLEUP = 0x40;      // 中央ボタンを離す
        private const int MOUSEEVENTF_WHEEL = 0x800;        // ホイールを回転する
        private const int WHEEL_DELTA = 120;                // ホイール回転値

        private const int KEYEVENTF_KEYDOWN = 0x0;          // キーを押す
        private const int KEYEVENTF_EXTENDEDKEY = 0x1;      // 拡張コード
        private const int KEYEVENTF_KEYUP = 0x2;            // キーを離す
        private const int KEYEVENTF_UNICODE = 0x4;

        private const int VK_BACK = 8;                      // Backspaceキー
        private const int VK_SHIFT = 0x10;                  // SHIFTキー
        private const int VK_CONTROL = 0x11;                // Ctrlキー


        //[DllImport("User32.dll", CharSet = CharSet.Auto)]
        //private static extern Int32 PostMessageW(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

        [DllImport("user32.dll")]
        private static extern ushort GetAsyncKeyState(uint vkey);

        //[DllImport("user32.dll")]
        //private static extern uint keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint MAPVK_VK_TO_VSC = 0;

        private DateTime lastOutputDt;

        private void initializeKeyboardInput(ref INPUT input)
        {
            input.type = INPUT_KEYBOARD;
            input.ki.wVk = 0;
            input.ki.wScan = 0;
            input.ki.dwFlags = 0;
            input.ki.time = 0;
            input.ki.dwExtraInfo = MyMagicNumber;
        }

        private void setLeftCtrlInput(ref INPUT input, int keyEventFlag)
        {
            initializeKeyboardInput(ref input);
            input.ki.wVk = VK_CONTROL;
            input.ki.dwFlags = keyEventFlag;
        }

        private void setRightCtrlInput(ref INPUT input, int keyEventFlag)
        {
            setLeftCtrlInput(ref input, keyEventFlag);
            input.ki.dwFlags |= KEYEVENTF_EXTENDEDKEY;
        }

        /// <summary>
        /// Ctrlキーの事前上げ下げ
        /// </summary>
        /// <param name="leftCtrl"></param>
        /// <param name="rightCtrl"></param>
        private int upDownCtrlKeyInputs(INPUT[] inputs, int idx, bool bUp, out bool leftCtrl, out bool rightCtrl)
        {
            leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
            rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"bUp={bUp}, leftCtrl={leftCtrl}, rightCtrl={rightCtrl}");

            if (bUp && (leftCtrl || rightCtrl)) {
                // 両方一諸に上げる
                setLeftCtrlInput(ref inputs[idx++], KEYEVENTF_KEYUP);
                setRightCtrlInput(ref inputs[idx++], KEYEVENTF_KEYUP);
            } else if (!bUp && !(leftCtrl || rightCtrl)) {
                // 両方一諸に下げる
                setLeftCtrlInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                setRightCtrlInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
            }
            return idx;
        }

        /// <summary>
        /// Ctrlキーの事前上げ
        /// </summary>
        /// <param name="leftCtrl"></param>
        /// <param name="rightCtrl"></param>
        private int upCtrlKeyInputs(INPUT[] inputs, int idx, out bool leftCtrl, out bool rightCtrl)
        {
            return upDownCtrlKeyInputs(inputs, idx, true, out leftCtrl, out rightCtrl);
        }

        /// <summary>
        /// Ctrlキーの事前下げ
        /// </summary>
        /// <param name="leftCtrl"></param>
        /// <param name="rightCtrl"></param>
        //private int downCtrlKeyInputs(INPUT[] inputs, int idx, out bool leftCtrl, out bool rightCtrl)
        //{
        //    return upDownCtrlKeyInputs(inputs, idx, false, out leftCtrl, out rightCtrl);
        //}

        /// <summary>
        /// Ctrlキーの状態を戻す
        /// </summary>
        /// <param name="bUp">事前操作<</param>
        /// <param name="prevLeftCtrl"></param>
        /// <param name="prevRightCtrl"></param>
        private int revertCtrlKeyInputs(INPUT[] inputs, int idx, bool bUp, bool prevLeftCtrl, bool prevRightCtrl)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"bUp={bUp}, prevLeftCtrl={prevLeftCtrl}, prevRightCtrl={prevRightCtrl}");

            if (prevLeftCtrl && bUp) {
                setLeftCtrlInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
            } else if (!prevLeftCtrl && !bUp) {
                setLeftCtrlInput(ref inputs[idx++], KEYEVENTF_KEYUP);
            }

            if (prevRightCtrl && bUp) {
                setRightCtrlInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
            } else if (!prevRightCtrl && !bUp) {
                setRightCtrlInput(ref inputs[idx++], KEYEVENTF_KEYUP);
            }
            return idx;
        }

        /// <summary>
        /// Ctrlキーの事前上げ下げ (SendInput実行後、Waitあり)
        /// </summary>
        /// <param name="bUp">事前操作<</param>
        /// <param name="leftCtrl"></param>
        /// <param name="rightCtrl"></param>
        //private void sendInputUpDownCtrlKey(bool bUp, out bool leftCtrl, out bool rightCtrl)
        //{
        //    int waitUpMs = (ActiveWinSettings?.CtrlUpWaitMillisec)._value(-1)._geZeroOr(Settings.CtrlKeyUpGuardMillisec);
        //    var inputs = new INPUT[2];
        //    int idx = upDownCtrlKeyInputs(inputs, 0, bUp, out leftCtrl, out rightCtrl);
        //    if (idx > 0) {
        //        SendInput((uint)idx, inputs, Marshal.SizeOf(typeof(INPUT)));
        //        if (Settings.LoggingDecKeyInfo) logger.InfoH($"Ctrl Up/Down and Wait {waitUpMs} millisec");
        //        if (waitUpMs > 0) {
        //            Task.Delay(waitUpMs).Wait();            // やはりこれが無いと Ctrlが有効なままBSが渡ったりする
        //        }
        //    }
        //}

        /// <summary>
        /// Ctrlキーの事前上げ (SendInput 実行、Waitあり)
        /// </summary>
        /// <param name="leftCtrl"></param>
        /// <param name="rightCtrl"></param>
        //private void sendInputUpCtrlKey(out bool leftCtrl, out bool rightCtrl)
        //{
        //    sendInputUpDownCtrlKey(true, out leftCtrl, out rightCtrl);
        //}

        /// <summary>
        /// Ctrlキーの状態を戻す
        /// </summary>
        /// <param name="bUp">事前操作<</param>
        /// <param name="prevLeftCtrl"></param>
        /// <param name="prevRightCtrl"></param>
        //private void sendInputRevertCtrlKey(bool bUp, bool prevLeftCtrl, bool prevRightCtrl)
        //{
        //    var inputs = new INPUT[2];
        //    int idx = revertCtrlKeyInputs(inputs, 0, bUp, prevLeftCtrl, prevRightCtrl);
        //    if (idx > 0) {
        //        SendInput((uint)idx, inputs, Marshal.SizeOf(typeof(INPUT)));
        //        int waitDownMs = (ActiveWinSettings?.CtrlDownWaitMillisec)._value(-1)._geZeroOr(Settings.CtrlKeyDownGuardMillisec);
        //        if (Settings.LoggingDecKeyInfo) logger.InfoH($"Revert Ctrl and Wait {waitDownMs} millisec");
        //        if (waitDownMs > 0) {
        //            Task.Delay(waitDownMs).Wait();
        //        }
        //    }
        //}

        private void setLeftShiftInput(ref INPUT input, int keyEventFlag)
        {
            initializeKeyboardInput(ref input);
            input.ki.wVk = VK_SHIFT;
            input.ki.dwFlags = keyEventFlag;
        }

        private void setRightShiftInput(ref INPUT input, int keyEventFlag)
        {
            setLeftShiftInput(ref input, keyEventFlag);
            input.ki.dwFlags |= KEYEVENTF_EXTENDEDKEY;
        }

        /// <summary>
        /// Shiftキーの事前上げ下げ
        /// </summary>
        /// <param name="leftShift"></param>
        /// <param name="rightShift"></param>
        private int upDownShiftKeyInputs(INPUT[] inputs, int idx, bool bUp, out bool leftShift, out bool rightShift)
        {
            leftShift = (GetAsyncKeyState(VirtualKeys.LSHIFT) & 0x8000) != 0;
            rightShift = (GetAsyncKeyState(VirtualKeys.RSHIFT) & 0x8000) != 0;
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"bUp={bUp}, leftShift={leftShift}, rightShift={rightShift}");

            if (bUp && (leftShift || rightShift)) {
                // 両方一諸に上げる
                setLeftShiftInput(ref inputs[idx++], KEYEVENTF_KEYUP);
                setRightShiftInput(ref inputs[idx++], KEYEVENTF_KEYUP);
            } else if (!bUp && !(leftShift || rightShift)) {
                // 両方一諸に下げる
                setLeftShiftInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                setRightShiftInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
            }
            return idx;
        }

        /// <summary>
        /// Shiftキーの事前上げ
        /// </summary>
        /// <param name="leftShift"></param>
        /// <param name="rightShift"></param>
        //private int upShiftKeyInputs(INPUT[] inputs, int idx, out bool leftShift, out bool rightShift)
        //{
        //    return upDownShiftKeyInputs(inputs, idx, true, out leftShift, out rightShift);
        //}

        /// <summary>
        /// Shiftキーの事前下げ
        /// </summary>
        /// <param name="leftShift"></param>
        /// <param name="rightShift"></param>
        //private int downShiftKeyInputs(INPUT[] inputs, int idx, out bool leftShift, out bool rightShift)
        //{
        //    return upDownShiftKeyInputs(inputs, idx, false, out leftShift, out rightShift);
        //}

        /// <summary>
        /// Shiftキーの状態を戻す
        /// </summary>
        /// <param name="bUp">事前操作<</param>
        /// <param name="prevLeftShift"></param>
        /// <param name="prevRightShift"></param>
        private int revertShiftKeyInputs(INPUT[] inputs, int idx, bool bUp, bool prevLeftShift, bool prevRightShift)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"bUp={bUp}, prevLeftShift={prevLeftShift}, prevRightShift={prevRightShift}");

            if (prevLeftShift && bUp) {
                setLeftShiftInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
            } else if (!prevLeftShift && !bUp) {
                setLeftShiftInput(ref inputs[idx++], KEYEVENTF_KEYUP);
            }

            if (prevRightShift && bUp) {
                setRightShiftInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
            } else if (!prevRightShift && !bUp) {
                setRightShiftInput(ref inputs[idx++], KEYEVENTF_KEYUP);
            }
            return idx;
        }

        private int setVkeyInputs(ushort vkey, INPUT[] inputs, int idx)
        {
            initializeKeyboardInput(ref inputs[idx]);
            inputs[idx].ki.wVk = vkey;
            inputs[idx].ki.dwFlags = KEYEVENTF_KEYDOWN;
            ++idx;
            initializeKeyboardInput(ref inputs[idx]);
            inputs[idx].ki.wVk = vkey;
            inputs[idx].ki.dwFlags = KEYEVENTF_KEYUP;
            ++idx;
            return idx;
        }

        private int setStringInputs(char[] str, int strLen, INPUT[] inputs, int idx)
        {
            if (strLen > inputs._safeLength()) strLen = inputs._safeLength();
            for (int i = 0; i < strLen * 2; ++i) {
                initializeKeyboardInput(ref inputs[idx]);
                inputs[idx].ki.wScan = str[i / 2];
                inputs[idx].ki.dwFlags = (i % 2) == 0 ? KEYEVENTF_UNICODE : KEYEVENTF_KEYUP;
                ++idx;
            }
            return idx;
        }

        private void sendInputsWithHandlingDeckey(uint len, INPUT[] inputs, uint vkey)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"CALLED: len={len}, vkey={vkey}");

            if (len > 0) SendInput(len, inputs, Marshal.SizeOf(typeof(INPUT)));

            // Enterキーだったら、すぐに仮想鍵盤を移動するように MinValue とする
            lastOutputDt = vkey == (uint)Keys.Enter ? DateTime.MinValue : DateTime.Now;
        }

        /// <summary>
        /// キーボード入力をエミュレートして文字列を送出する
        /// </summary>
        /// <param name="str"></param>
        /// <param name="numBS"></param>
        public void SendString(char[] str, int strLen, int numBS)
        {
            bool loggingFlag = Settings.LoggingDecKeyInfo;
            if (loggingFlag) logger.InfoH($"CALLED: str={(str._isEmpty() ? "" : new string(str, 0, strLen._lowLimit(0)))}, numBS={numBS}");

            if (numBS < 0) numBS = 0;
            if (strLen < 0) strLen = 0;
            int numCtrlKeys = 2;            // leftCtrl と rightCtrl
            int inputsLen = strLen + numBS + numCtrlKeys * 2;

            var inputs = new INPUT[inputsLen * 2];

            int idx = 0;

            bool leftCtrl = false, rightCtrl = false;

            // Ctrl上げ
            idx = upCtrlKeyInputs(inputs, idx, out leftCtrl, out rightCtrl);
            if (loggingFlag) logger.InfoH($"upCtrl: idx={idx}");
            //sendInputUpCtrlKey(out leftCtrl, out rightCtrl);      // StikyNote など、Waitを入れても状況が変わらない

            // Backspace
            for (int i = 0; i < numBS; ++i) {
                idx = setVkeyInputs(VK_BACK, inputs, idx);
            }
            if (loggingFlag) logger.InfoH($"bs: idx={idx}");

            // 文字列
            idx = setStringInputs(str, strLen, inputs, idx);
            if (loggingFlag) logger.InfoH($"str: idx={idx}");

            // Ctrl戻し
            idx = revertCtrlKeyInputs(inputs, idx, true, leftCtrl, rightCtrl);
            if (loggingFlag) logger.InfoH($"revert: idx={idx}");

            // 送出
            sendInputsWithHandlingDeckey((uint)idx, inputs, VK_BACK);
        }

        BoolObject syncPostVkey = new BoolObject();

        /// <summary>
        /// 仮想キーComboを送出する<br/>
        /// </summary>
        /// <param name="n">キーダウンの数</param>
        public void SendVKeyCombo(uint modifier, uint vkey, int n)
        {
            bool loggingFlag = Settings.LoggingDecKeyInfo;
            if (loggingFlag) logger.InfoH($"CALLED: modifier={modifier:x}H, vkey={vkey:x}H, numKeys={n}");
            if (syncPostVkey.BusyCheck()) {
                if (loggingFlag) logger.InfoH($"IGNORED: numKeys={n}");
                return;
            }
            using (syncPostVkey) {
                lock (syncPostVkey) {
                    int numCtrlKeys = 2;            // leftCtrl と rightCtrl
                    var inputs = new INPUT[(n + numCtrlKeys * 2) * 2];

                    int idx = 0;

                    // Ctrl上げ(または下げ)
                    bool leftCtrl = false, rightCtrl = false;
                    bool bUp = (modifier & KeyModifiers.MOD_CONTROL) == 0;
                    idx = upDownCtrlKeyInputs(inputs, idx, bUp, out leftCtrl, out rightCtrl);
                    //sendInputUpDownCtrlKey(bUp, out leftCtrl, out rightCtrl);         // StikyNote など、Waitを入れても状況が変わらない

                    // Shift上げ(または下げ)
                    bool leftShift = false, rightShift = false;
                    bool bShiftUp = (modifier & KeyModifiers.MOD_SHIFT) == 0;
                    idx = upDownShiftKeyInputs(inputs, idx, bShiftUp, out leftShift, out rightShift);

                    // Vkey
                    for (int i = 0; i < n; ++i) {
                        idx = setVkeyInputs((ushort)vkey, inputs, idx);
                    }

                    // Shift戻し
                    idx = revertShiftKeyInputs(inputs, idx, bShiftUp, leftShift, rightShift);

                    // Ctrl戻し
                    idx = revertCtrlKeyInputs(inputs, idx, bUp, leftCtrl, rightCtrl);

                    // 送出
                    sendInputsWithHandlingDeckey((uint)idx, inputs, vkey);
                }
            }
        }

        /// <summary>
        /// 仮想キーを送出する<br/>
        /// </summary>
        /// <param name="n">キーダウンの数</param>
        //public void SendVirtualKey(uint vkey, int n)
        //{
        //    if (Settings.LoggingDecKeyInfo) logger.InfoH($"vkey={vkey:x}H, n={n}");

        //    var inputs = new INPUT[n * 2];

        //    int idx = 0;

        //    // Vkey
        //    for (int i = 0; i < n; ++i) {
        //        idx = setVkeyInputs((ushort)vkey, inputs, idx);
        //    }

        //    // 送出
        //    sendInputsWithHandlingDeckey((uint)idx, inputs, vkey);
        //}

        /// <summary>
        /// 文字列を送出する<br/>
        /// 文字送出前に numBSだけBackspaceを送る<br/>
        /// 必要ならクリップボードにコピーしてから Ctrl-V を送る
        /// </summary>
        public void SendStringViaClipboardIfNeeded(char[] str, int numBS)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"ActiveWinHandle={(int)ActiveWinHandle:x}H, str=\"{str._toString()}\", numBS={numBS}");

            if (ActiveWinHandle != IntPtr.Zero && ((str._notEmpty() && str[0] != 0) || numBS > 0)) {
                int len = str._isEmpty() ? 0 : str._findIndex(x => x == 0);
                if (len < 0) len = str._safeLength();
                if (Settings.MinLeghthViaClipboard <= 0 || len < Settings.MinLeghthViaClipboard) {
                    // 自前で送出
                    SendString(str, len, numBS);
                } else {
                    // クリップボードにコピー
                    Clipboard.SetText(new string(str, 0, len));
                    // Ctrl-V を送る (SendVirtualKeys の中でも upDownCtrlKey/revertCtrlKey をやっている)
                    SendString(null, 0, numBS);
                    if (numBS > 0 && Settings.PreWmCharGuardMillisec > 0) {
                        int waitMs = (int)(Math.Pow(numBS, Settings.ReductionExponet._lowLimit(0.5)) * Settings.PreWmCharGuardMillisec);
                        if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"Wait {waitMs} ms: PreWmCharGuardMillisec={Settings.PreWmCharGuardMillisec}, numBS={numBS}, reductionExp={Settings.ReductionExponet}");
                        Helper.WaitMilliSeconds(waitMs);
                    }
                    SendVKeyCombo(VirtualKeys.CtrlV_VKeyCombo.modifier, VirtualKeys.CtrlV_VKeyCombo.vkey, 1);
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
            if (bLog) logger.Info("ENTER");

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
                if (bLog) logger.Info(() => $"fgHan={(int)fgHan:x}H, focusHan={(int)focusHan:x}H");

                guiThreadInfo.GetScreenCaretPos(ref ActiveWinCaretPos);
                if (bLog) logger.Info(() => $"WndClass={ActiveWinClassName}: focus caret pos=({ActiveWinCaretPos.X}, {ActiveWinCaretPos.Y})");

                if (focusHan != IntPtr.Zero || ActiveWinClassName._equalsTo("ConsoleWindowClass")) break;  // CMD Prompt の場合は Focus が取れないっぽい?
                if (bLog || Logger.IsInfoEnabled) logger.Warn($"RETRY: count={count + 1}");
            }

            if (focusHan == IntPtr.Zero) {
                if (bLog || Logger.IsInfoEnabled) logger.Warn("Can't get window handle with focus");
            }
            ActiveWinHandle = (focusHan == IntPtr.Zero) ? fgHan : focusHan;

            if (bLog) logger.Info(() => $"LEAVE: ActiveWinHandle={(int)ActiveWinHandle:x}H");
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
                var vkbFixedPos = ActiveWinSettings?.VkbFixedPos;
                if (vkbFixedPos != null) {
                    logger.InfoH($"vkbFixedPos=({vkbFixedPos.Select(m => m.ToString())._join(",")})");
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

        private bool bFirstMove = true;

        /// <summary>
        /// 仮想鍵盤をカレットの近くに移動する (仮想鍵盤自身がアクティブの場合は移動しない)<br/>
        /// これが呼ばれるのはデコーダがONのときだけ
        /// </summary>
        private void moveWindow(bool bDiffWin, bool bMoveMandatory, bool bLog)
        {
            ActiveWinSettings = Settings.GetWinClassSettings(ActiveWinClassName);
            if (bLog || bFirstMove) {
                logger.InfoH($"CALLED: diffWin={bDiffWin}, mandatory={bMoveMandatory}, firstMove={bFirstMove}");
                loggingCaretInfo();
            }

            if (bFirstMove || (!FrmMain.IsVirtualKeyboardFreezed && !ActiveWinClassName.EndsWith(DlgVkbClassNameHash) && ActiveWinClassName._ne("SysShadow"))) {
                if (bFirstMove || bMoveMandatory ||
                    ((Math.Abs(ActiveWinCaretPos.X) >= NoMoveOffset || Math.Abs(ActiveWinCaretPos.Y) >= NoMoveOffset) &&
                     (Math.Abs(ActiveWinCaretPos.X - prevCaretPos.X) >= NoMoveOffset || Math.Abs(ActiveWinCaretPos.Y - prevCaretPos.Y) >= NoMoveOffset) &&
                     isInValidCaretMargin(ActiveWinSettings))
                   ) {
                    int xOffset = (ActiveWinSettings?.CaretOffset)._getNth(0, Settings.VirtualKeyboardOffsetX);
                    int yOffset = (ActiveWinSettings?.CaretOffset)._getNth(1, Settings.VirtualKeyboardOffsetY);
                    int xFixed = (ActiveWinSettings?.VkbFixedPos)._getNth(0, -1)._geZeroOr(Settings.VirtualKeyboardFixedPosX);
                    int yFixed = (ActiveWinSettings?.VkbFixedPos)._getNth(1, -1)._geZeroOr(Settings.VirtualKeyboardFixedPosY);
                    //double dpiRatio = 1.0; //FrmVkb.GetDeviceDpiRatio();
                    if (bLog || bFirstMove) logger.InfoH($"CaretPos.X={ActiveWinCaretPos.X}, CaretPos.Y={ActiveWinCaretPos.Y}, xOffset={xOffset}, yOffset={yOffset}, xFixed={xFixed}, yFixed={yFixed}");
                    if (ActiveWinCaretPos.X >= 0) {
                        int cX = ActiveWinCaretPos.X;
                        int cY = ActiveWinCaretPos.Y;
                        int cW = ActiveWinCaretPos.Width;
                        int cH = ActiveWinCaretPos.Height;
                        if (bLog) {
                            logger.InfoH($"MOVE: X={cX}, Y={cY}, W={cW}, H={cH}, OX={xOffset}, OY={yOffset}");
                            if (Settings.LoggingActiveWindowInfo) {
                                var dpis = ScreenInfo.ScreenDpi.Select(x => $"{x}")._join(", ");
                                FrmVkb.SetTopText($"DR={dpis}, CX={cX},CY={cY},CW={cW},CH={cH},OX={xOffset},OY={yOffset}");
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
                                int cBottom = cY + cH;
                                fX = cX + xOffset;
                                fY = cBottom + yOffset;
                                int fRight = fX + fW;
                                int fBottom = fY + fH;
                                Rectangle rect = ScreenInfo.GetScreenContaining(cX, cY);
                                if (fRight >= rect.X + rect.Width) fX = cX - fW - xOffset;
                                if (fBottom >= rect.Y + rect.Height) fY = cY - fH - yOffset;
                            }
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
                bFirstMove = false;
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

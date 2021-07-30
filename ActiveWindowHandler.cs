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

        //private void keybdEvent(uint vkey, uint eventFlags, bool bExt = false)
        //{
        //    // これは不要のようだ
        //    //uint sc = MapVirtualKey(Vkeys.LCONTROL, MAPVK_VK_TO_VSC);
        //    uint sc = 0;

        //    if (bExt) eventFlags |= KEYEVENTF_EXTENDEDKEY;
        //    keybd_event((byte)vkey, (byte)sc, eventFlags, UIntPtr.Zero);
        //}

        //private void keybd_up(uint vkey, bool bExt = false)
        //{
        //    keybdEvent(vkey, KEYEVENTF_KEYUP, bExt);
        //}

        //private void keybd_down(uint vkey, bool bExt = false)
        //{
        //    keybdEvent(vkey, KEYEVENTF_KEYDOWN, bExt);
        //}

        private DateTime lastOutputDt;

        ///// <summary>
        ///// Ctrlキーの事前上げ下げ
        ///// </summary>
        ///// <param name="leftCtrl"></param>
        ///// <param name="rightCtrl"></param>
        //private void upDownCtrlKey(bool bUp, out bool leftCtrl, out bool rightCtrl)
        //{
        //    bool loggingFlag = Settings.LoggingHotKeyInfo;
        //    int waitUpMs = (ActiveWinSettings?.CtrlUpWaitMillisec)._value(-1)._geZeroOr(Settings.CtrlKeyUpGuardMillisec);
        //    leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
        //    rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;
        //    if (bUp && (leftCtrl || rightCtrl)) {
        //        // 両方一諸に上げるようにした
        //        keybd_up(VirtualKeys.CONTROL);              // extended を付けないと LCONTROL 扱いのようだ
        //        keybd_up(VirtualKeys.CONTROL, true);        // extended を付けると RCONTROL 扱いのようだ
        //        if (loggingFlag) logger.InfoH($"Ctrl Up and wait {waitUpMs} millisec");
        //        if (waitUpMs > 0) {
        //            Task.Delay(waitUpMs).Wait();            // やはりこれが無いと Ctrlが有効なままBSが渡ったりする
        //        }
        //    } else if (!bUp && !(leftCtrl || rightCtrl)) {
        //        keybd_down(VirtualKeys.CONTROL);              // extended を付けないと LCONTROL 扱いのようだ
        //        keybd_down(VirtualKeys.CONTROL, true);        // extended を付けると RCONTROL 扱いのようだ
        //        if (loggingFlag) logger.InfoH($"Ctrl Down and wait {waitUpMs} millisec");
        //        if (waitUpMs > 0) {
        //            Task.Delay(waitUpMs).Wait();            // やはりこれが無いと Ctrlが有効なままBSが渡ったりする
        //        }
        //    }
        //}

        ///// <summary>
        ///// Ctrlキーの状態を戻す
        ///// </summary>
        ///// <param name="prevLeftCtrl"></param>
        ///// <param name="prevRightCtrl"></param>
        //private void revertCtrlKey(bool prevLeftCtrl, bool prevRightCtrl)
        //{
        //    bool loggingFlag = Settings.LoggingHotKeyInfo;
        //    int waitDownMs = (ActiveWinSettings?.CtrlDownWaitMillisec)._value(-1)._geZeroOr(Settings.CtrlKeyDownGuardMillisec);

        //    bool leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
        //    if (prevLeftCtrl && !leftCtrl) {
        //        if (loggingFlag) logger.InfoH($"LeftCtrl Down after waiting {waitDownMs} millisec");
        //        if (waitDownMs > 0) {
        //            Task.Delay(waitDownMs).Wait();
        //        }
        //        keybd_down(VirtualKeys.CONTROL);            // extended を付けないと LCONTROL 扱いのようだ
        //    } else if (!prevLeftCtrl && leftCtrl) {
        //        if (loggingFlag) logger.InfoH($"LeftCtrl Up after waiting {waitDownMs} millisec");
        //        if (waitDownMs > 0) {
        //            Task.Delay(waitDownMs).Wait();
        //        }
        //        keybd_up(VirtualKeys.CONTROL);            // extended を付けないと LCONTROL 扱いのようだ
        //    }

        //    bool rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;
        //    if (prevRightCtrl && !rightCtrl) {
        //        if (loggingFlag) logger.InfoH($"RightCtrl Down after waiting {waitDownMs} millisec");
        //        if (waitDownMs > 0) {
        //            Task.Delay(waitDownMs).Wait();
        //        }
        //        keybd_down(VirtualKeys.CONTROL, true);      // extended を付けると RCONTROL 扱いのようだ
        //    } else if (!prevRightCtrl && rightCtrl) {
        //        if (loggingFlag) logger.InfoH($"RightCtrl Up after waiting {waitDownMs} millisec");
        //        if (waitDownMs > 0) {
        //            Task.Delay(waitDownMs).Wait();
        //        }
        //        keybd_up(VirtualKeys.CONTROL, true);      // extended を付けると RCONTROL 扱いのようだ
        //    }
        //}

        private void initializeKeyboardInput(ref INPUT input)
        {
            input.type = INPUT_KEYBOARD;
            input.ki.wVk = 0;
            input.ki.wScan = 0;
            input.ki.dwFlags = 0;
            input.ki.time = 0;
            input.ki.dwExtraInfo = 0;
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
            if (Settings.LoggingHotKeyInfo) logger.InfoH($"bUp={bUp}, leftCtrl={leftCtrl}, rightCtrl={rightCtrl}");

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
        private int downCtrlKeyInputs(INPUT[] inputs, int idx, out bool leftCtrl, out bool rightCtrl)
        {
            return upDownCtrlKeyInputs(inputs, idx, false, out leftCtrl, out rightCtrl);
        }

        /// <summary>
        /// Ctrlキーの状態を戻す
        /// </summary>
        /// <param name="bUp">事前操作<</param>
        /// <param name="prevLeftCtrl"></param>
        /// <param name="prevRightCtrl"></param>
        private int revertCtrlKeyInputs(INPUT[] inputs, int idx, bool bUp, bool prevLeftCtrl, bool prevRightCtrl)
        {
            if (Settings.LoggingHotKeyInfo) logger.InfoH($"bUp={bUp}, prevLeftCtrl={prevLeftCtrl}, prevRightCtrl={prevRightCtrl}");

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
        private void sendInputUpDownCtrlKey(bool bUp, out bool leftCtrl, out bool rightCtrl)
        {
            int waitUpMs = (ActiveWinSettings?.CtrlUpWaitMillisec)._value(-1)._geZeroOr(Settings.CtrlKeyUpGuardMillisec);
            var inputs = new INPUT[2];
            int idx = upDownCtrlKeyInputs(inputs, 0, bUp, out leftCtrl, out rightCtrl);
            if (idx > 0) {
                SendInput((uint)idx, inputs, Marshal.SizeOf(typeof(INPUT)));
                if (Settings.LoggingHotKeyInfo) logger.InfoH($"Ctrl Up/Down and Wait {waitUpMs} millisec");
                if (waitUpMs > 0) {
                    Task.Delay(waitUpMs).Wait();            // やはりこれが無いと Ctrlが有効なままBSが渡ったりする
                }
            }
        }

        /// <summary>
        /// Ctrlキーの事前上げ (SendInput 実行、Waitあり)
        /// </summary>
        /// <param name="leftCtrl"></param>
        /// <param name="rightCtrl"></param>
        private void sendInputUpCtrlKey(out bool leftCtrl, out bool rightCtrl)
        {
            sendInputUpDownCtrlKey(true, out leftCtrl, out rightCtrl);
        }

        /// <summary>
        /// Ctrlキーの状態を戻す
        /// </summary>
        /// <param name="bUp">事前操作<</param>
        /// <param name="prevLeftCtrl"></param>
        /// <param name="prevRightCtrl"></param>
        private void sendInputRevertCtrlKey(bool bUp, bool prevLeftCtrl, bool prevRightCtrl)
        {
            var inputs = new INPUT[2];
            int idx = revertCtrlKeyInputs(inputs, 0, bUp, prevLeftCtrl, prevRightCtrl);
            if (idx > 0) {
                SendInput((uint)idx, inputs, Marshal.SizeOf(typeof(INPUT)));
                int waitDownMs = (ActiveWinSettings?.CtrlDownWaitMillisec)._value(-1)._geZeroOr(Settings.CtrlKeyDownGuardMillisec);
                if (Settings.LoggingHotKeyInfo) logger.InfoH($"Revert Ctrl and Wait {waitDownMs} millisec");
                if (waitDownMs > 0) {
                    Task.Delay(waitDownMs).Wait();
                }
            }
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

        private void sendInputsWithHandlingHotkey(uint len, INPUT[] inputs, uint vkey)
        {
            if (Settings.LoggingHotKeyInfo) logger.InfoH($"CALLED: len={len}, vkey={vkey}");

            int hotkey = 0;
            int ctrlHotkey = 0;
            if (vkey > 0) {
                // まずホットキーの解除
                hotkey = VirtualKeys.GetHotKeyFromCombo(0, vkey);
                ctrlHotkey = VirtualKeys.GetHotKeyFromCombo(KeyModifiers.MOD_CONTROL, vkey);
                HotKeyHandler.UnregisterHotKeyTemporary(hotkey);
                HotKeyHandler.UnregisterHotKeyTemporary(ctrlHotkey);
            }

            if (len > 0) SendInput(len, inputs, Marshal.SizeOf(typeof(INPUT)));

            if (vkey > 0) {
                // ホットキーの再登録
                HotKeyHandler.ResumeHotKey(hotkey);
                HotKeyHandler.ResumeHotKey(ctrlHotkey);
            }

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
            bool loggingFlag = Settings.LoggingHotKeyInfo;
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
            sendInputsWithHandlingHotkey((uint)idx, inputs, VK_BACK);
        }

        BoolObject syncPostVkey = new BoolObject();

        public void SendVirtualKeys(VKeyCombo combo, int n)
        {
            bool loggingFlag = Settings.LoggingHotKeyInfo;
            if (loggingFlag) logger.InfoH($"CALLED: combo=({combo.modifier:x}H:{combo.vkey:x}H), numKeys={n}");
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
                    bool bUp = (combo.modifier & KeyModifiers.MOD_CONTROL) == 0;
                    idx = upDownCtrlKeyInputs(inputs, idx, bUp, out leftCtrl, out rightCtrl);
                    //sendInputUpDownCtrlKey(bUp, out leftCtrl, out rightCtrl);         // StikyNote など、Waitを入れても状況が変わらない

                    // Vkey
                    for (int i = 0; i < n; ++i) {
                        idx = setVkeyInputs((ushort)combo.vkey, inputs, idx);
                    }

                    // Ctrl戻し
                    idx = revertCtrlKeyInputs(inputs, idx, bUp, leftCtrl, rightCtrl);

                    // 送出
                    sendInputsWithHandlingHotkey((uint)idx, inputs, combo.vkey);
                }
            }
        }

        /// <summary>
        /// 仮想キーを送出する<br/>
        /// </summary>
        /// <param name="n">キーダウンの数</param>
        public void SendVirtualKey(uint vkey, int n)
        {
            if (Settings.LoggingHotKeyInfo) logger.InfoH($"vkey={vkey:x}H, n={n}");

            var inputs = new INPUT[n * 2];

            int idx = 0;

            // Vkey
            for (int i = 0; i < n; ++i) {
                idx = setVkeyInputs((ushort)vkey, inputs, idx);
            }

            // 送出
            sendInputsWithHandlingHotkey((uint)idx, inputs, vkey);
        }

        ///// <summary>
        ///// アクティブウィンドウに文字を送出する
        ///// </summary>
        ///// <param name="ch"></param>
        //public void PostChar(char ch)
        //{
        //    PostMessageW(ActiveWinHandle, WM_Defs.WM_CHAR, ch, 1);
        //    //PostMessageW(ActiveWinHandle, WM_Defs.WM_IME_CHAR, ch, 1);
        //    //if (ch >= 0x100) CharCountDic[ch] = CharCountDic._safeGet(ch) + 1;
        //}

        ///// <summary>
        ///// アクティブウィンドウに仮想キーを送出する<br/>
        ///// bCheckCtrl == true なら、 Ctrlキーが立っている場合は、何もせず戻る
        ///// </summary>
        ///// <param name="n">キーダウンの数</param>
        //public void PostVirtualKey(uint vkey, int n, bool bCheckCtrl)
        //{
        //    if (Settings.LoggingHotKeyInfo) logger.InfoH($"vkey={vkey:x}H, n={n}");

        //    if (bCheckCtrl) {
        //        if ((GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0) {
        //            if (Settings.LoggingHotKeyInfo) logger.InfoH("LeftCtrl ON; return");
        //            return;
        //        }
        //        if ((GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0) {
        //            if (Settings.LoggingHotKeyInfo) logger.InfoH("RightCtrl ON; return");
        //            return;
        //        }
        //    }

        //    // まずホットキーの解除
        //    int hotkey = VirtualKeys.GetHotKeyFromCombo(0, vkey);
        //    int ctrlHotkey = VirtualKeys.GetHotKeyFromCombo(KeyModifiers.MOD_CONTROL, vkey);
        //    HotKeyHandler.UnregisterHotKeyTemporary(hotkey);
        //    HotKeyHandler.UnregisterHotKeyTemporary(ctrlHotkey);
        //    // キーの Down/Up
        //    for (int i = 0; i < n; ++i) {
        //        keybd_down(vkey);
        //        keybd_up(vkey);
        //    }
        //    // ホットキーの再登録
        //    HotKeyHandler.ResumeHotKey(hotkey);
        //    HotKeyHandler.ResumeHotKey(ctrlHotkey);

        //    // Enterキーだったら、すぐに仮想鍵盤を移動するように MinValue とする
        //    lastOutputDt = vkey == (uint)Keys.Enter ? DateTime.MinValue : DateTime.Now;
        //}

        ///// <summary>
        ///// アクティブウィンドウにVkeyを送出する
        ///// </summary>
        ///// <param name="n">vkeyの数</param>
        //public void PostVirtualKeys(VKeyCombo combo, int n)
        //{
        //    bool loggingFlag = Settings.LoggingHotKeyInfo;
        //    if (loggingFlag) logger.InfoH($"CALLED: combo=({combo.modifier:x}H:{combo.vkey:x}H), numKeys={n}");
        //    if (syncPostVkey.BusyCheck()) {
        //        if (loggingFlag) logger.InfoH($"IGNORED: numKeys={n}");
        //        return;
        //    }
        //    using (syncPostVkey) {
        //        lock (syncPostVkey) {
        //            bool leftCtrl = false, rightCtrl = false;
        //            bool bUp = (combo.modifier & KeyModifiers.MOD_CONTROL) == 0;
        //            upDownCtrlKey(bUp, out leftCtrl, out rightCtrl);

        //            PostVirtualKey(combo.vkey, n, false);

        //            revertCtrlKey(leftCtrl, rightCtrl);
        //        }
        //    }
        //}

        ///// <summary>
        ///// アクティブウィンドウにBSを送出する
        ///// </summary>
        ///// <param name="n">BSの数</param>
        //public void PostBackSpaces(int n)
        //{
        //    if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"ActiveWinHandle={(int)ActiveWinHandle:x}H, n={n}");
        //    if (n > 0) PostVirtualKeys(new VKeyCombo(0, (uint)Keys.Back), n);
        //}

        ///// <summary>
        ///// アクティブウィンドウに文字列を送出する<br/>
        ///// 文字送出前に nPreKeys * PreWmCharGuardMillisec だけ、wait を入れる。<br/>
        ///// 必要ならクリップボード経由で送り付ける
        ///// </summary>
        //public void PostStringViaClipboardIfNeeded(char[] str, int nPreKeys)
        //{
        //    if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"ActiveWinHandle={(int)ActiveWinHandle:x}H, WM_CHAR={WM_Defs.WM_CHAR:x}H, str=\"{str._toString()}\"");

        //    if (ActiveWinHandle != IntPtr.Zero && str.Length > 0 && str[0] != 0) {
        //        // Backspace の送出があった場合、すぐに PostMessage を呼ぶと、Backspace との順序が入れ替わることがあるっぽい？ので、少し wait を入れてみる
        //        // オリジナルの漢直Winでは、waitではなくWaitForInputIdle()を呼んでいた。
        //        // しかしオリジナル漢直では文字送出とBackspaceの入れ替わりがあったことを考えると、やはり一定時間 wait すべきではないかと考える。
        //        if (nPreKeys > 0 && Settings.PreWmCharGuardMillisec > 0) {
        //            int waitMs = (int)(Math.Pow(nPreKeys, Settings.ReductionExponet._lowLimit(0.5)) * Settings.PreWmCharGuardMillisec);
        //            if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"Wait {waitMs} ms: PreWmCharGuardMillisec={Settings.PreWmCharGuardMillisec}, nPreKeys={nPreKeys}, reductionExp={Settings.ReductionExponet}");
        //            Helper.WaitMilliSeconds(waitMs);
        //        }

        //        int len = str._findIndex(x => x == 0);
        //        if (Settings.MinLeghthViaClipboard <= 0 || len < Settings.MinLeghthViaClipboard) {
        //            // 自前で送出
        //            bool leftCtrl, rightCtrl;
        //            upDownCtrlKey(true, out leftCtrl, out rightCtrl);

        //            foreach (char ch in str) {
        //                if (ch == 0) break;
        //                PostChar(ch);
        //            }

        //            revertCtrlKey(leftCtrl, rightCtrl);
        //        } else {
        //            // クリップボードにコピー
        //            Clipboard.SetText(new string(str, 0, len));
        //            // Ctrl-V を送る (PostVirtualKeys の中でも upDownCtrlKey/revertCtrlKey をやっている)
        //            PostVirtualKeys(VirtualKeys.GetVKeyComboFromHotKey(HotKeys.CTRL_V_HOTKEY).Value, 1);
        //        }

        //        lastOutputDt = DateTime.Now;
        //    }
        //}

        /// <summary>
        /// アクティブウィンドウに文字列を送出する<br/>
        /// 文字送出前に numBSだけBackspaceを送る<br/>
        /// 必要ならクリップボードにコピーする
        /// </summary>
        public void SendStringViaClipboardIfNeeded(char[] str, int numBS)
        {
            if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"ActiveWinHandle={(int)ActiveWinHandle:x}H, str=\"{str._toString()}\", numBS={numBS}");

            if (ActiveWinHandle != IntPtr.Zero && ((str._notEmpty() && str[0] != 0) || numBS > 0)) {
                int len = str._isEmpty() ? 0 : str._findIndex(x => x == 0);
                if (len < 0) len = str._safeLength();
                if (Settings.MinLeghthViaClipboard <= 0 || len < Settings.MinLeghthViaClipboard) {
                    // 自前で送出
                    SendString(str, len, numBS);
                } else {
                    // クリップボードにコピー
                    Clipboard.SetText(new string(str, 0, len));
                    // Ctrl-V を送る (PostVirtualKeys の中でも upDownCtrlKey/revertCtrlKey をやっている)
                    //SendVirtualKeys(VirtualKeys.GetVKeyComboFromHotKey(HotKeys.CTRL_V_HOTKEY).Value, 1, numBS);
                    SendString(null, 0, numBS);
                    if (numBS > 0 && Settings.PreWmCharGuardMillisec > 0) {
                        int waitMs = (int)(Math.Pow(numBS, Settings.ReductionExponet._lowLimit(0.5)) * Settings.PreWmCharGuardMillisec);
                        if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"Wait {waitMs} ms: PreWmCharGuardMillisec={Settings.PreWmCharGuardMillisec}, numBS={numBS}, reductionExp={Settings.ReductionExponet}");
                        Helper.WaitMilliSeconds(waitMs);
                    }
                    //PostVirtualKeys(VirtualKeys.GetVKeyComboFromHotKey(HotKeys.CTRL_V_HOTKEY).Value, 1);
                    SendVirtualKeys(VirtualKeys.GetVKeyComboFromHotKey(HotKeys.CTRL_V_HOTKEY).Value, 1);
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
                    //double dpiRatio = 1.0; //FrmVkb.GetDeviceDpiRatio();
                    if (bLog) logger.InfoH($"CaretPos.X={ActiveWinCaretPos.X}, CaretPos.Y={ActiveWinCaretPos.Y}, xOffset={xOffset}, yOffset={yOffset}");
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
                            int cBottom = cY + cH;
                            int fX = cX + xOffset;
                            int fY = cBottom + yOffset;
                            int fW = frm.Size.Width;
                            int fH = frm.Size.Height;
                            int fRight = fX + fW;
                            int fBottom = fY + fH;
                            Rectangle rect = ScreenInfo.GetScreenContaining(cX, cY);
                            if (fRight >= rect.X + rect.Width) fX = cX - fW - xOffset;
                            if (fBottom >= rect.Y + rect.Height) fY = cY - fH - yOffset;
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

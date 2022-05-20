using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Utils;

namespace KanchokuWS.Handler
{
    using Keys = System.Windows.Forms.Keys;

    class SendInputHandler
    {
        private static Logger logger = Logger.GetLogger();

        public const int MyMagicNumber = 1959;

        /// <summary> シングルトンオブジェクト </summary>
        public static SendInputHandler Singleton { get; private set; }

        public static SendInputHandler CreateSingleton()
        {
            Singleton = new SendInputHandler();
            return Singleton;
        }

        //private bool bDisposed = false;

        public static void DisposeSingleton()
        {
            logger.Info("Disposed");
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        private SendInputHandler()
        {
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
        private const int VK_LSHIFT = 0xa0;                 // LSHIFTキー
        private const int VK_RSHIFT = 0xa1;                 // RSHIFTキー
        private const int VK_PACKET = 0xe7;                 // Unicode 

        private const int WM_CLOSE = 0x0010;
        private const int WM_COPYDATA = 0x4A;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;
        private const int WM_UNICHAR = 0x0109;
        private const int WM_IME_CHAR = 0x0286;
        private const int WM_HOTKEY = 0x0312;

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 PostMessageW(IntPtr hWnd, uint Msg, uint wParam, uint lParam);


        [DllImport("user32.dll")]
        private static extern ushort GetAsyncKeyState(uint vkey);

        //[DllImport("user32.dll")]
        //private static extern uint keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private void sendInput(uint len, INPUT[] inputs)
        {
            SendInput(len, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private const uint MAPVK_VK_TO_VSC = 0;

        public DateTime LastOutputDt { get; private set; }

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
            input.ki.dwFlags |= KEYEVENTF_EXTENDEDKEY;  // 右Ctrlは、0xa3 ではなく、EXTENTED を設定する必要あり
        }

        public void GetCtrlKeyState(out bool leftCtrl, out bool rightCtrl)
        {
            leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
            rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;
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
            if (Settings.LoggingDecKeyInfo) logger.Info($"bUp={bUp}, leftCtrl={leftCtrl}, rightCtrl={rightCtrl}");

            if (bUp) {
                // DOWNしているほうだけ上げる
                if (leftCtrl) setLeftCtrlInput(ref inputs[idx++], KEYEVENTF_KEYUP);
                if (rightCtrl) setRightCtrlInput(ref inputs[idx++], KEYEVENTF_KEYUP);
            } else {
                if (!leftCtrl && !rightCtrl) {
                    // leftだけ下げる
                    setLeftCtrlInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                    //setRightCtrlInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                }
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
        /// Ctrlキーの状態を戻す
        /// </summary>
        /// <param name="bUp">事前操作<</param>
        /// <param name="prevLeftCtrl"></param>
        /// <param name="prevRightCtrl"></param>
        private int revertCtrlKeyInputs(INPUT[] inputs, int idx, bool bUp, bool prevLeftCtrl, bool prevRightCtrl)
        {
            if (Settings.LoggingDecKeyInfo) logger.Info($"bUp={bUp}, prevLeftCtrl={prevLeftCtrl}, prevRightCtrl={prevRightCtrl}");

            if (bUp) {
                // 事前操作がUPだった
                if (prevLeftCtrl) setLeftCtrlInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                if (prevRightCtrl) setRightCtrlInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
            } else {
                // 事前操作がDOWNだった⇒左Ctrlだけを上げる
                if (!prevLeftCtrl && !prevRightCtrl) setLeftCtrlInput(ref inputs[idx++], KEYEVENTF_KEYUP);
            }
            return idx;
        }

        public void RevertUpCtrlKey(bool prevLeftCtrl, bool prevRightCtrl)
        {
            if (Settings.LoggingDecKeyInfo) logger.Info($"CALLED");

            var inputs = new INPUT[2];
            
            // Ctrl戻し
            int idx = revertCtrlKeyInputs(inputs, 0, true, prevLeftCtrl, prevRightCtrl);
            if (Settings.LoggingDecKeyInfo) logger.Info($"revert: idx={idx}");

            // 送出
            sendInputsWithHandlingDeckey((uint)idx, inputs, VK_BACK);
        }

        private void setLeftShiftInput(ref INPUT input, int keyEventFlag)
        {
            initializeKeyboardInput(ref input);
            input.ki.wVk = VK_LSHIFT;           // 右シフトは EXTENTED ではなく、0xa1 を設定する必要あり
            input.ki.dwFlags = keyEventFlag;
        }

        private void setRightShiftInput(ref INPUT input, int keyEventFlag)
        {
            initializeKeyboardInput(ref input);
            input.ki.wVk = VK_RSHIFT;
            input.ki.dwFlags = keyEventFlag;
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
            if (Settings.LoggingDecKeyInfo) logger.Info($"bUp={bUp}, leftShift={leftShift}, rightShift={rightShift}");

            if (bUp) {
                // 下がっているほうだけ上げる
                if (leftShift) setLeftShiftInput(ref inputs[idx++], KEYEVENTF_KEYUP);
                if (rightShift) setRightShiftInput(ref inputs[idx++], KEYEVENTF_KEYUP);
            } else {
                if (!leftShift && !rightShift) {
                    // 左だけ下げる
                    setLeftShiftInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                    //setRightShiftInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                }
            }
            return idx;
        }

        /// <summary>
        /// Shiftキーの事前上げ
        /// </summary>
        /// <param name="leftShift"></param>
        /// <param name="rightShift"></param>
        private int upShiftKeyInputs(INPUT[] inputs, int idx, out bool leftShift, out bool rightShift)
        {
            return upDownShiftKeyInputs(inputs, idx, true, out leftShift, out rightShift);
        }

        /// <summary>
        /// Shiftキーの状態を戻す
        /// </summary>
        /// <param name="bUp">事前操作<</param>
        /// <param name="prevLeftShift"></param>
        /// <param name="prevRightShift"></param>
        private int revertShiftKeyInputs(INPUT[] inputs, int idx, bool bUp, bool prevLeftShift, bool prevRightShift)
        {
            if (Settings.LoggingDecKeyInfo) logger.Info($"bUp={bUp}, prevLeftShift={prevLeftShift}, prevRightShift={prevRightShift}");

            if (bUp) {
                // 事前操作がUPだった
                if (prevLeftShift) setLeftShiftInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                if (prevRightShift) setRightShiftInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
            } else {
                // 事前操作がDOWNだった⇒左Shiftだけを上げる
                if (!prevLeftShift && !prevRightShift) setLeftShiftInput(ref inputs[idx++], KEYEVENTF_KEYUP);
            }
            return idx;
        }

        private int setVkeyInputs(ushort vkey, INPUT[] inputs, int idx)
        {
            initializeKeyboardInput(ref inputs[idx]);
            ushort wScan = (ushort)MapVirtualKey(vkey, 0);
            int extendedFlag = vkey >= (ushort)Keys.PageUp && vkey <= (ushort)Keys.Down || vkey == (ushort)Keys.Insert || vkey == (ushort)Keys.Delete ? KEYEVENTF_EXTENDEDKEY : 0;
            inputs[idx].ki.wVk = vkey;
            inputs[idx].ki.wScan = wScan;
            inputs[idx].ki.dwFlags = KEYEVENTF_KEYDOWN | extendedFlag;
            ++idx;
            initializeKeyboardInput(ref inputs[idx]);
            inputs[idx].ki.wVk = vkey;
            inputs[idx].ki.wScan = wScan;
            inputs[idx].ki.dwFlags = KEYEVENTF_KEYUP | extendedFlag;
            ++idx;
            return idx;
        }

        private int setUnicodeInputs(char uc, INPUT[] inputs, int idx)
        {
            initializeKeyboardInput(ref inputs[idx]);
            inputs[idx].ki.wScan = (ushort)uc;
            inputs[idx].ki.dwFlags = KEYEVENTF_UNICODE;
            ++idx;
            inputs[idx].ki.wScan = (ushort)uc;
            inputs[idx].ki.dwFlags = KEYEVENTF_KEYUP;
            ++idx;
            return idx;
        }

        private int setFuncKeyInputs(string str, ref int pos, int strLen, INPUT[] inputs, int idx)
        {
            //logger.InfoH(() => $"CALLED: str={str}, idx={idx}");
            bool bLCtrl = false;
            bool bRCtrl = false;
            bool bLShift = false;
            bool bRShift = false;
            bool bRight = false;
            var sb = new StringBuilder();
            while (pos < strLen) {
                var ch = str[pos];
                if (ch == '}') break;
                if (ch == '<') {
                    bRight = false;
                } else if (ch == '>') {
                    bRight = true;
                } else if (ch == '^') {
                    // Ctrl
                    if (bRight) {
                        bRCtrl = true;
                    } else {
                        bLCtrl = true;
                    }
                    bRight = false;
                } else if (ch == '+') {
                    // Shift
                    if (bRight) {
                        bRShift = true;
                    } else {
                        bLShift = true;
                    }
                    bRight = false;
                } else {
                    sb.Append(ch);
                }
                ++pos;
            }
            if (sb.Length > 0) {
                string name = sb.ToString();
                uint vkey = VirtualKeys.GetFuncVkeyByName(name);
                //logger.InfoH(() => $"vkey={vkey:x} by FuncKey");
                if (vkey == 0) vkey = VirtualKeys.GetAlphabetVkeyByName(name);
                //logger.InfoH(() => $"vkey={vkey:x} by Alphabet");
                if (vkey > 0) {
                    if (bLCtrl) {
                        // 左Ctrl下げ
                        setLeftCtrlInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                    }
                    if (bRCtrl) {
                        // 右Ctrl下げ
                        setRightCtrlInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                    }
                    if (bLShift) {
                        // 左Shift下げ
                        setLeftShiftInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                    }
                    if (bRShift) {
                        // 右Shift下げ
                        setRightShiftInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                    }
                    // キー送出
                    idx = setVkeyInputs((ushort)vkey, inputs, idx);
                    if (bLShift) {
                        // 左Shift戻し
                        setLeftShiftInput(ref inputs[idx++], KEYEVENTF_KEYUP);
                    }
                    if (bRShift) {
                        // 右Shift戻し
                        setRightShiftInput(ref inputs[idx++], KEYEVENTF_KEYUP);
                    }
                    if (bLCtrl) {
                        // 左Ctrl戻し
                        setLeftCtrlInput(ref inputs[idx++], KEYEVENTF_KEYUP);
                    }
                    if (bRCtrl) {
                        // 右Ctrl戻し
                        setRightCtrlInput(ref inputs[idx++], KEYEVENTF_KEYUP);
                    }
                }
            }
            //logger.InfoH(() => $"LEAVE: idx={idx}");
            return idx;
        }

        private static System.Text.RegularExpressions.Regex reThreeTerms = new System.Text.RegularExpressions.Regex(@"\(([^)]+)\)\?\(([^)]+)\):\(([^)]+)\)");

        /// <summary>(Q)?(A):(B) 形式だったら、Q に該当するウィンドウクラスか否かを判定し、当ならAを、否ならBを返す</summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string extractSubString(string str)
        {
            //logger.InfoH(() => $"CALLED: str={str}, actWinName={ActiveWinClassName._toLower()}");
            var resultStr = str;
            if (str._getFirst() == '(' && str.Last() == ')') {
                var items = str._reScan(reThreeTerms);
                //logger.InfoH(() => $"items={items._join(" | ")}, actWinName={ActiveWinClassName._toLower()}");
                if (items._safeCount() == 4) {
                    string activeWinClassName = ActiveWindowHandler.Singleton?.ActiveWinClassName._toLower();
                    var names = items[1]._toLower()._split('|');
                    bool checkFunc()
                    {
                        if (activeWinClassName._notEmpty() && names._notEmpty()) {
                            foreach (var name in names) {
                                if (name._notEmpty()) {
                                    //logger.InfoH(() => $"name={name}");
                                    if (activeWinClassName.StartsWith(name)) return true;
                                    if (name.Last() == '$' && name.Length == activeWinClassName.Length + 1 && name.StartsWith(activeWinClassName)) return true;
                                }
                            }
                        }
                        return false;
                    }
                    resultStr = checkFunc() ? items[2] : items[3];
                }
            }
            //logger.InfoH(() => $"RESULT: {resultStr}");
            return resultStr;
        }

        private int setStringInputs(string str, INPUT[] inputs, int idx)
        {
            int strLen = str._safeCount();
            if (strLen > inputs._safeLength()) strLen = inputs._safeLength();
            for (int i = 0; i < strLen; ++i) {
                if (str[i] == '!' && (i + 1) < strLen && str[i + 1] == '{') {     // "!{"
                    i += 2;
                    idx = setFuncKeyInputs(str, ref i, strLen, inputs, idx);
                } else {
                    initializeKeyboardInput(ref inputs[idx]);
                    //inputs[idx].ki.wVk = VK_PACKET;       // SendInput でUniCodeを出力するときは、ここを 0 にしておく
                    string roman = null;
                    if (IMEHandler.ImeEnabled) {
                        roman = str[i]._hiraganaToRoman();
                        if (roman._isEmpty() && str[i] >= ' ' && str[i] < (char)0x7f) roman = str[i].ToString();
                    }
                    if (roman._notEmpty()) {
                        foreach (var r in roman) {
                            uint vk = VirtualKeys.GetVKeyFromFaceString(r.ToString()) & 0xff;
                            if (vk > 0) {
                                idx = setVkeyInputs((ushort)vk, inputs, idx);
                            }
                        }
                    } else {
                        idx = setUnicodeInputs(str[i], inputs, idx);
                    }
                }
            }
            return idx;
        }

        private void sendInputsWithHandlingDeckey(uint len, INPUT[] inputs, uint vkey)
        {
            if (Settings.LoggingDecKeyInfo) logger.Info($"CALLED: len={len}, vkey={vkey}");

            //if (len > 0) SendInput(len, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (len > 0) sendInput(len, inputs);

            // Enterキーだったら、すぐに仮想鍵盤を移動するように MinValue とする
            LastOutputDt = vkey == (uint)Keys.Enter ? DateTime.MinValue : DateTime.Now;
        }

        /// <summary>
        /// キーボード入力をエミュレートして、CtrlキーとShiftキーをUp状態にする
        /// </summary>
        public void UpCtrlAndShftKeys()
        {
            if (Settings.LoggingDecKeyInfo) logger.Info($"CALLED");

            var inputs = new INPUT[4];
            int idx = 0;
            bool left = false, right = false;

            // Ctrl上げ
            idx = upCtrlKeyInputs(inputs, idx, out left, out right);
            // Shift上げ
            idx = upShiftKeyInputs(inputs, idx, out left, out right);
            // 送出
            if (idx > 0) SendInput((uint)idx, inputs, Marshal.SizeOf(typeof(INPUT)));
            //if (idx > 0) sendInput((uint)idx, inputs);
        }

        private void sendStringInputs(string str)
        {
            int strLen = str._safeCount();
            int inputsLen = strLen * 3;     // IMEがONの時、ひらがなはローマ字等に変換するので、3倍にしておく

            var inputs = new INPUT[inputsLen * 2];

            int idx = 0;

            if (strLen > inputs._safeLength()) strLen = inputs._safeLength();
            for (int i = 0; i < strLen; ++i) {
                if (str[i] == '!' && (i + 1) < strLen && str[i + 1] == '{') {     // "!{"
                    i += 2;
                    idx = setFuncKeyInputs(str, ref i, strLen, inputs, idx);
                } else {
                    initializeKeyboardInput(ref inputs[idx]);
                    //inputs[idx].ki.wVk = VK_PACKET;       // SendInput でUniCodeを出力するときは、ここを 0 にしておく
                    string faceStr = null;
                    bool bKana = false;
                    if (IMEHandler.ImeEnabled && Settings.ImeSendInputInRoman) {
                        faceStr = str[i]._hiraganaToRoman();
                        if (faceStr._isEmpty()) {
                            faceStr = str[i]._hiraganaToKeyface();
                            bKana = faceStr._notEmpty();
                        }
                        if (faceStr._isEmpty() && str[i] >= ' ' && str[i] < (char)0x7f) faceStr = str[i].ToString();
                    }
                    if (faceStr._notEmpty()) {
                        foreach (var fc in faceStr) {
                            uint vk = VirtualKeys.GetVKeyFromFaceChar(fc);
                            if (vk > 0) {
                                using (var changer = new IMEInputModeChanger(bKana)) {
                                    bool leftShift = false, rightShift = false;
                                    if (vk >= 0x100) {
                                        // Shift下げ
                                        idx = upDownShiftKeyInputs(inputs, idx, false, out leftShift, out rightShift);
                                    }

                                    // Vkey
                                    idx = setVkeyInputs((ushort)(vk & 0xff), inputs, idx);

                                    // Shift戻し
                                    if (vk >= 0x100) {
                                        idx = revertShiftKeyInputs(inputs, idx, false, leftShift, rightShift);
                                    }
                                }
                            }
                        }
                    } else {
                        idx = setUnicodeInputs(str[i], inputs, idx);
                    }
                }
            }
            // 送出
            sendInputsWithHandlingDeckey((uint)idx, inputs, VK_BACK);
        }

        /// <summary>
        /// キーボード入力をエミュレートして文字列を送出する
        /// </summary>
        /// <param name="str"></param>
        /// <param name="numBS"></param>
        public void SendString(char[] str, int strLen, int numBS)
        {
            bool loggingFlag = Settings.LoggingDecKeyInfo;
            if (loggingFlag) logger.Info($"CALLED: str={(str._isEmpty() ? "" : new string(str, 0, strLen._lowLimit(0)))}, numBS={numBS}");

            if (numBS < 0) numBS = 0;
            if (strLen < 0) strLen = 0;
            int numCtrlKeys = 2;            // leftCtrl と rightCtrl
            int inputsLen = numBS + numCtrlKeys * 2;   // IMEがONの時、ひらがなはローマ字に変換するので、3倍にしておく

            var inputs = new INPUT[inputsLen * 2];

            int idx = 0;

            bool leftCtrl = false, rightCtrl = false;

            // Ctrl上げ
            idx = upCtrlKeyInputs(inputs, idx, out leftCtrl, out rightCtrl);
            if (loggingFlag) logger.Info($"upCtrl: idx={idx}");
            //sendInputUpCtrlKey(out leftCtrl, out rightCtrl);      // StikyNote など、Waitを入れても状況が変わらない

            // Backspace
            for (int i = 0; i < numBS; ++i) {
                idx = setVkeyInputs(VK_BACK, inputs, idx);
            }
            if (loggingFlag) logger.Info($"bs: idx={idx}");

            // 送出
            sendInputsWithHandlingDeckey((uint)idx, inputs, VK_BACK);

            // 文字列
            if (strLen > 0) {
                sendStringInputs(extractSubString(str._toString()));
            }

            // Ctrl戻し
            idx = 0;
            idx = revertCtrlKeyInputs(inputs, idx, true, leftCtrl, rightCtrl);
            if (loggingFlag) logger.Info($"revert: idx={idx}");
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
            if (loggingFlag) logger.Info($"CALLED: modifier={modifier:x}H, vkey={vkey:x}H, numKeys={n}");
            if (syncPostVkey.BusyCheck()) {
                if (loggingFlag) logger.Info($"IGNORED: numKeys={n}");
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
        /// 文字列を送出する (str は \0 終端文字)<br/>
        /// 文字送出前に numBSだけBackspaceを送る<br/>
        /// 必要ならクリップボードにコピーしてから Ctrl-V を送る
        /// </summary>
        public void SendStringViaClipboardIfNeeded(char[] str, int numBS, bool bForceString = false)
        {
            var activeWinHandle = ActiveWindowHandler.Singleton?.ActiveWinHandle ?? IntPtr.Zero;
            if (Settings.LoggingDecKeyInfo) logger.Info(() => $"ActiveWinHandle={(int)activeWinHandle:x}H, str=\"{str._toString()}\", numBS={numBS}, bForceString={bForceString}");

            if (activeWinHandle != IntPtr.Zero && ((str._notEmpty() && str[0] != 0) || numBS > 0)) {
                int len = str._isEmpty() ? 0 : str._strlen();     // 終端までの長さを取得
                if (bForceString || Settings.MinLeghthViaClipboard <= 0 || len < Settings.MinLeghthViaClipboard) {
                    // 自前で送出
                    SendString(str, len, numBS);
                } else {
                    // クリップボードにコピー
                    System.Windows.Forms.Clipboard.SetText(new string(str, 0, len));
                    // BSを送る
                    SendString(null, 0, numBS);
                    // Ctrl-V を送る (SendVirtualKeys の中でも upDownCtrlKey/revertCtrlKey をやっている)
                    if (numBS > 0 && Settings.PreWmCharGuardMillisec > 0) {
                        int waitMs = (int)(Math.Pow(numBS, Settings.ReductionExponet._lowLimit(0.5)) * Settings.PreWmCharGuardMillisec);
                        if (Settings.LoggingDecKeyInfo) logger.Info(() => $"Wait {waitMs} ms: PreWmCharGuardMillisec={Settings.PreWmCharGuardMillisec}, numBS={numBS}, reductionExp={Settings.ReductionExponet}");
                        Helper.WaitMilliSeconds(waitMs);
                    }
                    SendVKeyCombo(VirtualKeys.CtrlV_VKeyCombo.modifier, VirtualKeys.CtrlV_VKeyCombo.vkey, 1);
                }

                LastOutputDt = DateTime.Now;
            }
        }

    }
}

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
        private const int VK_LSHIFT = 0xa0;                 // LSHIFTキー
        private const int VK_RSHIFT = 0xa1;                 // RSHIFTキー
        private const int VK_PACKET = 0xe7;                 // Unicode 


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

        private int setFuncKeyInputs(string str, ref int pos, int strLen, INPUT[] inputs, int idx)
        {
            bool bLCtrl = false;
            bool bLShift = false;
            var sb = new StringBuilder();
            while (pos < strLen) {
                var ch = str[pos];
                if (ch == '}') break;
                if (ch == '^') {
                    // Ctrl
                    bLCtrl = true;
                } else if (ch == '+') {
                    // Shift
                    bLShift = true;
                } else {
                    sb.Append(ch);
                }
                ++pos;
            }
            if (sb.Length > 0) {
                string name = sb.ToString();
                uint vkey = VirtualKeys.GetFuncVkeyByName(name);
                if (vkey == 0) vkey = VirtualKeys.GetAlphabetVkeyByName(name);
                if (vkey > 0) {
                    if (bLCtrl) {
                        // 左Ctrl下げ
                        setLeftCtrlInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                    }
                    if (bLShift) {
                        // 左Shift下げ
                        setLeftShiftInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                    }
                    // キー送出
                    idx = setVkeyInputs((ushort)vkey, inputs, idx);
                    if (bLShift) {
                        // 左Shift戻し
                        setLeftShiftInput(ref inputs[idx++], KEYEVENTF_KEYUP);
                    }
                    if (bLCtrl) {
                        // 左Ctrl戻し
                        setLeftCtrlInput(ref inputs[idx++], KEYEVENTF_KEYUP);
                    }
                }
            }
            return idx;
        }

        private static System.Text.RegularExpressions.Regex reThreeTerms = new System.Text.RegularExpressions.Regex(@"\(([^)]+)\)\?\(([^)]+)\):\(([^)]+)\)");

        //private static string[] threeTermDelims = { ")?(", "):(" };

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
                //List<string> items = new List<string>();
                //int start = 1;
                //int strLen = str.Length - 1;
                //string extractor(string toStr)
                //{
                //    logger.InfoH(() => $"start={start}, subStr={str._safeSubstring(start)}, toStr={toStr}");
                //    StringBuilder sb = new StringBuilder();
                //    int toStrLen = toStr._safeLength();
                //    if (toStrLen > 0) {
                //        for (int i = start; i < strLen - toStrLen; ++i) {
                //            if (str[i] == toStr[0]) {
                //                bool flag = true;
                //                for (int j = 1; j < toStrLen; ++j) {
                //                    if (str[i + j] != toStr[j]) {
                //                        flag = false;
                //                        break;
                //                    }
                //                }
                //                if (flag) return sb.ToString();
                //            }
                //            sb.Append(str[i]);
                //        }
                //    }
                //    return null;
                //}
                //for (int i = 0; i < threeTermDelims.Length; ++i) {
                //    var delim = threeTermDelims[i];
                //    var substr = extractor(delim);
                //    if (substr._notEmpty()) {
                //        items.Add(substr);
                //        start += substr.Length + delim.Length;
                //    }
                //}
                //if (items.Count == 2 && start < strLen) items.Add(str.Substring(start, strLen - start));
                //logger.InfoH(() => $"items={items._join(" | ")}, actWinName={ActiveWinClassName._toLower()}");
                if (items._safeCount() == 4) {
                    string activeWinClassName = ActiveWinClassName._toLower();
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
                for (int j = 0; j < 2; ++j) {
                    if (j == 0 && str[i] == '!' && (i + 1) < strLen && str[i + 1] == '{') {     // "!{"
                        i += 2;
                        idx = setFuncKeyInputs(str, ref i, strLen, inputs, idx);
                    } else {
                        initializeKeyboardInput(ref inputs[idx]);
                        //inputs[idx].ki.wVk = VK_PACKET;       // SendInput でUniCodeを出力するときは、ここを 0 にしておく
                        inputs[idx].ki.wScan = str[i];
                        inputs[idx].ki.dwFlags = j == 0 ? KEYEVENTF_UNICODE : KEYEVENTF_KEYUP;
                        ++idx;
                    }
                }
            }
            return idx;
        }

        private void sendInputsWithHandlingDeckey(uint len, INPUT[] inputs, uint vkey)
        {
            if (Settings.LoggingDecKeyInfo) logger.Info($"CALLED: len={len}, vkey={vkey}");

            if (len > 0) SendInput(len, inputs, Marshal.SizeOf(typeof(INPUT)));

            // Enterキーだったら、すぐに仮想鍵盤を移動するように MinValue とする
            lastOutputDt = vkey == (uint)Keys.Enter ? DateTime.MinValue : DateTime.Now;
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
            int inputsLen = strLen + numBS + numCtrlKeys * 2;

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

            // 文字列
            if (strLen > 0) {
                idx = setStringInputs(extractSubString(str._toString()), inputs, idx);
                if (loggingFlag) logger.Info($"str: idx={idx}");
            }

            // Ctrl戻し
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
            if (Settings.LoggingDecKeyInfo) logger.Info(() => $"ActiveWinHandle={(int)ActiveWinHandle:x}H, str=\"{str._toString()}\", numBS={numBS}, bForceString={bForceString}");

            if (ActiveWinHandle != IntPtr.Zero && ((str._notEmpty() && str[0] != 0) || numBS > 0)) {
                int len = str._isEmpty() ? 0 : str._strlen();     // 終端までの長さを取得
                if (bForceString || Settings.MinLeghthViaClipboard <= 0 || len < Settings.MinLeghthViaClipboard) {
                    // 自前で送出
                    SendString(str, len, numBS);
                } else {
                    // クリップボードにコピー
                    Clipboard.SetText(new string(str, 0, len));
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

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, int ProcessId);

        //[DllImport("user32.dll")]
        //static extern IntPtr GetForegroundWindow();

        //[DllImport("user32.dll")]
        //private static extern IntPtr GetFocus();

        //[DllImport("user32.dll")]
        //private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern bool GetCaretPos(out Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        GUIThreadInfo guiThreadInfo = new GUIThreadInfo();

        /// <summary>
        /// アクティブウィンドウハンドルの取得
        /// </summary>
        private void GetActiveWindowHandle(bool bLog)
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

                // カレットのスクリーン座標を取得
                guiThreadInfo.GetScreenCaretPos(ref ActiveWinCaretPos);
                //getScreenCaretPosByOriginalWay(fgHan, ref ActiveWinCaretPos, bLog);   // やっぱりこのやり方だとうまく取れない場合あり
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

        // 従来のやり方でカレットのスクリーン座標を取得(やはりうまくいかない)
        private void getScreenCaretPosByOriginalWay(IntPtr fgHan, ref Rectangle rect, bool bLog = false)
        {
            uint targetThread = GetWindowThreadProcessId(fgHan, 0);
            uint selfThread = GetCurrentThreadId();

            //AttachTrheadInput is needed so we can get the handle of a focused window in another app
            AttachThreadInput(selfThread, targetThread, true);

            ////Get the handle of a focused window
            //focusHan = GetFocus();
            //if (bLog) logger.Debug(() => $"focusHan={(int)focusHan:x}");

            Point caretPos;
            GetCaretPos(out caretPos);
            ClientToScreen(fgHan, ref caretPos);
            if (bLog) logger.Info(() => $"focus caret pos=({caretPos.X}, {caretPos.Y})");

            //Now detach since we got the focused handle
            AttachThreadInput(selfThread, targetThread, false);

            rect.X = caretPos.X;
            rect.Y = caretPos.Y;
            rect.Width = 2;
            rect.Height = 20;
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
                    logger.Info($"caretPos=(X:{ActiveWinCaretPos.X}, Y:{ActiveWinCaretPos.Y}), " +
                        $"validCaretMargin=({caretMargin.Select(m => m.ToString())._join(",")}), " +
                        $"WinRect=(L:{rect.iLeft}, T:{rect.iTop}, R:{rect.iRight}, B:{rect.iBottom}), " +
                        $"validWinRect=(L:{rect.iLeft + caretMargin._getNth(2)}, " +
                        $"T:{rect.iTop + caretMargin._getNth(0)}, " +
                        $"R:{rect.iRight - caretMargin._getNth(3)}, " +
                        $"B:{rect.iBottom - caretMargin._getNth(1)})");
                }
                var caretOffset = ActiveWinSettings?.CaretOffset;
                if (caretOffset != null) {
                    logger.Info($"caretOffset=({caretOffset.Select(m => m.ToString())._join(",")})");
                }
                var vkbFixedPos = ActiveWinSettings?.VkbFixedPos;
                if (vkbFixedPos != null) {
                    logger.Info($"vkbFixedPos=({vkbFixedPos.Select(m => m.ToString())._join(",")})");
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
                logger.Info($"CALLED: diffWin={bDiffWin}, mandatory={bMoveMandatory}, firstMove={bFirstMove}");
                loggingCaretInfo();
            }

            if (Settings.VirtualKeyboardPosFixedTemporarily) return;    // 一時的に固定されている

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
                                fX = cX + (xOffset >= 0 ? cW : -fW) + xOffset ;
                                if (fX < 0) fX = cX + cW + Math.Abs(xOffset);
                                fY = cY + (yOffset >= 0 ? cH : -fH) + yOffset;
                                if (fY < 0) fY = cY + cH + Math.Abs(yOffset);
                                int fRight = fX + fW;
                                int fBottom = fY + fH;
                                Rectangle rect = ScreenInfo.GetScreenContaining(cX, cY);
                                if (fRight >= rect.X + rect.Width) fX = cX - fW - Math.Abs(xOffset);
                                if (fBottom >= rect.Y + rect.Height) fY = cY - fH - Math.Abs(yOffset);
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

        private DateTime lastBusyDt;

        private int busyCount = 0;

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

        public void GetActiveWindowInfo(MoveWinType moveWin, bool bLog = false)
        {
            GUIThreadInfo.SetLogFlag(bLog);

            if (bLog) logger.Info($"ENTER: moveWin={moveWin}");

            // 異スレッドおよび同一スレッドでの再入を防ぐ
            lock (syncObj) {
                if (syncObj.BusyCheck()) {
                    //logger.Warn("LEAVE: In Progress");
                    // ビジーカウントをインクリメント
                    ++busyCount;
                    if (lastBusyDt._notValid()) {
                        // 初回のビジー
                        lastBusyDt = DateTime.Now;
                    } else if (DateTime.Now >= lastBusyDt.AddSeconds(5)) {
                        // 前回ビジーから5秒経過したら、busyCount をビジー時刻をクリア
                        lastBusyDt = DateTime.Now;
                        if (busyCount >= 5) {
                            // この5秒間にビジーが5回以上あったら、busyFlag をクリアする。
                            // この間、微妙なタイマー割り込みでbusyFlagがONのままになって、ビジーを繰り返している可能性もあるので。
                            logger.Warn("RESET: Busy Flag");
                            syncObj.Reset();
                        }
                        busyCount = 0;
                    }
                    if (Logger.IsInfoEnabled && !ActiveWinClassName._endsWith(DlgVkbClassNameHash)) {
                        logger.InfoH("LEAVE: In Progress");
                    }
                    return;
                }
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
            if (bLog) logger.Info(() => $"LEAVE: ActiveWinClassName={ActiveWinClassName}");
        }

    }

}

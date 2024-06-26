﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Utils;
using KanchokuWS.Domain;

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

        /// <summary>
        /// 機能キー名の別名定義
        /// </summary>
        private Dictionary<string, string> functionalKeyAliases = new Dictionary<string, string>();

        public void AddFunctionalKeyAlias(string alias, string name)
        {
            logger.Info(() => $"CALLED: alias={alias}, keyname={name}");
            functionalKeyAliases._safeAdd(alias, name);
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
        private const int VK_LALT = 0xa4;                   // LALTキー
        private const int VK_RALT = 0xa5;                   // RALTキー
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

        private static void sendInput(InputInfo info)
        {
            if (info.Index > 0) SendInput((uint)info.Index, info.Inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private const uint MAPVK_VK_TO_VSC = 0;

        public DateTime LastOutputDt { get; private set; }

        private void updateLastOutputDt(bool bMoveVkbAtOnce = false)
        {
            // Enterキーだったら、すぐに仮想鍵盤を移動するように MinValue とする
            LastOutputDt = bMoveVkbAtOnce ? DateTime.MinValue : HRDateTime.Now;
        }

        private static void initializeKeyboardInput(ref INPUT input)
        {
            input.type = INPUT_KEYBOARD;
            input.ki.wVk = 0;
            input.ki.wScan = 0;
            input.ki.dwFlags = 0;
            input.ki.time = 0;
            input.ki.dwExtraInfo = MyMagicNumber;
        }

        private static void setLeftCtrlInput(ref INPUT input, int keyEventFlag)
        {
            initializeKeyboardInput(ref input);
            input.ki.wVk = VK_CONTROL;
            input.ki.dwFlags = keyEventFlag;
        }

        private static void setRightCtrlInput(ref INPUT input, int keyEventFlag)
        {
            setLeftCtrlInput(ref input, keyEventFlag);
            input.ki.dwFlags |= KEYEVENTF_EXTENDEDKEY;  // 右Ctrlは、0xa3 ではなく、EXTENTED を設定する必要あり
        }

        public static ModifierKeyState GetCtrlKeyState(bool bUp = false)
        {
            return new ModifierKeyState(
                bUp,
                (GetAsyncKeyState(FuncVKeys.LCONTROL) & 0x8000) != 0,
                (GetAsyncKeyState(FuncVKeys.RCONTROL) & 0x8000) != 0);
        }

        public static ModifierKeyState GetCtrlKeyState(ModifierKeyState state)
        {
            state.LeftKeyDown = (GetAsyncKeyState(FuncVKeys.LCONTROL) & 0x8000) != 0;
            state.RightKeyDown = (GetAsyncKeyState(FuncVKeys.RCONTROL) & 0x8000) != 0;
            return state;
        }

        public static bool IsAltKeyPressed()
        {
            return (GetAsyncKeyState(FuncVKeys.ALT) & 0x8000) != 0;
        }

        class InputInfo
        {
            public INPUT[] Inputs;
            public int Index;
            public ModifierKeyState KeyState;

            public InputInfo(INPUT[] inputs, int idx = 0, ModifierKeyState state = null)
            {
                Inputs = inputs;
                Index = idx;
                KeyState = state != null ? state : new ModifierKeyState();
            }

            public InputInfo(int inputsLen, ModifierKeyState state = null)
                : this(new INPUT[inputsLen * 2], 0, state)
            {
            }
        }

        /// <summary>
        /// Ctrlキーの事前上げ下げ
        /// </summary>
        /// <param name="leftCtrl"></param>
        /// <param name="rightCtrl"></param>
        private static void upDownCtrlKeyInputs(InputInfo info)
        {
            var keyState = info.KeyState;
            keyState.LeftKeyDown = (GetAsyncKeyState(FuncVKeys.LCONTROL) & 0x8000) != 0;
            keyState.RightKeyDown = (GetAsyncKeyState(FuncVKeys.RCONTROL) & 0x8000) != 0;
            logger.DebugH(() => $"bUp={keyState.OperationUp}, LeftKeyDown={keyState.LeftKeyDown}, RightKeyDown={keyState.RightKeyDown}");

            if (keyState.OperationUp) {
                // DOWNしているほうだけ上げる
                if (keyState.LeftKeyDown) setLeftCtrlInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYUP);
                if (keyState.RightKeyDown) setRightCtrlInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYUP);
            } else {
                if (!keyState.LeftKeyDown && !keyState.RightKeyDown) {
                    // leftだけ下げる
                    setLeftCtrlInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYDOWN);
                    //setRightCtrlInput(ref info.Inputs[idx++], KEYEVENTF_KEYDOWN);
                }
            }
        }

        /// <summary>
        /// Ctrlキーの事前上げ下げ
        /// </summary>
        /// <param name="leftCtrl"></param>
        /// <param name="rightCtrl"></param>
        private static ModifierKeyState upDownCtrlKeyInputs(bool bUp)
        {
            var keyState = new ModifierKeyState(
                bUp,
                (GetAsyncKeyState(FuncVKeys.LCONTROL) & 0x8000) != 0,
                (GetAsyncKeyState(FuncVKeys.RCONTROL) & 0x8000) != 0);
            logger.DebugH(() => $"bUp={keyState.OperationUp}, LeftKeyDown={keyState.LeftKeyDown}, RightKeyDown={keyState.RightKeyDown}");

            var info = new InputInfo(4, keyState);
            if (keyState.OperationUp) {
                // DOWNしているほうだけ上げる
                if (keyState.LeftKeyDown) setLeftCtrlInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYUP);
                if (keyState.RightKeyDown) setRightCtrlInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYUP);
            } else {
                if (!keyState.LeftKeyDown && !keyState.RightKeyDown) {
                    // leftだけ下げる
                    setLeftCtrlInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYDOWN);
                    //setRightCtrlInput(ref info.Inputs[idx++], KEYEVENTF_KEYDOWN);
                }
            }
            sendInput(info);
            return keyState;
        }

        /// <summary>
        /// Ctrlキーの事前上げ
        /// </summary>
        /// <param name="leftCtrl"></param>
        /// <param name="rightCtrl"></param>
        private static void upCtrlKeyInputs(InputInfo info)
        {
            info.KeyState.OperationUp = true;
            upDownCtrlKeyInputs(info);
        }

        /// <summary>
        /// Ctrlキーの事前上げ
        /// </summary>
        private static ModifierKeyState upCtrlKeyInputs()
        {
            return upDownCtrlKeyInputs(true);
        }

        /// <summary>
        /// Ctrlキーの事前下げ
        /// </summary>
        private static ModifierKeyState downCtrlKeyInputs()
        {
            return upDownCtrlKeyInputs(false);
        }

        /// <summary>
        /// Ctrlキーの状態を戻す
        /// </summary>
        /// <param name="bUp">事前操作<</param>
        /// <param name="prevLeftCtrl"></param>
        /// <param name="prevRightCtrl"></param>
        private static void revertCtrlKeyInputs(InputInfo info)
        {
            var keyState = info.KeyState;
            logger.DebugH(() => $"bUp={keyState.OperationUp}, prevLeftCtrl={keyState.LeftKeyDown}, prevRightCtrl={keyState.RightKeyDown}");

            if (keyState.OperationUp) {
                // 事前操作がUPだった
                if (keyState.LeftKeyDown) setLeftCtrlInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYDOWN);
                if (keyState.RightKeyDown) setRightCtrlInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYDOWN);
            } else {
                // 事前操作がDOWNだった⇒左Ctrlだけを上げる
                if (!keyState.LeftKeyDown && !keyState.RightKeyDown) setLeftCtrlInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYUP);
            }
        }

        public void RevertCtrlKey(ModifierKeyState state)
        {
            logger.DebugH($"CALLED");

            var inputs = new INPUT[2];
            var info = new InputInfo(2, state);

            // Ctrl戻し
            revertCtrlKeyInputs(info);
            logger.DebugH(() => $"revert: idx={info.Index}");

            // 送出
            sendInput(info);
            updateLastOutputDt();
        }

        private static void setLeftShiftInput(ref INPUT input, int keyEventFlag)
        {
            initializeKeyboardInput(ref input);
            input.ki.wVk = VK_LSHIFT;           // 右シフトは EXTENTED ではなく、0xa1 を設定する必要あり
            input.ki.dwFlags = keyEventFlag;
        }

        private static void setRightShiftInput(ref INPUT input, int keyEventFlag)
        {
            initializeKeyboardInput(ref input);
            input.ki.wVk = VK_RSHIFT;
            input.ki.dwFlags = keyEventFlag;
        }

        private static void setLeftAltInput(ref INPUT input, int keyEventFlag)
        {
            initializeKeyboardInput(ref input);
            input.ki.wVk = VK_LALT;           // 右シフトは EXTENTED ではなく、0xa1 を設定する必要あり
            input.ki.dwFlags = keyEventFlag;
        }

        private static void setRightAltInput(ref INPUT input, int keyEventFlag)
        {
            initializeKeyboardInput(ref input);
            input.ki.wVk = VK_RALT;
            input.ki.dwFlags = keyEventFlag;
        }


        /// <summary>
        /// Shiftキーの事前上げ下げ
        /// </summary>
        /// <param name="leftShift"></param>
        /// <param name="rightShift"></param>
        private static ModifierKeyState upDownShiftKeyInputs(bool bUp)
        {
            var keyState = new ModifierKeyState(
                bUp,
                (GetAsyncKeyState(FuncVKeys.LSHIFT) & 0x8000) != 0,
                (GetAsyncKeyState(FuncVKeys.RSHIFT) & 0x8000) != 0);
            logger.DebugH(() => $"bUp={keyState.OperationUp}, leftShift={keyState.LeftKeyDown}, rightShift={keyState.RightKeyDown}");

            var info = new InputInfo(4, keyState);
            if (keyState.OperationUp) {
                // 下がっているほうだけ上げる
                if (keyState.LeftKeyDown) setLeftShiftInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYUP);
                if (keyState.RightKeyDown) setRightShiftInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYUP);
            } else {
                if (!keyState.LeftKeyDown && !keyState.RightKeyDown) {
                    // 左だけ下げる
                    setLeftShiftInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYDOWN);
                    //setRightShiftInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYDOWN);
                }
            }
            sendInput(info);
            return keyState;
        }

        /// <summary>
        /// Shiftキーの事前上げ
        /// </summary>
        private static ModifierKeyState upShiftKeyInputs()
        {
            return upDownShiftKeyInputs(true);
        }

        /// <summary>
        /// Shiftキーの事前下げ
        /// </summary>
        private static ModifierKeyState downShiftKeyInputs()
        {
            return upDownShiftKeyInputs(false);
        }

        /// <summary>
        /// Shiftキーの状態を戻す
        /// </summary>
        private static InputInfo revertShiftKey(ModifierKeyState keyState, bool bSend = true)
        {
            logger.DebugH(() => $"bUp={keyState?.OperationUp}, prevLeftShift={keyState?.LeftKeyDown}, prevRightShift={keyState?.RightKeyDown}");

            InputInfo info = null;
            if (keyState != null) {
                info = new InputInfo(2, keyState);
                if (keyState.OperationUp) {
                    // 事前操作がUPだった
                    if (keyState.LeftKeyDown) setLeftShiftInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYDOWN);
                    if (keyState.RightKeyDown) setRightShiftInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYDOWN);
                } else {
                    // 事前操作がDOWNだった⇒左Shiftだけを上げる
                    if (!keyState.LeftKeyDown && !keyState.RightKeyDown) setLeftShiftInput(ref info.Inputs[info.Index++], KEYEVENTF_KEYUP);
                }
                sendInput(info);
            }
            return info;
        }

        /// <summary>
        /// Shiftキーの状態を戻す
        /// </summary>
        /// <param name="bUp">事前操作<</param>
        /// <param name="prevLeftShift"></param>
        /// <param name="prevRightShift"></param>
        public void RevertShiftKey(ModifierKeyState state)
        {
            logger.DebugH($"CALLED");

            // Shift戻し
            var info = revertShiftKey(state, false);

            // 送出
            sendInput(info);
            updateLastOutputDt();
        }


        class ShiftKeyUpDownGuard : IDisposable
        {
            ModifierKeyState keyState = null;

            public ShiftKeyUpDownGuard(bool bUp, bool bEffective)
            {
                logger.DebugH(() => $"ShiftKeyUpDownGuard {(bUp ? "UP" : "DOWN")}: bEffective={bEffective}");
                if (bEffective) {
                    keyState = upDownShiftKeyInputs(bUp);
                }
            }

            public void Dispose()
            {
                logger.DebugH($"ShiftKeyUpDownGuard REVERT");
                if (keyState != null) revertShiftKey(keyState);
            }
        }

        class ShiftKeyUpGuard : ShiftKeyUpDownGuard
        {
            public ShiftKeyUpGuard(bool bEffective = true)
                : base(true, bEffective)
            {
            }
        }

        class ShiftKeyDownGuard : ShiftKeyUpDownGuard
        {
            public ShiftKeyDownGuard(bool bEffective = true)
                : base(false, bEffective)
            {
            }
        }

        private static int setVkeyInputs(ushort vkey, INPUT[] inputs, int idx)
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

        private static void setVkeyInputs(ushort vkey, InputInfo info)
        {
            info.Index = setVkeyInputs(vkey, info.Inputs, info.Index);
        }

        private static int setUnicodeInputs(char uc, INPUT[] inputs, int idx)
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

        private static void sendInputsUnicode(char uc)
        {
            logger.DebugH(() => $"CALLED: uc={uc}");
            var info = new InputInfo(2);
            info.Index = setUnicodeInputs(uc, info.Inputs, info.Index);
            sendInput(info);
        }

        private int sendFuncKeyInputs(string str, int pos, int strLen)
        {
            logger.DebugH(() => $"CALLED: str={str}, pos={pos}, strLen={strLen}");

            bool bLCtrl = false;
            bool bRCtrl = false;
            bool bLShift = false;
            bool bRShift = false;
            bool bLAlt = false;
            bool bRAlt = false;
            bool bRight = false;
            bool bRepeatCnt = false;
            int repeatCount = 0;
            var sb = new StringBuilder();
            while (pos < strLen) {
                var ch = str[pos];
                if (ch == '}') break;
                if (bRepeatCnt) {
                    if (ch >= '0' && ch <= '9') {
                        repeatCount = repeatCount * 10 + (ch - '0');
                    }
                } else {
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
                    } else if (ch == '!') {
                        // Alt
                        if (bRight) {
                            bRAlt = true;
                        } else {
                            bLAlt = true;
                        }
                        bRight = false;
                    } else if (ch == ' ' || ch == ',' || ch == ':') {
                        bRepeatCnt = true;
                    } else {
                        sb.Append(ch);
                    }
                }
                ++pos;
            }
            if (repeatCount == 0) repeatCount = 1;

            var info = new InputInfo(repeatCount + 5);
            INPUT[] inputs = info.Inputs;
            int idx = info.Index;

            if (sb.Length > 0) {
                string name = sb.ToString();
                name = functionalKeyAliases._safeGet(name, name);
                logger.Info(() => $"alias={sb}, key={name}");
                uint vkey = DecoderKeyVsVKey.GetFuncVkeyByName(name);
                //logger.DebugH(() => $"vkey={vkey:x} by FuncKey");
                if (vkey == 0) vkey = AlphabetVKeys.GetAlphabetVkeyByName(name);
                //logger.DebugH(() => $"vkey={vkey:x} by Alphabet");
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
                    if (bLAlt) {
                        // 左Alt下げ
                        setLeftAltInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                    }
                    if (bRAlt) {
                        // 右Alt下げ
                        setRightAltInput(ref inputs[idx++], KEYEVENTF_KEYDOWN);
                    }
                    // キー送出
                    for (int i = 0; i < repeatCount; ++i) {
                        idx = setVkeyInputs((ushort)vkey, inputs, idx);
                    }
                    if (bLAlt) {
                        // 左Alt戻し
                        setLeftAltInput(ref inputs[idx++], KEYEVENTF_KEYUP);
                    }
                    if (bRAlt) {
                        // 右Alt戻し
                        setRightAltInput(ref inputs[idx++], KEYEVENTF_KEYUP);
                    }
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
            //logger.DebugH(() => $"LEAVE: idx={idx}");
            info.Index = idx;
            sendInput(info);

            return pos;
        }

        private static System.Text.RegularExpressions.Regex reThreeTerms = new System.Text.RegularExpressions.Regex(@"\(([^)]+)\)\?\(([^)]+)\):\(([^)]+)\)");

        /// <summary>(Q)?(A):(B) 形式だったら、Q に該当するウィンドウクラスか否かを判定し、当ならAを、否ならBを返す</summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string extractSubString(string str)
        {
            //logger.DebugH(() => $"CALLED: str={str}, actWinName={ActiveWinClassName._toLower()}");
            var resultStr = str;
            if (str._getFirst() == '(' && str.Last() == ')') {
                var items = str._reScan(reThreeTerms);
                //logger.DebugH(() => $"items={items._join(" | ")}, actWinName={ActiveWinClassName._toLower()}");
                if (items._safeCount() == 4) {
                    string activeWinClassName = ActiveWindowHandler.Singleton?.ActiveWinClassName._toLower();
                    var names = items[1]._toLower()._split('|');
                    bool checkFunc()
                    {
                        if (activeWinClassName._notEmpty() && names._notEmpty()) {
                            foreach (var name in names) {
                                if (name._notEmpty()) {
                                    //logger.DebugH(() => $"name={name}");
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
            //logger.DebugH(() => $"RESULT: {resultStr}");
            return resultStr;
        }

        private void sendInputsVkey(uint vkey, int num, bool bMoveVkbAtOnce = false)
        {
            logger.DebugH(() => $"CALLED: vkey={vkey}, len={num}, MoveVkbAtOnce={bMoveVkbAtOnce}");

            if (num > 0) {
                var info = new InputInfo(num);
                for (int i = 0; i < num; ++i) {
                    setVkeyInputs((ushort)vkey, info);
                }
                // 送出
                sendInput(info);
            }

            // Enterキーだったら、すぐに仮想鍵盤を移動するように MinValue とする
            updateLastOutputDt(bMoveVkbAtOnce);
        }

        private bool isShiftLeftArrowDeleteComboUsed(int numBS)
        {
            return Settings.IsShiftLeftArrowDeleteComboUsed(numBS, ActiveWindowHandler.Singleton.ActiveWinClassName);
        }

        private void waitAfterBS()
        {
            int millisec = Settings.GetWinClassSettings(ActiveWindowHandler.Singleton.ActiveWinClassName)?.AfterBSWaitMillisec ?? 0;
            if (millisec > 0) Helper.WaitMilliSeconds(millisec);
        }

        private void sendBackSpaces(int num, bool bDelete)
        {
            logger.DebugH(() => $"CALLED: len={num}, bDelete={bDelete}");

            if (num > 0) {
                if (isShiftLeftArrowDeleteComboUsed(num)) {
                    logger.DebugH(() => $"SHIFT + LEFTARROW*{num} + DELETE");
                    // Shift下げ
                    var shiftState = downShiftKeyInputs();
                    // Vkey送出
                    sendInputsVkey((uint)Keys.Left, num);
                    if (bDelete) sendInputsVkey((uint)Keys.Delete, 1);
                    // Shift戻し
                    RevertShiftKey(shiftState);
                } else {
                    sendInputsVkey((uint)Keys.Back, num);
                }
            }
        }

        private static void sendInputsVkey(ushort vkey)
        {
            logger.DebugH(() => $"CALLED: vkey={vkey}");
            var info = new InputInfo(2);
            setVkeyInputs(vkey, info);
            sendInput(info);
        }

        /// <summary>
        /// キーボード入力をエミュレートして、CtrlキーとShiftキーをUp状態にする
        /// </summary>
        public void UpCtrlAndShftKeys()
        {
            logger.DebugH($"CALLED");

            // Ctrl上げ
            upCtrlKeyInputs();
            // Shift上げ
            upShiftKeyInputs();
        }

        private static void sendInputsRomanOrKanaUnicodeEx(char fc, bool bOnlyASCII)
        {
            uint vk = !bOnlyASCII || fc < 0x80 ? CharVsVKey.GetVKeyFromFaceChar(fc) : 0;
            if (vk > 0) {
                using (var guard = new ShiftKeyDownGuard(vk >= 0x100)) {
                    // Vkey
                    sendInputsVkey((ushort)(vk & 0xff));
                }
            } else {
                sendInputsUnicode(fc);
            }
        }

        private static void sendInputsRomanOrKanaUnicode(string faceStr)
        {
            logger.DebugH(() => $"CALLED: faceStr={faceStr}");

            foreach (var fc in faceStr) {
                sendInputsRomanOrKanaUnicodeEx(fc, false);
            }
        }

        //private bool isUnicodeSendWindow()
        //{
        //    string activeWinClassName = ActiveWindowHandler.Singleton.ActiveWinClassName._toLower();
        //    bool contained = activeWinClassName._notEmpty()
        //        && Settings.ImeUnicodeClassNamesHash._notEmpty()
        //        && Settings.ImeUnicodeClassNamesHash.Any(name => name._notEmpty() && (activeWinClassName == name || name.Last() == '*' && activeWinClassName.StartsWith(name._safeSubstring(0, -1))));
        //    logger.DebugH(() => $"contained={contained}, activeWinClassName={activeWinClassName}");
        //    return contained;
        //}

        private char hiraganaConv(char ch)
        {
            return Settings.ImeKatakanaToHiragana ? Helper.ConvertKatakanaToHiragana(ch) : ch;
        }

        // かな入力練習モードの時に使うタイマー
        // 書き換え文字があるときに、それをいったんストックしておき、制限時間を過ぎてもBSによる削除がなければ出力する)
        System.Timers.Timer timerForWaitingChar = null;
        // 書き換え対象文字のストック
        private char waitingCharWhenKanaTraining = '\0';

        public void InitializePreRewriteTimer(FrmKanchoku frm)
        {
            logger.Info(() => $"CALLED: waitTime={Settings.PreRewriteWaitTimeMsWhenTrainingMode}, preRewriteTargetChars={Settings.PreRewriteTargetChars}");
            if (timerForWaitingChar == null) {
                timerForWaitingChar = new System.Timers.Timer();
                timerForWaitingChar.SynchronizingObject = frm;
                timerForWaitingChar.Elapsed += timerForWaitingChar_elapsed;
                logger.Info(() => $"TIMER Initialized");
            }
        }

        private void timerForWaitingChar_elapsed(Object sender, System.Timers.ElapsedEventArgs e)
        {
            logger.DebugH(() => $"ENTER: TIMER ELAPSED");
            timerForWaitingChar?.Stop();
            sendWaitingCharWhenKanaTraining();
            logger.DebugH(() => $"LEAVE");
        }

        private void sendWaitingCharWhenKanaTraining()
        {
            if (waitingCharWhenKanaTraining != '\0') {
                string faceStr = waitingCharWhenKanaTraining._hiraganaToKeyface();
                if (faceStr._notEmpty()) {
                    logger.DebugH(() => $"sendInputsRomanOrKanaUnicode({waitingCharWhenKanaTraining}:{faceStr})");
                    sendInputsRomanOrKanaUnicode(faceStr);
                }
                waitingCharWhenKanaTraining = '\0';
            }
        }

        private bool pushWaitingCharWhenKanaTraining(char ch)
        {
            logger.DebugH(() => $"CALLED: ch={ch}, preRewriteWaitTimeMs={Settings.PreRewriteWaitTimeMsWhenTrainingMode})");
            if (Settings.PreRewriteWaitTimeMsWhenTrainingMode <= 0) return false;   // 待ち時間が 0 以下ならストックしない

            timerForWaitingChar?.Stop();
            sendWaitingCharWhenKanaTraining();
            waitingCharWhenKanaTraining = ch;
            if (timerForWaitingChar != null) {
                timerForWaitingChar.Interval = Settings.PreRewriteWaitTimeMsWhenTrainingMode;
                timerForWaitingChar.Start();
            }
            return true;
        }

        private int clearOrSendWaitingCharWhenKanaTraining(int numBS)
        {
            logger.DebugH(() => $"CALLED: numBS={numBS}, waitingChar={waitingCharWhenKanaTraining})");
            if (numBS > 0 && waitingCharWhenKanaTraining != '\0') {
                waitingCharWhenKanaTraining = '\0';
                --numBS;
            }
            sendWaitingCharWhenKanaTraining();
            return numBS;
        }

        private void sendStringInputs(string str)
        {
            logger.DebugH(() => $"ENTER: str={str}");

            int strLen = str._safeCount();
            char prevUniChar = '\0';
            for (int i = 0; i < strLen; ++i) {
                if (str[i] == '!' && (i + 1) < strLen && str[i + 1] == '{') {     // "!{"
                    i += 2;
                    i = sendFuncKeyInputs(str, i, strLen);
                } else {
                    string faceStr = null;
                    logger.DebugH(() => $"ImeEnabled={IMEHandler.ImeEnabled}, ImeSendInputInRoman={Settings.ImeSendInputInRoman}, ImeSendInputInKana={Settings.ImeSendInputInKana}");
                    if (IMEHandler.ImeEnabled /* && (str[i] == ' ' || !isUnicodeSendWindow())*/) {
                        if (Settings.ImeSendInputInRoman) {
                            faceStr = hiraganaConv(str[i])._hiraganaToRoman();
                            if (faceStr._isEmpty()) logger.DebugH($"_hiraganaToRoman empty");
                        } else if (Settings.ImeSendInputInKana) {
                            faceStr = hiraganaConv(str[i])._hiraganaToKeyface();
                            if (faceStr._isEmpty()) logger.DebugH($"_hiraganaToKeyface empty");
                        }
                        if (faceStr._notEmpty()) {
                            sendInputsRomanOrKanaUnicode(faceStr);
                            continue;
                        }

                        logger.DebugH($"send asis string");
                    } else if (Settings.KanaTrainingMode) {
                        faceStr = str[i]._hiraganaToKeyface();
                        if (faceStr._notEmpty()) {
                            bool stocked = false;
                            if (i == strLen - 1 && (Settings.PreRewriteTargetChars.Contains('*') || Settings.PreRewriteTargetChars.Contains(str[i]))) {
                                // かな入力練習モードのときは末尾文字が指定の文字なら、バッファに積む
                                logger.DebugH(() => $"i={i}, strLen={strLen}");
                                stocked = pushWaitingCharWhenKanaTraining(str[i]);
                            }
                            if (!stocked) {
                                sendInputsRomanOrKanaUnicode(faceStr);
                            }
                            continue;
                        } else {
                            logger.DebugH($"send SPACE for other than KanaMoji in KanaTrainingMode");
                            sendInputsRomanOrKanaUnicode(" ");
                            continue;
                        }
                    }
                    logger.DebugH($"send Unicode string");
                    char uniChar = str[i];
                    // 同じ文字を続けてSendInputすると2文字めが落ちる現象に対応
                    if (uniChar >= 0x80 && uniChar == prevUniChar) Helper.WaitMilliSeconds(10);
                    //sendInputsUnicode(str[i]);
                    sendInputsRomanOrKanaUnicodeEx(str[i], true);
                    prevUniChar = uniChar;
                }
            }
            updateLastOutputDt();
            logger.DebugH(() => $"LEAVE: str={str}");
        }

        /// <summary>
        /// キーボード入力をエミュレートして文字列を送出する
        /// </summary>
        /// <param name="str"></param>
        /// <param name="numBS"></param>
        public void SendString(char[] str, int strLen, int numBS)
        {
            logger.DebugH(() => $"CALLED: str={(str._isEmpty() ? "" : new string(str, 0, strLen._lowLimit(0)))}, numBS={numBS}");

            // Ctrl上げ
            var ctrlState = upCtrlKeyInputs();
            logger.DebugH($"upCtrl");

            // Backspace
            //sendInputsVkey(VK_BACK, numBS);
            numBS = clearOrSendWaitingCharWhenKanaTraining(numBS);
            sendBackSpaces(numBS, strLen <= 0);

            // 文字列
            if (strLen > 0) {
                if (numBS > 0) waitAfterBS();
                sendStringInputs(extractSubString(str._toString()));
            }

            // Ctrl戻し
            RevertCtrlKey(ctrlState);
            logger.DebugH($"revertCtrl");
        }

        BoolObject syncPostVkey = new BoolObject();

        /// <summary>
        /// 仮想キーComboを送出する<br/>
        /// </summary>
        /// <param name="n">キーダウンの数</param>
        public void SendVKeyCombo(uint modifier, uint vkey, int n)
        {
            if (Settings.LoggingDecKeyInfo)logger.Info(() => $"ENTER: modifier={modifier:x}H, vkey={vkey:x}H, numKeys={n}");
            if (syncPostVkey.BusyCheck()) {
                if (Settings.LoggingDecKeyInfo)logger.Info(() => $"LEAVE: IGNORED: numKeys={n}");
                return;
            }
            using (syncPostVkey) {
                lock (syncPostVkey) {
                    // Ctrl上げ(または下げ)
                    var ctrlState = upDownCtrlKeyInputs((modifier & KeyModifiers.MOD_CONTROL) == 0);

                    // Shift上げ(または下げ)
                    var shiftState = upDownShiftKeyInputs((modifier & KeyModifiers.MOD_SHIFT) == 0);

                    // Vkey送出
                    sendInputsVkey(vkey, n);

                    // Shift戻し
                    RevertShiftKey(shiftState);

                    // Ctrl戻し
                    RevertCtrlKey(ctrlState);
                }
            }
            if (Settings.LoggingDecKeyInfo)logger.Info("LEAVE");
        }

        /// <summary>
        /// 文字列を送出する (str は \0 終端文字)<br/>
        /// 文字送出前に numBSだけBackspaceを送る<br/>
        /// 必要ならクリップボードにコピーしてから Ctrl-V を送る
        /// </summary>
        public void SendStringViaClipboardIfNeeded(char[] str, int numBS, bool bForceString = false)
        {
            var activeWinHandle = ActiveWindowHandler.Singleton?.ActiveWinHandle ?? IntPtr.Zero;
            logger.DebugH(() => $"ActiveWinHandle={(int)activeWinHandle:x}H, str=\"{str._toString()}\", numBS={numBS}, bForceString={bForceString}");

            if (activeWinHandle != IntPtr.Zero && ((str._notEmpty() && str[0] != 0) || numBS > 0)) {
                int len = str._isEmpty() ? 0 : str._strlen();     // 終端までの長さを取得
                if (bForceString || Settings.MinLeghthViaClipboard <= 0 || len < Settings.MinLeghthViaClipboard || isShiftLeftArrowDeleteComboUsed(numBS)) {
                    // 自前で送出
                    SendString(str, len, numBS);
                } else {
                    // クリップボードにコピー
                    System.Windows.Forms.Clipboard.SetText(new string(str, 0, len));
                    // BSを送る
                    SendString(null, 0, numBS);
                    // Ctrl-V を送る (SendVirtualKeys の中でも upDownCtrlKey/revertCtrlKey をやっている)
                    if (numBS > 0 && Settings.PreCtrlVGuardMillisec > 0) {
                        int waitMs = (int)(Math.Pow(numBS, Settings.ReductionExponet._lowLimit(0.5)) * Settings.PreCtrlVGuardMillisec);
                        logger.DebugH(() => $"Wait {waitMs} ms: PreWmCharGuardMillisec={Settings.PreCtrlVGuardMillisec}, numBS={numBS}, reductionExp={Settings.ReductionExponet}");
                        Helper.WaitMilliSeconds(waitMs);
                    }
                    SendVKeyCombo(KeyModifiers.MOD_CONTROL, (uint)Keys.V, 1);
                }

                LastOutputDt = HRDateTime.Now;
            }
        }

    }
}

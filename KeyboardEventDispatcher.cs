using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Utils;

namespace KanchokuWS
{
    public class KeyboardEventDispatcher : IDisposable
    {
        private static Logger logger = Logger.GetLogger();

        /// <summary>キーダウン</summary>
        public delegate bool DelegateOnKeyEvent(int vkey, int extraInfo);

        /// <summary>デコーダ ON/OFF </summary>
        public delegate void DelegateToggleDecoder();

        /// <summary>デコーダ ON </summary>
        public delegate void DelegateActivateDecoder();

        /// <summary>デコーダ OFF </summary>
        public delegate void DelegateDeactivateDecoder();

        /// <summary>打鍵ヘルプのローテーション<br/>ローテーションを行わない場合は false を返す</summary>
        public delegate bool DelegateRotateStrokeHelp();

        /// <summary>打鍵ヘルプの逆ローテーション<br/>ローテーションを行わない場合は false を返す</summary>
        public delegate bool DelegateRotateReverseStrokeHelp();

        /// <summary>日付出力のローテーション<br/>日付出力を行わない場合は false を返す</summary>
        public delegate bool DelegateRotateDateString();

        /// <summary>日付出力の逆ローテーション<br/>日付出力を行わない場合は false を返す</summary>
        public delegate bool DelegateRotateReverseDateString();

        /// <summary>デコーダキーに変換してデコーダを呼び出す<br/>デコーダ呼び出しを行わない場合は false を返す</summary>
        public delegate bool DelegateInvokeDecoder(int decKey);

        /// <summary>キーダウン</summary>
        public DelegateOnKeyEvent OnKeyDown { get; set; }

        /// <summary>キーダウン</summary>
        public DelegateOnKeyEvent OnKeyUp { get; set; }

        /// <summary>デコーダ ON/OFF </summary>
        public DelegateToggleDecoder ToggleDecoder { get; set; }

        /// <summary>デコーダ ON </summary>
        public DelegateActivateDecoder ActivateDecoder { get; set; }

        /// <summary>デコーダ OFF </summary>
        public DelegateDeactivateDecoder DeactivateDecoder { get; set; }

        /// <summary>打鍵ヘルプのローテーション<br/>ローテーションを行わない場合は false を返す</summary>
        public DelegateRotateStrokeHelp RotateStrokeHelp { get; set; }

        /// <summary>打鍵ヘルプの逆ローテーション<br/>ローテーションを行わない場合は false を返す</summary>
        public DelegateRotateReverseStrokeHelp RotateReverseStrokeHelp { get; set; }

        /// <summary>日付出力のローテーション<br/>日付出力を行わない場合は false を返す</summary>
        public DelegateRotateDateString RotateDateString { get; set; }

        /// <summary>日付出力の逆ローテーション<br/>日付出力を行わない場合は false を返す</summary>
        public DelegateRotateReverseDateString RotateReverseDateString { get; set; }

        /// <summary>デコーダキーに変換してデコーダを呼び出す<br/>デコーダ呼び出しを行わない場合は false を返す</summary>
        public DelegateInvokeDecoder InvokeDecoder { get; set; }

        private bool bHooked = false;

        /// <summary>コンストラクタ</summary>
        public KeyboardEventDispatcher()
        {
            clearKeyCodeTable();
        }

        public void InstallKeyboardHook()
        {
            logger.InfoH($"ENTER");
            KeyboardHook.OnKeyDownEvent = onKeyboardDownHandler;
            KeyboardHook.OnKeyUpEvent = onKeyboardUpHandler;
            KeyboardHook.Hook();
            bHooked = true;
            logger.InfoH($"LEAVE");
        }

        public void ReleaseKeyboardHook()
        {
            logger.InfoH($"ENTER");
            if (bHooked) {
                bHooked = false;
                KeyboardHook.UnHook();
                logger.InfoH($"UNHOOKED");
            }
            logger.InfoH($"LEAVE");
        }

        public void Dispose()
        {
            ReleaseKeyboardHook();
        }

        //----------------------------------------------------------------------------------------------------------
        [DllImport("user32.dll")]
        private static extern ushort GetAsyncKeyState(uint vkey);

        private const int vkeyNum = 256;

        private int[] normalKeyCodes = new int[vkeyNum];

        private int[] shiftKeyCodes = new int[vkeyNum];

        private int[] ctrlKeyCodes = new int[vkeyNum];

        private int[] ctrlShiftKeyCodes = new int[vkeyNum];

        private void setKeyCode(int[] table, int vkey, int code, int altVkey = -1)
        {
            if (vkey < 0 || vkey >= table.Length) vkey = altVkey;
            if (vkey >= 0 && vkey < table.Length) {
                table[vkey] = code;
            }
        }

        private void setKeyCode(VKeyCombo combo, int code)
        {
            int[] table = normalKeyCodes;
            if ((combo.modifier & (KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT)) == (KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT)) {
                table = ctrlShiftKeyCodes;
            } else if ((combo.modifier & KeyModifiers.MOD_CONTROL) != 0) {
                table = ctrlKeyCodes;
            } else if ((combo.modifier & KeyModifiers.MOD_SHIFT) != 0) {
                table = shiftKeyCodes;
            }
            setKeyCode(table, (int)combo.vkey, code);
        }

        private void setVkeyCode(int code)
        {
            VKeyCombo? combo = VirtualKeys.GetVKeyComboFromDecKey(code);
            if (combo != null) setKeyCode(combo.Value, code);
        }

        private bool ctrlKeyPressed()
        {
            return (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0 || (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;
        }

        private bool shiftKeyPressed()
        {
            return (GetAsyncKeyState(VirtualKeys.LSHIFT) & 0x8000) != 0 || (GetAsyncKeyState(VirtualKeys.RSHIFT) & 0x8000) != 0;
        }

        /// <summary>キーボード押下時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <param name="extraInfo"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private bool onKeyboardDownHandler(int vkey, int extraInfo)
        {
            logger.DebugH(() => $"CALLED: vkey={vkey:x}H({vkey}), extraInfo={extraInfo}");
            if ((vkey >= 0 && vkey < 0xa0 || vkey >= 0xa6 && vkey < vkeyNum) && extraInfo != ActiveWindowHandler.MyMagicNumber) {   // 0xa0 = LSHIFT, 0xa5 = RMENU
                bool ctrl = ctrlKeyPressed();
                bool shift = shiftKeyPressed();
                int keycode = -1;
                if (ctrl && shift) {
                    keycode = ctrlShiftKeyCodes[vkey];
                } else if (ctrl) {
                    keycode = ctrlKeyCodes[vkey];
                } else if (shift) {
                    keycode = shiftKeyCodes[vkey];
                } else {
                    keycode = normalKeyCodes[vkey];
                }

                logger.DebugH(() => $"keycode={keycode:x}H({keycode}), ctrl={ctrl}, shift={shift}");
                // どうやら KeyboardHook で CallNextHookEx を呼ばないと次のキー入力の処理に移らないみたいだが、
                // 将来必要になるかもしれないので、下記処理を残しておく
                if (busyFlag) {
                    if (vkeyQueue.Count < vkeyQueueMaxSize) {
                        vkeyQueue.Enqueue(vkey);
                        logger.DebugH(() => $"vkeyQueue.Count={vkeyQueue.Count}");
                    }
                    return true;
                }
                while (true) {
                    bool result = invokeHandler(keycode);
                    if (vkeyQueue.Count == 0) return result;
                    keycode = vkeyQueue.Dequeue();
                }
            }
            return false;
        }

        private bool busyFlag = false;

        private const int vkeyQueueMaxSize = 4;

        private Queue<int> vkeyQueue = new Queue<int>();

        /// <summary>キーボード離上時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <param name="extraInfo"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private bool onKeyboardUpHandler(int vkey, int extraInfo)
        {
            logger.DebugH(() => $"CALLED: vkey={vkey:x}H({vkey}), extraInfo={extraInfo}");
            if ((vkey >= 0 && vkey < 0xa0 || vkey >= 0xa6 && vkey < vkeyNum) && extraInfo != ActiveWindowHandler.MyMagicNumber) {   // 0xa0 = LSHIFT, 0xa5 = RMENU
                // キーアップ時はなにもしない
                return OnKeyUp?.Invoke(vkey, extraInfo) ?? false;
            }
            return false;
        }

        private bool invokeHandler(int keycode)
        {
            logger.DebugH(() => $"ENTER: keycode={keycode:x}H({keycode})");
            busyFlag = true;
            try {
                switch (keycode) {
                    case DecoderKeys.TOGGLE_DECKEY:
                        ToggleDecoder?.Invoke();
                        return true;
                    case DecoderKeys.ACTIVE_DECKEY:
                    case DecoderKeys.ACTIVE2_DECKEY:
                        ActivateDecoder?.Invoke();
                        return true;
                    case DecoderKeys.DEACTIVE_DECKEY:
                    case DecoderKeys.DEACTIVE2_DECKEY:
                        DeactivateDecoder?.Invoke();
                        return true;
                    case DecoderKeys.STROKE_HELP_ROTATION_DECKEY:
                        return RotateStrokeHelp?.Invoke() ?? false;
                    case DecoderKeys.STROKE_HELP_UNROTATION_DECKEY:
                        return RotateReverseStrokeHelp?.Invoke() ?? false;
                    case DecoderKeys.DATE_STRING_ROTATION_DECKEY:
                        return RotateDateString?.Invoke() ?? false;
                    case DecoderKeys.DATE_STRING_UNROTATION_DECKEY:
                        return RotateReverseDateString?.Invoke() ?? false;
                    default:
                        if (keycode >= 0 && keycode < DecoderKeys.TOTAL_DECKEY_NUM) return InvokeDecoder?.Invoke(keycode) ?? false;
                        return false;
                }
            } finally {
                busyFlag = false;
                logger.DebugH(() => $"LEAVE");
            }
        }

        private void clearKeyCodeTable()
        {
            for (int i = 0; i < normalKeyCodes.Length; ++i) normalKeyCodes[i] = -1;
            for (int i = 0; i < shiftKeyCodes.Length; ++i) shiftKeyCodes[i] = -1;
            for (int i = 0; i < ctrlKeyCodes.Length; ++i) ctrlKeyCodes[i] = -1;
            for (int i = 0; i < ctrlShiftKeyCodes.Length; ++i) ctrlShiftKeyCodes[i] = -1;
        }


        /// <summary> 機能キー (Esc, 半/全, Tab, Caps, 英数, 無変換, 変換, かな, BS, Enter, Ins, Del, Home, End, PgUp, PgDn, ↑, ↓, ←, →)</summary>
        private static uint[] vkeyArrayFuncKeys = {
            /*Esc*/ 0x1b, /*半/全*/ 0xf3, /*Tab*/ 0x09, /*Caps*/ 0x14, /*英数*/ 0xf0, /*無変換*/ 0x1d, /*変換*/ 0x1c, /*かな*/ 0xf2, /*BS*/ 0x08, /*Enter*/ 0x0d,
            /*Ins*/ 0x2d, /*Del*/ 0x2e, /*Home*/ 0x24, /*End*/ 0x23, /*PgUp*/ 0x21, /*PgDn*/ 0x22, /*↑*/ 0x26, /*↓*/ 0x28, /*←*/ 0x25, /*→*/ 0x27
        };

        /// <summary>
        /// キーコードテーブルの構築
        /// </summary>
        /// <param name="reloadKeyboardFile"></param>
        /// <returns>キーボードファイルを読み込めなかったら false を返す</returns>
        public bool SetupKeycodeTable(bool reloadKeyboardFile = false)
        {
            logger.InfoH("ENTER");

            if (reloadKeyboardFile || strokeVKeys == null) {
                if (!readKeyboardFile()) return false;
            }

            clearKeyCodeTable();

            // ストロークキー
            if (strokeVKeys._notEmpty()) {
                for (int i = 0; i < strokeVKeys.Length; ++i) {
                    uint code = strokeVKeys[i];
                    if (code >= 0 && code < vkeyNum) {
                        normalKeyCodes[code] = i + DecoderKeys.NORMAL_DECKEY_START;
                        shiftKeyCodes[code] = i + DecoderKeys.SHIFT_DECKEY_START;
                        ctrlKeyCodes[code] = i + DecoderKeys.CTRL_DECKEY_START;
                        ctrlShiftKeyCodes[code] = i + DecoderKeys.CTRL_SHIFT_DECKEY_START;
                    }
                }
            }

            // 機能キー
            for (int i = 0; i < vkeyArrayFuncKeys.Length; ++i) {
                    uint code = vkeyArrayFuncKeys[i];
                    if (code >= 0 && code < vkeyNum) {
                        normalKeyCodes[code] = i + DecoderKeys.FUNC_DECKEY_START;
                        ctrlKeyCodes[code] = i + DecoderKeys.CTRL_FUNC_DECKEY_START;
                        ctrlShiftKeyCodes[code] = i + DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_START;
                    }
            }

            // デコーダON/OFF系機能の呼び出し
            if (Settings.DeactiveKey == 0) {
                setKeyCode(normalKeyCodes, (int)Settings.ActiveKey, DecoderKeys.TOGGLE_DECKEY);
            } else {
                setKeyCode(normalKeyCodes, (int)Settings.ActiveKey, DecoderKeys.ACTIVE_DECKEY);
                setKeyCode(normalKeyCodes, (int)Settings.DeactiveKey, DecoderKeys.DEACTIVE_DECKEY);
            }
            if (Settings.DeactiveKeyWithCtrl == 0) {
                setKeyCode(ctrlKeyCodes, (int)Settings.ActiveKeyWithCtrl, DecoderKeys.TOGGLE_DECKEY);
            } else {
                setKeyCode(ctrlKeyCodes, (int)Settings.ActiveKeyWithCtrl, DecoderKeys.ACTIVE_DECKEY);
                setKeyCode(ctrlKeyCodes, (int)Settings.DeactiveKeyWithCtrl, DecoderKeys.DEACTIVE_DECKEY);
            }

            // UI側機能の呼び出し
            setVkeyCode(DecoderKeys.STROKE_HELP_ROTATION_DECKEY);
            setVkeyCode(DecoderKeys.STROKE_HELP_UNROTATION_DECKEY);
            setVkeyCode(DecoderKeys.DATE_STRING_ROTATION_DECKEY);
            setVkeyCode(DecoderKeys.DATE_STRING_UNROTATION_DECKEY);

            // ストローク変換系機能の呼び出し

            // ショートカット系機能の呼び出し 
            setVkeyCode(DecoderKeys.FULL_ESCAPE_DECKEY);
            setVkeyCode(DecoderKeys.UNBLOCK_DECKEY);

            logger.InfoH("LEAVE");
            return true;
        }

        public uint GetVKeyFromDecKey(int deckey)
        {
            return strokeVKeys._getNth(deckey);
        }

        /// <summary> 打鍵で使われる仮想キー配列(DecKeyId順に並んでいる) </summary>
        private static uint[] strokeVKeys;

        private static uint[] VKeyArray106 = new uint[] {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
            0x51, 0x57, 0x45, 0x52, 0x54, 0x59, 0x55, 0x49, 0x4f, 0x50,
            0x41, 0x53, 0x44, 0x46, 0x47, 0x48, 0x4a, 0x4b, 0x4c, 0xbb,
            0x5a, 0x58, 0x43, 0x56, 0x42, 0x4e, 0x4d, 0xbc, 0xbe, 0xbf,
            0x20, 0xbd, 0xde, 0xdc, 0xc0, 0xdb, 0xba, 0xdd, 0xe2, 0x00,
        };

        /// <summary>
        /// 仮想キーコードからなるキーボードファイル(106.keyとか)を読み込んで、仮想キーコードの配列を作成する<br/>
        /// 仮想キーコードはDecKeyId順に並んでいる必要がある。
        /// </summary>
        /// <returns></returns>
        private bool readKeyboardFile()
        {
            logger.Info("ENTER");

            var array = VKeyArray106;

            var ini = KanchokuIni.Singleton;
            var filePath = ini.KanchokuDir._joinPath(Settings.KeyboardFile);
            if (filePath._notEmpty()) {
                logger.Info($"keyboard file path={filePath}");
                var vkeys = Helper.GetFileContent(filePath, Encoding.UTF8);
                if (vkeys == null) {
                    logger.Error($"Can't read keyboard file: {filePath}");
                    SystemHelper.ShowErrorMessageBox($"キーボード定義ファイル({filePath}の読み込みに失敗しました。");
                    return false;
                }
                var items = vkeys._split('\n').Select(line => line.Trim().Replace(" ", "")).
                    Where(line => line._notEmpty() && line[0] != '#' && line[0] != ';')._join("").TrimEnd(',')._split(',').ToArray();

                array = items.Select(x => (uint)x._parseHex(0)).ToArray();
                int idx = array._findIndex(x => x < 0);
                if (idx >= 0 && idx < array.Length) {
                    logger.Warn($"Invalid keyboard def: file={filePath}, {idx}th: {items[idx]}");
                    SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}の{idx}番目のキー定義({items[idx]})が誤っています。");
                    return false;
                }
                if (array.Length < DecoderKeys.NORMAL_DECKEY_NUM) {
                    logger.Warn($"No sufficient keyboard def: file={filePath}, total {array.Length} defs");
                    SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}のキー定義の数({array.Length})が不足しています。");
                    return false;
                }
            }
            logger.Info(() => $"keyboard keyNum={array.Length}, array={array.Select(x=>x.ToString("x"))._join(", ")}");

            strokeVKeys = array;

            logger.Info("LEAVE");
            return true;
        }

    }
}

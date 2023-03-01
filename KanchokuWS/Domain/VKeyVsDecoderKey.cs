using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Utils;

namespace KanchokuWS.Domain
{
    static class FuncVKeys
    {
        //public const uint BACK = 0x08;
        public const uint CONTROL = 0x11;
        public const uint ALT = 0x12;
        public const uint LSHIFT = 0xa0;
        public const uint RSHIFT = 0xa1;
        public const uint LCONTROL = 0xa2;
        public const uint RCONTROL = 0xa3;
        public const uint SPACE = (uint)Keys.Space;

        // 以下は JP/US によってキーコードが変わるor無効になる可能性あり
        public static uint CAPSLOCK => VKeyVsDecoderKey.GetFuncVKeyByIndex(3);
        public static uint EISU => VKeyVsDecoderKey.GetFuncVKeyByIndex(4);
        public static uint MUHENKAN => VKeyVsDecoderKey.GetFuncVKeyByIndex(5);
        public static uint HENKAN => VKeyVsDecoderKey.GetFuncVKeyByIndex(6);
        public static uint KANA => VKeyVsDecoderKey.GetFuncVKeyByIndex(7);
    }

    static class AlphabetVKeys
    {
        public static uint GetAlphabetVkeyByName(string name)
        {
            if (name._safeLength() != 1) return 0;
            char ch = name[0];
            if (ch >= 'a' && ch <= 'z') ch = (char)(ch - ('a' - 'A'));
            return (ch >= 'A' && ch <= 'Z') ? (uint)Keys.A + (uint)(ch - 'A') : 0;
        }
    }

    static class VKeyVsDecoderKey
    {
        private static Logger logger = Logger.GetLogger();

        /// <summary>
        /// キーボード設定がJPモードなら true
        /// </summary>
        public static bool IsJPmode { get; private set; } = true;

        /// <summary> 打鍵で使われる仮想キー配列(DecKeyId順に並んでいる) </summary>
        private static uint[] normalVKeys;

        private static uint[] VKeyArrayJP = new uint[] {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
            0x51, 0x57, 0x45, 0x52, 0x54, 0x59, 0x55, 0x49, 0x4f, 0x50,
            0x41, 0x53, 0x44, 0x46, 0x47, 0x48, 0x4a, 0x4b, 0x4c, 0xbb,
            0x5a, 0x58, 0x43, 0x56, 0x42, 0x4e, 0x4d, 0xbc, 0xbe, 0xbf,
            0x20, 0xbd, 0xde, 0xdc, 0xc0, 0xdb, 0xba, 0xdd, 0xe2, 0x00,
        };

        private static uint[] VKeyArrayUS = new uint[] {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
            0x51, 0x57, 0x45, 0x52, 0x54, 0x59, 0x55, 0x49, 0x4f, 0x50,
            0x41, 0x53, 0x44, 0x46, 0x47, 0x48, 0x4a, 0x4b, 0x4c, 0xba,
            0x5a, 0x58, 0x43, 0x56, 0x42, 0x4e, 0x4d, 0xbc, 0xbe, 0xbf,
            0x20, 0xbd, 0xbb, 0xdc, 0xdb, 0xdd, 0xde, 0xc0, 0xc1, 0x00,
        };

        /// <summary> 機能キーの VKeyの配列</summary>
        private static uint[] functionalVKeys;

        ///// <summary> 機能キー (Esc, 半/全, Tab, Caps, 英数, 無変換, 変換, かな, BS, Enter, Ins, Del, Home, End, PgUp, PgDn, ↑, ↓, ←, →)</summary>
        //private static uint[] vkeyArrayFuncKeys = {
        //    // 0 - 9
        //    /*Esc*/ 0x1b, /*半/全*/ 0xf3, /*Tab*/ 0x09, /*Caps*/ 0x14, /*英数*/ 0xf0, /*無変換*/ 0x1d, /*変換*/ 0x1c, /*かな*/ 0xf2, /*BS*/ 0x08, /*Enter*/ 0x0d,
        //    // 10 - 19
        //    /*Ins*/ 0x2d, /*Del*/ 0x2e, /*Home*/ 0x24, /*End*/ 0x23, /*PgUp*/ 0x21, /*PgDn*/ 0x22, /*↑*/ 0x26, /*↓*/ 0x28, /*←*/ 0x25, /*→*/ 0x27,
        //    // 20 - 23
        //    /*Lctrl*/ FuncVKeys.LCONTROL, /*Rctrl*/ FuncVKeys.RCONTROL, /*Lshift*/ FuncVKeys.LSHIFT, /*Rshift*/ FuncVKeys.RSHIFT,
        //    // 24 - 27
        //    /*ScrLock*/ 0x91, /*Pause*/ 0x13, /*IME ON*/ 0x16, /*IME OFF*/ 0x1a,
        //    // 28 - 37
        //    /*F1*/ 0x70, /*F2*/ 0x71, /*F3*/ 0x72, /*F4*/ 0x73, /*F5*/ 0x74, /*F6*/ 0x75, /*F7*/ 0x76, /*F8*/ 0x77, /*F9*/ 0x78, /*F10*/ 0x79,
        //    // 38 - 47
        //    /*F11*/ 0x7a, /*F12*/ 0x7b, /*F13*/ 0x7c, /*F14*/ 0x7d, /*F15*/ 0x7e, /*F16*/ 0x7f, /*F17*/ 0x80, /*F18*/ 0x81, /*F19*/ 0x82, /*F20*/ 0x83,
        //    // /*F21*/ 0x84, /*F22*/ 0x85, /*F23*/ 0x86, /*F24*/ 0x87,
        //};

        // 日本語キーボードだと Shift + 0x14 で CapsLock になる
        private const uint capsVkeyWithShift = 0x14;

        // 機能キー配列の初期化 (エラーがあったら、その行を返す)
        private static void initFunctionalVKeys()
        {
            functionalVKeys = new uint[DecoderKeys.FUNC_DECKEY_NUM];
            functionalVKeys[DecoderKeys.ESC_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.Escape;
            functionalVKeys[DecoderKeys.HANZEN_DECKEY - DecoderKeys.FUNC_DECKEY_START] = 0xf3;
            functionalVKeys[DecoderKeys.TAB_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.Tab;
            functionalVKeys[DecoderKeys.CAPS_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.CapsLock;
            functionalVKeys[DecoderKeys.ALNUM_DECKEY - DecoderKeys.FUNC_DECKEY_START] = 0xf0;
            functionalVKeys[DecoderKeys.NFER_DECKEY - DecoderKeys.FUNC_DECKEY_START] = 0x1d;
            functionalVKeys[DecoderKeys.XFER_DECKEY - DecoderKeys.FUNC_DECKEY_START] = 0x1c;
            functionalVKeys[DecoderKeys.KANA_DECKEY - DecoderKeys.FUNC_DECKEY_START] = 0xf2;
            functionalVKeys[DecoderKeys.BS_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.Back;
            functionalVKeys[DecoderKeys.ENTER_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.Enter;
            functionalVKeys[DecoderKeys.INS_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.Insert;
            functionalVKeys[DecoderKeys.DEL_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.Delete;
            functionalVKeys[DecoderKeys.HOME_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.Home;
            functionalVKeys[DecoderKeys.END_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.End;
            functionalVKeys[DecoderKeys.PAGE_UP_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.PageUp;
            functionalVKeys[DecoderKeys.PAGE_DOWN_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.PageDown;
            functionalVKeys[DecoderKeys.UP_ARROW_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.Up;
            functionalVKeys[DecoderKeys.DOWN_ARROW_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.Down;
            functionalVKeys[DecoderKeys.LEFT_ARROW_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.Left;
            functionalVKeys[DecoderKeys.RIGHT_ARROW_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.Right;
            functionalVKeys[DecoderKeys.PAUSE_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.Pause;
            functionalVKeys[DecoderKeys.SCR_LOCK_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.Scroll;
            functionalVKeys[DecoderKeys.IME_ON_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)0x16;
            functionalVKeys[DecoderKeys.IME_OFF_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)0x1a;
            functionalVKeys[DecoderKeys.LEFT_CONTROL_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.LControlKey;
            functionalVKeys[DecoderKeys.RIGHT_CONTROL_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.RControlKey;
            functionalVKeys[DecoderKeys.LEFT_SHIFT_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.LShiftKey;
            functionalVKeys[DecoderKeys.RIGHT_SHIFT_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.RShiftKey;
            functionalVKeys[DecoderKeys.F1_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F1;
            functionalVKeys[DecoderKeys.F2_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F2;
            functionalVKeys[DecoderKeys.F3_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F3;
            functionalVKeys[DecoderKeys.F4_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F4;
            functionalVKeys[DecoderKeys.F5_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F5;
            functionalVKeys[DecoderKeys.F6_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F6;
            functionalVKeys[DecoderKeys.F7_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F7;
            functionalVKeys[DecoderKeys.F8_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F8;
            functionalVKeys[DecoderKeys.F9_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F9;
            functionalVKeys[DecoderKeys.F10_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F10;
            functionalVKeys[DecoderKeys.F11_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F11;
            functionalVKeys[DecoderKeys.F12_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F12;
            functionalVKeys[DecoderKeys.F13_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F13;
            functionalVKeys[DecoderKeys.F14_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F14;
            functionalVKeys[DecoderKeys.F15_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F15;
            functionalVKeys[DecoderKeys.F16_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F16;
            functionalVKeys[DecoderKeys.F17_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F17;
            functionalVKeys[DecoderKeys.F18_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F18;
            functionalVKeys[DecoderKeys.F19_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F19;
            functionalVKeys[DecoderKeys.F20_DECKEY - DecoderKeys.FUNC_DECKEY_START] = (uint)Keys.F20;
        }

        /// <summary>
        /// 機能キーに対する VKeyテーブルから、そのインデックスに該当する VKey を取得する
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public static uint GetFuncVKeyByIndex(int idx)
        {
            return functionalVKeys._getNth(idx);
        }

        /// <summary>機能キーの名前から、VKeyテーブルにおいてそれに対応するインデックスを得る (-1なら該当せず)</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int GetFuncKeyIndexByName(string name)
        {
            int n = -1;
            switch (name._toLower()) {
                case "esc": case "escape": n = DecoderKeys.ESC_DECKEY; break;
                case "zenkaku": n = DecoderKeys.CTRL_HANZEN_DECKEY; break;
                case "tab": n = DecoderKeys.TAB_DECKEY; break;
                case "caps": case "capslock": n = DecoderKeys.CAPS_DECKEY; break;
                case "alnum": case "alphanum": case "eisu": n = DecoderKeys.ALNUM_DECKEY; break;
                case "nfer": case "muhenkan": n = DecoderKeys.NFER_DECKEY; break;
                case "xfer": case "henkan": n = DecoderKeys.XFER_DECKEY; break;
                case "kana": case "hiragana": n = DecoderKeys.KANA_DECKEY; break;
                case "bs": case "back": case "backspace": n = DecoderKeys.BS_DECKEY; break;
                case "enter": n = DecoderKeys.ENTER_DECKEY; break;
                case "ins": case "insert": n = DecoderKeys.INS_DECKEY; break;
                case "del": case "delete": n = DecoderKeys.DEL_DECKEY; break;
                case "home": n = DecoderKeys.HOME_DECKEY; break;
                case "end": n = DecoderKeys.END_DECKEY; break;
                case "pgup": case "pageup": n = DecoderKeys.PAGE_UP_DECKEY; break;
                case "pgdn": case "pagedown": n = DecoderKeys.PAGE_DOWN_DECKEY; break;
                case "up": case "uparrow": n = DecoderKeys.UP_ARROW_DECKEY; break;
                case "down": case "downarrow": n = DecoderKeys.DOWN_ARROW_DECKEY; break;
                case "left": case "leftarrow": n = DecoderKeys.LEFT_ARROW_DECKEY; break;
                case "right": case "rightarrow": n = DecoderKeys.RIGHT_ARROW_DECKEY; break;
                case "lctrl": n = DecoderKeys.LEFT_CONTROL_DECKEY; break;
                case "rctrl": n = DecoderKeys.RIGHT_CONTROL_DECKEY; break;
                case "lshift": n = DecoderKeys.LEFT_SHIFT_DECKEY; break;
                case "rshift": n = DecoderKeys.RIGHT_SHIFT_DECKEY; break;
                case "scrlock": n = DecoderKeys.SCR_LOCK_DECKEY; break;
                case "pause": n = DecoderKeys.PAUSE_DECKEY; break;
                case "imeon": n = DecoderKeys.IME_ON_DECKEY; break;
                case "imeoff": n = DecoderKeys.IME_OFF_DECKEY; break;
                case "f1": case "f01": n = DecoderKeys.F1_DECKEY; break;
                case "f2": case "f02": n = DecoderKeys.F2_DECKEY; break;
                case "f3": case "f03": n = DecoderKeys.F3_DECKEY; break;
                case "f4": case "f04": n = DecoderKeys.F4_DECKEY; break;
                case "f5": case "f05": n = DecoderKeys.F5_DECKEY; break;
                case "f6": case "f06": n = DecoderKeys.F6_DECKEY; break;
                case "f7": case "f07": n = DecoderKeys.F7_DECKEY; break;
                case "f8": case "f08": n = DecoderKeys.F1_DECKEY; break;
                case "f9": case "f09": n = DecoderKeys.F9_DECKEY; break;
                case "f10": n = DecoderKeys.F10_DECKEY; break;
                case "f11": n = DecoderKeys.F11_DECKEY; break;
                case "f12": n = DecoderKeys.F12_DECKEY; break;
                case "f13": n = DecoderKeys.F13_DECKEY; break;
                case "f14": n = DecoderKeys.F14_DECKEY; break;
                case "f15": n = DecoderKeys.F15_DECKEY; break;
                case "f16": n = DecoderKeys.F16_DECKEY; break;
                case "f17": n = DecoderKeys.F17_DECKEY; break;
                case "f18": n = DecoderKeys.F18_DECKEY; break;
                case "f19": n = DecoderKeys.F19_DECKEY; break;
                case "f20": n = DecoderKeys.F20_DECKEY; break;
                //case "f21": n = DecoderKeys.F21_DECKEY; break;
                //case "f22": n = DecoderKeys.F22_DECKEY; break;
                //case "f23": n = DecoderKeys.F23_DECKEY; break;
                //case "f24": n = DecoderKeys.F24_DECKEY; break;
                default: n = -1; break;
            }
            return n >= 0 ? n - DecoderKeys.FUNC_DECKEY_START : n;
        }

        /// <summary>
        /// 機能キーの名前から、それに対応する VKey を得る<br/>
        /// 対応するVKeyがなければ 0 を返す
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static uint GetFuncVkeyByName(string name)
        {
            if (name._toLower() == "space") {
                return (uint)Keys.Space;
            }
            if (name._toLower().StartsWith("vk")) {
                // "VKxx" のケース
                int vk = name._safeSubstring(2)._parseHex();
                if (vk > 0 && vk < 0xff) return (uint)vk;
            }
            return GetFuncVKeyByIndex(GetFuncKeyIndexByName(name));
        }

        /// <summary>
        /// 機能キーの名前から、それに対応する DecKey を得る<br/>
        /// 対応するDecKeyがなければ -1 を返す
        /// </summary>
        public static int GetFuncDeckeyByName(string name)
        {
            int dk = GetFuncKeyIndexByName(name);
            return dk >= 0 ? DecoderKeys.FUNC_DECKEY_START + dk : -1;
        }

        /// <summary>
        /// VKey から、それに対応する DecKey を得る<br/>
        /// 対応する DecKey がなければ -1 を返す
        /// </summary>
        /// <param name="vkey"></param>
        /// <returns></returns>
        public static int GetDecKeyFromVKey(uint vkey)
        {
            return normalVKeys._findIndex(x => x == vkey);
        }

        /// <summary>
        /// DecoderKey からそれに対応する VKey を得る
        /// </summary>
        /// <param name="deckey"></param>
        /// <returns></returns>
        public static uint GetVKeyFromDecKey(int deckey)
        {
            if (deckey >= DecoderKeys.NORMAL_DECKEY_START && deckey < DecoderKeys.NORMAL_DECKEY_NUM) {
                return normalVKeys._getNth(deckey);
            }  else if (deckey >= DecoderKeys.FUNC_DECKEY_START && deckey < DecoderKeys.FUNC_DECKEY_END) {
                // 機能DECKEY
                return functionalVKeys._getNth(deckey - DecoderKeys.FUNC_DECKEY_START);
            } else if (deckey >= DecoderKeys.SHIFT_DECKEY_START && deckey < DecoderKeys.SHIFT_DECKEY_END) {
                // SHIFT修飾DECKEY
                return normalVKeys._getNth(deckey - DecoderKeys.SHIFT_DECKEY_START);
            }  else if (deckey >= DecoderKeys.CTRL_DECKEY_START && deckey < DecoderKeys.CTRL_DECKEY_END) {
                // Ctrl修飾DECKEY
                return normalVKeys._getNth(deckey - DecoderKeys.CTRL_DECKEY_START);
            }  else if (deckey >= DecoderKeys.CTRL_FUNC_DECKEY_START && deckey < DecoderKeys.CTRL_FUNC_DECKEY_END) {
                // Ctrl修飾機能DECKEY
                return functionalVKeys._getNth(deckey - DecoderKeys.CTRL_FUNC_DECKEY_START);
            }  else if (deckey >= DecoderKeys.CTRL_SHIFT_DECKEY_START && deckey < DecoderKeys.CTRL_SHIFT_DECKEY_END) {
                // Ctrl+Shift修飾DECKEY
                return normalVKeys._getNth(deckey - DecoderKeys.CTRL_SHIFT_DECKEY_START);
            }  else if (deckey >= DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_START && deckey < DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_END) {
                // Ctrl+Shift修飾機能DECKEY
                return functionalVKeys._getNth(deckey - DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_START);
            }
            return 0;
        }

        /// <summary>
        /// 仮想キーコードからなるキーボードファイル(106.keyとか)を読み込んで、仮想キーコードの配列を作成する<br/>
        /// 仮想キーコードはDecKeyId順に並んでいる必要がある。<br/>
        /// NAME=xx の形式で、機能キーの仮想キーコード定義を記述できる
        /// </summary>
        /// <returns></returns>
        public static bool ReadKeyboardFile()
        {
            logger.InfoH("ENTER");

            initFunctionalVKeys();

            var kbName = Settings.GetString("keyboard", "JP");
            if (kbName._isEmpty() || kbName._toUpper() == "JP") {
                normalVKeys = VKeyArrayJP;
            } else if (kbName._toUpper() == "US") {
                normalVKeys = VKeyArrayUS;
                IsJPmode = false;
            } else {
                var filePath = KanchokuIni.Singleton.KanchokuDir._joinPath(Settings.KeyboardFileDir, kbName);
                logger.Info($"keyboard file path={filePath}");
                var allLines = Helper.GetFileContent(filePath, Encoding.UTF8);
                if (allLines == null) {
                    logger.Error($"Can't read keyboard file: {filePath}");
                    SystemHelper.ShowErrorMessageBox($"キーボード定義ファイル({filePath}の読み込みに失敗しました。");
                    return false;
                }
                var lines = allLines._split('\n').Select(line => line.Trim().Replace(" ", "")).Where(line => line._notEmpty() && line[0] != '#' && line[0] != ';').ToArray();

                List<uint> list = null;

                // MODE=JP or MODE=US
                if (lines._notEmpty()) {
                    var items = lines._getFirst()._toUpper()._split('=');
                    if (items._safeLength() == 2 && items[0] == "MODE") {
                        if (items[1] == "JP") {
                            list = VKeyArrayJP.ToList();
                        } else if (items[1] == "US") {
                            list = VKeyArrayUS.ToList();
                            IsJPmode = false;
                        }
                    }
                }
                // ストロークキーの仮想キーコードを得る
                var hexes = lines.Where(line => line.IndexOf('=') < 0)._join("").TrimEnd(',')._split(',').ToArray();
                if (hexes._notEmpty()) {
                    list = hexes.Select(x => (uint)x._parseHex(0)).ToList();
                    int idx = list.FindIndex(x => x < 0 || x >= 0x100);
                    if (idx >= 0 && idx < list.Count) {
                        logger.Warn($"Invalid keyboard def: file={filePath}, {idx}th: {hexes[idx]}");
                        SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}の{idx}番目のキー定義({hexes[idx]})が誤っています。");
                        return false;
                    }
                    //if (list.Count < DecoderKeys.NORMAL_DECKEY_NUM) {
                    //    logger.Warn($"No sufficient keyboard def: file={filePath}, total {list.Length} defs");
                    //    SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}のキー定義の数({list.Length})が不足しています。");
                    //    return false;
                    //}
                    for (int i = list.Count; i < DecoderKeys.NORMAL_DECKEY_NUM; ++i) list.Add(0);
                }

                if (list._isEmpty()) list = VKeyArrayJP.ToList();

                normalVKeys = list.ToArray();

                // 機能キー
                // NAME=xx の形式で、機能キー(Esc, BS, Enter, 矢印キーなど)の仮想キーコード定義を得る
                foreach (var line in lines) {
                    var items = line._toLower()._split('=');
                    if (items._safeLength() == 2 && items[0]._notEmpty() && items[1]._notEmpty()) {
                        int n = -1;
                        int vk = items[1]._parseHex();
                        if (vk >= 0 && vk < 0x100) {
                            n = GetFuncKeyIndexByName(items[0]);
                        }
                        if (n >= 0 && n < functionalVKeys.Length) {
                            functionalVKeys[n] = (uint)vk;
                        } else {
                            logger.Warn($"Invalid functional key def: file={filePath}, line: {line}");
                            SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}の行 {line} が誤っています。");
                            return false;
                        }
                    }
                }
            }
            logger.Info(() => $"keyboard keyNum={normalVKeys.Length}, array={normalVKeys.Select(x => x.ToString("x"))._join(", ")}");

            setupDecKeyAndComboTable();

            logger.InfoH("LEAVE");
            return true;
        }

        /// <summary>
        /// 仮想キーとそれをSHIFT修飾したキーに対する DecKey の登録
        /// </summary>
        private static void setupDecKeyAndComboTable()
        {
            logger.InfoH($"ENTER");
            // 通常文字ストロークキー
            for (int id = 0; id < DecoderKeys.NORMAL_DECKEY_NUM; ++id) {
                uint vkey = GetVKeyFromDecKey(id);
                if (vkey > 0) {
                    // Normal
                    VKeyComboRepository.AddDecKeyAndCombo(id, 0, vkey);
                    // Shifted
                    VKeyComboRepository.AddDecKeyAndCombo(DecoderKeys.SHIFT_DECKEY_START + id, KeyModifiers.MOD_SHIFT, vkey);
                    // Ctrl
                    //AddDecKeyAndCombo(DecoderKeys.CTRL_DECKEY_START + id, KeyModifiers.MOD_CONTROL, vkey);
                    // Ctrl+Shift
                    //AddDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_DECKEY_START + id, KeyModifiers.MOD_CONTROL + KeyModifiers.MOD_SHIFT, vkey);
                }
            }

            // 機能キー(RSHFTも登録される)
            for (int id = 0; id < DecoderKeys.FUNC_DECKEY_NUM; ++id) {
                uint vkey = GetVKeyFromDecKey(DecoderKeys.FUNC_DECKEY_START + id);
                if (vkey > 0) {
                    // Normal
                    VKeyComboRepository.AddDecKeyAndCombo(DecoderKeys.FUNC_DECKEY_START + id, 0, vkey);
                    // Shift
                    if (vkey == capsVkeyWithShift) VKeyComboRepository.AddDecKeyAndCombo(DecoderKeys.FUNC_DECKEY_START + id, KeyModifiers.MOD_SHIFT, vkey);
                    // Ctrl
                    //AddDecKeyAndCombo(DecoderKeys.CTRL_FUNC_DECKEY_START + id, KeyModifiers.MOD_CONTROL, vkey);
                    // Ctrl+Shifted
                    //AddDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_START + id, KeyModifiers.MOD_CONTROL + KeyModifiers.MOD_SHIFT, vkey);
                }
            }

            // Shift+Tab
            VKeyComboRepository.AddDecKeyAndCombo(DecoderKeys.SHIFT_TAB_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.Tab);
            //AddModConvertedDecKeyFromCombo(DecoderKeys.SHIFT_TAB_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.Tab);
            logger.InfoH($"LEAVE");
        }

    }

}

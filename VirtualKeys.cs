using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Utils;

namespace KanchokuWS
{
    public static class KeyModifiers
    {
        // VKEY に対する modifier ALT
        public const uint MOD_ALT = 0x0001;

        // VKEY に対する modifier CTRL
        public const uint MOD_CONTROL = 0x0002;

        // VKEY に対する modifier SHIFT
        public const uint MOD_SHIFT = 0x0004;

        // VKEY に対する modifier WIN
        public const uint MOD_WIN = 0x0008;

        // VKEY に対する modifier Space
        public const uint MOD_SPACE = 0x0100;

        // VKEY に対する modifier CapsLock
        public const uint MOD_CAPS = 0x0200;

        // VKEY に対する modifier 英数
        public const uint MOD_ALNUM = 0x0400;

        // VKEY に対する modifier NFER
        public const uint MOD_NFER = 0x0800;

        // VKEY に対する modifier XFER
        public const uint MOD_XFER = 0x1000;

        // VKEY に対する modifier KANA
        public const uint MOD_KANA = 0x2000;

        public static uint MakeModifier(bool ctrl, bool shift)
        {
            return (ctrl ? MOD_CONTROL : 0) + (shift ? MOD_SHIFT : 0);
        }

    }

    /// <summary>
    /// 修飾キーと仮想キーの組み合わせ
    /// </summary>
    public struct VKeyCombo {
        public uint modifier;
        public uint vkey;

        public VKeyCombo(uint mod, uint vk)
        {
            modifier = mod;
            vkey = vk;
        }

        public uint SerialValue => CalcSerialValue(modifier, vkey);

        public static uint CalcSerialValue(uint mod, uint vkey)
        {
            return ((mod & 0xffff) << 16) + (vkey & 0xffff);
        }
    }

    public static class VirtualKeys
    {
        private static Logger logger = Logger.GetLogger();

        public const uint BACK = 0x08;
        public const uint CONTROL = 0x11;
        public const uint LSHIFT = 0xa0;
        public const uint RSHIFT = 0xa1;
        public const uint LCONTROL = 0xa2;
        public const uint RCONTROL = 0xa3;

        /// <summary> 打鍵で使われる仮想キー配列(DecKeyId順に並んでいる) </summary>
        private static uint[] strokeVKeys;

        private static uint[] VKeyArray106 = new uint[] {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
            0x51, 0x57, 0x45, 0x52, 0x54, 0x59, 0x55, 0x49, 0x4f, 0x50,
            0x41, 0x53, 0x44, 0x46, 0x47, 0x48, 0x4a, 0x4b, 0x4c, 0xbb,
            0x5a, 0x58, 0x43, 0x56, 0x42, 0x4e, 0x4d, 0xbc, 0xbe, 0xbf,
            0x20, 0xbd, 0xde, 0xdc, 0xc0, 0xdb, 0xba, 0xdd, 0xe2, 0x00,
        };

        public const uint CapsLock = 0x14;
        public const uint AlphaNum = 0xf0;
        public const uint Nfer = 0x1d;
        public const uint Xfer = 0x1c;
        public const uint Hiragana = 0xf2;
        public const uint Zenkaku = 0xf3;
        public const uint Kanji = 0xf4;

        /// <summary> 機能キー (Esc, 半/全, Tab, Caps, 英数, 無変換, 変換, かな, BS, Enter, Ins, Del, Home, End, PgUp, PgDn, ↑, ↓, ←, →)</summary>
        private static uint[] vkeyArrayFuncKeys = {
            /*Esc*/ 0x1b, /*半/全*/ Zenkaku, /*Tab*/ 0x09, /*Caps*/ 0x14, /*英数*/ 0xf0, /*無変換*/ Nfer, /*変換*/ Xfer, /*かな*/ Hiragana, /*BS*/ 0x08, /*Enter*/ 0x0d,
            /*Ins*/ 0x2d, /*Del*/ 0x2e, /*Home*/ 0x24, /*End*/ 0x23, /*PgUp*/ 0x21, /*PgDn*/ 0x22, /*↑*/ 0x26, /*↓*/ 0x28, /*←*/ 0x25, /*→*/ 0x27
        };

        private const uint capsVkeyWithShift = 0x14;    // 日本語キーボードだと Shift + 0x14 で CapsLock になる

        private static uint getVKeyFromDecKey(int deckey)
        {
            if (deckey < 0) return 0;

            bool bFunc = false;
            if (deckey >= DecoderKeys.SHIFT_DECKEY_START && deckey < DecoderKeys.FUNC_DECKEY_START) {
                deckey -= DecoderKeys.SHIFT_DECKEY_START;
            }  else if (deckey >= DecoderKeys.FUNC_DECKEY_START && deckey < DecoderKeys.CTRL_DECKEY_START) {
                deckey -= DecoderKeys.FUNC_DECKEY_START;
                bFunc = true;
            }  else if (deckey >= DecoderKeys.CTRL_DECKEY_START && deckey < DecoderKeys.CTRL_FUNC_DECKEY_START) {
                deckey -= DecoderKeys.CTRL_DECKEY_START;
            }  else if (deckey >= DecoderKeys.CTRL_FUNC_DECKEY_START && deckey < DecoderKeys.CTRL_SHIFT_DECKEY_START) {
                deckey -= DecoderKeys.CTRL_FUNC_DECKEY_START;
                bFunc = true;
            }  else if (deckey >= DecoderKeys.CTRL_SHIFT_DECKEY_START && deckey < DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_START) {
                deckey -= DecoderKeys.CTRL_SHIFT_DECKEY_START;
            }  else if (deckey >= DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_START && deckey < DecoderKeys.TOTAL_DECKEY_NUM) {
                deckey -= DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_START;
                bFunc = true;
            }
            return bFunc ? vkeyArrayFuncKeys._getNth(deckey) : strokeVKeys._getNth(deckey);
        }

        private static Dictionary<string, uint> faceToVkey = new Dictionary<string, uint>() {
            {" ", (uint)Keys.Space },
            {"SPACE", (uint)Keys.Space },
            {"A", (uint)Keys.A },
            {"B", (uint)Keys.B },
            {"C", (uint)Keys.C },
            {"D", (uint)Keys.D },
            {"E", (uint)Keys.E },
            {"F", (uint)Keys.F },
            {"G", (uint)Keys.G },
            {"H", (uint)Keys.H },
            {"I", (uint)Keys.I },
            {"J", (uint)Keys.J },
            {"K", (uint)Keys.K },
            {"L", (uint)Keys.L },
            {"M", (uint)Keys.M },
            {"N", (uint)Keys.N },
            {"O", (uint)Keys.O },
            {"P", (uint)Keys.P },
            {"Q", (uint)Keys.Q },
            {"R", (uint)Keys.R },
            {"S", (uint)Keys.S },
            {"T", (uint)Keys.T },
            {"U", (uint)Keys.U },
            {"V", (uint)Keys.V },
            {"W", (uint)Keys.W },
            {"X", (uint)Keys.X },
            {"Y", (uint)Keys.Y },
            {"Z", (uint)Keys.Z },
            { "COLON", (uint)Keys.Oem1 },       // ba
            { "PLUS", (uint)Keys.Oemplus },     // bb
            { "COMMA", (uint)Keys.Oemcomma },   // bc
            { "MINUS", (uint)Keys.OemMinus },   // bd
            { "PERIOD", (uint)Keys.OemPeriod }, // be
            { "SLASH", (uint)Keys.Oem2 },       // bf
            { "BQUOTE", (uint)Keys.Oem3 },      // c0/106
            { "OEM4", (uint)Keys.Oem4 },        // db
            { "OEM5", (uint)Keys.Oem5 },        // dc
            { "OEM6", (uint)Keys.Oem6 },        // dd
            { "OEM7", (uint)Keys.Oem7 },        // de
            { "OEM8", (uint)Keys.Oem8 },        // df
            { "OEM102", (uint)Keys.Oem102 },    // e2/106
        };

        public static VKeyCombo EmptyCombo = new VKeyCombo(0, 0);

        public static VKeyCombo CtrlV_VKeyCombo = new VKeyCombo(KeyModifiers.MOD_CONTROL, faceToVkey["V"]);

        public static VKeyCombo? GetVKeyComboFromFaceString(string face, bool ctrl, bool shift)
        {
            uint vkey = faceToVkey._safeGet(face);
            if (vkey > 0) {
                return new VKeyCombo(KeyModifiers.MakeModifier(ctrl, shift), vkey);
            }
            return null;
        }

        /// <summary>
        /// DECKEY id から仮想キーコンビネーションを得るための配列
        /// </summary>
        private static VKeyCombo?[] VKeyComboFromDecKey;

        public static VKeyCombo? GetVKeyComboFromDecKey(int deckey)
        {
            return VKeyComboFromDecKey._getNth(deckey);
        }

        /// <summary>
        /// 仮想キーコンビネーションのSerial値からDECKEY を得るための辞書
        /// </summary>
        private static Dictionary<uint, int> DecKeyFromVKeyCombo;

        /// <summary>
        /// 仮想キーコンビネーションのSerial値からModキーによるシフト変換されたDECKEY を得るための辞書
        /// </summary>
        private static Dictionary<uint, int> ModConvertedDecKeyFromVKeyCombo;

        public static void AddDecKeyAndCombo(int deckey, uint mod, uint vkey)
        {
            logger.Debug(() => $"deckey={deckey:x}H({deckey}), mod={mod:x}H, vkey={vkey:x}H({vkey})");
            var combo = new VKeyCombo(mod, (uint)vkey);
            VKeyComboFromDecKey[deckey] = combo;
            DecKeyFromVKeyCombo[combo.SerialValue] = deckey;
        }

        public static void AddModConvertedDecKeyFromCombo(int deckey, uint mod, uint vkey)
        {
            logger.Debug(() => $"deckey={deckey:x}H({deckey}), mod={mod:x}H, vkey={vkey:x}H({vkey})");
            ModConvertedDecKeyFromVKeyCombo[VKeyCombo.CalcSerialValue(mod, vkey)] = deckey;
        }

        private static void setupDecKeyAndComboTable()
        {
            // ストロークキー
            for (int id = 0; id < DecoderKeys.NORMAL_DECKEY_NUM; ++id) {
                uint vkey = getVKeyFromDecKey(id);
                // Normal
                AddDecKeyAndCombo(id, 0, vkey);
                // Shifted
                AddDecKeyAndCombo(DecoderKeys.SHIFT_DECKEY_START + id, KeyModifiers.MOD_SHIFT, vkey);
                // Ctrl
                //AddDecKeyAndCombo(DecoderKeys.CTRL_DECKEY_START + id, KeyModifiers.MOD_CONTROL, vkey);
                // Ctrl+Shift
                //AddDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_DECKEY_START + id, KeyModifiers.MOD_CONTROL + KeyModifiers.MOD_SHIFT, vkey);
            }

            // 機能キー
            for (int id = 0; id < DecoderKeys.FUNC_DECKEY_NUM; ++id) {
                uint vkey = getVKeyFromDecKey(DecoderKeys.FUNC_DECKEY_START + id);
                // Normal
                AddDecKeyAndCombo(DecoderKeys.FUNC_DECKEY_START + id, 0, vkey);
                // Shift
                if (vkey == capsVkeyWithShift) AddDecKeyAndCombo(DecoderKeys.FUNC_DECKEY_START + id, KeyModifiers.MOD_SHIFT, vkey);
                // Ctrl
                //AddDecKeyAndCombo(DecoderKeys.CTRL_FUNC_DECKEY_START + id, KeyModifiers.MOD_CONTROL, vkey);
                // Ctrl+Shifted
                //AddDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_START + id, KeyModifiers.MOD_CONTROL + KeyModifiers.MOD_SHIFT, vkey);
            }
        }

        public static int GetDecKeyFromCombo(uint mod, uint vkey)
        {
            return DecKeyFromVKeyCombo._safeGet(VKeyCombo.CalcSerialValue(mod, vkey), -1);
        }

        public static int GetModConvertedDecKeyFromCombo(uint mod, uint vkey)
        {
            int deckey = ModConvertedDecKeyFromVKeyCombo._safeGet(VKeyCombo.CalcSerialValue(mod, vkey), -1);
            //return deckey > 0 ? deckey : DecKeyFromVKeyCombo._safeGet(VKeyCombo.CalcSerialValue(mod, vkey), -1);
            return deckey > 0 ? deckey : GetDecKeyFromCombo(mod, vkey);
        }

        public static int GetCtrlDecKeyOf(string face)
        {
            uint vkey = faceToVkey._safeGet(face);
            return (vkey > 0) ? GetModConvertedDecKeyFromCombo(KeyModifiers.MOD_CONTROL, vkey) : -1;
        }

        // 静的コンストラクタ
        static VirtualKeys()
        {
            VKeyComboFromDecKey = new VKeyCombo?[DecoderKeys.GLOBAL_DECKEY_ID_END];
            DecKeyFromVKeyCombo = new Dictionary<uint, int>();
            ModConvertedDecKeyFromVKeyCombo = new Dictionary<uint, int>();
        }

        public static void AddCtrlDeckeyFromCombo(string keyFace, int ctrlDeckey, int ctrlShiftDeckey)
        {
            var combo = GetVKeyComboFromFaceString(keyFace, false, false);
            if (combo != null) {
                if (ctrlDeckey > 0) AddModConvertedDecKeyFromCombo(ctrlDeckey, KeyModifiers.MOD_CONTROL, combo.Value.vkey);
                if (ctrlShiftDeckey > 0) AddModConvertedDecKeyFromCombo(ctrlShiftDeckey, KeyModifiers.MOD_CONTROL | KeyModifiers.MOD_SHIFT, combo.Value.vkey);
            }
        }

        public static void AddCtrlDeckeyAndCombo(string keyFace, int ctrlDeckey, int ctrlShiftDeckey)
        {
                var combo = GetVKeyComboFromFaceString(keyFace, false, false);
                if (combo != null) {
                    if (ctrlDeckey > 0) AddDecKeyAndCombo(ctrlDeckey, KeyModifiers.MOD_CONTROL, combo.Value.vkey);
                    if (ctrlShiftDeckey > 0) AddDecKeyAndCombo(ctrlShiftDeckey, KeyModifiers.MOD_CONTROL | KeyModifiers.MOD_SHIFT, combo.Value.vkey);
                }
        }

        /// <summary>
        /// 仮想キーコードからなるキーボードファイル(106.keyとか)を読み込んで、仮想キーコードの配列を作成する<br/>
        /// 仮想キーコードはDecKeyId順に並んでいる必要がある。
        /// </summary>
        /// <returns></returns>
        public static bool ReadKeyboardFile()
        {
            logger.Info("ENTER");

            var array = VKeyArray106;

            var filePath = KanchokuIni.Singleton.KanchokuDir._joinPath(Settings.GetString("keyboard", "106.key"));
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
            logger.Info(() => $"keyboard keyNum={array.Length}, array={array.Select(x => x.ToString("x"))._join(", ")}");

            strokeVKeys = array;

            setupDecKeyAndComboTable();

            logger.Info("LEAVE");
            return true;
        }

        private static Dictionary<string, uint> modifierKeysFromName = new Dictionary<string, uint>() {
            {"caps", KeyModifiers.MOD_CAPS },
            {"alnum", KeyModifiers.MOD_ALNUM },
            {"nfer", KeyModifiers.MOD_NFER },
            {"xfer", KeyModifiers.MOD_XFER },
            {"cana", KeyModifiers.MOD_KANA },
        };

        private static Dictionary<string, int> specialDecKeysFromName = new Dictionary<string, int>() {
            {"bs", DecoderKeys.BS_DECKEY },
            {"backspace", DecoderKeys.BS_DECKEY },
            {"del", DecoderKeys.DEL_DECKEY},
            {"delete", DecoderKeys.DEL_DECKEY},
            {"left", DecoderKeys.LEFT_ARROW_DECKEY},
            {"leftarrow", DecoderKeys.LEFT_ARROW_DECKEY},
            {"right", DecoderKeys.RIGHT_ARROW_DECKEY},
            {"rightarrow", DecoderKeys.RIGHT_ARROW_DECKEY},
            {"up", DecoderKeys.UP_ARROW_DECKEY},
            {"uparrow", DecoderKeys.UP_ARROW_DECKEY},
            {"down", DecoderKeys.DOWN_ARROW_DECKEY},
            {"downarrow", DecoderKeys.DOWN_ARROW_DECKEY},
            {"home", DecoderKeys.HOME_DECKEY},
            {"end", DecoderKeys.END_DECKEY},
            {"esc", DecoderKeys.ESC_DECKEY},
            {"escape", DecoderKeys.ESC_DECKEY},
            {"tab", DecoderKeys.TAB_DECKEY},
            {"enter", DecoderKeys.ENTER_DECKEY},
            {"ins", DecoderKeys.INS_DECKEY},
            {"insert", DecoderKeys.INS_DECKEY},
            {"pgup", DecoderKeys.PAGE_UP_DECKEY},
            {"pageup", DecoderKeys.PAGE_UP_DECKEY},
            {"pgdn", DecoderKeys.PAGE_DOWN_DECKEY},
            {"pagedown", DecoderKeys.PAGE_DOWN_DECKEY},
        };

        /// <summary>
        /// 追加の modifier 変換表を読み込む
        /// </summary>
        public static bool ReadExtraModConversionFile(string filename)
        {
            logger.Info("ENTER");
            if (filename._notEmpty()) {
                var filePath = KanchokuIni.Singleton.KanchokuDir._joinPath(filename);
                logger.Info($"modConversion file path={filePath}");
                var lines = Helper.GetFileContent(filePath, Encoding.UTF8);
                if (lines == null) {
                    logger.Error($"Can't read modConversion file: {filePath}");
                    SystemHelper.ShowErrorMessageBox($"修飾キー変換定義ファイル({filePath}の読み込みに失敗しました。");
                    return false;
                }
                int nl = 0;
                foreach (var rawLine in lines._split('\n')) {
                    ++nl;
                    var line = rawLine._reReplace("#.*", "").Trim().Replace(" ", "")._toLower();
                    if (line._notEmpty() && line[0] != '#') {
                        var items = line._split(':');
                        if (items._length() == 3) {
                            uint mod = modifierKeysFromName._safeGet(items[0]);
                            uint vkey = getVKeyFromDecKey(items[1]._parseInt(-1, -1));
                            int deckey = specialDecKeysFromName._safeGet(items[2]);
                            if (mod != 0 && vkey > 0 && deckey > 0) {
                                logger.DebugH(() => $"AddModConvertedDecKeyFromCombo: deckey={deckey}, mod={mod}, vkey={vkey}, rawLine={rawLine}");
                                AddModConvertedDecKeyFromCombo(deckey, mod, vkey);
                                continue;
                            }
                        }
                        logger.Warn($"Invalid line({nl}): {rawLine}");
                    }
                }
            }
            logger.Info("LEAVE");
            return true;
        }
    }
}

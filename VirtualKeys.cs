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

        // VKEY に対する modifier RSHIFT
        public const uint MOD_RSHIFT = 0x2000;

        // VKEY に対する modifier LCTRL
        public const uint MOD_LCTRL = 0x4000;

        // VKEY に対する modifier RCTRL
        public const uint MOD_RCTRL = 0x8000;

        // 単打用キー
        public const uint MOD_SINGLE = 0x10000;

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

        //public const uint BACK = 0x08;
        public const uint CONTROL = 0x11;
        public const uint LSHIFT = 0xa0;
        public const uint RSHIFT = 0xa1;
        public const uint LCONTROL = 0xa2;
        public const uint RCONTROL = 0xa3;
        public const uint SPACE = (uint)Keys.Space;

        /// <summary> 打鍵で使われる仮想キー配列(DecKeyId順に並んでいる) </summary>
        private static uint[] strokeVKeys;

        private static uint[] VKeyArray106 = new uint[] {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
            0x51, 0x57, 0x45, 0x52, 0x54, 0x59, 0x55, 0x49, 0x4f, 0x50,
            0x41, 0x53, 0x44, 0x46, 0x47, 0x48, 0x4a, 0x4b, 0x4c, 0xbb,
            0x5a, 0x58, 0x43, 0x56, 0x42, 0x4e, 0x4d, 0xbc, 0xbe, 0xbf,
            0x20, 0xbd, 0xde, 0xdc, 0xc0, 0xdb, 0xba, 0xdd, 0xe2, 0x00,
        };

        public static uint CapsLock => vkeyArrayFuncKeys[3];
        public static uint AlphaNum => vkeyArrayFuncKeys[4];
        public static uint Nfer => vkeyArrayFuncKeys[5];
        public static uint Xfer => vkeyArrayFuncKeys[6];
        public static uint Hiragana => vkeyArrayFuncKeys[7];
        //public static uint Zenkaku => vkeyArrayFuncKeys[1];
        //public static uint Kanji = 0xf4;

        /// <summary> 機能キー (Esc, 半/全, Tab, Caps, 英数, 無変換, 変換, かな, BS, Enter, Ins, Del, Home, End, PgUp, PgDn, ↑, ↓, ←, →)</summary>
        private static uint[] vkeyArrayFuncKeys = {
            /*Esc*/ 0x1b, /*半/全*/ 0xf3, /*Tab*/ 0x09, /*Caps*/ 0x14, /*英数*/ 0xf0, /*無変換*/ 0x1d, /*変換*/ 0x1c, /*かな*/ 0xf2, /*BS*/ 0x08, /*Enter*/ 0x0d,
            /*Ins*/ 0x2d, /*Del*/ 0x2e, /*Home*/ 0x24, /*End*/ 0x23, /*PgUp*/ 0x21, /*PgDn*/ 0x22, /*↑*/ 0x26, /*↓*/ 0x28, /*←*/ 0x25, /*→*/ 0x27, /*Rshift*/ RSHIFT
        };

        private const uint capsVkeyWithShift = 0x14;    // 日本語キーボードだと Shift + 0x14 で CapsLock になる

        /// <summary>機能キーのインデックスを得る (-1なら該当せず)</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int GetFuncKeyIndexByName(string name)
        {
            int n = -1;
            switch (name._toLower()) {
                case "esc": case "escape": n = 0; break;
                case "zenkaku": n = 1; break;
                case "tab": n = 2; break;
                case "caps": case "capslock": n = 3; break;
                case "alnum": case "alphanum": case "eisu": n = 4; break;
                case "nfer": n = 5; break;
                case "xfer": n = 6; break;
                case "kana": case "hiragana": n = 7; break;
                case "bs": case "back": case "backspace": n = 8; break;
                case "enter": n = 9; break;
                case "ins": case "insert": n = 10; break;
                case "del": case "delete": n = 11; break;
                case "home": n = 12; break;
                case "end": n = 13; break;
                case "pgup": case "pageup": n = 14; break;
                case "pgdn": case "pagedown": n = 15; break;
                case "up": case "uparrow": n = 16; break;
                case "down": case "downarrow": n = 17; break;
                case "left": case "leftarrow": n = 18; break;
                case "right": case "rightarrow": n = 19; break;
                case "rshift": n = 20; break;
                default: n = -1; break;
            }
            return n;
        }

        public static int GetFuncDeckeyByName(string name)
        {
            int dk = GetFuncKeyIndexByName(name);
            return dk >= 0 ? DecoderKeys.FUNC_DECKEY_START + dk : -1;
        }

        public static uint GetFuncVkeyByName(string name)
        {
            return vkeyArrayFuncKeys._getNth(GetFuncKeyIndexByName(name));
        }

        public enum ShiftPlane
        {
            NONE = 0,
            NormalPlane = 1,
            PlaneA = 2,
            PlaneB = 3
        }

        /// <summary> デコーダ機能に割り当てられた拡張修飾キー(space, Caps, alnum, nfer, xfer, Rshift)のVkeyを集めた集合 </summary>
        private static HashSet<uint> decoderFuncAssignedExModKeys = new HashSet<uint>();

        /// <summary> インデックスで指定される拡張修飾キー(space, Caps, alnum, nfer, xfer, Rshift)をデコーダ機能に割り当てられたキーの集合に追加 </summary>
        public static void AddExModVkeyAssignedForDecoderFuncByIndex(int idx)
        {
            if (idx >= 0 && idx < vkeyArrayFuncKeys.Length)
            decoderFuncAssignedExModKeys.Add(vkeyArrayFuncKeys[idx]);
        }

        /// <summary> 拡張修飾キー(space, Caps, alnum, nfer, xfer, Rshift)がデコーダ機能に割り当てられているか </summary>
        public static bool IsExModKeyIndexAssignedForDecoderFunc(uint vkey)
        {
            return decoderFuncAssignedExModKeys.Contains(vkey);
        }

        /// <summary> シフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) に割り当てるシフト面</summary>
        private static Dictionary<uint, ShiftPlane> shiftPlaneForShiftModFlag = new Dictionary<uint, ShiftPlane>();

        /// <summary> DecoderがOffの時のシフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) に割り当てるシフト面</summary>
        private static Dictionary<uint, ShiftPlane> shiftPlaneForShiftModFlagWhenDecoderOff = new Dictionary<uint, ShiftPlane>();

        /// <summary> シフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) に割り当てられたシフト面を得る</summary>
        public static ShiftPlane GetShiftPlaneFromShiftModFlag(uint modFlag, bool bDecoderOn)
        {
            return bDecoderOn ? shiftPlaneForShiftModFlag._safeGet(modFlag, ShiftPlane.NONE) : shiftPlaneForShiftModFlagWhenDecoderOff._safeGet(modFlag, ShiftPlane.NONE);
        }

        /// <summary> シフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) にシフト面が割り当てられているか</summary>
        public static bool IsShiftPlaneAssignedForShiftModFlag(uint modFlag, bool bDecoderOn)
        {
            return GetShiftPlaneFromShiftModFlag(modFlag, bDecoderOn) != ShiftPlane.NONE;
        }

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
            var combo = VKeyComboFromDecKey._getNth(deckey);
            logger.DebugH(() => $"deckey={deckey:x}H({deckey}), combo.mod={(combo.HasValue ? combo.Value.modifier : 0):x}, combo.vkey={(combo.HasValue ? combo.Value.vkey : 0)}");
            return combo;
        }

        /// <summary>
        /// 仮想キーコンビネーションのSerial値からDECKEY を得るための辞書
        /// </summary>
        private static Dictionary<uint, int> DecKeyFromVKeyCombo;

        /// <summary>
        /// 仮想キーコンビネーションのSerial値からModキーによるシフト変換されたDECKEY を得るための辞書
        /// </summary>
        private static Dictionary<uint, int> ModConvertedDecKeyFromVKeyCombo;

        public static void AddModifiedDeckey(int deckey, uint mod, uint vkey)
        {
            logger.DebugH(() => $"deckey={deckey:x}H({deckey}), mod={mod:x}H, vkey={vkey:x}H({vkey})");
            var combo = new VKeyCombo(mod, vkey);
            VKeyComboFromDecKey[deckey] = combo;
        }

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

        public static void RemoveModConvertedDecKeyFromCombo(uint mod, uint vkey)
        {
            logger.Debug(() => $"mod={mod:x}H, vkey={vkey:x}H({vkey})");
            try {
                ModConvertedDecKeyFromVKeyCombo.Remove(VKeyCombo.CalcSerialValue(mod, vkey));
            } catch { }
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

            // 機能キー(RSHFTも登録される)
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
            Initialize();
        }

        public static void Initialize()
        {
            decoderFuncAssignedExModKeys = new HashSet<uint>();
            VKeyComboFromDecKey = new VKeyCombo?[DecoderKeys.GLOBAL_DECKEY_ID_END];
            DecKeyFromVKeyCombo = new Dictionary<uint, int>();
            ModConvertedDecKeyFromVKeyCombo = new Dictionary<uint, int>();
        }

        public static void AddCtrlDeckeyFromCombo(string keyFace, int ctrlDeckey, int ctrlShiftDeckey)
        {
            bool bRemove = false;
            if (keyFace._startsWith("#")) {
                bRemove = true;
                keyFace = keyFace.Replace("#", "");
            }
            var combo = GetVKeyComboFromFaceString(keyFace, false, false);
            if (combo != null) {
                if (bRemove) {
                    if (ctrlDeckey > 0) RemoveModConvertedDecKeyFromCombo(KeyModifiers.MOD_CONTROL, combo.Value.vkey);
                    if (ctrlShiftDeckey > 0) RemoveModConvertedDecKeyFromCombo(KeyModifiers.MOD_CONTROL | KeyModifiers.MOD_SHIFT, combo.Value.vkey);
                } else {
                    if (ctrlDeckey > 0) AddModConvertedDecKeyFromCombo(ctrlDeckey, KeyModifiers.MOD_CONTROL, combo.Value.vkey);
                    if (ctrlShiftDeckey > 0) AddModConvertedDecKeyFromCombo(ctrlShiftDeckey, KeyModifiers.MOD_CONTROL | KeyModifiers.MOD_SHIFT, combo.Value.vkey);
                }
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
        /// 仮想キーコードはDecKeyId順に並んでいる必要がある。<br/>
        /// NAME=xx の形式で、機能キーの仮想キーコード定義を記述できる
        /// </summary>
        /// <returns></returns>
        public static bool ReadKeyboardFile()
        {
            logger.Info("ENTER");

            var array = VKeyArray106;

            var filePath = KanchokuIni.Singleton.KanchokuDir._joinPath(Settings.GetString("keyboard", "106.key"));
            if (filePath._notEmpty()) {
                logger.Info($"keyboard file path={filePath}");
                var allLines = Helper.GetFileContent(filePath, Encoding.UTF8);
                if (allLines == null) {
                    logger.Error($"Can't read keyboard file: {filePath}");
                    SystemHelper.ShowErrorMessageBox($"キーボード定義ファイル({filePath}の読み込みに失敗しました。");
                    return false;
                }
                var lines = allLines._split('\n').Select(line => line.Trim().Replace(" ", "")).Where(line => line._notEmpty() && line[0] != '#' && line[0] != ';').ToArray();

                // ストロークキーの仮想キーコードを得る
                var hexes = lines.  Where(line => line.IndexOf('=') < 0)._join("").TrimEnd(',')._split(',').ToArray();
                array = hexes.Select(x => (uint)x._parseHex(0)).ToArray();
                int idx = array._findIndex(x => x < 0 || x >= 0x100);
                if (idx >= 0 && idx < array.Length) {
                    logger.Warn($"Invalid keyboard def: file={filePath}, {idx}th: {hexes[idx]}");
                    SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}の{idx}番目のキー定義({hexes[idx]})が誤っています。");
                    return false;
                }
                if (array.Length < DecoderKeys.NORMAL_DECKEY_NUM) {
                    logger.Warn($"No sufficient keyboard def: file={filePath}, total {array.Length} defs");
                    SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}のキー定義の数({array.Length})が不足しています。");
                    return false;
                }
                // NAME=xx の形式で、機能キー(Esc, BS, Enter, 矢印キーなど)の仮想キーコード定義を得る
                foreach (var line in lines) {
                    var items = line._toLower()._split('=');
                    if (items._safeLength() == 2 && items[0]._notEmpty() && items[1]._notEmpty()) {
                        int n = -1;
                        int vk = items[1]._parseHex();
                        if (vk >= 0 && vk < 0x100) {
                            n = GetFuncKeyIndexByName(items[0]);
                        }
                        if (n < 0 || n >= vkeyArrayFuncKeys.Length) {
                            logger.Warn($"Invalid functional key def: file={filePath}, line: {line}");
                            SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}の行 {line} が誤っています。");
                            return false;
                        }
                        vkeyArrayFuncKeys[n] = (uint)vk;
                    }
                }

            }
            logger.Info(() => $"keyboard keyNum={array.Length}, array={array.Select(x => x.ToString("x"))._join(", ")}");

            strokeVKeys = array;

            setupDecKeyAndComboTable();

            logger.Info("LEAVE");
            return true;
        }

        private static Dictionary<string, uint> modifierKeysFromName = new Dictionary<string, uint>() {
            {"space", KeyModifiers.MOD_SPACE },
            {"caps", KeyModifiers.MOD_CAPS },
            {"alnum", KeyModifiers.MOD_ALNUM },
            {"nfer", KeyModifiers.MOD_NFER },
            {"xfer", KeyModifiers.MOD_XFER },
            {"kana", KeyModifiers.MOD_SINGLE },
            {"lctrl", KeyModifiers.MOD_LCTRL },
            {"rctrl", KeyModifiers.MOD_RCTRL },
            {"rshift", KeyModifiers.MOD_RSHIFT },
            {"zenkaku", KeyModifiers.MOD_SINGLE },
        };

        /// <summary>(拡張)シフト面に割り当てられる拡張修飾キーか (kana, lctrl, rctrl 以外)</summary>
        /// <param name="mod"></param>
        /// <returns></returns>
        private static bool isPlaneMappedModifier(uint mod)
        {
            return (mod & (KeyModifiers.MOD_SINGLE | KeyModifiers.MOD_LCTRL | KeyModifiers.MOD_RCTRL)) == 0;
        }

        private static Dictionary<string, int> specialDecKeysFromName = new Dictionary<string, int>() {
            {"esc", DecoderKeys.ESC_DECKEY},
            {"escape", DecoderKeys.ESC_DECKEY},
            {"zenkaku", DecoderKeys.HANZEN_DECKEY },
            {"hanzen", DecoderKeys.HANZEN_DECKEY },
            {"tab", DecoderKeys.TAB_DECKEY},
            {"caps", DecoderKeys.CAPS_DECKEY },
            {"capslock", DecoderKeys.CAPS_DECKEY },
            {"alnum", DecoderKeys.ALNUM_DECKEY },
            {"alphanum", DecoderKeys.ALNUM_DECKEY },
            {"eisu", DecoderKeys.ALNUM_DECKEY },
            {"nfer", DecoderKeys.NFER_DECKEY },
            {"xfer", DecoderKeys.XFER_DECKEY },
            {"kana", DecoderKeys.KANA_DECKEY },
            {"hiragana", DecoderKeys.KANA_DECKEY },
            {"bs", DecoderKeys.BS_DECKEY },
            {"back", DecoderKeys.BS_DECKEY },
            {"backspace", DecoderKeys.BS_DECKEY },
            {"enter", DecoderKeys.ENTER_DECKEY},
            {"ins", DecoderKeys.INS_DECKEY},
            {"insert", DecoderKeys.INS_DECKEY},
            {"del", DecoderKeys.DEL_DECKEY},
            {"delete", DecoderKeys.DEL_DECKEY},
            {"home", DecoderKeys.HOME_DECKEY},
            {"end", DecoderKeys.END_DECKEY},
            {"pgup", DecoderKeys.PAGE_UP_DECKEY},
            {"pageup", DecoderKeys.PAGE_UP_DECKEY},
            {"pgdn", DecoderKeys.PAGE_DOWN_DECKEY},
            {"pagedown", DecoderKeys.PAGE_DOWN_DECKEY},
            {"left", DecoderKeys.LEFT_ARROW_DECKEY},
            {"leftarrow", DecoderKeys.LEFT_ARROW_DECKEY},
            {"right", DecoderKeys.RIGHT_ARROW_DECKEY},
            {"rightarrow", DecoderKeys.RIGHT_ARROW_DECKEY},
            {"up", DecoderKeys.UP_ARROW_DECKEY},
            {"uparrow", DecoderKeys.UP_ARROW_DECKEY},
            {"down", DecoderKeys.DOWN_ARROW_DECKEY},
            {"downarrow", DecoderKeys.DOWN_ARROW_DECKEY},
            {"rshift", DecoderKeys.RIGHT_SHIFT_DECKEY},
            {"space", DecoderKeys.STROKE_SPACE_DECKEY},
            {"shiftspace", DecoderKeys.SHIFT_SPACE_DECKEY},
            {"modetoggle", DecoderKeys.TOGGLE_DECKEY},
            {"modetogglefollowcaret", DecoderKeys.MODE_TOGGLE_FOLLOW_CARET_DECKEY},
            {"activate", DecoderKeys.ACTIVE_DECKEY},
            {"deactivate", DecoderKeys.DEACTIVE_DECKEY},
            {"fullescape", DecoderKeys.FULL_ESCAPE_DECKEY},
            {"unblock", DecoderKeys.UNBLOCK_DECKEY},
            {"helprotate", DecoderKeys.STROKE_HELP_ROTATION_DECKEY},
            {"helpunrotate", DecoderKeys.STROKE_HELP_UNROTATION_DECKEY},
            {"daterotate", DecoderKeys.DATE_STRING_ROTATION_DECKEY},
            {"dateunrotate", DecoderKeys.DATE_STRING_UNROTATION_DECKEY},
            {"histnext", DecoderKeys.HISTORY_NEXT_SEARCH_DECKEY},
            {"histprev", DecoderKeys.HISTORY_PREV_SEARCH_DECKEY},
            {"bushucomphelp", DecoderKeys.BUSHU_COMP_HELP},
            {"zenkakuconvert", DecoderKeys.TOGGLE_ZENKAKU_CONVERSION},
            {"zenkakuconversion", DecoderKeys.TOGGLE_ZENKAKU_CONVERSION},
            {"romanstrokeguide", DecoderKeys.TOGGLE_ROMAN_STROKE_GUIDE},
            {"upperromanstrokeguide", DecoderKeys.TOGGLE_UPPER_ROMAN_STROKE_GUIDE},
            {"hiraganastrokeguide", DecoderKeys.TOGGLE_HIRAGANA_STROKE_GUIDE},
            {"exchangecodetable", DecoderKeys.EXCHANGE_CODE_TABLE_DECKEY},
            {"leftshiftblocker", DecoderKeys.LEFT_SHIFT_BLOCKER_DECKEY},
            {"rightshiftblocker", DecoderKeys.RIGHT_SHIFT_BLOCKER_DECKEY},
            {"leftshiftmazestartpos", DecoderKeys.LEFT_SHIFT_MAZE_START_POS_DECKEY},
            {"rightshiftmazestartpos", DecoderKeys.RIGHT_SHIFT_MAZE_START_POS_DECKEY},
            //{"^a", DecoderKeys.CTRL_},
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
                shiftPlaneForShiftModFlag.Clear();
                shiftPlaneForShiftModFlagWhenDecoderOff.Clear();
                int nl = 0;
                foreach (var rawLine in lines._split('\n')) {
                    ++nl;
                    var line = rawLine._reReplace("#.*", "").Trim().Replace(" ", "")._toLower();
                    if (line._notEmpty() && line[0] != '#') {
                        if (line.IndexOf('=') > 0) {
                            // NAME=plane[|plane]...
                            var items = line._split('=');
                            if (items._length() == 2) {
                                uint mod = modifierKeysFromName._safeGet(items[0]);
                                var planes = items[1]._split('|');
                                ShiftPlane shiftPlane = ShiftPlane.NONE;
                                switch (planes._getNth(0)) {
                                    case "shift": shiftPlane = ShiftPlane.NormalPlane; break;
                                    case "shifta": shiftPlane = ShiftPlane.PlaneA; break;
                                    case "shiftb": shiftPlane = ShiftPlane.PlaneB; break;
                                }
                                var shiftPlaneWhenOff = shiftPlane;
                                if (planes.Length > 1) {
                                    switch (planes._getNth(1)) {
                                        case "shift": shiftPlaneWhenOff = ShiftPlane.NormalPlane; break;
                                        case "shifta": shiftPlaneWhenOff = ShiftPlane.PlaneA; break;
                                        case "shiftb": shiftPlaneWhenOff = ShiftPlane.PlaneB; break;
                                        default: shiftPlaneWhenOff = ShiftPlane.NONE; break;
                                    }
                                }
                                logger.DebugH(() => $"mod={mod:x}H({mod}), shiftPlane={shiftPlane}, shiftPlaneWhenOff={shiftPlaneWhenOff}");
                                if (mod != 0 && shiftPlane > 0) {
                                    logger.DebugH(() => $"shiftPlaneForShiftFuncKey[{mod}] = {shiftPlane}, shiftPlaneForShiftFuncKeyWhenDecoderOff[{mod}] = {shiftPlaneWhenOff}");
                                    shiftPlaneForShiftModFlag[mod] = shiftPlane;
                                    shiftPlaneForShiftModFlagWhenDecoderOff[mod] = shiftPlaneWhenOff;
                                    continue;
                                }
                            }
                        } else {
                            // NAME:xx:function
                            var items = line._split(':');
                            if (items._length() == 3) {
                                uint mod = modifierKeysFromName._safeGet(items[0]);
                                uint vkey = getVKeyFromDecKey(items[1]._parseInt(-1, -1));
                                if (mod != 0 && vkey == 0) {
                                    // 拡張修飾キー単打の場合
                                    mod = 0;
                                    vkey = GetFuncVkeyByName(items[0]);  // 被修飾キーが指定されていない場合は、修飾キーの単打とみなす
                                }
                                bool ctrl = items[2]._startsWith("^");
                                var name = items[2].Replace("^", "");
                                //int deckey = items[2]._parseInt(-1, -1)._geZeroOr(() => specialDecKeysFromName._safeGet(name)); // 数字による直接定義もOK ⇒ と思ったが、mod の拡張面で定義すればよいだけ
                                int deckey = specialDecKeysFromName._safeGet(name);
                                logger.DebugH(() => $"mod={mod:x}H, vkey={vkey:x}H, deckey={deckey:x}H({deckey}), ctrl={ctrl}, name={name}, rawLine={rawLine}");
                                if (ctrl) {
                                    uint decVkey = 0;
                                    if (name._safeLength() == 1 && name._ge("a") && name._le("z")) {
                                        decVkey = faceToVkey._safeGet(name._toUpper());
                                        deckey = DecoderKeys.DECKEY_CTRL_A + name[0] - 'a';
                                    } else if (deckey >= DecoderKeys.FUNC_DECKEY_START && deckey < DecoderKeys.FUNC_DECKEY_END) {
                                        decVkey = getVKeyFromDecKey(deckey);
                                        deckey += DecoderKeys.CTRL_FUNC_DECKEY_START - DecoderKeys.FUNC_DECKEY_START;
                                    }
                                    logger.DebugH(() => $"deckey={deckey:x}H({deckey}), ctrl={ctrl}, decVkey={decVkey:x}H({decVkey})");
                                    if (deckey > 0) AddModifiedDeckey(deckey, KeyModifiers.MOD_CONTROL, decVkey);
                                }
                                if (vkey > 0 && deckey > 0) {
                                    logger.DebugH(() => $"AddModConvertedDecKeyFromCombo: deckey={deckey}, mod={mod}, vkey={vkey}");
                                    if (mod == 0) {
                                        // 拡張修飾キー単打の場合は、キーの登録だけで、拡張シフトB面の割り当てはやらない
                                        AddDecKeyAndCombo(deckey, 0, vkey);
                                    } else {
                                        AddModConvertedDecKeyFromCombo(deckey, mod, vkey);
                                        if (isPlaneMappedModifier(mod) && !shiftPlaneForShiftModFlag.ContainsKey(mod)) {
                                            // mod に対する ShiftPlane が設定されていない場合は、拡張シフトB面を割り当てる
                                            shiftPlaneForShiftModFlag[mod] = ShiftPlane.PlaneB;
                                            shiftPlaneForShiftModFlagWhenDecoderOff[mod] = ShiftPlane.PlaneB;
                                        }
                                    }
                                    continue;
                                }
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

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

        // VKEY に対する modifier LSHIFT
        public const uint MOD_LSHIFT = 0x20000;

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

    /// <summary>
    /// シフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) に割り当てるシフト面
    /// </summary>
    public class ShiftPlaneMapper
    {
        Dictionary<uint, int> shiftPlaneMap = new Dictionary<uint, int>();

        public void Clear()
        {
            shiftPlaneMap.Clear();
        }

        public void Add(uint key, int plane)
        {
            shiftPlaneMap[key] = plane;
        }

        public bool ContainsKey(uint key)
        {
            return shiftPlaneMap.ContainsKey(key);
        }

        public int GetPlane(uint key)
        {
            return shiftPlaneMap._safeGet(key, 0);
        }

        public bool FindPlane(int plane)
        {
            return shiftPlaneMap.Values.Any(x => x == plane);
        }

        public List<KeyValuePair<uint, int>> GetPairs()
        {
            return shiftPlaneMap.ToList();
        }
    }

    public static class VirtualKeys
    {
        private static Logger logger = Logger.GetLogger(true);

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
            /*Ins*/ 0x2d, /*Del*/ 0x2e, /*Home*/ 0x24, /*End*/ 0x23, /*PgUp*/ 0x21, /*PgDn*/ 0x22, /*↑*/ 0x26, /*↓*/ 0x28, /*←*/ 0x25, /*→*/ 0x27,
            /*Rshift*/ RSHIFT, /*ScrLock*/ 0x91, /*Pause*/ 0x13, /*IME ON*/ 0x16, /*IME OFF*/ 0x1a,
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
                case "scrlock": n = 21; break;
                case "pause": n = 22; break;
                case "imeon": n = 23; break;
                case "imeoff": n = 24; break;
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
            if (name._toLower().StartsWith("vk")) {
                // "VKxx" のケース
                int vk = name._safeSubstring(2)._parseHex();
                if (vk > 0 && vk < 0xff) return (uint)vk;
            }
            return vkeyArrayFuncKeys._getNth(GetFuncKeyIndexByName(name));
        }

        public static uint GetAlphabetVkeyByName(string name)
        {
            return (name._safeLength() == 1 && name[0] >= 'A' && name[0] <= 'Z') ? (uint)Keys.A + (uint)(name[0] - 'A') : 0;
        }

        public const int ShiftPlane_NONE = 0;
        public const int ShiftPlane_SHIFT = 1;
        public const int ShiftPlane_A = 2;
        public const int ShiftPlane_B = 3;
        public const int ShiftPlane_C = 4;
        public const int ShiftPlane_D = 5;
        public const int ShiftPlane_E = 6;
        public const int ShiftPlane_F = 7;
        public const int ShiftPlane_NUM = 8;

        public static string GetShiftPlaneName(int plane)
        {
            switch (plane) {
                case 1: return "Shift";
                case 2: return "ShiftA";
                case 3: return "ShiftB";
                case 4: return "ShiftC";
                case 5: return "ShiftD";
                case 6: return "ShiftE";
                case 7: return "ShiftF";
                default: return "none";
            }
        }

        public static string GetShiftPlanePrefix(int plane)
        {
            switch (plane) {
                case 1: return "S";
                case 2: return "A";
                case 3: return "B";
                case 4: return "C";
                case 5: return "D";
                case 6: return "E";
                case 7: return "F";
                default: return "";
            }
        }

        /// <summary> デコーダ機能に割り当てられた拡張修飾キー(space, Caps, alnum, nfer, xfer, Rshift)のVkeyを集めた集合 </summary>
        private static HashSet<uint> decoderFuncAssignedExModKeys = new HashSet<uint>();

        /// <summary> 拡張修飾キー(space, Caps, alnum, nfer, xfer, Rshift)をデコーダ機能に割り当てられたキーの集合に追加 </summary>
        public static void AddExModVkeyAssignedForDecoderFuncByVkey(uint vkey)
        {
            decoderFuncAssignedExModKeys.Add(vkey);
        }

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

        /// <summary> 漢直モードのトグルをやるキーか </summary>
        public static int GetKanchokuToggleDecKey(uint mod, uint vkey)
        {
            int kanchokuCodeWithMod = GetModConvertedDecKeyFromCombo(mod, vkey);
            return (kanchokuCodeWithMod >= DecoderKeys.TOGGLE_DECKEY && kanchokuCodeWithMod <= DecoderKeys.DEACTIVE2_DECKEY) ? kanchokuCodeWithMod : -1;
        }

        /// <summary>無効化された拡張修飾キー</summary>
        private static HashSet<string> disabledExtKeys;

        private static HashSet<string> disabledExtKeyLines = new HashSet<string>();

        public static void AddDisabledExtKey(string name)
        {
            disabledExtKeys.Add(name._toLower());
        }

        public static bool IsDisabledExtKey(string name)
        {
            return disabledExtKeys.Contains(name._toLower());
        }

        /// <summary> シフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) に割り当てるシフト面</summary>
        public static ShiftPlaneMapper ShiftPlaneForShiftModKey { get; private set; } = new ShiftPlaneMapper();

        /// <summary> DecoderがOffの時のシフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) に割り当てるシフト面</summary>
        public static ShiftPlaneMapper ShiftPlaneForShiftModKeyWhenDecoderOff { get; private set; } = new ShiftPlaneMapper();

        private static void initializeShiftPlaneForShiftModKey()
        {
            ShiftPlaneForShiftModKey.Clear();
            ShiftPlaneForShiftModKeyWhenDecoderOff.Clear();

            // SHIFTなら標準シフト面をデフォルトとしておく
            ShiftPlaneForShiftModKey.Add(KeyModifiers.MOD_SHIFT, ShiftPlane_SHIFT);
            ShiftPlaneForShiftModKeyWhenDecoderOff.Add(KeyModifiers.MOD_SHIFT, ShiftPlane_SHIFT);
        }

        /// <summary> シフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) に割り当てられたシフト面を得る</summary>
        public static int GetShiftPlaneFromShiftModFlag(uint modFlag, bool bDecoderOn)
        {
            return bDecoderOn ? ShiftPlaneForShiftModKey.GetPlane(modFlag) : ShiftPlaneForShiftModKeyWhenDecoderOff.GetPlane(modFlag);
        }

        /// <summary> シフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) にシフト面が割り当てられているか</summary>
        public static bool IsShiftPlaneAssignedForShiftModFlag(uint modFlag, bool bDecoderOn)
        {
            return GetShiftPlaneFromShiftModFlag(modFlag, bDecoderOn) != ShiftPlane_NONE;
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
            {"0", (uint)Keys.D0 },
            {"1", (uint)Keys.D1 },
            {"2", (uint)Keys.D2 },
            {"3", (uint)Keys.D3 },
            {"4", (uint)Keys.D4 },
            {"5", (uint)Keys.D5 },
            {"6", (uint)Keys.D6 },
            {"7", (uint)Keys.D7 },
            {"8", (uint)Keys.D8 },
            {"9", (uint)Keys.D9 },
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
            { ":", (uint)Keys.Oem1 },       // ba
            { "*", (uint)Keys.Oem1 + 0x100 },       // ba
            { "PLUS", (uint)Keys.Oemplus },     // bb
            { ";", (uint)Keys.Oemplus },     // bb
            { "+", (uint)Keys.Oemplus + 0x100 },     // bb
            { "COMMA", (uint)Keys.Oemcomma },   // bc
            { ",", (uint)Keys.Oemcomma },   // bc
            { "<", (uint)Keys.Oemcomma + 0x100 },   // bc
            { "MINUS", (uint)Keys.OemMinus },   // bd
            { "-", (uint)Keys.OemMinus },   // bd
            { "=", (uint)Keys.OemMinus + 0x100 },   // bd
            { "PERIOD", (uint)Keys.OemPeriod }, // be
            { ".", (uint)Keys.OemPeriod }, // be
            { ">", (uint)Keys.OemPeriod + 0x100 }, // be
            { "SLASH", (uint)Keys.Oem2 },       // bf
            { "/", (uint)Keys.Oem2 },       // bf
            { "?", (uint)Keys.Oem2 + 0x100 },       // bf
            { "BQUOTE", (uint)Keys.Oem3 },      // c0/106
            { "@", (uint)Keys.Oem3 },      // c0/106
            { "`", (uint)Keys.Oem3 + 0x100 },      // c0/106
            { "OEM4", (uint)Keys.Oem4 },        // db
            { "[", (uint)Keys.Oem4 },        // db
            { "{", (uint)Keys.Oem4 + 0x100 },        // db
            { "OEM5", (uint)Keys.Oem5 },        // dc
            { "\\", (uint)Keys.Oem5 },        // dc
            { "|", (uint)Keys.Oem5 + 0x100 },        // dc
            { "OEM6", (uint)Keys.Oem6 },        // dd
            { "]", (uint)Keys.Oem6 },        // dd
            { "}", (uint)Keys.Oem6 + 0x100 },        // dd
            { "OEM7", (uint)Keys.Oem7 },        // de
            { "^", (uint)Keys.Oem7 },        // de
            { "~", (uint)Keys.Oem7 + 0x100 },        // de
            { "OEM8", (uint)Keys.Oem8 },        // df
            { "OEM102", (uint)Keys.Oem102 },    // e2/106
            { "＼", (uint)Keys.Oem102 },    // e2/106
            { "_", (uint)Keys.Oem102 + 0x100 },        // de
        };

        private static Dictionary<char, uint> charToVkey = new Dictionary<char, uint>() {
            {' ', (uint)Keys.Space },
            {'1', (uint)Keys.D1 },
            {'2', (uint)Keys.D2 },
            {'3', (uint)Keys.D3 },
            {'4', (uint)Keys.D4 },
            {'5', (uint)Keys.D5 },
            {'6', (uint)Keys.D6 },
            {'7', (uint)Keys.D7 },
            {'8', (uint)Keys.D8 },
            {'9', (uint)Keys.D9 },
            {'0', (uint)Keys.D0 },
            {'を', (uint)Keys.D0 + 0x100 },
            {'!', (uint)Keys.D1 + 0x100 },
            {'\"', (uint)Keys.D2 + 0x100 },
            {'#', (uint)Keys.D3 + 0x100 },
            {'$', (uint)Keys.D4 + 0x100 },
            {'%', (uint)Keys.D5 + 0x100 },
            {'&', (uint)Keys.D6 + 0x100 },
            {'\'', (uint)Keys.D7 + 0x100 },
            {'(', (uint)Keys.D8 + 0x100 },
            {')', (uint)Keys.D9 + 0x100 },
            {'A', (uint)Keys.A + 0x100 },
            {'a', (uint)Keys.A },
            {'B', (uint)Keys.B + 0x100 },
            {'b', (uint)Keys.B },
            {'C', (uint)Keys.C + 0x100 },
            {'c', (uint)Keys.C },
            {'D', (uint)Keys.D + 0x100 },
            {'d', (uint)Keys.D },
            {'E', (uint)Keys.E + 0x100 },
            {'e', (uint)Keys.E },
            {'ぃ', (uint)Keys.E + 0x100 },
            {'F', (uint)Keys.F + 0x100 },
            {'f', (uint)Keys.F },
            {'G', (uint)Keys.G + 0x100 },
            {'g', (uint)Keys.G },
            {'H', (uint)Keys.H + 0x100 },
            {'h', (uint)Keys.H },
            {'I', (uint)Keys.I + 0x100 },
            {'i', (uint)Keys.I },
            {'J', (uint)Keys.J + 0x100 },
            {'j', (uint)Keys.J },
            {'K', (uint)Keys.K + 0x100 },
            {'k', (uint)Keys.K },
            {'L', (uint)Keys.L + 0x100 },
            {'l', (uint)Keys.L },
            {'M', (uint)Keys.M + 0x100 },
            {'m', (uint)Keys.M },
            {'N', (uint)Keys.N + 0x100 },
            {'n', (uint)Keys.N },
            {'O', (uint)Keys.O + 0x100 },
            {'o', (uint)Keys.O },
            {'P', (uint)Keys.P + 0x100 },
            {'p', (uint)Keys.P },
            {'Q', (uint)Keys.Q + 0x100 },
            {'q', (uint)Keys.Q },
            {'R', (uint)Keys.R + 0x100 },
            {'r', (uint)Keys.R },
            {'S', (uint)Keys.S + 0x100 },
            {'s', (uint)Keys.S },
            {'T', (uint)Keys.T + 0x100 },
            {'t', (uint)Keys.T },
            {'U', (uint)Keys.U + 0x100 },
            {'u', (uint)Keys.U },
            {'V', (uint)Keys.V + 0x100 },
            {'v', (uint)Keys.V },
            {'W', (uint)Keys.W + 0x100 },
            {'w', (uint)Keys.W },
            {'X', (uint)Keys.X + 0x100 },
            {'x', (uint)Keys.X },
            {'Y', (uint)Keys.Y + 0x100 },
            {'y', (uint)Keys.Y },
            {'Z', (uint)Keys.Z + 0x100 },
            {'z', (uint)Keys.Z },
            {'っ', (uint)Keys.Z + 0x100 },
            { ';', (uint)Keys.Oemplus },     // bb
            { '+', (uint)Keys.Oemplus + 0x100 },     // bb
            { ',', (uint)Keys.Oemcomma },   // bc
            { '<', (uint)Keys.Oemcomma + 0x100 },   // bc
            { '.', (uint)Keys.OemPeriod }, // be
            { '>', (uint)Keys.OemPeriod + 0x100 }, // be
            { '-', (uint)Keys.OemMinus },   // bd
            { '=', (uint)Keys.OemMinus + 0x100 },   // bd
            { ':', (uint)Keys.Oem1 },       // ba
            { '*', (uint)Keys.Oem1 + 0x100 },       // ba
            { '/', (uint)Keys.Oem2 },       // bf
            { '?', (uint)Keys.Oem2 + 0x100 },       // bf
            { '@', (uint)Keys.Oem3 },      // c0/106
            { '`', (uint)Keys.Oem3 + 0x100 },      // c0/106
            { '[', (uint)Keys.Oem4 },        // db
            { '{', (uint)Keys.Oem4 + 0x100 },        // db
            { '\\', (uint)Keys.Oem5 },        // dc
            { '|', (uint)Keys.Oem5 + 0x100 },        // dc
            { ']', (uint)Keys.Oem6 },        // dd
            { '}', (uint)Keys.Oem6 + 0x100 },        // dd
            { '^', (uint)Keys.Oem7 },        // de
            { '~', (uint)Keys.Oem7 + 0x100 },        // de
            { '＼', (uint)Keys.Oem102 },        // e1
            { '_', (uint)Keys.Oem102 + 0x100 },        // e1
        };

        private static Dictionary<uint, char> vkeyToChar = new Dictionary<uint, char>() {
            { (uint)Keys.Space,' ' },
            { (uint)Keys.D1,'1' },
            { (uint)Keys.D2,'2' },
            { (uint)Keys.D3,'3' },
            { (uint)Keys.D4,'4' },
            { (uint)Keys.D5,'5' },
            { (uint)Keys.D6,'6' },
            { (uint)Keys.D7,'7' },
            { (uint)Keys.D8,'8' },
            { (uint)Keys.D9,'9' },
            { (uint)Keys.D0,'0' },
            { (uint)Keys.A,'a' },
            { (uint)Keys.B,'b' },
            { (uint)Keys.C,'c' },
            { (uint)Keys.D,'d' },
            { (uint)Keys.E,'e' },
            { (uint)Keys.F,'f' },
            { (uint)Keys.G,'g' },
            { (uint)Keys.H,'h' },
            { (uint)Keys.I,'i' },
            { (uint)Keys.J,'j' },
            { (uint)Keys.K,'k' },
            { (uint)Keys.L,'l' },
            { (uint)Keys.M,'m' },
            { (uint)Keys.N,'n' },
            { (uint)Keys.O,'o' },
            { (uint)Keys.P,'p' },
            { (uint)Keys.Q,'q' },
            { (uint)Keys.R,'r' },
            { (uint)Keys.S,'s' },
            { (uint)Keys.T,'t' },
            { (uint)Keys.U,'u' },
            { (uint)Keys.V,'v' },
            { (uint)Keys.W,'w' },
            { (uint)Keys.X,'x' },
            { (uint)Keys.Y,'y' },
            { (uint)Keys.Z,'z' },
            { (uint)Keys.Oemplus, ';' },     // bb
            { (uint)Keys.Oemcomma, ',' },   // bc
            { (uint)Keys.OemPeriod, '.' }, // be
            { (uint)Keys.OemMinus, '-' },   // bd
            { (uint)Keys.Oem1, ':' },       // ba
            { (uint)Keys.Oem2, '/' },       // bf
            { (uint)Keys.Oem3, '@' },      // c0/106
            { (uint)Keys.Oem4, '[' },        // db
            { (uint)Keys.Oem5, '\\' },        // dc
            { (uint)Keys.Oem6, ']' },        // dd
            { (uint)Keys.Oem7, '^' },        // de
            { (uint)Keys.Oem102, '＼' },        // e1
            { (uint)Keys.IMEConvert, '変' },        // 1c
            { (uint)Keys.IMENonconvert, '無' },        // 1d
        };

        public static VKeyCombo EmptyCombo = new VKeyCombo(0, 0);

        public static VKeyCombo CtrlC_VKeyCombo = new VKeyCombo(KeyModifiers.MOD_CONTROL, faceToVkey["C"]);

        public static VKeyCombo CtrlV_VKeyCombo = new VKeyCombo(KeyModifiers.MOD_CONTROL, faceToVkey["V"]);

        public static uint GetVKeyFromFaceString(string face)
        {
            return faceToVkey._safeGet(face);
        }

        public static uint GetVKeyFromFaceChar(char face)
        {
            return charToVkey._safeGet(face);
        }

        public static VKeyCombo? GetVKeyComboFromFaceString(string face, bool ctrl, bool shift)
        {
            uint vkey = faceToVkey._safeGet(face);
            if (vkey > 0 && vkey < 0x100) {
                return new VKeyCombo(KeyModifiers.MakeModifier(ctrl, shift), vkey);
            }
            return null;
        }

        public static char GetFaceCharFromVKey(uint vkey)
        {
            return vkeyToChar._safeGet(vkey);
        }

        public static char GetFaceCharFromDecKey(int decKey)
        {
            return GetFaceCharFromVKey(GetVKeyComboFromDecKey(decKey)?.vkey ?? 0);
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

        /// <summary>
        /// xfer, nfer など特殊キーに割り当てられている DecoderKey を登録
        /// </summary>
        /// <param name="deckey"></param>
        public static void AddSpecialDeckey(string name, int deckey)
        {
            if (deckey > 0) {
                uint vk = GetFuncVkeyByName(name);
                if (vk > 0) {
                    VKeyComboFromDecKey[deckey] = new VKeyCombo(0, vk);
                }
            }
        }

        public static void AddModifiedDeckey(int deckey, uint mod, uint vkey)
        {
            logger.DebugH(() => $"deckey={deckey:x}H({deckey}), mod={mod:x}H, vkey={vkey:x}H({vkey})");
            var combo = new VKeyCombo(mod, vkey);
            VKeyComboFromDecKey[deckey] = combo;
        }

        public static void AddDecKeyAndCombo(int deckey, uint mod, uint vkey, bool bFromComboOnly = false)
        {
            logger.Debug(() => $"deckey={deckey:x}H({deckey}), mod={mod:x}H, vkey={vkey:x}H({vkey})");
            var combo = new VKeyCombo(mod, (uint)vkey);
            if (!bFromComboOnly) VKeyComboFromDecKey[deckey] = combo;
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
            // 通常文字ストロークキー
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

            // Shift+Tab
            AddDecKeyAndCombo(DecoderKeys.SHIFT_TAB_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.Tab);
            //AddModConvertedDecKeyFromCombo(DecoderKeys.SHIFT_TAB_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.Tab);
        }

        public static int GetDecKeyFromCombo(uint mod, uint vkey)
        {
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"CALLED: mod={mod:x}H({mod}), vkey={vkey:x}H({vkey})");
            return DecKeyFromVKeyCombo._safeGet(VKeyCombo.CalcSerialValue(mod, vkey), -1);
        }

        public static int GetModConvertedDecKeyFromCombo(uint mod, uint vkey)
        {
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"CALLED: mod={mod:x}H({mod}), vkey={vkey:x}H({vkey})");
            int deckey = ModConvertedDecKeyFromVKeyCombo._safeGet(VKeyCombo.CalcSerialValue(mod, vkey), -1);
            if (deckey <= 0) { deckey = GetDecKeyFromCombo(mod, vkey); }
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"deckey={deckey:x}H({deckey})");
            return deckey;
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
            disabledExtKeys = new HashSet<string>();
            disabledExtKeyLines.Clear();
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
            {"shift", KeyModifiers.MOD_SHIFT },
            {"rshift", KeyModifiers.MOD_RSHIFT },
            {"zenkaku", KeyModifiers.MOD_SINGLE },
        };

        public static uint GetModifierKeyByName(string name)
        {
            return modifierKeysFromName._safeGet(name);
        }

        public static string GetModifierNameByKey(uint modKey)
        {
            foreach (var pair in modifierKeysFromName) {
                if (pair.Value == modKey) return pair.Key;
            }
            return null;
        }

        /// <summary>(拡張)シフト面に割り当てられる拡張修飾キーか</summary>
        /// <param name="mod"></param>
        /// <returns></returns>
        private static bool isPlaneMappedModifier(uint mod)
        {
            //return (mod & (KeyModifiers.MOD_SINGLE | KeyModifiers.MOD_LCTRL | KeyModifiers.MOD_RCTRL)) == 0;
            return SpecialKeysAndFunctions.IsPlaneAssignableModKey(mod);
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
            {"scrlock", DecoderKeys.SCR_LOCK_DECKEY},
            {"pause", DecoderKeys.PAUSE_DECKEY},
            {"imeon", DecoderKeys.IME_ON_DECKEY},
            {"imeoff", DecoderKeys.IME_OFF_DECKEY},
            {"space", DecoderKeys.STROKE_SPACE_DECKEY},
            {"shiftspace", DecoderKeys.SHIFT_SPACE_DECKEY},
            {"directspace", DecoderKeys.DIRECT_SPACE_DECKEY},
            {"modetoggle", DecoderKeys.TOGGLE_DECKEY},
            {"modetogglefollowcaret", DecoderKeys.MODE_TOGGLE_FOLLOW_CARET_DECKEY},
            {"activate", DecoderKeys.ACTIVE_DECKEY},
            {"deactivate", DecoderKeys.DEACTIVE_DECKEY},
            {"fullescape", DecoderKeys.FULL_ESCAPE_DECKEY},
            {"unblock", DecoderKeys.UNBLOCK_DECKEY},
            {"toggleblocker", DecoderKeys.TOGGLE_BLOCKER_DECKEY},
            {"blockertoggle", DecoderKeys.TOGGLE_BLOCKER_DECKEY},
            {"helprotate", DecoderKeys.STROKE_HELP_ROTATION_DECKEY},
            {"helpunrotate", DecoderKeys.STROKE_HELP_UNROTATION_DECKEY},
            {"daterotate", DecoderKeys.DATE_STRING_ROTATION_DECKEY},
            {"dateunrotate", DecoderKeys.DATE_STRING_UNROTATION_DECKEY},
            {"histnext", DecoderKeys.HISTORY_NEXT_SEARCH_DECKEY},
            {"histprev", DecoderKeys.HISTORY_PREV_SEARCH_DECKEY},
            {"strokehelp", DecoderKeys.STROKE_HELP_DECKEY},
            {"bushucomphelp", DecoderKeys.BUSHU_COMP_HELP_DECKEY},
            {"zenkakuconvert", DecoderKeys.TOGGLE_ZENKAKU_CONVERSION_DECKEY},
            {"zenkakuconversion", DecoderKeys.TOGGLE_ZENKAKU_CONVERSION_DECKEY},
            {"katakanaconvert", DecoderKeys.TOGGLE_KATAKANA_CONVERSION_DECKEY},
            {"katakanaconversion", DecoderKeys.TOGGLE_KATAKANA_CONVERSION_DECKEY},
            {"romanstrokeguide", DecoderKeys.TOGGLE_ROMAN_STROKE_GUIDE_DECKEY},
            {"upperromanstrokeguide", DecoderKeys.TOGGLE_UPPER_ROMAN_STROKE_GUIDE_DECKEY},
            {"hiraganastrokeguide", DecoderKeys.TOGGLE_HIRAGANA_STROKE_GUIDE_DECKEY},
            {"exchangecodetable", DecoderKeys.EXCHANGE_CODE_TABLE_DECKEY},
            {"kanatrainingtoggle", DecoderKeys.KANA_TRAINING_TOGGLE_DECKEY},
            {"leftshiftblocker", DecoderKeys.LEFT_SHIFT_BLOCKER_DECKEY},
            {"rightshiftblocker", DecoderKeys.RIGHT_SHIFT_BLOCKER_DECKEY},
            {"leftshiftmazestartpos", DecoderKeys.LEFT_SHIFT_MAZE_START_POS_DECKEY},
            {"rightshiftmazestartpos", DecoderKeys.RIGHT_SHIFT_MAZE_START_POS_DECKEY},
            {"copyandregisterselection", DecoderKeys.COPY_SELECTION_AND_SEND_TO_DICTIONARY_DECKEY},
            {"copyselectionandsendtodictionary", DecoderKeys.COPY_SELECTION_AND_SEND_TO_DICTIONARY_DECKEY},
            {"clearstroke", DecoderKeys.CLEAR_STROKE_DECKEY},
            //{"^a", DecoderKeys.CTRL_},
        };

        private static Dictionary<int, string> keyNamesFromDecKey = new Dictionary<int, string>() {
            {DecoderKeys.ESC_DECKEY, "Esc"},
            {DecoderKeys.HANZEN_DECKEY , "Zenkaku"},
            {DecoderKeys.TAB_DECKEY, "Tab"},
            {DecoderKeys.CAPS_DECKEY , "CapsLock"},
            {DecoderKeys.ALNUM_DECKEY , "AlphaNum"},
            {DecoderKeys.NFER_DECKEY , "Nfer"},
            {DecoderKeys.XFER_DECKEY , "Xfer"},
            {DecoderKeys.KANA_DECKEY , "Kana"},
            {DecoderKeys.BS_DECKEY , "BackSpace"},
            {DecoderKeys.ENTER_DECKEY, "Enter"},
            {DecoderKeys.INS_DECKEY, "Insert"},
            {DecoderKeys.DEL_DECKEY, "Delete"},
            {DecoderKeys.HOME_DECKEY, "Home"},
            {DecoderKeys.END_DECKEY, "End"},
            {DecoderKeys.PAGE_UP_DECKEY, "PageUp"},
            {DecoderKeys.PAGE_DOWN_DECKEY, "PageDown"},
            {DecoderKeys.LEFT_ARROW_DECKEY, "Left"},
            {DecoderKeys.RIGHT_ARROW_DECKEY, "Right"},
            {DecoderKeys.UP_ARROW_DECKEY, "Up"},
            {DecoderKeys.DOWN_ARROW_DECKEY, "Down"},
            {DecoderKeys.SCR_LOCK_DECKEY, "ScrLock"},
            {DecoderKeys.PAUSE_DECKEY, "Pause"},
            {DecoderKeys.IME_ON_DECKEY, "ImeOn"},
            {DecoderKeys.IME_OFF_DECKEY, "ImeOff"},
            {DecoderKeys.RIGHT_SHIFT_DECKEY, "Rshift"},
            {DecoderKeys.STROKE_SPACE_DECKEY, "Space"},
            {DecoderKeys.SHIFT_SPACE_DECKEY, "ShiftSpace"},
            {DecoderKeys.TOGGLE_DECKEY, "ModeToggle"},
            {DecoderKeys.MODE_TOGGLE_FOLLOW_CARET_DECKEY, "ModeToggleFollowCaret"},
            {DecoderKeys.ACTIVE_DECKEY, "Activate"},
            {DecoderKeys.DEACTIVE_DECKEY, "Deactivate"},
            {DecoderKeys.FULL_ESCAPE_DECKEY, "FullEscape"},
            {DecoderKeys.UNBLOCK_DECKEY, "Unblock"},
            {DecoderKeys.TOGGLE_BLOCKER_DECKEY, "BlockerToggle"},
            {DecoderKeys.STROKE_HELP_ROTATION_DECKEY, "HelpRotate"},
            {DecoderKeys.STROKE_HELP_UNROTATION_DECKEY, "HelpUnrotate"},
            {DecoderKeys.DATE_STRING_ROTATION_DECKEY, "DateRotate"},
            {DecoderKeys.DATE_STRING_UNROTATION_DECKEY, "DateUnrotate"},
            {DecoderKeys.HISTORY_NEXT_SEARCH_DECKEY, "HistNext"},
            {DecoderKeys.HISTORY_PREV_SEARCH_DECKEY, "HistPrev"},
            {DecoderKeys.STROKE_HELP_DECKEY, "StrokeHelp"},
            {DecoderKeys.BUSHU_COMP_HELP_DECKEY, "BushuCompHelp"},
            {DecoderKeys.TOGGLE_ZENKAKU_CONVERSION_DECKEY, "ZenkakuConversion"},
            {DecoderKeys.TOGGLE_KATAKANA_CONVERSION_DECKEY, "KatakanaConversion"},
            {DecoderKeys.TOGGLE_ROMAN_STROKE_GUIDE_DECKEY, "RomanStrokeGuide"},
            {DecoderKeys.TOGGLE_UPPER_ROMAN_STROKE_GUIDE_DECKEY, "UpperRomanStrokeGuide"},
            {DecoderKeys.TOGGLE_HIRAGANA_STROKE_GUIDE_DECKEY, "HiraganaStrokeGuide"},
            {DecoderKeys.EXCHANGE_CODE_TABLE_DECKEY, "ExchangeCodeTable"},
            {DecoderKeys.KANA_TRAINING_TOGGLE_DECKEY, "KanaTrainingToggle"},
            {DecoderKeys.LEFT_SHIFT_BLOCKER_DECKEY, "LeftShiftBlocker"},
            {DecoderKeys.RIGHT_SHIFT_BLOCKER_DECKEY, "RightShiftBlocker"},
            {DecoderKeys.LEFT_SHIFT_MAZE_START_POS_DECKEY, "LeftShiftMazeStartpos"},
            {DecoderKeys.RIGHT_SHIFT_MAZE_START_POS_DECKEY, "RightShiftMazeStartpos"},
            {DecoderKeys.COPY_SELECTION_AND_SEND_TO_DICTIONARY_DECKEY, "CopyAndRegisterSelection"},
            {DecoderKeys.CLEAR_STROKE_DECKEY, "ClearStroke"},
        };

        public static string GetKeyNameByDeckey(int deckey)
        {
            return keyNamesFromDecKey._safeGet(deckey);
        }

        public static int CalcShiftOffset(char shiftChar)
        {
            return shiftChar == 'S' || shiftChar == 's' ? DecoderKeys.SHIFT_DECKEY_START
                : shiftChar >= 'A' && shiftChar <= 'F' ? DecoderKeys.SHIFT_A_DECKEY_START + (shiftChar - 'A') * DecoderKeys.PLANE_DECKEY_NUM
                : shiftChar >= 'a' && shiftChar <= 'f' ? DecoderKeys.SHIFT_A_DECKEY_START + (shiftChar - 'a') * DecoderKeys.PLANE_DECKEY_NUM
                : 0;
        }

        /// <summary>
        /// シフト面も含んだ漢直キーコード文字列を解析する("20", "A31", "B11" など)<br/>
        /// 漢直キーコードでなければ -1 を返す
        /// </summary>
        private static int parseShiftPlaneDeckey(string str)
        {
            if (str._isEmpty()) return -1;
            var s = str._toUpper();
            int offset = CalcShiftOffset(s[0]);
            int deckey = offset > 0 ? s._safeSubstring(1)._parseInt(-1) : s._parseInt(-1);
            if (deckey < 0 || deckey >= DecoderKeys.STROKE_DECKEY_END) return -1;
            return deckey + offset;
        }

        public static uint DefaultExtModifierKey = 0;

        public static Dictionary<int, string> SingleHitDefs = new Dictionary<int, string>();

        public static Dictionary<uint, Dictionary<int, string>> ExtModifierKeyDefs = new Dictionary<uint, Dictionary<int, string>>();

        /// <summary>
        /// 追加の modifier 変換表を読み込む<br/>
        /// <return>複合コマンド定義文字列を返す</return>
        /// </summary>
        public static string ReadExtraModConversionFile(string filename)
        {
            logger.Info("ENTER");
            initializeShiftPlaneForShiftModKey();
            SingleHitDefs.Clear();
            ExtModifierKeyDefs.Clear();
            disabledExtKeyLines.Clear();
            var sbCompCmds = new StringBuilder();   // 複合コマンド定義文字列
            if (filename._notEmpty()) {
                var filePath = KanchokuIni.Singleton.KanchokuDir._joinPath(filename);
                logger.Info($"modConversion file path={filePath}");
                var lines = Helper.GetFileContent(filePath, Encoding.UTF8);
                if (lines == null) {
                    logger.Error($"Can't read modConversion file: {filePath}");
                    SystemHelper.ShowErrorMessageBox($"修飾キー変換定義ファイル({filePath}の読み込みに失敗しました。");
                    return null;
                }
                Dictionary<uint, int> modCount = new Dictionary<uint, int>();
                int nl = 0;
                foreach (var rawLine in lines._split('\n')) {
                    ++nl;
                    logger.DebugH(() => $"line({nl}): {rawLine}");
                    var origLine = rawLine._reReplace("#.*", "").Trim();
                    var line = origLine.Replace(" ", "")._toLower();
                    if (line._notEmpty() && line[0] != '#') {
                        if (line._reMatch(@"^\w+=")) {
                            //シフト面の割り当て
                            if (AssignShiftPlane(line, rawLine)) continue;
                        } else {
                            // NAME:xx:function
                            var origItems = origLine._splitn(':', 3);
                            var items = line._splitn(':', 3);
                            if (items._length() == 3) {
                                string modName = items[0];
                                string modifiee = items[1];
                                string target = origItems[2]._strip()._stripDq();

                                if (IsDisabledExtKey(modName)) {
                                    // 無効にされた拡張修飾キーだった
                                    logger.DebugH(() => $"modName={modName} is disabled");
                                    disabledExtKeyLines.Add(rawLine);
                                    continue;
                                }

                                uint modKey = 0;
                                int modDeckey = SpecialKeysAndFunctions.GetDeckeyByName(modName);
                                int modifieeDeckey = SpecialKeysAndFunctions.GetDeckeyByName(modifiee)._gtZeroOr(modifiee._parseInt(-1));
                                logger.DebugH(() => $"modName={modName}, modifiee={modifiee}, target={target}, modDeckey={modDeckey}, modifieeDeckey={modifieeDeckey})");

                                // 被修飾キーの仮想キーコード: 特殊キー名(esc, tab, ins, ...)または漢直コード(00～49)から、それに該当する仮想キーコードを得る
                                uint vkey = getVKeyFromDecKey(modifieeDeckey);
                                logger.DebugH(() => $"vkey={vkey}");
                                if (vkey == 0) {
                                    // 被修飾キーが指定されていない場合は、拡張修飾キーまたは特殊キーの単打とみなす
                                    vkey = GetFuncVkeyByName(modName);  
                                } else {
                                    // 被修飾キーが指定されている場合は、拡張修飾キーの修飾フラグを取得
                                    modKey = GetModifierKeyByName(modName);
                                    if (isPlaneMappedModifier(modKey) && !ShiftPlaneForShiftModKey.ContainsKey(modKey)) {
                                        // mod に対する ShiftPlane が設定されていない場合は、適当なシフト面を割り当てる(通常Shiftはすでに設定済みのはず)
                                        // mod に対する ShiftPlane が設定されていない場合は、拡張シフトB面以降の空いている面を割り当てる(空いてなければF面)
                                        int pn = ShiftPlane_B;
                                        while (pn < ShiftPlane_F) {
                                            if (!ShiftPlaneForShiftModKey.FindPlane(pn) && !ShiftPlaneForShiftModKeyWhenDecoderOff.FindPlane(pn)) {
                                                break;
                                            }
                                            ++pn;
                                        }
                                        ShiftPlaneForShiftModKey.Add(modKey, pn);
                                        ShiftPlaneForShiftModKeyWhenDecoderOff.Add(modKey, pn);
                                    }
                                }

                                // targetが漢直コードによる直接定義(nfer:11:B11など)の場合⇒無条件のデコーダ呼び出し(デコーダがOFFの場合も呼び出される)
                                int convertUnconditional(int deckey)
                                {
                                    return deckey >= 0 ? deckey += DecoderKeys.UNCONDITIONAL_DECKEY_OFFSET : deckey;
                                }

                                bool ctrl = target._startsWith("^");
                                var name = target.Replace("^", "")._toLower();
                                int targetDeckey = convertUnconditional(parseShiftPlaneDeckey(target));   // まず、拡張シフト面も含めた漢直コードとして解析する

                                logger.DebugH(() => $"ctrl={ctrl}, name={name}, targetDeckey={targetDeckey:x}H({targetDeckey})");

                                if (targetDeckey < 0) {
                                    // 変換先は拡張シフト面も含めた漢直コードではなかったので、特殊キーとして解析する
                                    targetDeckey = specialDecKeysFromName._safeGet(name);
                                    if (ctrl) {
                                        uint decVkey = 0;
                                        if (name._safeLength() == 1 && name._ge("a") && name._le("z")) {
                                            // Ctrl-A～Ctrl-Z
                                            decVkey = faceToVkey._safeGet(name._toUpper());
                                            targetDeckey = DecoderKeys.DECKEY_CTRL_A + name[0] - 'a';
                                        } else if (targetDeckey >= DecoderKeys.FUNC_DECKEY_START && targetDeckey < DecoderKeys.FUNC_DECKEY_END) {
                                            // Ctrl+機能キー(特殊キー)(Ctrl+Tabとか)
                                            decVkey = getVKeyFromDecKey(targetDeckey);
                                            targetDeckey += DecoderKeys.CTRL_FUNC_DECKEY_START - DecoderKeys.FUNC_DECKEY_START;
                                        }
                                        logger.DebugH(() => $"targetDeckey={targetDeckey:x}H({targetDeckey}), ctrl={ctrl}, decVkey={decVkey:x}H({decVkey})");
                                        if (targetDeckey > 0) AddModifiedDeckey(targetDeckey, KeyModifiers.MOD_CONTROL, decVkey);
                                    }

                                    if (targetDeckey == 0) {
                                        if (modKey > 0 && modifieeDeckey >= 0) {
                                            // 特殊キーでもなかったので、文字列、複合コマンドまたは機能として扱う
                                            var strokeCode = GetShiftPlanePrefix(ShiftPlaneForShiftModKey.GetPlane(modKey)) + modifieeDeckey.ToString();
                                            var decoderStr = target._getFirst() == '@' ? target : $"\"{target}\"";
                                            sbCompCmds.Append($"-{strokeCode}>{decoderStr}\n");
                                            targetDeckey = convertUnconditional(parseShiftPlaneDeckey(strokeCode));   // 拡張シフト面も含めた漢直コードとして解析する
                                        } else {
                                            targetDeckey = -1;  // invalid line
                                        }
                                    } else {
                                        // 特殊キーだったので、漢直コードから変換テーブルに登録しておく
                                        logger.DebugH(() => $"AddSpecialDeckey: name={name}, targetDeckey={targetDeckey:x}H({targetDeckey})");
                                        AddSpecialDeckey(name, targetDeckey);
                                    }
                                }

                                logger.DebugH(() => $"modKey={modKey:x}H, vkey={vkey:x}H, targetDeckey={targetDeckey:x}H({targetDeckey}), ctrl={ctrl}, name={name}");

                                if (vkey > 0 && targetDeckey > 0) {
                                    if (modKey == 0) {
                                        logger.DebugH(() => $"Single Hit");
                                        // キー単打の場合は、キーの登録だけで、拡張シフトB面の割り当てはやらない
                                        AddDecKeyAndCombo(targetDeckey, 0, vkey, true);  // targetDeckey から vkey(拡張修飾キー)への逆マップは不要
                                        VirtualKeys.AddExModVkeyAssignedForDecoderFuncByVkey(vkey);
                                        SingleHitDefs[modDeckey] = target;
                                    } else {
                                        logger.DebugH(() => $"Extra Modifier");
                                        // 拡張修飾キー設定
                                        modCount[modKey] = modCount._safeGet(modKey) + 1;
                                        ExtModifierKeyDefs._safeGetOrNewInsert(modKey)[modifieeDeckey] = target;
                                        AddModConvertedDecKeyFromCombo(targetDeckey, modKey, vkey);
                                    }
                                    continue;
                                }
                            }
                        }
                        logger.Warn($"Invalid line({nl}): {rawLine}");
                    }
                }
                int maxCnt = 0;
                foreach (var pair in modCount) {
                    if (pair.Value > maxCnt) {
                        maxCnt = pair.Value;
                        DefaultExtModifierKey = pair.Key;
                    }
                }
            }
            logger.Info("LEAVE");
            return sbCompCmds.ToString();
        }

        //public static void UpdateModConversion(IEnumerable<string> lines)
        //{
        //    foreach (var line in lines) {
        //        if (line._reMatch(@"^\w+=")) {
        //            AssignShiftPlane(line);
        //        }
        //    }
        //}

        /// <summary>
        /// 拡張シフトキーに対するシフト面の割り当て<br/>
        ///   EXT_MOD_NAME=plane[|plane]
        /// </summary>
        /// <param name="line"></param>
        /// <param name="rawLine"></param>
        /// <returns></returns>
        public static bool AssignShiftPlane(string line, string rawLine = null)
        {
            // NAME=plane[|plane]...
            var items = line._toLower()._split('=');
            if (items._length() != 2) return false;

            if (IsDisabledExtKey(items[0])) {
                disabledExtKeyLines.Add(rawLine._orElse(line));
                return true;
            }

            uint modKey = GetModifierKeyByName(items[0]);
            var planes = items[1]._split('|');
            int shiftPlane = ShiftPlane_NONE;
            switch (planes._getNth(0)) {
                case "shift": shiftPlane = ShiftPlane_SHIFT; break;
                case "shifta": shiftPlane = ShiftPlane_A; break;
                case "shiftb": shiftPlane = ShiftPlane_B; break;
                case "shiftc": shiftPlane = ShiftPlane_C; break;
                case "shiftd": shiftPlane = ShiftPlane_D; break;
                case "shifte": shiftPlane = ShiftPlane_E; break;
                case "shiftf": shiftPlane = ShiftPlane_F; break;
                case "none": shiftPlane = ShiftPlane_NONE; break;
                default: return false;
            }

            var shiftPlaneWhenOff = shiftPlane;
            if (planes.Length > 1) {
                switch (planes._getNth(1)) {
                    case "shift": shiftPlaneWhenOff = ShiftPlane_SHIFT; break;
                    case "shifta": shiftPlaneWhenOff = ShiftPlane_A; break;
                    case "shiftb": shiftPlaneWhenOff = ShiftPlane_B; break;
                    case "shiftc": shiftPlaneWhenOff = ShiftPlane_C; break;
                    case "shiftd": shiftPlaneWhenOff = ShiftPlane_D; break;
                    case "shifte": shiftPlaneWhenOff = ShiftPlane_E; break;
                    case "shiftf": shiftPlaneWhenOff = ShiftPlane_F; break;
                    case "none": shiftPlaneWhenOff = ShiftPlane_NONE; break;
                    default: return false;
                }
            }

            logger.DebugH(() => $"mod={modKey:x}H({modKey}), shiftPlane={shiftPlane}, shiftPlaneWhenOff={shiftPlaneWhenOff}");
            if (modKey != 0 && shiftPlane > 0) {
                logger.DebugH(() => $"shiftPlaneForShiftFuncKey[{modKey}] = {shiftPlane}, shiftPlaneForShiftFuncKeyWhenDecoderOff[{modKey}] = {shiftPlaneWhenOff}");
                ShiftPlaneForShiftModKey.Add(modKey, shiftPlane);
                ShiftPlaneForShiftModKeyWhenDecoderOff.Add(modKey, shiftPlaneWhenOff);
            }
            return true;    // OK
        }

        /// <summary>テーブルファイルor設定ダイアログで割り当てたSandSシフト面を優先する</summary>
        public static void AssignSanSPlane(int shiftPlane = 0)
        {
            logger.DebugH(() => $"CALLED: SandSEnabled={Settings.SandSEnabled}, SandSAssignedPlane={Settings.SandSAssignedPlane}");
            if (Settings.SandSEnabled) {
                if (shiftPlane <= 0) shiftPlane = Settings.SandSAssignedPlane;
                if (shiftPlane > 0 && shiftPlane < ShiftPlane_NUM) {
                    ShiftPlaneForShiftModKey.Add(KeyModifiers.MOD_SPACE, shiftPlane);
                }
            }
        }

        public static int GetSandSPlane()
        {
            return ShiftPlaneForShiftModKey.GetPlane(KeyModifiers.MOD_SPACE);
        }

        // ファイルに書き出す拡張修飾キー設定を作成
        public static string MakeModConversionContents()
        {
            var sb = new StringBuilder();

            // シフト面設定
            var dict = new Dictionary<string, string>();
            foreach (var pair in ShiftPlaneForShiftModKey.GetPairs()) {
                var keyName = GetModifierNameByKey(pair.Key);
                if (keyName._notEmpty()) {
                    dict[keyName] = GetShiftPlaneName(pair.Value);
                }
            }
            foreach (var pair in ShiftPlaneForShiftModKeyWhenDecoderOff.GetPairs()) {
                var keyName = GetModifierNameByKey(pair.Key);
                if (keyName._notEmpty()) {
                    var str = dict._safeGet(keyName);
                    if (str._notEmpty()) {
                        dict[keyName] = $"{str}|{GetShiftPlaneName(pair.Value)}";
                    }
                }
            }
            sb.Append("## Shift plane settings ##\n");
            foreach (var pair in dict) {
                if (pair.Value != "none|none") {
                    sb.Append($"{pair.Key}={pair.Value}\n");
                }
            }

            // 単打設定
            sb.Append("\n## Single hit settings ##\n");
            foreach (var pair in SingleHitDefs) {
                var keyName = SpecialKeysAndFunctions.GetKeyNameByDeckey(pair.Key);
                if (keyName._notEmpty()) {
                    sb.Append($"{keyName}::{pair.Value}\n");
                }
            }

            // 拡張修飾キー設定
            sb.Append("\n## Extra modifier settings ##\n");
            foreach (var pair in ExtModifierKeyDefs) {
                var keyName = GetModifierNameByKey(pair.Key);
                logger.DebugH(() => $"modKey={pair.Key}, keyName={keyName}, dict.Size={pair.Value.Count}");
                if (keyName._notEmpty()) {
                    foreach (var p in pair.Value) {
                        var deckey = p.Key;
                        var target = p.Value;
                        if (target._notEmpty()) {
                            if (deckey >= 0 && deckey < DecoderKeys.NORMAL_DECKEY_NUM) {
                                sb.Append($"{keyName}:{deckey}:{target}\n");
                            } else {
                                var modifieeName = SpecialKeysAndFunctions.GetKeyNameByDeckey(deckey);
                                if (modifieeName._notEmpty()) sb.Append($"{keyName}:{modifieeName}:{target}\n");
                            }
                        }
                    }
                }
            }

            // 無効化された設定
            sb.Append("\n## Disabled modifier settings ##\n");
            foreach(var line in disabledExtKeyLines) {
                sb.Append(line + "\n");
            }

            return sb.ToString();
        }
    }
}

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

        /// <summary> 機能キー (Esc, 半/全, Tab, Caps, 英数, 無変換, 変換, かな, BS, Enter, Ins, Del, Home, End, PgUp, PgDn, ↑, ↓, ←, →)</summary>
        private static uint[] vkeyArrayFuncKeys = {
            /*Esc*/ 0x1b, /*半/全*/ 0xf3, /*Tab*/ 0x09, /*Caps*/ 0x14, /*英数*/ 0xf0, /*無変換*/ 0x1d, /*変換*/ 0x1c, /*かな*/ 0xf2, /*BS*/ 0x08, /*Enter*/ 0x0d,
            /*Ins*/ 0x2d, /*Del*/ 0x2e, /*Home*/ 0x24, /*End*/ 0x23, /*PgUp*/ 0x21, /*PgDn*/ 0x22, /*↑*/ 0x26, /*↓*/ 0x28, /*←*/ 0x25, /*→*/ 0x27
        };

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
            //{":*", (uint)Keys.Oem1 },           // ba/106
            //{";:", (uint)Keys.Oem1 },           // ba/101
            //{";+", (uint)Keys.Oemplus },        // bb/106
            //{",<", (uint)Keys.Oemcomma },       // bc
            //{"-=", (uint)Keys.OemMinus },       // bd/106
            //{"-_", (uint)Keys.OemMinus },       // bd/101
            //{".>", (uint)Keys.OemPeriod },      // be
            //{"/?", (uint)Keys.Oem2 },           // bf
            //{"@`", (uint)Keys.Oem3 },           // c0/106
            //{"[{", (uint)Keys.Oem4 },           // db
            //{"\\|", (uint)Keys.Oem5 },          // dc
            //{"]}", (uint)Keys.Oem6 },           // dd
            //{"^~", (uint)Keys.Oem7 },           // de/106
            //{"'\"", (uint)Keys.Oem7 },          // de/101
            //{"\\_", (uint)Keys.Oem102 },        // e2/106
            //{"\\ ", (uint)Keys.Oem102 },        // e2/101
        };

        public static VKeyCombo EmptyCombo = new VKeyCombo(0, 0);

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

        public static void AddDecKeyAndCombo(int deckey, uint mod, uint vkey)
        {
            logger.Debug(() => $"deckey={deckey:x}H({deckey}), mod={mod:x}H, vkey={vkey:x}H({vkey})");
            var combo = new VKeyCombo(mod, (uint)vkey);
            VKeyComboFromDecKey[deckey] = combo;
            DecKeyFromVKeyCombo[combo.SerialValue] = deckey;
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
                AddDecKeyAndCombo(DecoderKeys.CTRL_DECKEY_START + id, KeyModifiers.MOD_CONTROL, vkey);
                // Ctrl+Shift
                AddDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_DECKEY_START + id, KeyModifiers.MOD_CONTROL + KeyModifiers.MOD_SHIFT, vkey);
            }

            // 機能キー
            for (int id = 0; id < DecoderKeys.FUNC_DECKEY_NUM; ++id) {
                uint vkey = getVKeyFromDecKey(DecoderKeys.FUNC_DECKEY_START + id);
                // Normal
                AddDecKeyAndCombo(DecoderKeys.FUNC_DECKEY_START + id, 0, vkey);
                // Ctrl
                AddDecKeyAndCombo(DecoderKeys.CTRL_FUNC_DECKEY_START + id, KeyModifiers.MOD_CONTROL, vkey);
                // Ctrl+Shifted
                AddDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_START + id, KeyModifiers.MOD_CONTROL + KeyModifiers.MOD_SHIFT, vkey);
            }
        }

        public static int GetDecKeyFromCombo(uint mod, uint vkey)
        {
            return DecKeyFromVKeyCombo._safeGet(VKeyCombo.CalcSerialValue(mod, vkey), -1);
        }

        // 静的コンストラクタ
        static VirtualKeys()
        {
            VKeyComboFromDecKey = new VKeyCombo?[DecoderKeys.GLOBAL_DECKEY_ID_END];
            DecKeyFromVKeyCombo = new Dictionary<uint, int>();

            AddDecKeyAndCombo(DecoderKeys.CTRL_A_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.A);
            AddDecKeyAndCombo(DecoderKeys.CTRL_B_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.B);
            AddDecKeyAndCombo(DecoderKeys.CTRL_C_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.C);
            AddDecKeyAndCombo(DecoderKeys.CTRL_D_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.D);
            AddDecKeyAndCombo(DecoderKeys.CTRL_E_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.E);
            AddDecKeyAndCombo(DecoderKeys.CTRL_F_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.F);
            AddDecKeyAndCombo(DecoderKeys.CTRL_G_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.G);
            AddDecKeyAndCombo(DecoderKeys.CTRL_H_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.H);
            AddDecKeyAndCombo(DecoderKeys.CTRL_I_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.I);
            AddDecKeyAndCombo(DecoderKeys.CTRL_J_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.J);
            AddDecKeyAndCombo(DecoderKeys.CTRL_K_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.K);
            AddDecKeyAndCombo(DecoderKeys.CTRL_L_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.L);
            AddDecKeyAndCombo(DecoderKeys.CTRL_M_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.M);
            AddDecKeyAndCombo(DecoderKeys.CTRL_N_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.N);
            AddDecKeyAndCombo(DecoderKeys.CTRL_O_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.O);
            AddDecKeyAndCombo(DecoderKeys.CTRL_P_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.P);
            AddDecKeyAndCombo(DecoderKeys.CTRL_Q_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Q);
            AddDecKeyAndCombo(DecoderKeys.CTRL_R_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.R);
            AddDecKeyAndCombo(DecoderKeys.CTRL_S_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.S);
            AddDecKeyAndCombo(DecoderKeys.CTRL_T_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.T);
            AddDecKeyAndCombo(DecoderKeys.CTRL_U_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.U);
            AddDecKeyAndCombo(DecoderKeys.CTRL_V_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.V);
            AddDecKeyAndCombo(DecoderKeys.CTRL_W_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.W);
            AddDecKeyAndCombo(DecoderKeys.CTRL_X_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.X);
            AddDecKeyAndCombo(DecoderKeys.CTRL_Y_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Y);
            AddDecKeyAndCombo(DecoderKeys.CTRL_Z_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Z);

            AddDecKeyAndCombo(DecoderKeys.LEFT_TRIANGLE_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.Oemcomma);
            AddDecKeyAndCombo(DecoderKeys.RIGHT_TRIANGLE_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.OemPeriod);
            AddDecKeyAndCombo(DecoderKeys.QUESTION_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.OemQuestion);

            AddDecKeyAndCombo(DecoderKeys.SHIFT_SPACE_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.Space);

            AddDecKeyAndCombo(DecoderKeys.ENTER_DECKEY, 0, (uint)Keys.Enter);
            AddDecKeyAndCombo(DecoderKeys.ESC_DECKEY, 0, (uint)Keys.Escape);
            AddDecKeyAndCombo(DecoderKeys.BS_DECKEY, 0, (uint)Keys.Back);
            AddDecKeyAndCombo(DecoderKeys.TAB_DECKEY, 0, (uint)Keys.Tab);
            AddDecKeyAndCombo(DecoderKeys.DEL_DECKEY, 0, (uint)Keys.Delete);
            AddDecKeyAndCombo(DecoderKeys.HOME_DECKEY, 0, (uint)Keys.Home);
            AddDecKeyAndCombo(DecoderKeys.END_DECKEY, 0, (uint)Keys.End);
            AddDecKeyAndCombo(DecoderKeys.PAGE_UP_DECKEY, 0, (uint)Keys.PageUp);
            AddDecKeyAndCombo(DecoderKeys.PAGE_DOWN_DECKEY, 0, (uint)Keys.PageDown);
            AddDecKeyAndCombo(DecoderKeys.LEFT_ARROW_DECKEY, 0, (uint)Keys.Left);
            AddDecKeyAndCombo(DecoderKeys.RIGHT_ARROW_DECKEY, 0, (uint)Keys.Right);
            AddDecKeyAndCombo(DecoderKeys.UP_ARROW_DECKEY, 0, (uint)Keys.Up);
            AddDecKeyAndCombo(DecoderKeys.DOWN_ARROW_DECKEY, 0, (uint)Keys.Down);

            AddDecKeyAndCombo(DecoderKeys.CTRL_LEFT_ARROW_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Left);
            AddDecKeyAndCombo(DecoderKeys.CTRL_RIGHT_ARROW_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Right);
            AddDecKeyAndCombo(DecoderKeys.CTRL_UP_ARROW_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Up);
            AddDecKeyAndCombo(DecoderKeys.CTRL_DOWN_ARROW_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Down);

            AddDecKeyAndCombo(DecoderKeys.CTRL_SPACE_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Space);
            AddDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_SPACE_DECKEY, KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT, (uint)Keys.Space);
            //addDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_G_DECKEY, KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT, (uint)Keys.G);
            //addDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_T_DECKEY, KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT, (uint)Keys.T);

            //addDecKeyAndCombo(DecoderKeys.CTRL_SEMICOLON_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Oemplus);
            //addDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_SEMICOLON_DECKEY, KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT, (uint)Keys.Oemplus);
            //addDecKeyAndCombo(DecoderKeys.CTRL_COLON_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Oem1);
            //addDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_COLON_DECKEY, KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT, (uint)Keys.Oem1);
        }

        public static void AddCtrlDeckey(string faces, int ctrlDeckey, int ctrlShiftDeckey)
        {
            uint vkey = faceToVkey._safeGet(faces);
            if (vkey != 0) {
                if (ctrlDeckey > 0) AddDecKeyAndCombo(ctrlDeckey, KeyModifiers.MOD_CONTROL, vkey);
                if (ctrlShiftDeckey > 0) AddDecKeyAndCombo(ctrlShiftDeckey, KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT, vkey);
            }
        }

        public static void AddCtrlDeckey(int deckey, int ctrlDeckey, int ctrlShiftDeckey)
        {
            if (deckey >= 0) {
                uint vkey = getVKeyFromDecKey(deckey);
                if (vkey != 0) {
                    if (ctrlDeckey > 0) AddDecKeyAndCombo(ctrlDeckey, KeyModifiers.MOD_CONTROL, vkey);
                    if (ctrlShiftDeckey > 0) AddDecKeyAndCombo(ctrlShiftDeckey, KeyModifiers.MOD_CONTROL | KeyModifiers.MOD_SHIFT, vkey);
                }
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

            //// 49番目を Shift+Space として登録しておく
            //addDeckeyAndCombo(49, KeyModifiers.MOD_SHIFT, (uint)Keys.Space);

            logger.Info("LEAVE");
            return true;
        }

    }
}

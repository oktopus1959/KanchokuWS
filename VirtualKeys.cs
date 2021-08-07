using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Utils;

namespace KanchokuWS
{
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

        public uint SerialValue => ((modifier & 0xffff) << 16) + (vkey & 0xffff);
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

        ///// <summary> 打鍵で使われる仮想キー配列(DecKeyId順に並んでいる) </summary>
        //private static uint[] strokeVKeys;

        //private static uint[] VKeyArray106 = new uint[] {
        //    0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
        //    0x51, 0x57, 0x45, 0x52, 0x54, 0x59, 0x55, 0x49, 0x4f, 0x50,
        //    0x41, 0x53, 0x44, 0x46, 0x47, 0x48, 0x4a, 0x4b, 0x4c, 0xbb,
        //    0x5a, 0x58, 0x43, 0x56, 0x42, 0x4e, 0x4d, 0xbc, 0xbe, 0xbf,
        //    0x20, 0xbd, 0xde, 0xdc, 0xc0, 0xdb, 0xba, 0xdd, 0xe2, 0x00,
        //};

        //public static uint GetVKeyFromDecKey(int deckey)
        //{
        //    return strokeVKeys._getNth(deckey);
        //}

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

        private static void addDecKeyAndCombo(int deckey, uint mod, uint vkey)
        {
            logger.Debug(() => $"deckey={deckey:x}H({deckey}), mod={mod:x}H, vkey={vkey:x}H({vkey})");
            var combo = new VKeyCombo(mod, (uint)vkey);
            VKeyComboFromDecKey[deckey] = combo;
            DecKeyFromVKeyCombo[combo.SerialValue] = deckey;
        }

        public static void SetupDecKeyAndComboTable(KeyboardEventDispatcher dispatcher)
        {
            for (int id = 0; id < DecoderKeys.NORMAL_DECKEY_NUM; ++id) {
                // Normal
                addDecKeyAndCombo(id, 0, dispatcher.GetVKeyFromDecKey(id));
                // Shifted
                addDecKeyAndCombo(DecoderKeys.SHIFT_DECKEY_START + id, KeyModifiers.MOD_SHIFT, dispatcher.GetVKeyFromDecKey(id));
            }
        }

        public static int GetDecKeyFromCombo(uint mod, uint vkey)
        {
            return DecKeyFromVKeyCombo._safeGet(new VKeyCombo(mod, vkey).SerialValue, -1);
        }

        // 静的コンストラクタ
        static VirtualKeys()
        {
            VKeyComboFromDecKey = new VKeyCombo?[DecoderKeys.GLOBAL_DECKEY_ID_END];
            DecKeyFromVKeyCombo = new Dictionary<uint, int>();

            addDecKeyAndCombo(DecoderKeys.CTRL_A_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.A);
            addDecKeyAndCombo(DecoderKeys.CTRL_B_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.B);
            addDecKeyAndCombo(DecoderKeys.CTRL_C_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.C);
            addDecKeyAndCombo(DecoderKeys.CTRL_D_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.D);
            addDecKeyAndCombo(DecoderKeys.CTRL_E_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.E);
            addDecKeyAndCombo(DecoderKeys.CTRL_F_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.F);
            addDecKeyAndCombo(DecoderKeys.CTRL_G_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.G);
            addDecKeyAndCombo(DecoderKeys.CTRL_H_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.H);
            addDecKeyAndCombo(DecoderKeys.CTRL_I_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.I);
            addDecKeyAndCombo(DecoderKeys.CTRL_J_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.J);
            addDecKeyAndCombo(DecoderKeys.CTRL_K_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.K);
            addDecKeyAndCombo(DecoderKeys.CTRL_L_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.L);
            addDecKeyAndCombo(DecoderKeys.CTRL_M_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.M);
            addDecKeyAndCombo(DecoderKeys.CTRL_N_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.N);
            addDecKeyAndCombo(DecoderKeys.CTRL_O_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.O);
            addDecKeyAndCombo(DecoderKeys.CTRL_P_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.P);
            addDecKeyAndCombo(DecoderKeys.CTRL_Q_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Q);
            addDecKeyAndCombo(DecoderKeys.CTRL_R_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.R);
            addDecKeyAndCombo(DecoderKeys.CTRL_S_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.S);
            addDecKeyAndCombo(DecoderKeys.CTRL_T_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.T);
            addDecKeyAndCombo(DecoderKeys.CTRL_U_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.U);
            addDecKeyAndCombo(DecoderKeys.CTRL_V_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.V);
            addDecKeyAndCombo(DecoderKeys.CTRL_W_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.W);
            addDecKeyAndCombo(DecoderKeys.CTRL_X_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.X);
            addDecKeyAndCombo(DecoderKeys.CTRL_Y_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Y);
            addDecKeyAndCombo(DecoderKeys.CTRL_Z_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Z);

            addDecKeyAndCombo(DecoderKeys.LEFT_TRIANGLE_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.Oemcomma);
            addDecKeyAndCombo(DecoderKeys.RIGHT_TRIANGLE_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.OemPeriod);
            addDecKeyAndCombo(DecoderKeys.QUESTION_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.OemQuestion);

            addDecKeyAndCombo(DecoderKeys.SHIFT_SPACE_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.Space);

            addDecKeyAndCombo(DecoderKeys.ENTER_DECKEY, 0, (uint)Keys.Enter);
            addDecKeyAndCombo(DecoderKeys.ESC_DECKEY, 0, (uint)Keys.Escape);
            addDecKeyAndCombo(DecoderKeys.BS_DECKEY, 0, (uint)Keys.Back);
            addDecKeyAndCombo(DecoderKeys.TAB_DECKEY, 0, (uint)Keys.Tab);
            addDecKeyAndCombo(DecoderKeys.DEL_DECKEY, 0, (uint)Keys.Delete);
            addDecKeyAndCombo(DecoderKeys.HOME_DECKEY, 0, (uint)Keys.Home);
            addDecKeyAndCombo(DecoderKeys.END_DECKEY, 0, (uint)Keys.End);
            addDecKeyAndCombo(DecoderKeys.PAGE_UP_DECKEY, 0, (uint)Keys.PageUp);
            addDecKeyAndCombo(DecoderKeys.PAGE_DOWN_DECKEY, 0, (uint)Keys.PageDown);
            addDecKeyAndCombo(DecoderKeys.LEFT_ARROW_DECKEY, 0, (uint)Keys.Left);
            addDecKeyAndCombo(DecoderKeys.RIGHT_ARROW_DECKEY, 0, (uint)Keys.Right);
            addDecKeyAndCombo(DecoderKeys.UP_ARROW_DECKEY, 0, (uint)Keys.Up);
            addDecKeyAndCombo(DecoderKeys.DOWN_ARROW_DECKEY, 0, (uint)Keys.Down);

            addDecKeyAndCombo(DecoderKeys.CTRL_LEFT_ARROW_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Left);
            addDecKeyAndCombo(DecoderKeys.CTRL_RIGHT_ARROW_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Right);
            addDecKeyAndCombo(DecoderKeys.CTRL_UP_ARROW_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Up);
            addDecKeyAndCombo(DecoderKeys.CTRL_DOWN_ARROW_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Down);

            addDecKeyAndCombo(DecoderKeys.CTRL_SPACE_DECKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Space);
            addDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_SPACE_DECKEY, KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT, (uint)Keys.Space);
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
                if (ctrlDeckey > 0) addDecKeyAndCombo(ctrlDeckey, KeyModifiers.MOD_CONTROL, vkey);
                if (ctrlShiftDeckey > 0) addDecKeyAndCombo(ctrlShiftDeckey, KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT, vkey);
            }
        }

        ///// <summary>
        ///// 仮想キーコードからなるキーボードファイル(106.keyとか)を読み込んで、仮想キーコードの配列を作成する<br/>
        ///// 仮想キーコードはDecKeyId順に並んでいる必要がある。
        ///// </summary>
        ///// <returns></returns>
        //public static bool ReadKeyboardFile()
        //{
        //    logger.Info("ENTER");

        //    var array = VKeyArray106;

        //    var ini = KanchokuIni.Singleton;
        //    var filePath = ini.KanchokuDir._joinPath(Settings.KeyboardFile);
        //    if (filePath._notEmpty()) {
        //        logger.Info($"keyboard file path={filePath}");
        //        var vkeys = Helper.GetFileContent(filePath, Encoding.UTF8);
        //        if (vkeys == null) {
        //            logger.Error($"Can't read keyboard file: {filePath}");
        //            SystemHelper.ShowErrorMessageBox($"キーボード定義ファイル({filePath}の読み込みに失敗しました。");
        //            return false;
        //        }
        //        var items = vkeys._split('\n').Select(line => line.Trim().Replace(" ", "")).
        //            Where(line => line._notEmpty() && line[0] != '#' && line[0] != ';')._join("").TrimEnd(',')._split(',').ToArray();

        //        array = items.Select(x => (uint)x._parseHex(0)).ToArray();
        //        int idx = array._findIndex(x => x < 0);
        //        if (idx >= 0 && idx < array.Length) {
        //            logger.Warn($"Invalid keyboard def: file={filePath}, {idx}th: {items[idx]}");
        //            SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}の{idx}番目のキー定義({items[idx]})が誤っています。");
        //            return false;
        //        }
        //        if (array.Length < DecoderKeys.NORMAL_DECKEY_NUM) {
        //            logger.Warn($"No sufficient keyboard def: file={filePath}, total {array.Length} defs");
        //            SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}のキー定義の数({array.Length})が不足しています。");
        //            return false;
        //        }
        //    }
        //    logger.Info(() => $"keyboard keyNum={array.Length}, array={array.Select(x=>x.ToString("x"))._join(", ")}");

        //    strokeVKeys = array;

        //    for (int id = 0; id < DecoderKeys.NORMAL_DECKEY_NUM; ++id) {
        //        // Normal
        //        addDecKeyAndCombo(id, 0, GetVKeyFromDecKey(id));
        //        // Shifted
        //        addDecKeyAndCombo(DecoderKeys.SHIFT_DECKEY_START + id, KeyModifiers.MOD_SHIFT, GetVKeyFromDecKey(id));
        //    }

        //    //// 49番目を Shift+Space として登録しておく
        //    //addDeckeyAndCombo(49, KeyModifiers.MOD_SHIFT, (uint)Keys.Space);

        //    logger.Info("LEAVE");
        //    return true;
        //}

    }
}

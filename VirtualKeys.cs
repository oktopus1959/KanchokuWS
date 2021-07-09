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
        public const uint LCONTROL = 0xa2;
        public const uint RCONTROL = 0xa3;

        /// <summary> 打鍵で使われる仮想キー配列(HotKeyId順に並んでいる) </summary>
        private static uint[] strokeVKeys;

        private static uint[] VKeyArray106 = new uint[] {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
            0x51, 0x57, 0x45, 0x52, 0x54, 0x59, 0x55, 0x49, 0x4f, 0x50,
            0x41, 0x53, 0x44, 0x46, 0x47, 0x48, 0x4a, 0x4b, 0x4c, 0xbb,
            0x5a, 0x58, 0x43, 0x56, 0x42, 0x4e, 0x4d, 0xbc, 0xbe, 0xbf,
            0x20, 0xbd, 0xde, 0xdc, 0xc0, 0xdb, 0xba, 0xdd, 0xe2, 0x00,
        };

        public static uint GetVKeyFromHotKey(int hotkey)
        {
            return strokeVKeys._getNth(hotkey);
        }

        public static VKeyCombo EmptyCombo = new VKeyCombo(0, 0);

        /// <summary>
        /// HOTKEY id から仮想キーコンビネーションを得るための配列
        /// </summary>
        private static VKeyCombo?[] VKeyComboFromHotKey;

        public static VKeyCombo? GetVKeyComboFromHotKey(int hotkey)
        {
            return VKeyComboFromHotKey._getNth(hotkey);
        }

        /// <summary>
        /// 仮想キーコンビネーションのSerial値からHOTKEY を得るための辞書
        /// </summary>
        private static Dictionary<uint, int> HotKeyFromVKeyCombo;

        private static void addHotkeyAndCombo(int hotkey, uint mod, uint vkey)
        {
            logger.Debug(() => $"hotkey={hotkey:x}H({hotkey}), mod={mod:x}H, vkey={vkey:x}H({vkey})");
            var combo = new VKeyCombo(mod, (uint)vkey);
            VKeyComboFromHotKey[hotkey] = combo;
            HotKeyFromVKeyCombo[combo.SerialValue] = hotkey;
        }

        public static int GetHotKeyFromCombo(uint mod, uint vkey)
        {
            return HotKeyFromVKeyCombo._safeGet(new VKeyCombo(mod, vkey).SerialValue, -1);
        }

        // 静的コンストラクタ
        static VirtualKeys()
        {
            VKeyComboFromHotKey = new VKeyCombo?[HotKeys.GLOBAL_HOTKEY_ID_END];
            HotKeyFromVKeyCombo = new Dictionary<uint, int>();

            addHotkeyAndCombo(HotKeys.CTRL_A_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.A);
            addHotkeyAndCombo(HotKeys.CTRL_B_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.B);
            addHotkeyAndCombo(HotKeys.CTRL_C_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.C);
            addHotkeyAndCombo(HotKeys.CTRL_D_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.D);
            addHotkeyAndCombo(HotKeys.CTRL_E_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.E);
            addHotkeyAndCombo(HotKeys.CTRL_F_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.F);
            addHotkeyAndCombo(HotKeys.CTRL_G_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.G);
            addHotkeyAndCombo(HotKeys.CTRL_H_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.H);
            addHotkeyAndCombo(HotKeys.CTRL_I_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.I);
            addHotkeyAndCombo(HotKeys.CTRL_J_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.J);
            addHotkeyAndCombo(HotKeys.CTRL_K_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.K);
            addHotkeyAndCombo(HotKeys.CTRL_L_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.L);
            addHotkeyAndCombo(HotKeys.CTRL_M_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.M);
            addHotkeyAndCombo(HotKeys.CTRL_N_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.N);
            addHotkeyAndCombo(HotKeys.CTRL_O_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.O);
            addHotkeyAndCombo(HotKeys.CTRL_P_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.P);
            addHotkeyAndCombo(HotKeys.CTRL_Q_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Q);
            addHotkeyAndCombo(HotKeys.CTRL_R_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.R);
            addHotkeyAndCombo(HotKeys.CTRL_S_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.S);
            addHotkeyAndCombo(HotKeys.CTRL_T_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.T);
            addHotkeyAndCombo(HotKeys.CTRL_U_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.U);
            addHotkeyAndCombo(HotKeys.CTRL_V_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.V);
            addHotkeyAndCombo(HotKeys.CTRL_W_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.W);
            addHotkeyAndCombo(HotKeys.CTRL_X_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.X);
            addHotkeyAndCombo(HotKeys.CTRL_Y_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Y);
            addHotkeyAndCombo(HotKeys.CTRL_Z_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Z);

            addHotkeyAndCombo(HotKeys.LEFT_TRIANGLE_HOTKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.Oemcomma);
            addHotkeyAndCombo(HotKeys.RIGHT_TRIANGLE_HOTKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.OemPeriod);
            addHotkeyAndCombo(HotKeys.QUESTION_HOTKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.OemQuestion);

            addHotkeyAndCombo(HotKeys.SHIFT_SPACE_HOTKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.Space);

            addHotkeyAndCombo(HotKeys.ENTER_HOTKEY, 0, (uint)Keys.Enter);
            addHotkeyAndCombo(HotKeys.ESC_HOTKEY, 0, (uint)Keys.Escape);
            addHotkeyAndCombo(HotKeys.BS_HOTKEY, 0, (uint)Keys.Back);
            addHotkeyAndCombo(HotKeys.TAB_HOTKEY, 0, (uint)Keys.Tab);
            addHotkeyAndCombo(HotKeys.DEL_HOTKEY, 0, (uint)Keys.Delete);
            addHotkeyAndCombo(HotKeys.HOME_HOTKEY, 0, (uint)Keys.Home);
            addHotkeyAndCombo(HotKeys.END_HOTKEY, 0, (uint)Keys.End);
            addHotkeyAndCombo(HotKeys.PAGE_UP_HOTKEY, 0, (uint)Keys.PageUp);
            addHotkeyAndCombo(HotKeys.PAGE_DOWN_HOTKEY, 0, (uint)Keys.PageDown);
            addHotkeyAndCombo(HotKeys.LEFT_ARROW_HOTKEY, 0, (uint)Keys.Left);
            addHotkeyAndCombo(HotKeys.RIGHT_ARROW_HOTKEY, 0, (uint)Keys.Right);
            addHotkeyAndCombo(HotKeys.UP_ARROW_HOTKEY, 0, (uint)Keys.Up);
            addHotkeyAndCombo(HotKeys.DOWN_ARROW_HOTKEY, 0, (uint)Keys.Down);

            addHotkeyAndCombo(HotKeys.CTRL_LEFT_ARROW_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Left);
            addHotkeyAndCombo(HotKeys.CTRL_RIGHT_ARROW_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Right);
            addHotkeyAndCombo(HotKeys.CTRL_UP_ARROW_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Up);
            addHotkeyAndCombo(HotKeys.CTRL_DOWN_ARROW_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Down);

            addHotkeyAndCombo(HotKeys.CTRL_SPACE_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Space);
            addHotkeyAndCombo(HotKeys.CTRL_SHIFT_SPACE_HOTKEY, KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT, (uint)Keys.Space);
            addHotkeyAndCombo(HotKeys.CTRL_SHIFT_G_HOTKEY, KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT, (uint)Keys.G);
            addHotkeyAndCombo(HotKeys.CTRL_SHIFT_T_HOTKEY, KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT, (uint)Keys.T);

            addHotkeyAndCombo(HotKeys.CTRL_SEMICOLON_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Oemplus);
            addHotkeyAndCombo(HotKeys.CTRL_SHIFT_SEMICOLON_HOTKEY, KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT, (uint)Keys.Oemplus);
            addHotkeyAndCombo(HotKeys.CTRL_COLON_HOTKEY, KeyModifiers.MOD_CONTROL, (uint)Keys.Oem1);
            addHotkeyAndCombo(HotKeys.CTRL_SHIFT_COLON_HOTKEY, KeyModifiers.MOD_CONTROL|KeyModifiers.MOD_SHIFT, (uint)Keys.Oem1);
        }

        /// <summary>
        /// 仮想キーコードからなるキーボードファイル(106.keyとか)を読み込んで、仮想キーコードの配列を作成する<br/>
        /// 仮想キーコードはHotKeyId順に並んでいる必要がある。
        /// </summary>
        /// <returns></returns>
        public static bool ReadKeyboardFile()
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
                if (array.Length < HotKeys.NUM_STROKE_HOTKEY) {
                    logger.Warn($"No sufficient keyboard def: file={filePath}, total {array.Length} defs");
                    SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}のキー定義の数({array.Length})が不足しています。");
                    return false;
                }
            }
            logger.Info(() => $"keyboard keyNum={array.Length}, array={array.Select(x=>x.ToString("x"))._join(", ")}");

            strokeVKeys = array;

            for (int id = 0; id < HotKeys.NUM_STROKE_HOTKEY; ++id) {
                // Normal
                addHotkeyAndCombo(id, 0, GetVKeyFromHotKey(id));
                // Shifted
                addHotkeyAndCombo(HotKeys.SHIFT_FUNC_HOTKEY_ID_BASE + id, KeyModifiers.MOD_SHIFT, GetVKeyFromHotKey(id));
            }

            logger.Info("LEAVE");
            return true;
        }

    }
}

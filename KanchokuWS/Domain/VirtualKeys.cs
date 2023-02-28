using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Utils;

namespace KanchokuWS.Domain
{
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

    /// <summary>
    /// DecoderKeys と仮想キーを扱うクラス
    /// </summary>
    public static class VirtualKeys
    {
        private static Logger logger = Logger.GetLogger();

        public static int GetFuncDeckeyByName(string name)
        {
            int dk = VKeyArrayFuncKeys.GetFuncKeyIndexByName(name);
            return dk >= 0 ? DecoderKeys.FUNC_DECKEY_START + dk : -1;
        }

        //public static uint GetFuncVkeyByName(string name)
        //{
        //    if (name._toLower() == "space") {
        //        return (uint)Keys.Space;
        //    }
        //    if (name._toLower().StartsWith("vk")) {
        //        // "VKxx" のケース
        //        int vk = name._safeSubstring(2)._parseHex();
        //        if (vk > 0 && vk < 0xff) return (uint)vk;
        //    }
        //    return VKeyArrayFuncKeys.getVKey(VKeyArrayFuncKeys.GetFuncKeyIndexByName(name));
        //}

        public static uint GetAlphabetVkeyByName(string name)
        {
            if (name._safeLength() != 1) return 0;
            char ch = name[0];
            if (ch >= 'a' && ch <= 'z') ch = (char)(ch - ('a' - 'A'));
            return (ch >= 'A' && ch <= 'Z') ? (uint)Keys.A + (uint)(ch - 'A') : 0;
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
            var vkey = VKeyArrayFuncKeys.getVKey(idx);
            if (vkey > 0) decoderFuncAssignedExModKeys.Add(vkey);
        }

        /// <summary> 拡張修飾キー(space, Caps, alnum, nfer, xfer, Rshift)がデコーダ機能に割り当てられているか </summary>
        public static bool IsExModKeyIndexAssignedForDecoderFunc(uint vkey)
        {
            return decoderFuncAssignedExModKeys.Contains(vkey);
        }

        /// <summary> 漢直モードのトグルをやるキーか </summary>
        public static int GetKanchokuToggleDecKey(uint mod, uint vkey)
        {
            int kanchokuCodeWithMod = VKeyComboRepository.GetModConvertedDecKeyFromCombo(mod, vkey);
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

        // 静的コンストラクタ
        static VirtualKeys()
        {
            Initialize();
        }

        public static void Initialize()
        {
            logger.InfoH("ENTER");
            decoderFuncAssignedExModKeys = new HashSet<uint>();
            disabledExtKeys = new HashSet<string>();
            disabledExtKeyLines.Clear();
            logger.InfoH("LEAVE");
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

        private static Dictionary<string, uint> modifierKeysFromName2 = new Dictionary<string, uint>() {
            {"eisu", KeyModifiers.MOD_ALNUM },
            {"muhenkan", KeyModifiers.MOD_NFER },
            {"henkan", KeyModifiers.MOD_XFER },
        };

        private static uint getModifierKeyByName(string name)
        {
            return modifierKeysFromName._safeGet(name)._gtZeroOr(() => modifierKeysFromName2._safeGet(name));
        }

        private static string getModifierNameByKey(uint modKey)
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
            {"muhenkan", DecoderKeys.NFER_DECKEY },
            {"xfer", DecoderKeys.XFER_DECKEY },
            {"henkan", DecoderKeys.XFER_DECKEY },
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
            {"rctrl", DecoderKeys.RIGHT_CONTROL_DECKEY},
            {"rshift", DecoderKeys.RIGHT_SHIFT_DECKEY},
            {"scrlock", DecoderKeys.SCR_LOCK_DECKEY},
            {"pause", DecoderKeys.PAUSE_DECKEY},
            {"imeon", DecoderKeys.IME_ON_DECKEY},
            {"imeoff", DecoderKeys.IME_OFF_DECKEY},
            {"f1", DecoderKeys.F1_DECKEY},
            {"f01", DecoderKeys.F1_DECKEY},
            {"f2", DecoderKeys.F2_DECKEY},
            {"f02", DecoderKeys.F2_DECKEY},
            {"f3", DecoderKeys.F3_DECKEY},
            {"f03", DecoderKeys.F3_DECKEY},
            {"f4", DecoderKeys.F4_DECKEY},
            {"f04", DecoderKeys.F4_DECKEY},
            {"f5", DecoderKeys.F5_DECKEY},
            {"f05", DecoderKeys.F5_DECKEY},
            {"f6", DecoderKeys.F6_DECKEY},
            {"f06", DecoderKeys.F6_DECKEY},
            {"f7", DecoderKeys.F7_DECKEY},
            {"f07", DecoderKeys.F7_DECKEY},
            {"f8", DecoderKeys.F8_DECKEY},
            {"f08", DecoderKeys.F8_DECKEY},
            {"f9", DecoderKeys.F9_DECKEY},
            {"f09", DecoderKeys.F9_DECKEY},
            {"f10", DecoderKeys.F10_DECKEY},
            {"f11", DecoderKeys.F11_DECKEY},
            {"f12", DecoderKeys.F12_DECKEY},
            {"f13", DecoderKeys.F13_DECKEY},
            {"f14", DecoderKeys.F14_DECKEY},
            {"f15", DecoderKeys.F15_DECKEY},
            {"f16", DecoderKeys.F16_DECKEY},
            {"f17", DecoderKeys.F17_DECKEY},
            {"f18", DecoderKeys.F18_DECKEY},
            {"f19", DecoderKeys.F19_DECKEY},
            {"f20", DecoderKeys.F20_DECKEY},
            //{"f21", DecoderKeys.F21_DECKEY},
            //{"f22", DecoderKeys.F22_DECKEY},
            //{"f23", DecoderKeys.F23_DECKEY},
            //{"f24", DecoderKeys.F24_DECKEY},
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
            {"vkbshowhide", DecoderKeys.VKB_SHOW_HIDE_DECKEY},
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
            {"katakanaconversion1", DecoderKeys.TOGGLE_KATAKANA_CONVERSION1_DECKEY},
            {"katakanaconversion2", DecoderKeys.TOGGLE_KATAKANA_CONVERSION2_DECKEY},
            {"eisumodetoggle", DecoderKeys.EISU_MODE_TOGGLE_DECKEY},
            {"eisumodecancel", DecoderKeys.EISU_MODE_CANCEL_DECKEY},
            {"eisudecapitalize", DecoderKeys.EISU_DECAPITALIZE_DECKEY},
            {"romanstrokeguide", DecoderKeys.TOGGLE_ROMAN_STROKE_GUIDE_DECKEY},
            {"upperromanstrokeguide", DecoderKeys.TOGGLE_UPPER_ROMAN_STROKE_GUIDE_DECKEY},
            {"hiraganastrokeguide", DecoderKeys.TOGGLE_HIRAGANA_STROKE_GUIDE_DECKEY},
            {"exchangecodetable", DecoderKeys.EXCHANGE_CODE_TABLE_DECKEY},
            {"exchangecodetable2", DecoderKeys.EXCHANGE_CODE_TABLE2_DECKEY},
            {"selectcodetable1", DecoderKeys.SELECT_CODE_TABLE1_DECKEY},
            {"selectcodetable2", DecoderKeys.SELECT_CODE_TABLE2_DECKEY},
            {"selectcodetable3", DecoderKeys.SELECT_CODE_TABLE3_DECKEY},
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
            logger.InfoH("ENTER");
            initializeShiftPlaneForShiftModKey();
            SingleHitDefs.Clear();
            ExtModifierKeyDefs.Clear();
            disabledExtKeyLines.Clear();
            var sbCompCmds = new StringBuilder();   // 複合コマンド定義文字列
            if (filename._notEmpty()) {
                var filePath = KanchokuIni.Singleton.KanchokuDir._joinPath(filename);
                if (Settings.LoggingDecKeyInfo) logger.InfoH($"modConversion file path={filePath}");
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
                    if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"line({nl}): {rawLine}");
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

                                if (IsDisabledExtKey(modName) && modifiee._isEmpty()) {
                                    // 単打でなく、無効にされた拡張修飾キーだった
                                    if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"modName={modName} is disabled");
                                    disabledExtKeyLines.Add(rawLine);
                                    continue;
                                }

                                uint modKey = 0;
                                int modDeckey = SpecialKeysAndFunctions.GetDeckeyByName(modName);
                                int modifieeDeckey = SpecialKeysAndFunctions.GetDeckeyByName(modifiee)._gtZeroOr(modifiee._parseInt(-1));
                                if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"modName={modName}, modifiee={modifiee}, target={target}, modDeckey={modDeckey}, modifieeDeckey={modifieeDeckey})");

                                // 被修飾キーの仮想キーコード: 特殊キー名(esc, tab, ins, ...)または漢直コード(00～49)から、それに該当する仮想キーコードを得る
                                uint vkey = VKeyVsDecoderKey.getVKeyFromDecKey(modifieeDeckey);
                                if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"vkey={vkey}");
                                if (vkey == 0) {
                                    // 被修飾キーが指定されていない場合は、拡張修飾キーまたは特殊キーの単打とみなす
                                    vkey = VKeyArrayFuncKeys.GetFuncVkeyByName(modName);  
                                } else {
                                    // 被修飾キーが指定されている場合は、拡張修飾キーの修飾フラグを取得
                                    modKey = getModifierKeyByName(modName);
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

                                if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"ctrl={ctrl}, name={name}, targetDeckey={targetDeckey:x}H({targetDeckey})");

                                if (targetDeckey < 0) {
                                    // 変換先は拡張シフト面も含めた漢直コードではなかったので、特殊キーとして解析する
                                    targetDeckey = specialDecKeysFromName._safeGet(name);
                                    if (ctrl) {
                                        // Ctrlキーの登録
                                        uint decVkey = 0;
                                        if (name._safeLength() == 1 && name._ge("a") && name._le("z")) {
                                            // Ctrl-A～Ctrl-Z
                                            decVkey = CharVsVKey.GetVKeyFromFaceStr(name._toUpper());
                                            targetDeckey = DecoderKeys.DECKEY_CTRL_A + name[0] - 'a';
                                        } else if (targetDeckey >= DecoderKeys.FUNC_DECKEY_START && targetDeckey < DecoderKeys.FUNC_DECKEY_END) {
                                            // Ctrl+機能キー(特殊キー)(Ctrl+Tabとか)
                                            decVkey = VKeyVsDecoderKey.getVKeyFromDecKey(targetDeckey);
                                            targetDeckey += DecoderKeys.CTRL_FUNC_DECKEY_START - DecoderKeys.FUNC_DECKEY_START;
                                        }
                                        if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"targetDeckey={targetDeckey:x}H({targetDeckey}), ctrl={ctrl}, decVkey={decVkey:x}H({decVkey})");
                                        if (targetDeckey > 0) VKeyComboRepository.AddModifiedDeckey(targetDeckey, KeyModifiers.MOD_CONTROL, decVkey);
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
                                    } else if (!ctrl) {
                                        // Ctrl修飾なしの特殊キーだったので、漢直コードから変換テーブルに登録しておく
                                        if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"AddSpecialDeckey: name={name}, targetDeckey={targetDeckey:x}H({targetDeckey})");
                                        VKeyComboRepository.AddSpecialDeckey(name, targetDeckey);
                                    }
                                }

                                if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"modKey={modKey:x}H, vkey={vkey:x}H, targetDeckey={targetDeckey:x}H({targetDeckey}), ctrl={ctrl}, name={name}");

                                if (vkey > 0 && targetDeckey > 0) {
                                    if (modKey == 0) {
                                        if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"Single Hit");
                                        // キー単打の場合は、キーの登録だけで、拡張シフトB面の割り当てはやらない
                                        VKeyComboRepository.AddDecKeyAndCombo(targetDeckey, 0, vkey, true);  // targetDeckey から vkey(拡張修飾キー)への逆マップは不要
                                        VirtualKeys.AddExModVkeyAssignedForDecoderFuncByVkey(vkey);
                                        SingleHitDefs[modDeckey] = target;
                                    } else {
                                        if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"Extra Modifier");
                                        // 拡張修飾キー設定
                                        modCount[modKey] = modCount._safeGet(modKey) + 1;
                                        ExtModifierKeyDefs._safeGetOrNewInsert(modKey)[modifieeDeckey] = target;
                                        VKeyComboRepository.AddModConvertedDecKeyFromCombo(targetDeckey, modKey, vkey);
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
            logger.InfoH("LEAVE");
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

            uint modKey = getModifierKeyByName(items[0]);
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

            logger.Info(() => $"mod={modKey:x}H({modKey}), shiftPlane={shiftPlane}, shiftPlaneWhenOff={shiftPlaneWhenOff}");
            if (modKey != 0 && shiftPlane > 0) {
                logger.Info(() => $"shiftPlaneForShiftFuncKey[{modKey}] = {shiftPlane}, shiftPlaneForShiftFuncKeyWhenDecoderOff[{modKey}] = {shiftPlaneWhenOff}");
                ShiftPlaneForShiftModKey.Add(modKey, shiftPlane);
                ShiftPlaneForShiftModKeyWhenDecoderOff.Add(modKey, shiftPlaneWhenOff);
            }
            return true;    // OK
        }

        /// <summary>テーブルファイルor設定ダイアログで割り当てたSandSシフト面を優先する</summary>
        public static void AssignSanSPlane(int shiftPlane = 0)
        {
            logger.Info(() => $"CALLED: SandSEnabled={Settings.SandSEnabledCurrently}, SandSAssignedPlane={Settings.SandSAssignedPlane}");
            if (Settings.SandSEnabledCurrently) {
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
                var keyName = getModifierNameByKey(pair.Key);
                if (keyName._notEmpty()) {
                    dict[keyName] = GetShiftPlaneName(pair.Value);
                }
            }
            foreach (var pair in ShiftPlaneForShiftModKeyWhenDecoderOff.GetPairs()) {
                var keyName = getModifierNameByKey(pair.Key);
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
                var keyName = getModifierNameByKey(pair.Key);
                logger.Info(() => $"modKey={pair.Key}, keyName={keyName}, dict.Size={pair.Value.Count}");
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

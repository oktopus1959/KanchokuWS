using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Utils;

namespace KanchokuWS
{
    public class KeyOrFunction
    {
        public uint AttrFlag { get; private set; }
        public int DecKey { get; private set; }
        public uint ModKey { get; private set; }
        public string Name { get; private set; }
        public string ModName { get; private set; }
        public string Description { get; private set; }
        public string DetailedDesc { get; private set; }

        private string otherName0;
        private string otherName1;
        private string otherName2;

        public const uint ATTR_FUNCTION = 1;
        public const uint ATTR_MODIFIER = 2;
        public const uint ATTR_MODIFIEE = 4;
        public const uint ATTR_SINGLE = 8;
        public const uint ATTR_EXTENTED = 16;
        public const uint ATTR_SPACE = 32;
        public const uint ATTR_ASSIGNABLE = 64;

        public const uint ATTR_EXTMOD = ATTR_MODIFIER | ATTR_EXTENTED;
        public const uint ATTR_EXTSINGLE = ATTR_EXTMOD | ATTR_SINGLE;
        public const uint ATTR_SANDS = ATTR_EXTMOD | ATTR_SPACE;

        public KeyOrFunction(uint flag, int deckey, uint modKey, string name, string modName, string desc, string name1 = null, string name2 = null, string detail = null)
        {
            AttrFlag = flag;
            DecKey = deckey;
            ModKey = modKey;
            Name = name;
            ModName = modName;
            Description = desc;
            otherName0 = name._toLower();
            otherName1 = name1._toLower();
            otherName2 = name2._toLower();
            DetailedDesc = detail;
        }

        public bool IsFunction => (AttrFlag & ATTR_FUNCTION) != 0;
        public bool IsModifier => (AttrFlag & ATTR_MODIFIER) != 0;
        public bool IsModifiee => (AttrFlag & ATTR_MODIFIEE) != 0;
        public bool IsSingle => (AttrFlag & (ATTR_MODIFIEE | ATTR_SINGLE)) != 0;
        public bool IsExtModifier => (AttrFlag & ATTR_EXTMOD) == ATTR_EXTMOD;
        public bool IsAssignable => (AttrFlag & (ATTR_MODIFIEE | ATTR_SPACE | ATTR_FUNCTION | ATTR_ASSIGNABLE)) != 0;

        public bool MatchDeckey(int deckey)
        {
            return deckey == DecKey;
        }

        public bool MatchModKey(uint modkey)
        {
            return modkey == ModKey;
        }

        public bool MatchName(string name)
        {
            string key = name._toLower();
            return key == otherName0 || key == otherName1 || key == otherName2;
        }
    }

    public static class SpecialKeysAndFunctions
    {
        private static Logger logger = Logger.GetLogger();

        private static KeyOrFunction[] specialKeysAndFunctions = new KeyOrFunction[] {
            //new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.STROKE_SPACE_DECKEY, 0, "Space", "", "Space キー", "spc"),
            new KeyOrFunction(KeyOrFunction.ATTR_SANDS, DecoderKeys.STROKE_SPACE_DECKEY, KeyModifiers.MOD_SPACE, "Space", "SandS", "Space キー", "spc"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.ESC_DECKEY, 0, "Esc", "", "Escape キー", "escape"),
            new KeyOrFunction(KeyOrFunction.ATTR_SINGLE | KeyOrFunction.ATTR_ASSIGNABLE, DecoderKeys.HANZEN_DECKEY, 0, "zenkaku", "半角/全角", "半角/全角 キー", "hanzen"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.TAB_DECKEY, 0, "Tab", "", "Tab キー"),
            new KeyOrFunction(KeyOrFunction.ATTR_EXTMOD, DecoderKeys.CAPS_DECKEY, KeyModifiers.MOD_CAPS, "CapsLock", "Caps Lock", "Caps Lock キー", "caps"),
            new KeyOrFunction(KeyOrFunction.ATTR_EXTSINGLE | KeyOrFunction.ATTR_ASSIGNABLE, DecoderKeys.ALNUM_DECKEY, KeyModifiers.MOD_ALNUM, "Eisu", "英数", "英数 キー", "alphanum", "alnum"),
            new KeyOrFunction(KeyOrFunction.ATTR_EXTSINGLE | KeyOrFunction.ATTR_ASSIGNABLE, DecoderKeys.NFER_DECKEY, KeyModifiers.MOD_NFER, "Muhenkan", "無変換", "無変換 キー", "nfer"),
            new KeyOrFunction(KeyOrFunction.ATTR_EXTSINGLE | KeyOrFunction.ATTR_ASSIGNABLE, DecoderKeys.XFER_DECKEY, KeyModifiers.MOD_XFER, "Henkan", "変換", "変換 キー", "xfer"),
            new KeyOrFunction(KeyOrFunction.ATTR_SINGLE | KeyOrFunction.ATTR_ASSIGNABLE, DecoderKeys.KANA_DECKEY, 0, "Kana", "ひらがな", "ひらがな キー", "hiragana"),
            new KeyOrFunction(KeyOrFunction.ATTR_ASSIGNABLE, DecoderKeys.IME_ON_DECKEY, 0, "ImeOn", "IME オン", "IME オン キー"),
            new KeyOrFunction(KeyOrFunction.ATTR_ASSIGNABLE, DecoderKeys.IME_OFF_DECKEY, 0, "ImeOff", "IME オフ", "IME オフ キー"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.BS_DECKEY, 0, "BackSpace", "", "Back Space キー", "back", "bs"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.ENTER_DECKEY, 0, "Enter", "", "Enter キー"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.INS_DECKEY, 0, "Insert", "", "Insert キー", "ins"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.DEL_DECKEY, 0, "Delete", "", "Delete キー", "del"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.HOME_DECKEY, 0, "Home", "", "Home キー"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.END_DECKEY, 0, "End", "", "End キー"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.PAGE_UP_DECKEY, 0, "PageUp", "", "Page Up キー", "pgup"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.PAGE_DOWN_DECKEY, 0, "PageDown", "", "Page Down キー", "pgdn"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.LEFT_ARROW_DECKEY, 0, "Left", "", "← キー", "leftarrow"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.RIGHT_ARROW_DECKEY, 0, "Right", "", "→ キー", "rightarrow"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.UP_ARROW_DECKEY, 0, "Up", "", "↑ キー", "uparrow"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.DOWN_ARROW_DECKEY, 0, "Down", "", "↓ キー", "downarrow"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.SCR_LOCK_DECKEY, 0, "ScrLock", "", "Scroll Lock キー", "scrlock"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.PAUSE_DECKEY, 0, "Pause", "", "Pause キー", "pause"),
            new KeyOrFunction(KeyOrFunction.ATTR_EXTSINGLE, DecoderKeys.RIGHT_SHIFT_DECKEY, KeyModifiers.MOD_RSHIFT, "Rshift", "右シフト", "右シフト キー"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F1_DECKEY, 0, "F1", "", "F1 キー", "f01"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F2_DECKEY, 0, "F2", "", "F2 キー", "f02"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F3_DECKEY, 0, "F3", "", "F3 キー", "f03"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F4_DECKEY, 0, "F4", "", "F4 キー", "f04"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F5_DECKEY, 0, "F5", "", "F5 キー", "f05"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F6_DECKEY, 0, "F6", "", "F6 キー", "f06"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F7_DECKEY, 0, "F7", "", "F7 キー", "f07"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F8_DECKEY, 0, "F8", "", "F8 キー", "f08"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F9_DECKEY, 0, "F9", "", "F9 キー", "f09"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F10_DECKEY, 0, "F10", "", "F10 キー", "f10"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F11_DECKEY, 0, "F11", "", "F11 キー", "f11"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F12_DECKEY, 0, "F12", "", "F12 キー", "f12"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F13_DECKEY, 0, "F13", "", "F13 キー", "f13"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F14_DECKEY, 0, "F14", "", "F14 キー", "f14"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F15_DECKEY, 0, "F15", "", "F15 キー", "f15"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F16_DECKEY, 0, "F16", "", "F16 キー", "f16"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F17_DECKEY, 0, "F17", "", "F17 キー", "f17"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F18_DECKEY, 0, "F18", "", "F18 キー", "f18"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F19_DECKEY, 0, "F19", "", "F19 キー", "f19"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F20_DECKEY, 0, "F20", "", "F20 キー", "f20"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F21_DECKEY, 0, "F21", "", "F21 キー", "f21"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F22_DECKEY, 0, "F22", "", "F22 キー", "f22"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F23_DECKEY, 0, "F23", "", "F23 キー", "f23"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIEE, DecoderKeys.F24_DECKEY, 0, "F24", "", "F24 キー", "f24"),
            new KeyOrFunction(KeyOrFunction.ATTR_EXTMOD, -1, KeyModifiers.MOD_LCTRL, "lctrl", "左コントロール", "左コントロール キー"),
            new KeyOrFunction(KeyOrFunction.ATTR_EXTMOD, -1, KeyModifiers.MOD_RCTRL, "rctrl", "右コントロール", "右コントロール キー"),
            new KeyOrFunction(KeyOrFunction.ATTR_MODIFIER, -1, KeyModifiers.MOD_SHIFT, "shift", "シフト", "シフト キー"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.TOGGLE_DECKEY, 0, "ModeToggle", "", "漢直モードのトグル"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.MODE_TOGGLE_FOLLOW_CARET_DECKEY, 0, "ModeToggleFollowCaret", "", "漢直モードのトグル（カレットへの再追従）"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.ACTIVE_DECKEY, 0, "Activate", "", "漢直モードに入る"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.DEACTIVE_DECKEY, 0, "Deactivate", "", "漢直モードから出る"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.EXCHANGE_CODE_TABLE_DECKEY, 0, "ExchangeCodeTable", "", "主・副テーブルファイルを切り替える"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.SELECT_CODE_TABLE1_DECKEY, 0, "SelectCodeTable1", "", "主テーブルファイルに切り替える"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.SELECT_CODE_TABLE2_DECKEY, 0, "SelectCodeTable2", "", "副テーブルファイルに切り替える"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.KANA_TRAINING_TOGGLE_DECKEY, 0, "KanaTrainingToggle", "", "かな入力練習モードと通常モードを切り替える"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.CLEAR_STROKE_DECKEY, 0, "ClearStroke", "", "打鍵中のストロークを取り消して、第1打鍵待ちに戻る"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.FULL_ESCAPE_DECKEY, 0, "FullEscape", "", "入力途中状態をクリアし、ミニバッファ末尾にブロッカーを置く"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.UNBLOCK_DECKEY, 0, "Unblock", "", "ミニバッファ末尾のブロッカーを解除する"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.TOGGLE_BLOCKER_DECKEY, 0, "BlockerToggle", "", "ミニバッファ末尾のブロッカーを設定・解除する", "toggleblocker"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.HISTORY_NEXT_SEARCH_DECKEY, 0, "HistNext", "", "履歴を先頭から選択"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.HISTORY_PREV_SEARCH_DECKEY, 0, "HistPrev", "", "履歴を末尾から選択"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.STROKE_HELP_ROTATION_DECKEY, 0, "HelpRotate", "", "ストロークヘルプの正順回転"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.STROKE_HELP_UNROTATION_DECKEY, 0, "HelpUnrotate", "", "ストロークヘルプの逆順回転"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.DATE_STRING_ROTATION_DECKEY, 0, "DateRotate", "", "日時変換の入力(フォーマットの正順切替)"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.DATE_STRING_UNROTATION_DECKEY, 0, "DateUnrotate", "", "日時変換の入力(フォーマットの逆順切替)"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.STROKE_HELP_DECKEY, 0, "StrokeHelp", "", "最後に入力した文字のストロークヘルプ"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.BUSHU_COMP_HELP_DECKEY, 0, "BushuCompHelp", "", "部首合成ヘルプ表示"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.TOGGLE_ROMAN_STROKE_GUIDE_DECKEY, 0, "RomanStrokeGuide", "", "打鍵ガイドへのローマ字による読み入力モードのON/OFF(読み入力OFF後にガイド開始)"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.TOGGLE_UPPER_ROMAN_STROKE_GUIDE_DECKEY, 0, "UpperRomanStrokeGuide", "", "英大文字ローマ字による読み打鍵ガイドモード"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.TOGGLE_HIRAGANA_STROKE_GUIDE_DECKEY, 0, "HiraganaStrokeGuide", "", "ひらがな入力による読み打鍵ガイドモード"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.TOGGLE_ZENKAKU_CONVERSION_DECKEY, 0, "ZenkakuConversion", "", "全角変換入力モードのON/OFF"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.TOGGLE_KATAKANA_CONVERSION_DECKEY, 0, "KatakanaConversion", "", "カタカナ入力モードのON/OFF"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.LEFT_SHIFT_BLOCKER_DECKEY, 0, "LeftShiftBlocker", "", "交ぜ書きブロッカーの左移動 (交ぜ書き候補が縦列表示されている時に有効)"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.RIGHT_SHIFT_BLOCKER_DECKEY, 0, "RightShiftBlocker", "", "交ぜ書きブロッカーの右移動 (交ぜ書き候補が縦列表示されている時に有効)"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.LEFT_SHIFT_MAZE_START_POS_DECKEY, 0, "LeftShiftMazeStartPos", "", "交ぜ書き開始位置の左移動 (交ぜ書き変換の確定直後、あるいは「先頭候補を無条件に出力する」を有効にしている時に有効)"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.RIGHT_SHIFT_MAZE_START_POS_DECKEY, 0, "RightShiftMazeStartPos", "", "交ぜ書き開始位置の右移動 (交ぜ書き変換の確定直後、あるいは「先頭候補を無条件に出力する」を有効にしている時に有効)"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.COPY_SELECTION_AND_SEND_TO_DICTIONARY_DECKEY, 0, "CopyAndRegisterSelection", "", "選択されている部分をデコーダの辞書に送って登録",
                "copyselectionandsendtodictionary", null, 
                "アクティブウィンドウに Ctrl-C を送りつけて、選択されている部分をクリップボードにコピーし、\nそれをデコーダの辞書に送って登録する。\n" +
                "形式はミニバッファへのコピペによる辞書登録と同じで、履歴、交ぜ書き、連想の3通りの登録が可能"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.SHIFT_SPACE_DECKEY, 0, "ShiftSpace", "", "Shift+Space に変換"),
            new KeyOrFunction(KeyOrFunction.ATTR_FUNCTION, DecoderKeys.DIRECT_SPACE_DECKEY, 0, "DirectSpace", "", "デコーダを通さずに直接Spaceをアプリに送る"),
        };

        public static KeyOrFunction[] GetSpecialKeyOrFunctionList()
        {
            return specialKeysAndFunctions;
        }

        public static KeyOrFunction[] GetModifierKeys(Func<string, bool> namePred)
        {
            return specialKeysAndFunctions.Where(x => x.IsModifier && namePred(x.Name)).ToArray();
        }

        public static KeyOrFunction[] GetModifieeKeys()
        {
            return specialKeysAndFunctions.Where(x => x.IsModifiee).ToArray();
        }

        public static KeyOrFunction[] GetSingleHitKeys()
        {
            return specialKeysAndFunctions.Where(x => x.IsSingle).ToArray();
        }

        public static KeyOrFunction[] GetAssignableKeyOrFunctions()
        {
            return specialKeysAndFunctions.Where(x => x.IsAssignable).ToArray();
        }

        public static KeyOrFunction GetKeyOrFuncByName(string name)
        {
            return name._isEmpty() ? null : specialKeysAndFunctions._getNth(specialKeysAndFunctions._findIndex(x => x.MatchName(name)));
        }

        public static int GetDeckeyByName(string name)
        {
            return GetKeyOrFuncByName(name)?.DecKey ?? -1;
        }

        public static KeyOrFunction GetKeyOrFuncByDeckey(int deckey)
        {
            return specialKeysAndFunctions._getNth(specialKeysAndFunctions._findIndex(x => x.MatchDeckey(deckey)));
        }

        public static bool IsPlaneAssignableModKey(uint modkey)
        {
            return specialKeysAndFunctions._getNth(specialKeysAndFunctions._findIndex(x => x.MatchModKey(modkey)))?.IsExtModifier ?? false;
        }

        public static string GetKeyNameByDeckey(int deckey)
        {
            return GetKeyOrFuncByDeckey(deckey)?.Name ?? "";
        }

        /// <summary>
        /// 静的コンストラクタ
        /// </summary>
        static SpecialKeysAndFunctions()
        {
            initialize();
        }

        private static void initialize()
        {
        }
    }
}

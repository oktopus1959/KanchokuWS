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
        public bool IsFunction { get; private set; }
        public int DecKey { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string DetailedDesc { get; private set; }

        private string otherName0;
        private string otherName1;
        private string otherName2;

        public KeyOrFunction(bool bFunc, int deckey, string name, string desc, string name1 = null, string name2 = null, string detail = null)
        {
            IsFunction = bFunc;
            DecKey = deckey;
            Name = name;
            Description = desc;
            otherName0 = name._toLower();
            otherName1 = name1._toLower();
            otherName2 = name2._toLower();
            DetailedDesc = detail;
        }

        public bool MatchDeckey(int deckey)
        {
            return deckey == DecKey;
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
            new KeyOrFunction(false, DecoderKeys.STROKE_SPACE_DECKEY, "Space", "Space キー", "spc"),
            new KeyOrFunction(false, DecoderKeys.ESC_DECKEY, "Esc", "Escape キー", "escape"),
            new KeyOrFunction(false, DecoderKeys.HANZEN_DECKEY, "Zenkaku", "全角/半角", "hanzen"),
            new KeyOrFunction(false, DecoderKeys.TAB_DECKEY, "Tab", "Tab キー"),
            new KeyOrFunction(false, DecoderKeys.CAPS_DECKEY, "CapsLock", "Caps Lock", "caps"),
            new KeyOrFunction(false, DecoderKeys.ALNUM_DECKEY, "AlphaNum", "英数", "alnum", "eisu"),
            new KeyOrFunction(false, DecoderKeys.NFER_DECKEY, "Nfer", "無変換"),
            new KeyOrFunction(false, DecoderKeys.XFER_DECKEY, "Xfer", "変換"),
            new KeyOrFunction(false, DecoderKeys.KANA_DECKEY, "Kana", "ひらがな", "hiragana"),
            new KeyOrFunction(false, DecoderKeys.BS_DECKEY, "BackSpace", "Back Space キー", "back", "bs"),
            new KeyOrFunction(false, DecoderKeys.ENTER_DECKEY, "Enter", "Enter キー"),
            new KeyOrFunction(false, DecoderKeys.INS_DECKEY, "Insert", "Insert キー", "ins"),
            new KeyOrFunction(false, DecoderKeys.DEL_DECKEY, "Delete", "Delete キー", "del"),
            new KeyOrFunction(false, DecoderKeys.HOME_DECKEY, "Home", "Home キー"),
            new KeyOrFunction(false, DecoderKeys.END_DECKEY, "End", "End キー"),
            new KeyOrFunction(false, DecoderKeys.PAGE_UP_DECKEY, "PageUp", "Page Up キー", "pgup"),
            new KeyOrFunction(false, DecoderKeys.PAGE_DOWN_DECKEY, "PageDown", "Page Down キー", "pgdn"),
            new KeyOrFunction(false, DecoderKeys.LEFT_ARROW_DECKEY, "Left", "← キー", "leftarrow"),
            new KeyOrFunction(false, DecoderKeys.RIGHT_ARROW_DECKEY, "Right", "→ キー", "rightarrow"),
            new KeyOrFunction(false, DecoderKeys.UP_ARROW_DECKEY, "Up", "↑ キー", "uparrow"),
            new KeyOrFunction(false, DecoderKeys.DOWN_ARROW_DECKEY, "Down", "↓ キー", "downarrow"),
            new KeyOrFunction(false, DecoderKeys.RIGHT_SHIFT_DECKEY, "Rshift", "右シフト キー"),
            new KeyOrFunction(true, DecoderKeys.TOGGLE_DECKEY, "ModeToggle", "漢直モードのトグル"),
            new KeyOrFunction(true, DecoderKeys.MODE_TOGGLE_FOLLOW_CARET_DECKEY, "ModeToggleFollowCaret", "漢直モードのトグル（カレットへの再追従）"),
            new KeyOrFunction(true, DecoderKeys.ACTIVE_DECKEY, "Activate", "漢直モードに入る"),
            new KeyOrFunction(true, DecoderKeys.DEACTIVE_DECKEY, "Deactivate", "漢直モードから出る"),
            new KeyOrFunction(true, DecoderKeys.EXCHANGE_CODE_TABLE_DECKEY, "ExchangeCodeTable", "主・副テーブルファイルを切り替える"),
            new KeyOrFunction(true, DecoderKeys.CLEAR_STROKE_DECKEY, "ClearStroke", "打鍵中のストロークを取り消して、第1打鍵待ちに戻る"),
            new KeyOrFunction(true, DecoderKeys.FULL_ESCAPE_DECKEY, "FullEscape", "入力途中状態をクリアし、ミニバッファ末尾にブロッカーを置く"),
            new KeyOrFunction(true, DecoderKeys.UNBLOCK_DECKEY, "Unblock", "ミニバッファ末尾のブロッカーを解除する"),
            new KeyOrFunction(true, DecoderKeys.TOGGLE_BLOCKER_DECKEY, "BlockerToggle", "ミニバッファ末尾のブロッカーを設定・解除する", "toggleblocker"),
            new KeyOrFunction(true, DecoderKeys.HISTORY_NEXT_SEARCH_DECKEY, "HistNext", "履歴を先頭から選択"),
            new KeyOrFunction(true, DecoderKeys.HISTORY_PREV_SEARCH_DECKEY, "HistPrev", "履歴を末尾から選択"),
            new KeyOrFunction(true, DecoderKeys.STROKE_HELP_ROTATION_DECKEY, "HelpRotate", "ストロークヘルプの正順回転"),
            new KeyOrFunction(true, DecoderKeys.STROKE_HELP_UNROTATION_DECKEY, "HelpUnrotate", "ストロークヘルプの逆順回転"),
            new KeyOrFunction(true, DecoderKeys.DATE_STRING_ROTATION_DECKEY, "DateRotate", "日時変換の入力(フォーマットの正順切替)"),
            new KeyOrFunction(true, DecoderKeys.DATE_STRING_UNROTATION_DECKEY, "DateUnrotate", "日時変換の入力(フォーマットの逆順切替)"),
            new KeyOrFunction(true, DecoderKeys.STROKE_HELP_DECKEY, "StrokeHelp", "最後に入力した文字のストロークヘルプ"),
            new KeyOrFunction(true, DecoderKeys.BUSHU_COMP_HELP_DECKEY, "BushuCompHelp", "部首合成ヘルプ表示"),
            new KeyOrFunction(true, DecoderKeys.TOGGLE_ROMAN_STROKE_GUIDE_DECKEY, "RomanStrokeGuide", "ローマ字による読み打鍵ガイドモードのON/OFF"),
            new KeyOrFunction(true, DecoderKeys.TOGGLE_UPPER_ROMAN_STROKE_GUIDE_DECKEY, "UpperRomanStrokeGuide", "英大文字ローマ字による読み打鍵ガイドモード"),
            new KeyOrFunction(true, DecoderKeys.TOGGLE_HIRAGANA_STROKE_GUIDE_DECKEY, "HiraganaStrokeGuide", "ひらがな入力による読み打鍵ガイドモード"),
            new KeyOrFunction(true, DecoderKeys.TOGGLE_ZENKAKU_CONVERSION_DECKEY, "ZenkakuConversion", "全角変換入力モードのON/OFF"),
            new KeyOrFunction(true, DecoderKeys.TOGGLE_KATAKANA_CONVERSION_DECKEY, "KatakanaConversion", "カタカナ入力モードのON/OFF"),
            new KeyOrFunction(true, DecoderKeys.SHIFT_SPACE_DECKEY, "ShiftSpace", "Shift+Space に変換"),
            new KeyOrFunction(true, DecoderKeys.LEFT_SHIFT_BLOCKER_DECKEY, "LeftShiftBlocker", "交ぜ書きブロッカーの左移動"),
            new KeyOrFunction(true, DecoderKeys.RIGHT_SHIFT_BLOCKER_DECKEY, "RightShiftBlocker", "交ぜ書きブロッカーの右移動"),
            new KeyOrFunction(true, DecoderKeys.LEFT_SHIFT_MAZE_START_POS_DECKEY, "LeftShiftMazeStartPos", "交ぜ書き開始位置の左移動"),
            new KeyOrFunction(true, DecoderKeys.RIGHT_SHIFT_MAZE_START_POS_DECKEY, "RightShiftMazeStartPos", "交ぜ書き開始位置の右移動"),
            new KeyOrFunction(true, DecoderKeys.COPY_SELECTION_AND_SEND_TO_DICTIONARY_DECKEY, "CopyAndRegisterSelection", "選択されている部分をデコーダの辞書に送って登録",
                "copyselectionandsendtodictionary", null, 
                "アクティブウィンドウに Ctrl-C を送りつけて、選択されている部分をクリップボードにコピーし、\nそれをデコーダの辞書に送って登録する。\n" +
                "形式はミニバッファへのコピペによる辞書登録と同じで、履歴、交ぜ書き、連想の3通りの登録が可能")
        };

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

        public static KeyOrFunction[] GetSpecialKeyOrFunctionList()
        {
            return specialKeysAndFunctions;
        }

        public static uint GetModifierKeyByName(string name)
        {
            return modifierKeysFromName._safeGet(name);
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

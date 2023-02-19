using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// このファイルを修正したら make_deckey_id_def.sh で kw-uni/deckey_id_def.h を作成し直すこと

namespace KanchokuWS
{
    /// <summary>
    /// デコーダキー定義<br/>
    /// </summary>
    public static class DecoderKeys
    {
        // 通常文字ストロークに使われるDECKEYの数
        public const int NORMAL_DECKEY_NUM = 50;

        // 機能キーとして使われるDECKEYの数
        public const int FUNC_DECKEY_NUM = 50;

        // 1面あたりのDECKEYの数 (通常文字キー＋機能キー)
        public const int PLANE_DECKEY_NUM = NORMAL_DECKEY_NUM + FUNC_DECKEY_NUM;    // 100;

        // 通常ストロークに使われるDECKEYの始まり
        public const int NORMAL_DECKEY_START = 0;

        /// <summary> 機能キー (Esc, 半/全, Tab, Caps, 英数, 無変換, 変換, かな, BS, Enter, Ins, Del, Home, End, PgUp, PgDn, ↑, ↓, ←, →)の始まり</summary>
        public const int FUNC_DECKEY_START = NORMAL_DECKEY_START + NORMAL_DECKEY_NUM;

        // 機能キーとして使われるDECKEYの終わり(の次)
        public const int FUNC_DECKEY_END = FUNC_DECKEY_START + FUNC_DECKEY_NUM;

        // SHIFT修飾DECKEYの始まり
        public const int SHIFT_DECKEY_START = FUNC_DECKEY_END;

        // SHIFT修飾DECKEYの終わり(の次)
        public const int SHIFT_DECKEY_END = SHIFT_DECKEY_START + PLANE_DECKEY_NUM;

        // SHIFT_A修飾DECKEYの始まり
        public const int SHIFT_A_DECKEY_START = SHIFT_DECKEY_END;

        // 面の総数(通常面、SHIFT面、SHIFT_A～SHIFT_F面)
        public const int ALL_PLANE_NUM = 8;

        // SHIFTされたキーの総数
        public const int TOTAL_SHIFT_DECKEY_NUM = PLANE_DECKEY_NUM * (ALL_PLANE_NUM - 1);

        // SHIFTされたキーの終わり(の次)
        public const int TOTAL_SHIFT_DECKEY_END = PLANE_DECKEY_NUM + TOTAL_SHIFT_DECKEY_NUM;

        // ストロークキーの終わり(の次)
        public const int STROKE_DECKEY_END = TOTAL_SHIFT_DECKEY_END;

        // 同時打鍵用PLANE idx
        public const int COMBO_SHIFT_PLANE = ALL_PLANE_NUM;

        // 同時打鍵用修飾DECKEYの始まり
        public const int COMBO_DECKEY_START = STROKE_DECKEY_END;

        // 機能キーに対する同時打鍵用修飾DECKEYの始まり
        public const int COMBO_EX_DECKEY_START = COMBO_DECKEY_START + NORMAL_DECKEY_NUM;

        // 同時打鍵用修飾DECKEYの終わり(の次)
        public const int COMBO_DECKEY_END = COMBO_EX_DECKEY_START + FUNC_DECKEY_NUM;

        // 英数モード同時打鍵用修飾DECKEYの始まり
        public const int EISU_COMBO_DECKEY_START = COMBO_DECKEY_END;

        // 機能キーに対する英数モード同時打鍵用修飾DECKEYの始まり
        public const int EISU_COMBO_EX_DECKEY_START = EISU_COMBO_DECKEY_START + NORMAL_DECKEY_NUM;

        // 英数モード同時打鍵用修飾DECKEYの終わり(の次)
        public const int EISU_COMBO_DECKEY_END = EISU_COMBO_EX_DECKEY_START + FUNC_DECKEY_NUM;

        // Ctrl修飾DECKEYの始まり
        public const int CTRL_DECKEY_START = EISU_COMBO_DECKEY_END;

        // Ctrl修飾DECKEYの終わり(の次)
        public const int CTRL_DECKEY_END = CTRL_DECKEY_START + NORMAL_DECKEY_NUM;

        // Ctrl修飾機能DECKEYの始まり
        public const int CTRL_FUNC_DECKEY_START = CTRL_DECKEY_END;

        // Ctrl修飾機能DECKEYの終わり(の次)
        public const int CTRL_FUNC_DECKEY_END = CTRL_FUNC_DECKEY_START + FUNC_DECKEY_NUM;

        // Ctrl修飾DECKEYの数
        public const int CTRL_DECKEY_NUM = NORMAL_DECKEY_NUM + FUNC_DECKEY_NUM;

        // Ctrl+Shift修飾DECKEYの始まり
        public const int CTRL_SHIFT_DECKEY_START = CTRL_FUNC_DECKEY_END;

        // Ctrl+Shift修飾DECKEYの終わり(の次)
        public const int CTRL_SHIFT_DECKEY_END = CTRL_SHIFT_DECKEY_START + NORMAL_DECKEY_NUM;

        // Ctrl+Shift修飾機能DECKEYの始まり
        public const int CTRL_SHIFT_FUNC_DECKEY_START = CTRL_SHIFT_DECKEY_END;

        // Ctrl+Shift修飾機能DECKEYの終わり(の次)
        public const int CTRL_SHIFT_FUNC_DECKEY_END = CTRL_SHIFT_FUNC_DECKEY_START + FUNC_DECKEY_NUM;

        // Ctrl+Shift修飾DECKEYの数
        public const int CTRL_SHIFT_DECKEY_NUM = NORMAL_DECKEY_NUM + FUNC_DECKEY_NUM;

        // DECKEYの総数
        public const int TOTAL_DECKEY_NUM = COMBO_DECKEY_END + CTRL_DECKEY_NUM + CTRL_SHIFT_DECKEY_NUM;

        // 無条件でデコーダを呼び出すストロークキーに変換するためのオフセット
        public const int UNCONDITIONAL_DECKEY_OFFSET = ((CTRL_SHIFT_FUNC_DECKEY_END + 99) / 100) * 100;

        // 無条件でデコーダを呼び出すストロークキーの終わり
        public const int UNCONDITIONAL_DECKEY_END = UNCONDITIONAL_DECKEY_OFFSET + STROKE_DECKEY_END;

        // 以下、特別なキーの設定

        // スペースキーに割り当てられたDecKeyId
        public const int STROKE_SPACE_DECKEY = 40;
        public const int SHIFT_SPACE_DECKEY = SHIFT_DECKEY_START + STROKE_SPACE_DECKEY;             // Shift+Space

        // スペースキー以降ののDecKeyId
        public const int DECKEY_STROKE_41 = 41;
        public const int DECKEY_STROKE_42 = 42;
        public const int DECKEY_STROKE_43 = 43;
        public const int DECKEY_STROKE_44 = 44;
        public const int DECKEY_STROKE_45 = 45;
        public const int DECKEY_STROKE_46 = 46;
        public const int DECKEY_STROKE_47 = 47;
        public const int DECKEY_STROKE_48 = 48;

        // Ctrl-A
        public const int DECKEY_CTRL_A = CTRL_DECKEY_START;
        // Ctrl-Z
        public const int DECKEY_CTRL_Z = DECKEY_CTRL_A + 25;

        // 以下、編集機能や日付入力などの特殊機能のキーコード定義
        // 特殊機能は、それに対応するキーコードに変換されてからデコーダが呼び出される
        // デコーダ側では、ストロークテーブルを経由することなく、キーコードにより直接特殊機能を呼び出す

        // Shift が押されていたら、 上記 ID + NUM_STROKE_DECKEY の ID となる。
        public const int LEFT_TRIANGLE_DECKEY = SHIFT_DECKEY_START + 37;     // "<"
        public const int RIGHT_TRIANGLE_DECKEY = SHIFT_DECKEY_START + 38;    // ">"
        public const int QUESTION_DECKEY = SHIFT_DECKEY_START + 39;          // "?"

        // Ctrl+Shift+スペースキーに割り当てられたDecKeyId
        //public const int SHIFT_SPACE_DECKEY = SHIFT_DECKEY_START + STROKE_SPACE_DECKEY;             // Shift+Space
        //public const int CTRL_SPACE_DECKEY = CTRL_DECKEY_START + STROKE_SPACE_DECKEY;               // Ctrl+Space
        //public const int CTRL_SHIFT_SPACE_DECKEY = CTRL_SHIFT_DECKEY_START + STROKE_SPACE_DECKEY;   // Ctrl+Shift+Space

        /// <summary> 機能キー (Esc, 半/全, Tab, Caps, 英数, 無変換, 変換, かな, BS, Enter, Ins, Del, Home, End, PgUp, PgDn, ↑, ↓, ←, →)</summary>
        public const int ESC_DECKEY = FUNC_DECKEY_START;
        public const int HANZEN_DECKEY = ESC_DECKEY + 1;
        public const int TAB_DECKEY = HANZEN_DECKEY + 1;
        public const int CAPS_DECKEY = TAB_DECKEY + 1;
        public const int ALNUM_DECKEY = CAPS_DECKEY + 1;
        public const int NFER_DECKEY = ALNUM_DECKEY + 1;
        public const int XFER_DECKEY = NFER_DECKEY + 1;
        public const int KANA_DECKEY = XFER_DECKEY + 1;
        public const int BS_DECKEY = KANA_DECKEY + 1;
        public const int ENTER_DECKEY = BS_DECKEY + 1;
        public const int INS_DECKEY = ENTER_DECKEY + 1;
        public const int DEL_DECKEY = INS_DECKEY + 1;
        public const int HOME_DECKEY = DEL_DECKEY + 1;
        public const int END_DECKEY = HOME_DECKEY + 1;
        public const int PAGE_UP_DECKEY = END_DECKEY + 1;
        public const int PAGE_DOWN_DECKEY = PAGE_UP_DECKEY + 1;
        public const int UP_ARROW_DECKEY = PAGE_DOWN_DECKEY + 1;
        public const int DOWN_ARROW_DECKEY = UP_ARROW_DECKEY + 1;
        public const int LEFT_ARROW_DECKEY = DOWN_ARROW_DECKEY + 1;
        public const int RIGHT_ARROW_DECKEY = LEFT_ARROW_DECKEY + 1;
        public const int PAUSE_DECKEY = RIGHT_ARROW_DECKEY + 1;
        public const int SCR_LOCK_DECKEY = PAUSE_DECKEY + 1;
        public const int IME_ON_DECKEY = SCR_LOCK_DECKEY + 1;
        public const int IME_OFF_DECKEY = IME_ON_DECKEY + 1;
        public const int LEFT_CONTROL_DECKEY = IME_OFF_DECKEY + 1;
        public const int RIGHT_CONTROL_DECKEY = LEFT_CONTROL_DECKEY + 1;
        public const int LEFT_SHIFT_DECKEY = RIGHT_CONTROL_DECKEY + 1;
        public const int RIGHT_SHIFT_DECKEY = LEFT_SHIFT_DECKEY + 1;
        public const int F1_DECKEY = RIGHT_SHIFT_DECKEY + 1;
        public const int F2_DECKEY = F1_DECKEY + 1;
        public const int F3_DECKEY = F2_DECKEY + 1;
        public const int F4_DECKEY = F3_DECKEY + 1;
        public const int F5_DECKEY = F4_DECKEY + 1;
        public const int F6_DECKEY = F5_DECKEY + 1;
        public const int F7_DECKEY = F6_DECKEY + 1;
        public const int F8_DECKEY = F7_DECKEY + 1;
        public const int F9_DECKEY = F8_DECKEY + 1;
        public const int F10_DECKEY = F9_DECKEY + 1;
        public const int F11_DECKEY = F10_DECKEY + 1;
        public const int F12_DECKEY = F11_DECKEY + 1;
        public const int F13_DECKEY = F12_DECKEY + 1;
        public const int F14_DECKEY = F13_DECKEY + 1;
        public const int F15_DECKEY = F14_DECKEY + 1;
        public const int F16_DECKEY = F15_DECKEY + 1;
        public const int F17_DECKEY = F16_DECKEY + 1;
        public const int F18_DECKEY = F17_DECKEY + 1;
        public const int F19_DECKEY = F18_DECKEY + 1;
        public const int F20_DECKEY = F19_DECKEY + 1;
        //public const int F21_DECKEY = F20_DECKEY + 1;
        //public const int F22_DECKEY = F21_DECKEY + 1;
        //public const int F23_DECKEY = F22_DECKEY + 1;
        //public const int F24_DECKEY = F23_DECKEY + 1;
        public const int SHIFT_TAB_DECKEY = F20_DECKEY + 1;     // これは末尾である必要あり
        // これでもう満杯である

        public const int CTRL_ESC_DECKEY = ESC_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_HANZEN_DECKEY = HANZEN_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_TAB_DECKEY = TAB_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_CAPS_DECKEY = CAPS_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_ALNUM_DECKEY = ALNUM_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_NFER_DECKEY = NFER_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_XFER_DECKEY = XFER_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_KANA_DECKEY = KANA_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_BS_DECKEY = BS_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_ENTER_DECKEY = ENTER_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_INS_DECKEY = INS_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_DEL_DECKEY = DEL_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_HOME_DECKEY = HOME_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_END_DECKEY = END_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_PAGE_UP_DECKEY = PAGE_UP_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_PAGE_DOWN_DECKEY = PAGE_DOWN_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_UP_ARROW_DECKEY = UP_ARROW_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_DOWN_ARROW_DECKEY = DOWN_ARROW_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_LEFT_ARROW_DECKEY = LEFT_ARROW_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_RIGHT_ARROW_DECKEY = RIGHT_ARROW_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_PAUSE_DECKEY = PAUSE_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;
        public const int CTRL_SCR_LOCK_DECKEY = SCR_LOCK_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START;

        /// <summary> 
        /// 特殊なDECKEY<br/>
        /// UNCONDITIONAL_DECKEY_END よりも大きな値にする必要がある
        /// </summary>
        public const int SPECIAL_DECKEY_ID_BASE = 3000;

        public const int TOGGLE_DECKEY = SPECIAL_DECKEY_ID_BASE + 1;     // ON/OFF の切り替えキー
        public const int ACTIVE_DECKEY = TOGGLE_DECKEY + 1;             // ON への切り替えキー
        public const int ACTIVE2_DECKEY = ACTIVE_DECKEY + 1;            // ON への切り替えキー、その2
        public const int DEACTIVE_DECKEY = ACTIVE2_DECKEY + 1;          // OFF への切り替えキー
        public const int DEACTIVE2_DECKEY = DEACTIVE_DECKEY + 1;        // OFF への切り替えキー、その2

        public const int VKB_SHOW_HIDE_DECKEY = DEACTIVE2_DECKEY + 1;   // 仮想鍵盤の表示/非表示の切り替え

        public const int STROKE_HELP_ROTATION_DECKEY = VKB_SHOW_HIDE_DECKEY + 1;            // 打鍵ヘルプローテーションキー
        public const int STROKE_HELP_UNROTATION_DECKEY = STROKE_HELP_ROTATION_DECKEY + 1;   // 打鍵ヘルプ逆ローテーションキー

        public const int DATE_STRING_ROTATION_DECKEY = STROKE_HELP_UNROTATION_DECKEY + 1;   // 今日の日付文字列を正順に出力
        public const int DATE_STRING_UNROTATION_DECKEY = DATE_STRING_ROTATION_DECKEY + 1;   // 今日の日付文字列を逆順に出力

        public const int FULL_ESCAPE_DECKEY = DATE_STRING_UNROTATION_DECKEY + 1;        // モードを抜けたり、履歴ブロックをしたりする
        public const int UNBLOCK_DECKEY = FULL_ESCAPE_DECKEY + 1;                       // 改行コード除去と履歴ブロックの解除
        public const int TOGGLE_BLOCKER_DECKEY = UNBLOCK_DECKEY + 1;                    // 末尾のブロッカーの設定・解除のトグル

        public const int SOFT_ESCAPE_DECKEY = TOGGLE_BLOCKER_DECKEY + 1;                // ESCの出力をせずに ESC同等のキャンセル動作を行う
        public const int CLEAR_STROKE_DECKEY = SOFT_ESCAPE_DECKEY + 1;                  // 途中まで打ったストロークのクリア
        public const int COMMIT_STATE_DECKEY = CLEAR_STROKE_DECKEY + 1;                 // 状態をコミットする

        public const int HISTORY_NEXT_SEARCH_DECKEY = COMMIT_STATE_DECKEY + 1;          // 履歴検索実行&次候補選択キー
        public const int HISTORY_PREV_SEARCH_DECKEY = HISTORY_NEXT_SEARCH_DECKEY + 1;   // 履歴検索実行&前候補選択キー

        //public const int NEXT_CAND_TRIGGER_DECKEY = HISTORY_SEARCH_DECKEY + 1;      // 履歴検索開始&次の候補選択
        //public const int PREV_CAND_TRIGGER_DECKEY = NEXT_CAND_TRIGGER_DECKEY + 1;   // 履歴検索開始&前の候補選択

        public const int STROKE_HELP_DECKEY = HISTORY_PREV_SEARCH_DECKEY + 1;              // ストロークヘルプ
        public const int BUSHU_COMP_HELP_DECKEY = STROKE_HELP_DECKEY + 1;                  // 部首合成ヘルプ

        public const int TOGGLE_ZENKAKU_CONVERSION_DECKEY = BUSHU_COMP_HELP_DECKEY + 1;             // 全角変換のトグル
        public const int TOGGLE_KATAKANA_CONVERSION_DECKEY = TOGGLE_ZENKAKU_CONVERSION_DECKEY + 1;  // カタカナ変換のトグル
        public const int TOGGLE_KATAKANA_CONVERSION1_DECKEY = TOGGLE_KATAKANA_CONVERSION_DECKEY + 1;    // 主テーブルを選択してカタカナ変換のトグル
        public const int TOGGLE_KATAKANA_CONVERSION2_DECKEY = TOGGLE_KATAKANA_CONVERSION1_DECKEY + 1;   // 副テーブルを選択してカタカナ変換のトグル

        public const int TOGGLE_UPPER_ROMAN_STROKE_GUIDE_DECKEY = TOGGLE_KATAKANA_CONVERSION2_DECKEY + 1;   // 大文字ローマ字読みによる打鍵ガイドのトグル
        public const int TOGGLE_ROMAN_STROKE_GUIDE_DECKEY = TOGGLE_UPPER_ROMAN_STROKE_GUIDE_DECKEY + 1;   // ローマ字読みによる打鍵ガイドのトグル
        public const int TOGGLE_HIRAGANA_STROKE_GUIDE_DECKEY = TOGGLE_ROMAN_STROKE_GUIDE_DECKEY + 1;      // ひらがな読みによる打鍵ガイドのトグル

        public const int EXCHANGE_CODE_TABLE_DECKEY = TOGGLE_HIRAGANA_STROKE_GUIDE_DECKEY + 1;  // 漢直コード系(通常コード系と代替コード系)の入れ替え
        public const int EXCHANGE_CODE_TABLE2_DECKEY = EXCHANGE_CODE_TABLE_DECKEY + 1;          // 漢直コード系(通常コード系と代替コード系)の入れ替え; 3が選択されていたら2に切り替える
        public const int SELECT_CODE_TABLE1_DECKEY = EXCHANGE_CODE_TABLE2_DECKEY + 1;           // テーブル1を選択
        public const int SELECT_CODE_TABLE2_DECKEY = SELECT_CODE_TABLE1_DECKEY + 1;             // テーブル2を選択
        public const int SELECT_CODE_TABLE3_DECKEY = SELECT_CODE_TABLE2_DECKEY + 1;             // テーブル3を選択

        public const int LEFT_SHIFT_BLOCKER_DECKEY = SELECT_CODE_TABLE3_DECKEY + 1;         // ブロッカーを左シフトする
        public const int RIGHT_SHIFT_BLOCKER_DECKEY = LEFT_SHIFT_BLOCKER_DECKEY + 1;        // ブロッカーを右シフトする

        public const int LEFT_SHIFT_MAZE_START_POS_DECKEY = RIGHT_SHIFT_BLOCKER_DECKEY + 1;         // 交ぜ書き開始位置を左シフトする
        public const int RIGHT_SHIFT_MAZE_START_POS_DECKEY = LEFT_SHIFT_MAZE_START_POS_DECKEY + 1;  // 交ぜ書き開始位置を右シフトする

        public const int MODE_TOGGLE_FOLLOW_CARET_DECKEY = RIGHT_SHIFT_MAZE_START_POS_DECKEY + 1;   // 仮想鍵盤をカレットに再追従させて、漢直モードのトグル

        public const int COPY_SELECTION_AND_SEND_TO_DICTIONARY_DECKEY = MODE_TOGGLE_FOLLOW_CARET_DECKEY + 1; // 文字列をコピーして、それをデコーダの辞書に登録する

        public const int PSEUDO_SPACE_DECKEY = COPY_SELECTION_AND_SEND_TO_DICTIONARY_DECKEY + 1;       // 疑似スペースキー
        public const int POST_NORMAL_SHIFT_DECKEY = PSEUDO_SPACE_DECKEY + 1;                // 後置通常シフトキー
        public const int POST_PLANE_A_SHIFT_DECKEY = POST_NORMAL_SHIFT_DECKEY + 1;          // 後置拡張シフトAキー
        public const int POST_PLANE_B_SHIFT_DECKEY = POST_PLANE_A_SHIFT_DECKEY + 1;         // 後置拡張シフトBキー
        public const int POST_PLANE_C_SHIFT_DECKEY = POST_PLANE_B_SHIFT_DECKEY + 1;         // 後置拡張シフトCキー
        public const int POST_PLANE_D_SHIFT_DECKEY = POST_PLANE_C_SHIFT_DECKEY + 1;         // 後置拡張シフトDキー
        public const int POST_PLANE_E_SHIFT_DECKEY = POST_PLANE_D_SHIFT_DECKEY + 1;         // 後置拡張シフトEキー
        public const int POST_PLANE_F_SHIFT_DECKEY = POST_PLANE_E_SHIFT_DECKEY + 1;         // 後置拡張シフトFキー

        public const int DIRECT_SPACE_DECKEY = POST_PLANE_F_SHIFT_DECKEY + 1;               // デコーダを介さずに直接出力されるスペースキー
        public const int CANCEL_POST_REWRITE_DECKEY = DIRECT_SPACE_DECKEY + 1;              // 後置書き換えをキャンセルする
        public const int KANA_TRAINING_TOGGLE_DECKEY = CANCEL_POST_REWRITE_DECKEY + 1;      // かな入力練習モードのトグル
        public const int EISU_MODE_TOGGLE_DECKEY = KANA_TRAINING_TOGGLE_DECKEY + 1;         // 英数モードと通常モードのトグル
        public const int EISU_MODE_CANCEL_DECKEY = EISU_MODE_TOGGLE_DECKEY + 1;             // 英数モードをキャンセルする
        public const int EISU_DECAPITALIZE_DECKEY = EISU_MODE_CANCEL_DECKEY + 1;            // 英数モードで先頭文字を小文字化する

        public const int UNDEFINED_DECKEY = EISU_DECAPITALIZE_DECKEY + 1;                   // 未定義のキー
        public const int GLOBAL_DECKEY_ID_END = SPECIAL_DECKEY_ID_BASE + 100;

        // END_OF_AUTO_MAKE

        /// <summary>同時打鍵用修飾DECKEYの始まりの取得</summary>
        public static int GetComboDeckeyStart(bool bForKanchoku)
        {
            return bForKanchoku ? COMBO_DECKEY_START : EISU_COMBO_DECKEY_START;
        }

        /// <summary>英数モードでの同時打鍵キーか</summary>
        public static bool IsEisuComboDeckey(int deckey)
        {
            return deckey >= EISU_COMBO_DECKEY_START && deckey <= EISU_COMBO_DECKEY_END;
        }

        /// <summary>スペースキーまたは機能キーか</summary>
        public static bool IsSpaceOrFuncKey(int deckey)
        {
            int dk = deckey % PLANE_DECKEY_NUM;
            return dk == STROKE_SPACE_DECKEY || dk >= FUNC_DECKEY_START;
        }
    }
}

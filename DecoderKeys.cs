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
    /// キー番号 0 〜 48 および 50〜98 (with Shift) が漢直入力に用いるキー。<br/>
    /// 0x100以降が機能キーとなっている。<br/>
    /// </summary>
    public static class DecoderKeys
    {
        // 通常ストロークに使われるDECKEYの始まり
        public const int NORMAL_DECKEY_START = 0;

        // 通常ストロークに使われるDECKEYの数
        public const int NORMAL_DECKEY_NUM = 50;

        // 通常ストロークに使われるDECKEYの終わり(の次)
        public const int NORMAL_DECKEY_END = NORMAL_DECKEY_START + NORMAL_DECKEY_NUM;

        // SHIFT修飾DECKEYの始まり
        public const int SHIFT_DECKEY_START = NORMAL_DECKEY_END;

        // SHIFT修飾DECKEYの数
        public const int SHIFT_DECKEY_NUM = 50;

        // SHIFT修飾DECKEYの終わり(の次)
        public const int SHIFT_DECKEY_END = SHIFT_DECKEY_START + SHIFT_DECKEY_NUM;

        // SHIFT_A修飾DECKEYの始まり
        public const int SHIFT_A_DECKEY_START = SHIFT_DECKEY_END;

        // SHIFT修飾DECKEYの終わり(の次)
        public const int SHIFT_A_DECKEY_END = SHIFT_A_DECKEY_START + SHIFT_DECKEY_NUM;

        // SHIFT_B修飾DECKEYの始まり
        public const int SHIFT_B_DECKEY_START = SHIFT_A_DECKEY_END;

        // SHIFT修飾DECKEYの終わり(の次)
        public const int SHIFT_B_DECKEY_END = SHIFT_B_DECKEY_START + SHIFT_DECKEY_NUM;

        // SHIFTキーの総数
        public const int TOTAL_SHIFT_DECKEY_NUM = SHIFT_DECKEY_NUM * 3;

        // ストロークキーの数
        public const int STROKE_DECKEY_NUM = NORMAL_DECKEY_NUM + TOTAL_SHIFT_DECKEY_NUM;  // シフト面は3面ある

        // ストロークキーの終わり(の次)
        public const int STROKE_DECKEY_END = SHIFT_B_DECKEY_END;

        /// <summary> 機能キー (Esc, 半/全, Tab, Caps, 英数, 無変換, 変換, かな, BS, Enter, Ins, Del, Home, End, PgUp, PgDn, ↑, ↓, ←, →)の始まり</summary>
        public const int FUNC_DECKEY_START = STROKE_DECKEY_END;

        // 機能キーとして使われるDECKEYの数
        public const int FUNC_DECKEY_NUM = 30;

        // 機能キーとして使われるDECKEYの終わり(の次)
        public const int FUNC_DECKEY_END = FUNC_DECKEY_START + FUNC_DECKEY_NUM;

        // 修飾なしキーの数
        public const int UNMODIFIED_DECKEY_NUM = NORMAL_DECKEY_NUM + TOTAL_SHIFT_DECKEY_NUM + FUNC_DECKEY_NUM;

        // Ctrl修飾DECKEYの始まり
        public const int CTRL_DECKEY_START = FUNC_DECKEY_END;

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
        public const int TOTAL_DECKEY_NUM = UNMODIFIED_DECKEY_NUM + CTRL_DECKEY_NUM + CTRL_SHIFT_DECKEY_NUM;

        // 無条件でデコーダを呼び出すストロークキーに変換するためのオフセット
        public const int UNCONDITIONAL_DECKEY_OFFSET = ((CTRL_SHIFT_FUNC_DECKEY_END + 99) / 100) * 100;

        // 無条件でデコーダを呼び出すストロークキーの終わり
        public const int UNCONDITIONAL_DECKEY_END = UNCONDITIONAL_DECKEY_OFFSET + STROKE_DECKEY_NUM;

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
        public const int RIGHT_SHIFT_DECKEY = RIGHT_ARROW_DECKEY + 1;
        public const int SHIFT_TAB_DECKEY = RIGHT_SHIFT_DECKEY + 1;

        public const int CTRL_ESC_DECKEY = CTRL_FUNC_DECKEY_START;
        public const int CTRL_HANZEN_DECKEY = CTRL_ESC_DECKEY + 1;
        public const int CTRL_TAB_DECKEY = CTRL_HANZEN_DECKEY + 1;
        public const int CTRL_CAPS_DECKEY = CTRL_TAB_DECKEY + 1;
        public const int CTRL_ALNUM_DECKEY = CTRL_CAPS_DECKEY + 1;
        public const int CTRL_NFER_DECKEY = CTRL_ALNUM_DECKEY + 1;
        public const int CTRL_XFER_DECKEY = CTRL_NFER_DECKEY + 1;
        public const int CTRL_KANA_DECKEY = CTRL_XFER_DECKEY + 1;
        public const int CTRL_BS_DECKEY = CTRL_KANA_DECKEY + 1;
        public const int CTRL_ENTER_DECKEY = CTRL_BS_DECKEY + 1;
        public const int CTRL_INS_DECKEY = CTRL_ENTER_DECKEY + 1;
        public const int CTRL_DEL_DECKEY = CTRL_INS_DECKEY + 1;
        public const int CTRL_HOME_DECKEY = CTRL_DEL_DECKEY + 1;
        public const int CTRL_END_DECKEY = CTRL_HOME_DECKEY + 1;
        public const int CTRL_PAGE_UP_DECKEY = CTRL_END_DECKEY + 1;
        public const int CTRL_PAGE_DOWN_DECKEY = CTRL_PAGE_UP_DECKEY + 1;
        public const int CTRL_UP_ARROW_DECKEY = CTRL_PAGE_DOWN_DECKEY + 1;
        public const int CTRL_DOWN_ARROW_DECKEY = CTRL_UP_ARROW_DECKEY + 1;
        public const int CTRL_LEFT_ARROW_DECKEY = CTRL_DOWN_ARROW_DECKEY + 1;
        public const int CTRL_RIGHT_ARROW_DECKEY = CTRL_LEFT_ARROW_DECKEY + 1;

        /// <summary> 
        /// 特殊なDECKEY<br/>
        /// CTRL_SHIFT_FUNC_DECKEY_END よりも大きな値にする必要がある
        /// </summary>
        public const int SPECIAL_DECKEY_ID_BASE = 0x400;

        public const int TOGGLE_DECKEY = SPECIAL_DECKEY_ID_BASE + 1;     // ON/OFF の切り替えキー
        public const int ACTIVE_DECKEY = TOGGLE_DECKEY + 1;             // ON への切り替えキー
        public const int ACTIVE2_DECKEY = ACTIVE_DECKEY + 1;            // ON への切り替えキー、その2
        public const int DEACTIVE_DECKEY = ACTIVE2_DECKEY + 1;          // OFF への切り替えキー
        public const int DEACTIVE2_DECKEY = DEACTIVE_DECKEY + 1;        // OFF への切り替えキー、その2

        public const int STROKE_HELP_ROTATION_DECKEY = DEACTIVE2_DECKEY + 1;                // 打鍵ヘルプローテーションキー
        public const int STROKE_HELP_UNROTATION_DECKEY = STROKE_HELP_ROTATION_DECKEY + 1;   // 打鍵ヘルプ逆ローテーションキー

        public const int DATE_STRING_ROTATION_DECKEY = STROKE_HELP_UNROTATION_DECKEY + 1;   // 今日の日付文字列を正順に出力
        public const int DATE_STRING_UNROTATION_DECKEY = DATE_STRING_ROTATION_DECKEY + 1;   // 今日の日付文字列を逆順に出力

        public const int FULL_ESCAPE_DECKEY = DATE_STRING_UNROTATION_DECKEY + 1;        // モードを抜けたり、履歴ブロックをしたりする
        public const int UNBLOCK_DECKEY = FULL_ESCAPE_DECKEY + 1;                       // 改行コード除去と履歴ブロックの解除

        public const int CLEAR_STROKE_DECKEY = UNBLOCK_DECKEY + 1;                      // 途中まで打ったストロークのクリア

        public const int HISTORY_NEXT_SEARCH_DECKEY = CLEAR_STROKE_DECKEY + 1;          // 履歴検索実行&次候補選択キー
        public const int HISTORY_PREV_SEARCH_DECKEY = HISTORY_NEXT_SEARCH_DECKEY + 1;   // 履歴検索実行&前候補選択キー

        //public const int NEXT_CAND_TRIGGER_DECKEY = HISTORY_SEARCH_DECKEY + 1;      // 履歴検索開始&次の候補選択
        //public const int PREV_CAND_TRIGGER_DECKEY = NEXT_CAND_TRIGGER_DECKEY + 1;   // 履歴検索開始&前の候補選択

        public const int STROKE_HELP_DECKEY = HISTORY_PREV_SEARCH_DECKEY + 1;              // ストロークヘルプ
        public const int BUSHU_COMP_HELP_DECKEY = STROKE_HELP_DECKEY + 1;                  // 部首合成ヘルプ

        public const int TOGGLE_ZENKAKU_CONVERSION_DECKEY = BUSHU_COMP_HELP_DECKEY + 1;    // 全角変換のトグル

        public const int TOGGLE_UPPER_ROMAN_STROKE_GUIDE_DECKEY = TOGGLE_ZENKAKU_CONVERSION_DECKEY + 1;   // 大文字ローマ字読みによる打鍵ガイドのトグル
        public const int TOGGLE_ROMAN_STROKE_GUIDE_DECKEY = TOGGLE_UPPER_ROMAN_STROKE_GUIDE_DECKEY + 1;   // ローマ字読みによる打鍵ガイドのトグル
        public const int TOGGLE_HIRAGANA_STROKE_GUIDE_DECKEY = TOGGLE_ROMAN_STROKE_GUIDE_DECKEY + 1;      // ひらがな読みによる打鍵ガイドのトグル

        public const int EXCHANGE_CODE_TABLE_DECKEY = TOGGLE_HIRAGANA_STROKE_GUIDE_DECKEY + 1;     // 漢直コード系(通常コード系と代替コード系)の入れ替え

        public const int LEFT_SHIFT_BLOCKER_DECKEY = EXCHANGE_CODE_TABLE_DECKEY + 1;        // ブロッカーを左シフトする
        public const int RIGHT_SHIFT_BLOCKER_DECKEY = LEFT_SHIFT_BLOCKER_DECKEY + 1;        // ブロッカーを右シフトする

        public const int LEFT_SHIFT_MAZE_START_POS_DECKEY = RIGHT_SHIFT_BLOCKER_DECKEY + 1;         // 交ぜ書き開始位置を左シフトする
        public const int RIGHT_SHIFT_MAZE_START_POS_DECKEY = LEFT_SHIFT_MAZE_START_POS_DECKEY + 1;  // 交ぜ書き開始位置を右シフトする

        public const int PSEUDO_SPACE_DECKEY = RIGHT_SHIFT_MAZE_START_POS_DECKEY + 1;       // 疑似スペースキー
        public const int POST_NORMAL_SHIFT_DECKEY = PSEUDO_SPACE_DECKEY + 1;                // 後置通常シフトキー
        public const int POST_PLANE_A_SHIFT_DECKEY = POST_NORMAL_SHIFT_DECKEY + 1;          // 後置拡張シフトAキー
        public const int POST_PLANE_B_SHIFT_DECKEY = POST_PLANE_A_SHIFT_DECKEY + 1;         // 後置拡張シフトBキー

        public const int MODE_TOGGLE_FOLLOW_CARET_DECKEY = POST_PLANE_B_SHIFT_DECKEY + 1;   // 仮想鍵盤をカレットに再追従させて、漢直モードのトグル

        public const int COPY_SELECTION_AND_SEND_TO_DICTIONARY_DECKEY = MODE_TOGGLE_FOLLOW_CARET_DECKEY + 1; // 文字列をコピーして、それをデコーダの辞書に登録する

        public const int GLOBAL_DECKEY_ID_END = SPECIAL_DECKEY_ID_BASE + 100;

    }
}

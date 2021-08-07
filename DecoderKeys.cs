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

        // SHIFT修飾DECKEYの始まり
        public const int SHIFT_DECKEY_START = NORMAL_DECKEY_NUM;

        // SHIFT修飾DECKEYの数
        public const int SHIFT_DECKEY_NUM = 50;

        // ストロークキーの数
        public const int STROKE_DECKEY_NUM = NORMAL_DECKEY_NUM + SHIFT_DECKEY_NUM;

        // 機能キーとして使われるDECKEYの始まり
        public const int FUNC_DECKEY_START = SHIFT_DECKEY_START + SHIFT_DECKEY_NUM;

        // 機能キーとして使われるDECKEYの数
        public const int FUNC_DECKEY_NUM = 20;

        // Ctrl修飾DECKEYの始まり
        public const int CTRL_DECKEY_START = FUNC_DECKEY_START + FUNC_DECKEY_NUM;

        // Ctrl修飾機能DECKEYの始まり
        public const int CTRL_FUNC_DECKEY_START = CTRL_DECKEY_START + NORMAL_DECKEY_NUM;

        // Ctrl修飾DECKEYの数
        public const int CTRL_DECKEY_NUM = NORMAL_DECKEY_NUM + FUNC_DECKEY_NUM;

        // Ctrl+Shift修飾DECKEYの始まり
        public const int CTRL_SHIFT_DECKEY_START = CTRL_FUNC_DECKEY_START + FUNC_DECKEY_NUM;

        // Ctrl+Shift修飾機能DECKEYの始まり
        public const int CTRL_SHIFT_FUNC_DECKEY_START = CTRL_SHIFT_DECKEY_START + NORMAL_DECKEY_NUM;

        // Ctrl+Shift修飾DECKEYの数
        public const int CTRL_SHIFT_DECKEY_NUM = NORMAL_DECKEY_NUM + FUNC_DECKEY_NUM;

        // DECKEYの総数
        public const int TOTAL_DECKEY_NUM = NORMAL_DECKEY_NUM + SHIFT_DECKEY_NUM + FUNC_DECKEY_NUM + CTRL_DECKEY_NUM + CTRL_SHIFT_DECKEY_NUM;

        // 上記は、ストロークテーブルの第1打鍵に含まれる
        // ストロークテーブルの第2打鍵以降には、NORMAL と SHIFT修飾だけが含まれる
        // 履歴検索や交ぜ書きなどの状態呼び出しは、ストロークテーブルで定義する
        // つまり、DECKEYコードのままデコーダを呼び出すことにより、ストロークテーブルを経て、そこに定義された状態が呼び出される

        // 以下、編集機能や日付入力などの特殊機能のキーコード定義
        // 特殊機能は、それに対応するキーコードに変換されてからデコーダが呼び出される
        // デコーダ側では、ストロークテーブルを経由することなく、キーコードにより直接特殊機能を呼び出す

        // B, F, H, N, P
        public const int DECKEY_A = 20;
        public const int DECKEY_B = 34;
        public const int DECKEY_C = 32;
        public const int DECKEY_D = 22;
        public const int DECKEY_E = 12;
        public const int DECKEY_F = 23;
        public const int DECKEY_G = 24;
        public const int DECKEY_H = 25;
        public const int DECKEY_I = 17;
        public const int DECKEY_J = 26;
        public const int DECKEY_K = 27;
        public const int DECKEY_L = 28;
        public const int DECKEY_M = 36;
        public const int DECKEY_N = 35;
        public const int DECKEY_O = 18;
        public const int DECKEY_P = 19;
        public const int DECKEY_Q = 10;
        public const int DECKEY_R = 13;
        public const int DECKEY_S = 21;
        public const int DECKEY_T = 14;
        public const int DECKEY_U = 16;
        public const int DECKEY_V = 33;
        public const int DECKEY_W = 11;
        public const int DECKEY_X = 31;
        public const int DECKEY_Y = 15;
        public const int DECKEY_Z = 30;

        // スペースキーに割り当てられたDecKeyId
        public const int STROKE_SPACE_DECKEY = 40;

        // スペースキー以降ののDecKeyId
        public const int DECKEY_STROKE_41 = 41;
        public const int DECKEY_STROKE_42 = 42;
        public const int DECKEY_STROKE_43 = 43;
        public const int DECKEY_STROKE_44 = 44;
        public const int DECKEY_STROKE_45 = 45;
        public const int DECKEY_STROKE_46 = 46;
        public const int DECKEY_STROKE_47 = 47;
        public const int DECKEY_STROKE_48 = 48;

        // Shift が押されていたら、 上記 ID + NUM_STROKE_DECKEY の ID となる。
        public const int LEFT_TRIANGLE_DECKEY = SHIFT_DECKEY_START + 37;     // "<"
        public const int RIGHT_TRIANGLE_DECKEY = SHIFT_DECKEY_START + 38;    // ">"
        public const int QUESTION_DECKEY = SHIFT_DECKEY_START + 39;          // "?"

        // Shift+スペースキーに割り当てられたDecKeyId
        public const int SHIFT_SPACE_DECKEY = SHIFT_DECKEY_START + 40;


        // デコーダには渡されないDECKEY
        // FUNCTIONAL_DECKEY_ID_BASE よりも大きな値にする必要がある
        public const int GLOBAL_DECKEY_ID_BASE = 0x200;

        public const int TOGGLE_DECKEY = GLOBAL_DECKEY_ID_BASE + 1;     // ON/OFF の切り替えキー
        public const int ACTIVE_DECKEY = TOGGLE_DECKEY + 1;             // ON への切り替えキー
        public const int ACTIVE2_DECKEY = ACTIVE_DECKEY + 1;            // ON への切り替えキー、その2
        public const int DEACTIVE_DECKEY = ACTIVE2_DECKEY + 1;          // OFF への切り替えキー
        public const int DEACTIVE2_DECKEY = DEACTIVE_DECKEY + 1;        // OFF への切り替えキー、その2

        public const int STROKE_HELP_ROTATION_DECKEY = DEACTIVE2_DECKEY + 1;                // 打鍵ヘルプローテーションキー
        public const int STROKE_HELP_UNROTATION_DECKEY = STROKE_HELP_ROTATION_DECKEY + 1;   // 打鍵ヘルプ逆ローテーションキー

        public const int DATE_STRING_ROTATION_DECKEY = STROKE_HELP_UNROTATION_DECKEY + 1;   // 今日の日付文字列を正順に出力
        public const int DATE_STRING_UNROTATION_DECKEY = DATE_STRING_ROTATION_DECKEY + 1;   // 今日の日付文字列を逆順に出力

        public const int FULL_ESCAPE_DECKEY = DATE_STRING_UNROTATION_DECKEY + 1;    // モードを抜けたり、履歴ブロックをしたりする
        public const int UNBLOCK_DECKEY = FULL_ESCAPE_DECKEY + 1;                   // 改行コード除去と履歴ブロックの解除

        public const int NEXT_CAND_TRIGGER_DECKEY = UNBLOCK_DECKEY + 1;             // 履歴検索開始&次の候補選択
        public const int PREV_CAND_TRIGGER_DECKEY = NEXT_CAND_TRIGGER_DECKEY + 1;   // 履歴検索開始&前の候補選択

        public const int GLOBAL_DECKEY_ID_END = GLOBAL_DECKEY_ID_BASE + 100;

        // デコーダ内部で使われる Ctrlキーや特殊キーのDECKEY_IDの定義
        // 0 ～ 0xff まではデコーダのストローク用に確保
        public const int FUNCTIONAL_DECKEY_ID_BASE = 0x100;

        public const int CTRL_FUNC_DECKEY_ID_BASE = FUNCTIONAL_DECKEY_ID_BASE;
        public const int CTRL_A_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 1;
        public const int CTRL_B_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 2;
        public const int CTRL_C_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 3;
        public const int CTRL_D_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 4;
        public const int CTRL_E_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 5;
        public const int CTRL_F_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 6;
        public const int CTRL_G_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 7;
        public const int CTRL_H_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 8;
        public const int CTRL_I_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 9;
        public const int CTRL_J_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 10;
        public const int CTRL_K_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 11;
        public const int CTRL_L_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 12;
        public const int CTRL_M_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 13;
        public const int CTRL_N_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 14;
        public const int CTRL_O_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 15;
        public const int CTRL_P_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 16;
        public const int CTRL_Q_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 17;
        public const int CTRL_R_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 18;
        public const int CTRL_S_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 19;
        public const int CTRL_T_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 20;
        public const int CTRL_U_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 21;
        public const int CTRL_V_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 22;
        public const int CTRL_W_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 23;
        public const int CTRL_X_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 24;
        public const int CTRL_Y_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 25;
        public const int CTRL_Z_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 26;
        public const int CTRL_FUNC_DECKEY_ID_END = CTRL_Z_DECKEY + 1;

        public const int SPECIAL_DECKEY_ID_BASE = CTRL_FUNC_DECKEY_ID_END;

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

        public const int CTRL_SPACE_DECKEY = CTRL_DECKEY_START + STROKE_SPACE_DECKEY;            // Ctrl+Space
        public const int CTRL_SHIFT_SPACE_DECKEY = CTRL_SHIFT_DECKEY_START + STROKE_SPACE_DECKEY;           // Ctrl+Shift+Space
        //public const int CTRL_SHIFT_G_DECKEY = CTRL_SHIFT_SPACE_DECKEY + 1;         // Ctrl+Shift+G
        //public const int CTRL_SHIFT_T_DECKEY = CTRL_SHIFT_G_DECKEY + 1;             // Ctrl+Shift+T

        //public const int CTRL_SEMICOLON_DECKEY = CTRL_SHIFT_T_DECKEY + 1;           // Ctrl+;
        //public const int CTRL_SHIFT_SEMICOLON_DECKEY = CTRL_SEMICOLON_DECKEY + 1;   // Ctrl+Shift+;
        //public const int CTRL_COLON_DECKEY = CTRL_SHIFT_SEMICOLON_DECKEY + 1;       // Ctrl+:
        //public const int CTRL_SHIFT_COLON_DECKEY = CTRL_COLON_DECKEY + 1;           // Ctrl+Shift+:

        //public const int SPECIAL_DECKEY_ID_END = PREV_CAND_TRIGGER_DECKEY + 1;

        //public const int FUNCTION_DECKEY_ID_END = SPECIAL_DECKEY_ID_END;
    }
}

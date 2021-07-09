using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// このファイルを修正したら make_hotkey_id_def.sh で kw-uni/hotkey_id_def.h を作成し直すこと

namespace KanchokuWS
{
    /// <summary>
    /// ホットキー定義<br/>
    /// キー番号 0 〜 48 および 50〜98 (with Shift) が漢直入力に用いるキー。<br/>
    /// 0x100以降が機能キーとなっている。<br/>
    /// 衝突しないように気をつけること。
    /// 
    /// </summary>
    public static class HotKeys
    {
        // ストロークに使われるHOTKEYの数
        public const int NUM_STROKE_HOTKEY = 50;

        // B, F, H, N, P
        public const int HOTKEY_A = 20;
        public const int HOTKEY_B = 34;
        public const int HOTKEY_C = 32;
        public const int HOTKEY_D = 22;
        public const int HOTKEY_E = 12;
        public const int HOTKEY_F = 23;
        public const int HOTKEY_G = 24;
        public const int HOTKEY_H = 25;
        public const int HOTKEY_I = 17;
        public const int HOTKEY_J = 26;
        public const int HOTKEY_K = 27;
        public const int HOTKEY_L = 28;
        public const int HOTKEY_M = 36;
        public const int HOTKEY_N = 35;
        public const int HOTKEY_O = 18;
        public const int HOTKEY_P = 19;
        public const int HOTKEY_Q = 10;
        public const int HOTKEY_R = 13;
        public const int HOTKEY_S = 21;
        public const int HOTKEY_T = 14;
        public const int HOTKEY_U = 16;
        public const int HOTKEY_V = 33;
        public const int HOTKEY_W = 11;
        public const int HOTKEY_X = 31;
        public const int HOTKEY_Y = 15;
        public const int HOTKEY_Z = 30;

        // スペースキーに割り当てられたHotKeyId
        public const int HOTKEY_STROKE_SPACE = 40;

        // スペースキー以降ののHotKeyId
        public const int HOTKEY_STROKE_41 = 41;
        public const int HOTKEY_STROKE_42 = 42;
        public const int HOTKEY_STROKE_43 = 43;
        public const int HOTKEY_STROKE_44 = 44;
        public const int HOTKEY_STROKE_45 = 45;
        public const int HOTKEY_STROKE_46 = 46;
        public const int HOTKEY_STROKE_47 = 47;
        public const int HOTKEY_STROKE_48 = 48;
        public const int HOTKEY_STROKE_49 = 49;

        // Shift が押されていたら、 上記 ID + NUM_STROKE_HOTKEY の ID となる。

        public const int SHIFT_FUNC_HOTKEY_ID_BASE = NUM_STROKE_HOTKEY;

        public const int LEFT_TRIANGLE_HOTKEY = SHIFT_FUNC_HOTKEY_ID_BASE + 37;     // "<"
        public const int RIGHT_TRIANGLE_HOTKEY = SHIFT_FUNC_HOTKEY_ID_BASE + 38;    // ">"
        public const int QUESTION_HOTKEY = SHIFT_FUNC_HOTKEY_ID_BASE + 39;          // "?"

        // Shift+スペースキーに割り当てられたHotKeyId
        public const int SHIFT_SPACE_HOTKEY = SHIFT_FUNC_HOTKEY_ID_BASE + 40;


        // デコーダには渡されないHOTKEY
        // FUNCTIONAL_HOTKEY_ID_BASE よりも大きな値にする必要がある
        public const int GLOBAL_HOTKEY_ID_BASE = 0x200;

        public const int ACTIVE_HOTKEY = GLOBAL_HOTKEY_ID_BASE + 1;     // ON/OFF の切り替えキー
        public const int ACTIVE2_HOTKEY = ACTIVE_HOTKEY + 1;            // ON/OFF の切り替えキー、その2
        public const int INACTIVE_HOTKEY = ACTIVE2_HOTKEY + 1;          // OFF への切り替えキー
        public const int INACTIVE2_HOTKEY = INACTIVE_HOTKEY + 1;        // OFF への切り替えキー、その2
        public const int ACTIVEIME_HOTKEY = INACTIVE2_HOTKEY + 1;       // IME連動による ON/OFF の切り替え (漢直Winが従)

        public const int FULL_ESCAPE_HOTKEY = ACTIVEIME_HOTKEY + 1;     // モードを抜けたり、履歴ブロックをしたりする
        public const int UNBLOCK_HOTKEY = FULL_ESCAPE_HOTKEY + 1;       // 改行コード除去と履歴ブロックの解除

        public const int NEXT_CAND_TRIGGER_HOTKEY = UNBLOCK_HOTKEY + 1;             // 履歴検索開始&次の候補選択
        public const int PREV_CAND_TRIGGER_HOTKEY = NEXT_CAND_TRIGGER_HOTKEY + 1;   // 履歴検索開始&前の候補選択

        public const int DATE_STRING_HOTKEY1 = PREV_CAND_TRIGGER_HOTKEY + 1;    // 今日の日付文字列を出力
        public const int DATE_STRING_HOTKEY2 = DATE_STRING_HOTKEY1 + 1;         // 今日の日付文字列を出力

        public const int GLOBAL_HOTKEY_ID_END = DATE_STRING_HOTKEY2 + 1;

        // デコーダ内部で使われる Ctrlキーや特殊キーのHOTKEY_IDの定義
        // 0 ～ 0xff まではデコーダのストローク用に確保
        public const int FUNCTIONAL_HOTKEY_ID_BASE = 0x100;

        public const int CTRL_FUNC_HOTKEY_ID_BASE = FUNCTIONAL_HOTKEY_ID_BASE;
        public const int CTRL_A_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 1;
        public const int CTRL_B_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 2;
        public const int CTRL_C_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 3;
        public const int CTRL_D_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 4;
        public const int CTRL_E_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 5;
        public const int CTRL_F_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 6;
        public const int CTRL_G_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 7;
        public const int CTRL_H_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 8;
        public const int CTRL_I_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 9;
        public const int CTRL_J_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 10;
        public const int CTRL_K_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 11;
        public const int CTRL_L_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 12;
        public const int CTRL_M_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 13;
        public const int CTRL_N_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 14;
        public const int CTRL_O_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 15;
        public const int CTRL_P_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 16;
        public const int CTRL_Q_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 17;
        public const int CTRL_R_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 18;
        public const int CTRL_S_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 19;
        public const int CTRL_T_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 20;
        public const int CTRL_U_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 21;
        public const int CTRL_V_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 22;
        public const int CTRL_W_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 23;
        public const int CTRL_X_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 24;
        public const int CTRL_Y_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 25;
        public const int CTRL_Z_HOTKEY = CTRL_FUNC_HOTKEY_ID_BASE + 26;
        public const int CTRL_FUNC_HOTKEY_ID_END = CTRL_Z_HOTKEY + 1;

        public const int SPECIAL_HOTKEY_ID_BASE = CTRL_FUNC_HOTKEY_ID_END;

        public const int ENTER_HOTKEY = SPECIAL_HOTKEY_ID_BASE + 1;
        public const int ESC_HOTKEY = ENTER_HOTKEY + 1;
        public const int BS_HOTKEY = ESC_HOTKEY + 1;
        public const int TAB_HOTKEY = BS_HOTKEY + 1;
        public const int DEL_HOTKEY = TAB_HOTKEY + 1;
        public const int HOME_HOTKEY = DEL_HOTKEY + 1;
        public const int END_HOTKEY = HOME_HOTKEY + 1;
        public const int PAGE_UP_HOTKEY = END_HOTKEY + 1;
        public const int PAGE_DOWN_HOTKEY = PAGE_UP_HOTKEY + 1;
        public const int LEFT_ARROW_HOTKEY = PAGE_DOWN_HOTKEY + 1;
        public const int RIGHT_ARROW_HOTKEY = LEFT_ARROW_HOTKEY + 1;
        public const int UP_ARROW_HOTKEY = RIGHT_ARROW_HOTKEY + 1;
        public const int DOWN_ARROW_HOTKEY = UP_ARROW_HOTKEY + 1;

        public const int CTRL_LEFT_ARROW_HOTKEY = DOWN_ARROW_HOTKEY + 1;            // Ctrl+←
        public const int CTRL_RIGHT_ARROW_HOTKEY = CTRL_LEFT_ARROW_HOTKEY + 1;      // Ctrl+→
        public const int CTRL_UP_ARROW_HOTKEY = CTRL_RIGHT_ARROW_HOTKEY + 1;        // Ctrl+↑
        public const int CTRL_DOWN_ARROW_HOTKEY = CTRL_UP_ARROW_HOTKEY + 1;         // Ctrl+↓

        public const int CTRL_SPACE_HOTKEY = CTRL_DOWN_ARROW_HOTKEY + 1;            // Ctrl+Space
        public const int CTRL_SHIFT_SPACE_HOTKEY = CTRL_SPACE_HOTKEY + 1;           // Ctrl+Shift+Space
        public const int CTRL_SHIFT_G_HOTKEY = CTRL_SHIFT_SPACE_HOTKEY + 1;         // Ctrl+Shift+G
        public const int CTRL_SHIFT_T_HOTKEY = CTRL_SHIFT_G_HOTKEY + 1;             // Ctrl+Shift+T

        public const int CTRL_SEMICOLON_HOTKEY = CTRL_SHIFT_T_HOTKEY + 1;           // Ctrl+;
        public const int CTRL_SHIFT_SEMICOLON_HOTKEY = CTRL_SEMICOLON_HOTKEY + 1;   // Ctrl+Shift+;
        public const int CTRL_COLON_HOTKEY = CTRL_SHIFT_SEMICOLON_HOTKEY + 1;       // Ctrl+:
        public const int CTRL_SHIFT_COLON_HOTKEY = CTRL_COLON_HOTKEY + 1;           // Ctrl+Shift+:

        public const int SPECIAL_HOTKEY_ID_END = CTRL_SHIFT_COLON_HOTKEY + 1;

        public const int FUNCTION_HOTKEY_ID_END = SPECIAL_HOTKEY_ID_END;
    }
}

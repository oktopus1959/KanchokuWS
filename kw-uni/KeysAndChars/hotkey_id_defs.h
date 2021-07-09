// DO NOT EDIT THIS FILE!!!!
// このファイルは ./make_hotkey_id_def.sh により ./HotKeys.cs から自動的に作成されました (2021/06/29 14:57:10)
#pragma once

// ストロークに使われるHOTKEYの数
#define NUM_STROKE_HOTKEY (50)

// B, F, H, N, P
#define HOTKEY_A (20)
#define HOTKEY_B (34)
#define HOTKEY_C (32)
#define HOTKEY_D (22)
#define HOTKEY_E (12)
#define HOTKEY_F (23)
#define HOTKEY_G (24)
#define HOTKEY_H (25)
#define HOTKEY_I (17)
#define HOTKEY_J (26)
#define HOTKEY_K (27)
#define HOTKEY_L (28)
#define HOTKEY_M (36)
#define HOTKEY_N (35)
#define HOTKEY_O (18)
#define HOTKEY_P (19)
#define HOTKEY_Q (10)
#define HOTKEY_R (13)
#define HOTKEY_S (21)
#define HOTKEY_T (14)
#define HOTKEY_U (16)
#define HOTKEY_V (33)
#define HOTKEY_W (11)
#define HOTKEY_X (31)
#define HOTKEY_Y (15)
#define HOTKEY_Z (30)

// スペースキーに割り当てられたHotKeyId
#define HOTKEY_STROKE_SPACE (40)

// スペースキー以降ののHotKeyId
#define HOTKEY_STROKE_41 (41)
#define HOTKEY_STROKE_42 (42)
#define HOTKEY_STROKE_43 (43)
#define HOTKEY_STROKE_44 (44)
#define HOTKEY_STROKE_45 (45)
#define HOTKEY_STROKE_46 (46)
#define HOTKEY_STROKE_47 (47)
#define HOTKEY_STROKE_48 (48)
#define HOTKEY_STROKE_49 (49)

// Shift が押されていたら、 上記 ID + NUM_STROKE_HOTKEY の ID となる。

#define SHIFT_FUNC_HOTKEY_ID_BASE (NUM_STROKE_HOTKEY)

#define LEFT_TRIANGLE_HOTKEY (SHIFT_FUNC_HOTKEY_ID_BASE + 37)
#define RIGHT_TRIANGLE_HOTKEY (SHIFT_FUNC_HOTKEY_ID_BASE + 38)
#define QUESTION_HOTKEY (SHIFT_FUNC_HOTKEY_ID_BASE + 39)

// Shift+スペースキーに割り当てられたHotKeyId
#define SHIFT_SPACE_HOTKEY (SHIFT_FUNC_HOTKEY_ID_BASE + 40)


// デコーダには渡されないHOTKEY
// FUNCTIONAL_HOTKEY_ID_BASE よりも大きな値にする必要がある
#define GLOBAL_HOTKEY_ID_BASE (0x200)

#define ACTIVE_HOTKEY (GLOBAL_HOTKEY_ID_BASE + 1)
#define ACTIVE2_HOTKEY (ACTIVE_HOTKEY + 1)
#define INACTIVE_HOTKEY (ACTIVE2_HOTKEY + 1)
#define INACTIVE2_HOTKEY (INACTIVE_HOTKEY + 1)
#define ACTIVEIME_HOTKEY (INACTIVE2_HOTKEY + 1)

#define FULL_ESCAPE_HOTKEY (ACTIVEIME_HOTKEY + 1)
#define UNBLOCK_HOTKEY (FULL_ESCAPE_HOTKEY + 1)

#define NEXT_CAND_TRIGGER_HOTKEY (UNBLOCK_HOTKEY + 1)
#define PREV_CAND_TRIGGER_HOTKEY (NEXT_CAND_TRIGGER_HOTKEY + 1)

#define DATE_STRING_HOTKEY1 (PREV_CAND_TRIGGER_HOTKEY + 1)
#define DATE_STRING_HOTKEY2 (DATE_STRING_HOTKEY1 + 1)

#define GLOBAL_HOTKEY_ID_END (DATE_STRING_HOTKEY2 + 1)

// デコーダ内部で使われる Ctrlキーや特殊キーのHOTKEY_IDの定義
// 0 ～ 0xff まではデコーダのストローク用に確保
#define FUNCTIONAL_HOTKEY_ID_BASE (0x100)

#define CTRL_FUNC_HOTKEY_ID_BASE (FUNCTIONAL_HOTKEY_ID_BASE)
#define CTRL_A_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 1)
#define CTRL_B_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 2)
#define CTRL_C_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 3)
#define CTRL_D_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 4)
#define CTRL_E_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 5)
#define CTRL_F_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 6)
#define CTRL_G_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 7)
#define CTRL_H_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 8)
#define CTRL_I_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 9)
#define CTRL_J_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 10)
#define CTRL_K_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 11)
#define CTRL_L_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 12)
#define CTRL_M_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 13)
#define CTRL_N_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 14)
#define CTRL_O_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 15)
#define CTRL_P_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 16)
#define CTRL_Q_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 17)
#define CTRL_R_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 18)
#define CTRL_S_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 19)
#define CTRL_T_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 20)
#define CTRL_U_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 21)
#define CTRL_V_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 22)
#define CTRL_W_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 23)
#define CTRL_X_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 24)
#define CTRL_Y_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 25)
#define CTRL_Z_HOTKEY (CTRL_FUNC_HOTKEY_ID_BASE + 26)
#define CTRL_FUNC_HOTKEY_ID_END (CTRL_Z_HOTKEY + 1)

#define SPECIAL_HOTKEY_ID_BASE (CTRL_FUNC_HOTKEY_ID_END)

#define ENTER_HOTKEY (SPECIAL_HOTKEY_ID_BASE + 1)
#define ESC_HOTKEY (ENTER_HOTKEY + 1)
#define BS_HOTKEY (ESC_HOTKEY + 1)
#define TAB_HOTKEY (BS_HOTKEY + 1)
#define DEL_HOTKEY (TAB_HOTKEY + 1)
#define HOME_HOTKEY (DEL_HOTKEY + 1)
#define END_HOTKEY (HOME_HOTKEY + 1)
#define PAGE_UP_HOTKEY (END_HOTKEY + 1)
#define PAGE_DOWN_HOTKEY (PAGE_UP_HOTKEY + 1)
#define LEFT_ARROW_HOTKEY (PAGE_DOWN_HOTKEY + 1)
#define RIGHT_ARROW_HOTKEY (LEFT_ARROW_HOTKEY + 1)
#define UP_ARROW_HOTKEY (RIGHT_ARROW_HOTKEY + 1)
#define DOWN_ARROW_HOTKEY (UP_ARROW_HOTKEY + 1)

#define CTRL_LEFT_ARROW_HOTKEY (DOWN_ARROW_HOTKEY + 1)
#define CTRL_RIGHT_ARROW_HOTKEY (CTRL_LEFT_ARROW_HOTKEY + 1)
#define CTRL_UP_ARROW_HOTKEY (CTRL_RIGHT_ARROW_HOTKEY + 1)
#define CTRL_DOWN_ARROW_HOTKEY (CTRL_UP_ARROW_HOTKEY + 1)

#define CTRL_SPACE_HOTKEY (CTRL_DOWN_ARROW_HOTKEY + 1)
#define CTRL_SHIFT_SPACE_HOTKEY (CTRL_SPACE_HOTKEY + 1)
#define CTRL_SHIFT_G_HOTKEY (CTRL_SHIFT_SPACE_HOTKEY + 1)
#define CTRL_SHIFT_T_HOTKEY (CTRL_SHIFT_G_HOTKEY + 1)

#define CTRL_SEMICOLON_HOTKEY (CTRL_SHIFT_T_HOTKEY + 1)
#define CTRL_SHIFT_SEMICOLON_HOTKEY (CTRL_SEMICOLON_HOTKEY + 1)
#define CTRL_COLON_HOTKEY (CTRL_SHIFT_SEMICOLON_HOTKEY + 1)
#define CTRL_SHIFT_COLON_HOTKEY (CTRL_COLON_HOTKEY + 1)

#define SPECIAL_HOTKEY_ID_END (CTRL_SHIFT_COLON_HOTKEY + 1)

#define FUNCTION_HOTKEY_ID_END (SPECIAL_HOTKEY_ID_END)

namespace hotkey_id_defs { const wchar_t* GetHotkeyNameFromId(int id); }
#define HOTKEY_NAME_FROM_ID(id) hotkey_id_defs::GetHotkeyNameFromId(id)

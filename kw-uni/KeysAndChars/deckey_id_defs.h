// DO NOT EDIT THIS FILE!!!!
// このファイルは tools/make_deckey_id_def.sh により KanchokuWS/Domain/DecoderKeys.cs から自動的に作成されました (2023/01/03 17:18:01)
#pragma once

// 通常文字ストロークに使われるDECKEYの数
#define NORMAL_DECKEY_NUM (50)

// 機能キーとして使われるDECKEYの数
#define FUNC_DECKEY_NUM (50)

// 1面あたりのDECKEYの数 (通常文字キー＋機能キー)
#define PLANE_DECKEY_NUM (NORMAL_DECKEY_NUM + FUNC_DECKEY_NUM)

// 通常ストロークに使われるDECKEYの始まり
#define NORMAL_DECKEY_START (0)

/// <summary> 機能キー (Esc, 半/全, Tab, Caps, 英数, 無変換, 変換, かな, BS, Enter, Ins, Del, Home, End, PgUp, PgDn, ↑, ↓, ←, →)の始まり</summary>
#define FUNC_DECKEY_START (NORMAL_DECKEY_START + NORMAL_DECKEY_NUM)

// 機能キーとして使われるDECKEYの終わり(の次)
#define FUNC_DECKEY_END (FUNC_DECKEY_START + FUNC_DECKEY_NUM)

// SHIFT修飾DECKEYの始まり
#define SHIFT_DECKEY_START (FUNC_DECKEY_END)

// SHIFT修飾DECKEYの終わり(の次)
#define SHIFT_DECKEY_END (SHIFT_DECKEY_START + PLANE_DECKEY_NUM)

// SHIFT_A修飾DECKEYの始まり
#define SHIFT_A_DECKEY_START (SHIFT_DECKEY_END)

// 面の総数(通常面、SHIFT面、SHIFT_A～SHIFT_F面)
#define ALL_PLANE_NUM (8)

// SHIFTされたキーの総数
#define TOTAL_SHIFT_DECKEY_NUM (PLANE_DECKEY_NUM * (ALL_PLANE_NUM - 1))

// SHIFTされたキーの終わり(の次)
#define TOTAL_SHIFT_DECKEY_END (PLANE_DECKEY_NUM + TOTAL_SHIFT_DECKEY_NUM)

// ストロークキーの終わり(の次)
#define STROKE_DECKEY_END (TOTAL_SHIFT_DECKEY_END)

// 同時打鍵用PLANE idx
#define COMBO_SHIFT_PLANE (ALL_PLANE_NUM)

// 同時打鍵用修飾DECKEYの始まり
#define COMBO_DECKEY_START (STROKE_DECKEY_END)

// 機能キーに対する同時打鍵用修飾DECKEYの始まり
#define COMBO_EX_DECKEY_START (COMBO_DECKEY_START + NORMAL_DECKEY_NUM)

// 同時打鍵用修飾DECKEYの終わり(の次)
#define COMBO_DECKEY_END (COMBO_EX_DECKEY_START + FUNC_DECKEY_NUM)

// 英数モード同時打鍵用修飾DECKEYの始まり
#define EISU_COMBO_DECKEY_START (COMBO_DECKEY_END)

// 機能キーに対する英数モード同時打鍵用修飾DECKEYの始まり
#define EISU_COMBO_EX_DECKEY_START (EISU_COMBO_DECKEY_START + NORMAL_DECKEY_NUM)

// 英数モード同時打鍵用修飾DECKEYの終わり(の次)
#define EISU_COMBO_DECKEY_END (EISU_COMBO_EX_DECKEY_START + FUNC_DECKEY_NUM)

// Ctrl修飾DECKEYの始まり
#define CTRL_DECKEY_START (EISU_COMBO_DECKEY_END)

// Ctrl修飾DECKEYの終わり(の次)
#define CTRL_DECKEY_END (CTRL_DECKEY_START + NORMAL_DECKEY_NUM)

// Ctrl修飾機能DECKEYの始まり
#define CTRL_FUNC_DECKEY_START (CTRL_DECKEY_END)

// Ctrl修飾機能DECKEYの終わり(の次)
#define CTRL_FUNC_DECKEY_END (CTRL_FUNC_DECKEY_START + FUNC_DECKEY_NUM)

// Ctrl修飾DECKEYの数
#define CTRL_DECKEY_NUM (NORMAL_DECKEY_NUM + FUNC_DECKEY_NUM)

// Ctrl+Shift修飾DECKEYの始まり
#define CTRL_SHIFT_DECKEY_START (CTRL_FUNC_DECKEY_END)

// Ctrl+Shift修飾DECKEYの終わり(の次)
#define CTRL_SHIFT_DECKEY_END (CTRL_SHIFT_DECKEY_START + NORMAL_DECKEY_NUM)

// Ctrl+Shift修飾機能DECKEYの始まり
#define CTRL_SHIFT_FUNC_DECKEY_START (CTRL_SHIFT_DECKEY_END)

// Ctrl+Shift修飾機能DECKEYの終わり(の次)
#define CTRL_SHIFT_FUNC_DECKEY_END (CTRL_SHIFT_FUNC_DECKEY_START + FUNC_DECKEY_NUM)

// Ctrl+Shift修飾DECKEYの数
#define CTRL_SHIFT_DECKEY_NUM (NORMAL_DECKEY_NUM + FUNC_DECKEY_NUM)

// DECKEYの総数
#define TOTAL_DECKEY_NUM (COMBO_DECKEY_END + CTRL_DECKEY_NUM + CTRL_SHIFT_DECKEY_NUM)

// 無条件でデコーダを呼び出すストロークキーに変換するためのオフセット
#define UNCONDITIONAL_DECKEY_OFFSET (((CTRL_SHIFT_FUNC_DECKEY_END + 99) / 100) * 100)

// 無条件でデコーダを呼び出すストロークキーの終わり
#define UNCONDITIONAL_DECKEY_END (UNCONDITIONAL_DECKEY_OFFSET + STROKE_DECKEY_END)

// 以下、特別なキーの設定

// スペースキーに割り当てられたDecKeyId
#define STROKE_SPACE_DECKEY (40)
#define SHIFT_SPACE_DECKEY (SHIFT_DECKEY_START + STROKE_SPACE_DECKEY)

// スペースキー以降ののDecKeyId
#define DECKEY_STROKE_41 (41)
#define DECKEY_STROKE_42 (42)
#define DECKEY_STROKE_43 (43)
#define DECKEY_STROKE_44 (44)
#define DECKEY_STROKE_45 (45)
#define DECKEY_STROKE_46 (46)
#define DECKEY_STROKE_47 (47)
#define DECKEY_STROKE_48 (48)

// Ctrl-A
#define DECKEY_CTRL_A (CTRL_DECKEY_START)
// Ctrl-Z
#define DECKEY_CTRL_Z (DECKEY_CTRL_A + 25)

// 以下、編集機能や日付入力などの特殊機能のキーコード定義
// 特殊機能は、それに対応するキーコードに変換されてからデコーダが呼び出される
// デコーダ側では、ストロークテーブルを経由することなく、キーコードにより直接特殊機能を呼び出す

// Shift が押されていたら、 上記 ID + NUM_STROKE_DECKEY の ID となる。
#define LEFT_TRIANGLE_DECKEY (SHIFT_DECKEY_START + 37)
#define RIGHT_TRIANGLE_DECKEY (SHIFT_DECKEY_START + 38)
#define QUESTION_DECKEY (SHIFT_DECKEY_START + 39)

// Ctrl+Shift+スペースキーに割り当てられたDecKeyId
//#define SHIFT_SPACE_DECKEY (SHIFT_DECKEY_START + STROKE_SPACE_DECKEY)
//#define CTRL_SPACE_DECKEY (CTRL_DECKEY_START + STROKE_SPACE_DECKEY)
//#define CTRL_SHIFT_SPACE_DECKEY (CTRL_SHIFT_DECKEY_START + STROKE_SPACE_DECKEY)

/// <summary> 機能キー (Esc, 半/全, Tab, Caps, 英数, 無変換, 変換, かな, BS, Enter, Ins, Del, Home, End, PgUp, PgDn, ↑, ↓, ←, →)</summary>
#define ESC_DECKEY (FUNC_DECKEY_START)
#define HANZEN_DECKEY (ESC_DECKEY + 1)
#define TAB_DECKEY (HANZEN_DECKEY + 1)
#define CAPS_DECKEY (TAB_DECKEY + 1)
#define ALNUM_DECKEY (CAPS_DECKEY + 1)
#define NFER_DECKEY (ALNUM_DECKEY + 1)
#define XFER_DECKEY (NFER_DECKEY + 1)
#define KANA_DECKEY (XFER_DECKEY + 1)
#define BS_DECKEY (KANA_DECKEY + 1)
#define ENTER_DECKEY (BS_DECKEY + 1)
#define INS_DECKEY (ENTER_DECKEY + 1)
#define DEL_DECKEY (INS_DECKEY + 1)
#define HOME_DECKEY (DEL_DECKEY + 1)
#define END_DECKEY (HOME_DECKEY + 1)
#define PAGE_UP_DECKEY (END_DECKEY + 1)
#define PAGE_DOWN_DECKEY (PAGE_UP_DECKEY + 1)
#define UP_ARROW_DECKEY (PAGE_DOWN_DECKEY + 1)
#define DOWN_ARROW_DECKEY (UP_ARROW_DECKEY + 1)
#define LEFT_ARROW_DECKEY (DOWN_ARROW_DECKEY + 1)
#define RIGHT_ARROW_DECKEY (LEFT_ARROW_DECKEY + 1)
#define PAUSE_DECKEY (RIGHT_ARROW_DECKEY + 1)
#define SCR_LOCK_DECKEY (PAUSE_DECKEY + 1)
#define IME_ON_DECKEY (SCR_LOCK_DECKEY + 1)
#define IME_OFF_DECKEY (IME_ON_DECKEY + 1)
#define LEFT_CONTROL_DECKEY (IME_OFF_DECKEY + 1)
#define RIGHT_CONTROL_DECKEY (LEFT_CONTROL_DECKEY + 1)
#define LEFT_SHIFT_DECKEY (RIGHT_CONTROL_DECKEY + 1)
#define RIGHT_SHIFT_DECKEY (LEFT_SHIFT_DECKEY + 1)
#define F1_DECKEY (RIGHT_SHIFT_DECKEY + 1)
#define F2_DECKEY (F1_DECKEY + 1)
#define F3_DECKEY (F2_DECKEY + 1)
#define F4_DECKEY (F3_DECKEY + 1)
#define F5_DECKEY (F4_DECKEY + 1)
#define F6_DECKEY (F5_DECKEY + 1)
#define F7_DECKEY (F6_DECKEY + 1)
#define F8_DECKEY (F7_DECKEY + 1)
#define F9_DECKEY (F8_DECKEY + 1)
#define F10_DECKEY (F9_DECKEY + 1)
#define F11_DECKEY (F10_DECKEY + 1)
#define F12_DECKEY (F11_DECKEY + 1)
#define F13_DECKEY (F12_DECKEY + 1)
#define F14_DECKEY (F13_DECKEY + 1)
#define F15_DECKEY (F14_DECKEY + 1)
#define F16_DECKEY (F15_DECKEY + 1)
#define F17_DECKEY (F16_DECKEY + 1)
#define F18_DECKEY (F17_DECKEY + 1)
#define F19_DECKEY (F18_DECKEY + 1)
#define F20_DECKEY (F19_DECKEY + 1)
//#define F21_DECKEY (F20_DECKEY + 1)
//#define F22_DECKEY (F21_DECKEY + 1)
//#define F23_DECKEY (F22_DECKEY + 1)
//#define F24_DECKEY (F23_DECKEY + 1)
#define SHIFT_TAB_DECKEY (F20_DECKEY + 1)
// これでもう満杯である

#define CTRL_ESC_DECKEY (ESC_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_HANZEN_DECKEY (HANZEN_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_TAB_DECKEY (TAB_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_CAPS_DECKEY (CAPS_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_ALNUM_DECKEY (ALNUM_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_NFER_DECKEY (NFER_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_XFER_DECKEY (XFER_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_KANA_DECKEY (KANA_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_BS_DECKEY (BS_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_ENTER_DECKEY (ENTER_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_INS_DECKEY (INS_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_DEL_DECKEY (DEL_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_HOME_DECKEY (HOME_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_END_DECKEY (END_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_PAGE_UP_DECKEY (PAGE_UP_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_PAGE_DOWN_DECKEY (PAGE_DOWN_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_UP_ARROW_DECKEY (UP_ARROW_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_DOWN_ARROW_DECKEY (DOWN_ARROW_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_LEFT_ARROW_DECKEY (LEFT_ARROW_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_RIGHT_ARROW_DECKEY (RIGHT_ARROW_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_PAUSE_DECKEY (PAUSE_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)
#define CTRL_SCR_LOCK_DECKEY (SCR_LOCK_DECKEY + CTRL_FUNC_DECKEY_START - FUNC_DECKEY_START)

/// <summary> 
/// 特殊なDECKEY<br/>
/// UNCONDITIONAL_DECKEY_END よりも大きな値にする必要がある
/// </summary>
#define SPECIAL_DECKEY_ID_BASE (3000)

#define TOGGLE_DECKEY (SPECIAL_DECKEY_ID_BASE + 1)
#define ACTIVE_DECKEY (TOGGLE_DECKEY + 1)
#define ACTIVE2_DECKEY (ACTIVE_DECKEY + 1)
#define DEACTIVE_DECKEY (ACTIVE2_DECKEY + 1)
#define DEACTIVE2_DECKEY (DEACTIVE_DECKEY + 1)

#define VKB_SHOW_HIDE_DECKEY (DEACTIVE2_DECKEY + 1)

#define STROKE_HELP_ROTATION_DECKEY (VKB_SHOW_HIDE_DECKEY + 1)
#define STROKE_HELP_UNROTATION_DECKEY (STROKE_HELP_ROTATION_DECKEY + 1)

#define DATE_STRING_ROTATION_DECKEY (STROKE_HELP_UNROTATION_DECKEY + 1)
#define DATE_STRING_UNROTATION_DECKEY (DATE_STRING_ROTATION_DECKEY + 1)

#define FULL_ESCAPE_DECKEY (DATE_STRING_UNROTATION_DECKEY + 1)
#define UNBLOCK_DECKEY (FULL_ESCAPE_DECKEY + 1)
#define TOGGLE_BLOCKER_DECKEY (UNBLOCK_DECKEY + 1)

#define SOFT_ESCAPE_DECKEY (TOGGLE_BLOCKER_DECKEY + 1)
#define CLEAR_STROKE_DECKEY (SOFT_ESCAPE_DECKEY + 1)
#define COMMIT_STATE_DECKEY (CLEAR_STROKE_DECKEY + 1)

#define HISTORY_NEXT_SEARCH_DECKEY (COMMIT_STATE_DECKEY + 1)
#define HISTORY_PREV_SEARCH_DECKEY (HISTORY_NEXT_SEARCH_DECKEY + 1)

//#define NEXT_CAND_TRIGGER_DECKEY (HISTORY_SEARCH_DECKEY + 1)
//#define PREV_CAND_TRIGGER_DECKEY (NEXT_CAND_TRIGGER_DECKEY + 1)

#define STROKE_HELP_DECKEY (HISTORY_PREV_SEARCH_DECKEY + 1)
#define BUSHU_COMP_HELP_DECKEY (STROKE_HELP_DECKEY + 1)

#define TOGGLE_ZENKAKU_CONVERSION_DECKEY (BUSHU_COMP_HELP_DECKEY + 1)
#define TOGGLE_KATAKANA_CONVERSION_DECKEY (TOGGLE_ZENKAKU_CONVERSION_DECKEY + 1)
#define TOGGLE_KATAKANA_CONVERSION1_DECKEY (TOGGLE_KATAKANA_CONVERSION_DECKEY + 1)
#define TOGGLE_KATAKANA_CONVERSION2_DECKEY (TOGGLE_KATAKANA_CONVERSION1_DECKEY + 1)

#define TOGGLE_UPPER_ROMAN_STROKE_GUIDE_DECKEY (TOGGLE_KATAKANA_CONVERSION2_DECKEY + 1)
#define TOGGLE_ROMAN_STROKE_GUIDE_DECKEY (TOGGLE_UPPER_ROMAN_STROKE_GUIDE_DECKEY + 1)
#define TOGGLE_HIRAGANA_STROKE_GUIDE_DECKEY (TOGGLE_ROMAN_STROKE_GUIDE_DECKEY + 1)

#define EXCHANGE_CODE_TABLE_DECKEY (TOGGLE_HIRAGANA_STROKE_GUIDE_DECKEY + 1)
#define EXCHANGE_CODE_TABLE2_DECKEY (EXCHANGE_CODE_TABLE_DECKEY + 1)
#define SELECT_CODE_TABLE1_DECKEY (EXCHANGE_CODE_TABLE2_DECKEY + 1)
#define SELECT_CODE_TABLE2_DECKEY (SELECT_CODE_TABLE1_DECKEY + 1)
#define SELECT_CODE_TABLE3_DECKEY (SELECT_CODE_TABLE2_DECKEY + 1)

#define LEFT_SHIFT_BLOCKER_DECKEY (SELECT_CODE_TABLE3_DECKEY + 1)
#define RIGHT_SHIFT_BLOCKER_DECKEY (LEFT_SHIFT_BLOCKER_DECKEY + 1)

#define LEFT_SHIFT_MAZE_START_POS_DECKEY (RIGHT_SHIFT_BLOCKER_DECKEY + 1)
#define RIGHT_SHIFT_MAZE_START_POS_DECKEY (LEFT_SHIFT_MAZE_START_POS_DECKEY + 1)

#define MODE_TOGGLE_FOLLOW_CARET_DECKEY (RIGHT_SHIFT_MAZE_START_POS_DECKEY + 1)

#define COPY_SELECTION_AND_SEND_TO_DICTIONARY_DECKEY (MODE_TOGGLE_FOLLOW_CARET_DECKEY + 1)

#define PSEUDO_SPACE_DECKEY (COPY_SELECTION_AND_SEND_TO_DICTIONARY_DECKEY + 1)
#define POST_NORMAL_SHIFT_DECKEY (PSEUDO_SPACE_DECKEY + 1)
#define POST_PLANE_A_SHIFT_DECKEY (POST_NORMAL_SHIFT_DECKEY + 1)
#define POST_PLANE_B_SHIFT_DECKEY (POST_PLANE_A_SHIFT_DECKEY + 1)
#define POST_PLANE_C_SHIFT_DECKEY (POST_PLANE_B_SHIFT_DECKEY + 1)
#define POST_PLANE_D_SHIFT_DECKEY (POST_PLANE_C_SHIFT_DECKEY + 1)
#define POST_PLANE_E_SHIFT_DECKEY (POST_PLANE_D_SHIFT_DECKEY + 1)
#define POST_PLANE_F_SHIFT_DECKEY (POST_PLANE_E_SHIFT_DECKEY + 1)

#define DIRECT_SPACE_DECKEY (POST_PLANE_F_SHIFT_DECKEY + 1)
#define CANCEL_POST_REWRITE_DECKEY (DIRECT_SPACE_DECKEY + 1)
#define KANA_TRAINING_TOGGLE_DECKEY (CANCEL_POST_REWRITE_DECKEY + 1)
#define EISU_MODE_TOGGLE_DECKEY (KANA_TRAINING_TOGGLE_DECKEY + 1)
#define EISU_MODE_CANCEL_DECKEY (EISU_MODE_TOGGLE_DECKEY + 1)
#define EISU_DECAPITALIZE_DECKEY (EISU_MODE_CANCEL_DECKEY + 1)

#define GLOBAL_DECKEY_ID_END (SPECIAL_DECKEY_ID_BASE + 100)

// END_OF_AUTO_MAKE

namespace deckey_id_defs { const wchar_t* GetDeckeyNameFromId(int id); }
#define DECKEY_NAME_FROM_ID(id) deckey_id_defs::GetDeckeyNameFromId(id)

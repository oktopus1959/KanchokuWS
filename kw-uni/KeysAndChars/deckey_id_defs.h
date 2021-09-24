// DO NOT EDIT THIS FILE!!!!
// このファイルは ./make_deckey_id_def.sh により ./DecoderKeys.cs から自動的に作成されました (2021/09/24 11:36:27)
#pragma once

// 通常ストロークに使われるDECKEYの始まり
#define NORMAL_DECKEY_START (0)

// 通常ストロークに使われるDECKEYの数
#define NORMAL_DECKEY_NUM (50)

// 通常ストロークに使われるDECKEYの終わり(の次)
#define NORMAL_DECKEY_END (NORMAL_DECKEY_START + NORMAL_DECKEY_NUM)

// SHIFT修飾DECKEYの始まり
#define SHIFT_DECKEY_START (NORMAL_DECKEY_END)

// SHIFT修飾DECKEYの数
#define SHIFT_DECKEY_NUM (50)

// SHIFT修飾DECKEYの終わり(の次)
#define SHIFT_DECKEY_END (SHIFT_DECKEY_START + SHIFT_DECKEY_NUM)

// ストロークキーの数
#define STROKE_DECKEY_NUM (NORMAL_DECKEY_NUM + SHIFT_DECKEY_NUM)

// ストロークキーの終わり(の次)
#define STROKE_DECKEY_END (SHIFT_DECKEY_END)

// 機能キーとして使われるDECKEYの始まり
#define FUNC_DECKEY_START (STROKE_DECKEY_END)

// 機能キーとして使われるDECKEYの数
#define FUNC_DECKEY_NUM (20)

// 機能キーとして使われるDECKEYの終わり(の次)
#define FUNC_DECKEY_END (FUNC_DECKEY_START + FUNC_DECKEY_NUM)

// 修飾なしキーの数
#define UNMODIFIED_DECKEY_NUM (NORMAL_DECKEY_NUM + SHIFT_DECKEY_NUM + FUNC_DECKEY_NUM)

// Ctrl修飾DECKEYの始まり
#define CTRL_DECKEY_START (FUNC_DECKEY_END)

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
#define TOTAL_DECKEY_NUM (UNMODIFIED_DECKEY_NUM + CTRL_DECKEY_NUM + CTRL_SHIFT_DECKEY_NUM)

// 上記は、ストロークテーブルの第1打鍵に含まれる
// ストロークテーブルの第2打鍵以降には、NORMAL と SHIFT修飾だけが含まれる
// 履歴検索や交ぜ書きなどの状態呼び出しは、ストロークテーブルで定義する
// つまり、DECKEYコードのままデコーダを呼び出すことにより、ストロークテーブルを経て、そこに定義された状態が呼び出される

// スペースキーに割り当てられたDecKeyId
#define STROKE_SPACE_DECKEY (40)

// スペースキー以降ののDecKeyId
#define DECKEY_STROKE_41 (41)
#define DECKEY_STROKE_42 (42)
#define DECKEY_STROKE_43 (43)
#define DECKEY_STROKE_44 (44)
#define DECKEY_STROKE_45 (45)
#define DECKEY_STROKE_46 (46)
#define DECKEY_STROKE_47 (47)
#define DECKEY_STROKE_48 (48)

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

#define CTRL_ESC_DECKEY (CTRL_FUNC_DECKEY_START)
#define CTRL_HANZEN_DECKEY (CTRL_ESC_DECKEY + 1)
#define CTRL_TAB_DECKEY (CTRL_HANZEN_DECKEY + 1)
#define CTRL_CAPS_DECKEY (CTRL_TAB_DECKEY + 1)
#define CTRL_ALNUM_DECKEY (CTRL_CAPS_DECKEY + 1)
#define CTRL_NFER_DECKEY (CTRL_ALNUM_DECKEY + 1)
#define CTRL_XFER_DECKEY (CTRL_NFER_DECKEY + 1)
#define CTRL_KANA_DECKEY (CTRL_XFER_DECKEY + 1)
#define CTRL_BS_DECKEY (CTRL_KANA_DECKEY + 1)
#define CTRL_ENTER_DECKEY (CTRL_BS_DECKEY + 1)
#define CTRL_INS_DECKEY (CTRL_ENTER_DECKEY + 1)
#define CTRL_DEL_DECKEY (CTRL_INS_DECKEY + 1)
#define CTRL_HOME_DECKEY (CTRL_DEL_DECKEY + 1)
#define CTRL_END_DECKEY (CTRL_HOME_DECKEY + 1)
#define CTRL_PAGE_UP_DECKEY (CTRL_END_DECKEY + 1)
#define CTRL_PAGE_DOWN_DECKEY (CTRL_PAGE_UP_DECKEY + 1)
#define CTRL_UP_ARROW_DECKEY (CTRL_PAGE_DOWN_DECKEY + 1)
#define CTRL_DOWN_ARROW_DECKEY (CTRL_UP_ARROW_DECKEY + 1)
#define CTRL_LEFT_ARROW_DECKEY (CTRL_DOWN_ARROW_DECKEY + 1)
#define CTRL_RIGHT_ARROW_DECKEY (CTRL_LEFT_ARROW_DECKEY + 1)

// デコーダには渡されないDECKEY
// FUNCTIONAL_DECKEY_ID_BASE よりも大きな値にする必要がある
#define GLOBAL_DECKEY_ID_BASE (0x200)

#define TOGGLE_DECKEY (GLOBAL_DECKEY_ID_BASE + 1)
#define ACTIVE_DECKEY (TOGGLE_DECKEY + 1)
#define ACTIVE2_DECKEY (ACTIVE_DECKEY + 1)
#define DEACTIVE_DECKEY (ACTIVE2_DECKEY + 1)
#define DEACTIVE2_DECKEY (DEACTIVE_DECKEY + 1)

#define STROKE_HELP_ROTATION_DECKEY (DEACTIVE2_DECKEY + 1)
#define STROKE_HELP_UNROTATION_DECKEY (STROKE_HELP_ROTATION_DECKEY + 1)

#define DATE_STRING_ROTATION_DECKEY (STROKE_HELP_UNROTATION_DECKEY + 1)
#define DATE_STRING_UNROTATION_DECKEY (DATE_STRING_ROTATION_DECKEY + 1)

#define FULL_ESCAPE_DECKEY (DATE_STRING_UNROTATION_DECKEY + 1)
#define UNBLOCK_DECKEY (FULL_ESCAPE_DECKEY + 1)

#define HISTORY_NEXT_SEARCH_DECKEY (UNBLOCK_DECKEY + 1)
#define HISTORY_PREV_SEARCH_DECKEY (HISTORY_NEXT_SEARCH_DECKEY + 1)

//#define NEXT_CAND_TRIGGER_DECKEY (HISTORY_SEARCH_DECKEY + 1)
//#define PREV_CAND_TRIGGER_DECKEY (NEXT_CAND_TRIGGER_DECKEY + 1)

#define BUSHU_COMP_HELP (HISTORY_PREV_SEARCH_DECKEY + 1)

#define TOGGLE_UPPER_ROMAN_STROKE_GUIDE (BUSHU_COMP_HELP + 1)
#define TOGGLE_ROMAN_STROKE_GUIDE (TOGGLE_UPPER_ROMAN_STROKE_GUIDE + 1)

#define GLOBAL_DECKEY_ID_END (GLOBAL_DECKEY_ID_BASE + 100)


namespace deckey_id_defs { const wchar_t* GetDeckeyNameFromId(int id); }
#define DECKEY_NAME_FROM_ID(id) deckey_id_defs::GetDeckeyNameFromId(id)

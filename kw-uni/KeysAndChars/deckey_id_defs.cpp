// DO NOT EDIT THIS FILE!!!!
// このファイルは ./make_deckey_id_def.sh により ./DecoderKeys.cs から自動的に作成されました (2021/11/27 13:34:54)

#include "string_type.h"
#include "deckey_id_defs.h"

namespace deckey_id_defs {

    std::map<int, const wchar_t*> deckeyId_name_map = {
        {(SPECIAL_DECKEY_ID_BASE + 1), _T("TOGGLE_DECKEY")},
        {(TOGGLE_DECKEY + 1), _T("ACTIVE_DECKEY")},
        {(ACTIVE_DECKEY + 1), _T("ACTIVE2_DECKEY")},
        {(ACTIVE2_DECKEY + 1), _T("DEACTIVE_DECKEY")},
        {(DEACTIVE_DECKEY + 1), _T("DEACTIVE2_DECKEY")},
        {(DEACTIVE2_DECKEY + 1), _T("STROKE_HELP_ROTATION_DECKEY")},
        {(STROKE_HELP_ROTATION_DECKEY + 1), _T("STROKE_HELP_UNROTATION_DECKEY")},
        {(STROKE_HELP_UNROTATION_DECKEY + 1), _T("DATE_STRING_ROTATION_DECKEY")},
        {(DATE_STRING_ROTATION_DECKEY + 1), _T("DATE_STRING_UNROTATION_DECKEY")},
        {(DATE_STRING_UNROTATION_DECKEY + 1), _T("FULL_ESCAPE_DECKEY")},
        {(FULL_ESCAPE_DECKEY + 1), _T("UNBLOCK_DECKEY")},
        {(UNBLOCK_DECKEY + 1), _T("HISTORY_NEXT_SEARCH_DECKEY")},
        {(HISTORY_NEXT_SEARCH_DECKEY + 1), _T("HISTORY_PREV_SEARCH_DECKEY")},
        //public const int NEXT_CAND_TRIGGER_DECKEY = HISTORY_SEARCH_DECKEY + 1;      // 履歴検索開始&次の候補選択
        //public const int PREV_CAND_TRIGGER_DECKEY = NEXT_CAND_TRIGGER_DECKEY + 1;   // 履歴検索開始&前の候補選択
        {(HISTORY_PREV_SEARCH_DECKEY + 1), _T("BUSHU_COMP_HELP")},
        {(TOGGLE_HIRAGANA_STROKE_GUIDE + 1), _T("EXCHANGE_CODE_TABLE_DECKEY")},
        {(EXCHANGE_CODE_TABLE_DECKEY + 1), _T("LEFT_SHIFT_BLOCKER_DECKEY")},
        {(LEFT_SHIFT_BLOCKER_DECKEY + 1), _T("RIGHT_SHIFT_BLOCKER_DECKEY")},
        {(RIGHT_SHIFT_BLOCKER_DECKEY + 1), _T("LEFT_SHIFT_MAZE_START_POS_DECKEY")},
        {(LEFT_SHIFT_MAZE_START_POS_DECKEY + 1), _T("RIGHT_SHIFT_MAZE_START_POS_DECKEY")},
        {(RIGHT_SHIFT_MAZE_START_POS_DECKEY + 1), _T("PSEUDO_SPACE_DECKEY")},
        {(PSEUDO_SPACE_DECKEY + 1), _T("MODE_TOGGLE_FOLLOW_CARET_DECKEY")},
    };

    const wchar_t* GetDeckeyNameFromId(int id) {
        auto iter = deckeyId_name_map.find(id);
        return iter != deckeyId_name_map.end() ? iter->second : _T("?");
    }

} // namespace deckey_id_defs

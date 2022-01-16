// DO NOT EDIT THIS FILE!!!!
// このファイルは ./make_deckey_id_def.sh により ./DecoderKeys.cs から自動的に作成されました (2022/01/16 16:21:19)

#include "string_type.h"
#include "deckey_id_defs.h"

namespace deckey_id_defs {

    std::map<int, const wchar_t*> deckeyId_name_map = {
        {(40), _T("STROKE_SPACE")},
        {(SHIFT_DECKEY_START + STROKE_SPACE_DECKEY), _T("SHIFT_SPACE")},
        {(SHIFT_DECKEY_START + 37), _T("LEFT_TRIANGLE")},
        {(SHIFT_DECKEY_START + 38), _T("RIGHT_TRIANGLE")},
        {(SHIFT_DECKEY_START + 39), _T("QUESTION")},
        {(FUNC_DECKEY_START), _T("ESC")},
        {(ESC_DECKEY + 1), _T("HANZEN")},
        {(HANZEN_DECKEY + 1), _T("TAB")},
        {(TAB_DECKEY + 1), _T("CAPS")},
        {(CAPS_DECKEY + 1), _T("ALNUM")},
        {(ALNUM_DECKEY + 1), _T("NFER")},
        {(NFER_DECKEY + 1), _T("XFER")},
        {(XFER_DECKEY + 1), _T("KANA")},
        {(KANA_DECKEY + 1), _T("BS")},
        {(BS_DECKEY + 1), _T("ENTER")},
        {(ENTER_DECKEY + 1), _T("INS")},
        {(INS_DECKEY + 1), _T("DEL")},
        {(DEL_DECKEY + 1), _T("HOME")},
        {(HOME_DECKEY + 1), _T("END")},
        {(END_DECKEY + 1), _T("PAGE_UP")},
        {(PAGE_UP_DECKEY + 1), _T("PAGE_DOWN")},
        {(PAGE_DOWN_DECKEY + 1), _T("UP_ARROW")},
        {(UP_ARROW_DECKEY + 1), _T("DOWN_ARROW")},
        {(DOWN_ARROW_DECKEY + 1), _T("LEFT_ARROW")},
        {(LEFT_ARROW_DECKEY + 1), _T("RIGHT_ARROW")},
        {(RIGHT_ARROW_DECKEY + 1), _T("RIGHT_SHIFT")},
        {(RIGHT_SHIFT_DECKEY + 1), _T("SHIFT_TAB")},
        {(CTRL_FUNC_DECKEY_START), _T("CTRL_ESC")},
        {(CTRL_ESC_DECKEY + 1), _T("CTRL_HANZEN")},
        {(CTRL_HANZEN_DECKEY + 1), _T("CTRL_TAB")},
        {(CTRL_TAB_DECKEY + 1), _T("CTRL_CAPS")},
        {(CTRL_CAPS_DECKEY + 1), _T("CTRL_ALNUM")},
        {(CTRL_ALNUM_DECKEY + 1), _T("CTRL_NFER")},
        {(CTRL_NFER_DECKEY + 1), _T("CTRL_XFER")},
        {(CTRL_XFER_DECKEY + 1), _T("CTRL_KANA")},
        {(CTRL_KANA_DECKEY + 1), _T("CTRL_BS")},
        {(CTRL_BS_DECKEY + 1), _T("CTRL_ENTER")},
        {(CTRL_ENTER_DECKEY + 1), _T("CTRL_INS")},
        {(CTRL_INS_DECKEY + 1), _T("CTRL_DEL")},
        {(CTRL_DEL_DECKEY + 1), _T("CTRL_HOME")},
        {(CTRL_HOME_DECKEY + 1), _T("CTRL_END")},
        {(CTRL_END_DECKEY + 1), _T("CTRL_PAGE_UP")},
        {(CTRL_PAGE_UP_DECKEY + 1), _T("CTRL_PAGE_DOWN")},
        {(CTRL_PAGE_DOWN_DECKEY + 1), _T("CTRL_UP_ARROW")},
        {(CTRL_UP_ARROW_DECKEY + 1), _T("CTRL_DOWN_ARROW")},
        {(CTRL_DOWN_ARROW_DECKEY + 1), _T("CTRL_LEFT_ARROW")},
        {(CTRL_LEFT_ARROW_DECKEY + 1), _T("CTRL_RIGHT_ARROW")},
        {(SPECIAL_DECKEY_ID_BASE + 1), _T("TOGGLE")},
        {(TOGGLE_DECKEY + 1), _T("ACTIVE")},
        {(ACTIVE_DECKEY + 1), _T("ACTIVE2")},
        {(ACTIVE2_DECKEY + 1), _T("DEACTIVE")},
        {(DEACTIVE_DECKEY + 1), _T("DEACTIVE2")},
        {(DEACTIVE2_DECKEY + 1), _T("STROKE_HELP_ROTATION")},
        {(STROKE_HELP_ROTATION_DECKEY + 1), _T("STROKE_HELP_UNROTATION")},
        {(STROKE_HELP_UNROTATION_DECKEY + 1), _T("DATE_STRING_ROTATION")},
        {(DATE_STRING_ROTATION_DECKEY + 1), _T("DATE_STRING_UNROTATION")},
        {(DATE_STRING_UNROTATION_DECKEY + 1), _T("FULL_ESCAPE")},
        {(FULL_ESCAPE_DECKEY + 1), _T("UNBLOCK")},
        {(UNBLOCK_DECKEY + 1), _T("CLEAR_STROKE")},
        {(CLEAR_STROKE_DECKEY + 1), _T("HISTORY_NEXT_SEARCH")},
        {(HISTORY_NEXT_SEARCH_DECKEY + 1), _T("HISTORY_PREV_SEARCH")},
        {(TOGGLE_HIRAGANA_STROKE_GUIDE + 1), _T("EXCHANGE_CODE_TABLE")},
        {(EXCHANGE_CODE_TABLE_DECKEY + 1), _T("LEFT_SHIFT_BLOCKER")},
        {(LEFT_SHIFT_BLOCKER_DECKEY + 1), _T("RIGHT_SHIFT_BLOCKER")},
        {(RIGHT_SHIFT_BLOCKER_DECKEY + 1), _T("LEFT_SHIFT_MAZE_START_POS")},
        {(LEFT_SHIFT_MAZE_START_POS_DECKEY + 1), _T("RIGHT_SHIFT_MAZE_START_POS")},
        {(RIGHT_SHIFT_MAZE_START_POS_DECKEY + 1), _T("PSEUDO_SPACE")},
        {(PSEUDO_SPACE_DECKEY + 1), _T("POST_NORMAL_SHIFT")},
        {(POST_NORMAL_SHIFT_DECKEY + 1), _T("POST_PLANE_A_SHIFT")},
        {(POST_PLANE_A_SHIFT_DECKEY + 1), _T("POST_PLANE_B_SHIFT")},
        {(POST_PLANE_B_SHIFT_DECKEY + 1), _T("MODE_TOGGLE_FOLLOW_CARET")},
        {(MODE_TOGGLE_FOLLOW_CARET_DECKEY + 1), _T("COPY_SELECTION_AND_SEND_TO_DICTIONARY")},
    };

    const wchar_t* GetDeckeyNameFromId(int id) {
        auto iter = deckeyId_name_map.find(id);
        return iter != deckeyId_name_map.end() ? iter->second : _T("?");
    }

} // namespace deckey_id_defs

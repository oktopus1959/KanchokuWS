// DO NOT EDIT THIS FILE!!!!
// このファイルは ./make_deckey_id_def.sh により ./DecoderKeys.cs から自動的に作成されました (2021/08/08 10:45:57)

#include "string_type.h"
#include "deckey_id_defs.h"

namespace deckey_id_defs {

    std::map<int, const wchar_t*> deckeyId_name_map = {
        {(GLOBAL_DECKEY_ID_BASE + 1), _T("TOGGLE_DECKEY")},
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
        {(UNBLOCK_DECKEY + 1), _T("NEXT_CAND_TRIGGER_DECKEY")},
        {(NEXT_CAND_TRIGGER_DECKEY + 1), _T("PREV_CAND_TRIGGER_DECKEY")},
        //public const int CTRL_A_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 1;
        //public const int CTRL_B_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 2;
        //public const int CTRL_C_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 3;
        //public const int CTRL_D_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 4;
        //public const int CTRL_E_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 5;
        //public const int CTRL_F_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 6;
        //public const int CTRL_G_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 7;
        //public const int CTRL_H_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 8;
        //public const int CTRL_I_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 9;
        //public const int CTRL_J_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 10;
        //public const int CTRL_K_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 11;
        //public const int CTRL_L_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 12;
        //public const int CTRL_M_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 13;
        //public const int CTRL_N_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 14;
        //public const int CTRL_O_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 15;
        //public const int CTRL_P_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 16;
        //public const int CTRL_Q_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 17;
        //public const int CTRL_R_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 18;
        //public const int CTRL_S_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 19;
        //public const int CTRL_T_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 20;
        //public const int CTRL_U_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 21;
        //public const int CTRL_V_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 22;
        //public const int CTRL_W_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 23;
        //public const int CTRL_X_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 24;
        //public const int CTRL_Y_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 25;
        //public const int CTRL_Z_DECKEY = CTRL_FUNC_DECKEY_ID_BASE + 26;
        //public const int CTRL_FUNC_DECKEY_ID_END = CTRL_Z_DECKEY + 1;
    };

    const wchar_t* GetDeckeyNameFromId(int id) {
        auto iter = deckeyId_name_map.find(id);
        return iter != deckeyId_name_map.end() ? iter->second : _T("?");
    }

} // namespace deckey_id_defs

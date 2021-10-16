#include "Logger.h"
#include "misc_utils.h"
#include "path_utils.h"
#include "string_utils.h"

#include "StrokeTable.h"
#include "Settings.h"

DEFINE_LOCAL_LOGGER(Settings);

namespace {
    inline tstring make_path(const tstring& dirpath, const tstring& filepath) {
        return filepath.empty() ? filepath : utils::joinPath(dirpath, filepath);
    }
}

void Settings::SetValues(const std::map<tstring, tstring>& dict) {

#define SET_KEY_VALUE(k) k = utils::safe_get(dict, tstring(_T(#k))); LOG_INFO(_T(#k ## "=%s"), k.c_str())
#define SET_FILE_PATH(k) k = make_path(SETTINGS->rootDir, utils::safe_get(dict, tstring(_T(#k)))); LOG_INFO(_T(#k ## "=%s"), k.c_str())
#define SET_INT_VALUE(k) k = utils::strToInt(utils::safe_get(dict, tstring(_T(#k)))); LOG_INFO(_T(#k ## "=%d"), k)
#define SET_UINT_VALUE(k) k = (size_t)utils::strToInt(utils::safe_get(dict, tstring(_T(#k)))); LOG_INFO(_T(#k ## "=%d"), k)
#define SET_BOOL_VALUE(k) k = utils::strToBool(utils::safe_get(dict, tstring(_T(#k)))); LOG_INFO(_T(#k ## "=%s"), utils::boolToString(k).c_str())
#define GET_BOOL_VALUE(k) utils::strToBool(utils::safe_get(dict, tstring(_T(#k))))
#define RESET_STROKE_FUNC(k) StrokeTableNode::AssignFucntion(utils::safe_get(dict, tstring(_T(k ## "KeySeq"))), _T(k))

    SET_BOOL_VALUE(firstUse);

    SET_KEY_VALUE(rootDir);
    SET_FILE_PATH(tableFile);
    SET_FILE_PATH(charsDefFile);
    SET_FILE_PATH(easyCharsFile);
    SET_FILE_PATH(bushuFile);
    SET_FILE_PATH(autoBushuFile);
    SET_FILE_PATH(bushuAssocFile);
    //SET_FILE_PATH(mazegakiFile);
    SET_KEY_VALUE(mazegakiFile);
    SET_FILE_PATH(historyFile);
    SET_FILE_PATH(historyUsedFile);
    SET_FILE_PATH(historyExcludeFile);
    SET_FILE_PATH(historyNgramFile);

    SET_INT_VALUE(backFileRotationGeneration);

    SET_INT_VALUE(histKanjiWordMinLength);
    SET_INT_VALUE(histKanjiWordMinLengthEx);
    SET_INT_VALUE(histKatakanaWordMinLength);
    SET_INT_VALUE(histHiraganaKeyLength);
    SET_INT_VALUE(histKatakanaKeyLength);
    SET_INT_VALUE(histKanjiKeyLength);

    SET_BOOL_VALUE(autoHistSearchEnabled);
    //SET_BOOL_VALUE(histSearchByCtrlSpace);
    //SET_BOOL_VALUE(histSearchByShiftSpace);
    SET_BOOL_VALUE(selectFirstCandByEnter);
    SET_INT_VALUE(histDelDeckeyId);
    SET_INT_VALUE(histNumDeckeyId);
    SET_UINT_VALUE(histHorizontalCandMax);
    SET_BOOL_VALUE(histMoveShortestAt2nd);

    SET_BOOL_VALUE(useArrowKeyToSelectCandidate);

    SET_BOOL_VALUE(mazegakiSelectFirstCand);
    SET_BOOL_VALUE(mazeBlockerTail);
    SET_BOOL_VALUE(mazeRemoveHeadSpace);
    SET_BOOL_VALUE(mazeRightShiftYomiPos);
    SET_INT_VALUE(mazeYomiMaxLen);
    SET_INT_VALUE(mazeGobiMaxLen);
    SET_INT_VALUE(mazeNoIfxGobiMaxLen);
    SET_INT_VALUE(mazeGobiLikeTailLen);

    SET_INT_VALUE(hiraganaToKatakanaShiftPlane);
    SET_BOOL_VALUE(convertJaPeriod);
    SET_BOOL_VALUE(convertJaComma);

    SET_BOOL_VALUE(removeOneStrokeByBackspace);

    SET_BOOL_VALUE(autoBushuComp);

    SET_KEY_VALUE(romanBushuCompPrefix);

    // 機能へのキー割り当ての変更
    RESET_STROKE_FUNC("zenkakuMode");
    RESET_STROKE_FUNC("zenkakuOneChar");
    RESET_STROKE_FUNC("nextThrough");
    RESET_STROKE_FUNC("history");
    RESET_STROKE_FUNC("historyOneChar");
    RESET_STROKE_FUNC("mazegaki");
    RESET_STROKE_FUNC("bushuComp");
    RESET_STROKE_FUNC("bushuAssoc");
    RESET_STROKE_FUNC("bushuAssocDirect");
    RESET_STROKE_FUNC("katakanaMode");
    RESET_STROKE_FUNC("katakanaOneShot");
    RESET_STROKE_FUNC("hankakuKatakanaOneShot");
    RESET_STROKE_FUNC("blockerSetterOneShot");
    //if (GET_BOOL_VALUE(mazegakiByShiftSpace)) StrokeTableNode::AssignFucntion(utils::format(_T("%d"), DECKEY_STROKE_49), _T("mazegaki"));

    // for Debug
    SET_BOOL_VALUE(debughState);
    SET_BOOL_VALUE(debughMazegaki);
    SET_BOOL_VALUE(debughMazegakiDic);
    SET_BOOL_VALUE(debughHistory);
    SET_BOOL_VALUE(debughStrokeTable);
    SET_BOOL_VALUE(debughBushu);
    SET_BOOL_VALUE(debughString);
    SET_BOOL_VALUE(debughZenkaku);
    SET_BOOL_VALUE(debughKatakana);
    SET_BOOL_VALUE(debughMyPrevChar);
    SET_BOOL_VALUE(bushuDicLogEnabled);
}

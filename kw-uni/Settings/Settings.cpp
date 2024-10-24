#include "Logger.h"
#include "misc_utils.h"
#include "path_utils.h"
#include "string_utils.h"

#include "StrokeTable.h"
#include "Settings.h"

DEFINE_LOCAL_LOGGER(Settings);

namespace {
    inline String make_path(StringRef dirpath, StringRef filepath) {
        return filepath.empty() ? filepath : utils::joinPath(dirpath, filepath);
    }

    inline wchar_t safe_get_head_char(StringRef s) {
        return s.empty() ? '\0' : s[0];
    }
}

void Settings::SetValues(const std::map<String, String>& dict) {

#define SET_KEY_VALUE(k) k = utils::safe_get(dict, String(_T(#k))); LOG_DEBUGH(_T(#k "={}"), k)
#define SET_FILE_PATH(k) k = make_path(SETTINGS->rootDir, utils::safe_get(dict, String(_T(#k)))); LOG_DEBUGH(_T(#k "={}"), k)
#define SET_CHAR_VALUE(k) k = safe_get_head_char(utils::safe_get(dict, String(_T(#k)))); LOG_DEBUGH(_T(#k "={}"), k)
#define SET_INT_VALUE(k) k = utils::strToInt(utils::safe_get(dict, String(_T(#k)))); LOG_DEBUGH(_T(#k "={}"), k)
#define SET_UINT_VALUE(k) k = (size_t)utils::strToInt(utils::safe_get(dict, String(_T(#k)))); LOG_DEBUGH(_T(#k "={}"), k)
#define SET_BOOL_VALUE(k) k = utils::strToBool(utils::safe_get(dict, String(_T(#k)))); LOG_DEBUGH(_T(#k "={}"), k)
#define GET_BOOL_VALUE(k) utils::strToBool(utils::safe_get(dict, String(_T(#k))))
#define RESET_STROKE_FUNC(k) StrokeTableNode::AssignFucntion(utils::safe_get(dict, String(_T(k "KeySeq"))), _T(k))

    SET_BOOL_VALUE(firstUse);
    SET_BOOL_VALUE(isJPmode);

    SET_KEY_VALUE(rootDir);
    SET_FILE_PATH(tableFile);
    SET_FILE_PATH(tableFile2);
    SET_FILE_PATH(tableFile3);
    SET_FILE_PATH(charsDefFile);
    SET_FILE_PATH(easyCharsFile);
    SET_FILE_PATH(kanjiYomiFile);
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

    SET_INT_VALUE(histMaxLength);
    SET_INT_VALUE(histKanjiWordMinLength);
    SET_INT_VALUE(histKanjiWordMaxLength);
    SET_INT_VALUE(histKanjiWordMinLengthEx);
    SET_INT_VALUE(histKatakanaWordMinLength);
    SET_INT_VALUE(histKatakanaWordMaxLength);
    SET_INT_VALUE(histHiraganaKeyLength);
    SET_INT_VALUE(histKatakanaKeyLength);
    SET_INT_VALUE(histKanjiKeyLength);
    SET_INT_VALUE(histRomanKeyLength);
    //SET_INT_VALUE(histMapKeyLength);
    SET_INT_VALUE(histMapKeyMaxLength);
    SET_INT_VALUE(histMapGobiMaxLength);

    SET_BOOL_VALUE(autoHistSearchEnabled);
    //SET_BOOL_VALUE(histSearchByCtrlSpace);
    //SET_BOOL_VALUE(histSearchByShiftSpace);
    SET_BOOL_VALUE(selectFirstCandByEnter);
    SET_BOOL_VALUE(newLineWhenHistEnter);
    SET_INT_VALUE(histDelDeckeyId);
    SET_INT_VALUE(histNumDeckeyId);
    SET_UINT_VALUE(histHorizontalCandMax);
    SET_BOOL_VALUE(histMoveShortestAt2nd);
    SET_BOOL_VALUE(showHistCandsFromFirst);
    SET_BOOL_VALUE(selectHistCandByNumberKey);

    SET_BOOL_VALUE(useArrowToSelCand);
    SET_BOOL_VALUE(selectHistCandByTab);

    SET_BOOL_VALUE(mazeHistRegisterAnyway);
    SET_BOOL_VALUE(mazegakiSelectFirstCand);
    SET_BOOL_VALUE(mazeBlockerTail);
    SET_BOOL_VALUE(mazeRemoveHeadSpace);
    SET_BOOL_VALUE(mazeRightShiftYomiPos);
    SET_BOOL_VALUE(mazeNoIfxConnectKanji);
    SET_BOOL_VALUE(mazeNoIfxConnectAny);
    SET_INT_VALUE(mazeHistRegisterMinLen);
    SET_INT_VALUE(mazeYomiMaxLen);
    SET_INT_VALUE(mazeGobiMaxLen);
    SET_INT_VALUE(mazeNoIfxGobiMaxLen);
    SET_INT_VALUE(mazeGobiLikeTailLen);

    SET_INT_VALUE(bushuAssocSelectCount);

    SET_INT_VALUE(hiraToKataShiftPlane);
    SET_BOOL_VALUE(hiraToKataNormalPlane);
    SET_BOOL_VALUE(convertJaPeriod);
    SET_BOOL_VALUE(convertJaComma);

    SET_BOOL_VALUE(eisuModeEnabled);
    SET_CHAR_VALUE(eisuHistSearchChar);
    if (eisuHistSearchChar == '_') eisuHistSearchChar = ' ';
    SET_CHAR_VALUE(eisuExitAsIsChar);
    if (eisuExitAsIsChar == '_') eisuExitAsIsChar = ' ';
    SET_CHAR_VALUE(eisuExitDecapitalChar);
    if (eisuExitDecapitalChar == '_') eisuExitDecapitalChar = ' ';
    SET_INT_VALUE(eisuExitCapitalCharNum);
    SET_INT_VALUE(eisuExitSpaceNum);

    SET_BOOL_VALUE(removeOneByBS);

    SET_BOOL_VALUE(yamanobeEnabled);
    //SET_BOOL_VALUE(autoBushuComp);
    SET_INT_VALUE(autoBushuCompMinCount);

    SET_KEY_VALUE(romanBushuCompPrefix);
    SET_KEY_VALUE(romanSecPlanePrefix);

    SET_BOOL_VALUE(googleCompatible);

    SET_BOOL_VALUE(multiStreamMode);
    SET_INT_VALUE(commitBeforeTailLen);
    SET_INT_VALUE(kanjiNoKanjiBonus);
    SET_KEY_VALUE(mergerCandidateFile);

    // 機能へのキー割り当ての変更
    RESET_STROKE_FUNC("zenkakuMode");
    RESET_STROKE_FUNC("zenkakuOneChar");
    RESET_STROKE_FUNC("nextThrough");
    RESET_STROKE_FUNC("history");
    RESET_STROKE_FUNC("historyOneChar");
    RESET_STROKE_FUNC("historyFewChars");
    RESET_STROKE_FUNC("mazegaki");
    RESET_STROKE_FUNC("bushuComp");
    RESET_STROKE_FUNC("bushuAssoc");
    RESET_STROKE_FUNC("bushuAssocDirect");
    RESET_STROKE_FUNC("katakanaMode");
    RESET_STROKE_FUNC("katakanaOneShot");
    RESET_STROKE_FUNC("hanKataOneShot");
    RESET_STROKE_FUNC("blkSetOneShot");
    //if (GET_BOOL_VALUE(mazegakiByShiftSpace)) StrokeTableNode::AssignFucntion(utils::format(_T("{}"), DECKEY_STROKE_49), _T("mazegaki"));

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

#pragma once

#include "string_type.h"
#include "misc_utils.h"
#include "Logger.h"
#include "deckey_id_defs.h"

// Deckey から文字へのマッピング
class DeckeyToChars {
    DECLARE_CLASS_LOGGER;

    wchar_t normalChars[NORMAL_DECKEY_NUM] = { 0 };
    wchar_t shiftedChars[SHIFT_DECKEY_NUM] = { 0 };

    int yenPos = -1;

public:
    static std::unique_ptr<DeckeyToChars> Singleton;

    static int CreateSingleton(const tstring& filepath);

    void ReadDefFile(const std::vector<wstring>&);

    wchar_t GetCharFromDeckey(int deckeyId, wchar_t defChar = 0) {
        if (deckeyId >= 0) {
            if (deckeyId < NORMAL_DECKEY_NUM) return normalChars[deckeyId];
            if (deckeyId < STROKE_DECKEY_NUM) return shiftedChars[deckeyId - SHIFT_DECKEY_START];
        }
        return defChar;
    }

    int GetYenPos() const { return yenPos; }

    wchar_t oneCharResult[2] = { 0, 0 };

    const wchar_t* GetDeckeyNameFromId(int deckeyId) {
        wchar_t ch = GetCharFromDeckey(deckeyId);
        if (ch != 0) {
            oneCharResult[0] = ch;
            return oneCharResult;
        }
        return DECKEY_NAME_FROM_ID(deckeyId);
    }

    // Deckey順に並んだ通常文字列とシフト文字列を返す
    void GetCharsOrderedByDeckey(wchar_t* table) {
        LOG_INFO(_T("CALLED"));
        std::map<wchar_t, size_t> indexMap;

        for (size_t i = 0; i < NORMAL_DECKEY_NUM; ++i) {
            table[i] = normalChars[i];
            table[i + SHIFT_DECKEY_START] = shiftedChars[i];
        }
    }


};
#define DECKEY_TO_CHARS (DeckeyToChars::Singleton)


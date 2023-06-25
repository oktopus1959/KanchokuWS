#pragma once

#include "string_type.h"
#include "misc_utils.h"
#include "Logger.h"
#include "deckey_id_defs.h"

// Deckey から文字へのマッピング
class DeckeyToChars {
    DECLARE_CLASS_LOGGER;

    wchar_t normalChars[NORMAL_DECKEY_NUM] = { 0 };
    wchar_t shiftedChars[NORMAL_DECKEY_NUM] = { 0 };

    int yenPos = -1;

public:
    static std::unique_ptr<DeckeyToChars> Singleton;

    static int CreateSingleton(StringRef filepath);

    void ReadDefFile(const std::vector<String>&);

    wchar_t GetCharFromDeckey(int deckeyId, wchar_t defChar = '?') {
        if (deckeyId >= 0) {
            int id = deckeyId;
            if (id >= 0 && id < NORMAL_DECKEY_NUM) return normalChars[id];
            id = deckeyId - SHIFT_DECKEY_START;
            if (id >= 0 && id < NORMAL_DECKEY_NUM) return shiftedChars[id];
        }
        return defChar;
    }

    int GetYenPos() const { return yenPos; }

    wchar_t oneCharResult[2] = { 0, 0 };

    const wchar_t* GetDeckeyNameFromId(int deckeyId) {
        wchar_t ch = GetCharFromDeckey(deckeyId, 0);
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


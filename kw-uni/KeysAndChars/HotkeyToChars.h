#pragma once

#include "string_type.h"
#include "misc_utils.h"
#include "Logger.h"
#include "hotkey_id_defs.h"

// Hotkey から文字へのマッピング
class HotkeyToChars {
    DECLARE_CLASS_LOGGER;

    wchar_t normalChars[NUM_STROKE_HOTKEY] = { 0 };
    wchar_t shiftedChars[NUM_STROKE_HOTKEY] = { 0 };

    int yenPos = -1;

public:
    static std::unique_ptr<HotkeyToChars> Singleton;

    static int CreateSingleton(const tstring& filepath);

    void ReadDefFile(const std::vector<wstring>&);

    wchar_t GetCharFromHotkey(int hotkeyId, wchar_t defChar = 0) {
        if (hotkeyId >= 0) {
            if (hotkeyId < NUM_STROKE_HOTKEY) return normalChars[hotkeyId];
            if (hotkeyId < NUM_STROKE_HOTKEY * 2) return shiftedChars[hotkeyId - NUM_STROKE_HOTKEY];
        }
        return defChar;
    }

    int GetYenPos() const { return yenPos; }

    wchar_t oneCharResult[2] = { 0, 0 };

    const wchar_t* GetHotkeyNameFromId(int hotkeyId) {
        wchar_t ch = GetCharFromHotkey(hotkeyId);
        if (ch != 0) {
            oneCharResult[0] = ch;
            return oneCharResult;
        }
        return HOTKEY_NAME_FROM_ID(hotkeyId);
    }
};
#define HOTKEY_TO_CHARS (HotkeyToChars::Singleton)


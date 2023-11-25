#include <algorithm>
#include "file_utils.h"
#include "misc_utils.h"
#include "ErrorHandler.h"
#include "DeckeyToChars.h"
#include "Settings.h"
#include "Logger.h"

namespace {
    wchar_t QwertyCharsJP[NORMAL_DECKEY_NUM] = {
        '1', '2', '3', '4', '5', '6', '7', '8', '9', '0',
        'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p',
        'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', ';',
        'z', 'x', 'c', 'v', 'b', 'n', 'm', ',', '.', '/',
        ' ', '-', '^', '\\', '@', '[', ':', ']', '\\', 0
    };

    wchar_t QwertyShiftedCharsJP[NORMAL_DECKEY_NUM] = {
        '!', '"', '#', '$', '%', '&', '\'', '(', ')', 0,
        'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P',
        'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L', '+',
        'Z', 'X', 'C', 'V', 'B', 'N', 'M', '<', '>', '?',
        ' ', '=', '~', '|', '`', '{', '*', '}', '_', 0
    };

    wchar_t QwertyCharsUS[NORMAL_DECKEY_NUM] = {
        '1', '2', '3', '4', '5', '6', '7', '8', '9', '0',
        'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p',
        'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', ';',
        'z', 'x', 'c', 'v', 'b', 'n', 'm', ',', '.', '/',
        ' ', '-', '=', '\\', '[', ']', '\'', '`', '\0', '\0'
    };

    wchar_t QwertyShiftedCharsUS[NORMAL_DECKEY_NUM] = {
        '!', '@', '#', '$', '%', '^', '&', '*', '(', ')',
        'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P',
        'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L', ':',
        'Z', 'X', 'C', 'V', 'B', 'N', 'M', '<', '>', '?',
        ' ', '_', '+', '|', '{', '}', '"', '~', '\0', '\0'
    };

    int QwertyYenPos = 43;

    const wchar_t* QwertyChars() {
        return (SETTINGS->isJPmode ? QwertyCharsJP : QwertyCharsUS);
    }

    const wchar_t* QwertyShiftedChars() {
        return (SETTINGS->isJPmode ? QwertyShiftedCharsJP : QwertyShiftedCharsUS);
    }
}

DEFINE_CLASS_LOGGER(DeckeyToChars);

std::unique_ptr<DeckeyToChars> DeckeyToChars::Singleton;

int DeckeyToChars::CreateSingleton(StringRef filePath) {
    LOG_INFO(_T("ENTER: filePath=<{}>, kbMode={}"), filePath, SETTINGS->isJPmode ? L"JP" : L"US");

    Singleton.reset(new DeckeyToChars());

    memcpy_s(Singleton->normalChars, NORMAL_DECKEY_NUM * sizeof(wchar_t), QwertyChars(), NORMAL_DECKEY_NUM * sizeof(wchar_t));
    memcpy_s(Singleton->shiftedChars, NORMAL_DECKEY_NUM * sizeof(wchar_t), QwertyShiftedChars(), NORMAL_DECKEY_NUM * sizeof(wchar_t));
    Singleton->yenPos = QwertyYenPos;

    if (!filePath.empty()) {
        LOG_INFO(_T("open chars def file: {}"), filePath);

        utils::IfstreamReader reader(filePath);
        if (reader.success()) {
            Singleton->ReadDefFile(reader.getAllLines());
            LOG_INFO(_T("close chars def file: {}"), filePath);
        } else {
            // エラーメッセージを表示
            LOG_ERROR(L"Can't read chars def file: {}", filePath);
            ERROR_HANDLER->Warn(std::format(_T("漢直キー⇒文字定義ファイル({})が開けません"), filePath));
        }
    }
    LOG_INFO(_T("LEAVE"));
    return 0;
}

namespace {
    template<size_t N>
    void storeChars(std::vector<String>::const_iterator iter, std::vector<String>::const_iterator end, wchar_t (&buf)[N]) {
        size_t pos = 0;
        for (; iter != end; ++iter) {
            StringRef line = *iter;
            if (utils::startsWith(line, _T("## END"))) break;
            for (auto ch : line) {
                if (pos >= N) break;
                buf[pos++] = ch;
            }
        }
    }
}

void DeckeyToChars::ReadDefFile(const std::vector<String>& lines) {
    LOG_DEBUGH(_T("CALLED"));

    bool shifted = false;
    auto iter = lines.begin();
    for ( ; iter != lines.end(); ++iter) {
        StringRef line = *iter;

        if (utils::startsWith(line, _T("## NORMAL"))) {
            storeChars(++iter, lines.end(), normalChars);
        } else if (utils::startsWith(line, _T("## SHIFT"))) {
            storeChars(++iter, lines.end(), shiftedChars);
            shifted = true;
        } else if (utils::startsWith(line, _T("## YEN="))) {
            yenPos = utils::strToInt(line.substr(7));
        }
    }

    if (!shifted) {
        const wchar_t* normalQwerty = QwertyChars();
        const wchar_t* shiftedQwerty = QwertyShiftedChars();
        for (size_t i = 0; i < NORMAL_DECKEY_NUM; ++i) {
            wchar_t ch = normalChars[i];
            for (size_t j = 0; j < NORMAL_DECKEY_NUM; ++j) {
                if (ch == normalQwerty[j]) {
                    shiftedChars[i] = shiftedQwerty[j];
                    break;
                }
            }
        }
    }
}



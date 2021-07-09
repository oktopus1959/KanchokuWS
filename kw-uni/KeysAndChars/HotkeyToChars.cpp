#include "file_utils.h"
#include "misc_utils.h"
#include "ErrorHandler.h"
#include "HotkeyToChars.h"

namespace {
    wchar_t QwertyChars[NUM_STROKE_HOTKEY] = {
         '1', '2', '3', '4', '5', '6', '7', '8', '9', '0',
         'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p',
         'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', ';',
         'z', 'x', 'c', 'v', 'b', 'n', 'm', ',', '.', '/',
         ' ', '-', '^', '\\', '@', '[', ':', ']', '\\', 0
    };

    wchar_t QwertyShiftedChars[NUM_STROKE_HOTKEY] = {
         '!', '"', '#', '$', '%', '&', '\'', '(', ')', 0,
         'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P',
         'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L', '+',
         'Z', 'X', 'C', 'V', 'B', 'N', 'M', '<', '>', '?',
         ' ', '=', '~', '|', '`', '{', '*', '}', '_', 0
    };

    int QwertyYenPos = 43;
}

DEFINE_CLASS_LOGGER(HotkeyToChars);

std::unique_ptr<HotkeyToChars> HotkeyToChars::Singleton;

int HotkeyToChars::CreateSingleton(const tstring& filePath) {

    Singleton.reset(new HotkeyToChars());

    memcpy_s(Singleton->normalChars, NUM_STROKE_HOTKEY * sizeof(wchar_t), QwertyChars, NUM_STROKE_HOTKEY * sizeof(wchar_t));
    memcpy_s(Singleton->shiftedChars, NUM_STROKE_HOTKEY * sizeof(wchar_t), QwertyShiftedChars, NUM_STROKE_HOTKEY * sizeof(wchar_t));
    Singleton->yenPos = QwertyYenPos;

    if (!filePath.empty()) {
        LOG_INFO(_T("open chars def file: %s"), filePath.c_str());

        utils::IfstreamReader reader(filePath);
        if (reader.success()) {
            Singleton->ReadDefFile(utils::IfstreamReader(filePath).getAllLines());
            LOG_INFO(_T("close chars def file: %s"), filePath.c_str());
        } else {
            // エラーメッセージを表示
            LOG_ERROR(_T("Can't read chars def file: %s"), filePath.c_str());
            ERROR_HANDLER->Warn(utils::format(_T("ホットキー⇒文字定義ファイル(%s)が開けません"), filePath.c_str()));
        }
    }
    LOG_INFO(_T("LEAVE"));
    return 0;
}

namespace {
    template<size_t N>
    void storeChars(std::vector<wstring>::const_iterator iter, std::vector<wstring>::const_iterator end, wchar_t (&buf)[N]) {
        size_t pos = 0;
        for (; iter != end; ++iter) {
            const wstring& line = *iter;
            if (utils::startsWith(line, _T("## END"))) break;
            for (auto ch : line) {
                if (pos >= N) break;
                buf[pos++] = ch;
            }
        }
    }
}

void HotkeyToChars::ReadDefFile(const std::vector<wstring>& lines) {
    LOG_DEBUG(_T("CALLED"));

    //size_t idx = 0;
    auto iter = lines.begin();
    for ( ; iter != lines.end(); ++iter) {
        const wstring& line = *iter;

        if (utils::startsWith(line, _T("## NORMAL"))) {
            storeChars(++iter, lines.end(), normalChars);
        } else if (utils::startsWith(line, _T("## SHIFT"))) {
            storeChars(++iter, lines.end(), shiftedChars);
        } else if (utils::startsWith(line, _T("## YEN="))) {
            yenPos = utils::strToInt(line.substr(7));
        }
    }
}



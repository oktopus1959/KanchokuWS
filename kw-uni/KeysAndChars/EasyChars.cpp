// EasyChars
#include "file_utils.h"
#include "StrokeTable.h"
#include "StringNode.h"
#include "ErrorHandler.h"

#include "deckey_id_defs.h"
#include "Settings/Settings.h"
#include "EasyChars.h"

namespace {
    DEFINE_NAMESPACE_LOGGER(EasyChars);

    // Nストローク文字を追加
    void addNStrokeChars(StrokeTableNode* node, size_t start, size_t deckeyNum, size_t depth) {
        for (size_t i = start; i < deckeyNum; ++i) {
            auto blk = node->getNth(i);
            if (blk && blk->isStringNode()) {
                EASY_CHARS->AddEasyChar(utils::safe_front(blk->getString()));
            } else if (depth > 1 && blk && blk->isStrokeTableNode()) {
                addNStrokeChars((StrokeTableNode*)blk, start, deckeyNum, depth - 1);
            }
        }
    }

    // Nストローク文字を追加
    void addNStrokeChars(size_t start, size_t deckeyNum, size_t depth) {
        LOG_INFO(_T("start=%d, deckeyNum=%d, depth=%d"), start, deckeyNum, depth);
        if (ROOT_STROKE_NODE) addNStrokeChars(ROOT_STROKE_NODE, start, deckeyNum, depth);
    }
}

DEFINE_CLASS_LOGGER(EasyChars);

// Decoder.cpp で生成される
std::unique_ptr<EasyChars> EasyChars::Singleton;

// 簡易打鍵文字(最上段を使わないレベル1(900文字)、2ストローク文字、全ストローク文字、およびユーザー定義の簡易打鍵文字)を集める
void EasyChars::GatherEasyChars() {
    LOG_INFOH(_T("ENTER"));

    if (Singleton) {
        LOG_INFOH(_T("Already created. Do cleaning: Singleton=%p"), Singleton.get());
        Singleton->CleanUp();
    } else {
        Singleton.reset(new EasyChars());
        LOG_INFOH(_T("New Singleton = %p"), Singleton.get());
    }

    auto easyCharsFile = SETTINGS->easyCharsFile;
    if (!easyCharsFile.empty()) {
        LOG_INFOH(_T("open easy chars file: %s"), easyCharsFile.c_str());
        utils::IfstreamReader reader(easyCharsFile);
        if (reader.success()) {
            for (const auto& line : reader.getAllLines()) {
                LOG_INFO(_T("line=%s"), line.c_str());
                auto ln = utils::strip(line);
                if (ln.empty() || ln[0] == '#') continue;

                LOG_INFOH(_T("line=%s"), line.c_str());

                // 最上段を使わないレベル1の2ストローク
                if (utils::toLower(ln) == _T("includefirstlevel")) {
                    addNStrokeChars(10, NORMAL_DECKEY_NUM, 2);
                    continue;
                }

                // 全2ストローク文字
                if (utils::toLower(ln) == _T("include2strokechars")) {
                    addNStrokeChars(0, NORMAL_DECKEY_NUM, 2);
                    continue;
                }

                // 全ストローク文字
                if (utils::toLower(ln) == _T("includeallstrokechars")) {
                    addNStrokeChars(0, NORMAL_DECKEY_NUM, 9);
                    continue;
                }

                // ユーザー定義の容易打鍵文字
                for (auto ch : ln) {
                    Singleton->AddEasyChar(ch);
                }
            }
        } else {
            // エラーメッセージを表示
            LOG_ERROR(_T("Can't read easyChars file: %s"), easyCharsFile.c_str());
            ERROR_HANDLER->Warn(utils::format(_T("簡易打鍵文字ファイル(%s)が開けません"), easyCharsFile.c_str()));
        }
    }
    LOG_INFOH(_T("LEAVE"));
}

void EasyChars::DumpEasyCharsMemory(int n) {
    if (Singleton) {
        const std::set<mchar_t>& set_ = Singleton->GetCharsSet();
        unsigned long* p = (unsigned long*)(&set_);
        LOG_INFOH(_T("%d: %p=%08x,%08x,%08x,%08x,%08x,%08x,%08x,%08x"), n, p, p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7]);
        MString chars;
        for (auto x : set_) {
            chars.push_back(x);
        }
        LOG_INFOH(_T("CHARS: %s"), MAKE_WPTR(chars));
    }
}


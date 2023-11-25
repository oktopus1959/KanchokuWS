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
            if (blk && blk->isStringLikeNode()) {
                EASY_CHARS->AddEasyChar(utils::safe_front(blk->getString()));
            } else if (depth > 1 && blk && blk->isStrokeTableNode()) {
                addNStrokeChars((StrokeTableNode*)blk, start, deckeyNum, depth - 1);
            }
        }
    }

    // Nストローク文字を追加
    void addNStrokeChars(size_t start, size_t deckeyNum, size_t depth) {
        LOG_DEBUGH(_T("start={}, deckeyNum={}, depth={}"), start, deckeyNum, depth);
        if (ROOT_STROKE_NODE) addNStrokeChars(ROOT_STROKE_NODE, start, deckeyNum, depth);
    }
}

DEFINE_CLASS_LOGGER(EasyChars);

// Decoder.cpp で生成される
std::unique_ptr<EasyChars> EasyChars::Singleton;

// 簡易打鍵文字(最上段を使わないレベル1(900文字)、2ストローク文字、全ストローク文字、およびユーザー定義の簡易打鍵文字)を集める
void EasyChars::GatherEasyChars() {
    LOG_INFO(_T("ENTER"));

    if (Singleton) {
        LOG_INFO(_T("Already created. Do cleaning: Singleton={:p}"), (void*)Singleton.get());
        Singleton->CleanUp();
    } else {
        Singleton.reset(new EasyChars());
        LOG_INFO(_T("New Singleton = {:p}"), (void*)Singleton.get());
    }

    auto easyCharsFile = SETTINGS->easyCharsFile;
    if (!easyCharsFile.empty()) {
        LOG_INFO(_T("open easy chars file: {}"), easyCharsFile);
        utils::IfstreamReader reader(easyCharsFile);
        if (reader.success()) {
            for (const auto& line : reader.getAllLines()) {
                LOG_DEBUGH(_T("line={}"), line);
                auto ln = utils::strip(line);
                if (ln.empty() || ln[0] == '#') continue;

                LOG_INFO(_T("line={}"), line);

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
            LOG_ERROR(_T("Can't read easyChars file: {}"), easyCharsFile);
            ERROR_HANDLER->Warn(std::format(_T("簡易打鍵文字ファイル({})が開けません"), easyCharsFile));
        }
    }
    LOG_INFO(_T("LEAVE"));
}

void EasyChars::DumpEasyCharsMemory(int n) {
    if (Singleton) {
        const std::set<mchar_t>& set_ = Singleton->GetCharsSet();
        unsigned long* p = (unsigned long*)(&set_);
        LOG_INFO(_T("{}: {:p}={:08x},{:08x},{:08x},{:08x},{:08x},{:08x},{:08x},{:08x}"), n, (void*)p, p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7]);
        MString chars;
        for (auto x : set_) {
            chars.push_back(x);
        }
        LOG_INFO(_T("CHARS: {}"), to_wstr(chars));
    }
}


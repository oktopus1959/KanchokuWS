// EasyChars
#include "file_utils.h"
#include "StrokeTable.h"
#include "StringNode.h"
#include "ErrorHandler.h"

#include "deckey_id_defs.h"
#include "Settings/Settings.h"
#include "EasyChars.h"

namespace {

}

DEFINE_CLASS_LOGGER(EasyChars);

// Decoder.cpp で生成される
std::unique_ptr<EasyChars> EasyChars::Singleton;

// 最上段を使わないレベル1(900文字)とユーザー定義の簡易打鍵文字を集める
void EasyChars::GatherEasyChars() {
    LOG_INFO(_T("ENTER"));

    //if (Singleton) {
    //    LOG_INFO(_T("LEAVE: Already created"));
    //    return;
    //}

    Singleton.reset(new EasyChars());

    auto easyCharsFile = SETTINGS->easyCharsFile;
    if (!easyCharsFile.empty()) {
        LOG_INFO(_T("open easy chars file: %s"), easyCharsFile.c_str());
        utils::IfstreamReader reader(easyCharsFile);
        if (reader.success()) {
            for (const auto& line : reader.getAllLines()) {
                LOG_INFO(_T("line=%s"), line.c_str());
                auto ln = utils::strip(line);
                if (ln.empty() || ln[0] == '#') continue;

                if (utils::toLower(ln) == _T("includefirstlevel")) {
                    Singleton->includeFirstLevel();
                    continue;
                }

                for (auto ch : ln) {
                    Singleton->easyChars.push_back(ch);
                }
            }
        } else {
            // エラーメッセージを表示
            LOG_ERROR(_T("Can't read maze file: %s"), easyCharsFile.c_str());
            ERROR_HANDLER->Warn(utils::format(_T("簡易打鍵文字ファイル(%s)が開けません"), easyCharsFile.c_str()));
        }
    }
    LOG_INFO(_T("LEAVE"));
}

// 最上段を使わないレベル1(900文字)を追加
void EasyChars::includeFirstLevel() {
    LOG_INFO(_T("ENTER"));
    for (size_t i = 10; i < STROKE_SPACE_DECKEY; ++i) {
        auto blk = ROOT_STROKE_NODE->getNth(i);
        if (blk && blk->isStrokeTableNode()) {
            for (int j = 10; j < STROKE_SPACE_DECKEY; ++j) {
                Node* sb = ((StrokeTableNode*)blk)->getNth(j);
                if (sb && sb->isStringNode()) {
                    EASY_CHARS->easyChars.push_back(utils::safe_front(sb->getString()));
                }
            }
        }
    }
    LOG_INFO(_T("LEAVE"));
}

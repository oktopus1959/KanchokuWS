#pragma once

#include "string_utils.h"
#include "misc_utils.h"
#include "Logger.h"

class Node;

// 最上段を使わないレベル1(900文字)とユーザー定義の簡易打鍵文字を集めたクラス
class EasyChars {
    DECLARE_CLASS_LOGGER;

private:
    std::vector<mchar_t> chars;

    void includeFirstLevel();

public:
    static std::unique_ptr<EasyChars> Singleton;

public:
    // 最上段を使わないレベル1(900文字)とユーザー定義の簡易打鍵文字を集める
    static void GatherEasyChars();

    // 第1レベル文字か
    inline bool IsFirstLevel(mchar_t ch) {
        return std::find(chars.begin(), chars.end(), ch) != chars.end();
    }

    // 引数の文字列の中の全ての文字は第1レベルの文字である
    inline bool AllContainedIn(const MString& w) {
        for (auto ch : w) {
            if (!IsFirstLevel(ch)) return false;
        }
        return true;
    }
};

#define EASY_CHARS  (EasyChars::Singleton)

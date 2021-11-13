#pragma once

#include "string_utils.h"
#include "misc_utils.h"
#include "Logger.h"

class Node;

// 最上段を使わないレベル1(900文字)とユーザー定義の簡易打鍵文字を集めたクラス
class EasyChars {
    DECLARE_CLASS_LOGGER;

private:
    std::set<mchar_t> easyChars;

public:
    static std::unique_ptr<EasyChars> Singleton;

public:
    // 最上段を使わないレベル1(900文字)とユーザー定義の簡易打鍵文字を集める
    static void GatherEasyChars();

    // 容易打鍵文字を追加
    inline void AddEasyChar(mchar_t ch) {
        easyChars.insert(ch);
    }

    // 容易打鍵文字か
    inline bool IsEasyChar(mchar_t ch) {
        //return std::find(easyChars.begin(), easyChars.end(), ch) != easyChars.end();
        return easyChars.find(ch) != easyChars.end();
    }

    // 引数の文字列の中の全ての文字は容易打鍵文字である
    inline bool AllContainedIn(const MString& w) {
        for (auto ch : w) {
            if (!IsEasyChar(ch)) return false;
        }
        return true;
    }
};

#define EASY_CHARS  (EasyChars::Singleton)

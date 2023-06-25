#pragma once

#include "string_utils.h"

class RegexUtil {
    std::wregex _re;

public:
    RegexUtil(const String& rs) : _re(rs) { }

    RegexUtil(const std::wregex& rx) : _re(rx) { }


    // 正規表現にマッチする部分を含むかテストする
    bool search(StringRef s) {
        return std::regex_search(s, _re);
    }

    // 文字列全体が正規表現にマッチするかテストする
    bool match(StringRef s) {
        return std::regex_match(s, _re);
    }

    // 正規表現 delim で n 個に分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    std::vector<String> split(StringRef s, size_t n) {
        return utils::reSplit(s, n, _re);
    }

    // 正規表現 delim で分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    std::vector<String> split(StringRef s) {
        return utils::reSplit(s, 0, _re);
    }

    // 正規表現 delim で n 個に分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    static inline std::vector<String> reSplit(StringRef s, size_t n, const std::wregex& reDelim) {
        return utils::reSplit(s, n, reDelim);
    }

    // 正規表現 delim で n 個に分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    static inline std::vector<String> reSplit(StringRef s, size_t n, StringRef delim) {
        return reSplit(s, n, std::wregex(delim));
    }

    // 正規表現 delim で分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    static inline std::vector<String> reSplit(StringRef s, const std::wregex& reDelim) {
        return reSplit(s, 0, reDelim);
    }

    // 正規表現 delim で分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    static inline std::vector<String> reSplit(StringRef s, StringRef delim) {
        return reSplit(s, 0, std::wregex(delim));
    }

    // 正規表現 pで置換する
    String replace(StringRef s, StringRef r) {
        return std::regex_replace(s, _re, r);
    }

    // 正規表現 pで置換する
    static inline String reReplace(StringRef s, const std::wregex& re, StringRef r) {
        return std::regex_replace(s, re, r);
    }

    // 正規表現 pで置換する
    static inline String reReplace(StringRef s, const String& re, StringRef r) {
        return std::regex_replace(s, std::wregex(re), r);
    }

    // 正規表現にマッチする要素を取り出す
    // 正規表現 p は s 全体にマッチする必要がある。
    // 最初のカッコにマッチするものが戻値の 0 番目の要素になる
    std::vector<String> scan(StringRef s) {
        return utils::reScan(s, _re);
    }

    // 正規表現にマッチする要素を取り出す
    // 正規表現 p は s 全体にマッチする必要がある。
    // 最初のカッコにマッチするものが戻値の 0 番目の要素になる
    static inline std::vector<String> reScan(StringRef s, const std::wregex& re) {
        return utils::reScan(s, re);
    }

    // 正規表現にマッチする要素を取り出す
    // 正規表現 p は s 全体にマッチする必要がある。
    // 最初のカッコにマッチするものが戻値の 0 番目の要素になる
    static inline std::vector<String> reScan(StringRef s, const String& re) {
        return reScan(s, std::wregex(re));
    }

};

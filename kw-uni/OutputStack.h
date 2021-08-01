#pragma once

//#include "pch.h"
#include "string_type.h"
#include "misc_utils.h"

#include "Logger.h"

// 出力文字のスタック
class OutputStack {
    DECLARE_CLASS_LOGGER;

public:
    static const unsigned short FLAG_NEW_LINE = 1;
    static const unsigned short FLAG_BLOCK_HIST = 2;

private:
    struct Moji {
        uint32_t flag = 0;
        mchar_t chr = 0;

        Moji() : flag(0), chr(0) { }

        Moji(mchar_t ch) : flag(ch == '\n' ? FLAG_NEW_LINE : 0), chr(ch) { }
    };

public:
    inline void push(const MString& str) {
        utils::transform_append(str, stack, [](mchar_t ch) { return Moji(ch);});
        resize();
    }

    inline void push(const mchar_t* str) {
        push(MString(str));
    }

    inline void push(const wstring& str) {
        push(to_mstr(str));
    }

    inline void push(const wchar_t* str) {
        push(to_mstr(str));
    }

    inline void push(mchar_t ch) {
        stack.push_back(Moji(ch));
        resize();
    }

    // 改行は、全ブロッカーとなる
    inline void pushNewLine() {
        push('\n');
        setFlag(FLAG_NEW_LINE);
    }

    inline void popNewLine() {
        if (!stack.empty() && stack.back().chr == '\n') stack.pop_back();
    }

    inline void pop(size_t n) {
        if (n > 0) {
            if (n > stack.size()) n = stack.size();
            n = utils::minimum(tail_size(), n);
            if (n > 0) {
                stack.resize(stack.size() - n);
            }
        }
    }

    inline void pop() {
        pop(1);
    }

    inline mchar_t back() const {
        return stack.empty() ? 0 : stack.back().chr;
    }

    inline mchar_t back(size_t n) const {
        return (n + 1) <= stack.size() ? stack[stack.size() - (n + 1)].chr : 0;
    }

    inline unsigned short getFlag() const {
        return stack.empty() ? 0 : (unsigned short)stack.back().flag;
    }

    inline bool isLastBlocker() const {
        return back() == '\n' || (getFlag() & (FLAG_NEW_LINE | FLAG_BLOCK_HIST)) != 0;
    }

    inline void setFlag(unsigned short flag) {
        if (!stack.empty()) {
            stack.back().flag |= flag;
        }
    }

    inline void unsetFlag(unsigned short flag) {
        if (!stack.empty()) {
            stack.back().flag &= ~flag;
        }
    }

    inline void clearFlag() {
        if (!stack.empty()) {
            stack.back().flag = 0;
        }
    }

    // 末尾文字のブロックフラグをクリアし、さらに改行だったらそれを除去する
    inline void clearFlagAndPopNewLine() {
        while (isLastBlocker()) {
            clearFlag();
            popNewLine();
        }
    }

    // 改行を含まない末尾部分で、flag の直後までの長さ
    inline size_t tail_size_upto(unsigned short flag) const {
        if (size() == 0) return 0;
        size_t pos = size();
        while (pos > 0) {
            if (stack[pos - 1].chr == '\n' || (stack[pos - 1].flag & flag) != 0) break;
            --pos;
        }
        return size() - pos;
    }

    // 改行を含まない末尾部分の長さ
    inline size_t tail_size() const {
        return tail_size_upto(0);
    }

    // 改行を含まない末尾部分(最大長maxlen)を返す
    inline MString BackStringUptoNewLine(size_t maxlen) const {
        return tail_string(maxlen, tail_size());
    }

    // 改行を含まない末尾部分(最大長maxlen)で、指定の flag の直後までの部分文字列を返す
    inline MString backStringUpto(size_t maxlen, unsigned short flag) const {
        return tail_string(maxlen, tail_size_upto(flag));
    }

    // 改行を含まない末尾部分(最大長maxlen)で、何かflagがあれば | を付加した部分文字列を返す
    inline MString backStringWithFlagUpto(size_t maxlen) const {
        return tail_string(maxlen, tail_size(), true);
    }

    inline MString backStringFull(size_t maxlen) const {
        return tail_string(maxlen, size());
    }

    inline size_t size() const { return stack.size(); }

    //inline const OutputStack& OutStack() { return outputStack; }

    inline MString OutputStackBackStr(size_t len) const { return backStringFull(len); }
    wstring OutputStackBackStrForDebug(size_t len) const;

    inline mchar_t OutputStackLastChar() const { return back(); }
    inline mchar_t LastOutStackChar() const { return OutputStackLastChar(); }
    inline mchar_t GetLastOutputStackChar() const { return OutputStackLastChar(); }
    inline mchar_t OutputStackLastChar(size_t n) const { return back(n); }
    inline mchar_t LastOutStackChar(size_t n) const { return back(n); }

    inline MString OutputStackBackStrUpto(size_t len, unsigned short flag = 0) const { return backStringUpto(len, flag); }
    inline MString GetLastOutputStackStr(size_t len, unsigned short flag = 0) const { return OutputStackBackStrUpto(len, flag); }

    inline MString GetLastOutputStackStrUptoNL(size_t len) const { return GetLastOutputStackStr(len, OutputStack::FLAG_NEW_LINE); }

    // ブロッカーを反映した文字列を取得
    inline MString OutputStackBackStrWithFlagUpto(size_t len) const { return backStringWithFlagUpto(len); }

    inline bool isLastOutputStackCharBlocker() const {
        return isLastBlocker();
    }

    inline bool isLastOutputStackCharHirakana() const {
        return utils::is_hirakana(GetLastOutputStackChar());
    }

    inline bool isLastOutputStackCharKanjiOrKatakana() const {
        return utils::is_kanji_or_katakana_except_nakaguro(wchar_t(GetLastOutputStackChar()));
    }

    // 改行以降で、出力履歴の末尾から len 文字までのカタカナor漢字文字列を取得する
    template<typename T>
    inline T GetLastKanjiOrKatakanaStr(size_t len = 20) const {
        return utils::find_tail_kanji_or_katakana_str(backStringUpto(len, OutputStack::FLAG_NEW_LINE));
    }

    // 改行以降で、出力履歴の末尾から len 文字までのカタカナ文字列を取得する
    template<typename T>
    inline T GetLastKatakanaStr(size_t len = 20) const {
        return utils::find_tail_katakana_str(backStringUpto(len, OutputStack::FLAG_NEW_LINE));
    }

    // 改行以降で、出力履歴の末尾から len 文字までの日本語文字列を取得する
    template<typename T>
    inline T GetLastJapaneseStr(size_t len) const {
        return utils::find_tail_japanese_str(backStringUpto(len, OutputStack::FLAG_NEW_LINE));
    }

    // ブロッカー以降で、出力履歴の末尾から len 文字までのカタカナor漢字文字列を取得する
    template<typename T>
    inline T GetLastKanjiOrKatakanaKey() const {
        return utils::find_tail_kanji_or_katakana_str(backStringUpto(20, OutputStack::FLAG_BLOCK_HIST));
    }

    // ブロッカー以降で、出力履歴の末尾から len 文字までの平仮名文字列を取得する
    template<typename T>
    inline T GetLastHiraganaStr() const {
        return utils::find_tail_hiragana_str(backStringUpto(20, OutputStack::FLAG_BLOCK_HIST));
    }

    // ブロッカー以降で、出力履歴の末尾から len 文字までの日本語文字列を取得する
    template<typename T>
    inline T GetLastJapaneseKey(size_t len) const {
        return utils::find_tail_japanese_str(backStringUpto(len, OutputStack::FLAG_BLOCK_HIST));
    }

    // ブロッカー以降で、出力履歴の末尾から len 文字までのASCII文字列を取得する
    template<typename T>
    inline T GetLastAsciiKey(size_t len) const {
        return utils::find_tail_ascii_str(backStringUpto(len, OutputStack::FLAG_BLOCK_HIST));
    }

    // 出力履歴の末尾から4文字以上(ただしブロッカー以降)の漢字列またはカタカナ列をとり出す
    // 3文字以下だったら、ひらがなも含めて4文字まで取り出す
    // 最後は10文字までのASCII文字列を取り出す(abbrev用)
    template<typename T>
    inline T GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey() const {
        T key = GetLastKanjiOrKatakanaKey<T>();
        if (key.size() >= 4) return key;
        key = GetLastJapaneseKey<T>(4);
        if (!key.empty()) return key;
        return GetLastAsciiKey<T>(10);
    }

private:
    std::vector<Moji> stack;

    void resize();

    // stackの末尾から、tailMaxlen の範囲で、tailLen の長さの文字列を取得する
    // 例: stack="fooBarHoge", tailLen=4, tailMaxLen=7 なら "Hoge" を返す
    // bWithFlag = true なら、FLAG のセットしてある文字の後に "|" を付加する
    MString tail_string(size_t tailLen, size_t tailMaxlen, bool bWithFlag = false) const;

public:
    static std::unique_ptr<OutputStack> Singleton;

    static void CreateSingleton();

}; // class OutputStack

#define OUTPUT_STACK (OutputStack::Singleton)

#undef OUTPUT_STACK_MAXSIZE


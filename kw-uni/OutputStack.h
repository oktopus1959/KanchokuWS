#pragma once

//#include "pch.h"
#include "string_type.h"
#include "misc_utils.h"
#include "transform_utils.h"

#include "Logger.h"

// 出力文字のスタック
class OutputStack {
    DECLARE_CLASS_LOGGER;

public:
    static const unsigned short FLAG_NEW_LINE = 1;
    static const unsigned short FLAG_BLOCK_HIST = 2;
    static const unsigned short FLAG_BLOCK_MAZE = 4;
    static const unsigned short FLAG_BLOCK_KATA = 8;
    static const unsigned short FLAG_REWRITABLE = 16;
    static const unsigned short FLAG_REWRITABLE_BEGIN = 32;

    static const size_t HIST_KEY_MAX_LEN = 8;   // 履歴用のキーの最大長
    static const size_t HIST_ROMAN_KEY_MAX_LEN = 16;   // 英字履歴用のキーの最大長

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
        _resize();
    }

    inline void push(const mchar_t* str) {
        push(MString(str));
    }

    inline void push(StringRef str) {
        push(to_mstr(str));
    }

    inline void push(const wchar_t* str) {
        push(to_mstr(str));
    }

    inline void push(mchar_t ch) {
        stack.push_back(Moji(ch));
        _resize();
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
            uint32_t lastFlag = 0;
            if (n > stack.size()) n = stack.size();
            n = utils::minimum(tail_size(), n);
            if (n > 0) {
                lastFlag = stack[stack.size() - n].flag;
                stack.resize(stack.size() - n);
            }
            if ((lastFlag & FLAG_REWRITABLE) != 0 && (lastFlag & FLAG_REWRITABLE_BEGIN) == 0) {
                // 書き換え可能フラグが途中まで削除されたら、書き換え可能フラグ先頭までフラグを削除する
                for (size_t pos = stack.size(); pos > 0; --pos) {
                    stack[pos - 1].flag &= ~FLAG_REWRITABLE;
                    if ((stack[pos - 1].flag & FLAG_REWRITABLE_BEGIN) != 0) break;
                }
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

    //inline void clearLastHistBlocker() {
    //    if (!stack.empty()) {
    //        stack.back().flag &= ~FLAG_BLOCK_HIST;
    //    }
    //}

    inline void toggleLastBlocker() {
        if (!stack.empty()) {
            if (_isLastHistBlocker())
                stack.back().flag &= ~FLAG_BLOCK_HIST;
            else
                stack.back().flag |= FLAG_BLOCK_HIST;
            if (isLastMazeBlocker())
                stack.back().flag &= ~FLAG_BLOCK_MAZE;
            else
                stack.back().flag |= FLAG_BLOCK_MAZE;
        }
    }

    inline void setLastBlocker() {
        if (!stack.empty()) {
            stack.back().flag |= FLAG_BLOCK_HIST;
            stack.back().flag |= FLAG_BLOCK_MAZE;
        }
    }

    inline void setHistBlockerAt(size_t lastNth) {
        if (stack.size() > lastNth) {
            stack[stack.size() - lastNth - 1].flag |= FLAG_BLOCK_HIST;
        }
    }

    inline void setHistBlocker() {
        setFlag(FLAG_BLOCK_HIST);
    }

    // 末尾にカタカナ変換用の平仮名採取ブロッカーをセット
    inline void setKataBlocker() {
        setFlag(FLAG_BLOCK_KATA);
    }

    inline void unsetKataBlocker() {
        unsetFlag(FLAG_BLOCK_KATA);
    }

    inline bool isLastKataBlocker() const {
        return (getFlag() & FLAG_BLOCK_KATA) != 0;
    }

    inline void setMazeBlocker(size_t lastNth) {
        if (stack.size() > lastNth) {
            stack[stack.size() - lastNth - 1].flag |= FLAG_BLOCK_MAZE;
        }
        setKataBlocker();
    }

    // 書き換え可能フラグ(末尾 lastNum 文字にセット)
    inline void setRewritable(size_t lastNum) {
        if (!stack.empty() && lastNum > 0) {
            for (size_t i = 0; i < lastNum; ++i) {
                if (i >= stack.size()) break;
                stack[stack.size() - i - 1].flag |= FLAG_REWRITABLE;
            }
            size_t beg = lastNum < stack.size() ? stack.size() - lastNum : 0;
            stack[beg].flag |= FLAG_REWRITABLE_BEGIN;   // 書き換え可能先頭フラグをセットしておく
        }
    }

    // 書き換え可能フラグをキャンセル
    inline void cancelRewritable() {
        if (!stack.empty()) {
            stack.back().flag &= ~FLAG_REWRITABLE;
        }
    }

    // 末尾に交ぜ書きブロッカーをセットする
    inline void setMazeBlocker() {
        setFlag(FLAG_BLOCK_MAZE);
        setKataBlocker();
    }

    // 末尾の交ぜ書きブロッカーをリセットする
    inline void unsetMazeBlocker() {
        unsetFlag(FLAG_BLOCK_MAZE);
    }

    inline bool isLastMazeBlocker() const {
        return (getFlag() & FLAG_BLOCK_MAZE) != 0;
    }

    inline void leftShiftMazeBlocker() {
        if (!stack.empty()) {
            size_t pos = 1;
            for (; pos < stack.size(); ++pos) {
                Moji& elem = stack[stack.size() - pos];
                auto flag = elem.flag & (FLAG_BLOCK_HIST | FLAG_BLOCK_MAZE);
                //if (!utils::is_hiragana(elem.chr)) {
                //    // 交ぜ書きブロッカーが見つからなかった場合は、末尾にブロッカーをセット
                //    if (pos > 1 && flag == 0) setMazeBlocker();
                //    return;
                //}
                if (flag != 0) {
                    Moji& elem1 = stack[stack.size() - (pos + 1)];
                    elem.flag &= ~(FLAG_BLOCK_HIST | FLAG_BLOCK_MAZE);
                    elem1.flag |= (flag | FLAG_BLOCK_MAZE);
                    return;
                }
            }
            // 交ぜ書きブロッカーが見つからなかった場合は、末尾にブロッカーをセット
            setMazeBlocker();
        }
    }

    inline void rightShiftMazeBlocker() {
        if (!stack.empty()) {
            for (size_t pos = 1; pos < stack.size(); ++pos) {
                Moji& elem = stack[stack.size() - pos];
                auto flag = elem.flag & (FLAG_BLOCK_HIST | FLAG_BLOCK_MAZE);
                if (elem.chr < 0x20 || flag != 0) {
                    if (pos > 1) {
                        Moji& elem1 = stack[stack.size() - (pos - 1)];
                        elem.flag &= ~(FLAG_BLOCK_HIST | FLAG_BLOCK_MAZE);
                        elem1.flag |= (flag | FLAG_BLOCK_MAZE);
                    }
                    return;
                }
            }
        }
    }

    inline void setFlag(unsigned short flag) {
        if (!stack.empty()) {
            stack.back().flag |= flag;
        }
    }

    // 末尾のフラグをリセットする
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
        while (_isLastBlocker()) {
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

    // 改行を含まない末尾部分で、flag のみが続く長さ
    inline size_t tail_size_while_only(unsigned short flag) const {
        if (size() == 0) return 0;
        size_t pos = size();
        while (pos > 0) {
            if (stack[pos - 1].chr == '\n' || stack[pos - 1].flag != flag) break;
            --pos;
        }
        return size() - pos;
    }

//#define __LOG_DEBUGH LOG_INFOH
#define __LOG_DEBUGH LOG_DEBUGH

    // 改行を含まない末尾部分で、flag のみが続き、flagUpto が来るまでの最大長
    inline size_t tail_size_while_only_and_upto(size_t maxlen, unsigned short flag, unsigned short flagUpto) const {
        if (size() == 0) return 0;
        size_t pos = size();
        size_t minpos = maxlen >= size() ? 0 : size() - maxlen;
        size_t maxpos = pos;
        __LOG_DEBUGH(_T("ENTER: size={}, maxlen={}, pos={}, minpos={}, maxpos={}"), size(), maxlen, pos, minpos, maxpos);
        while (pos > minpos) {
            size_t npos = pos - 1;
            __LOG_DEBUGH(_T("npos={}, chr={}, flag={:x}"), npos, (wchar_t)stack[npos].chr, stack[npos].flag);
            if ((stack[npos].flag & flagUpto) != 0 && (stack[npos].flag & ~flagUpto) == flag) {
                maxpos = npos;
                __LOG_DEBUGH(_T("new maxpos={}"), maxpos);
            }
            if (stack[npos].chr == '\n' || (stack[npos].flag & ~flagUpto) != flag) {
                __LOG_DEBUGH(_T("break"));
                break;
            }
            pos = npos;
        }
        __LOG_DEBUGH(_T("LEAVE: result len={}"), size() - maxpos);
        return size() - maxpos;
    }
#undef __LOG_DEBUGH

    // 改行を含まない末尾部分で、句読点の直後までの長さ(ただし末尾句読点は含める)
    inline size_t tail_size_upto_flag_or_punct(unsigned short flag) const {
        if (size() == 0) return 0;
        bool otherThanPunct = false;
        size_t pos = size();
        while (pos > 0) {
            auto m = stack[pos - 1];
            if (m.chr == '\n' || (m.flag & flag) != 0) break;

            if (m.chr == ' ') {
                // 空白なら、それを含む
                --pos;
                break;
            }

            if (utils::is_punct(m.chr)) {
                if (otherThanPunct) break;     // 句読点以外のものが存在し、再び句読点が見つかったので、ここで終わり
                // 初めに見つかたった(つまり末尾の)句読点はスルー
            } else {
                otherThanPunct = true;
            }
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

    // 改行を含まない末尾部分(最大長maxlen)で、履歴ブロッカーまたは句読点までの部分文字列を返す(先頭の空白と末尾の句読点は含める)
    inline MString BackStringUptoHistBlockerOrPunct(size_t maxlen) const {
        return tail_string(maxlen, tail_size_upto_flag_or_punct(OutputStack::FLAG_BLOCK_HIST));
    }

    // 改行を含まない末尾部分の長さ(最大長maxlen)で、交ぜ書きor履歴ブロッカーまたは句読点までの部分文字列を返す(先頭の空白と末尾の句読点は含める)
    inline size_t TailSizeUptoMazeOrHistBlockerOrPunct() const {
        return tail_size_upto_flag_or_punct(OutputStack::FLAG_BLOCK_HIST | OutputStack::FLAG_BLOCK_MAZE);
    }

    // 改行を含まない末尾部分(最大長maxlen)で、交ぜ書きor履歴ブロッカーまたは句読点までの部分文字列を返す(先頭の空白と末尾の句読点は含める)
    inline MString BackStringUptoMazeOrHistBlockerOrPunct(size_t maxlen) const {
        return tail_string(maxlen, TailSizeUptoMazeOrHistBlockerOrPunct());
    }

    // 改行を含まない末尾部分(最大長maxlen)で、指定の flag の直後までの部分文字列を返す
    inline MString backStringUpto(size_t maxlen, unsigned short flag) const {
        return tail_string(maxlen, tail_size_upto(flag));
    }

    // 改行を含まない末尾部分(最大長maxlen)で、指定の flag のみが続いている部分文字列を返す
    inline MString backStringWhileOnly(size_t maxlen, unsigned short flag) const {
        return tail_string(maxlen, tail_size_while_only(flag));
    }

    // 改行を含まない末尾部分(最大長maxlen)で、REWRITABLE のみが続いている部分文字列を返す
    inline MString backStringWhileOnlyRewritable(size_t maxlen) const {
        return tail_string(maxlen, tail_size_while_only_and_upto(maxlen, FLAG_REWRITABLE, FLAG_REWRITABLE_BEGIN));
    }

    // 改行を含まない末尾部分(最大長maxlen)で、何かflagがあれば | を付加した部分文字列を返す
    inline MString backStringWithFlagUpto(size_t maxlen, size_t extraBarPos = 0) const {
        return tail_string(maxlen, tail_size(), true, extraBarPos);
    }

    inline MString backStringFull(size_t maxlen) const {
        return tail_string(maxlen, size());
    }

    inline size_t size() const { return stack.size(); }

    inline size_t isEmpty() const { return stack.empty(); }

    //inline const OutputStack& OutStack() { return outputStack; }

    inline MString OutputStackBackStr(size_t len) const { return backStringFull(len); }
    String OutputStackBackStrForDebug(size_t len) const;

    inline mchar_t OutputStackLastChar() const { return back(); }
    inline mchar_t LastOutStackChar() const { return OutputStackLastChar(); }
    inline mchar_t GetLastOutputStackChar() const { return OutputStackLastChar(); }
    inline mchar_t OutputStackLastChar(size_t n) const { return back(n); }
    inline mchar_t LastOutStackChar(size_t n) const { return back(n); }

    inline MString OutputStackBackStrUpto(size_t len, unsigned short flag = 0) const { return backStringUpto(len, flag); }
    inline MString GetLastOutputStackStr(size_t len, unsigned short flag = 0) const { return OutputStackBackStrUpto(len, flag); }

    inline MString GetLastOutputStackStrUptoNL(size_t len) const { return GetLastOutputStackStr(len, OutputStack::FLAG_NEW_LINE); }
    inline MString GetLastOutputStackStrUptoBlocker(size_t len) const { return GetLastOutputStackStr(len, OutputStack::FLAG_NEW_LINE | OutputStack::FLAG_BLOCK_HIST); }

    // ブロッカーを反映した文字列を取得
    inline MString OutputStackBackStrWithFlagUpto(size_t len, size_t extraBarPos = 0) const { return backStringWithFlagUpto(len, extraBarPos); }

    inline bool isLastOutputStackCharBlocker() const {
        return _isLastBlocker();
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

    // ブロッカー以降で、出力履歴の末尾から len 文字までのカタカナ文字列を取得する
    template<typename T>
    inline T GetLastKatakanaStr(size_t len = 20) const {
        return utils::find_tail_katakana_str(backStringUpto(len, OutputStack::FLAG_BLOCK_HIST));
    }

    // ブロッカー以降で、出力履歴の末尾から len 文字までの半角カタカナ文字列を取得する
    template<typename T>
    inline T GetLastHankakuKatakanaStr(size_t len = 20) const {
        return utils::find_tail_hankaku_katakana_str(backStringUpto(len, OutputStack::FLAG_BLOCK_HIST));
    }

    // 改行以降で、出力履歴の末尾から len 文字までの日本語文字列を取得する
    template<typename T>
    inline T GetLastJapaneseStr(size_t len) const {
        return utils::find_tail_japanese_str(backStringUpto(len, OutputStack::FLAG_NEW_LINE));
    }

    // ブロッカー以降で、出力履歴の末尾から len 文字までのカタカナor漢字文字列を取得する
    template<typename T>
    inline T GetLastKanjiOrKatakanaKey(size_t len) const {
        return utils::find_tail_kanji_or_katakana_str(backStringUpto(len, OutputStack::FLAG_BLOCK_HIST));
    }

    // ブロッカー以降で、出力履歴の末尾から len 文字までの平仮名文字列を取得する (bHeadSpace=trueなら先頭の空白も含む)
    template<typename T>
    inline T GetLastHiraganaStr(bool bHeadSpace = false) const {
        return utils::find_tail_hiragana_str(backStringUpto(20, OutputStack::FLAG_BLOCK_HIST + OutputStack::FLAG_BLOCK_KATA), bHeadSpace);
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
    // 末尾に漢字、カタカナ、ひらがながなかったら、ASCII文字列を取り出す
    template<typename T>
    inline T GetLastKanjiOrKatakanaOrHirakanaOrAsciiKey(size_t asciiMaxLen) const {
        T key = GetLastKanjiOrKatakanaKey<T>(HIST_KEY_MAX_LEN);
        if (key.size() >= 4) return key;
        key = GetLastJapaneseKey<T>(4);
        if (!key.empty()) return key;
        return GetLastAsciiKey<T>(asciiMaxLen);
    }

    // 末尾のアルファベット文字列をクリアする
    inline void ClearTailAlaphabetStr() {
        size_t pos = 0;
        for (; pos < stack.size(); ++pos) {
            if (!is_alphabet(back(pos))) break;
        }
        if (pos > 0) pop(pos);
    }

    // 末尾にひらがな採取ブロッカーをセットする
    inline void SetHiraganaBlocker() {
        setKataBlocker();
    }

    // 末尾のひらがな文字列をクリアする
    inline void ClearTailHiraganaStr() {
        size_t pos = 0;
        size_t len = tail_size_upto(FLAG_BLOCK_KATA);
        for (; pos < len; ++pos) {
            if (!utils::is_hiragana(back(pos))) break;
        }
        if (pos > 0) pop(pos);
    }

private:
    // 出力文字スタック
    std::vector<Moji> stack;

    void _resize();

    inline bool _isLastBlocker() const {
        return back() == '\n' || (getFlag() & (FLAG_NEW_LINE | FLAG_BLOCK_HIST)) != 0;
    }

    inline bool _isLastHistBlocker() const {
        return (getFlag() & FLAG_BLOCK_HIST) != 0;
    }

    // stackの末尾から、tailMaxlen の範囲で、tailLen の長さの文字列を取得する
    // 例: stack="fooBarHoge", tailLen=4, tailMaxLen=7 なら "Hoge" を返す
    // bWithFlag = true なら、FLAG のセットしてある文字の後に "|" を付加する
    MString tail_string(size_t tailLen, size_t tailMaxlen, bool bWithFlag = false, size_t extraBarPos = 0) const;

public:
    static std::unique_ptr<OutputStack> Singleton;

    static void CreateSingleton();

}; // class OutputStack

#define OUTPUT_STACK (OutputStack::Singleton)

#undef OUTPUT_STACK_MAXSIZE


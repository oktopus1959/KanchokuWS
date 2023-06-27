#pragma once

#include <regex>
#include <stdarg.h>

#include "std_utils.h"
#include "string_type.h"
#include "langedge/ctypeutil.hpp"

//#define _WC(p) (_T(p)[0])

namespace {

    const wchar_t VERT_BAR = '|';     // |

    const wchar_t HASH_MARK = '#';    // #

    const wchar_t TOTEN = 0x3001;     // 、

    const wchar_t KUTEN = 0x3002;     // 。

    const wchar_t NAKAGURO = 0x30fb;  // '・'

    const wchar_t HAN_NAKAGURO = 0xff65;  // '・'

    const wchar_t CHOON = 0x30fc;     // 'ー'

    const wchar_t HAN_CHOON = 0xff70; // 'ー'

    const wchar_t QUESTION_MARK = 0xff1f;   // '？'

    inline MString to_mstr(mchar_t x) {
        return x != 0 ? MString(1, x) : MString();
    }

    const MString EMPTY_MSTR;

    const MString MSTR_SPACE = to_mstr(' ');

    const MString MSTR_CHOON = to_mstr(_T("ー")[0]);

    const MString MSTR_THREE_DOTS = to_mstr(L'…');

    const MString MSTR_VERT_BAR = to_mstr('|');

    const MString MSTR_HASH_MARK = to_mstr('#');

    const MString MSTR_CMD_HEADER = MString({mchar_t('!'), mchar_t('{')});

    const mchar_t strip_delims[] = {' ', '\r', '\n' };

    inline bool is_paired_mchar(mchar_t m) {
        return (m & 0xffff0000) != 0;
    }

    inline wchar_t to_upper(wchar_t ch) { return wchar_t(langedge::CtypeUtil::toUpper(ch)); }

    inline mchar_t to_upper(mchar_t ch) { return mchar_t(langedge::CtypeUtil::toUpper(ch)); }

    inline wchar_t to_lower(wchar_t ch) { return wchar_t(langedge::CtypeUtil::toLower(ch)); }

    inline mchar_t to_lower(mchar_t ch) { return mchar_t(langedge::CtypeUtil::toLower(ch)); }

    //inline bool is_ascii_char(mchar_t ch) {
    //    return ch >= 0x20 && ch <= 0x7f;
    //}

    inline bool is_ascii_char(char32_t ch) {
        return ch >= 0x20 && ch <= 0x7f;
    }

    inline bool is_lower_alphabet(mchar_t ch) {
        return (ch >= 'a' && ch <= 'z');
    }

    inline bool is_upper_alphabet(mchar_t ch) {
        return (ch >= 'A' && ch <= 'Z');
    }

    inline bool is_alphabet(mchar_t ch) {
        return is_upper_alphabet(ch) || is_lower_alphabet(ch);
    }

    inline bool is_numeral(wchar_t ch) {
        return ch >= '0' && ch <= '9';
    }

    inline bool is_wide_numeral(wchar_t ch) {
        return ch >= L'０' && ch <= L'９';
    }

    inline wchar_t make_halfwide_nummeral(wchar_t ch) {
        return (ch - L'０') + '0';
    }

    inline bool is_high_surrogate(wchar_t ch) {
        return ch >= 0xd800 && ch <= 0xdbff;
    }

    inline bool is_low_surrogate(wchar_t ch) {
        return ch >= 0xdc00 && ch <= 0xdfff;
    }

    inline bool is_surrogate_pair(wchar_t ch1, wchar_t ch2) {
        return is_high_surrogate(ch1) && is_low_surrogate(ch2);
    }

    inline bool is_surrogate_pair(const wchar_t* wp) {
        return wp[0] != 0 && wp[1] != 0 && is_surrogate_pair(wp[0], wp[1]);
    }

    inline bool is_ascii_pair(wchar_t ch1, wchar_t ch2) {
        return is_ascii_char(ch1) && is_ascii_char(ch2);
    }

    inline bool is_ascii_pair(StringRef ws) {
        return ws.size() >= 2 && is_ascii_pair(ws[0], ws[1]);
    }

    inline bool is_ascii_pair(const MString& ws) {
        return ws.size() >= 2 && is_ascii_pair(wchar_t(ws[0]), wchar_t(ws[1]));
    }

    inline bool is_ascii_str(const MString& s) {
        if (s.empty()) return false;
        for (auto c : s) {
            if (!is_ascii_char(c)) return false;
        }
        return true;
    }

    inline bool is_ascii_str(StringRef s) {
        if (s.empty()) return false;
        for (auto c : s) {
            if (!is_ascii_char(c)) return false;
        }
        return true;
    }

    inline bool is_roman_str(const MString& s) {
        if (s.empty()) return false;
        for (auto c : s) {
            if (!is_alphabet(c)) return false;
        }
        return true;
    }

    inline bool is_roman_str(StringRef s) {
        if (s.empty()) return false;
        for (auto c : s) {
            if (!is_alphabet(c)) return false;
        }
        return true;
    }

    // ワイルドカード('?' or '*')か
    inline bool is_wildcard(mchar_t mch) { return mch == '?' || mch == '*'; }

    // 丸数字
    inline wchar_t make_enclosed_alphanumeric(int n) {
        if (n >= 0 && n < 160)
            return wchar_t(0x2460 + n);
        else
            return 0;
    }

    // 全角文字
    inline wchar_t make_fullwide_char(int ch) {
        return ch == 0x20 ? 0x3000 : wchar_t(0xff00 + (ch - 0x20));
    }

    MojiPair decomp_mchar(mchar_t m) {
        return MojiPair{ static_cast<wchar_t>(m >> 16), static_cast<wchar_t>(m & 0xffff) };
    }

    mchar_t make_mchar(wchar_t first, wchar_t second) {
        return mchar_t((first << 16) + second);
    }

    mchar_t make_mchar(const wchar_t* wp, size_t* ppos = 0) {
        size_t pos = ppos != 0 ? *ppos : 0;
        if (is_surrogate_pair(wp + pos)) {
            if (ppos != 0) *ppos = pos + 2;
            return make_mchar(wp[pos], wp[pos + 1]);
        } else {
            if (ppos != 0) *ppos = pos + 1;
            return wp[pos];
        }
    }

    mchar_t make_mchar(StringRef ws) {
        return ws.empty() ? 0 : ws.size() == 1 || !is_surrogate_pair(ws[0], ws[1]) ? ws[0] : make_mchar(ws[0], ws[1]);
    }

    mchar_t make_mchar(StringRef ws, size_t* ppos) {
        size_t pos = *ppos;
        *ppos = pos + 1;
        if (pos >= ws.size()) {
            return 0;
        }
        if (pos + 1 == ws.size() || !is_surrogate_pair(ws[pos], ws[pos + 1])) {
            return ws[pos];
        }
        *ppos = pos + 2;
        return make_mchar(ws[pos], ws[pos + 1]);
    }

    mchar_t make_mchar_with_ascii(StringRef ws) {
        return ws.empty() ? 0 : ws.size() == 1 || !is_surrogate_pair(ws[0], ws[1]) || !is_ascii_pair(ws[0], ws[1]) ? ws[0] : make_mchar(ws[0], ws[1]);
    }

    MString to_mstr(StringRef ws) {
        MString result;
        size_t pos = 0;
        while (pos < ws.size()) result.push_back(make_mchar(ws, &pos));
        return result;
    }

    MString to_mstr(const wchar_t* wp) {
        MString result;
        size_t pos = 0;
        while (wp[pos] != 0) {
            result.push_back(make_mchar(wp, &pos));
        }
        return result;
    }

    MString to_mstr(const wchar_t* wp, size_t len) {
        MString result;
        size_t pos = 0;
        while (pos < len) {
            result.push_back(make_mchar(wp, &pos));
        }
        return result;
    }

    const MString MSTR_VERT_BAR_2 = to_mstr(_T("||"));

    void push_back_wstr(mchar_t m, String& ws) {
        auto mp = decomp_mchar(m);
        if (mp.first != 0) ws.push_back(mp.first);
        if (mp.second != 0) ws.push_back(mp.second);
    }

    String to_wstr(mchar_t m) {
        String result;
        push_back_wstr(m, result);
        return result;
    }

    String to_wstr(const MString& mstr) {
        String result;
        for (auto m : mstr) {
            push_back_wstr(m, result);
        }
        return result;
    }

    String to_wstr(const std::vector<mchar_t>& mstr) {
        String result;
        for (auto m : mstr) {
            push_back_wstr(m, result);
        }
        return result;
    }

    String to_wstr(const std::vector<mchar_t>& mstr, size_t begin, size_t len) {
        String result;
        size_t end = begin + len;
        if (end > mstr.size()) end = mstr.size();
        for (size_t i = begin; i < end; ++i) {
            push_back_wstr(mstr[i], result);
        }
        return result;
    }

    // cpyLen で指定された長さの文字列を wp 配列に追加する。末尾には 0 が付加される。wp は十分な長さを確保しておくこと。コピーした長さを返す。
    size_t append_wstr(StringRef ws, wchar_t* wp, size_t cpyLen) {
        const size_t maxlen = 1024;
        size_t p = 0;
        for (; p < maxlen - 1; ++p) {
            if (wp[p] == 0) break;
        }
        size_t i = 0;
        for (; i < cpyLen; ++i) {
            if (i >= ws.size() || p + i >= maxlen - 1) break;
            wp[p + i] = ws[i];
        }
        wp[p + i] = 0;
        return i;
    }

    // cpyLen で指定された長さの文字列を wp 配列にコピーする。末尾には 0 が付加される。wp は十分な長さを確保しておくこと。コピーした長さを返す。
    size_t copy_mstr(const MString& ms, wchar_t* wp, size_t cpyLen) {
        wp[0] = 0;
        return append_wstr(to_wstr(ms), wp, cpyLen);
    }

    // cpyLen で指定された長さの文字列を wp 配列に追加する。末尾には 0 が付加される。wp は十分な長さを確保しておくこと。コピーした長さを返す。
    size_t append_mstr(const MString& ms, wchar_t* wp, size_t cpyLen) {
        return append_wstr(to_wstr(ms), wp, cpyLen);
    }

    // mchar を wp 配列に追加する。末尾には 0 が付加される。wp は十分な長さを確保しておくこと。コピーした長さを返す。
    size_t append_mchar(mchar_t mch, wchar_t* wp) {
        return append_wstr(to_wstr(mch), wp, 2);
    }

    String make_wstring(wchar_t a, wchar_t b) {
        String result(2, 0);
        result[0] = a;
        result[1] = b;
        return result;
    }

    String make_wstring(wchar_t c, wchar_t a, wchar_t b) {
        String result(3, 0);
        result[0] = c;
        result[1] = a;
        result[2] = b;
        return result;
    }

    MString make_mstring(mchar_t a, mchar_t b) {
        MString result(2, 0);
        result[0] = a;
        result[1] = b;
        return result;
    }

    MString make_mstring(mchar_t c, mchar_t a, mchar_t b) {
        MString result(3, 0);
        result[0] = c;
        result[1] = a;
        result[2] = b;
        return result;
    }
} // namespace

namespace utils
{
#define UTILS_BUFSIZ 2048

#ifdef _WINDOWS_
    // hWnd で指定されたウインドウのクラス名を取得する
    inline String getClassNameFromHwnd(HWND hWnd) {
        TCHAR s[UTILS_BUFSIZ];
        GetClassName(hWnd, s, _countof(s));
        return s;
    }
#endif

    template<typename T>
    inline String to_wstring(T val) {
#ifdef _UNICODE
        return std::to_wstring(val);
#else
        return std::to_string(val);
#endif
    }


    bool ConvWstrToU8str(StringRef wstr, std::string& u8Str);

    /**
    * Convert a wide Unicode string to an UTF8 string
    */
    inline std::string utf8_encode(StringRef wstr)
    {
        std::string utf8str;
        if (!wstr.empty()) ConvWstrToU8str(wstr, utf8str);
        return utf8str;
    }

    /**
    * Convert a wide Unicode string to an UTF8 string
    */
    inline std::vector<uchar_t> utf8_byte_encode(StringRef wstr)
    {
        Vector<uchar_t> result;
        for (auto ch : utf8_encode(wstr)) {
            result.push_back((uchar_t)ch);
        }
        return result;
    }

    bool ConvU8strToWstr(const std::string& u8Str, String& wstr);

    /**
    * Convert an UTF8 string to a wide Unicode String
    */
    inline String utf8_decode(const std::string& str)
    {
        String wstr;
        if (!str.empty()) ConvU8strToWstr(str, wstr);
        return wstr;

    }

    // wchar_t to mbs (_buffer は最低4バイト分確保しておくこと)
    inline int wc_to_mbs(wchar_t wc, unsigned char* buffer)
    {
        size_t size;
        wchar_t ws[2]{ wc, 0 };
        setlocale(LC_CTYPE, "ja-JP");
        return wcstombs_s(&size, (char*)buffer, 4, ws, _TRUNCATE);
    }

    inline std::tuple<int, int> decompose_moji(wchar_t m) {
        unsigned char buf[4]{ 0, 0 };
        wc_to_mbs(m, buf);
        if (buf[1] == 0) {
            // 1バイト文字
            return { buf[1], buf[0] };
        } else {
            // 2バイト文字
            return { buf[0], buf[1] };
        }
    }

    inline std::string ws_to_mbs(StringRef ws)
    {
        size_t size;
        char buffer[UTILS_BUFSIZ];
        setlocale(LC_CTYPE, "ja-JP");
        errno_t error = wcstombs_s(&size, buffer, sizeof(buffer), ws.c_str(), _TRUNCATE);
        //debug_rptf2("error=%d size=%d\n", error, size);
        return (error == 0 && size > 0) ? buffer : "";
    }

    inline std::string wc_to_mbs(wchar_t wc)
    {
        size_t size;
        wchar_t ws[2]{ wc,0 };
        char buffer[8];
        setlocale(LC_CTYPE, "ja-JP");
        errno_t error = wcstombs_s(&size, buffer, sizeof(buffer), ws, _TRUNCATE);
        //debug_rptf2("error=%d size=%d\n", error, size);
        return (error == 0 && size > 0) ? buffer : "";
    }

    // 数値に変換
    // int に変換
    inline int strToInt(StringRef s, int defval = 0) {
        try {
            return std::stoi(s);
        }
        catch (...) {
            return defval;
        }
    }

    inline int strToInt(const MString& s, int defval = 0) {
        try {
            return std::stoi(to_wstr(s));
        }
        catch (...) {
            return defval;
        }
    }

    // hex int に変換
    inline int strToHex(StringRef s, int defval = 0) {
        try {
            return std::stoi(s, nullptr, 16);
        }
        catch (...) {
            return defval;
        }
    }

    inline float strToFloat(StringRef s, float defval = 0.0) {
        try {
            return std::stof(s, nullptr);
        }
        catch (...) {
            return defval;
        }
    }

    inline double strToDouble(StringRef s, double defval = 0.0) {
        try {
            return std::stod(s, nullptr);
        }
        catch (...) {
            return defval;
        }
    }

    // toupper
    inline String toUpper(StringRef s) {
        String result(s.size(), 0);
        for (size_t i = 0; i < s.size(); ++i) result[i] = wchar_t(langedge::CtypeUtil::toUpper(s[i]));
        return result;
    }

    inline String toUpperFromMS(const MString& s) {
        String result(s.size(), 0);
        for (size_t i = 0; i < s.size(); ++i) result[i] = wchar_t(langedge::CtypeUtil::toUpper(s[i]));
        return result;
    }

    inline MString toUpperMS(StringRef s) {
        MString result(s.size(), 0);
        for (size_t i = 0; i < s.size(); ++i) result[i] = mchar_t(langedge::CtypeUtil::toUpper(s[i]));
        return result;
    }

    // tolower
    inline String toLower(StringRef s) {
        String result(s.size(), 0);
        for (size_t i = 0; i < s.size(); ++i) result[i] = wchar_t(langedge::CtypeUtil::toLower(s[i]));
        return result;
    }

    // bool に変換
    inline bool strToBool(StringRef s) {
        return toLower(s) == L"true";
    }

    inline bool startsWith(StringRef s, StringRef t) {
        return s.size() >= t.size() && std::equal(std::begin(t), std::end(t), std::begin(s));
    }

    inline bool startsWith(const MString& s, const MString& t) {
        return s.size() >= t.size() && std::equal(std::begin(t), std::end(t), std::begin(s));
    }

    inline bool endsWith(StringRef s, StringRef t) {
        return s.size() >= t.size() && std::equal(std::rbegin(t), std::rend(t), std::rbegin(s));
    }

    inline bool endsWith(const MString& s, const MString& t) {
        return s.size() >= t.size() && std::equal(std::rbegin(t), std::rend(t), std::rbegin(s));
    }

    inline size_t get_hash(StringRef s) {
        return std::hash<String>()(s);
    }

    inline size_t get_hash(const MString& s) {
        return std::hash<MString>()(s);
    }

    inline String safe_substr(StringRef s, size_t start, size_t len = std::string::npos) {
        if (start >= s.size()) start = s.size();
        return s.substr(start, len);
    }

    inline MString safe_substr(const MString& s, size_t start, size_t len = std::string::npos) {
        if (start >= s.size()) start = s.size();
        return s.substr(start, len);
    }

    inline bool contains(StringRef s, const wchar_t* t) {
        return s.find(t) != String::npos;
    }

    inline bool contains(const MString& s, const mchar_t* t) {
        return s.find(t) != MString::npos;
    }

    inline MString replace(const MString& s, const MString& t, const MString& r) {
        size_t pos = s.find(t);
        return (pos == MString::npos) ? s : s.substr(0, pos) + r + s.substr(pos + t.size());
    }

    inline String replace(StringRef s, StringRef t, StringRef r) {
        size_t pos = s.find(t);
        return (pos == String::npos) ? s : s.substr(0, pos) + r + s.substr(pos + t.size());
    }

    inline MString replace_all(const MString& s, mchar_t t, mchar_t r) {
        MString result = s;
        std::replace(result.begin(), result.end(), t, r);
        return result;
    }

    inline String replace_all(StringRef s, wchar_t t, wchar_t r) {
        String result = s;
        std::replace(result.begin(), result.end(), t, r);
        return result;
    }

    inline String replace_all(StringRef s, StringRef t, StringRef r) {
        return std::regex_replace(s, std::wregex(t), r);
    }

    inline void remove(MString& s, mchar_t t) {
        size_t pos = s.find(t);
        if (pos < s.size()) s.erase(pos);
    }

    inline void remove(String& s, wchar_t t) {
        size_t pos = s.find(t);
        if (pos < s.size()) s.erase(pos);
    }

    // 文字列の末尾の n 文字からなら部分文字列を返す
    inline String last_substr(StringRef s, size_t n) {
        size_t len = s.size();
        return n <= len ? s.substr(len - n) : s;
    }

    inline MString last_substr(const MString& s, size_t n) {
        size_t len = s.size();
        return n <= len ? s.substr(len - n) : s;
    }

    inline String tail_substr(StringRef s, size_t n) {
        return last_substr(s, n);
    }

    inline MString tail_substr(const MString& s, size_t n) {
        return last_substr(s, n);
    }

    inline bool is_hirakana(mchar_t ch) {
        return ch >= 0x3041 && ch <= 0x3096;    // 'ぁ' 〜 '小け'
    }

    inline bool is_hiragana(mchar_t ch) {
        return is_hirakana(ch);
    }

    inline bool is_punct(mchar_t ch) {
        return ch == TOTEN || ch == KUTEN;
    }

    inline bool is_hiragana_or_punct(mchar_t ch) {
        return is_hiragana(ch) || is_punct(ch);
    }

    // 中黒・長音も含むひらがなか
    inline bool is_hiragana_or_etc(mchar_t ch) {
        return is_hirakana(ch) || ch == NAKAGURO || ch == CHOON;
    }

    inline mchar_t hiragana_to_katakana(mchar_t ch) {
        return ch + 0x0060;
    }

    inline mchar_t katakana_to_hiragana(mchar_t ch) {
        return ch - 0x0060;
    }

    inline bool is_katakana(mchar_t ch) {
        return ch >= 0x30a1 && ch <= 0x30fc;    // 'ァ' 〜 'ヺ'、'・'、'ー'
    }

    inline bool is_pure_katakana(mchar_t ch) {
        return ch >= 0x30a1 && ch <= 0x30f6;    // 'ァ' 〜 'ヶ'
    }

    inline bool is_hankaku_katakana(mchar_t ch) {
        return ch >= 0xff65 && ch <= 0xff9f;    // '・' 〜 'ﾟ'
    }

    inline bool is_kanji(mchar_t ch) {
        return ch >= 0x4e00 && ch <= 0x9fff || ch == 0x3005 /*々*/;
    }

    inline bool is_kanji_or_katakana(mchar_t ch) {
        return is_kanji(ch) || is_katakana(ch);
    }

    template<typename T>
    inline bool is_kanji_or_katakana_str(const T& s) {
        for (auto ch : s) {
            if (!is_kanji_or_katakana(ch)) return false;
        }
        return true;
    }

    inline bool is_japanese_char(mchar_t ch) {
        return is_hirakana(ch) || is_kanji(ch) || is_katakana(ch);
    }

    inline bool is_kanji_or_katakana_except_nakaguro(mchar_t ch) {
        return ch != NAKAGURO && (is_kanji_or_katakana(ch));
    }

    inline bool is_japanese_char_except_nakaguro(mchar_t ch) {
        return ch != NAKAGURO && (is_japanese_char(ch));
    }

    inline bool is_not_kanji_nor_katakana(mchar_t ch) {
        return !is_kanji_or_katakana(ch);
    }

    inline bool is_not_japanese_char(mchar_t ch) {
        return !is_japanese_char(ch);
    }

    inline String convert_star_and_question_to_hankaku(const wchar_t* wp) {
        String result;
        while (*wp != '\0') {
            if (*wp == 0xff0a)
                result.push_back('*');
            else if (*wp == 0xff1f)
                result.push_back('?');
            else
                result.push_back(*wp);
            ++wp;
        }
        return result;
    }

    inline MString convert_hiragana_to_katakana(const MString& mstr) {
        MString result;
        for (auto mc : mstr) {
            result.push_back(is_hiragana(mc) ? hiragana_to_katakana(mc) : mc);
        }
        return result;
    }

    inline String convert_hiragana_to_katakana(StringRef wstr) {
        String result;
        for (auto wc : wstr) {
            result.push_back((wchar_t)(is_hiragana(wc) ? hiragana_to_katakana(wc) : wc));
        }
        return result;
    }

    inline MString convert_katakana_to_hiragana(const MString& mstr) {
        MString result;
        for (auto mc : mstr) {
            result.push_back(is_pure_katakana(mc) ? katakana_to_hiragana(mc) : mc);
        }
        return result;
    }

    inline String convert_katakana_to_hiragana(StringRef wstr) {
        String result;
        for (auto wc : wstr) {
            result.push_back((wchar_t)(is_pure_katakana(wc) ? katakana_to_hiragana(wc) : wc));
        }
        return result;
    }

    // 先頭の漢字部分の長さを取得
    template<typename T>
    inline size_t count_head_kanji(const T& s) {
        size_t len = 0;
        while (len < s.size() && is_kanji(s[len])) {
            ++len;
        }
        return len;
    }

    // 末尾の漢字部分の長さを取得
    template<typename T>
    inline size_t count_tail_kanji(const T& s) {
        size_t pos = s.size();
        while (pos > 0 && is_kanji(s[pos - 1])) {
            --pos;
        }
        return s.size() - pos;
    }

    // 漢字を含むか
    template<typename T>
    inline bool contains_kanji(const T& s) {
        for (size_t pos = 0; pos < s.size(); ++pos) {
            if (is_kanji(s[pos])) return true;
        }
        return false;
    }

    // ASCII文字を含むか
    template<typename T>
    inline bool contains_ascii(const T& s) {
        for (size_t pos = 0; pos < s.size(); ++pos) {
            if (s[pos] >= 0 && s[pos] < 0x80) return true;
        }
        return false;
    }

    // 末尾の漢字またはカタカナ部分の長さを取得
    template<typename T>
    inline size_t count_tail_kanji_or_katakana(const T& s) {
        size_t pos = s.size();
        while (pos > 0 && (is_kanji(s[pos - 1]) || is_katakana(s[pos - 1]))) {
            --pos;
        }
        return s.size() - pos;
    }

    // 末尾のひらがな部分の長さを取得
    template<typename T>
    inline size_t count_tail_hiragana(const T& s) {
        size_t pos = s.size();
        while (pos > 0 && is_hiragana(s[pos - 1])) {
            --pos;
        }
        return s.size() - pos;
    }

    // 末尾のひらがな部分の長さを取得(句読点を含む)
    template<typename T>
    inline size_t count_tail_hiragana_including_punct(const T& s) {
        size_t pos = s.size();
        while (pos > 0 && is_hiragana_or_punct(s[pos - 1])) {
            --pos;
        }
        return s.size() - pos;
    }

    /// <summary> 末尾の平仮名連鎖を取得(中黒・長音を含む; bHeadSpace==trueなら先頭の空白も含む)</summary>
    template<typename T>
    inline T find_tail_hiragana_str(const T& s, bool bHeadSpace = false) {
        size_t pos = s.size();
        while (pos > 0 && is_hiragana_or_etc(s[pos - 1])) {
            --pos;
        }
        if (bHeadSpace && pos > 0 && s[pos - 1] == ' ') --pos;
        if (pos < s.size()) return s.substr(pos);
        return T();
    }

    /// <summary> 末尾のカタカナ連鎖を取得 (先頭の中黒は削除する)</summary>
    template<typename T>
    inline T find_tail_katakana_str(const T& s, bool excludeTailNakaguro = false) {
        if (!s.empty()) {
            size_t len = s.size();
            if (excludeTailNakaguro && len > 0 && s[len - 1] == NAKAGURO) --len;
            size_t i = len;
            for (; i > 0; --i) {
                if (!is_katakana(s[i - 1])) break;
            }
            if (i < len && s[i] == NAKAGURO) ++i;
            if (i < len) return s.substr(i, len - i);
        }
        return T();
    }

    /// <summary> 末尾の半角カタカナ連鎖を取得 (先頭の中黒は削除する)</summary>
    template<typename T>
    inline T find_tail_hankaku_katakana_str(const T& s) {
        if (!s.empty()) {
            size_t len = s.size();
            //if (len > 0 && s[len - 1] == HAN_NAKAGURO) --len;
            size_t i = len;
            for (; i > 0; --i) {
                if (!is_hankaku_katakana(s[i - 1])) break;
            }
            if (i < len && s[i] == HAN_NAKAGURO) ++i;
            if (i < len) return s.substr(i, len - i);
        }
        return T();
    }

    /// <summary> 末尾の漢字連鎖を取得 </summary>
    template<typename T>
    inline T find_tail_kanji_str(const T& s) {
        if (!s.empty()) {
            int i = s.size() - 1;
            for (; i >= 0; --i) {
                if (!is_kanji(s[i])) break;
            }
            if (i < (int)s.size() - 1) return s.substr(i + 1);
        }
        return T();
    }

    template<typename T>
    inline T find_tail_kanji_or_katakana_str(const T& s) {
        T r = find_tail_kanji_str(s);
        return !r.empty() ? r : find_tail_katakana_str(s);
    }

    template<typename T>
    inline T find_tail_japanese_str(const T& s) {
        if (!s.empty()) {
            size_t len = s.size();
            if (len > 0 && s[len - 1] == NAKAGURO) --len;
            size_t i = len;
            for (; i > 0; --i) {
                if (!is_japanese_char(wchar_t(s[i - 1]))) break;
            }
            if (i < len && s[i] == NAKAGURO) ++i;
            if (i < len) return s.substr(i, len - i);
        }
        return T();
    }

    template<typename T>
    inline T find_tail_ascii_str(const T& s) {
        if (!s.empty()) {
            size_t len = s.size();
            size_t i = len;
            for (; i > 0; --i) {
                auto ch = wchar_t(s[i - 1]);
                if (ch == 0x20 || !is_ascii_char(ch)) break;
            }
            if (i < len) return s.substr(i, len - i);
        }
        return T();
    }

    // 文字列を指定の長さに縮める
    inline String str_shrink(StringRef s, size_t len) {
        if (len >= s.size()) return s;
        size_t head = len / 2;
        size_t tail = len - head - 1;
        if ((head + tail) >= len) --tail;
        return s.substr(0, head) + L"…" + last_substr(s, tail);
    }

    inline MString str_shrink(const MString& s, size_t len) {
        if (len >= s.size()) return s;
        size_t head = len / 2;
        size_t tail = len - head - 1;
        if ((head + tail) >= len) --tail;
        return s.substr(0, head) + MSTR_THREE_DOTS + last_substr(s, tail);
    }

    /**
    * split
    */
    template<class T>
    inline bool __is_equals_to(T x, T d) { return x == d; }

    template<class T>
    inline bool __is_contained(T x, const T* d) {
        if (d == 0) return false;
        while (*d) {
            if (x == *d) return true;
            ++d;
        }
        return false;
    }

#define SPLIT(T, N, F) \
        std::vector<T> elems; \
        T item; \
        for (auto ch : s) { \
            if ((N == 0 || elems.size() + 1 < N) && F(ch, delim)) { \
                /*if (!item.empty() || !elems.empty()) { */ \
                elems.push_back(item); \
                /*}*/ \
                item.clear(); \
            } else { \
                item += ch; \
            } \
        } \
        /*if (!item.empty())*/ \
            elems.push_back(item); \
        return elems;

    // delim で分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    inline std::vector<std::string> split(const std::string& s, char delim) {
        SPLIT(std::string, 0, __is_equals_to);
    }

    // delim で分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    inline std::vector<std::string> split(const std::string& s, const char* delim) {
        SPLIT(std::string, 0, __is_contained);
    }

    // delim で分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    inline std::vector<String> split(StringRef s, wchar_t delim) {
        SPLIT(String, 0, __is_equals_to);
    }

    // delim に含まれる文字で分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    inline std::vector<String> split(StringRef s, const wchar_t* delim) {
        SPLIT(String, 0, __is_contained);
    }

    // delim で分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    inline std::vector<MString> split(const MString& s, mchar_t delim) {
        SPLIT(MString, 0, __is_equals_to)
    }

    // delim で n 個に分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    inline std::vector<String> split(StringRef s, size_t n, wchar_t delim) {
        SPLIT(String, n, __is_equals_to)
    }

    // delim に含まれる文字で n 個に分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    inline std::vector<String> split(StringRef s, size_t n, const wchar_t* delim) {
        SPLIT(String, n, __is_contained)
    }

    // delim で n 個に分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    inline std::vector<MString> split(const MString& s, size_t n, mchar_t delim) {
        SPLIT(MString, n, __is_equals_to)
    }
#undef SPLIT

    // 正規表現 delim で n 個に分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    inline std::vector<String> reSplit(StringRef s, size_t n, const std::wregex& reDelim) {
        std::vector<String> result;
        String ss = s;
        std::wsmatch m;
        if (n == 0) n = (size_t)INT_MAX;
        if (n > 1) {
            while (std::regex_search(ss, m, reDelim)) {
                result.push_back(m.prefix());
                ss = m.suffix();
                if (ss.empty() || result.size() + 1 == n) break;
            }
        }
        result.push_back(ss);
        return result;
    }

    // 正規表現 delim で n 個に分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    inline std::vector<String> reSplit(StringRef s, size_t n, StringRef delim) {
        return reSplit(s, n, std::wregex(delim));
    }

    // 正規表現 delim で分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    inline std::vector<String> reSplit(StringRef s, const std::wregex& reDelim) {
        return reSplit(s, 0, reDelim);
    }

    // 正規表現 delim で分割する。先頭が delim の場合、戻値の先頭要素は空文字列になる
    inline std::vector<String> reSplit(StringRef s, StringRef delim) {
        return reSplit(s, 0, std::wregex(delim));
    }

    // 正規表現 pで置換する
    inline String reReplace(StringRef s, const std::wregex& p, StringRef r) {
        return std::regex_replace(s, p, r);
    }

    // 正規表現 pで置換する
    inline String reReplace(StringRef s, StringRef p, StringRef r) {
        return std::regex_replace(s, std::wregex(p), r);
    }

    // 正規表現にマッチする要素を取り出す
    // 正規表現 p は s 全体にマッチする必要がある。
    // 最初のカッコにマッチするものが戻値の 0 番目の要素になる
    inline std::vector<String> reScan(StringRef s, const std::wregex& p) {
        std::vector<String> result;

        std::wsmatch m;
        if (std::regex_match(s, m, p) && m.size() > 1) {
            std::copy(m.begin() + 1, m.end(), std::back_inserter(result));
        }

        return result;
    }

    // 正規表現にマッチする要素を取り出す
    // 正規表現 p は s 全体にマッチする必要がある。
    // 最初のカッコにマッチするものが戻値の 0 番目の要素になる
    inline std::vector<String> reScan(StringRef s, StringRef p) {
        return reScan(s, std::wregex(p));
    }

    /**
    * join
    */
    inline String join(const std::vector<String>& list, wchar_t delim, size_t maxElem = 0)
    {
        String result;
        if (maxElem == 0) maxElem = list.size();
        size_t n = 0;
        for (auto& e : list) {
            if (n++ >= maxElem) break;
            if (!result.empty()) result.push_back(delim);
            result.append(e);
        }
        return result;
    }

    inline String join(const std::vector<String>& list, StringRef delim, size_t maxElem = 0)
    {
        String result;
        if (maxElem == 0) maxElem = list.size();
        size_t n = 0;
        for (auto& e : list) {
            if (n++ >= maxElem) break;
            if (!result.empty()) result.append(delim);
            result.append(e);
        }
        return result;
    }

    template<typename T>
    inline String join_primitive(const std::vector<T>& list, StringRef delim, size_t maxElem = 0)
    {
        String result;
        if (maxElem == 0) maxElem = list.size();
        size_t n = 0;
        for (auto& e : list) {
            if (n++ >= maxElem) break;
            if (!result.empty()) result.append(delim);
            result.append(std::to_wstring(e));
        }
        return result;
    }

    inline String join(const std::vector<int>& list, StringRef delim, size_t maxElem = 0)
    {
        return join_primitive(list, delim, maxElem);
    }

    inline MString join(const std::vector<MString>& list, mchar_t delim, size_t maxElem = 0)
    {
        MString result;
        if (maxElem == 0) maxElem = list.size();
        size_t n = 0;
        for (auto& e : list) {
            if (n++ >= maxElem) break;
            if (!result.empty()) result.push_back(delim);
            result.append(e);
        }
        return result;
    }

    inline String join(const std::set<String>& list, StringRef delim, size_t maxElem = 0)
    {
        String result;
        if (maxElem == 0) maxElem = list.size();
        size_t n = 0;
        for (auto& e : list) {
            if (n++ >= maxElem) break;
            if (!result.empty()) result.append(delim);
            result.append(e);
        }
        return result;
    }

    inline MString join(const std::set<MString>& list, mchar_t delim, size_t maxElem = 0)
    {
        MString result;
        if (maxElem == 0) maxElem = list.size();
        size_t n = 0;
        for (auto& e : list) {
            if (n++ >= maxElem) break;
            if (!result.empty()) result.push_back(delim);
            result.append(e);
        }
        return result;
    }

    /**
    * strip
    */
    inline String strip(StringRef s, StringRef delims = L" \r\n")
    {
        String result;

        auto left = s.find_first_not_of(delims);
        if (left != String::npos) {
            // 左側にデリミタ以外の文字が見つかった
            auto right = s.find_last_not_of(delims);
            result = s.substr(left, right - left + 1);
        }
        return result;
    }

    inline MString strip(const MString& s, StringRef delims)
    {
        MString result;

        auto msDelims = to_mstr(delims);
        auto left = s.find_first_not_of(msDelims);
        if (left != MString::npos) {
            // 左側にデリミタ以外の文字が見つかった
            auto right = s.find_last_not_of(msDelims);
            result = s.substr(left, right - left + 1);
        }
        return result;
    }

    inline MString strip(const MString& s)
    {
        MString result;

        auto left = s.find_first_not_of(strip_delims);
        if (left != MString::npos) {
            // 左側にデリミタ以外の文字が見つかった
            auto right = s.find_last_not_of(strip_delims);
            result = s.substr(left, right - left + 1);
        }
        return result;
    }

    inline TCHAR safe_front(StringRef s) {
        return s.empty() ? TCHAR('\0') : s[0];
    }

    inline mchar_t safe_front(const MString& s) {
        return s.empty() ? mchar_t('\0') : s[0];
    }

    inline TCHAR safe_back(StringRef s) {
        return s.empty() ? TCHAR('\0') : s[s.size() - 1];
    }

    inline mchar_t safe_back(const MString& s) {
        return s.empty() ? mchar_t('\0') : s[s.size() - 1];
    }
    inline TCHAR safe_back(StringRef s, size_t n) {
        return s.size() < n ? TCHAR('\0') : s[s.size() - n];
    }

    inline mchar_t safe_back(const MString& s, size_t n) {
        return s.size() < n ? mchar_t('\0') : s[s.size() - n];
    }

    inline String wildcard_to_regex(StringRef s) {
        String result;
        for (wchar_t mch : s) {
            if (mch == '?') {
                result.append(1, '.');
            }  else if (mch == '*') {
                result.append(1, '.');
                result.append(1, '*');
            } else {
                result.append(1, mch);
            }
        }
        return result;
    }

    inline bool match_key_containing_question(const MString& str, size_t pos, const MString& qKey, size_t vertBarGobiMaxLen = 2) {
        for (size_t i = 0; i < qKey.size(); ++i) {
            if (i >= str.size()) return false;
            if (str[i] == VERT_BAR) {
                return qKey.size() <= i + vertBarGobiMaxLen;
            }
            if (qKey[i] == '?') continue;
            if (qKey[i] != str[pos + i]) return false;
        }
        return true;
    }

    inline bool startsWithWildKey(const MString& str, const MString qKey, size_t vertBarGobiMaxLen) {
        return (vertBarGobiMaxLen > 0 || str.size() >= qKey.size()) && match_key_containing_question(str, 0, qKey, vertBarGobiMaxLen);
    }

    inline bool endsWithWildKey(const MString& str, const MString qKey) {
        return str.size() >= qKey.size() && match_key_containing_question(str, str.size() - qKey.size(), qKey, 0);
    }

    inline bool isRomanString(const MString& s) {
        return is_roman_str(s);
    }

    inline bool isRomanString(StringRef s) {
        return is_roman_str(s);
    }

    inline bool isAsciiString(const MString& s) {
        return is_ascii_str(s);
    }

    inline bool isAsciiString(StringRef s) {
        return is_ascii_str(s);
    }

    inline String boolToString(bool flag) {
        return flag ? L"True" : L"False";
    }

    inline String intToString(int val) {
        return std::to_wstring(val);
    }

    template<class T>
    inline int stringToInt(const T& s, int emptyVal = 0, int errVal = 0) {
        try {
            return s.empty() ? emptyVal : std::stoi(s);
        }
        catch (...) {
            return errVal;
        }
    }

    template<class T>
    inline int parseInt(const T& s, int errVal = 0) {
        return stringToInt(s, errVal, errVal);
    }

    inline String doubleToString(double val) {
        return std::to_wstring(val);
    }

    // 可変個の引数を stringstream で連結する
    template<class T>
    class any_concatenator {
        std::basic_stringstream<T> ss;


        std::basic_string<T> concatenateAny() {
            return ss.str();
        }

    public:
        template<class Head, class... Tail>
        std::basic_string<T> concatenateAny(Head&& head, Tail&&... tail) {
            ss << head;
            return concatenateAny(std::forward<Tail>(tail)...);
        }
    };

    template<class... Args>
    inline std::string concatAny(Args... args) {
        return any_concatenator<char>().concatenateAny(args...);
    }

    template<class... Args>
    inline String wconcatAny(Args... args) {
        return any_concatenator<wchar_t>().concatenateAny(args...);
    }

#undef UTILS_BUFSIZ

    struct StringUtil {
        static inline bool notEmpty(StringRef s) {
            return !s.empty();
        }

        static inline int toInt(StringRef s, int emptyVal = 0, int errVal = 0) {
            return utils::stringToInt(s, emptyVal, errVal);
        }

        static inline int parseInt(StringRef s, int errVal = 0) {
            return utils::parseInt(s, errVal);
        }

        static inline float parseFloat(StringRef s, float errVal = 0.0) {
            return utils::strToFloat(s, errVal);
        }

        static inline double parseDouble(StringRef s, double errVal = 0.0) {
            return utils::strToDouble(s, errVal);
        }

        static inline String trim(StringRef s) {
            return utils::strip(s);
        }

        static inline String strip(StringRef s) {
            return utils::strip(s);
        }

        static inline String trim(StringRef s, StringRef delim) {
            return utils::strip(s, delim);
        }

        static inline String strip(StringRef s, StringRef delim) {
            return utils::strip(s, delim);
        }

        template<class... Args>
        static inline String catAny(Args... args) {
            return utils::wconcatAny(args...);
        }

        template<class T>
        static inline String toStr(T v) {
            return std::to_wstring(v);
        }

    };

    namespace VectorStringUtil {
        static inline String join(const Vector<String>& vs, wchar_t delim, size_t maxElem = 0) {
            return utils::join(vs, delim, maxElem);
        }

        static inline String join(const Vector<String>& vs, StringRef delim, size_t maxElem = 0) {
            return utils::join(vs, delim, maxElem);
        }
    };

#if 0
    // String の拡張メソッドクラス
    template<class S>
    struct StringExtension_ {
        S s;

        StringExtension_(StringRef s) : s(s) { }

        // コピーコンストラクタは禁止(dangling reference を防止するため、左辺値を作らせない)
        StringExtension_(const StringExtension_&) = delete;

        StringRef str() {
            //std::cout << "str() called" << std::endl;
            return s;
        }

        operator String() {
            //std::cout << "cast operator called" << std::endl;
            return s;
        }

        bool notEmpty() const {
            return !s.empty();
        }

        int toInt(int emptyVal = 0, int errVal = 0) {
            return utils::stringToInt(s, emptyVal, errVal);
        }

        int parseInt(int errVal = 0) const {
            return utils::parseInt(s, errVal);
        }

        StringExtension_<String> trim() const {
            return utils::strip(s);
        }

        String _trim() const {
            return utils::strip(s);
        }

        StringExtension_<String> strip() const {
            return utils::strip(s);
        }

        String _strip() const {
            return utils::strip(s);
        }

        StringExtension_<String> trim(StringRef delim) const {
            return utils::strip(s, delim);
        }

        String _trim(StringRef delim) const {
            return utils::strip(s, delim);
        }

        StringExtension_<String> strip(StringRef delim) const {
            return utils::strip(s, delim);
        }

        String _strip(StringRef delim) const {
            return utils::strip(s, delim);
        }

        template<class... Args>
        String cat(Args... args) {
            return utils::wconcatAny(args...);
        }

        // static methods
        template<class T>
        static String toStr(T v) {
            return std::to_string(v);
        }

    };

    // 先頭の StringExtension は、操作対象の文字列のコピーを防ぐため、const参照を保持するようにする
    // const参照でバインドするので、操作対象が一時オブジェクトであっても、一連の操作が完了するまでその寿命が延長される
    using StringExtension = StringExtension_<StringRef>;

    // Vector<String> の拡張メソッドクラス
    struct VectorStringExtension {
        const Vector<String>& sv;

        String join(StringRef delim, size_t maxElem = 0) {
            return utils::join(sv, delim, maxElem);
        }
    };
#endif

} // namespace utils


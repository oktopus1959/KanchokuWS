#pragma once

#include "string_type.h"

namespace {
    wchar_t TOTEN = 0x3001;     // 、

    wchar_t KUTEN = 0x3002;     // 。

    wchar_t NAKAGURO = 0x30fb;  // '・'

    wchar_t HAN_NAKAGURO = 0xff65;  // '・'

    wchar_t CHOON = 0x30fc;     // 'ー'

    wchar_t HAN_CHOON = 0xff70; // 'ー'

    wchar_t QUESTION_MARK = 0xff1f;   // '？'

    inline MString to_mstr(mchar_t x) {
        return x != 0 ? MString(1, x) : MString();
    }

    MString EMPTY_MSTR;

    MString MSTR_SPACE = to_mstr(' ');

    MString MSTR_THREE_DOTS = to_mstr(_T("…")[0]);

    mchar_t strip_delims[] = {' ', '\r', '\n' };

    inline bool is_paired_mchar(mchar_t m) {
        return (m & 0xffff0000) != 0;
    }

    inline bool is_ascii_char(wchar_t ch) {
        return ch >= 0x20 && ch <= 0x7f;
    }

    inline bool is_alphabet(mchar_t ch) {
        return (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
    }

    inline bool is_numeral(wchar_t ch) {
        return ch >= '0' && ch <= '9';
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

    inline bool is_ascii_pair(const wstring& ws) {
        return ws.size() >= 2 && is_ascii_pair(ws[0], ws[1]);
    }

    inline bool is_ascii_pair(const MString& ws) {
        return ws.size() >= 2 && is_ascii_pair(wchar_t(ws[0]), wchar_t(ws[1]));
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
        return MojiPair{ m >> 16, m & 0xffff };
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

    mchar_t make_mchar(const wstring& ws) {
        return ws.empty() ? 0 : ws.size() == 1 || !is_surrogate_pair(ws[0], ws[1]) ? ws[0] : make_mchar(ws[0], ws[1]);
    }

    mchar_t make_mchar(const wstring& ws, size_t* ppos) {
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

    mchar_t make_mchar_with_ascii(const wstring& ws) {
        return ws.empty() ? 0 : ws.size() == 1 || !is_surrogate_pair(ws[0], ws[1]) || !is_ascii_pair(ws[0], ws[1]) ? ws[0] : make_mchar(ws[0], ws[1]);
    }

    MString to_mstr(const wstring& ws) {
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

    void push_back_wstr(mchar_t m, wstring& ws) {
        auto mp = decomp_mchar(m);
        if (mp.first != 0) ws.push_back(mp.first);
        if (mp.second != 0) ws.push_back(mp.second);
    }

    wstring to_wstr(mchar_t m) {
        wstring result;
        push_back_wstr(m, result);
        return result;
    }

    wstring to_wstr(const MString& mstr) {
        wstring result;
        for (auto m : mstr) {
            push_back_wstr(m, result);
        }
        return result;
    }

    wstring to_wstr(const std::vector<mchar_t>& mstr) {
        wstring result;
        for (auto m : mstr) {
            push_back_wstr(m, result);
        }
        return result;
    }

    wstring to_wstr(const std::vector<mchar_t>& mstr, size_t begin, size_t len) {
        wstring result;
        size_t end = begin + len;
        if (end > mstr.size()) end = mstr.size();
        for (size_t i = begin; i < end; ++i) {
            push_back_wstr(mstr[i], result);
        }
        return result;
    }

    // cpyLen で指定された長さの文字列を wp 配列に追加する。末尾には 0 が付加される。wp は十分な長さを確保しておくこと。コピーした長さを返す。
    size_t append_wstr(const wstring& ws, wchar_t* wp, size_t cpyLen) {
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

    wstring make_wstring(wchar_t a, wchar_t b) {
        wstring result(2, 0);
        result[0] = a;
        result[1] = b;
        return result;
    }

    MString make_mstring(mchar_t a, mchar_t b) {
        MString result(2, 0);
        result[0] = a;
        result[1] = b;
        return result;
    }
} // namespace

namespace utils
{
    // hWnd で指定されたウインドウのクラス名を取得する
    inline tstring getClassNameFromHwnd(HWND hWnd) {
        TCHAR s[2048];
        GetClassName(hWnd, s, _countof(s));
        return s;
    }

    template<typename T>
    inline tstring to_tstring(T val) {
#ifdef _UNICODE
        return std::to_wstring(val);
#else
        return std::to_string(val);
#endif
    }

    inline tstring vformat(const TCHAR* fmt, va_list list) {
#ifdef _UNICODE
        wchar_t buf[2048];
        wvsprintf(buf, fmt, list);
#else
        char buf[2048];
        vsprintf_s(buf, fmt, list);
#endif
        return buf;
    }

    inline tstring format(const TCHAR* fmt, ...) {
        va_list ap;
        va_start(ap, fmt);
        tstring msg = vformat(fmt, ap);
        va_end(ap);
        return msg;
    }

    // 参照型を引数にとる va_list は許可されないので、引数をそのまま返す
    inline tstring format(const tstring& fmt) {
        return fmt;
    }

    inline std::string formatA(const char* fmt, ...) {
        va_list ap;
        va_start(ap, fmt);

        char buf[2048];
        vsprintf_s(buf, fmt, ap);

        va_end(ap);

        return buf;
    }

    /**
    * Convert a wide Unicode string to an UTF8 string
    */
    inline std::string utf8_encode(const std::wstring& wstr)
    {
        if (wstr.empty()) return std::string();
        int size_needed = WideCharToMultiByte(CP_UTF8, 0, &wstr[0], (int)wstr.size(), NULL, 0, NULL, NULL);
        std::string strTo((size_t)size_needed, 0);
        WideCharToMultiByte(CP_UTF8, 0, &wstr[0], (int)wstr.size(), &strTo[0], size_needed, NULL, NULL);
        return strTo;
    }

    /**
    * Convert an UTF8 string to a wide Unicode String
    */
    inline std::wstring utf8_decode(const std::string& str)
    {
        if (str.empty()) return std::wstring();
        int size_needed = MultiByteToWideChar(CP_UTF8, 0, &str[0], (int)str.size(), NULL, 0);
        std::wstring wstrTo((size_t)size_needed, 0);
        MultiByteToWideChar(CP_UTF8, 0, &str[0], (int)str.size(), &wstrTo[0], size_needed);
        return wstrTo;
    }

    // wchar_t to mbs (buffer は最低4バイト分確保しておくこと)
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

    inline std::string ws_to_mbs(const wstring& ws)
    {
        size_t size;
        char buffer[2048];
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
    inline int strToInt(const tstring& s, int defval = 0) {
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
    inline int strToHex(const tstring& s, int defval = 0) {
        try {
            return std::stoi(s, nullptr, 16);
        }
        catch (...) {
            return defval;
        }
    }

    // toupper
    inline tstring toUpper(const tstring& s) {
        tstring result(s.size(), 0);
        for (size_t i = 0; i < s.size(); ++i) result[i] = wchar_t(langedge::CtypeUtil::toUpper(s[i]));
        return result;
    }

    // tolower
    inline tstring toLower(const tstring& s) {
        tstring result(s.size(), 0);
        for (size_t i = 0; i < s.size(); ++i) result[i] = wchar_t(langedge::CtypeUtil::toLower(s[i]));
        return result;
    }

    // bool に変換
    inline bool strToBool(const tstring& s) {
        return toLower(s) == _T("true");
    }

    inline bool startsWith(const tstring& s, const tstring& t) {
        return s.size() >= t.size() && std::equal(std::begin(t), std::end(t), std::begin(s));
    }

    inline bool startsWith(const MString& s, const MString& t) {
        return s.size() >= t.size() && std::equal(std::begin(t), std::end(t), std::begin(s));
    }

    inline bool endsWith(const tstring& s, const tstring& t) {
        return s.size() >= t.size() && std::equal(std::rbegin(t), std::rend(t), std::rbegin(s));
    }

    inline bool endsWith(const MString& s, const MString& t) {
        return s.size() >= t.size() && std::equal(std::rbegin(t), std::rend(t), std::rbegin(s));
    }

    inline size_t get_hash(const tstring& s) {
        return std::hash<tstring>()(s);
    }

    inline size_t get_hash(const MString& s) {
        return std::hash<MString>()(s);
    }

    inline tstring safe_substr(const tstring& s, size_t start, size_t len = std::string::npos) {
        if (start >= s.size()) start = s.size();
        return s.substr(start, len);
    }

    inline MString safe_substr(const MString& s, size_t start, size_t len = std::string::npos) {
        if (start >= s.size()) start = s.size();
        return s.substr(start, len);
    }

    inline bool contains(const tstring& s, const wchar_t* t) {
        return s.find(t) != tstring::npos;
    }

    inline bool contains(const MString& s, const mchar_t* t) {
        return s.find(t) != MString::npos;
    }

    inline tstring replace(const tstring& s, const wchar_t* t, const wchar_t* r) {
        size_t pos = s.find(t);
        return (pos == tstring::npos) ? s : s.substr(0, pos) + r + s.substr(pos + wcslen(t));
    }

    // 文字列の末尾の n 文字からなら部分文字列を返す
    inline tstring last_substr(const tstring& s, size_t n) {
        size_t len = s.size();
        return n <= len ? s.substr(len - n) : s;
    }

    inline MString last_substr(const MString& s, size_t n) {
        size_t len = s.size();
        return n <= len ? s.substr(len - n) : s;
    }

    inline tstring tail_substr(const tstring& s, size_t n) {
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

    inline wstring convert_star_and_question_to_hankaku(const wchar_t* wp) {
        wstring result;
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

    inline wstring convert_hiragana_to_katakana(const wstring& wstr) {
        wstring result;
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

    inline wstring convert_katakana_to_hiragana(const wstring& wstr) {
        wstring result;
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
    inline tstring str_shrink(const tstring& s, size_t len) {
        if (len >= s.size()) return s;
        size_t head = len / 2;
        size_t tail = len - head - 1;
        if ((head + tail) >= len) --tail;
        return s.substr(0, head) + _T("…") + last_substr(s, tail);
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
    inline std::vector<wstring> split(const wstring& s, TCHAR delim) {
        std::vector<wstring> elems;
        wstring item;
        for (auto ch : s) {
            if (ch == delim) {
                if (!item.empty())
                    elems.push_back(item);
                item.clear();
            } else {
                item += ch;
            }
        }
        if (!item.empty())
            elems.push_back(item);
        return elems;
    }

    // delim で分割する。先頭が delim の場合、そのdelimは削除してから分割する
    inline std::vector<MString> split(const MString& s, mchar_t delim) {
        std::vector<MString> elems;
        MString item;
        for (auto ch : s) {
            if (ch == delim) {
                if (!item.empty())
                    elems.push_back(item);
                item.clear();
            } else {
                item += ch;
            }
        }
        if (!item.empty())
            elems.push_back(item);
        return elems;
    }

    /**
    * join
    */
    inline wstring join(const std::vector<wstring>& list, const wstring& delim, size_t maxElem = 0)
    {
        wstring result;
        if (maxElem == 0) maxElem = list.size();
        size_t n = 0;
        for (auto& e : list) {
            if (n++ >= maxElem) break;
            if (!result.empty()) result.append(delim);
            result.append(e);
        }
        return result;
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

    inline wstring join(const std::set<wstring>& list, const wstring& delim, size_t maxElem = 0)
    {
        wstring result;
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
    inline wstring strip(const wstring& s, const wstring& delims = _T(" \r\n"))
    {
        wstring result;

        auto left = s.find_first_not_of(delims);
        if (left != wstring::npos) {
            // 左側にデリミタ以外の文字が見つかった
            auto right = s.find_last_not_of(delims);
            result = s.substr(left, right - left + 1);
        }
        return result;
    }

    inline MString strip(const MString& s, const wstring& delims)
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

    inline TCHAR safe_front(const tstring& s) {
        return s.empty() ? TCHAR('\0') : s[0];
    }

    inline mchar_t safe_front(const MString& s) {
        return s.empty() ? mchar_t('\0') : s[0];
    }

    inline TCHAR safe_back(const tstring& s) {
        return s.empty() ? TCHAR('\0') : s[s.size() - 1];
    }

    inline mchar_t safe_back(const MString& s) {
        return s.empty() ? mchar_t('\0') : s[s.size() - 1];
    }
    inline TCHAR safe_back(const tstring& s, size_t n) {
        return s.size() < n ? TCHAR('\0') : s[s.size() - n];
    }

    inline mchar_t safe_back(const MString& s, size_t n) {
        return s.size() < n ? mchar_t('\0') : s[s.size() - n];
    }

    inline wstring wildcard_to_regex(const wstring& s) {
        wstring result;
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

    inline bool match_key_containing_question(const MString& str, size_t pos, const MString& qKey) {
        for (size_t i = 0; i < qKey.size(); ++i) {
            if (i >= str.size()) return false;
            if (qKey[i] == '?') continue;
            if (qKey[i] != str[pos + i]) return false;
        }
        return true;
    }

    inline bool startsWithWildKey(const MString& str, const MString qKey) {
        return str.size() >= qKey.size() && match_key_containing_question(str, 0, qKey);
    }

    inline bool endsWithWildKey(const MString& str, const MString qKey) {
        return str.size() >= qKey.size() && match_key_containing_question(str, str.size() - qKey.size(), qKey);
    }

} // namespace utils

#define MAKE_WPTR(ms) to_wstr(ms).c_str()

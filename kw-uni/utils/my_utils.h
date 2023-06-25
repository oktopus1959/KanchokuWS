#pragma once

#include "std_utils.h"
#include "string_utils.h"

namespace util {
    uint64_t fingerprint(const Vector<UCHAR>& data);

    inline uint64_t fingerprint(StringRef str) {
        return fingerprint(utils::utf8_byte_encode(str));
    }

    inline double logsumexp(double x, double y, bool flg) {
        double MINUS_LOG_EPSILON = 50.0;

        if (flg) return y;  // init mode
        double vmin = std::min(x, y);
        double vmax = std::max(x, y);
        if (vmax > vmin + MINUS_LOG_EPSILON) {
            return vmax;
        } else {
            return vmax + std::log(std::exp(vmin - vmax) + 1.0);
        }
    }

    // Print Progress Bar
    void progress_bar(StringRef message, size_t current, size_t total);

    /**
      * toCost
      */
    inline int toCost(double d, int n) {
        return (int)(std::max(std::min(-n * d, (double)INT_MAX), (double)INT_MIN));
    }

    inline Vector<String> tokenizeN(StringRef s, wchar_t delim, size_t n) {
        return utils::split(s, n, delim);
    }

    // delim に含まれる文字または delim を正規表現としてマッチさせてトークナイズする
    inline Vector<String> tokenizeN(StringRef s, const wchar_t* delim, size_t n) {
        if (delim == nullptr || delim[0] == '\0') return Vector<String>();
        if (delim[0] == '[') return utils::reSplit(s, n, delim);
        return utils::reSplit(s, n, String(L"[") + delim + L"]");
    }

    inline wchar_t getEscapedChar(wchar_t p) {
        switch (p) {
        case '0': return '\0';
        case 'a': return 1; // '\a'
        case 'b': return '\b';
        case 't': return '\t';
        case 'n': return '\n';
        case 'v': return 22; // '\v'
        case 'f': return '\f';
        case 'r': return '\r';
        case 's': return ' ';
        case '\\': return '\\';
        default: return '\0'; // never be here
        }
    }

    inline char getEscapedChar(char p) {
        return (char)getEscapedChar((wchar_t)p);
    }

    /**
     * enum csv files in the specified directory
     */
    Vector<String> enum_csv_dictionaries(StringRef dir);

} // namespace util

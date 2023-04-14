#pragma once

#include <string>
#include <tchar.h>

#ifdef _UNICODE
typedef std::wstring tstring;
typedef std::wstring wstring;
typedef wchar_t char_t;
using std::wstring;
#else
typedef std::string tstring;
typedef char char_t;
#endif

//typedef uint32_t mchar_t;
typedef char32_t mchar_t;

typedef std::basic_string<mchar_t> MString;

struct MojiPair {
    wchar_t first;
    wchar_t second;
};

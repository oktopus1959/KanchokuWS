#pragma once

#include <string>

#ifndef _T
#define _T(x) L ## x
#endif

using uchar_t = unsigned char;

//using mchar_t = uint32_t;
using mchar_t = char32_t;

using MString = std::basic_string<mchar_t>;

struct MojiPair {
    wchar_t first;
    wchar_t second;
};

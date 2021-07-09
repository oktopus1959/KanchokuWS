/** 
 * @file  tstring.hpp
 * @brief _MBCS/_UNICODE で std::string と std::wstring を切り替える
 * This file is in the PUBLIC DOMAIN.
 */

#ifndef LANGEDGE_STL_SUPPORT_TSTRING_H
#define LANGEDGE_STL_SUPPORT_TSTRING_H

#include <string>
#include <string.h>
#ifdef _WIN32
#include <tchar.h>
#else
#ifdef _UNICODE
typedef unsigned short TCHAR;
#define _tcslen wcslen
#define _sntprintf snwprintf
#define _vsntprintf vsnwprintf
#else
typedef char TCHAR;
#define _tcslen strlen
#define _sntprintf snprintf
#define _vsntprintf vsnprintf
#endif
#endif

#ifdef _UNICODE
typedef std::wstring _tstring;
#else
typedef std::string _tstring;
#endif

#endif //LANGEDGE_STL_SUPPORT_STRING_SUPPORT_HPP

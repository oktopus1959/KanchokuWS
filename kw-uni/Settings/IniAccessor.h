#pragma once

#include "string_utils.h"
#include "file_utils.h"

class IniAccessor {
private:
    String IniFilePath;

    String Section;

public:
    inline IniAccessor() {}

    inline IniAccessor(StringRef path, StringRef section)
        : IniFilePath(path), Section(section)
    {
    }

    StringRef GetIniFilePath() const { return IniFilePath; }

    inline std::vector<String> getSectionNames(int& err) const {
        std::vector<String> result;
        err = 0;
        TCHAR buf[1024];
        if (GetPrivateProfileSectionNames(buf, _countof(buf), IniFilePath.c_str()) >= sizeof(buf) - 2) {
            err = -1;
        } else {
            for (TCHAR* p = buf; *p != '\0' && p < buf + _countof(buf); p += lstrlen(p) + 1) {
                result.push_back(p);
            }
        }
        return result;
    }

    inline String getAttributeStringBySection(StringRef section, StringRef attr, StringRef defVal = _T("")) const {
        TCHAR value[1024];
        GetPrivateProfileString(section.c_str(), attr.c_str(), defVal.c_str(), value, _countof(value), IniFilePath.c_str());
        return value;
    }

    inline String getAttributeString(StringRef attr, StringRef defVal = _T("")) const {
        return getAttributeStringBySection(Section, attr, defVal);
    }

    inline int getAttributeInt(StringRef attr, int defVal, int base = 10) const {
        return std::stoi(getAttributeString(attr, utils::to_wstring(defVal)), 0, base);
    }

    inline int getAttributeHex(StringRef attr, StringRef defVal = _T("0")) const {
        return std::stoi(getAttributeString(attr, defVal), 0, 16);
    }

    inline void writeAttributeInt(StringRef attr, int val) const {
        WritePrivateProfileString(Section.c_str(), attr.c_str(), utils::to_wstring(val).c_str(), IniFilePath.c_str());
    }

};


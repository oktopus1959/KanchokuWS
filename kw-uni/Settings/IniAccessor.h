#pragma once

#include "string_utils.h"
#include "file_utils.h"

class IniAccessor {
private:
    tstring IniFilePath;

    tstring Section;

public:
    inline IniAccessor() {}

    inline IniAccessor(const tstring& path, const tstring& section)
        : IniFilePath(path), Section(section)
    {
    }

    const tstring& GetIniFilePath() const { return IniFilePath; }

    inline std::vector<tstring> getSectionNames(int& err) const {
        std::vector<tstring> result;
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

    inline tstring getAttributeStringBySection(const tstring& section, const tstring& attr, const tstring& defVal = _T("")) const {
        TCHAR value[1024];
        GetPrivateProfileString(section.c_str(), attr.c_str(), defVal.c_str(), value, _countof(value), IniFilePath.c_str());
        return value;
    }

    inline tstring getAttributeString(const tstring& attr, const tstring& defVal = _T("")) const {
        return getAttributeStringBySection(Section, attr, defVal);
    }

    inline int getAttributeInt(const tstring& attr, int defVal, int base = 10) const {
        return std::stoi(getAttributeString(attr, utils::to_tstring(defVal)), 0, base);
    }

    inline int getAttributeHex(const tstring& attr, const tstring& defVal = _T("0")) const {
        return std::stoi(getAttributeString(attr, defVal), 0, 16);
    }

    inline void writeAttributeInt(const tstring& attr, int val) const {
        WritePrivateProfileString(Section.c_str(), attr.c_str(), utils::to_tstring(val).c_str(), IniFilePath.c_str());
    }

};


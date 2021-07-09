#pragma once
// kanchoku.ini のアクセッサクラス

#include "path_utils.h"
#include "IniAccessor.h"

class KanchokuIni : public IniAccessor {
public:
    KanchokuIni() { }

    KanchokuIni(const wstring& iniFile) : IniAccessor(iniFile, _T("kanchoku")) { }

    std::wstring ParentDir() const { return utils::getParentDirPath(GetIniFilePath()); }

    static std::unique_ptr<KanchokuIni> Singleton;
};

#define KANCHOKU_INI  (KanchokuIni::Singleton)

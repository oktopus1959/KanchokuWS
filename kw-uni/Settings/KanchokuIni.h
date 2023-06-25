#pragma once
// kanchoku.ini のアクセッサクラス

#include "path_utils.h"
#include "IniAccessor.h"

class KanchokuIni : public IniAccessor {
public:
    KanchokuIni() { }

    KanchokuIni(StringRef iniFile) : IniAccessor(iniFile, _T("kanchoku")) { }

    String ParentDir() const { return utils::getParentDirPath(GetIniFilePath()); }

    static std::unique_ptr<KanchokuIni> Singleton;
};

#define KANCHOKU_INI  (KanchokuIni::Singleton)

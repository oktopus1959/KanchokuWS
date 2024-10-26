#include "Logger.h"
#include "DymazinBridge.h"
#include "DyMazinLib.h"

#if 1
#undef _LOG_INFOH
#undef LOG_DEBUGH
#if 0
#define _LOG_INFOH LOG_INFOH
#define LOG_DEBUGH LOG_INFOH
#else
#define _LOG_INFOH LOG_WARN
#define LOG_DEBUGH LOG_INFOH
#endif
#endif

namespace DymazinBridge {
    DEFINE_LOCAL_LOGGER(DymazinBridge);

    int dymazinInitialize(StringRef rcfile, StringRef dicdir, int unkMax) {
        LOG_INFOH(_T("ENTER: rcfile={}, dicdir={}"), rcfile, dicdir);

        std::vector<const wchar_t*> av;
        av.push_back(L"-r");
        av.push_back(rcfile.c_str());
        av.push_back(L"-d");
        av.push_back(dicdir.c_str());
        String maxGroupSize(L"--max-grouping-size=");
        maxGroupSize.append(std::to_wstring(unkMax));
        av.push_back(maxGroupSize.c_str());
        av.push_back(L"-Owakati");
        int result = DymazinInitialize(av.size(), av.data(), L"dymazin.log");

        LOG_INFOH(_T("LEAVE: result={}"), result);
        return result;
    }

    void dymazinFinalize() {
        LOG_INFOH(_T("CALLED"));
        DymazinFinalize();
    }

#if 1
    int dymazinCalcCost(const MString& str, std::vector<MString>& words) {
        LOG_DEBUGH(_T("ENTER: str={}"), to_wstr(str));
        const size_t BUFSIZE = 1000;
        wchar_t wchbuf[BUFSIZE] = { '\0' };
        int cost = DymazinAnalyze(to_wstr(str).c_str(), wchbuf, BUFSIZE, false);
        for (const auto& s : utils::split(wchbuf, L' ')) {
            words.push_back(to_mstr(s));
        }
        LOG_DEBUGH(_T("LEAVE: dymazinCost={}, words={}"), cost, to_wstr(utils::join(words, ' ')));
        return cost;
    }
#else
    int dymazinCalcCost(const MString& str, std::vector<MString>& words) {
        return 10000;
    }
#endif
}

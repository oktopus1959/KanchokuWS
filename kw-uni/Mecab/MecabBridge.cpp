#include "Logger.h"
#include "mecab.h"
#include "MecabBridge.h"

#if 1
#undef LOG_INFOH
#undef LOG_DEBUGH
#if 0
#define LOG_INFOH LOG_INFOH
#define LOG_DEBUGH LOG_INFOH
#else
#define LOG_INFOH LOG_WARN
#define LOG_DEBUGH LOG_WARN
#endif
#endif

namespace MecabBridge {
    DEFINE_LOCAL_LOGGER(MecabBridge);

    int mecabInitialize(StringRef rcfile, StringRef dicdir, int unkMax) {
        LOG_INFOH(_T("ENTER: rcfile={}, dicdir={}"), rcfile, dicdir);
        int result = mecab_init(utils::utf8_encode(rcfile).c_str(), utils::utf8_encode(dicdir).c_str(), std::to_string(unkMax).c_str());
        LOG_INFOH(_T("LEAVE: result={}, errorStr={}"), result, result == 0 ? _T("none") : utils::utf8_decode(mecab_init_strerror()));
        return result;
    }

    void mecabFinalize() {
        LOG_INFOH(_T("CALLED"));
        mecab_end();
    }

    int mecabCalcCost(const MString& str, std::vector<MString>& words) {
        LOG_DEBUGH(_T("ENTER: str={}"), to_wstr(str));
        char utf8buf[1000] = { '\0' };
        int cost = mecab_do_cost(utils::utf8_encode(to_wstr(str)).c_str(), utf8buf, sizeof(utf8buf)); // ここでは逆順に単語が返る
        for (const auto& s : utils::split(utf8buf, '\t')) {
            words.insert(words.begin(), to_mstr(utils::utf8_decode(s)));
        }
        LOG_DEBUGH(_T("LEAVE: mecabCost={}, words={}"), cost, to_wstr(utils::join(words, ' ')));
        return cost;
    }
}

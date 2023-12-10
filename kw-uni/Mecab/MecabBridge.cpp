#include "Logger.h"
#include "mecab.h"
#include "MecabBridge.h"

namespace MecabBridge {
    DEFINE_LOCAL_LOGGER(MecabBridge);

    int mecabInitialize(StringRef rcfile, StringRef dicdir) {
        LOG_INFOH(_T("ENTER: rcfile={}, dicdir={}"), rcfile, dicdir);
        int result = mecab_init(utils::utf8_encode(rcfile).c_str(), utils::utf8_encode(dicdir).c_str());
        LOG_INFOH(_T("LEAVE: result={}, errorStr={}"), result, result == 0 ? _T("none") : utils::utf8_decode(mecab_init_strerror()));
        return result;
    }

    void mecabFinalize() {
        LOG_INFOH(_T("CALLED"));
        mecab_end();
    }

    int mecabCalcCost(const MString& str) {
        LOG_INFOH(_T("ENTER: str={}"), to_wstr(str));
        int cost = mecab_do_cost(utils::utf8_encode(to_wstr(str)).c_str());
        LOG_INFOH(_T("LEAVE: cost={}"), cost);
        return cost;
    }
}

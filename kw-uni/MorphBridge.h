#pragma once

#include "Reporting/Logger.h"
#include "Mecab/MecabBridge.h"
#include "DyMazin/DymazinBridge.h"

#define USE_DYMAZIN 1

namespace MorphBridge {
    inline void morphInitialize(Reporting::Logger& logger) {
        int unkMax = 3;
#if USE_DYMAZIN
        auto rcfile = utils::joinPath(SETTINGS->rootDir, _T("dymazin/etc/morphrc"));
        auto dicdir = utils::joinPath(SETTINGS->rootDir, _T("dymazin/dic/ipadic"));
        int ret = DymazinBridge::dymazinInitialize(rcfile, dicdir, unkMax);
#else
        auto rcfile = utils::joinPath(SETTINGS->rootDir, _T("mecab/etc/mecabrc"));
        auto dicdir = utils::joinPath(SETTINGS->rootDir, _T("mecab/dic/ipadic"));
        int ret = MecabBridge::mecabInitialize(rcfile, dicdir, unkMax);
#endif
        if (ret != 0) {
            LOG_INFOH(_T("Morpher Initialize FAILED: rcfile={}, dicdir={}, unMax={}"), rcfile, dicdir, unkMax);
        }
    }

    inline void morphFinalize() {
#if USE_DYMAZIN
        return DymazinBridge::dymazinFinalize();
#else
        return MecabBridge::mecabFinalize();
#endif
    }

    inline int morphCalcCost(const MString& str, std::vector<MString>& words) {
#if USE_DYMAZIN
        return DymazinBridge::dymazinCalcCost(str, words);
#else
        return MecabBridge::mecabCalcCost(str, words);
#endif
    }
}

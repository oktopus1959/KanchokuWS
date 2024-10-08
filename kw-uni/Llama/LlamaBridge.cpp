#include "Logger.h"
#include "LlamaBridge.h"
#include "e:/Dev/Cpp/llama.cpp/examples/LlamaLoss/LlamaLoss.h"

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

namespace LlamaBridge {
    DEFINE_LOCAL_LOGGER(LlamaBridge);

    int llamaInitialize() {
        LOG_INFOH(_T("ENTER"));

        Vector<char*> argv;
        argv.push_back((char*)"-m");
        argv.push_back((char*)"E:\\Dev\\Cpp\\llama.cpp\\gpt2-small-japanese-char\\gpt2-small-japanese-char-F16.gguf");
        argv.push_back((char*)"--log-disable");
        int result = LlamaLossInitialize((int)argv.size(), argv.data());

        LOG_INFOH(_T("LEAVE: result={}"), result);
        return result;
    }

    void llamaFinalize() {
        LOG_INFOH(_T("CALLED"));
        // リソースの解放
        LlamaLossFinalize();
    }

    int llamaCalcCost(const MString& str) {
        LOG_DEBUGH(_T("ENTER: str={}"), to_wstr(str));
        int cost = (int)(LlamaLossAnalyze(utils::utf8_encode(to_wstr(str)).c_str()));
        LOG_DEBUGH(_T("LEAVE: llamaCost={}"), cost);
        return cost;
    }
}

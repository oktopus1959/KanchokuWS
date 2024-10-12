#include "Logger.h"
#include "LlamaBridge.h"
#include "e:/Dev/Cpp/llama.cpp/examples/LlamaLoss/LlamaLoss.h"

#if 0
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

#define USE_LLAMA 0

namespace LlamaBridge {
    DEFINE_LOCAL_LOGGER(LlamaBridge);

    bool bInitialized = false;

    int llamaInitialize() {
#if USE_LLAMA
        LOG_INFOH(_T("ENTER"));

        Vector<char*> argv;
        argv.push_back((char*)"program");
        argv.push_back((char*)"-m");
        argv.push_back((char*)"E:\\Dev\\Cpp\\llama.cpp\\gpt2-small-japanese-char\\gpt2-small-japanese-char-F16.gguf");
        argv.push_back((char*)"--log-disable");
        int result = LlamaLossInitialize((int)argv.size(), argv.data());

        bInitialized = (result >= 0);

        LOG_INFOH(_T("LEAVE: result={}"), result);
        return result;
#else
        return 0;
#endif
    }

    void llamaFinalize() {
#if USE_LLAMA
        LOG_INFOH(_T("CALLED"));
        if (bInitialized) {
            // リソースの解放
            LlamaLossFinalize();
            bInitialized = false;
        }
#endif
    }

    float llamaCalcCost(const MString& str, std::vector<float>& logits) {
#if USE_LLAMA
        LOG_DEBUGH(_T("ENTER: str={}"), to_wstr(str));
        float cost = 0.0;
        if (bInitialized) {
            const size_t BUFSIZE = 1000;
            float logitBuf[BUFSIZE] = { std::numeric_limits<float>::quiet_NaN() };
            cost = LlamaLossAnalyze(utils::utf8_encode(to_wstr(str)).c_str(), logitBuf, BUFSIZE);
            for (int i = 0; i < BUFSIZE; ++i) {
                if (std::isnan(logitBuf[i])) break;
                logits.push_back(logitBuf[i]);
            }
        }
        LOG_DEBUGH(_T("LEAVE: llamaCost={}"), cost);
        return cost;
#else
        return 0.0f;
#endif
    }
}

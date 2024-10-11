#include "string_utils.h"

namespace LlamaBridge {
    // 初期化
    int llamaInitialize();

    // リソースの解放
    void llamaFinalize();

    // Cost(Loss)の計算
    float llamaCalcCost(const MString& str, std::vector<float>& logits);
}

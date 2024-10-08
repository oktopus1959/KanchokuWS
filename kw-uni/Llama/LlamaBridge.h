#include "string_utils.h"

namespace LlamaBridge {
    // 初期化
    int llamaInitialize();

    // リソースの解放
    void llamaFinalize();

    // Cost(Loss)の計算
    int llamaCalcCost(const MString& str);
}

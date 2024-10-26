#include "string_utils.h"

namespace DymazinBridge {
    int dymazinInitialize(StringRef rcfile, StringRef dicdir, int unkMax);

    void dymazinFinalize();

    int dymazinCalcCost(const MString& str, std::vector<MString>& words);
}

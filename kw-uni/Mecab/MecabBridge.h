#include "string_utils.h"

namespace MecabBridge {
    int mecabInitialize(StringRef rcfile, StringRef dicdir, int unkMax);

    void mecabFinalize();

    int mecabCalcCost(const MString& str, std::vector<MString>& words);
}

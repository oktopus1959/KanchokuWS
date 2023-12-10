#include "string_utils.h"

namespace MecabBridge {
    int mecabInitialize(StringRef rcfile, StringRef dicdir);

    void mecabFinalize();

    int mecabCalcCost(const MString& str);
}

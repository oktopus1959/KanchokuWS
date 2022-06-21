//#include "pch.h"
#include "Node.h"


#if 0
#define _LOG_DEBUGH_FLAG (SETTINGS->debughState)
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_INFO LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

// -------------------------------------------------------------------
// ストローク木を構成するノードのクラスのデフォルト実装

DEFINE_CLASS_LOGGER(Node);

Node::~Node() {
    _LOG_DEBUGH(_T("CALLED: destructor: ptr=%p"), this);
};


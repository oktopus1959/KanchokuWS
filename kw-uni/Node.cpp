//#include "pch.h"
#include "Node.h"


#if 0
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_DEBUGH LOG_INFO
#define LOG_DEBUG LOG_INFO
#define _LOG_DEBUGH LOG_INFO
#define _LOG_DEBUGH_COND LOG_INFO_COND
#endif

// -------------------------------------------------------------------
// ストローク木を構成するノードのクラスのデフォルト実装

DEFINE_CLASS_LOGGER(Node);

Node::~Node() {
    _LOG_DEBUGH(_T("CALLED: destructor: ptr={:p}"), (void*)this);
};


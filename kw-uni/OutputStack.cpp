#include "OutputStack.h"

#define OUTPUT_STACK_MAXSIZE 32

namespace {
}
DEFINE_CLASS_LOGGER(OutputStack);

// 唯一のオブジェクト
std::unique_ptr<OutputStack> OutputStack::Singleton;

void OutputStack::CreateSingleton() {
    Singleton.reset(new OutputStack());
}

String OutputStack::OutputStackBackStrForDebug(size_t len) const {
    return std::regex_replace(to_wstr(OUTPUT_STACK->OutputStackBackStr(len)), std::wregex(_T("\n")), _T("|"));
}

void OutputStack::_resize() {
    if (stack.size() > OUTPUT_STACK_MAXSIZE) {
        // 前半の1/4を削除する
        size_t len = (OUTPUT_STACK_MAXSIZE / 4) * 3;
        size_t pos = stack.size() - len;
        for (size_t i = 0; i < len; ++i) stack[i] = stack[pos + i];
        stack.resize(len);
    }
}

// stackの末尾から、tailMaxlen の範囲で、tailLen の長さの文字列を取得する
// 例: stack="fooBarHoge", tailLen=4, tailMaxLen=7 なら "Hoge" を返す
// bWithFlag = true なら、FLAG のセットしてある文字の後に "|" を付加する
// extraBarPos > 0 なら、末尾から extraBarPos位置に "|" を付加する
MString OutputStack::tail_string(size_t tailLen, size_t tailMaxlen, bool bWithFlag, size_t extraBarPos) const {
    LOG_DEBUGH(_T("ENTER: tailLen={}, tailMaxLen={}, bWithFlag={}, extraBarPos={}"), tailLen, tailMaxlen, bWithFlag, extraBarPos);
    mchar_t buf[OUTPUT_STACK_MAXSIZE + 8];
    size_t stackSize = size();
    if (tailLen > OUTPUT_STACK_MAXSIZE) tailLen = OUTPUT_STACK_MAXSIZE;
    if (tailLen > tailMaxlen) tailLen = tailMaxlen;
    if (tailLen > stackSize) tailLen = stackSize;
    size_t pos = stackSize - tailLen;
    size_t extraPos = stackSize - extraBarPos;
    size_t i = 0;
    while (i < OUTPUT_STACK_MAXSIZE && pos < stackSize) {
        if (bWithFlag && pos == extraPos && i > 0 && buf[i - 1] != '|') buf[i++] = '|';
        buf[i++] = stack[pos].chr;
        if (bWithFlag && ((stack[pos].flag & ~(FLAG_BLOCK_KATA | FLAG_REWRITABLE | FLAG_REWRITABLE_BEGIN)) != 0)) buf[i++] = '|';
        ++pos;
    }
    LOG_DEBUGH(_T("i={}"), i);
    buf[i] = 0;
    LOG_DEBUGH(_T("LEAVE: result={}"), to_wstr(buf));
    return buf;
}

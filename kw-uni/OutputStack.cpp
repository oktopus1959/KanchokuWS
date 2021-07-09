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

wstring OutputStack::OutputStackBackStrForDebug(size_t len) const {
    return std::regex_replace(to_wstr(OUTPUT_STACK->OutputStackBackStr(len)), std::wregex(_T("\n")), _T("|"));
}

void OutputStack::resize() {
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
MString OutputStack::tail_string(size_t tailLen, size_t tailMaxlen, bool bWithFlag) const {
    mchar_t buf[OUTPUT_STACK_MAXSIZE + 2];
    size_t stackSize = size();
    if (tailLen > OUTPUT_STACK_MAXSIZE) tailLen = OUTPUT_STACK_MAXSIZE;
    if (tailLen > tailMaxlen) tailLen = tailMaxlen;
    if (tailLen > stackSize) tailLen = stackSize;
    size_t pos = stackSize - tailLen;
    size_t i = 0;
    while (i < OUTPUT_STACK_MAXSIZE && pos < stackSize) {
        buf[i++] = stack[pos].chr;
        if (bWithFlag && stack[pos].flag != 0) buf[i++] = '|';
        ++pos;
    }
    buf[i] = 0;
    return buf;
}

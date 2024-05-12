#pragma once

#include "string_type.h"
#include "OneShot/PostRewriteOneShot.h"

// GetResultString() の戻り値
class MStringResult {
    // 書き換えノード
    PostRewriteOneShotNode* _rewriteNode;
    // 出力文字列
    MString _resultStr;
    // 書き換え対象文字数
    size_t _rewritableLen;
    // 削除文字数
    int _numBS;
    // 自動部首合成
    bool _bBushuComp;

public:
    PostRewriteOneShotNode* rewriteNode() const { return _rewriteNode; }
    MString resultStr() const { return _resultStr; }
    size_t rewritableLen() const { return _rewritableLen; }
    bool isBushuComp() const { return _bBushuComp; }
    int numBS() const { return _numBS >= 0 ? _numBS : 0; }

    MStringResult() : MStringResult(0) {
    }

    MStringResult(PostRewriteOneShotNode* rewriteNode) : _rewriteNode(rewriteNode), _rewritableLen(0), _numBS(0), _bBushuComp(true) {
    }

    MStringResult(const MString& str, int nBS = 0)
        : _rewriteNode(0), _resultStr(str), _rewritableLen(0), _numBS(nBS), _bBushuComp(true) {
    }

    MStringResult(const MString& str, size_t rewLen, bool bushuComp, int nBS = 0)
        : _rewriteNode(0), _resultStr(str), _rewritableLen(rewLen), _numBS(nBS), _bBushuComp(bushuComp) {
    }

    bool isDefault() const {
        return !_rewriteNode && _resultStr.empty() && _rewritableLen == 0 && _numBS == 0;
    }

    bool isModified() const {
        return !isDefault();
    }

    void clear() {
        _rewriteNode = 0;
        _resultStr.clear();
        _rewritableLen = 0;
        _numBS = 0;
        _bBushuComp = true;
    }

    void setResult(mchar_t mc) {
        _resultStr.assign(1, mc);
    }

    void setResult(const MString& str, int nBS = -1) {
        _resultStr = str;
        if (nBS >= 0) _numBS = nBS;
    }

    void setNumBS(int nBS) {
        _numBS = nBS;
    }

    const PostRewriteOneShotNode* getRewriteNode() const {
        return _rewriteNode;
    }

    void setRewriteNode(PostRewriteOneShotNode* node) {
        _rewriteNode = node;
    }

    void setResultWithRewriteLen(const MString& str, size_t rewLen, int nBS = -1) {
        _resultStr = str;
        _rewritableLen = rewLen;
        if (nBS >= 0) _numBS = nBS;
    }

    void setResult(const MStringResult& result) {
        setResult(result._resultStr, result._rewritableLen, result._bBushuComp, result._numBS);
    }

    void setResult(const MString& str, size_t rewLen, bool bushuComp, int nBS) {
        _resultStr = str;
        _rewritableLen = rewLen;
        _numBS = nBS;
        _bBushuComp = bushuComp;
    }

    String debugString() {
        return _T("str=") + to_wstr(_resultStr) + _T(", rewLen=") + std::to_wstring(_rewritableLen) + _T(", numBS=") + std::to_wstring(_numBS) + _T(", bushuComp=") + std::to_wstring(_bBushuComp);
    }
};


#pragma once

#include "string_type.h"

#include "Logger.h"

#include "deckey_id_defs.h"
#include "DeckeyToChars.h"
#include "StateCommonInfo.h"
#include "Node.h"
//#include "ModalState.h"

#define UNSHIFT_DECKEY(x) (x % PLANE_DECKEY_NUM)
#define DECKEY_TO_SHIFT_PLANE(x) (x / PLANE_DECKEY_NUM)

#define STATE_NAME(p) (p == 0 ? _T("None") : p->GetName())

//-----------------------------------------------------------------------
// TranslateString からの戻り値
class MStringApplyResult {
public:
    MString resultStr;
    size_t rewritableLen;
    int numBS;

    MStringApplyResult() : rewritableLen(0), numBS(-1) {
    }

    MStringApplyResult(const MString& str, size_t rewLen, int nBS = -1)
        : resultStr(str), rewritableLen(rewLen), numBS(nBS) {
    }

    bool isDefault() const {
        return resultStr.empty() && rewritableLen == 0 && numBS == -1;
    }
};

// GetResultString() の戻り値
class MStringResult {
public:
    MString resultStr;
    size_t rewritableLen;
    int numBS;
    bool bBushuComp;

    MStringResult() : rewritableLen(0), numBS(-1), bBushuComp(true) {
    }

    MStringResult(const MString& str, int nBS = -1)
        : resultStr(str), rewritableLen(0), numBS(nBS), bBushuComp(true) {
    }

    MStringResult(const MString& str, size_t rewLen, bool bushuComp, int nBS = -1)
        : resultStr(str), rewritableLen(rewLen), numBS(nBS), bBushuComp(bushuComp) {
    }

    bool isDefault() const {
        return resultStr.empty() && rewritableLen == 0 && numBS == -1;
    }
};

//-----------------------------------------------------------------------
// デコーダ状態の基底クラス
class State {
    DECLARE_CLASS_LOGGER;

    //friend ModalState;

    // 状態チェーンの前の状態
    State* pPrev = 0;

    // 状態チェーンの次の状態
    State* pNext = 0;

    // 次の状態を生成する元となるノード
    // これは状態生成の時に一時的に使用されるだけ
    Node* pNextNodeMaybe= 0;

private:
    // チェーン不要フラグ(デフォルトでチェーンしない)
    bool bUnnecessary = true;

protected:
    // 不要フラグをセット
    void MarkUnnecessary();

    // 不要フラグをリセット
    void MarkNecessary();

public:
    // 不要になった状態か
    virtual bool IsUnnecessary();

protected:
    // この状態が処理の対象とするノード
    // 他で管理されているので、ここで delete してはならない
    Node* pNode = 0;

public:
    inline State* NextState() const { return pNext; }

    // 状態チェーンの次の状態をセット
    State* SetNextState(State* p);

    inline State* PrevState() { return pPrev; }

    // 後続状態を削除する
    void DeleteNextState();

    inline Node* MyNode() { return pNode; }

    // 次の処理のためのノードを取得する
    Node* NextNodeMaybe() const { return pNextNodeMaybe; }

protected:
    //// コンストラクタ -- 派生クラスでコンストラクタを定義しなくて済むようにするため、下記 Initialize を使うようにした
    //State(const Node* pN) : pNode(pN) { }

    // 初期化
    virtual void Initialize(StringRef name, Node* pN) {
        Name = name;
        pNode = pN;
    }

    // 自状態の名前を返す (デバックログ出力で用いる)
    // これ、最初は pure virtual にしていたけど、デストラクタから呼び出そうとすると、(当たり前だが)クラッシュする。
    // (デストラクタは自クラスの vtbl を参照するので)
    // したがって、pure virtual 宣言されたメソッドをデストラクタから呼びたければデストラクタも pure virtual にする必要あり。
    // ここでは Name() を pure virtual にするのを諦めた。
    //virtual String Name() const/* = 0*/;
    // で結局メンバ変数で持つことにした。
    String Name;

public:
    // デストラクタ
    virtual ~State();

    inline String GetName() const { return Name; }

    virtual String JoinedName() const;

public:
    // 入力された DECKEY を処理するチェイン
    virtual void HandleDeckeyChain(int deckey);
protected:
    // 入力された DECKEY を処理する(前処理)
    virtual int HandleDeckeyPreProc(int deckey);
    // 入力された DECKEY を処理する(後処理)
    virtual void HandleDeckeyPostProc();

protected:
    //// 履歴常駐状態の事前チェック
    //virtual void DoHistoryResidentPreCheck();

    //// ModalStateの前処理
    //virtual int DoModalStatePreProc(int /*deckey*/);

protected:
    // 中間チェック
    void DoIntermediateCheckChain();
    virtual void DoIntermediateCheck();

//public:
//    // DECKEY処理の後半部
//    void DoDeckeyPostProcChain();
//protected:
//    void DoDeckeyPostProc();

protected:
    // 新しい状態作成のチェイン
    virtual void CreateNewStateChain();
    void CreateNewState();

    // 状態が生成されたときに実行する処理 (その状態をチェインする場合は true を返す)
    virtual void DoProcOnCreated();

protected:
    // 出力文字を取得する
    virtual void GetResultStringChain(MStringResult&);

protected:
    // チェーンをたどって不要とマークされた後続状態を削除する
    virtual void DeleteUnnecessarySuccessorStateChain();
    // 不要とマークされた後続状態を削除する
    void DeleteUnnecessarySuccessorState();

protected:
    // チェーンをたどって後続状態を削除する
    void DeleteNextStateChain();

    // TODO: protected にする
public:
    // 文字列を変換
    virtual MString TranslateString(const MString&);

    // ノードが保持する文字列をこれまでの出力文字列に適用
    virtual MStringApplyResult ApplyResultString();

    // この状態以降を不要としてマークする
    virtual void MarkUnnecessaryFromThis();

    // 履歴検索を初期化する状態か
    virtual bool IsHistoryReset();

protected:
    // モード標識文字を返す
    virtual mchar_t GetModeMarker();


    //----------------------------------------------------------------------------------
    // Decoder からも呼ばれるメソッド
public:
    // 「最終的な出力履歴が整ったところで呼び出される処理」を先に次状態に対して実行する
    void DoLastHistoryProcChain();
protected:
    // 最終的な出力履歴が整ったところで呼び出される処理
    virtual void DoLastHistoryProc();

public:
    // 状態の再アクティブ化
    virtual void Reactivate();

    // ノードから生成した状態を後接させ、その状態を常駐させる
    virtual void CreateStateAndStayResidentAtEndOfChain(Node*);

    // 居残っている一時状態の削除(常駐ノードなら false を返す)
    virtual void DeleteRemainingState();

    //// ストローク機能をすべて削除するか
    //virtual bool IsToRemoveAllStroke() const;

    // ストロークテーブルチェインの長さ(テーブルのレベル)
    virtual size_t StrokeTableChainLength() const;

    // 状態チェインの長さ
    inline size_t ChainLength() { return pNext == 0 ? 1 : pNext->ChainLength() + 1; }

    // 入力・変換モード標識を連結して返す
    MString JoinModeMarker();

    void JoinModeMarker(MString& modeMarker);

    //----------------------------------------------------------------------------------
protected:
    // 次の処理のためのノードをセットする
    void SetNextNodeMaybe(Node* pN) { pNextNodeMaybe = pN; }

    void ClearNextNodeMaybe() { pNextNodeMaybe = nullptr; }

    //// 次状態をチェックして、自身の状態を変更させるのに使う。DECKEY処理の後半部で呼ばれる。必要に応じてオーバーライドすること。
    //// 例：ストロークの末尾まで到達して、ストロークチェイン全体が不要になった
    //// 例：次ストロークが取り消されたので、自ストロークも初期状態に戻す
    //virtual void CheckNextState();

    //// 自身の状態をチェックして後処理するのに使う。DECKEY処理の後半部で呼ばれる。必要に応じてオーバーライドすること。
    //virtual void CheckMyState();

    // 常駐機能か
    virtual bool IsResident() const;

    //仮想鍵盤にストロークヘルプの情報を設定する
    void copyStrokeHelpToVkbFaces(wchar_t ch);

    //仮想鍵盤にストロークヘルプの情報を設定する(outStringの先頭文字)
    void copyStrokeHelpToVkbFaces();

    // 文字削除をリザルト情報にセットする
    // 引数は、削除する文字数
    static void setCharDeleteInfo(int numDelete) {
        STATE_COMMON->SetBackspaceNum(numDelete);
    }

    // 入力されたDECKEYをそのままGUI返す
    static void setThroughDeckeyFlag() {
        STATE_COMMON->SetDeckeyToVkeyFlag();
    }

    // 入力されたDECKEYをそのままGUI返す
    static bool isThroughDeckey() {
        return STATE_COMMON->IsDeckeyToVkey();
    }

    // 特殊キーをDECKEYとして登録する必要あり
    static void setSpecialDeckeys() {
        STATE_COMMON->SetSpecialDeckeyRequiredFlag();
    }

public:
    // DECKEY はストロークキーか
    static bool isNormalStrokeKey(int deckey);

    // DECKEY はShift修飾キーか
    static bool isShiftedKey(int deckey);

    // DECKEY はCtrl修飾キーか
    static bool isCtrledKey(int deckey);

    // DECKEY はストロークキーとして扱われる機能キーか
    static bool isStrokableFuncKey(int deckey);

    // DECKEY は同時打鍵シフトキーか
    static bool isComboShiftedKey(int deckey);

    // DECKEY はストロークキーとして扱われるキーか
    static bool isStrokableKey(int deckey);

public:
    // 入力された DECKEY をディスパッチする
    virtual void dispatchDeckey(int deckey);

    // EisuCancel ハンドラ
    virtual void handleEisuCancel();

    // EisuCancel ハンドラ
    virtual void handleEisuDecapitalize();

    // commit ハンドラ
    virtual void handleCommitState();

    //--------------------------------------------------------------------
    // 以下、DECKEYハンドラのデフォルト実装
 
    // ストロークキーハンドラ
    virtual void handleStrokeKeys(int deckey);

    // 機能キーハンドラ
    // 一括で何かをしたい場合にオーバーライドする。その後、個々の処理を続ける場合は、 false を返すこと
    virtual bool handleFunctionKeys(int deckey);

    // スペースキーハンドラ
    virtual void handleSpaceKey();

    // FullEscape ハンドラ
    virtual void handleFullEscape();

    // Unblock ハンドラ
    virtual void handleUnblock();

    // NextCandTrigger ハンドラ
    virtual void handleNextCandTrigger();

    // PrevCandTrigger ハンドラ
    virtual void handlePrevCandTrigger();

    // ZenkakuConversion ハンドラ
    virtual void handleZenkakuConversion();

    // KatakanaConversion ハンドラ
    virtual void handleKatakanaConversion();

    // EisuMode デフォルトハンドラ
    virtual void handleEisuMode();

    // DecoderOff ハンドラ
    virtual void handleDecoderOff();

    // ClearStroke ハンドラ
    virtual void handleClearStroke();

    // ToggleBlocker ハンドラ
    virtual void handleToggleBlocker();

    // UndefinedDeckey ハンドラ
    virtual void handleUndefinedDeckey(int deckey);

    //// Ctrl-A ～ Ctrl-Zハンドラ
    virtual void handleCtrlKeys(int deckey);
    //virtual void handleCtrlA();
    //virtual void handleCtrlB();
    //virtual void handleCtrlC();
    //virtual void handleCtrlD();
    //virtual void handleCtrlE();
    //virtual void handleCtrlF();
    //virtual void handleCtrlG();
    //virtual void handleCtrlH();
    //virtual void handleCtrlI();
    //virtual void handleCtrlJ();
    //virtual void handleCtrlK();
    //virtual void handleCtrlL();
    //virtual void handleCtrlM();
    //virtual void handleCtrlN();
    //virtual void handleCtrlO();
    //virtual void handleCtrlP();
    //virtual void handleCtrlQ();
    //virtual void handleCtrlR();
    //virtual void handleCtrlS();
    //virtual void handleCtrlT();
    //virtual void handleCtrlU();
    //virtual void handleCtrlV();
    //virtual void handleCtrlW();
    //virtual void handleCtrlX();
    //virtual void handleCtrlY();
    //virtual void handleCtrlZ();

    // Shiftキー ハンドラ
    virtual void handleShiftKeys(int deckey);

    virtual void handleSpecialKeys(int deckey);

    // < ハンドラ
    virtual void handleLeftTriangle();

    // > ハンドラ
    virtual void handleRightTriangle();

    // ? ハンドラ
    virtual void handleQuestion();

    // left/right maze shift keys
    virtual void handleLeftRightMazeShift(int deckey);

    //// Shift+Space ハンドラ
    //virtual void handleShiftSpace();

    //// Shift+Space AsNoramlSpace ハンドラ
    //virtual void handleShiftSpaceAsNormalSpace();

    //// Ctrl+Space ハンドラ
    //virtual void handleCtrlSpace();

    //// Ctrl+Shift+Space ハンドラ
    //virtual void handleCtrlShiftSpace();

    // RET/Enter ハンドラ
    virtual void handleEnter();
    
    // ESC ハンドラ
    virtual void handleEsc();
    
    // BS ハンドラ
    virtual void handleBS();

    // TAB ハンドラ
    virtual void handleTab();

    // ShiftTAB ハンドラ
    virtual void handleShiftTab();

    // Delete ハンドラ
    virtual void handleDelete();

    // Home ハンドラ
    virtual void handleHome();

    // End ハンドラ
    virtual void handleEnd();

    // PageUp ハンドラ
    virtual void handlePageUp();

    // PageDown ハンドラ
    virtual void handlePageDown();

    // ← ハンドラ
    virtual void handleLeftArrow();

    // Ctrl ← ハンドラ
    virtual void handleCtrlLeftArrow();

    // → ハンドラ
    virtual void handleRightArrow();

    // Ctrl → ハンドラ
    virtual void handleCtrlRightArrow();

    // ↑ ハンドラ
    virtual void handleUpArrow();

    // Ctrl ↑ ハンドラ
    virtual void handleCtrlUpArrow();

    // ↓ ハンドラ
    virtual void handleDownArrow();

    // Ctrl ↓ ハンドラ
    virtual void handleCtrlDownArrow();

};


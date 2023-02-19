#pragma once

#include "string_type.h"

#include "Logger.h"

#include "deckey_id_defs.h"
#include "DeckeyToChars.h"
#include "StateCommonInfo.h"
#include "Node.h"

#define UNSHIFT_DECKEY(x) (x % PLANE_DECKEY_NUM)
#define DECKEY_TO_SHIFT_PLANE(x) (x / PLANE_DECKEY_NUM)

#define STATE_NAME_PTR(p) (p == 0 ? _T("None") : p->GetName().c_str())

//-----------------------------------------------------------------------
// デコーダ状態の基底クラス
class State {
    DECLARE_CLASS_LOGGER;

    // 次の状態を生成する元となるノード
    // これは状態生成の時に一時的に使用されるだけ
    Node* pNextNodeMaybe= 0;

    // 現在処理中のキー
    int currentDeckey = 0;

protected:
    // 不要フラグ
    bool bUnnecessary = false;

protected:
    // 前の状態
    State* pPrev = 0;

    // 次の状態
    State* pNext = 0;

    // この状態が処理の対象とするノード
    // 他で管理されているので、ここで delete してはならない
    Node* pNode;

public:
    inline void SetPrevState(State* pp) { pPrev = pp; }

protected:
    //// コンストラクタ -- 派生クラスでコンストラクタを定義しなくて済むようにするため、下記 Initialize を使うようにした
    //State(const Node* pN) : pNode(pN) { }

    // 初期化
    virtual void Initialize(const tstring& name, Node* pN) {
        Name = name;
        pNode = pN;
    }

    // 自状態の名前を返す (デバックログ出力で用いる)
    // これ、最初は pure virtual にしていたけど、デストラクタから呼び出そうとすると、(当たり前だが)クラッシュする。
    // (デストラクタは自クラスの vtbl を参照するので)
    // したがって、pure virtual 宣言されたメソッドをデストラクタから呼びたければデストラクタも pure virtual にする必要あり。
    // ここでは Name() を pure virtual にするのを諦めた。
    //virtual tstring Name() const/* = 0*/;
    // で結局メンバ変数で持つことにした。
    tstring Name;

public:
    // デストラクタ
    virtual ~State();

    inline tstring GetName() const { return Name; }

    inline tstring JoinedName() const {
        if (pNext) {
            return Name + _T("-") + pNext->JoinedName();
        } else {
            return Name;
        }
    }

public:
    // カスタマイズ不可なメソッド

    // 入力された DECKEY を処理する(これは全状態で共通の処理)
    void HandleDeckey(int deckey);

    // 「最終的な出力履歴が整ったところで呼び出される処理」を先に次状態に対して実行する
    void DoOutStringProcChain();

private:
    // DECKEY処理の前半部
    void DoDeckeyPreProc(int deckey);

    // DECKEY処理の後半部
    void DoDeckeyPostProc();

    // 後続状態チェインに対して事後チェック
    void DoPostCheckChain();

    // 不要とマークされた後続状態を削除する (HandleDeckeyから呼ばれる)
    void DeleteUnnecessarySuccessorState();

public:
    // カスタマイズ可能なメソッド

    // 事前チェック
    virtual void DoPreCheck() { }

    // 中間チェック
    virtual void DoIntermediateCheck() { }

    // 状態の再アクティブ化
    virtual void Reactivate();

    // 最終的な出力履歴が整ったところで呼び出される処理
    virtual void DoOutStringProc();

    // ノードから生成した状態を後接させ、その状態を常駐させる
    virtual void ChainAndStay(Node*);

    // 居残っている一時状態の削除(常駐ノードなら false を返す)
    virtual void DeleteRemainingState();

    //// ストローク機能をすべて削除するか
    //virtual bool IsToRemoveAllStroke() const;

    // ストロークテーブルチェインの長さ(テーブルのレベル)
    virtual size_t StrokeTableChainLength();

    // 状態チェインの長さ
    inline size_t ChainLength() { return pNext == 0 ? 1 : pNext->ChainLength() + 1; }

    // 不要になった状態か
    virtual bool IsUnnecessary();

    // 文字列を変換
    virtual MString TranslateString(const MString&);

    // この状態以降を不要としてマークする
    virtual void MarkUnnecessaryFromThis();

    // 履歴検索を初期化する状態か
    virtual bool IsHistoryReset();

    // 入力・変換モード標識を連結して返す
    MString JoinModeMarker();

    void JoinModeMarker(MString& modeMarker);

    // モード標識文字を返す
    virtual mchar_t GetModeMarker();

protected:
    // モード状態か
    virtual bool IsModeState() { return false; }

    // 次の処理のためのノードをセットする
    void SetNextNodeMaybe(Node* pN) { pNextNodeMaybe = pN; }

    void ClearNextNodeMaybe() { pNextNodeMaybe = nullptr; }

    // 次の処理のためのノードを取得する
    Node* NextNodeMaybe() const { return pNextNodeMaybe; }

    // 次状態をチェックして、自身の状態を変更させるのに使う。DECKEY処理の後半部で呼ばれる。必要に応じてオーバーライドすること。
    // 例：ストロークの末尾まで到達して、ストロークチェイン全体が不要になった
    // 例：次ストロークが取り消されたので、自ストロークも初期状態に戻す
    virtual void CheckNextState();

    // 常駐機能か
    virtual bool IsStay() const;

    //仮想鍵盤にストロークヘルプの情報を設定する
    void copyStrokeHelpToVkbFaces(wchar_t ch);

    //仮想鍵盤にストロークヘルプの情報を設定する(outStringの先頭文字)
    void copyStrokeHelpToVkbFaces();

    // 状態が生成されたときに実行する処理 (その状態をチェインする場合は true を返す)
    virtual bool DoProcOnCreated();

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


//#include "Settings.h"
//#include "State.h"

#include "History/HistoryDic.h"

// -------------------------------------------------------------------
// 履歴入力リストのクラス
class HistCandidates {
public:
    // デストラクタ
    virtual ~HistCandidates() { }

    // 履歴検索キー設定をクリアする
    virtual void ClearKeyInfo() = 0;

    virtual bool IsHistInSearch() = 0;

    virtual const MString& GetOrigKey() = 0;

    // 指定のキーで始まる候補を取得する (len > 0 なら指定の長さの候補だけを取得, len < 0 なら Abs(len)以下の長さの候補を取得)
    virtual const std::vector<HistResult>& GetCandidates(const MString& key, bool bCheckMinKeyLen, int len) = 0;

    virtual const std::vector<MString> GetCandWords(const MString& key, bool bCheckMinKeyLen, int len) = 0;

    // 取得済みの候補列を返す
    //virtual const std::vector<HistResult>& GetCandidates() const = 0;

    virtual const std::vector<MString> GetCandWords() const = 0;

    virtual const MString& GetCurrentKey() const = 0;

    // 次の履歴を選択する
    virtual const HistResult GetNext() const = 0;

    // 前の履歴を選択する
    virtual const HistResult GetPrev() const = 0;

    // 選択された単語を取得する
    virtual const HistResult GetPositionedHist(size_t pos) const = 0;

    // 選択された単語を取得する
    virtual const MString& GetSelectedWord() const = 0;

    // 選択されている位置を返す -- 未選択状態なら -1を返す
    virtual int GetSelectPos() const = 0;

    // 選択位置を初期化(未選択状態)する
    virtual const HistResult ClearSelectPos() = 0;

    // 候補が選択されていれば、それを使用履歴の先頭にpushする -- selectPos は未選択状態に戻る
    virtual void DelayedPushFrontSelectedWord() = 0;

    // 取得済みの履歴入力候補リストから指定位置の候補を返す
    // 選択された候補は使用履歴の先頭に移動する
    virtual const HistResult SelectNth(size_t n) = 0;

    virtual void DeleteNth(size_t n) = 0;

public:
    static std::unique_ptr<HistCandidates> Singleton;

    static void CreateSingleton();
};

#define HIST_CAND (HistCandidates::Singleton)


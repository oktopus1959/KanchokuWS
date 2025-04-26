#pragma once

#include "string_utils.h"
#include "misc_utils.h"

class Node;
class StrokeTableNode;

// ストロークヘルプ
class StrokeHelp {
private:
    // 文字に対するストローク列を与えるマップ
    std::map<mchar_t, std::vector<int>> strokeHelpMap;

    std::vector<int> emptyVec;

    int temp[10];

    // 各文字に対するストロークを求める(下請け)
    void gatherStrokeHelp(StrokeTableNode*, size_t);

public:
    static std::unique_ptr<StrokeHelp> Singleton;

    // 各文字に対するストロークを求める
    static void GatherStrokeHelp();

public:
    StrokeHelp();

    ~StrokeHelp();

    // 文字に対するストローク(DeckeyId)列を返す
    inline const std::vector<int>& GetStrokeHelp(mchar_t ch) const {
        auto iter = strokeHelpMap.find(ch);
        if (iter != strokeHelpMap.end())
            return iter->second;
        return emptyVec;
    }

    // 文字がストローク表にあるか否かを返す
    inline bool Find(mchar_t ch) {
        return strokeHelpMap.find(ch) != strokeHelpMap.end();
    }

    //仮想鍵盤にストロークヘルプの情報を設定する
    bool copyStrokeHelpToVkbFacesOutParams(mchar_t ch, wchar_t* faces, size_t facesSize);

    //仮想鍵盤にストロークヘルプの情報を設定する
    bool copyStrokeHelpToVkbFacesStateCommon(mchar_t ch, mchar_t* faces, size_t facesSize);

};

#define STROKE_HELP  (StrokeHelp::Singleton)

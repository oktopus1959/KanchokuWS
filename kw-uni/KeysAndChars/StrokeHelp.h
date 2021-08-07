#pragma once

#include "string_utils.h"
#include "misc_utils.h"

class Node;
class StrokeTableNode;

// ストロークヘルプ
class StrokeHelp {
private:
    // 文字に対するストローク列を与えるマップ
    // ストローク列は -1 で終端する
    std::map<mchar_t, int*> strokeHelpMap;

    int* strokeHelpArray = 0;

    int temp[10];

    // 各文字に対するストロークを求める(下請け)
    size_t gatherStrokeHelp(StrokeTableNode*, size_t, size_t);

public:
    static std::unique_ptr<StrokeHelp> Singleton;

    // 各文字に対するストロークを求める
    static void GatherStrokeHelp();

public:
    StrokeHelp();

    ~StrokeHelp();

    // 文字に対するストローク(DeckeyId)列を返す
    inline int* GetStrokeHelp(mchar_t ch) {
        auto iter = strokeHelpMap.find(ch);
        if (iter != strokeHelpMap.end())
            return iter->second;
        return nullptr;
    }

    // 文字がストローク表にあるか否かを返す
    inline bool Find(mchar_t ch) {
        return strokeHelpMap.find(ch) != strokeHelpMap.end();
    }

    //仮想鍵盤にストロークヘルプの情報を設定する
    bool copyStrokeHelpToVkbFacesOutParams(mchar_t ch, wchar_t* faces);

    //仮想鍵盤にストロークヘルプの情報を設定する
    bool copyStrokeHelpToVkbFacesStateCommon(mchar_t ch, mchar_t* faces);

};

#define STROKE_HELP  (StrokeHelp::Singleton)

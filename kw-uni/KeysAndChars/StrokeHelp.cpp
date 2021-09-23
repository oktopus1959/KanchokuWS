// StrokeHelp
#include "file_utils.h"
#include "StrokeTable.h"
#include "StringNode.h"

#include "deckey_id_defs.h"
#include "StrokeHelp.h"

#define STROKE_HELP_ARRAY_SIZE 10000

namespace {
}

// Decoder.cpp で生成される
std::unique_ptr<StrokeHelp> StrokeHelp::Singleton;

StrokeHelp::StrokeHelp() {
    strokeHelpArray = new int[STROKE_HELP_ARRAY_SIZE];
}

StrokeHelp::~StrokeHelp() {
    delete[] strokeHelpArray;
}

// 各文字に対するストロークを求める
void StrokeHelp::GatherStrokeHelp() {
    if (Singleton) return;

    Singleton.reset(new StrokeHelp());

    Singleton->gatherStrokeHelp(ROOT_STROKE_NODE.get(), 0, 0);
}

size_t StrokeHelp::gatherStrokeHelp(StrokeTableNode* pNode, size_t pos, size_t depth) {
    for (size_t i = 0; i < STROKE_SPACE_DECKEY; ++i) {
        Node* blk = pNode->getNth(i);
        if (blk) {
            if (blk->isStrokeTableNode()) {
                temp[depth] = (int)i;
                pos = gatherStrokeHelp((StrokeTableNode*)blk, pos, depth + 1);
            } else if (blk->isStringNode()) {
                if (pos + depth + 2 > STROKE_HELP_ARRAY_SIZE) break;
                if (blk->getString().size() == 1) {
                    strokeHelpMap[utils::safe_front(blk->getString())] = Singleton->strokeHelpArray + pos;
                    for (size_t j = 0; j < depth; ++j) {
                        strokeHelpArray[pos++] = temp[j];
                    }
                    strokeHelpArray[pos++] = i;
                    strokeHelpArray[pos++] = -1;
                }
            }
        }
    }
    return pos;
}

namespace {
    wstring strokeMarkers = _T("◎●○△");

    //仮想鍵盤にストロークヘルプの情報を設定する (faces は 1セルが 2 wchar で構成されることに注意)
    template<typename T>
    bool copyStrokeHelpToVkbFacesImpl(int* pStroke, T* faces, size_t factor) {
        if (pStroke == nullptr) return false;

        bool bFirst = true;
        size_t count = 0;
        while (*pStroke >= 0 && count < strokeMarkers.size()) {
            ++count;
            int strPos = *pStroke++;
            if (strPos == *pStroke) {
                faces[strPos * factor] = strokeMarkers[0];
                ++pStroke;
                ++count;
            } else {
                faces[strPos * factor] = strokeMarkers[count];
                bFirst = false;
            }
        }
        return true;

    }
}

//仮想鍵盤にストロークヘルプの情報を設定する
bool StrokeHelp::copyStrokeHelpToVkbFacesOutParams(mchar_t ch, wchar_t* faces) {
    return copyStrokeHelpToVkbFacesImpl(GetStrokeHelp(ch), faces, 2);
}

//仮想鍵盤にストロークヘルプの情報を設定する
bool StrokeHelp::copyStrokeHelpToVkbFacesStateCommon(mchar_t ch, mchar_t* faces) {
    return copyStrokeHelpToVkbFacesImpl(GetStrokeHelp(ch), faces, 1);
}

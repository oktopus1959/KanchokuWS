// このファイルは FunctionNodeManager.cpp によってインクルードされる
#include "FunctionNodeManager.h"

//----------------------------------------------------------------------
// 以下に新しい機能のためのビルダーヘッダーファイルを追加
#include "KeysAndChars/MyPrevChar.h"
#include "BushuComp/BushuComp.h"
#include "BushuComp/BushuAssoc.h"
#include "Mazegaki/Mazegaki.h"
#include "History/History.h"
#include "EscapeNode.h"
#include "KeysAndChars/Zenkaku.h"
#include "KeysAndChars/Katakana.h"
#include "OneShot/KatakanaOneShot.h"
#include "OneShot/HankakuKatakanaOneShot.h"
#include "OneShot/PostRewriteOneShot.h"
#include "OneShot/OneShot.h"
//#include "Template/Template.h"

void FunctionNodeManager::AddFunctionNodeBuilders() {
    // 以下に新しい機能のためのビルダーとその指定子を追加
    // ここで指定する名前は、Settings.cpp の機能へのキー割り当ての変更で使われる
    addFunctionNodeBuilder(_T("^"), _T("myChar"), new MyCharNodeBuilder());
    addFunctionNodeBuilder(_T("v"), _T("prevChar"), new PrevCharNodeBuilder());
    addFunctionNodeBuilder(_T("B"), _T("bushuComp"), new BushuCompNodeBuilder());
    addFunctionNodeBuilder(_T("b"), _T("bushuComp"), new BushuCompNodeBuilder());
    addFunctionNodeBuilder(_T("A"), _T("bushuAssoc"), new BushuAssocNodeBuilder());
    addFunctionNodeBuilder(_T("a"), _T("bushuAssocDirect"), new BushuAssocExNodeBuilder());
    addFunctionNodeBuilder(_T("M"), _T("mazegaki"), new MazegakiNodeBuilder());
    addFunctionNodeBuilder(_T("m"), _T("mazegaki"), new MazegakiNodeBuilder());
    addFunctionNodeBuilder(_T("!"), _T("history"), new HistoryNodeBuilder());
    addFunctionNodeBuilder(_T("1"), _T("historyOneChar"), new HistoryOneCharNodeBuilder());
    addFunctionNodeBuilder(_T("?"), _T("historyFewChars"), new HistoryFewCharsNodeBuilder());
    addFunctionNodeBuilder(_T("\\"), _T("nextThrough"), new EscapeNodeBuilder());
    addFunctionNodeBuilder(_T("Z"), _T("zenkakuMode"), new ZenkakuNodeBuilder());
    addFunctionNodeBuilder(_T("z"), _T("zenkakuOneChar"), new ZenkakuOneNodeBuilder());
    addFunctionNodeBuilder(_T("K"), _T("katakanaMode"), new KatakanaNodeBuilder());
    addFunctionNodeBuilder(_T("k"), _T("katakanaOneShot"), new KatakanaOneShotNodeBuilder());
    addFunctionNodeBuilder(_T("h"), _T("hanKataOneShot"), new HankakuKatakanaOneShotNodeBuilder());
    addFunctionNodeBuilder(_T("bs"), _T("blkSetOneShot"), new BlockerSetterNodeBuilder());
    addFunctionNodeBuilder(_T("D"), _T("dakutenOneShot"), new DakutenOneShotNodeBuilder());
    addFunctionNodeBuilder(_T("゛"), _T("dakutenOneShot"), new DakutenOneShotNodeBuilder());
    addFunctionNodeBuilder(_T("d"), _T("handakutenOneShot"), new HanDakutenOneShotNodeBuilder());
    addFunctionNodeBuilder(_T("゜"), _T("handakutenOneShot"), new HanDakutenOneShotNodeBuilder());
    //addFunctionNodeBuilder(_T("Z"), new TemplateNodeBuilder());
}


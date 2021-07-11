/* -------------------------------------------------------------------
 * Decoder クラス - ストローク⇒文字 変換器
 */
//#include "pch.h"
#include "string_utils.h"
#include "path_utils.h"
#include "misc_utils.h"
#include "file_utils.h"
#include "misc_utils.h"
#include "KanchokuIni.h"

#include "Decoder.h"
#include "hotkey_id_defs.h"
#include "KeysAndChars/HotkeyToChars.h"
#include "KeysAndChars/VkbTableMaker.h"
#include "ErrorHandler.h"
#include "Settings.h"
#include "State.h"
#include "StartNode.h"
#include "FunctionNodeManager.h"
#include "EasyChars.h"
#include "StrokeTable.h"
#include "StrokeHelp.h"

#include "BushuComp/BushuDic.h"
#include "BushuComp/BushuAssocDic.h"
#include "History/History.h"
#include "History/HistoryDic.h"
#include "Mazegaki/MazegakiDic.h"

// -------------------------------------------------------------------
namespace {
    inline void set_facestr(mchar_t m, wchar_t* faces) {
        auto p = decomp_mchar(m);
        if (p.first != 0) {
            *faces++ = p.first;
        } else {
            faces[1] = 0;
        }
        *faces = p.second;
    }

}

// デコーダの実装クラス
class DecoderImpl : public Decoder {
private:
    DECLARE_CLASS_LOGGER;

    // UI側に情報出力するための構造体へのポインタ
    // インスタンス自体はUI側で用意するため、こちら側で delete してはいけない
    DecoderOutParams* OutParams;

    // デコーダ状態の始点
    std::unique_ptr<Node> startNode;
    std::unique_ptr<State> startState;

    void setErrorMsg(DecoderCommandParams* params) {
        if (params) {
            wcscpy_s(params->inOutData, ERROR_HANDLER->GetErrorMsg().c_str());
        }
    }

public:
    // コンストラクタ
    DecoderImpl() : OutParams(0)
    {
        LOG_INFO(_T("CALLED"));
    }

    // デストラクタ
    ~DecoderImpl() {
    }

    // 初期化
    void Initialize(DecoderCommandParams* params) {
        LOG_INFO(_T("ENTER"));

        // 状態の共有情報生成
        StateCommonInfo::CreateSingleton();

        // 出力された文字列を保持するスタックを生成
        OutputStack::CreateSingleton();

        // 機能ノードビルダーの登録
        FunctionNodeManager::AddFunctionNodeBuilders();

        // GUIから送られてきた settings を読み込んで Settings::Singleton を構築
        createSettings(params->inOutData);

        // Hotkey から文字への変換インスタンスの構築
        createHotkeyToCharsInstance();

        // ストローク木の構築
        createStrokeTree();

        // ここで機能呼び出しキーの再割り当て
        reloadSettings(params->inOutData);

        // 始状態
        startNode.reset(new StartNode());
        startState.reset(startNode->CreateState());

        // 履歴入力機能を生成して常駐させる
        startState->ChainAndStay(HistoryStayNode::CreateNode());
        // 必要があれば、ここにその他の常駐機能を追加する

        // 簡易打鍵文字を集める
        EasyChars::GatherEasyChars();

        // ストロークヘルプを求めておく
        StrokeHelp::GatherStrokeHelp();
        // 部首合成の部品について、ストローク可能文字か否かを設定しておく
        if (BUSHU_DIC) BUSHU_DIC->MakeStrokableMap();

        LOG_INFO(_T("LEAVE"));
    }

    // 終了
    void Destroy() {
        LOG_INFO(_T("CALLED"));
    }

    // GUIから送られてきた settings を読み込んで Settings::Singleton を構築
    void createSettings(const wstring& settings) {
        LOG_INFO(_T("ENTER"));

        SETTINGS.reset(new Settings);

        reloadSettings(settings);

        LOG_INFO(_T("LEAVE"));
    }
    
    void reloadSettings(const wstring & settings) {
        LOG_INFO(_T("ENTER: settings=%s"), settings.c_str());
        
        std::map<tstring, tstring> key_vals;

        for (auto item : utils::split(settings, '\n')) {
            LOG_INFO(_T("item=%s"), item.c_str());
            auto pair = utils::split(item, '=');
            if (pair.size() == 2) {
                key_vals[pair[0]] = pair[1];
            }
        }

        int logLevel = utils::strToInt(utils::safe_get(key_vals, tstring(_T("logLevel"))));
        if (logLevel > 0) Logger::LogLevel = logLevel;

        SETTINGS->SetValues(key_vals);

        LOG_INFO(_T("LEAVE"));
    }

    // Hotkey から文字への変換インスタンスの構築
    void createHotkeyToCharsInstance() {

        auto filePath = SETTINGS->charsDefFile;
        LOG_INFO(_T("charsDefFile=%s"), filePath.c_str());
        //if (filePath.empty()) {
        //    ErrorHandler::Error(_T("「charsDefFile=(ファイル名)」の設定がまちがっているようです"));
        //}
        HotkeyToChars::CreateSingleton(filePath);
    }

    // テーブルファイルを読み込んでストローク木を作成する
    void createStrokeTree() {
        LOG_INFO(_T("ENTER"));

        // テーブルファイル名
        auto tableFile = SETTINGS->tableFile;
        LOG_INFO(_T("tableFile=%s"), tableFile.c_str());
        if (tableFile.empty()) {
            // エラー
            ERROR_HANDLER->Error(_T("「tableFile=(ファイル名)」の設定がまちがっているようです"));
        }
        SETTINGS->tableFile = tableFile;

        LOG_INFO(_T("open table file: %s"), tableFile.c_str());

        utils::IfstreamReader reader(tableFile);
        if (reader.success()) {
            auto lines = utils::IfstreamReader(tableFile).getAllLines();
            // ストロークノード木の構築
            StrokeTableNode::CreateStrokeTree(lines);
            LOG_INFO(_T("close table file: %s"), tableFile.c_str());
        } else {
            // エラー
            LOG_ERROR(_T("Can't read table file: %s"), tableFile.c_str());
            ERROR_HANDLER->Error(utils::format(_T("テーブルファイル(%s)が開けません"), tableFile.c_str()));
        }

        LOG_INFO(_T("LEAVE"));
    }

    // 初期打鍵表(下端機能キー以外は空白)の作成
    void MakeInitialVkbTable(DecoderOutParams* outParam) {
        VkbTableMaker::MakeInitialVkbTable(outParam->faceStrings);
    }

    // デコーダ状態のリセット (Decoder が ON になったときに呼ばれる)
    void Reset() {
        LOG_INFOH(_T("\nENTER: states=%s (len=%d), flags=%u, numBS=%d, outLength=%d, stack=%s"),
            startState->JoinedName().c_str(), startState->ChainLength(), STATE_COMMON->GetResultFlags(), 
            STATE_COMMON->GetBackspaceNum(), STATE_COMMON->OutString().size(), OUTPUT_STACK->OutputStackBackStrForDebug(5).c_str());
        if (startState) startState->DeleteRemainingState();
        STATE_COMMON->ClearAllStateInfo();
        OUTPUT_STACK->pushNewLine();    // 履歴ブロッカーとして改行を追加
        if (startState) startState->Reactivate();
        LOG_INFOH(_T("LEAVE: states=%s (len=%d), flags=%u, numBS=%d, outLength=%d, stack=%s\n"),
            startState->JoinedName().c_str(), startState->ChainLength(), STATE_COMMON->GetResultFlags(),
            STATE_COMMON->GetBackspaceNum(), STATE_COMMON->OutString().size(), OUTPUT_STACK->OutputStackBackStrForDebug(5).c_str());
    }

    // デコーダが扱う辞書を保存する
    void SaveDicts() {
        LOG_INFO(_T("CALLED"));
        if (BUSHU_DIC) BUSHU_DIC->WriteBushuDic();
        if (BUSHU_ASSOC_DIC) BUSHU_ASSOC_DIC->WriteBushuAssocDic();
        if (MAZEGAKI_DIC) MAZEGAKI_DIC->WriteMazegakiDic();
        if (HISTORY_DIC) {
            HISTORY_DIC->WriteHistoryDic();
            //HISTORY_DIC->WriteHistUsedDic();
            //HISTORY_DIC->WriteHistExcludeDic();
            //HISTORY_DIC->WriteNgramDic();
        }
    }

    // コマンド実行
    // cmdParams->inOutData に "コマンド\t引数" の形でコマンドラインが格納されている
    // 結果は outParams で返す
    void ExecCmd(DecoderCommandParams* cmdParams, DecoderOutParams* outParams) {
        LOG_INFOH(_T("ENTER: data=%s"), cmdParams->inOutData);

        auto items = utils::split(cmdParams->inOutData, '\t');
        if (!items.empty()) {
            const auto& cmd = items[0];
            LOG_INFO(_T("cmd=%s"), cmd.c_str());
            if (cmd == _T("addHistEntry") && HISTORY_DIC) {
                // 履歴登録
                if (items.size() >= 2 && !items[1].empty()) {
                    HISTORY_DIC->AddNewEntryAnyway(to_mstr(items[1]));
                } else {
                    HISTORY_DIC->AddNewEntryAnyway(OUTPUT_STACK->GetLastJapaneseKey<MString>(32));
                }
            } else if (cmd == _T("saveHistoryDic") && BUSHU_ASSOC_DIC) {
                // 履歴辞書の保存
                if (HISTORY_DIC) HISTORY_DIC->WriteHistoryDic();
            } else if (cmd == _T("readBushuDic") && BUSHU_DIC) {
                // 部首合成辞書の再読み込み
                BushuDic::ReadBushuDic(SETTINGS->bushuFile);
            } else if (cmd == _T("saveBushuDic") && BUSHU_DIC) {
                // 部首合成辞書の保存
                BUSHU_DIC->WriteBushuDic();
            } else if (cmd == _T("addBushuEntry") && BUSHU_DIC && items.size() >= 2 && !items[1].empty()) {
                // 部首合成エントリの追加
                BUSHU_DIC->AddBushuEntry(items[1]);
            } else if (cmd == _T("mergeBushuAssoc") && BUSHU_ASSOC_DIC) {
                // 部首連想辞書マージ
                BushuAssocDic::MergeBushuAssocDic(SETTINGS->bushuAssocFile);
            } else if (cmd == _T("mergeBushuAssocEntry") && BUSHU_ASSOC_DIC) {
                // 部首連想エントリマージ
                if (items.size() >= 2 && !items[1].empty()) {
                    BUSHU_ASSOC_DIC->MergeEntry(items[1]);
                }
            } else if (cmd == _T("saveBushuAssocDic") && BUSHU_ASSOC_DIC) {
                // 部首連想辞書の保存
                BUSHU_ASSOC_DIC->WriteBushuAssocDic();
            } else if (cmd == _T("addMazegakiEntry")) {
                LOG_DEBUGH(_T("addMazegakiEntry: %s"), items.size() >= 2 && !items[1].empty() ? items[1].c_str() : _T("none"));
                if (MAZEGAKI_DIC && items.size() >= 2 && !items[1].empty()) {
                    // 交ぜ書きエントリの追加
                    MAZEGAKI_DIC->AddMazeDicEntry(items[1], true);
                }
            } else if (cmd == _T("saveMazegakiDic") && BUSHU_ASSOC_DIC) {
                // 交ぜ書き辞書の保存
                if (MAZEGAKI_DIC) MAZEGAKI_DIC->WriteMazegakiDic();
            } else if (cmd == _T("showStrokeHelp") && items.size() >= 2 && !items[1].empty() && STROKE_HELP) {
                // ストロークヘルプの表示
                OutParams = outParams;
                makeStrokeHelp(items[1]);
            } else if (cmd == _T("makeExtraCharsStrokePositionTable")) {
                // 外字(左→左または右→右でどちらかに数字キーを含むもの)を集めたストローク表を作成する
                VkbTableMaker::MakeExtraCharsStrokePositionTable(outParams->faceStrings);
            } else if (cmd == _T("makeStrokeKeysTable") && items.size() >= 2 && !items[1].empty()) {
                // 指定の文字配列をストロークキー配列に変換
                VkbTableMaker::MakeStrokeKeysTable(outParams->faceStrings, items[1].c_str());
            } else if (cmd == _T("reorderByFirstStrokePosition") && items.size() >= 2 && !items[1].empty()) {
                // 指定の文字配列を第1ストロークの位置に従って並べかえる
                VkbTableMaker::ReorderByFirstStrokePosition(outParams->faceStrings, items[1].c_str());
            } else if (cmd == _T("makeHiraganaTable")) {
                // ひらがな50音図の作成
                makeHiraganaTable(outParams);
            } else if (cmd == _T("makeKatakanaTable")) {
                // カタカナ50音図の作成
                makeKatakanaTable(outParams);
            } else if (cmd == _T("reloadSettings")) {
                // 設定の再読み込み
                if (items.size() > 1 && !items[1].empty()) reloadSettings(items[1]);
            } else if (cmd == _T("saveDictFiles")) {
                // ファイル保存
                SaveDicts();
            } else if (cmd == _T("setBackspaceBlocker")) {
                // Backspace Blocker のセット
                setBackspaceBlocker();
            }
        }
    }

    // HOTKEY処理
    void HandleHotkey(int keyId, DecoderOutParams* params) {
        LOG_INFOH(_T("\nENTER: keyId=%xH(%d=%s)"), keyId, keyId, HOTKEY_TO_CHARS->GetHotkeyNameFromId(keyId));
        OutParams = params;
        initializeOutParams();

        if (startState == 0) return;

        // 各種状態を初期化してから
        STATE_COMMON->ClearStateInfo();
        STATE_COMMON->IncrementTotalHotKeyCount();
        STATE_COMMON->CountSameHotKey(keyId);
        // HotKey処理を呼ぶ
        startState->HandleHotkey(keyId);

        LOG_DEBUGH(_T("OUTPUT: outString=\"%s\", origString=\"%s\", flags=%x, numBS=%d"), \
            MAKE_WPTR(STATE_COMMON->OutString()), MAKE_WPTR(STATE_COMMON->OrigString()), STATE_COMMON->GetResultFlags(), STATE_COMMON->GetBackspaceNum());

        // アクティブウィンドウへの送出文字列
        size_t maxLen = utils::array_length(OutParams->outString);
        size_t cpyLen = copy_mstr(STATE_COMMON->OutString(), OutParams->outString, maxLen - 1);
        OutParams->resultFlags = STATE_COMMON->GetResultFlags();
        if (startState->ChainLength() > 1) {
            // 始状態に何か他の状態が後続していれば、Ctrl-Hなどの特殊キーをHOTKEY化する
            OutParams->resultFlags |= (UINT32)ResultFlags::SpecialHotkeyRequired;
        }
        OutParams->numBackSpaces = STATE_COMMON->GetBackspaceNum();

        // 出力履歴に BackSpaces を反映
        OUTPUT_STACK->pop(OutParams->numBackSpaces);
        // 出力文字列を履歴に反映 (全角の＊と？は半角に変換しておく⇒ワイルドカードを含む交ぜ書き変換で使う)
        OUTPUT_STACK->push(utils::convert_star_and_question_to_hankaku(OutParams->outString));
        if (Logger::IsDebugEnabled()) {
            //wstring stack = std::regex_replace(to_wstr(OUTPUT_STACK->backStringFull(10)), std::wregex(_T("\n")), _T("|"));
            LOG_DEBUGH(_T("outStack=%s"), OUTPUT_STACK->OutputStackBackStrForDebug(10).c_str());
        }
        // 出力履歴に BackSpaceStopper を反映
        if (STATE_COMMON->IsAppendBackspaceStopper()) { OUTPUT_STACK->pushNewLine(); }
        // 出力履歴に HistoryBlock を反映
        if (STATE_COMMON->IsSetHistoryBlockFlag()) {
            OUTPUT_STACK->setFlag(OutputStack::FLAG_BLOCK_HIST);
            LOG_DEBUGH(_T("OUTPUT_STACK->setFlag(OutputStack::FLAG_BLOCK_HIST): %s"), MAKE_WPTR(OUTPUT_STACK->backStringWithFlagUpto(20)));
        }

        int strokeTableChainLen = startState->StrokeTableChainLength();
        LOG_DEBUGH(_T("strokeTableChainLen=%d"), strokeTableChainLen);
        STATE_COMMON->SetStrokeCount(max(strokeTableChainLen - 1, 0));
        if (strokeTableChainLen >= 2) STATE_COMMON->SetWaiting2ndStroke();
        LOG_DEBUGH(_T("STATE_COMMON->StrokeCount=%d"), STATE_COMMON->GetStrokeCount());

        // 最終的な出力履歴が整ったところで呼び出される処理
        if (!STATE_COMMON->IsOutStringProcDone() && !STATE_COMMON->IsWaiting2ndStroke()) startState->DoOutStringProcChain();

        // ヘルプや候補文字列
        setHelpOrCandidates();

        if (Logger::IsInfoHEnabled()) {
            //wstring stack = std::regex_replace(to_wstr(OUTPUT_STACK->OutputStackBackStr(10)), std::wregex(_T("\n")), _T("|"));
            LOG_INFOH(_T("LEAVE: states=%s (len=%d), flags=%x, expKey=%d, layout=%d, numBS=%d, outLength=%d, stack=%s"),
                startState->JoinedName().c_str(), startState->ChainLength(), STATE_COMMON->GetResultFlags(), STATE_COMMON->GetNextExpectedKeyType(),
                STATE_COMMON->GetLayoutInt(), STATE_COMMON->GetBackspaceNum(), cpyLen, OUTPUT_STACK->OutputStackBackStrForDebug(10).c_str());
        }
    }

    // BackspaceStopper や HistoryBlock をセット
    void setBackspaceBlocker() {
        STATE_COMMON->SetBothHistoryBlockFlag();
        OUTPUT_STACK->pushNewLine();
        OUTPUT_STACK->setFlag(OutputStack::FLAG_BLOCK_HIST);
    }

    // ヘルプや候補文字列
    void setHelpOrCandidates() {
        //if (startState->StrokeTableChainLength() >= 2) STATE_COMMON->SetWaiting2ndStroke();
        LOG_DEBUG(_T("layout=%d, nextExp=%d"), STATE_COMMON->GetLayoutInt(), (int)STATE_COMMON->GetNextExpectedKeyType());
        OutParams->nextExpectedKeyType = (int)STATE_COMMON->GetNextExpectedKeyType();
        OutParams->strokeCount = (int)STATE_COMMON->GetStrokeCount();
        OutParams->nextSelectPos = (int)STATE_COMMON->GetNextSelectPos();
        OutParams->layout = STATE_COMMON->GetLayoutInt();
        copyToCenterString();
        mchar_t lastChar = copyToTopString();

        switch (STATE_COMMON->GetLayout()) {
        case VkbLayout::Normal:
        case VkbLayout::StrokeHelp:
            LOG_DEBUG(_T("Normal or StrokeHelp"));
        {
            //copyToTopString();
            //copyToCenterString();
            mchar_t* faces = STATE_COMMON->GetFaces();
            size_t numFaces = STATE_COMMON->FacesSize();
            for (size_t i = 0; i < numFaces; ++i) {
                //OutParams->faceStrings[i] = STATE_COMMON->faces[i];
                set_facestr(faces[i], OutParams->faceStrings + i * 2);
            }
        }
            break;
        case VkbLayout::Vertical:
        case VkbLayout::Horizontal:
            LOG_DEBUG(_T("Vertical or Horizontal"));
        {
            //copyToTopString();
            //copyToCenterString();
            size_t pos = 0;
            size_t maxlen = utils::array_length(OutParams->faceStrings);
            for (auto& s : STATE_COMMON->LongVkeyCandidates()) {
                size_t i = 0;
                for (; i < s.size(); ++i) {
                    if (i >= LONG_VKEY_CHAR_SIZE - 1 || pos + i >= maxlen) break;
                    OutParams->faceStrings[pos + i] = s[i];
                }
                OutParams->faceStrings[pos + i] = 0;
                pos += LONG_VKEY_CHAR_SIZE;
                if (pos >= maxlen) break;
            }
            while (pos < maxlen) {
                OutParams->faceStrings[pos] = 0;
                pos += LONG_VKEY_CHAR_SIZE;
            }
        }
            break;
        case VkbLayout::BushuCompHelp:
            // 部首合成ヘルプが要求されている場合
            LOG_DEBUG(_T("BushuCompHelp"));
            //copyToTopString();
            if ((STATE_COMMON->GetBackspaceNum() > 0 && STATE_COMMON->OutString().size() == 1)) {
                if (BUSHU_DIC && BUSHU_DIC->CopyBushuCompHelpToVkbFaces(lastChar, OutParams->faceStrings, LONG_VKEY_CHAR_SIZE, LONG_VKEY_NUM, false)) {
                    OutParams->layout = (int)VkbLayout::BushuCompHelp;
                    OutParams->centerString[0] = (wchar_t)lastChar;
                    OutParams->centerString[1] = 0;
                }
            }
            break;
        default:
            LOG_DEBUG(_T("default"));
            break;
        }
    }

    // 出力構造体の初期化
    void initializeOutParams() {
        if (OutParams) {
            OutParams->numBackSpaces = 0;
            OutParams->resultFlags = 0;
            OutParams->nextExpectedKeyType = 0;
            OutParams->strokeCount = 0;
            OutParams->outString[0] = 0;
            OutParams->layout = (int)VkbLayout::None;
            OutParams->centerString[0] = 0;
            OutParams->topString[0] = 0;
            for (size_t i = 0; i < utils::array_length(OutParams->faceStrings); ++i) {
                OutParams->faceStrings[i] = 0;
            }
        }
    }

    // 中央鍵盤への文字列コピー
    void copyToCenterString() {
        copyToCenterString(to_wstr(startState->JoinModeMarker()) + STATE_COMMON->CenterString());
    }

    void copyToCenterString(wchar_t ch) {
        LOG_DEBUG(_T("ENTER: centerString=%c"), ch);
        OutParams->centerString[0] = ch;
        OutParams->centerString[1] = 0;
        LOG_DEBUG(_T("LEAVE"));
    }

    void copyToCenterString(const wstring& s) {
        LOG_DEBUG(_T("ENTER: centerString=%s"), s.c_str());
        size_t i = 0;
        size_t maxlen = utils::array_length(OutParams->centerString) - 1;
        for (; i < maxlen; ++i) {
            if (i >= s.size() || s[i] == 0) break;
            OutParams->centerString[i] = s[i];
        }
        OutParams->centerString[i] = 0;
        LOG_DEBUG(_T("LEAVE"));
    }

    mchar_t copyToTopString() {
        LOG_DEBUGH(_T("\nENTER: outStackStr=%s"), MAKE_WPTR(OUTPUT_STACK->OutputStackBackStrUpto(32)));
        size_t origLen = 0;
        // 打鍵途中なら打鍵中のキー文字列も表示する
        if (STATE_COMMON->IsWaiting2ndStroke()) origLen = STATE_COMMON->OrigString().size();
        size_t topLen = utils::array_length(OutParams->topString);
        LOG_DEBUGH(_T("origLen=%d"), origLen);
        auto s = OUTPUT_STACK->OutputStackBackStrWithFlagUpto(topLen - origLen - 1);        // ブロッカーを反映した文字列を取得
        LOG_DEBUGH(_T("OutputStackBackStrWithFlagUpto(%d)=%s"), (topLen - origLen - 1), MAKE_WPTR(s));
        size_t pos = copy_mstr(s, OutParams->topString, topLen);
        if (origLen > 0) copy_mstr(STATE_COMMON->OrigString(), OutParams->topString + pos, origLen);
        mchar_t lastChar = origLen == 0 ? OUTPUT_STACK->OutputStackLastChar() : 0;
        LOG_DEBUGH(_T("LEAVE: OutParams->topString=%s, lastChar=%s"), OutParams->topString, MAKE_WPTR(lastChar));
        return lastChar;
    }

    void makeStrokeHelp(const wstring& ws) {
        LOG_DEBUG(_T("CALLED: %s"), ws.c_str());
        auto ms = to_mstr(ws);
        OutParams->layout = (int)VkbLayout::StrokeHelp;
        copyToCenterString(ws);
        clearKeyFaces();
        if (!ms.empty()) {
            if (!STROKE_HELP->copyStrokeHelpToVkbFacesOutParams(ms[0], OutParams->faceStrings)) {
                if (BUSHU_DIC) {
                    if (BUSHU_DIC->CopyBushuCompHelpToVkbFaces(ms[0], OutParams->faceStrings, LONG_VKEY_CHAR_SIZE, LONG_VKEY_NUM, true)) {
                        OutParams->layout = (int)VkbLayout::BushuCompHelp;
                    }
                }
            }
        }
    }

    // ひらがな50音図配列を作成する (あかさたなはまやらわ、ぁがざだばぱゃ)
    void makeHiraganaTable(DecoderOutParams* outParam) {
        VkbTableMaker::MakeVkbHiraganaTable(outParam->faceStrings);
    }

    // カタカナ50音図配列を作成する (アカサタナハマヤラワ、ァガザダバパャヮ)
    void makeKatakanaTable(DecoderOutParams* outParam) {
        VkbTableMaker::MakeVkbKatakanaTable(outParam->faceStrings);
    }

    void makeBushuCompHelp(wchar_t ch) {
        LOG_DEBUG(_T("ENTER: %c"), ch);
        copyToCenterString(ch);
        clearKeyFaces();
        if (BUSHU_DIC) {
            if (BUSHU_DIC->CopyBushuCompHelpToVkbFaces(ch, OutParams->faceStrings, LONG_VKEY_CHAR_SIZE, LONG_VKEY_NUM, true)) {
                OutParams->layout = (int)VkbLayout::BushuCompHelp;
            }
        }
        LOG_DEBUG(_T("LEAVE: layout=%d"), OutParams->layout);
    }

    void clearKeyFaces() {
        for (size_t i = 0; i < utils::array_length(OutParams->faceStrings); ++i) {
            OutParams->faceStrings[i] = 0;
        }
    }

}; // class DecoderImpl

DEFINE_CLASS_LOGGER(DecoderImpl);

//-------------------------------------------------------------------
// 各種 Singleton の実体定義

// KanchokuIni::Singleton
std::unique_ptr<KanchokuIni> KanchokuIni::Singleton;

// Settings::Singleton
std::unique_ptr<Settings> Settings::Singleton;

//-------------------------------------------------------------------
// C#側から呼ばれる関数群
//-------------------------------------------------------------------
namespace {
    DEFINE_LOCAL_LOGGER(Decoder);
}

// デコーダを生成してそのポインタを C# 側に返す
// 引数: LogLevel (0 ならログ出力を抑制する。3: INFO, 4:DEBUG, 5:TRACE)
void* CreateDecoder(int logLevel) {
    try {
        Logger::LogLevel = logLevel;
        Logger::LogFilename = _T("kw-uni.log");
        return new DecoderImpl();
    }
    catch (tstring msg) {
        LOG_ERROR(msg);
        return 0;
    }
    catch (...) {
        LOG_ERROR(_T("Some exception caught"));
        return 0;
    }
}

namespace {
    template<typename F>
    int invokeDecoderMethod(F method_call, DecoderCommandParams* params) {
        int result = 0;
        try {
            ERROR_HANDLER->Clear();
            method_call();
            result = ERROR_HANDLER->GetErrorLevel();
            if (result > 0 && params) wcscpy_s(params->inOutData, ERROR_HANDLER->GetErrorMsg().c_str());
            return result;
        }
        catch (ErrorHandler* pErr) {
            if (pErr) {
                LOG_ERROR(pErr->GetErrorMsg());
                if (params) wcscpy_s(params->inOutData, ERROR_HANDLER->GetErrorMsg().c_str());
                return pErr->GetErrorLevel();
            }
            return -1;
        }
        catch (tstring msg) {
            LOG_ERROR(msg);
            return -1;
        }
        catch (...) {
            LOG_ERROR(_T("Some exception caught"));
            return -1;
        }
    }
}

// デコーダを初期化する
// 引数: 初期化パラメータ
int InitializeDecoder(void* pDecoder, DecoderCommandParams* cmdParams) {
    LOG_INFO(_T("\n======== kw-uni START ========"));
    LOG_INFO(_T("LogLevel=%d, inOutData=%s"), Logger::LogLevel, cmdParams->inOutData);

    // エラーハンドラの生成
    ErrorHandler::CreateSingleton();

    auto method_call = [pDecoder, cmdParams]() { ((Decoder*)pDecoder)->Initialize(cmdParams); };
    return invokeDecoderMethod(method_call, cmdParams);
}

// デコーダを終了する
int FinalizeDecoder(void* pDecoder) {
    auto method_call = [pDecoder]() {
        Decoder* p = (Decoder*)pDecoder;
        p->Destroy();
        delete p;
        LOG_INFO(_T("======== kw-uni TERMINATED ========\n"));
    };
    return invokeDecoderMethod(method_call, nullptr);
}

// デコーダをリセットする
int ResetDecoder(void* pDecoder) {
    auto method_call = [pDecoder]() { ((Decoder*)pDecoder)->Reset(); };
    return invokeDecoderMethod(method_call, nullptr);
}

// デコーダが扱う辞書を保存する
int SaveDictsDecoder(void* pDecoder) {
    auto method_call = [pDecoder]() { ((Decoder*)pDecoder)->SaveDicts(); };
    return invokeDecoderMethod(method_call, nullptr);
}

/// <summary> Decoder に外字テーブルの作成を要求する </summary>
int MakeInitialVkbTableDecoder(void* pDecoder, DecoderOutParams* table) {
    auto method_call = [pDecoder, table]() { ((Decoder*)pDecoder)->MakeInitialVkbTable(table); };
    return invokeDecoderMethod(method_call, nullptr);
}

// HOTKEYハンドラ
// 引数: keyId = HOTKEY ID
int HandleHotkeyDecoder(void* pDecoder, int keyId, DecoderOutParams* params) {
    auto method_call = [pDecoder, keyId, params]() { ((Decoder*)pDecoder)->HandleHotkey(keyId, params); };
    return invokeDecoderMethod(method_call, nullptr);
}

// デコーダにコマンドを送って実行させる
// 引数: コマンドパラメータ, 出力用パラメータ
int ExecCmdDecoder(void* pDecoder, DecoderCommandParams* cmdParams, DecoderOutParams* outParams) {
    auto method_call = [pDecoder, cmdParams, outParams]() { ((Decoder*)pDecoder)->ExecCmd(cmdParams, outParams); };
    return invokeDecoderMethod(method_call, cmdParams);
}

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
#include "deckey_id_defs.h"
#include "KeysAndChars/DeckeyToChars.h"
#include "KeysAndChars/VkbTableMaker.h"
#include "KeysAndChars/MyPrevChar.h"
#include "KeysAndChars/Zenkaku.h"
#include "KeysAndChars/Katakana.h"
#include "KeysAndChars/Eisu.h"
#include "KeysAndChars/RomanToKatakana.h"
#include "ErrorHandler.h"
#include "Settings.h"
#include "State.h"
#include "StartNode.h"
#include "FunctionNodeManager.h"
#include "EasyChars.h"
#include "StrokeTable.h"
#include "StrokeHelp.h"

#include "BushuComp/BushuComp.h"
#include "BushuComp/BushuDic.h"
#include "BushuComp/BushuAssoc.h"
#include "BushuComp/BushuAssocDic.h"
#include "History/History.h"
#include "History/HistoryDic.h"
#include "Mazegaki/Mazegaki.h"
#include "Mazegaki/MazegakiDic.h"

#define _LOG_DEBUGH_FLAG true
#if 0 || defined(_DEBUG)
#define IS_LOG_DEBUGH_ENABLED true
#define _DEBUG_SENT(x) x
#define _DEBUG_FLAG(x) (x)
#define LOG_INFO LOG_INFOH
#define LOG_DEBUG LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH_COND LOG_INFOH_COND
#endif

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

    // UI側から送られてきた設定情報の保存
    String decoderSettings;

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
    void initializeDecoder() {
        LOG_INFO(_T("ENTER"));

        // 状態の共有情報生成
        StateCommonInfo::CreateSingleton();

        // 出力された文字列を保持するスタックを生成
        OutputStack::CreateSingleton();

        // 機能ノードビルダーの登録
        FunctionNodeManager::AddFunctionNodeBuilders();

        // GUIから送られてきた settings を読み込んで Settings::Singleton を構築
        createSettings();

        // settings の再ロードとストローク木の再構築
        reloadSettings(false);

        // 始状態
        startNode.reset(new StartNode());
        startState.reset(startNode->CreateState());

        // 履歴入力機能を生成して常駐させる
        HistoryResidentNode::CreateSingleton();
        startState->ChainAndStayResident(HISTORY_STAY_NODE.get());
        // 必要があれば、ここにその他の常駐機能を追加する
       
        // PrevCharNode - 直前キー文字を返すノードのSingleton生成
        PrevCharNode::CreateSingleton();

        // 部首合成ノードのSingleton生成
        BushuCompNode::CreateSingleton();

        // 直接連想変換ノードのSingleton生成
        BushuAssocExNode::CreateSingleton();

        // 全角変換ノードのSingleton生成
        ZenkakuNode::CreateSingleton();

        // カタカナ変換ノードのSingleton生成
        KatakanaNode::CreateSingleton();

        // 英数入力ノードのSingleton生成
        EisuNode::CreateSingleton();

        // ストロークヘルプを求めておく
        StrokeHelp::GatherStrokeHelp();
        // 部首合成の部品について、ストローク可能文字か否かを設定しておく
        if (BUSHU_DIC) BUSHU_DIC->MakeStrokableMap();

        LOG_INFO(_T("LEAVE"));
    }

    // 終了
    void Destroy() override {
        LOG_INFO(_T("CALLED"));
    }

    // settings の事前受け取り
    void presendSettings(bool bFirst, const String & settings) {
        LOG_INFO(_T("ENTER: {}: settings={}"), bFirst?_T("FIRST"):_T("NEXT"), settings);
        if (bFirst) {
            decoderSettings = settings;
        } else {
            decoderSettings += settings;
        }
    }
    
    // GUIから送られてきた settings を読み込んで Settings::Singleton を構築
    void createSettings() {
        LOG_INFO(_T("ENTER"));

        SETTINGS.reset(new Settings);

        // settings のロード
        loadSettings(decoderSettings);

        LOG_INFO(_T("LEAVE"));
    }
    
    // settings の再ロードとストローク木の再構築
    void reloadSettings(bool bPreLoad = true) {
        LOG_INFO(_T("ENTER"));
       
        // settings の事前ロード
        if (bPreLoad) loadSettings(decoderSettings);

        // Deckey から文字への変換インスタンスの構築
        createDeckeyToCharsInstance();

        // ローマ字定義ファイルのロード
        RomanToKatakana::ReadRomanDefFile(_T("kwroman.def.txt"));

        // ストローク木の構築
        createStrokeTrees();

        // settings の再ロード
        loadSettings(decoderSettings);

        // 簡易打鍵文字を集める
        EasyChars::GatherEasyChars();

        LOG_INFO(_T("LEAVE"));
    }

    // settings のロード
    void loadSettings(const String & settings) {
        LOG_INFO(_T("ENTER: settings={}"), settings);
        
        std::map<String, String> key_vals;

        for (auto item : utils::split(settings, '\n')) {
            LOG_INFO(_T("item={}"), item);
            auto pair = utils::split(item, '=');
            if (pair.size() == 2) {
                key_vals[pair[0]] = pair[1];
            }
        }

        int logLevel = utils::strToInt(utils::safe_get(key_vals, String(_T("logLevel"))));
        if (logLevel > 0) Reporting::Logger::LogLevel = logLevel;

        SETTINGS->SetValues(key_vals);

        LOG_INFO(_T("LEAVE"));
    }

    // Deckey から文字への変換インスタンスの構築
    void createDeckeyToCharsInstance() {

        auto filePath = SETTINGS->charsDefFile;
        LOG_INFOH(_T("charsDefFile={}"), filePath);
        //if (filePath.empty()) {
        //    ErrorHandler::Error(_T("「charsDefFile=(ファイル名)」の設定がまちがっているようです"));
        //}
        DeckeyToChars::CreateSingleton(filePath);
    }

    // テーブルファイルを読み込んでストローク木を作成する
    void createStrokeTrees(bool bForceSecondary = false) {
        // テーブルファイル名
        if (SETTINGS->tableFile.empty()) {
            // エラー
            ERROR_HANDLER->Error(_T("「tableFile=(ファイル名)」の設定がまちがっているようです"));
        } else {
            // 主テーブルファイルの構築
            createStrokeTree(utils::joinPath(SETTINGS->rootDir, _T("tmp\\tableFile1.tbl")), [](const String& file, std::vector<String>& lines) {StrokeTableNode::CreateStrokeTree(file, lines);});

            if (bForceSecondary || !SETTINGS->tableFile2.empty()) {
                // 副テーブルファイルの構築
                createStrokeTree(utils::joinPath(SETTINGS->rootDir, _T("tmp\\tableFile2.tbl")), [](const String& file, std::vector<String>& lines) {StrokeTableNode::CreateStrokeTree2(file, lines);});
            }

            if (!SETTINGS->tableFile3.empty()) {
                // 第3テーブルファイルの構築
                createStrokeTree(utils::joinPath(SETTINGS->rootDir, _T("tmp\\tableFile3.tbl")), [](const String& file, std::vector<String>& lines) {StrokeTableNode::CreateStrokeTree3(file, lines);});
            }
        }
    }

    // テーブルファイルを読み込んでストローク木を作成する
    void createStrokeTree(const String& tableFile, void(*treeCreator)(const String&, std::vector<String>&)) {
        LOG_INFO(_T("ENTER: tableFile={}"), tableFile);

        utils::IfstreamReader reader(tableFile);
        if (reader.success()) {
            //auto lines = utils::IfstreamReader(tableFile).getAllLines();
            auto lines = reader.getAllLines();
            // ストロークノード木の構築
            treeCreator(tableFile, lines);
            LOG_INFO(_T("close table file: {}"), tableFile);
        } else {
            // エラー
            LOG_ERROR(_T("Can't read table file: {}"), tableFile);
            ERROR_HANDLER->Error(std::format(_T("テーブルファイル({})が開けません"), tableFile));
        }

        LOG_INFO(_T("LEAVE"));
    }

    // 初期打鍵表(下端機能キー以外は空白)の作成
    void MakeInitialVkbTable(DecoderOutParams* outParams) {
        OutParams = outParams;
        VkbTableMaker::MakeInitialVkbTable(outParams->faceStrings);
    }

    // デコーダ状態のリセット (Decoder が ON になったときに呼ばれる)
    void Reset() override {
        deleteRemainingState();
        STATE_COMMON->ClearAllStateInfo();
        OUTPUT_STACK->pushNewLine();    // 履歴ブロッカーとして改行を追加
        if (startState) startState->Reactivate();
        if (MAZEGAKI_INFO) MAZEGAKI_INFO->Initialize(false);
        if (startState) {
            LOG_INFOH(_T("LEAVE: states={} (len={}), flags={:x}, numBS={}, outLength={}, stack={}\n"),
                startState->JoinedName(), startState->ChainLength(), STATE_COMMON->GetResultFlags(),
                STATE_COMMON->GetBackspaceNum(), STATE_COMMON->OutString().size(), OUTPUT_STACK->OutputStackBackStrForDebug(5));
        }
    }

    // 居残っている一時状態の削除
    void deleteRemainingState() {
        if (startState) {
            LOG_INFOH(_T("\nENTER: states={} (len={}), flags={:x}, numBS={}, outLength={}, stack={}"),
                startState->JoinedName(), startState->ChainLength(), STATE_COMMON->GetResultFlags(),
                STATE_COMMON->GetBackspaceNum(), STATE_COMMON->OutString().size(), OUTPUT_STACK->OutputStackBackStrForDebug(5));
            startState->DeleteRemainingState();
        }
        STATE_COMMON->ClearStateInfo();
    }

    // 履歴のコミットと初期化
    void commitHistory() {
        HISTORY_STAY_STATE->commitHistory();
    }

    // デコーダが扱う辞書を保存する
    void SaveDicts() override {
        LOG_INFO(_T("CALLED"));
        if (BUSHU_DIC) {
            BUSHU_DIC->WriteBushuDic();
            BUSHU_DIC->WriteAutoBushuDic();
        }
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
    void ExecCmd(DecoderCommandParams* cmdParams, DecoderOutParams* outParams) override {
        if (Reporting::Logger::IsInfoHEnabled()) {
            size_t len = wcslen(cmdParams->inOutData);
            String data(cmdParams->inOutData, 200);
            LOG_INFOH(_T("ENTER: paramLen={}, data={}"), len, data);
        }

        OutParams = outParams;

        auto items = utils::split(cmdParams->inOutData, '\t');
        if (!items.empty()) {
            const auto& cmd = items[0];
            LOG_INFO(_T("cmd={}, items.size()={}"), cmd, items.size());
            if (cmd == _T("presendSettings")) {
                // 設定の先行送出
                if (items.size() > 2 && !items[2].empty()) presendSettings(utils::toLower(items[1]) == _T("true"), items[2]);
            } else if (cmd == _T("initializeDecoder")) {
                initializeDecoder();
            } else if (cmd == _T("reloadSettings")) {
                // 設定の再読み込み
                //if (items.size() > 1 && !items[1].empty()) reloadSettings(items[1]);
                reloadSettings();
            } else if (cmd == _T("addHistEntry") && HISTORY_DIC) {
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
            } else if (cmd == _T("addAutoBushuEntry") && BUSHU_DIC && items.size() >= 2 && !items[1].empty()) {
                // 自動部首合成エントリの追加
                BUSHU_DIC->AddAutoBushuEntry(items[1]);
            } else if (cmd == _T("readAutoBushuDic") && BUSHU_DIC) {
                // 自動部首合成辞書の再読み込み
                BushuDic::ReadAutoBushuDic(SETTINGS->autoBushuFile);
            } else if (cmd == _T("saveAutoBushuDic") && BUSHU_DIC) {
                // 自動部首合成辞書の保存
                BUSHU_DIC->WriteAutoBushuDic();
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
                LOG_DEBUGH(_T("addMazegakiEntry: {}"), items.size() >= 2 && !items[1].empty() ? items[1] : _T("none"));
                if (MAZEGAKI_DIC && items.size() >= 2 && !items[1].empty()) {
                    // 交ぜ書きエントリの追加
                    MAZEGAKI_DIC->AddMazeDicEntry(items[1], true, false);
                }
            } else if (cmd == _T("readMazegakiDic") && BUSHU_ASSOC_DIC) {
                // 交ぜ書き辞書の読み込み
                if (MAZEGAKI_DIC) MAZEGAKI_DIC->ReadMazegakiDic(items[1]);
            } else if (cmd == _T("saveMazegakiDic") && BUSHU_ASSOC_DIC) {
                // 交ぜ書き辞書の保存
                if (MAZEGAKI_DIC) MAZEGAKI_DIC->WriteMazegakiDic();
            } else if (cmd == _T("showStrokeHelp") && STROKE_HELP) {
                // ストロークヘルプの表示
                //OutParams = outParams;
                makeStrokeHelp(items.size() > 1 ? items[1] : _T(""));
            } else if (cmd == _T("showBushuCompHelp") && BUSHU_DIC) {
                // 部首合成ヘルプの表示
                //OutParams = outParams;
                makeBushuCompHelp(items.size() > 1 ? items[1] : _T(""));
            } else if (cmd == _T("clearTailRomanStr")) {
                // 末尾のローマ字列を削除
                clearTailRomanStr();
            } else if (cmd == _T("clearTailHiraganaStr")) {
                // 末尾のひらがな列を削除
                clearTailHiraganaStr();
            } else if (cmd == _T("setHiraganaBlocker")) {
                // 末尾にひらがなブロッカーを設定
                setHiraganaBlocker();
            } else if (cmd == _T("makeExtraCharsStrokePositionTable") || cmd == _T("makeExtraCharsStrokePositionTable1")) {
                // 主テーブルの外字(左→左または右→右でどちらかに数字キーを含むもの)を集めたストローク表を作成する
                VkbTableMaker::MakeExtraCharsStrokePositionTable1(outParams->faceStrings);
            } else if (cmd == _T("makeExtraCharsStrokePositionTable2")) {
                // 副テーブルの外字(左→左または右→右でどちらかに数字キーを含むもの)を集めたストローク表を作成する
                VkbTableMaker::MakeExtraCharsStrokePositionTable2(outParams->faceStrings);
            } else if (cmd == _T("makeExtraCharsStrokePositionTable3")) {
                // 第3テーブルの外字(左→左または右→右でどちらかに数字キーを含むもの)を集めたストローク表を作成する
                VkbTableMaker::MakeExtraCharsStrokePositionTable3(outParams->faceStrings);
            } else if (cmd == _T("makeStrokePosition") || cmd == _T("makeStrokePosition1")) {
                // アンシフトキー文字配列をストロークの位置に従って並べる
                VkbTableMaker::MakeKeyCharsStrokePositionTable(outParams->faceStrings);
            } else if (cmd == _T("makeStrokePosition2")) {
                // 第2テーブルから、アンシフトキー文字配列をストロークの位置に従って並べる
                VkbTableMaker::MakeKeyCharsStrokePositionTable2(outParams->faceStrings);
            } else if (cmd == _T("makeStrokePosition3")) {
                // 第3テーブルから、アンシフトキー文字配列をストロークの位置に従って並べる
                VkbTableMaker::MakeKeyCharsStrokePositionTable3(outParams->faceStrings);
            } else if (cmd == _T("makeShiftStrokePosition1")) {
                // シフトキー文字配列をストロークの位置に従って並べる
                VkbTableMaker::MakeShiftPlaneKeyCharsStrokePositionTable1(outParams->faceStrings, 1);
            } else if (cmd == _T("makeShiftAStrokePosition1")) {
                // シフトA面のキー文字配列をストロークの位置に従って並べる
                VkbTableMaker::MakeShiftPlaneKeyCharsStrokePositionTable1(outParams->faceStrings, 2);
            } else if (cmd == _T("makeShiftBStrokePosition1")) {
                // シフトB面のキー文字配列をストロークの位置に従って並べる
                VkbTableMaker::MakeShiftPlaneKeyCharsStrokePositionTable1(outParams->faceStrings, 3);
            } else if (cmd == _T("makeShiftPlaneStrokePosition1")) {
                // 指定のシフト面のキー文字配列をストロークの位置に従って並べる
                size_t shiftPlane = items.size() >= 2 ? utils::strToInt(items[1]) : 0;
                VkbTableMaker::MakeShiftPlaneKeyCharsStrokePositionTable1(outParams->faceStrings, shiftPlane);
            } else if (cmd == _T("makeShiftPlaneStrokePosition2")) {
                // 副テーブルの指定のシフト面のキー文字配列をストロークの位置に従って並べる
                size_t shiftPlane = items.size() >= 2 ? utils::strToInt(items[1]) : 0;
                VkbTableMaker::MakeShiftPlaneKeyCharsStrokePositionTable2(outParams->faceStrings, shiftPlane);
            } else if (cmd == _T("makeShiftPlaneStrokePosition3")) {
                // 第3テーブルの指定のシフト面のキー文字配列をストロークの位置に従って並べる
                size_t shiftPlane = items.size() >= 2 ? utils::strToInt(items[1]) : 0;
                VkbTableMaker::MakeShiftPlaneKeyCharsStrokePositionTable3(outParams->faceStrings, shiftPlane);
            } else if (cmd == _T("makeComboStrokePosition")) {
                // シフトキー文字配列をストロークの位置に従って並べる
                VkbTableMaker::MakeCombinationKeyCharsStrokePositionTable(outParams->faceStrings);
            } else if (cmd == _T("makeStrokeKeysTable") && items.size() >= 2 && !items[1].empty()) {
                // 指定の文字配列をストロークキー配列に変換
                VkbTableMaker::MakeStrokeKeysTable(outParams->faceStrings, items[1]);
            } else if (cmd == _T("reorderByFirstStrokePosition") && items.size() >= 2 && !items[1].empty()) {
                // 指定の文字配列を第1ストロークの位置に従って並べかえる
                VkbTableMaker::ReorderByFirstStrokePosition(outParams->faceStrings, items[1], 0);
            } else if (cmd == _T("reorderByFirstStrokePosition1") && items.size() >= 2 && !items[1].empty()) {
                // 指定の文字配列を第1ストロークの位置に従って並べかえる
                VkbTableMaker::ReorderByFirstStrokePosition(outParams->faceStrings, items[1], 1);
            } else if (cmd == _T("reorderByFirstStrokePosition2") && items.size() >= 2 && !items[1].empty()) {
                // 指定の文字配列を第1ストロークの位置に従って並べかえる
                VkbTableMaker::ReorderByFirstStrokePosition(outParams->faceStrings, items[1], 2);
            } else if (cmd == _T("makeHiraganaTable")) {
                // ひらがな50音図の作成
                makeHiraganaTable(outParams);
            } else if (cmd == _T("makeKatakanaTable")) {
                // カタカナ50音図の作成
                makeKatakanaTable(outParams);
            } else if (cmd == _T("makeNextStrokeTable")) {
                // 指定キーに対する次打鍵テーブルの作成
                if (items.size() > 1 && !items[1].empty()) {
                    //OutParams = outParams;
                    int deckey1 = utils::strToInt(items[1], -1);
                    int deckey2 = items.size() > 2 && !items[2].empty() ? utils::strToInt(items[2], -1) : -1;
                    makeNextStrokeTable(deckey1, deckey2);
                }
            } else if (cmd == _T("getCharsOrderedByDeckey")) {
                // Deckey順に並んだ通常文字列とシフト文字列を返す
                getCharsOrderedByDeckey(outParams);
            } else if (cmd == _T("createStrokeTrees")) {
                // ストローク木の再構築
                createStrokeTrees(items.size() >= 2 && !items[1].empty());
            } else if (cmd == _T("saveDictFiles")) {
                // ファイル保存
                SaveDicts();
            } else if (cmd == _T("setBackspaceBlocker")) {
                // Backspace Blocker のセット
                setBackspaceBlocker();
            } else if (cmd == _T("SaveRomanStrokeTable")) {
                // ローマ字テーブルを作成してファイルに書き出す
                VkbTableMaker::SaveRomanStrokeTable(items.size() >= 2 ? items[1] : 0, items.size() >= 3 ? items[2] : 0);
            } else if (cmd == _T("SaveEelllJsTable")) {
                // eelll/JS用テーブルを作成してファイルに書き出す
                VkbTableMaker::SaveEelllJsTable();
            } else if (cmd == _T("SaveDumpTable")) {
                // デバッグ用テーブルを作成してファイルに書き出す
                VkbTableMaker::SaveDumpTable();
            } else if (cmd == _T("exchangeCodeTable")) {
                // 主・副テーブルを切り替える
                outParams->strokeTableNum = StrokeTableNode::ExchangeStrokeTable();
            } else if (cmd == _T("useCodeTable1")) {
                // 主テーブルに切り替える
                outParams->strokeTableNum = StrokeTableNode::UseStrokeTable1();
            } else if (cmd == _T("useCodeTable2")) {
                // 副テーブルに切り替える
                outParams->strokeTableNum = StrokeTableNode::UseStrokeTable2();
            } else if (cmd == _T("useCodeTable3")) {
                // 第3テーブルに切り替える
                outParams->strokeTableNum = StrokeTableNode::UseStrokeTable3();
            } else if (cmd == _T("isKatakanaMode")) {
                // カタカナモードか
                if (STATE_COMMON->FindRunningState(_T("KatakanaState"))) outParams->resultFlags |=  (UINT32)ResultFlags::CurrentModeIsKatakana;
            } else if (cmd == _T("readBushuAssoc")) {
                // 連想辞書から定義文字列を読み出してくる
                readBushuAssoc(items[1], outParams->faceStrings);
            } else if (cmd == _T("updateStrokeNodes")) {
                // 後から部分的にストローク定義を解析してストローク木に差し込む
                updateStrokeNodes(items[1]);
            } else if (cmd == _T("cancelPreRewrite")) {
                // 前置書き換えをキャンセルする
                OUTPUT_STACK->cancelRewritable();
            } else if (cmd == _T("setAutoHistSearchEnabled")) {
                // 自動履歴検索のON/OFF
                SETTINGS->autoHistSearchEnabled = (items.size() >= 2 && items[1] == _T("true"));
            } else if (cmd == _T("setKanaTrainingMode")) {
                // かな入力練習モードのON/OFF
                SETTINGS->kanaTrainingMode = (items.size() >= 2 && items[1] == _T("true"));
            } else if (cmd == _T("deleteRemainingState")) {
                // 居残っている一時状態の削除
                deleteRemainingState();
            } else if (cmd == _T("commitHistory")) {
                // 履歴のコミットと初期化
                commitHistory();
            } else if (cmd == _T("closeLogger")) {
                Reporting::Logger::Close();
            }
        }
    }

    // DECKEY処理
    void HandleDeckey(int keyId, mchar_t targetChar, int intputFlags, DecoderOutParams* outParams) override {
        bool decodeKeyboardChar = (intputFlags & (int)InputFlags::DecodeKeyboardChar) != 0;
        bool upperRomanGuideMode = (intputFlags & (int)InputFlags::UpperRomanGuideMode) != 0;

        LOG_INFOH(_T("\nENTER: keyId={:x}H({}={}), targetChar={}, decodeKeyboardChar={}, upperRomanGuideMode={}"),
            keyId, keyId, DECKEY_TO_CHARS->GetDeckeyNameFromId(keyId), to_wstr(targetChar), decodeKeyboardChar, upperRomanGuideMode);

        OutParams = outParams;
        initializeOutParams();

        if (startState == 0) return;

        // 各種状態を初期化してから
        STATE_COMMON->ClearStateInfo();
        STATE_COMMON->IncrementTotalDecKeyCount();
        STATE_COMMON->CountSameDecKey(keyId);
        if (decodeKeyboardChar) STATE_COMMON->SetDecodeKeyboardCharMode();  // キーボードフェイス文字を返すモード
        if (upperRomanGuideMode) STATE_COMMON->SetUpperRomanGuideMode();    // 英大文字による入力ガイドモード
        LOG_INFO(_T("outStack={}"), OUTPUT_STACK->OutputStackBackStrForDebug(10));

        // 同時打鍵コードなら、RootStrokeStateを削除しておく⇒と思ったが、実際にはそのようなケースがあったのでコメントアウト(「のにいると」で  KkDF のケース)
        //if (keyId >= COMBO_DECKEY_START && keyId < EISU_COMBO_DECKEY_END) {
        //    _LOG_DEBUGH(_T("\nENTER: Clear stroke"));
        //    startState->HandleDeckey(CLEAR_STROKE_DECKEY);
        //    _LOG_DEBUGH(_T("LEAVE: Clear stroke\n"));
        //}

        // DecKey処理を呼ぶ
        startState->HandleDeckey(keyId);

        LOG_INFO(_T("OUTPUT: outString=\"{}\", origString=\"{}\", flags={:x}, numBS={}"), \
            to_wstr(STATE_COMMON->OutString()), to_wstr(STATE_COMMON->OrigString()), STATE_COMMON->GetResultFlags(), STATE_COMMON->GetBackspaceNum());

        // アクティブウィンドウへの送出文字列
        size_t maxLen = utils::array_length(OutParams->outString);
        size_t cpyLen = copy_mstr(STATE_COMMON->OutString(), OutParams->outString, maxLen - 1);
        OutParams->resultFlags = STATE_COMMON->GetResultFlags();
        if (startState->ChainLength() > 1) {
            // 始状態に何か他の状態が後続していれば、Ctrl-Hなどの特殊キーをDECKEY化する
            OutParams->resultFlags |= (UINT32)ResultFlags::SpecialDeckeyRequired;
        }
        OutParams->numBackSpaces = STATE_COMMON->GetBackspaceNum();
        OutParams->strokeTableNum = StrokeTableNode::GetCurrentStrokeTableNum();

        // 出力履歴に BackSpaces を反映
        LOG_INFO(_T("pop numBS={}, outStack={}"), OutParams->numBackSpaces, OUTPUT_STACK->OutputStackBackStrForDebug(10));
        OUTPUT_STACK->pop(OutParams->numBackSpaces);
        // 出力文字列を履歴に反映 (全角の＊と？は半角に変換しておく⇒ワイルドカードを含む交ぜ書き変換で使う)
        LOG_INFO(_T("outStr={}, outStack={}"), OutParams->outString, OUTPUT_STACK->OutputStackBackStrForDebug(10));
        OUTPUT_STACK->push(utils::convert_star_and_question_to_hankaku(OutParams->outString));
        //String stack = std::regex_replace(to_wstr(OUTPUT_STACK->backStringFull(10)), std::wregex(_T("\n")), _T("|"));
        LOG_INFO(_T("outStack={}"), OUTPUT_STACK->OutputStackBackStrForDebug(10));
        // 出力履歴に BackSpaceStopper を反映
        if (STATE_COMMON->IsAppendBackspaceStopper()) { OUTPUT_STACK->pushNewLine(); }
        // 出力履歴に HistoryBlock を反映
        if (STATE_COMMON->IsSetHistoryBlockFlag()) {
            OUTPUT_STACK->setHistBlocker();
            _LOG_DEBUGH(_T("OUTPUT_STACK->setHistBlocker(): {}"), to_wstr(OUTPUT_STACK->backStringWithFlagUpto(20)));
        }
        // 出力履歴に MazeBlock を反映
        if (STATE_COMMON->IsSetMazegakiBlockFlag()) {
            OUTPUT_STACK->setMazeBlocker(STATE_COMMON->GetMazegakiBlockerPosition());
            _LOG_DEBUGH(_T("OUTPUT_STACK->setMazeBlocker(): {}"), to_wstr(OUTPUT_STACK->backStringWithFlagUpto(20)));
        }
        // 出力履歴に Rewritable を反映
        _LOG_DEBUGH(_T("OUTPUT_STACK->setRewritable({})"), STATE_COMMON->RewritableLen());
        OUTPUT_STACK->setRewritable(STATE_COMMON->RewritableLen());

        int strokeTableChainLen = startState->StrokeTableChainLength();
        _LOG_DEBUGH(_T("strokeTableChainLen={}"), strokeTableChainLen);
        STATE_COMMON->SetStrokeCount(std::max(strokeTableChainLen - 1, 0));
        if (strokeTableChainLen >= 2) {
            STATE_COMMON->SetWaiting2ndStroke();
            if (STATE_COMMON->GetLayout() == VkbLayout::None) STATE_COMMON->SetNormalVkbLayout();
        }
        _LOG_DEBUGH(_T("STATE_COMMON->StrokeCount={}"), STATE_COMMON->GetStrokeCount());

        // 最終的な出力履歴が整ったところで呼び出される処理
        if (!STATE_COMMON->IsOutStringProcDone() && !STATE_COMMON->IsWaiting2ndStroke()) startState->DoOutStringProcChain();

        // ヘルプや候補文字列
        setHelpOrCandidates(targetChar);

        if (Reporting::Logger::IsInfoHEnabled()) {
            //String stack = std::regex_replace(to_wstr(OUTPUT_STACK->OutputStackBackStr(10)), std::wregex(_T("\n")), _T("|"));
            LOG_INFOH(_T("LEAVE: states={} (len={}), flags={:x}, expKey={}, layout={}, centerStr={}, numBS={}, outLength={}, stack={}"),
                startState->JoinedName(), startState->ChainLength(), STATE_COMMON->GetResultFlags(), STATE_COMMON->GetNextExpectedKeyType(),
                STATE_COMMON->GetLayoutInt(), outParams->centerString, STATE_COMMON->GetBackspaceNum(), cpyLen, OUTPUT_STACK->OutputStackBackStrForDebug(10));
        }
    }

    // BackspaceStopper や HistoryBlock をセット
    void setBackspaceBlocker() {
        STATE_COMMON->SetBothHistoryBlockFlag();
        OUTPUT_STACK->pushNewLine();
        OUTPUT_STACK->setHistBlocker();
    }

    // 末尾のローマ字列を削除
    void clearTailRomanStr() {
        OUTPUT_STACK->ClearTailAlaphabetStr();
        _LOG_DEBUGH(_T("outStack={}"), OUTPUT_STACK->OutputStackBackStrForDebug(10));
    }

    // 末尾のひらがな列を削除
    void clearTailHiraganaStr() {
        OUTPUT_STACK->ClearTailHiraganaStr();
        _LOG_DEBUGH(_T("outStack={}"), OUTPUT_STACK->OutputStackBackStrForDebug(10));
    }

    // 末尾にひらがなブロッカーを設定
    void setHiraganaBlocker() {
        OUTPUT_STACK->SetHiraganaBlocker();
    }

    // ヘルプや候補文字列
    void setHelpOrCandidates(mchar_t targetChar) {
        //if (startState->StrokeTableChainLength() >= 2) STATE_COMMON->SetWaiting2ndStroke();
        LOG_DEBUG(_T("layout={}, nextExp={}"), STATE_COMMON->GetLayoutInt(), (int)STATE_COMMON->GetNextExpectedKeyType());
        OutParams->nextExpectedKeyType = (int)STATE_COMMON->GetNextExpectedKeyType();
        OutParams->strokeCount = (int)STATE_COMMON->GetStrokeCount();
        OutParams->nextSelectPos = (int)STATE_COMMON->GetNextSelectPos();
        OutParams->layout = STATE_COMMON->GetLayoutInt();
        copyToCenterString();
        mchar_t lastChar = copyToTopString();

        if (ROOT_STROKE_NODE && targetChar != 0 && OutParams->strokeCount > 0) {
            auto list = ROOT_STROKE_NODE->getStrokeList(to_mstr(targetChar), true);
            LOG_DEBUG(_T("strokeList={}, targetChar={:c}"), utils::join(list, _T(":")), (wchar_t)targetChar);
            if (!list.empty()) {
                if (list.size() > (size_t)OutParams->strokeCount) {
                    OutParams->nextStrokeDeckey = list[OutParams->strokeCount];
                    LOG_DEBUG(_T("OutParams->nextStrokeDeckey={}"), OutParams->nextStrokeDeckey);
                }
            }
        }

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
                    if (pos + i >= maxlen) break;
                    if (i >= LONG_VKEY_CHAR_SIZE) {
                        OutParams->faceStrings[pos + i - 1] = '…';
                        break;
                    }
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
                    OutParams->nextExpectedKeyType = (int)ExpectedKeyType::BushuCompHelp;
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
            OutParams->nextStrokeDeckey = -1;
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
        LOG_DEBUG(_T("CALLED: STATE_COMMON->CenterString()={}"), STATE_COMMON->CenterString());
        copyToCenterString(to_wstr(startState->JoinModeMarker()) + STATE_COMMON->CenterString());
    }

    void copyToCenterString(wchar_t ch) {
        LOG_DEBUG(_T("ENTER: centerString={}"), ch);
        OutParams->centerString[0] = ch;
        OutParams->centerString[1] = 0;
        LOG_DEBUG(_T("LEAVE"));
    }

    void copyToCenterString(const String& s) {
        LOG_DEBUG(_T("ENTER: centerString={}"), s);
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
        _LOG_DEBUGH(_T("\nENTER: outStackStr={}"), OUTPUT_STACK->OutputStackBackStrForDebug(32));
        size_t origLen = 0;
        // 打鍵途中なら打鍵中のキー文字列もミニバッファに表示する(ただし書き換えが存在しない場合のみ)
        // 書き換えありの場合、OrigString に '?' がアペンドされてしまうと、後で書き換えのときに同一部分判定で問題が生じるため
        if (STATE_COMMON->IsWaiting2ndStroke() && !(ROOT_STROKE_NODE && ROOT_STROKE_NODE->hasPostRewriteNode())) origLen = STATE_COMMON->OrigString().size();
        size_t topBufSize = utils::array_length(OutParams->topString);
        size_t prevMazeLen = MAZEGAKI_INFO ? MAZEGAKI_INFO->GetPrevOutputLen() : 0;
        _LOG_DEBUGH(_T("topBufSize={}, origLen={}, prevMazeLen={}"), topBufSize, origLen, prevMazeLen);
        auto s = OUTPUT_STACK->OutputStackBackStrWithFlagUpto(topBufSize - origLen - 1, prevMazeLen);        // ブロッカーを反映した文字列を取得
        _LOG_DEBUGH(_T("OutputStackBackStrWithFlagUpto({})={}"), (topBufSize - origLen - 1), to_wstr(s));
        size_t pos = copy_mstr(s, OutParams->topString, topBufSize);
        if (origLen > 0) copy_mstr(STATE_COMMON->OrigString(), OutParams->topString + pos, origLen);
        mchar_t lastChar = origLen == 0 ? OUTPUT_STACK->OutputStackLastChar() : 0;
        _LOG_DEBUGH(_T("LEAVE: OutParams->topString={}, lastChar={}"), OutParams->topString, to_wstr(lastChar));
        return lastChar;
    }

    // ストロークヘルプの作成
    void makeStrokeHelp(const String& ws) {
        makeStrokeHelp(ws, false);
    }

    // 部首合成ヘルプの作成
    void makeBushuCompHelp(const String& ws) {
        makeStrokeHelp(ws, true);
    }

    void makeStrokeHelp(const String& ws, bool bBushuComp) {
        LOG_INFO(_T("ENTER: {}, bushuComp={}"), ws, bBushuComp);
        auto ms = !ws.empty() ? to_mstr(ws) : OUTPUT_STACK->OutputStackBackStr(1);
        if (!ms.empty()) {
            OutParams->layout = (int)VkbLayout::StrokeHelp;
            copyToCenterString(to_wstr(ms));
            clearKeyFaces();
            if (!ms.empty()) {
                if (bBushuComp || !STROKE_HELP->copyStrokeHelpToVkbFacesOutParams(ms[0], OutParams->faceStrings)) {
                    if (BUSHU_DIC) {
                        if (BUSHU_DIC->CopyBushuCompHelpToVkbFaces(ms[0], OutParams->faceStrings, LONG_VKEY_CHAR_SIZE, LONG_VKEY_NUM, true)) {
                            OutParams->layout = (int)VkbLayout::BushuCompHelp;
                            OutParams->nextExpectedKeyType = (int)ExpectedKeyType::BushuCompHelp;
                        }
                    }
                }
            }
        }
        LOG_INFO(_T("LEAVE: layout={}"), OutParams->layout);
    }

    // 指定キーに対する次打鍵テーブルの作成
    void makeNextStrokeTable(int decKey1, int decKey2) {
        LOG_INFO(_T("ENTER: decKey1={}, decKey2={}"), decKey1, decKey2);
        OutParams->layout = (int)VkbLayout::Normal;
        clearKeyFaces();
        if (ROOT_STROKE_NODE && decKey1 >= 0) {
            StrokeTableNode* pn = dynamic_cast<StrokeTableNode*>(ROOT_STROKE_NODE->getNth(decKey1));
            if (pn && decKey2 >= 0) pn = dynamic_cast<StrokeTableNode*>(pn->getNth(decKey2));
            if (pn) {
                mchar_t* faces = STATE_COMMON->GetFaces();
                size_t numFaces = STATE_COMMON->FacesSize();
                pn->CopyChildrenFace(faces, numFaces);
                for (size_t i = 0; i < numFaces; ++i) {
                    //OutParams->faceStrings[i] = STATE_COMMON->faces[i];
                    set_facestr(faces[i], OutParams->faceStrings + i * 2);
                }
            }
        }
        LOG_INFO(_T("LEAVE: layout={}"), OutParams->layout);
    }

    // ひらがな50音図配列を作成する (あかさたなはまやらわ、ぁがざだばぱゃ)
    void makeHiraganaTable(DecoderOutParams* outParam) {
        VkbTableMaker::MakeVkbHiraganaTable(outParam->faceStrings);
    }

    // カタカナ50音図配列を作成する (アカサタナハマヤラワ、ァガザダバパャヮ)
    void makeKatakanaTable(DecoderOutParams* outParam) {
        VkbTableMaker::MakeVkbKatakanaTable(outParam->faceStrings);
    }

    // Deckey順に並んだ通常文字列とシフト文字列を返す
    void getCharsOrderedByDeckey(DecoderOutParams* outParam) {
        DECKEY_TO_CHARS->GetCharsOrderedByDeckey(outParam->faceStrings);
    }

    void clearKeyFaces() {
        for (size_t i = 0; i < utils::array_length(OutParams->faceStrings); ++i) {
            OutParams->faceStrings[i] = 0;
        }
    }

    // 連想辞書から定義文字列を読み出してくる
    void readBushuAssoc(const String& ws, wchar_t* buffer) {
        LOG_INFOH(_T("CALLED: ws={}"), ws);
        buffer[0] = 0;
        if (!ws.empty()) {
            if (BUSHU_ASSOC_DIC) {
                BushuAssocEntry* entry = BUSHU_ASSOC_DIC->GetEntry(ws[0]);
                if (entry) {
                    std::vector<MString> list(11);
                    entry->CopySubList(list, 0, list.size(), true);
                    size_t i = 0;
                    for (auto ms : list) {
                        if (!ms.empty()) {
                            auto mp = decomp_mchar(ms[0]);
                            if (mp.first != 0) buffer[i++] = mp.first;
                            if (mp.second != 0) buffer[i++] = mp.second;
                        }
                    }
                    buffer[i] = 0;
                }
            }
        }
    }

    // 後から部分的にストローク定義を解析してストローク木に差し込む
    void updateStrokeNodes(const String& ws) {
        StrokeTableNode::UpdateStrokeNodes(ws);
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
        Reporting::Logger::LogLevel = logLevel;
        Reporting::Logger::LogFilename = _T("kw-uni.log");
        return new DecoderImpl();
    }
    catch (String msg) {
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
        }
        catch (String msg) {
            LOG_ERROR(msg);
        }
        catch (...) {
            LOG_ERROR(_T("Some exception caught"));
        }
        ROOT_STROKE_NODE = 0;
        return -1;
    }
}

// デコーダを初期化する
// 引数: 初期化パラメータ
int InitializeDecoder(void* , DecoderCommandParams* ) {
    LOG_INFO_UC(_T("\n======== kw-uni START ========"));
    LOG_INFOH(_T("LogLevel={}"), Reporting::Logger::LogLevel);

    // エラーハンドラの生成
    ErrorHandler::CreateSingleton();

    return 0;
}

// デコーダを終了する
int FinalizeDecoder(void* pDecoder) {
    auto method_call = [pDecoder]() {
        Decoder* p = (Decoder*)pDecoder;
        p->Destroy();
        delete p;       // デコーダの終了時にデコーダインスタンスを破棄する
        LOG_INFO_UC(_T("======== kw-uni TERMINATED ========\n"));
        Reporting::Logger::Close();
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

// DECKEYハンドラ
// 引数: keyId = DECKEY ID, targetChar = 入力しようとしている文字
int HandleDeckeyDecoder(void* pDecoder, int keyId, mchar_t targetChar, int inputFlags, DecoderOutParams* params) {
    auto method_call = [pDecoder, keyId, targetChar, inputFlags, params]() { ((Decoder*)pDecoder)->HandleDeckey(keyId, targetChar, inputFlags, params); };
    return invokeDecoderMethod(method_call, nullptr);
}

// デコーダにコマンドを送って実行させる
// 引数: コマンドパラメータ, 出力用パラメータ
int ExecCmdDecoder(void* pDecoder, DecoderCommandParams* cmdParams, DecoderOutParams* outParams) {
    auto method_call = [pDecoder, cmdParams, outParams]() { ((Decoder*)pDecoder)->ExecCmd(cmdParams, outParams); };
    return invokeDecoderMethod(method_call, cmdParams);
}


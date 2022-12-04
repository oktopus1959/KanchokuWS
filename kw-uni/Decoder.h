#pragma once

//#include "pch.h"
#include "Logger.h"
#include "string_type.h"

#include "Settings.h"

#include "ErrorHandler.h"
#include "Node.h"
#include "State.h" 
#include "OutputStack.h"

// -------------------------------------------------------------------
#define IN_OUT_DATA_SIZE 2048

// デコーダの初期化やコマンド実行に用いる構造体
struct DecoderCommandParams {
    // 送受信データ
    // [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = IN_OUT_DATA_SIZE)]
    wchar_t inOutData[IN_OUT_DATA_SIZE];
};

// -------------------------------------------------------------------
// デコーダでのDECKEY処理の結果をUI側に送信するために用いる構造体
struct DecoderOutParams
{
    // アクティブウィンドウに対して文字列を送信する前に送りつけるBSの数
    int numBackSpaces;

    // DECKEYを仮想キーに変換してアクティブウィンドウに対して送信する場合は 1
    UINT32 resultFlags;

    // 次の入力で期待されるキー (第2ストローク、履歴入力でのEnter、など)
    int nextExpectedKeyType;

    // 文字を入力する際の現在の打鍵数
    int strokeCount = 0;

    // 指定文字の次の打鍵位置
    int nextStrokeDeckey = -1;

    // 次の選択候補位置
    int nextSelectPos = 0;

    // 使用中のストロークテーブルの番号(1 or 2)
    int strokeTableNum;

    // アクティブウィンドウに送信する文字列(または制御キー)
    // '\\0' 終端であること
    // [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 100)]
    wchar_t outString[100];

    // 表示する仮想キーボードのレイアウト (10:縦10列、50:通常50キー)
    int layout;

    // 仮想キーボードの上部に表示する文字列
    // 32文字未満の場合は '\\0' 終端であること
    // [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 32)]
    wchar_t topString[32];

    // 仮想キーボードの中央に表示する文字列
    // 10文字未満の場合は '\\0' 終端であること
    // [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 20)]
    wchar_t centerString[20];

    // 仮想キーボードに表示する文字列 (レイアウトにより、 20x10 または 2x100 として扱う)
    // 配列文字長未満の場合は '\\0' 終端であること
    // [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 200)]
    wchar_t faceStrings[200];
};

// 長い鍵盤の文字長
#define LONG_VKEY_CHAR_SIZE 20

//---------------------------------------------------------------------
// Decoder 抽象クラス
// キーストローク⇒文字 変換器としてはたらく
class Decoder {
public:
    // デストラクタ
    virtual ~Decoder() { }

    // 初期化
    virtual void Initialize(DecoderCommandParams* params) = 0;

    // 外字(左→左または右→右でどちらかに数字キーを含むもの)を集めたストローク表を作成する
    virtual void MakeInitialVkbTable(DecoderOutParams* table) = 0;

    // リセット
    virtual void Reset() = 0;

    // 辞書の保存
    virtual void SaveDicts() = 0;

    // 終了
    virtual void Destroy() = 0;

    // コマンド実行
    virtual void ExecCmd(DecoderCommandParams*, DecoderOutParams*) = 0;

    // DECKEY処理
    virtual void HandleDeckey(int, mchar_t, bool, bool, DecoderOutParams*) = 0;

}; // class Decoder

// -------------------------------------------------------------------
// UI側から呼び出される関数群
extern "C" {
    __declspec(dllexport) void* CreateDecoder(int);
    __declspec(dllexport) int InitializeDecoder(void*, DecoderCommandParams*);
    __declspec(dllexport) int FinalizeDecoder(void*);
    __declspec(dllexport) int ResetDecoder(void*);
    __declspec(dllexport) int SaveDictsDecoder(void*);
    __declspec(dllexport) int MakeInitialVkbTableDecoder(void* decoder, DecoderOutParams* table);
    __declspec(dllexport) int HandleDeckeyDecoder(void*, int, mchar_t, bool, bool, DecoderOutParams*);
    __declspec(dllexport) int ExecCmdDecoder(void*, DecoderCommandParams*, DecoderOutParams*);
}


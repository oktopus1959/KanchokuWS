#pragma once

#include "string_utils.h"
#include "Logger.h"

// エラーハンドラ
class ErrorHandler {
    DECLARE_CLASS_LOGGER;

    int errorLevel = 0;

    wstring errorMsg;

    // エラー情報を格納
    void setErrorInfo(int level, const wstring& msg);

public:
    // 初期化
    void Clear();

    // エラー情報を格納し、例外を送出
    void Error(const wstring& msg);

    // 警告情報を格納するが、継続する
    void Warn(const wstring& msg);

    int GetErrorLevel() const { return errorLevel; }

    const wstring& GetErrorMsg() const { return errorMsg; }

public:
    static std::unique_ptr<ErrorHandler> Singleton;

    static void CreateSingleton();

};

#define ERROR_HANDLER (ErrorHandler::Singleton)

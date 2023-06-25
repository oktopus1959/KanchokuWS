#pragma once

#include "string_utils.h"
#include "Logger.h"

// エラーハンドラ
class ErrorHandler {
    DECLARE_CLASS_LOGGER;

    int errorLevel = 0;

    String errorMsg;

    // エラー情報を格納
    void setErrorInfo(int level, StringRef msg);

public:
    // 初期化
    void Clear();

    // エラー情報を格納し、例外を送出
    void Error(StringRef msg);

    // 警告情報を格納するが、継続する
    void Warn(StringRef msg);

    int GetErrorLevel() const { return errorLevel; }

    StringRef GetErrorMsg() const { return errorMsg; }

public:
    static std::unique_ptr<ErrorHandler> Singleton;

    static void CreateSingleton();

};

#define ERROR_HANDLER (ErrorHandler::Singleton)

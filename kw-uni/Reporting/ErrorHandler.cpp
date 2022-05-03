// エラーハンドラ
#include "ErrorHandler.h"

DEFINE_CLASS_LOGGER(ErrorHandler);

std::unique_ptr<ErrorHandler> ErrorHandler::Singleton;

void ErrorHandler::CreateSingleton() {
    Singleton.reset(new ErrorHandler());
}

// 初期化
void ErrorHandler::Clear() {
    errorLevel = 0;
    errorMsg.clear();
}

// エラー情報を格納
void ErrorHandler::setErrorInfo(int level, const wstring& msg) {
    errorLevel = level;
    if (errorMsg.size() + msg.size() < 1024) {
        if (!errorMsg.empty()) errorMsg.append(_T("\r\n\r\n"));
        errorMsg.append(msg);
    }
}

// エラー情報を格納し、例外を送出
void ErrorHandler::Error(const wstring& msg) {
    setErrorInfo(2, msg);
    throw ERROR_HANDLER.get();
}

// 警告情報を格納するが、継続する
void ErrorHandler::Warn(const wstring& msg) {
    setErrorInfo(1, msg);
}

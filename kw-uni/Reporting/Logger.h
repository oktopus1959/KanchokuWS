#pragma once

// -------------------------------------------------------------------
// Logger クラス
// 使い方：
// - Logger を使いたい class の中で DECLARE_CLASS_LOGGER を記述
//   例: class Foo {
//           DECLARE_CLASS_LOGGER;
//       }
// - class の外で DEFINE_CLASS_LOGGER(className) を記述
//   例: DEFINE_CLASS_LOGGER(Foo);
// - プログラムの適当な場所で出力ログファイル名をセット
//   例: void main() {
//           Logger::LogFilename = "hoge.log";
//       }
// - プログラムの適当な場所で EnableXxx() を呼び出して LogLevel をセットする
//   例: Logger::EnableDebug();
//       なお、最低でも Logger::EnableLogger() を呼んでおかないと何もログ出力を行わない
// - ログ出力には、下記のマクロが使える：
//   - LOG_ERROR(...)
//   - LOG_WARN(...)
//   - LOG_INFO(...)
//   - LOG_DEBUG(...)
//   - LOG_TRACE(...)
// -------------------------------------------------------------------

#include "string_utils.h"

class FileWriter;

class Logger {
    static int SaveLevel;

public:
    static const int LogLevelError = 1;
    static const int LogLevelWarn = 2;
    static const int LogLevelInfoH = 3;
    static const int LogLevelInfo = 4;
    static const int LogLevelDebugH = 5;
    static const int LogLevelDebug = 6;
    static const int LogLevelTrace = 7;

public:
    static int LogLevel;

    static tstring LogFilename;

    static inline void EnableLogger() { LogLevel = LogLevelError; }

    static inline void EnableError() { LogLevel = LogLevelError; }

    static inline void EnableWarn() { LogLevel = LogLevelWarn; }

    static inline void EnableInfoH() { LogLevel = LogLevelInfoH; }

    static inline void EnableInfo() { LogLevel = LogLevelInfo; }

    static inline void EnableDebugH() { LogLevel = LogLevelDebugH; }

    static inline void EnableDebug() { LogLevel = LogLevelDebug; }

    static inline void EnableTrace() { LogLevel = LogLevelTrace; }

    //static void UseDefaultEncoding() { useDefaultEncoding = true; }

    static inline bool IsErrorEnabled() { return LogLevel >= LogLevelError; }

    static inline bool IsWarnEnabled() { return LogLevel >= LogLevelWarn; }

    static inline bool IsInfoHEnabled() { return LogLevel >= LogLevelInfoH; }

    static inline bool IsInfoEnabled() { return LogLevel >= LogLevelInfo; }

    static inline bool IsDebugEnabledH() { return LogLevel >= LogLevelDebugH; }

    static inline bool IsDebugEnabled() { return LogLevel >= LogLevelDebug; }

    static inline bool IsTraceEnabled() { return LogLevel >= LogLevelTrace; }

    static inline void SaveAndSetLevel(int level) {
        SaveLevel = LogLevel;
        if (LogLevel > level) LogLevel = level;
    }

    static inline void RestoreLevel() {
        LogLevel = SaveLevel;
    }
    //static bool useDefaultEncoding;

    //static wstring m_logFilePath;

public:
    static inline Logger GetLogger(const char* className, const TCHAR* classNameT) {
        return Logger(className, classNameT);
    }

private:
    std::string _className;
    tstring _classNameT;

    static FileWriter* m_fw;

    void writeLog(const char* level, const char* method, const char* file, int line, const tstring& msg);

public:
    inline Logger(const char* className, const TCHAR* classNameT)
        : _className(className), _classNameT(classNameT)
    { }

    ~Logger() {
        Close();
    }

    static void Close();

    inline const std::string& ClassName() const { return _className; }
    inline const tstring& ClassNameT() const { return _classNameT; }

    inline void Trace(const tstring& msg, const char* method, const char* file, int line) {
        writeLog("TRACE", method, file, line, msg);
    }

    inline void Debug(const tstring& msg, const char* method, const char* file, int line) {
        writeLog("DEBUG", method, file, line, msg);
    }

    inline void DebugH(const tstring& msg, const char* method, const char* file, int line) {
        writeLog("DEBUH", method, file, line, msg);
    }

    inline void Info(const tstring& msg, const char* method, const char* file, int line) {
        writeLog("INFO ", method, file, line, msg);
    }

    inline void InfoH(const tstring& msg, const char* method, const char* file, int line) {
        writeLog("INFOH", method, file, line, msg);
    }

    inline void Warn(const tstring& msg, const char* method, const char* file, int line) {
        writeLog("WARN ", method, file, line, msg);
    }

    inline void Error(const tstring& msg, const char* method, const char* file, int line) {
        writeLog("ERROR", method, file, line, msg);
    }

};

#define DECLARE_CLASS_LOGGER        static Logger logger
#define DEFINE_CLASS_LOGGER(name)   Logger name::logger = Logger::GetLogger(#name, _T(#name))
#define DEFINE_GLOBAL_LOGGER()   Logger logger = Logger::GetLogger("GLOBAL", _T("GLOBAL"))
#define DEFINE_LOCAL_LOGGER(name)   Logger logger = Logger::GetLogger("LOCAL." ## #name, _T("LOCAL." ## #name))
#define DEFINE_NAMESPACE_LOGGER(name)   Logger logger = Logger::GetLogger("NAMESPACE." ## #name, _T("NAMESPACE." ## #name))

#define IS_LOG_DEBUGH_ENABLED   (Logger::IsDebugEnabledH() && _LOG_DEBUGH_FLAG) 

#ifndef _DEBUG
#define LOG_TRACE(...)      {}
#define LOG_DEBUG(...)      {}
#define LOG_DEBUGH(...)     {}
#define _LOG_DEBUGH(...)    {}
#else
#define LOG_TRACE(...)      if (Logger::IsTraceEnabled()) logger.Trace(utils::format(__VA_ARGS__), __func__, __FILE__, __LINE__)
#define LOG_DEBUG(...)      if (Logger::IsDebugEnabled()) logger.Debug(utils::format(__VA_ARGS__), __func__, __FILE__, __LINE__)
#define LOG_DEBUGH(...)     if (Logger::IsDebugEnabledH()) logger.DebugH(utils::format(__VA_ARGS__), __func__, __FILE__, __LINE__)
#define _LOG_DEBUGH(...)    if (Logger::IsDebugEnabledH() && _LOG_DEBUGH_FLAG) logger.DebugH(utils::format(__VA_ARGS__), __func__, __FILE__, __LINE__); \
                            else if (Logger::IsDebugEnabled()) logger.Debug(utils::format(__VA_ARGS__), __func__, __FILE__, __LINE__)
#endif

#define LOG_INFO(...)       if (Logger::IsInfoEnabled())  logger.Info(utils::format(__VA_ARGS__), __func__, __FILE__, __LINE__)
#define LOG_INFOH(...)      if (Logger::IsInfoHEnabled()) logger.InfoH(utils::format(__VA_ARGS__).c_str(), __func__, __FILE__, __LINE__)
#define LOG_WARN(...)       logger.Warn(utils::format(__VA_ARGS__).c_str(), __func__, __FILE__, __LINE__)
#define LOG_ERROR(...)      logger.Error(utils::format(__VA_ARGS__).c_str(), __func__, __FILE__, __LINE__)

#ifndef _DEBUG
#define LOG_DEBUG_INFOH     LOG_DEBUG
#else
#define LOG_DEBUG_INFOH(...) if (LOG_DEBUG_INFOH_FLAG) {\
                                if (Logger::IsInfoHEnabled()) logger.InfoH(utils::format(__VA_ARGS__).c_str(), __func__, __FILE__, __LINE__);\
                            } else {\
                                if (Logger::IsDebugEnabled()) logger.Debug(utils::format(__VA_ARGS__), __func__, __FILE__, __LINE__);\
                            }
#endif

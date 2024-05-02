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

#include "std_utils.h"
#include "string_utils.h"

namespace Reporting {
    class FileWriter;

    class Logger {
        static int _saveLevel;

    public:
        static const int LogLevelError = 1;
        static const int LogLevelWarnH = 2;
        static const int LogLevelWarn = 3;
        static const int LogLevelInfoH = 4;
        static const int LogLevelInfo = 5;
        static const int LogLevelDebugH = 6;
        static const int LogLevelDebug = 7;
        static const int LogLevelTrace = 8;

    private:
        static int _logLevel;

        static String _logFilename;

    public:
        static int LogLevel() { return _logLevel; }
        static void SetLogLevel(int logLevel);

        static void SetLogFilename(StringRef logFilename);

        static inline void EnableLogger() { SetLogLevel(LogLevelError); }

        static inline void EnableError() { SetLogLevel(LogLevelError); }

        static inline void EnableWarnH() { SetLogLevel(LogLevelWarnH); }

        static inline void EnableWarn() { SetLogLevel(LogLevelWarn); }

        static inline void EnableInfoH() { SetLogLevel(LogLevelInfoH); }

        static inline void EnableInfo() { SetLogLevel(LogLevelInfo); }

        static inline void EnableDebugH() { SetLogLevel(LogLevelDebugH); }

        static inline void EnableDebug() { SetLogLevel(LogLevelDebug); }

        static inline void EnableTrace() { SetLogLevel(LogLevelTrace); }

        //static void UseDefaultEncoding() { useDefaultEncoding = true; }

        static inline bool IsAnyLogDisabled() { return LogLevel() < LogLevelError; }

        static inline bool IsErrorEnabled() { return LogLevel() >= LogLevelError; }

        static inline bool IsWarnEnabledH() { return LogLevel() >= LogLevelWarnH; }

        static inline bool IsWarnEnabled() { return LogLevel() >= LogLevelWarn; }

        static inline bool IsInfoHEnabled() { return LogLevel() >= LogLevelInfoH; }

        static inline bool IsInfoEnabled() { return LogLevel() >= LogLevelInfo; }

        static inline bool IsDebugHEnabled() { return LogLevel() >= LogLevelDebugH; }

        static inline bool IsDebugEnabled() { return LogLevel() >= LogLevelDebug; }

        static inline bool IsTraceEnabled() { return LogLevel() >= LogLevelTrace; }

        static inline void SaveAndSetLevel(int level) {
            _saveLevel = LogLevel();
            if (LogLevel() > level) SetLogLevel(level);
        }

        static inline void RestoreLevel() {
            SetLogLevel(_saveLevel);
        }
        //static bool useDefaultEncoding;

        //static String m_logFilePath;

    public:
        static inline Logger GetLogger(const std::string& className, StringRef classNameT) {
            return Logger(className, classNameT);
        }

        static void WriteLog(const std::string& msg);
        static void WriteLog(const String& msg);

    private:
        std::string _className;
        String _classNameT;

        static std::unique_ptr<FileWriter> fileWriterPtr;
        static bool initializeFileWriter();

        void writeLog(const std::string& level, const std::string& method, const std::string& /*file*/, int line, StringRef msg);

    public:
        inline Logger(const std::string& className, StringRef classNameT)
            : _className(className), _classNameT(classNameT)
        { }

        ~Logger() {
            Close();
        }

        static void Close();

        inline const std::string& ClassName() const { return _className; }
        inline const String& ClassNameT() const { return _classNameT; }

        inline void Trace(const String& msg, const std::string& method, const std::string& file, int line) {
            writeLog("TRACE", method, file, line, msg);
        }

        inline void Debug(const String& msg, const std::string& method, const std::string& file, int line) {
            writeLog("DEBUG", method, file, line, msg);
        }

        inline void DebugH(const String& msg, const std::string& method, const std::string& file, int line) {
            writeLog("DEBUH", method, file, line, msg);
        }

        inline void Info(const String& msg, const std::string& method, const std::string& file, int line) {
            writeLog("INFO ", method, file, line, msg);
        }

        inline void InfoH(const String& msg, const std::string& method, const std::string& file, int line) {
            writeLog("INFOH", method, file, line, msg);
        }

        inline void Warn(const String& msg, const std::string& method, const std::string& file, int line) {
            writeLog("WARN ", method, file, line, msg);
        }

        inline void WarnH(const String& msg, const std::string& method, const std::string& file, int line) {
            writeLog("WARNH", method, file, line, msg);
        }

        inline void Error(const String& msg, const std::string& method, const std::string& file, int line) {
            writeLog("ERROR", method, file, line, msg);
        }

    };

#define EXTERN_LOGGER                   extern Reporting::Logger logger
#define DECLARE_LOGGER                  EXTERN_LOGGER
#define DECLARE_CLASS_LOGGER            static Reporting::Logger logger

#define DEFINE_QUALIFIED_LOGGER(name)   Reporting::Logger name::logger = Reporting::Logger::GetLogger(#name, _T(#name))
#define DEFINE_CLASS_LOGGER(name)       DEFINE_QUALIFIED_LOGGER(name)

#define DEFINE_LOGGER_STR(name)         Reporting::Logger logger = Reporting::Logger::GetLogger(name, _T(name))
#define DEFINE_LOGGER(name)             DEFINE_LOGGER_STR(#name)
#define DEFINE_GLOBAL_LOGGER()          DEFINE_LOGGER_STR("GLOBAL")
#define DEFINE_LOCAL_LOGGER(name)       DEFINE_LOGGER_STR("LOCAL." #name)
#define DEFINE_NAMESPACE_LOGGER(name)   DEFINE_LOGGER_STR("NAMESPACE." #name)

#define IS_LOG_DEBUGH_ENABLED   (Reporting::Logger::IsDebugHEnabled()) 

#define _SAFE_CHAR(ch) (ch > 0 ? ch : ' ')

#define LOG_REPORT(level, fmt, ...)       logger.level(__VA_OPT__(std::format)(fmt __VA_OPT__(,) __VA_ARGS__), __func__, __FILE__, __LINE__)

#define LOG_LEVEL_ENABLED(level)          Is ## level ## Enabled
#define LOG_REPORT_COND(level, fmt, ...)  if (Reporting::Logger::LOG_LEVEL_ENABLED(level)()) LOG_REPORT(level, fmt, __VA_ARGS__)

#ifndef _DEBUG
#define LOG_TRACE(...)      {}
#define LOG_DEBUG(...)      {}
#define LOG_DEBUG_MSG(msg)  {}
#define LOG_DEBUG_CAT(args) {}
#define LOG_DEBUGH(...)     {}
#define _LOG_DEBUGH(...)    {}
#define _LOG_DEBUGH_COND(flag, ...)    {}
#define _LOG_DEBUG_COND(flag, ...)    {}
#define _DEBUG_SENT(x)      
#define _DEBUG_FLAG(x)      false
#else
#define LOG_TRACE(fmt, ...)   LOG_REPORT_COND(Trace, fmt, __VA_ARGS__)
#define LOG_DEBUG(fmt, ...)   LOG_REPORT_COND(Debug, fmt, __VA_ARGS__)
#define LOG_DEBUGH(fmt, ...)  LOG_REPORT_COND(DebugH, fmt, __VA_ARGS__)
#define _LOG_DEBUGH(fmt, ...) LOG_REPORT_COND(DebugH, fmt, __VA_ARGS__); else LOG_REPORT_COND(Debug, fmt, __VA_ARGS__)
#define _LOG_DEBUG_COND(flag, fmt, ...)   if (flag) LOG_REPORT_COND(Debug, fmt, __VA_ARGS__)
#define _LOG_DEBUGH_COND(flag, fmt, ...)  if (flag) LOG_REPORT_COND(DebugH, fmt, __VA_ARGS__)
#define _DEBUG_SENT(x)      x
#define _DEBUG_FLAG(x)      (x)
#endif

#define LOG_INFO(fmt, ...)  LOG_REPORT_COND(Info, fmt, __VA_ARGS__)
#define LOG_INFOH(fmt, ...) LOG_REPORT_COND(InfoH, fmt, __VA_ARGS__)
#define LOG_INFOH_COND(flag, fmt, ...) if (flag) LOG_REPORT_COND(InfoH, fmt, __VA_ARGS__)
#define LOG_INFO_COND(flag, fmt, ...)  if (flag) LOG_REPORT_COND(InfoH, fmt, __VA_ARGS__)
#define LOG_INFO_UC(fmt, ...) if (!Reporting::Logger::IsAnyLogDisabled()) LOG_REPORT(Info, fmt, __VA_ARGS__)
#define LOG_WARN(fmt, ...)  LOG_REPORT_COND(Warn, fmt, __VA_ARGS__)
#define LOG_WARNH(fmt, ...)  LOG_REPORT(WarnH, fmt, __VA_ARGS__)
#define LOG_ERROR(fmt, ...) LOG_REPORT(Error, fmt, __VA_ARGS__)

#ifndef _DEBUG
#define LOG_DEBUG_INFOH     LOG_DEBUG
#else
#define LOG_DEBUG_INFOH(fmt, ...) if (LOG_DEBUG_INFOH_FLAG) {\
                                if (Reporting::Logger::IsInfoHEnabled()) logger.InfoH(__VA_OPT__(std::format)(fmt __VA_OPT__(,) __VA_ARGS__), __func__, __FILE__, __LINE__);\
                            } else {\
                                if (Reporting::Logger::IsDebugEnabled()) logger.Debug(__VA_OPT__(std::format)(fmt __VA_OPT__(,) __VA_ARGS__), __func__, __FILE__, __LINE__);\
                            }
#endif
}

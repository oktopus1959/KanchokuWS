using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Utils 
{
    public class Logger : IDisposable
    {
        public const int LogLevelError = 1;
        public const int LogLevelWarnH = 2;
        public const int LogLevelWarn = 3;
        public const int LogLevelInfoH = 4;
        public const int LogLevelInfo = 5;
        public const int LogLevelDebugH = 6;
        public const int LogLevelDebug = 7;
        public const int LogLevelTrace = 8;

        private const int LogPromotedLevel = 3;

        public static string LogFilename { get; set; }

        public static void EnableError() { LogLevel = LogLevelError; }

        public static void EnableWarnH() { LogLevel = LogLevelWarnH; }

        public static void EnableWarn() { LogLevel = LogLevelWarn; }

        public static void EnableInfoH() { LogLevel = LogLevelInfoH; }

        public static void EnableInfo() { LogLevel = LogLevelInfo; }

        public static void EnableDebugH() { LogLevel = LogLevelDebugH; }

        public static void EnableDebug() { LogLevel = LogLevelDebug; }

        public static void EnableTrace() { LogLevel = LogLevelTrace; }

        public static void UseDefaultEncoding() { useDefaultEncoding = true; }

        public static bool IsInfoHEnabled => LogLevel >= LogLevelInfoH;

        public static bool IsInfoEnabled => LogLevel >= LogLevelInfo;

        public static bool IsDebugHEnabled => LogLevel >= LogLevelDebugH;

        public static bool IsDebugEnabled => LogLevel >= LogLevelDebug;

        public static bool IsTraceEnabled => LogLevel >= LogLevelTrace;

        public static int LogLevel {
            get { return m_logLevel; }
            set {
                m_logLevel = value;
                if (m_logLevel <= LogLevelWarn) {
                    Close();
                }
            }
        }

        private static bool useDefaultEncoding { get; set; } = false;

        private static int m_logLevel = 0;
        private static string m_logFilePath;
        private static System.IO.StreamWriter m_sw = null;
        private static object m_sync = new object();

        public static Logger GetLogger(bool bInfoPromotion = false)
        {
            return new Logger() {
                ClassName = new StackFrame(1).GetMethod().DeclaringType.FullName._split('.').Last(),
                IsInfoPromoted = bInfoPromotion,
            };
        }

        private static System.IO.StreamWriter getWriter()
        {
            if (m_sw != null) return m_sw;

            if (LogFilename._isEmpty()) return null;

            lock (m_sync) {
                var logDir = Helper.GetExeDirectory();
                if (logDir._notEmpty()) {
                    Helper.CreateDirectory(logDir);
                    m_logFilePath = logDir._joinPath(LogFilename);
                } else {
                    m_logFilePath = LogFilename;
                }

                try {
                    m_sw = new System.IO.StreamWriter(m_logFilePath, true, useDefaultEncoding ? Encoding.Default : Encoding.UTF8);
                } catch {
                    m_sw = null;
                    //LogFilename = null;
                }
            }
            return m_sw;
        }

        public void Dispose()
        {
            Close();
        }

        public static void Close()
        {
            //LogFilename = null;
            if (m_sw != null) {
                lock (m_sync) {
                    try {
                        m_sw.Close();
                    } catch {
                    }
                    m_sw = null;
                }
            }
        }

        public string ClassName { get; private set; }

        public bool IsInfoPromoted { get; private set; }

        public void Trace(string msg,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if (LogLevel >= LogLevelTrace) {
                writeLog("TRACE", $"{ClassName}.{method}", lineNumber, msg);
            }
        }

        public void Trace(Func<string> func,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if (LogLevel >= LogLevelTrace && func != null) {
                writeLog("TRACE", $"{ClassName}.{method}", lineNumber, func());
            }
        }

        public void Debug(string msg,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if (LogLevel >= LogLevelDebug) {
                writeLog("DEBUG", $"{ClassName}.{method}", lineNumber, msg);
            }
        }

        public void Debug(Func<string> func,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if ((LogLevel >= LogLevelDebug) && func != null) {
                writeLog("DEBUG", $"{ClassName}.{method}", lineNumber, func());
            }
        }

        public void DebugH(string msg,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if (LogLevel >= LogLevelDebugH || (IsInfoPromoted && LogLevel >= LogPromotedLevel)) {
                writeLog("DEBUH", $"{ClassName}.{method}", lineNumber, msg);
            }
        }

        public void DebugH(Func<string> func,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if ((LogLevel >= LogLevelDebugH || (IsInfoPromoted && LogLevel >= LogPromotedLevel)) && func != null) {
                writeLog("DEBUH", $"{ClassName}.{method}", lineNumber, func());
            }
        }

        public void Info(string msg,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if (LogLevel >= LogLevelInfo || (IsInfoPromoted && LogLevel >= LogPromotedLevel)) {
                writeLog("INFO", $"{ClassName}.{method}", lineNumber, msg);
            }
        }

        public void Info(Func<string> func,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if ((LogLevel >= LogLevelInfo || (IsInfoPromoted && LogLevel >= LogPromotedLevel)) && func != null) {
                writeLog("INFO", $"{ClassName}.{method}", lineNumber, func());
            }
        }

        public void InfoH(string msg,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if (LogLevel >= LogLevelInfoH) {
                writeLog("INFOH", $"{ClassName}.{method}", lineNumber, msg);
            }
        }

        public void InfoH(Func<string> func,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if ((LogLevel >= LogLevelInfoH || (IsInfoPromoted && LogLevel >= LogPromotedLevel)) && func != null) {
                writeLog("INFOH", $"{ClassName}.{method}", lineNumber, func());
            }
        }

        public void Warn(string msg,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if (LogLevel >= LogLevelWarn) {
                writeLog("WARN", $"{ClassName}.{method}", lineNumber, msg);
            }
        }

        public void Warn(Func<string> func,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if (LogLevel >= LogLevelWarn && func != null) {
                writeLog("WARN", $"{ClassName}.{method}", lineNumber, func());
            }
        }

        public void WarnH(string msg,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if (LogLevel >= LogLevelWarnH) {
                writeLog("WARNH", $"{ClassName}.{method}", lineNumber, msg);
            }
        }

        public void WarnH(Func<string> func,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if (LogLevel >= LogLevelWarnH && func != null) {
                writeLog("WARNH", $"{ClassName}.{method}", lineNumber, func());
            }
        }

        public void Error(string msg,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if (LogLevel >= LogLevelError) {
                writeLog("ERROR", $"{ClassName}.{method}", lineNumber, msg);
            }
        }

        public void WriteInfo(string msg,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if (LogLevel > 0) {
                writeLog("INFO", $"{ClassName}.{method}", lineNumber, msg);
            }
        }

        public void WriteLog(string Level, string msg,
            [CallerMemberName] string method = "",
            [CallerLineNumber] int lineNumber = -1)
        {
            if (LogLevel > 0) {
                writeLog(Level, $"{ClassName}.{method}", lineNumber, msg);
            }
        }

        private void writeLog(string level, string caller, int line, string msg)
        {
            var sw = getWriter();
            if (sw != null && msg._notEmpty()) {
                int nlCnt = 0;
                while (nlCnt < msg.Length && msg[nlCnt] == '\n') ++nlCnt;
                if (nlCnt > 0) {
                    sw.Write(msg._safeSubstring(0, nlCnt));
                    msg = msg._safeSubstring(nlCnt);
                }
                try {
                    sw.WriteLine($"{HRDateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff")} {level} [{caller}({line})] {msg}");
                    sw.Flush();
                    if (LogLevel <= LogLevelWarn) Close();
                } catch { }
            }
        }
    }
}

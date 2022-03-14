//#include "pch.h"
#include "string_utils.h"
#include "path_utils.h"

#include "Logger.h"

//-----------------------------------------------------------------------------
// ファイルへの書き込みを行うクラス.
class FileWriter
{
private:
	HANDLE m_hFile;
	tstring m_logfilepath;
	bool m_fileCreationFailed = false;

public:
	FileWriter(const tstring& logfilepath)
		: m_hFile(INVALID_HANDLE_VALUE),
		m_logfilepath(logfilepath)
	{
	}

	~FileWriter() {
		Close();
	}

public:
	// ログファイルへの書き込み
	void WriteLog(const char* logmsg) {
		if (m_hFile == INVALID_HANDLE_VALUE && !m_fileCreationFailed) {
			openFile();
		}

		DWORD dwWritten = 0;
		WriteFile(m_hFile, (LPCVOID)logmsg, lstrlenA(logmsg), &dwWritten, NULL);

		flushLog();
	}

	// ログファイルへの書き込み
	void WriteLog(const std::string& logmsg) {
		WriteLog(logmsg.c_str());
	}

	void Close() {
		if (m_hFile != INVALID_HANDLE_VALUE) {
			FlushFileBuffers(m_hFile);
			CloseHandle(m_hFile);
			m_hFile = INVALID_HANDLE_VALUE;
		}
	}

private:
	// フラッシュ
	void flushLog() {
		if (INVALID_HANDLE_VALUE != m_hFile) {
			FlushFileBuffers(m_hFile);
		}
	}

	// ログファイルの作成
	void openFile()
	{
		try {
			HANDLE hRet = create_file(m_logfilepath);
			if (hRet == INVALID_HANDLE_VALUE) {
				tstring dirpath = utils::getParentDirPath(m_logfilepath);
				if (!dirpath.empty()) {
					if (utils::makeDirectory(dirpath)) {
						hRet = create_file(m_logfilepath);
					}
				}
				if (hRet == INVALID_HANDLE_VALUE) {
					m_fileCreationFailed = true;
					return;
				}
			}
			SetFilePointer(hRet, 0, 0, FILE_END);
			m_hFile = hRet;
		}
		catch (...) {
			m_hFile = INVALID_HANDLE_VALUE;
			throw;
		}
	}

	// ファイル作成
	HANDLE create_file(const tstring& filepath)
	{
		DWORD dwFlags =
			FILE_ATTRIBUTE_ARCHIVE |
			FILE_FLAG_SEQUENTIAL_SCAN |
			FILE_FLAG_WRITE_THROUGH;

		return ::CreateFile(filepath.c_str(), GENERIC_WRITE, FILE_SHARE_READ,
			NULL, OPEN_ALWAYS, dwFlags, NULL);
	}

}; // class FileWriter


namespace {
    std::string getDatetimeStr() {
		SYSTEMTIME st;
		GetLocalTime(&st);
		return utils::formatA("%04d/%02d/%02d %02d:%02d:%02d.%03d",
			st.wYear,
			st.wMonth,
			st.wDay,
			st.wHour,
			st.wMinute,
			st.wSecond,
			st.wMilliseconds);
    }

    void _write_log(FileWriter* fw, const char* level, const char* className, const char* method, int line, const tstring& msg) {
#ifdef _UNICODE
        fw->WriteLog(utils::formatA("%s %s [%s.%s(%d)] %s\n", getDatetimeStr().c_str(), level, className, method, line, utils::utf8_encode(msg).c_str()));
#else
        fprintf(fp, "%s %s [%s.%s(%d)] %s\n", getDatetimeStr().c_str(), level, className, method, line, msg.c_str());
#endif
    }
}

//-----------------------------------------------------------------------------
int Logger::SaveLevel = 0;
int Logger::LogLevel = 0;

tstring Logger::LogFilename;

FileWriter* Logger::m_fw = 0;

void Logger::Close() {
	LogFilename.clear();
    if (m_fw != 0) {
        try {
			delete m_fw;
        }
        catch (...) {
        }
        m_fw = 0;
    }
}

void Logger::writeLog(const char* level, const char* method, const char* /*file*/, int line, const tstring& msg)
{
	if (m_fw == 0) {
		if (LogFilename.empty()) return;
		m_fw = new FileWriter(LogFilename);
	}

    if (msg.size() > 0 && msg[0] == '\n') {
		std::string newlines;
        size_t n = 0;
        while (msg.size() > 0 && n < msg.size() && msg[n] == '\n') {
			newlines.push_back('\n');
            ++n;
        }
		m_fw->WriteLog(newlines);
        if (n < msg.size()) _write_log(m_fw, level, _className.c_str(), method, line, msg.substr(n));
    } else {
        _write_log(m_fw, level, _className.c_str(), method, line, msg);
    }
}


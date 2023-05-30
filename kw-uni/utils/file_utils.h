#pragma once

#include "string_utils.h"

namespace utils {
    /**
    * 行末の cr/nf を除去して1行読み込む。
    * appendNL == true なら末尾に lf を追加した一行読み込み
    */
    inline std::tuple<tstring, bool> getLine(std::ifstream& ifs, bool appendNL = false) {
        char buffer[2048] = { 0 };
        // get one line
        bool eof = ifs.eof();
        if (!eof) {
            ifs.getline(buffer, sizeof(buffer) - 2);
            size_t len = strlen(buffer);
            if (appendNL) {
                if (len == 0 || (buffer[len - 1] != '\r' && buffer[len - 1] != '\n')) {
                    buffer[len++] = '\n';
                    buffer[len++] = '\0';
                }
            }
        }
#ifdef _UNICODE
        return { utils::utf8_decode(buffer), eof };
#else
        return { buffer, eof };
#endif
    }

    // 行末に NL を付加したまま1行読み込む
    // 空行とファイルの終わりを区別したい場合に有用
    inline tstring getLineWithNl(std::ifstream& ifs) {
        tstring result;
        std::tie(result, std::ignore) = getLine(ifs, true);
        return result;
    }

    // ifstream の reader
    class IfstreamReader {
    public:
        IfstreamReader() { }

        IfstreamReader(const tstring& filepath) {
            open(filepath);
        }

        ~IfstreamReader() {
            if (success()) ifs.close();
        }

        inline bool open(const tstring& filepath) {
            ifs.open(filepath, std::ios_base::in);
            _fail = ifs.fail();
            return success();
        }

        inline bool success() { return !_fail; }

        inline bool fail() { return _fail; }

        inline std::ifstream* getPtr() { return &ifs; }

        // 1行読み込み。EOF になったら bool 値として false が返る。
        // appendNL == false (デフォルト)なら行末の NL は除去
        // appendNL == true なら行末に NL を付加
        inline std::tuple<tstring, bool> getLine(bool appendNL = false) {
            return utils::getLine(ifs, appendNL);
        }

        // 行末に NL を付加したまま1行読み込む
        // 空行とファイルの終わりを区別したい場合に有用
        inline tstring getLineWithNl() {
            return utils::getLineWithNl(ifs);
        }

        // 全行を読み込んで1行ずつ vector に push_back する。行末に NL は付かない。
        inline std::vector<tstring> getAllLines() {
            std::vector<tstring> result;
            if (success()) {
                while (true) {
                    tstring ts;
                    bool eof;
                    std::tie(ts, eof) = getLine();
                    if (eof) break;
                    result.push_back(ts);
                }
            }
            return result;
        }

    private:
        std::ifstream ifs;
        bool _fail = true;
    };
    
    // ofstream の writer
    class OfstreamWriter {
    public:
        OfstreamWriter(bool append = false) : _append(append) { }

        OfstreamWriter(const tstring& filepath, bool append = false) : _append(append) {
            open(filepath);
        }

        ~OfstreamWriter() {
            if (success()) ofs.close();
        }

        inline bool open(const tstring& filepath) {
            ofs.open(filepath, _append ? std::ios_base::app : std::ios_base::out);
            _fail = ofs.fail();
            return success();
        }

        inline bool success() { return !_fail; }

        inline bool fail() { return _fail; }

        inline size_t count() { return _count; }

        // 1行書き込み。
        // appendNL == true (デフォルト)なら行末の NL を追加
        // appendNL == false なら行末に NL を付加しない
        inline void writeLine(const std::string& line, bool appendNL = true) {
            ofs.write(line.c_str(), line.size());
            if (appendNL) ofs.write("\n", 1);
            ++_count;
        }

    private:
        std::ofstream ofs;
        bool _fail = true;
        bool _append = false;
        size_t _count = 0;
    };
   
} // namespace utils

#define UTILS_GET_LINE_FIRST(x, y, z) tstring x; bool y; std::tie(x, y) = z.getLine()

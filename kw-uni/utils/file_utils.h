#pragma once

#include "string_utils.h"
#include "misc_utils.h"
#include "path_utils.h"

namespace utils {
    /**
    * 行末の cr/nf を除去して1行読み込む。
    * appendNL == true なら末尾に lf を追加した一行読み込み
    * EOF なら bool として true を返す
    */
    inline std::tuple<String, bool> getLine(std::istream& is, bool appendNL = false) {
        char buffer[2048] = { 0 };
        // get one line
        bool eof = is.eof();
        if (!eof) {
            is.getline(buffer, sizeof(buffer) - 2);
            size_t len = strlen(buffer);
            if (len == 0 && is.eof()) {
                eof = true;
            } else if (appendNL) {
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
    // EOF なら bool として true を返す
    inline String getLineWithNl(std::istream& is) {
        String result;
        std::tie(result, std::ignore) = getLine(is, true);
        return result;
    }

    class IfstreamReader;
    class OfstreamWriter;

    class Serializable {
    public:
        virtual void serialize(OfstreamWriter& writer) const = 0;

        virtual void deserialize(IfstreamReader& reader) = 0;
    };

#define ULONG unsigned long

    // ifstream の reader
    class IfstreamReader {
    public:
        IfstreamReader() { }

        IfstreamReader(bool binary) : _binary(binary) { }

        IfstreamReader(StringRef filepath, bool binary = false) : _binary(binary) {
            open(filepath);
        }

        ~IfstreamReader() {
            if (success() && !_cin) ifs.close();
        }

        inline bool open(StringRef filepath) {
            std::ios_base::openmode openMode = std::ios_base::in;
            if (_binary) openMode = openMode | std::ios_base::binary;
            ifs.open(filepath, openMode);
            _fail = ifs.fail();
            _cin = false;
            return success();
        }

        inline void setDummyLines(const std::vector<String>& dummyLines) {
            _dummyLines = dummyLines;
            _dummyCount = 0;
        }

        inline bool success() { return !_fail; }

        inline bool fail() { return _fail; }

        // 1行読み込み。EOF になったら bool 値として true が返る。
        // appendNL == false (デフォルト)なら行末の NL は除去
        // appendNL == true なら行末に NL を付加
        inline std::tuple<String, bool> getLine(bool appendNL = false) {
            return success()
                ? utils::getLine(_is(), appendNL)
                : _dummyCount < _dummyLines.size() ? std::tuple<String, bool>{ _dummyLines[_dummyCount++], false } : std::tuple<String, bool>{ L"", true };
        }

        // 行末に NL を付加したまま1行読み込む
        // 空行とファイルの終わりを区別したい場合に有用
        inline String getLineWithNl() {
            return utils::getLineWithNl(_is());
        }

        // 全行を読み込んで1行ずつ vector に push_back する。行末に NL は付かない。
        inline std::vector<String> getAllLines() {
            std::vector<String> result;
            getAllLines(result);
            return result;
        }

        // 全行を読み込んで1行ずつ vector に push_back する。行末に NL は付かない。
        inline std::vector<String>& getAllLines(std::vector<String>& lines) {
            if (success()) {
                while (true) {
                    String ts;
                    bool eof;
                    std::tie(ts, eof) = getLine();
                    if (eof) break;
                    lines.push_back(ts);
                }
            }
            return lines;
        }

        // read unsigned long
        inline unsigned long read_ulong() {
            ULONG _value;
            ifs.read(reinterpret_cast<char*>(&_value), sizeof(ULONG));
            return _value;
        }

        // read unsigned long
        inline void read(unsigned long& value) {
            value = read_ulong();
        }

        // read size_t
        inline void read(size_t& value) {
            value = (size_t)read_ulong();
        }

        // read short
        inline void read(short& value) {
            value = (short)read_ulong();
        }

        // read int
        inline void read(int& value) {
            value = (int)read_ulong();
        }

        // read bool
        inline void read(bool& value) {
            value = (bool)read_ulong();
        }

        // read String
        inline void read(String& str) {
            size_t size = (size_t)read_ulong();
            str.resize(size);
            ifs.read(reinterpret_cast<char*>(str.data()), (std::streamsize)size * sizeof(wchar_t));
        }

        // read String vector
        inline void read(std::vector<String>& vec) {
            size_t size = (size_t)read_ulong();
            std::vector<wchar_t> buf(size);
            ifs.read(reinterpret_cast<char*>(buf.data()), buf.size() * sizeof(wchar_t));
            wchar_t* p = buf.data();
            vec.clear();
            while (p < buf.data() + size) {
                vec.push_back(p);
                p += vec.back().size() + 1;
            }
        }

        // read Serializable object
        template<class T> requires std::is_base_of_v<Serializable, T>
        inline void read(T& obj) {
            obj.deserialize(*this);
        }

        // read Serializable vector
        template<class T> requires std::is_base_of_v<Serializable, T>
        inline void read(std::vector<T>& vec) {
            size_t size = (size_t)read_ulong();
            vec.resize(size);
            for (auto& obj : vec) {
                obj.deserialize(*this);
            }
        }

        // read Serializable object
        template<class T>
        inline void readSerializable(T& obj) {
            obj.deserialize(*this);
        }

        // read Serializable vector
        template<class T>
        inline void readSerializable(std::vector<T>& vec) {
            size_t size = (size_t)read_ulong();
            vec.resize(size);
            for (auto& obj : vec) {
                obj.deserialize(*this);
            }
        }

        // read primitive vector
        template<class T>
        inline void read(std::vector<T>& vec) {
            size_t size = (size_t)read_ulong();
            vec.resize(size);
            ifs.read(reinterpret_cast<char*>(vec.data()), sizeof(T) * size);
        }

        // read map
        template<class T, class U>
        inline void read(std::map<T, U>& map) {
            std::vector<T> keys;
            std::vector<U> values;
            read(keys);
            read(values);
            map.clear();
            for (size_t i = 0; i < keys.size() && i < values.size(); ++i) {
                map[keys[i]] = values[i];
            }
        }

        inline size_t read(char* data, size_t bufsiz) {
            size_t size = (size_t)read_ulong();
            if (bufsiz >= size) {
                ifs.read(data, size);
                return size;
            } else {
                ifs.read(data, bufsiz);
                std::vector<char> dummy(size - bufsiz);
                ifs.read(dummy.data(), size - bufsiz);
                return bufsiz;
            }
        }

    private:
        std::ifstream ifs;
        bool _fail = false;
        bool _binary = false;
        bool _cin = true;

        std::vector<String> _dummyLines;
        size_t _dummyCount = 0;

        inline std::istream& _is() { return _cin ? std::cin : ifs; }
    };
   
    // ファイルのすべての行を返す
    inline std::vector<String> readAllLines(StringRef filepath) {
        //utils::IfstreamReader reader(filepath);
        //return reader.getAllLines();
        return utils::IfstreamReader(filepath).getAllLines();
    }

    // ファイルの最初の行を返す
    inline String readFirstLine(StringRef filepath) {
        auto [line, _] = utils::IfstreamReader(filepath).getLine();
        return line;
    }

    // ofstream の writer
    class OfstreamWriter {
    public:
        OfstreamWriter(bool append = false) : _append(append) { }

        OfstreamWriter(bool binary, bool append) : _binary(binary), _append(append) { }

        OfstreamWriter(StringRef filepath, bool append = false) : _append(append) {
            open(filepath);
        }

        OfstreamWriter(StringRef filepath, bool binary, bool append) : _binary(binary), _append(append) {
            open(filepath);
        }

        ~OfstreamWriter() {
            if (success()) ofs.close();
        }

        inline bool open(StringRef filepath) {
            std::ios_base::openmode openMode = _append ? std::ios_base::app : std::ios_base::out;
            if (_binary) openMode = openMode | std::ios_base::binary;
            ofs.open(filepath, openMode);
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

        inline void writeLine(StringRef line, bool appendNL = true) {
            writeLine(utf8_encode(line), appendNL);
        }

        // write unsigned long
        inline void write(ULONG size) {
            ofs.write(reinterpret_cast<const char*>(&size), sizeof(ULONG));
        }

        // write size_t
        inline void write(size_t value) {
            write((ULONG)value);
        }

        // write int
        inline void write(int value) {
            write((ULONG)value);
        }

        // write bool
        inline void write(bool value) {
            write((ULONG)value);
        }

        // write String
        inline void write(StringRef str) {
            write(str.size());
            ofs.write(reinterpret_cast<const char*>(str.data()), str.size() * sizeof(wchar_t));
        }

        // write String vector
        inline void write(const std::vector<String>& vec) {
            size_t size = 0;
            for (StringRef s : vec) {
                size += (s.size() + 1);
            }
            std::vector<wchar_t> buf(size);
            wchar_t* p = buf.data();
            for (StringRef s : vec) {
                size_t slen = s.size() + 1;
                wcsncpy_s(p, slen, s.c_str(), slen);
                p += slen;
            }
            write(size);
            ofs.write(reinterpret_cast<const char*>(buf.data()), buf.size() * sizeof(wchar_t));
        }

        // write Serializable object
        template<class T> requires std::is_base_of_v<Serializable, T>
        inline void write(const T& obj) {
            obj.serialize(*this);
        }

        // write Serializable vector
        template<class T> requires std::is_base_of_v<Serializable, T>
        inline void write(const std::vector<T>& vec) {
            write(vec.size());
            for (const auto& obj : vec) {
                obj.serialize(*this);
            }
        }

        // write Serializable object
        template<class T>
        inline void writeSerializable(const T& obj) {
            obj.serialize(*this);
        }

        // write Serializable vector
        template<class T>
        inline void writeSerializable(const std::vector<T>& vec) {
            write(vec.size());
            for (const auto& obj : vec) {
                obj.serialize(*this);
            }
        }

        // write primitive vector
        template<class T>
        inline void write(const std::vector<T>& vec) {
            write(vec.size());
            ofs.write(reinterpret_cast<const char*>(vec.data()), sizeof(T) * vec.size());
        }

        // write map
        template<class T, class U>
        inline void write(const std::map<T, U>& map) {
            std::vector<T> keys;
            std::vector<U> values;
            utils::get_keys_and_values(map, keys, values);
            write(keys);
            write(values);
        }

        inline void write(const char* data, size_t size) {
            write(size);
            ofs.write(data, size);
        }


    private:
        std::ofstream ofs;
        bool _fail = true;
        bool _append = false;
        bool _binary = false;
        size_t _count = 0;
    };
   
#undef ULONG

} // namespace utils

// File の拡張メソッドクラス
struct FileUtil {

    static inline bool exists(StringRef path) {
        return utils::isFileExistent(path);
    }

    static inline Vector<String> readAllLines(StringRef path) {
        return utils::readAllLines(path);
    }
};

#define UTILS_GET_LINE_FIRST(x, y, z) String x; bool y; std::tie(x, y) = z.getLine()

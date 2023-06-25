#pragma once

#include "std_utils.h"
#include "string_utils.h"
#include "Logger.h"

namespace util {
    class RuntimeException {
        String _cause;
        String _file;
        size_t _line = 0;

    public:
        RuntimeException() { }

        RuntimeException(const std::string& cause, const char* file, size_t line) : RuntimeException(utils::utf8_decode(cause), file, line)  { }

        RuntimeException(const String& cause, const char* file, size_t line) : _cause(cause), _file(utils::utf8_decode(file)), _line(line) { }

        const String& getCause() const {
            return _cause;
        }

        const String& getFile() const {
            return _file;
        }

        const size_t getLine() const {
            return _line;
        }
    };

} // namespace util

#define THROW_RTE(fmt, ...)  { LOG_ERROR(fmt, __VA_ARGS__); throw util::RuntimeException(__VA_OPT__(std::format)(fmt __VA_OPT__(,) __VA_ARGS__), __FILE__, __LINE__); }
//#define THROW_RTE(fmt, ...)  {}

#define LOG_ERROR_AND_THROW_RTE(fmt, ...)  THROW_RTE(fmt, __VA_ARGS__)

#define CHECK_OR_THROW(cond, fmt, ...)  if (!(cond)) { THROW_RTE(fmt, __VA_ARGS__); }

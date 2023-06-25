#pragma once

#include "regex_utils.h"
#include "xsvparser.hpp"


namespace utils {

    inline std::vector<String> parseCSV(StringRef line, size_t maxCount = (size_t)INT_MAX) {
        return langedge::CSVParser<String::const_iterator>::parseLine(line.begin(), line.end(), maxCount);
    }

    inline std::vector<std::string> parseCSV(const std::string& line, size_t maxCount = (size_t)INT_MAX) {
        return langedge::CSVParser<std::string::const_iterator>::parseLine(line.begin(), line.end(), maxCount);
    }

    inline std::vector<String> parseTSV(StringRef line, size_t maxCount = (size_t)INT_MAX) {
        return langedge::TSVParser<String::const_iterator>::parseLine(line.begin(), line.end(), maxCount);
    }

    inline std::vector<std::string> parseTSV(const std::string& line, size_t maxCount = (size_t)INT_MAX) {
        return langedge::TSVParser<std::string::const_iterator>::parseLine(line.begin(), line.end(), maxCount);
    }

    inline String enclose_by_dq(StringRef w) {
        String t;
        t.append(1, '"');
        t.append(utils::replace_all(w, L"\"", L"\"\""));
        t.append(1, '"');
        return t;
    }

    inline String escape_csv_element(StringRef w) {
        String t = w.find('"') < w.size() ? utils::replace_all(w, L"\"", L"\"\"") : w;
        if (w.find('"') < w.size()) {
            return enclose_by_dq(utils::replace_all(w, L"\"", L"\"\""));
        } else if (w.find(',') < w.size()) {
            return enclose_by_dq(w);
        } else {
            return w;
        }
    }
} // namespace utils

struct CsvUtil {
    inline static std::vector<String> parseCSV(const String& line, size_t maxCount = (size_t)INT_MAX) {
        return utils::parseCSV(line, maxCount);
    }

    inline static std::vector<std::string> parseCSV(const std::string& line, size_t maxCount = (size_t)INT_MAX) {
        return utils::parseCSV(line, maxCount);
    }

    inline static std::vector<String> parseTSV(const String& line, size_t maxCount = (size_t)INT_MAX) {
        return utils::parseTSV(line, maxCount);
    }

    inline static std::vector<std::string> parseTSV(const std::string& line, size_t maxCount = (size_t)INT_MAX) {
        return utils::parseTSV(line, maxCount);
    }

    inline String enclose_by_dq(const String& s) {
        return utils::enclose_by_dq(s);
    }

    inline String escape_csv_element(const String& s) {
        return utils::escape_csv_element(s);
    }
};

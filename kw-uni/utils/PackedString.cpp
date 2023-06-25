#pragma once

#include "PackedString.h"

namespace util {
    void PackedString::serialize(utils::OfstreamWriter& writer) const {
        writer.write(_buffer);
    }

    void PackedString::deserialize(utils::IfstreamReader& reader) {
        reader.read(_buffer);
    }

    size_t PackedString::pack(StringRef str) {
        size_t ptr = _buffer.size();
        _buffer.push_back((wchar_t)str.size());
        _buffer.append(str);
        return ptr;
    }

    std::vector<String> PackedString::list(size_t ptr) const {
        std::vector<String> result;
        while (ptr < _buffer.length()) {
            result.push_back(unpack(ptr));
            ptr = next(ptr);
        }
        return result;
    }

} // namespace util
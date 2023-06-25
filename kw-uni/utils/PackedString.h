#pragma once

#include "string_utils.h"
#include "file_utils.h"

namespace util {
    class PackedString {
        String _buffer;

    public:
        PackedString() { }

        //PackedString(StringRef str) {
        //    _buffer = str;
        //}

        PackedString(const PackedString& ps) : _buffer(ps._buffer) {
        }

        void serialize(utils::OfstreamWriter& writer) const;

        void deserialize(utils::IfstreamReader& reader);

        size_t pack(StringRef str);

        String unpack(size_t ptr) const {
            return utils::safe_substr(_buffer, ptr + 1, length(ptr));
        }

        size_t length(size_t ptr) const {
            return ptr < _buffer.size() ? (size_t)_buffer[ptr] : 0;
        }

        size_t next(size_t ptr) const {
            return ptr + length(ptr) + 1;
        }

        std::vector<String> list(size_t ptr = 0) const;

        String unpackAll(wchar_t delim = '|') const {
            return utils::join(list(), delim);
        }

        StringRef getBuffer() const {
            return _buffer;
        }

    };

} // namespace util
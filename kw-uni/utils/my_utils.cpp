#include "std_utils.h"
#include "string_utils.h"
#include "my_utils.h"
#include "exception.h"

namespace {
    DEFINE_LOCAL_LOGGER(my_util);

    // Copied from MurmurHash3.cpp
    // http://code.google.com/p/smhasher/source/browse/trunk/MurmurHash3.cpp
    //-----------------------------------------------------------------------------
    // Platform-specific functions and macros
    // Microsoft Visual Studio
#if defined(_MSC_VER)

#define FORCE_INLINE    __forceinline

#define ROTL32(x,y)     _rotl(x,y)

#define BIG_CONSTANT(x) (x)

// Other compilers

#else   // defined(_MSC_VER)

#define FORCE_INLINE inline __attribute__((always_inline))

    inline uint32_t rotl32(uint32_t x, uint8_t r) {
        return (x << r) | (x >> (32 - r));
    }

#define ROTL32(x,y)     rotl32(x,y)

#endif // !defined(_MSC_VER)

    //-----------------------------------------------------------------------------
    // Block read - if your platform needs to do endian-swapping or can only
    // handle aligned reads, do the conversion here

    FORCE_INLINE uint32_t getblock(const uint32_t* p, int i) {
        return p[i];
    }

    //-----------------------------------------------------------------------------
    // Finalization mix - force all bits of a hash block to avalanche

    FORCE_INLINE uint32_t fmix(uint32_t h) {
        h ^= h >> 16;
        h *= 0x85ebca6b;
        h ^= h >> 13;
        h *= 0xc2b2ae35;
        h ^= h >> 16;

        return h;
    }

    void MurmurHash3_x86_128(const void* key, const int len,
        uint32_t seed, char* out) {
        const uint8_t* data = (const uint8_t*)key;
        const int nblocks = len / 16;

        uint32_t h1 = seed;
        uint32_t h2 = seed;
        uint32_t h3 = seed;
        uint32_t h4 = seed;

        uint32_t c1 = 0x239b961b;
        uint32_t c2 = 0xab0e9789;
        uint32_t c3 = 0x38b34ae5;
        uint32_t c4 = 0xa1e38b93;

        //----------
        // body

        const uint32_t* blocks = (const uint32_t*)(data + nblocks * 16);

        for (int i = -nblocks; i; i++)
        {
            uint32_t k1 = getblock(blocks, i * 4 + 0);
            uint32_t k2 = getblock(blocks, i * 4 + 1);
            uint32_t k3 = getblock(blocks, i * 4 + 2);
            uint32_t k4 = getblock(blocks, i * 4 + 3);

            k1 *= c1; k1 = ROTL32(k1, 15); k1 *= c2; h1 ^= k1;

            h1 = ROTL32(h1, 19); h1 += h2; h1 = h1 * 5 + 0x561ccd1b;

            k2 *= c2; k2 = ROTL32(k2, 16); k2 *= c3; h2 ^= k2;

            h2 = ROTL32(h2, 17); h2 += h3; h2 = h2 * 5 + 0x0bcaa747;

            k3 *= c3; k3 = ROTL32(k3, 17); k3 *= c4; h3 ^= k3;

            h3 = ROTL32(h3, 15); h3 += h4; h3 = h3 * 5 + 0x96cd1c35;

            k4 *= c4; k4 = ROTL32(k4, 18); k4 *= c1; h4 ^= k4;

            h4 = ROTL32(h4, 13); h4 += h1; h4 = h4 * 5 + 0x32ac3b17;
        }

        //----------
        // tail

        const uint8_t* tail = (const uint8_t*)(data + nblocks * 16);
        uint32_t k1 = 0;
        uint32_t k2 = 0;
        uint32_t k3 = 0;
        uint32_t k4 = 0;

        switch (len & 15)
        {
        case 15: k4 ^= tail[14] << 16;
        case 14: k4 ^= tail[13] << 8;
        case 13: k4 ^= tail[12] << 0;
            k4 *= c4; k4 = ROTL32(k4, 18); k4 *= c1; h4 ^= k4;

        case 12: k3 ^= tail[11] << 24;
        case 11: k3 ^= tail[10] << 16;
        case 10: k3 ^= tail[9] << 8;
        case  9: k3 ^= tail[8] << 0;
            k3 *= c3; k3 = ROTL32(k3, 17); k3 *= c4; h3 ^= k3;

        case  8: k2 ^= tail[7] << 24;
        case  7: k2 ^= tail[6] << 16;
        case  6: k2 ^= tail[5] << 8;
        case  5: k2 ^= tail[4] << 0;
            k2 *= c2; k2 = ROTL32(k2, 16); k2 *= c3; h2 ^= k2;

        case  4: k1 ^= tail[3] << 24;
        case  3: k1 ^= tail[2] << 16;
        case  2: k1 ^= tail[1] << 8;
        case  1: k1 ^= tail[0] << 0;
            k1 *= c1; k1 = ROTL32(k1, 15); k1 *= c2; h1 ^= k1;
        };

        //----------
        // finalization

        h1 ^= len; h2 ^= len; h3 ^= len; h4 ^= len;

        h1 += h2; h1 += h3; h1 += h4;
        h2 += h1; h3 += h1; h4 += h1;

        h1 = fmix(h1);
        h2 = fmix(h2);
        h3 = fmix(h3);
        h4 = fmix(h4);

        h1 += h2; h1 += h3; h1 += h4;
        h2 += h1; h3 += h1; h4 += h1;

        std::memcpy(out, reinterpret_cast<char*>(&h1), 4);
        std::memcpy(out + 4, reinterpret_cast<char*>(&h2), 4);
        std::memcpy(out + 8, reinterpret_cast<char*>(&h3), 4);
        std::memcpy(out + 12, reinterpret_cast<char*>(&h4), 4);
    }
}

namespace progress {
    const size_t scale = 50;
    size_t prev = 0;
}

namespace util {
    // Print Progress Bar
    void progress_bar(StringRef message, size_t current, size_t total) {
        size_t cur_percentage = (size_t)((current * 100.0) / total);
        size_t bar_len = (size_t)((double)current * progress::scale / total);

      if (current == 0 || progress::prev != cur_percentage) {
          std::wcout
              << message
              << std::format(L": {:3d}% |", cur_percentage) << String(progress::scale - bar_len, L' ') << String(bar_len, '#')
              << (cur_percentage < 100 ? L"\r" : L"\n")
              << std::flush;
      }

      progress::prev = cur_percentage;
    }

    uint64_t fingerprint(const Vector<UCHAR>& data) {
        uint64_t result[2] = { 0 };
        const uint32_t kFingerPrint32Seed = 0xfd14deffL;
        MurmurHash3_x86_128(data.data(), data.size(), kFingerPrint32Seed, (char*)result);
        return result[0];
    }

    /**
     * enum csv files in the specified directory
     */
    Vector<String> enum_csv_dictionaries(StringRef dir) {
        Vector<String> result;
        try {
            for (const auto& entry : std::filesystem::directory_iterator(dir)) {
                if (entry.is_regular_file()) {
                    auto name = entry.path().wstring();
                    if (utils::endsWith(name, L".csv")) result.push_back(name);
                }
            }
        } catch (...) {
            THROW_RTE(L"no such directory: {}", dir);
        }
        return result;
    }

} // namespace util
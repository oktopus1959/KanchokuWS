#pragma once

#include "string_utils.h"

#define GE_ZEOR_OR(x, y) ((x) >= 0 ? (x) : (y))

// 種々の utils を収める
namespace utils {
    template <typename T, size_t SIZE>
    inline size_t array_length(const T(&)[SIZE])
    {
        return SIZE;
    }

    template <typename T>
    inline T minimum(T a, T b) { return a < b ? a : b; }

    template <typename T>
    inline std::vector<T> make_one_element_vector(const T& v) {
        std::vector<T> vec;
        vec.push_back(v);
        return vec;
    }

    template <typename T>
    inline std::set<T> make_one_element_set(const T& v) {
        std::set<T> st;
        st.insert(v);
        return st;
    }

    template <typename T, typename U>
    inline bool contains(const std::map<T, U>& m, const T& v) {
        return m.find(v) != m.end();
    }

    template <typename T>
    inline bool contains(const std::set<T>& m, const T& v) {
        return m.find(v) != m.end();
    }

    template <typename T>
    inline bool contains(const std::vector<T>& m, const T& v) {
        return std::find(m.begin(), m.end(), v) != m.end();
    }

    template <typename T>
    inline std::set<T> make_intersection(const std::set<T>& st1, const std::set<T>& st2) {
        std::set<T> result;
        for (const auto& v : st1) {
            if (contains(st2, v)) result.insert(v);
        }
        return result;
    }

    template <typename T>
    inline void apply_intersection(std::set<T>& st1, const std::set<T>& st2) {
        std::vector<T> list(st1.begin(), st1.end());
        for (const auto& v : list) {
            if (!contains(st2, v)) st1.erase(v);
        }
    }

    template <typename T>
    inline void apply_union(std::set<T>& st1, const std::set<T>& st2) {
        std::copy(st2.begin(), st2.end(), std::inserter(st1, st1.end()));
    }

    template <typename T>
    inline void append(std::vector<T>& vec1, const std::vector<T>& vec2) {
        std::copy(vec2.begin(), vec2.end(), std::back_inserter(vec1));
    }

    template <typename T>
    inline void append(std::vector<T>& vec1, const std::set<T>& vec2) {
        std::copy(vec2.begin(), vec2.end(), std::back_inserter(vec1));
    }

    template <typename T>
    inline void erase(std::vector<T>& vec, const T& val) {
        auto iter = std::find(vec.begin(), vec.end(), val);
        if (iter != vec.end()) vec.erase(iter);
    }

    // insert front
    template<typename T>
    inline void insert_front(std::vector<T>& dest, const T& val) {
        dest.insert(dest.begin(), val);
    }

    template<typename T, typename Iter>
    inline void insert_front(std::vector<T>& dest, Iter iter1, Iter iter2) {
        dest.insert(dest.begin(), iter1, iter2);
    }

    template<typename T>
    inline void insert_front(std::vector<T>& dest, const std::vector<T>& src) {
        dest.insert(dest.begin(), src.begin(), src.end());
    }

    // insert at
    template<typename T>
    inline void insert_at(std::vector<T>& dest, size_t pos, const T& val) {
        dest.insert(dest.begin() + pos, val);
    }

    template<typename T, typename Iter>
    inline void insert_at(std::vector<T>& dest, size_t pos, Iter iter1, Iter iter2) {
        dest.insert(dest.begin() + pos, iter1, iter2);
    }

    template<typename T>
    inline void insert_at(std::vector<T>& dest, size_t pos, const std::vector<T>& src) {
        dest.insert(dest.begin() + pos, src.begin(), src.end());
    }

    /**
    * find
    */
    template <typename T>
    inline const T* find(const std::vector<T>& container, const T& val) {
        for (const auto& x : container) { if (x == val) return &x; }
        return 0;
    }

    template<typename T, typename F>
    inline const T* find(const std::vector<T>& container, F pred) {
        for (const auto& x : container) { if (pred(x)) return &x; }
        return 0;
    }

    template<typename T>
    inline int find_index(const std::vector<T>& container, const T& val) {
        for (size_t i = 0; i < container.size(); ++i) {
            if (container[i] == val) return (int)i;
        }
        return -1;
    }

    inline void trim_and_copy(const std::vector<String>& src, std::vector<String>& dest) {
        for (const auto& s : src) {
            auto ss = utils::strip(s);
            if (!ss.empty() && ss[0] != '#') dest.push_back(ss);
        }
    }

    // safe_front()
    inline String safe_front(const std::vector<String>& vec) {
        if (vec.empty()) {
            return L"";
        } else {
            return vec.front();
        }
    }

    inline MString safe_front(const std::vector<MString>& vec) {
        if (vec.empty()) {
            return MString();
        } else {
            return vec.front();
        }
    }

    inline String safe_front(const std::set<String>& st) {
        if (st.empty()) {
            return L"";
        } else {
            return *st.begin();
        }
    }

    inline MString safe_front(const std::set<MString>& st) {
        if (st.empty()) {
            return MString();
        } else {
            return *st.begin();
        }
    }

    template<typename T, typename U>
    inline U safe_get(const std::map<T, U>& dic, const T& key) {
        auto iter = dic.find(key);
        return (iter != dic.end()) ? iter->second : U();
    }

    template<typename T, typename U>
    inline U safe_get(const std::map<T, U>& dic, const T& key, const U& defVal) {
        auto iter = dic.find(key);
        return (iter != dic.end()) ? iter->second : defVal;
    }

    template<typename T, typename U>
    inline void get_keys_and_values(const std::map<T, U>& map, std::vector<T>& keys, std::vector<U>& values) {
        for (const auto& pair : map) {
            keys.push_back(pair.first);
            values.push_back(pair.second);
        }
    }

    inline time_t getSecondsFromEpochTime() {
        return time(nullptr);
    }

    template<class T>
    struct VectorUtil {
        /**
        * transform to another type of container
        */
        template <typename C, typename F>
        static inline std::vector<T> transform(const C& container, F func) {
            std::vector<T> result;
            std::transform(container.begin(), container.end(), std::back_inserter(result), func);
            return result;
        }

    };

    struct MapUtil {
        template<class K, class V>
        static inline bool containsKey(std::map<K, V> map, const K& key) {
            auto iter = map.find(key);
            return iter != map.end();
        }

        template<class K, class V>
        static inline std::pair<bool, V> get(std::map<K, V> map, const K& key) {
            auto iter = map.find(key);
            return iter != map.end() ? std::pair<bool, V>{true, iter->second()} : std::pair<bool, V>{false, V()};
        }

        template<class K, class V>
        static inline const V& getOrElse(std::map<K, V> map, const K& key, const V& elseVal) {
            auto iter = map.find(key);
            return iter != map.end() ? iter->second() : elseVal;
        }
    };
} // namespace utils


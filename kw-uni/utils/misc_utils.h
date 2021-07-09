#pragma once

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

    /**
    * filter
    */
    template <typename T, typename P>
    inline std::vector<T> filter(const std::vector<T>& container, P predicate) {
        std::vector<T> result;
        std::copy_if(container.begin(), container.end(), std::back_inserter(result), predicate);
        return result;
    }

    template <typename T, typename P>
    inline std::set<T> filter(const std::set<T>& container, P predicate) {
        std::set<T> result;
        std::copy_if(container.begin(), container.end(), std::inserter(result, result.end()), predicate);
        return result;
    }

    /**
    * filter not_empty string
    */
    //template <typename C>
    //inline C filter_not_empty(const C& list) {
    //     return filter(list, [](wstring s) { return !s.empty(); });
    //}

    template <typename T>
    inline std::vector<T> filter_not_empty(const std::vector<T>& list) {
         return filter(list, [](T s) { return !s.empty(); });
    }

    /**
    * transform to same type of container
    */
    template <typename C, typename F>
    inline C transform(const C& container, F func) {
        C result;
        std::transform(container.begin(), container.end(), std::inserter(result, result.end()), func);
        return result;
    }

    /**
    * transform and append (vector)
    */
    template <typename T, typename U, typename F>
    inline void transform_append(const std::vector<T>& src, std::vector<U>& dest, F func) {
        std::transform(src.begin(), src.end(), std::back_inserter(dest), func);
    }

    /**
    * transform and copy to other type of container
    */
    template <typename C, typename D, typename F>
    inline void transform_append(const C& src, D& dest, F func) {
        std::transform(src.begin(), src.end(), std::inserter(dest, dest.end()), func);
    }

    /**
    * transform and insert (vector)
    */
    template <typename T, typename Iter, typename F>
    inline void transform_insert(const std::vector<T>& src, Iter iter, F func) {
        std::transform(src.begin(), src.end(), std::inserter(iter), func);
    }

    /**
    * to_vector
    */
    template <typename T, typename U, typename V, template<typename, typename, typename> typename C>
    inline std::vector<T> to_vector(const C<T, U, V> &container) {
        return std::vector<T>(container.begin(), container.end());
    }

    /**
    * to_ptr_vector
    */
    inline std::vector<const char_t*> to_ptr_vector(const std::vector<wstring>& list) {
        std::vector<const char_t*> result;
        transform_append(list, result, [](const wstring& s) { return s.c_str(); });
        return result;
    }

    // safe_front()
    inline tstring safe_front(const std::vector<tstring>& vec) {
        if (vec.empty()) {
            return _T("");
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

    inline tstring safe_front(const std::set<tstring>& st) {
        if (st.empty()) {
            return _T("");
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
        return (iter != dic.end()) ? iter->second : T();
    }

    inline time_t getSecondsFromEpochTime() {
        return time(nullptr);
    }

    inline tstring boolToString(bool flag) {
        return flag ? _T("True") : _T("False");
    }

} // namespace utils


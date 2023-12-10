#pragma once

#include "std_utils.h"

// transform, filter 系の utils を収める
namespace utils {
    /**
    * copy_if
    */
    template <typename T, typename P>
    inline void copy_if(const std::vector<T>& src, std::vector<T>& dest, P predicate) {
        std::copy_if(src.begin(), src.end(), std::back_inserter(dest), predicate);
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

    template <typename T, typename U, typename P>
    inline std::map<T, U> filter(const std::map<T, U>& container, P predicate) {
        std::map<T, U> result;
        std::copy_if(container.begin(), container.end(), std::inserter(result, result.end()), predicate);
        return result;
    }

    /**
    * filter not_empty string
    */
    //template <typename C>
    //inline C filter_not_empty(const C& list) {
    //     return filter(list, [](String s) { return !s.empty(); });
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
    * select
    */
    template <typename T, typename S, typename F>
    std::vector<T> select(const std::vector<S>& src, F func) {
        std::vector<T> result;
        for (const auto& x : src) {
            result.push_back(func(x));
        }
        return result;
    }

    /**
    * slice vector
    */
    template <typename T>
    inline std::vector<T> slice(const std::vector<T>& vec, size_t start, size_t num) {
        std::vector<T> result;
        size_t last = start + num;
        std::copy(vec.begin() + std::min(start, vec.size()), vec.begin() + std::min(last, vec.size()), std::back_inserter(result));
        return result;
    }

    template <typename T>
    inline std::vector<T> take(const std::vector<T>& vec, size_t num) {
        return slice(vec, 0, num);
    }

    template <typename T>
    inline std::vector<T> skip(const std::vector<T>& vec, size_t num) {
        size_t n = std::min(num, vec.size());
        return slice(vec, n, vec.size() - n);
    }

    /**
    * to_vector
    */
    template <typename T, typename U, typename V, template<typename, typename, typename> typename C>
    inline std::vector<T> to_vector(const C<T, U, V>& container) {
        return std::vector<T>(container.begin(), container.end());
    }

    /**
    * to_ptr_vector
    */
    inline std::vector<const wchar_t*> to_ptr_vector(const std::vector<String>& list) {
        std::vector<const wchar_t*> result;
        transform_append(list, result, [](const String& s) { return s.c_str(); });
        return result;
    }

} // namespace utils


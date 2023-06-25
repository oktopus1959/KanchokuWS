#pragma once

#include "std_utils.h"

template<typename T>
class SafePtr {
    T* _ptr;

public:
    SafePtr(T* p) : _ptr(p) { }
    SafePtr(const UniqPtr<T>& p) : _ptr(p.get()) { }
    SafePtr(const SharedPtr<T>& p) : _ptr(p.get()) { }

    T& operator*() const { return *_ptr; }
    T* operator->() const { return _ptr; }
    operator bool() const { return _ptr != nullptr; }
    SafePtr& operator=(const UniqPtr<T>& p) { _ptr = p.get(); return *this; }
    SafePtr& operator=(const SharedPtr<T>& p) { _ptr = p.get(); return *this; }
};

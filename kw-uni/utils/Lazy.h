#pragma once

#include "std_utils.h"

namespace util {
    template<class T>
    class Lazy {
        std::function<T* ()> _creator;

        std::function<SharedPtr<T> ()> _sharedCreator;

        SharedPtr<T> _impl;

    public:
        Lazy() { }

        // 1引数new用コンストラクタ
        template<class A1>
        Lazy(A1 arg) : _creator([arg]() {return new T(arg);}) { }

        // 2引数new用コンストラクタ
        template<class A1, class A2>
        Lazy(A1 arg1, A2 arg2) : _creator([arg1, arg2]() {return new T(arg1, arg2);}) { }

        // 3引数new用コンストラクタ
        template<class A1, class A2, class A3>
        Lazy(A1 arg1, A2 arg2, A3 arg3) : _creator([arg1, arg2, arg3]() {return new T(arg1, arg2, arg3);}) { }

        SharedPtr<T> operator()() {
            if (!_impl) {
                if (_sharedCreator) {
                    _impl = _sharedCreator();
                } else {
                    _impl.reset(_creator ? _creator() : new T());
                }
            }
            return _impl;
        }

        void setCreator(const std::function<T* ()>& creator) {
            _creator = creator;
        }

        void setCreator(const std::function<SharedPtr<T> ()>& sharedCreator) {
            _sharedCreator = sharedCreator;
        }
    };

}

#define LAZY_INITIALIZE(X, C)  X.setCreator([this]() { return C; })

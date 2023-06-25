#pragma once

#include <memory>
#include <sstream>
#include <map>
#include <set>
#include <vector>
#include <deque>
#include <queue>

template<class T>
using SharedPtr = std::shared_ptr<T>;

//template<class T>
//using MakeShared = std::make_shared<T>;
#define MakeShared std::make_shared

template<class T>
using UniqPtr = std::unique_ptr<T>;

//template<class T>
//using MakeUniq = std::make_unique<T>;
#define MakeUniq std::make_unique

template<class T>
using AutoPtr = std::unique_ptr<T>;

//template<class T>
//using MakeAuto = std::make_unique<T>;
#define MakeAuto(T) std::make_unique<T>

using String = std::wstring;
typedef const String& StringRef;

using StringStream = std::wstringstream;

template<class T>
using Vector = std::vector<T>;

template<class T>
using Deque = std::deque<T>;

template<class T, class C = std::less<T>>
using PriorityQueue = std::priority_queue<T, std::vector<T>, C>;

template<class T, class V>
using Map = std::map<T, V>;

#define Function std::function

//#include <filesystem>
//namespace fs = std::filesystem;

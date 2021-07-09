/** 
 * @file  array_size.hpp
 * @brief ARRAY_SIZE マクロ/テンプレート定義
 *
 * @author OKA Toshiyuki (LangEdge, Inc.)
 * @date 2003-06-30
 * @version $Id: array_size.hpp,v 1.1 2005/11/11 05:42:39 exoka Exp $
 *
 * This file is in the PUBLIC DOMAIN.
 */

#ifndef LANGEDGE_ARRAY_SIZE_HPP
#define LANGEDGE_ARRAY_SIZE_HPP

#if (defined(_MSC_VER) && _MSC_VER <= 1300) || (defined(__GNUC__) && __GNUC__ < 3)
/**
 * @def ARRAY_SIZE(a)
 * @brief 配列の要素数を返すマクロ/テンプレート
 *
 * VC++ 6/7.0 or g++ 2.x の場合はマクロで実装れさ、a がポインタの場合でもエラーにならないことに注意。<br>
 * VC++ 7.1 or g++ 3.x の場合は関数テンプレートで実装され、a がポインタの場合はコンパイルエラーになる。
 */
#define ARRAY_SIZE( a ) (sizeof(a)/sizeof(a[0]))

#else
/** 配列のサイズを返す関数テンプレート (g++ 3.x).
 */
#include <stddef.h>
template<class T, int N> inline size_t ARRAY_SIZE(T(&)[N]) { return N; }

#endif // _MSC_VER

#endif // LANGEDGE_ARRAY_SIZE_HPP

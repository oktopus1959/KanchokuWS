/** 
 * @file  pathname.hpp
 * @brief パス名を扱うクラス (simple version)
 *
 * @author OKA Toshiyuki (LangEdge, Inc.)
 * @date 2003-10-15
 * @version $Id: pathname.hpp,v 1.6 2006/02/06 02:57:08 oka Exp $
 *
 * Copyright (C) 2003-2005 LangEdge, Inc. All rights reserved.
 */

/*
 * 使用・複製・修正・配布に関する許諾条件:
 * 本ファイルは、以下の５条件が全て遵守される場合に限り、公序良俗に反しな
 * い範囲で、商用・非商用を問わず、いかなる個人または組織に対しても対価を
 * 支払うことなく、誰でも自由に、使用・複製・修正・配布の全部または一部の
 * 行為を行うことができる。
 * 
 * (1) この「使用・複製・修正・配布に関する許諾条件」にある文言を一切修正
 *     しないこと。
 * 
 * (2) ファイルの先頭部に記述してある author行および Copyright行または、
 *     そのいずれかを削除したり修正したりしないこと。ただし、author行また
 *     は Copyright行で記述されている当の個人または組織 (以後、「著作者」
 *     と呼ぶ) は、自身に関する記述に限り、削除したり修正したりすることが
 *     できる。
 * 
 * (3) ファイルに何らかの修正を加えた場合には、修正した個人または組織に関
 *     する author行と Copyright 行を追加することができる。その場合、追加
 *     された記述に対しても (2)項の規定が適用される。
 * 
 * (4) 本ファイルを使用、複製、修正または配布した結果として、いかなる種類
 *     の損失、損害または不利益が発生しても、「著作者」がその責を一切負わ
 *     ないことに同意し、かつ「著作者」にその責を一切負わせないこと。
 * 
 * (5) 本ファイルをコンパイラに適用して得られたバイナリオブジェクトは、そ
 *     のコンパイルを実行した個人または組織の所有物であり、「著作者」との
 *     間には、一切の権利・義務関係が存在しないことに同意する。
 */

#ifndef LANGEDGE_PATH_NAME_HPP
#define LANGEDGE_PATH_NAME_HPP

#include <string>
#include <stdlib.h>
#ifdef __unix__
#include <limits.h>
#endif

#include "ctypeutil.hpp"

#if defined(_WIN32) && defined(_MBCS)
#include "mbs_traits.hpp"
#endif

namespace langedge {

/// パスに関わる文字(列)型を定義
#if defined(_WIN32) && defined(_UNICODE)
#include <tchar.h>
typedef wchar_t PathCharType;
typedef std::wstring PathStringType;
#define PATH_TEXT(x) _T(x)
#else
typedef char PathCharType;
typedef std::string PathStringType;
#define PATH_TEXT(x) x
#endif

const size_t STRING_NPOS = PathStringType::npos;

/** パスに関わる定数を定義するためのテンプレート.
 * ヘッダファイルの中で static メンバとして定義したいので
 * template クラスにする。後で、インスタンス化したものを
 * 継承する形で使用する。
 */
template<int DUMMY>
struct PathNameConstants {
    /// パス区切り文字
    static const PathCharType PATH_DELIMITER;
};

#ifdef _WIN32
template<int DUMMY> const PathCharType PathNameConstants<DUMMY>::PATH_DELIMITER = PATH_TEXT('\\');
#else
template<int DUMMY> const PathCharType PathNameConstants<DUMMY>::PATH_DELIMITER = PATH_TEXT('/');
#endif

/** マルチバイトなパス名を扱うクラス.
 * パス名に対して、結合や、親ディレクトリ取得などの操作を行う。<br>
 * ShiftJIS環境でパス名を構成する2バイト文字の一部として 0x5C ('\\')
 * が使われていても、それをパス区切りとは見なさない。
 */
class PathName : public PathNameConstants<0> {
public:
	/** パス区切り文字を返す.
     * Windows環境なら '\\' を返し、それ以外では '/' を返す
     */
	static PathCharType delimChar()
	{
		return PATH_DELIMITER;
	}

	/** パス区切り文字列を返す.
     * Windows環境なら "\" を返し、それ以外では "/" を返す
     */
	static const PathStringType& delimStr()
	{
		static PathCharType delim[] = { PATH_DELIMITER, 0 };
		static PathStringType s_delim( delim );
		return s_delim;
	}

public:
    /// パス長の最大値
#ifdef _WIN32
    enum { PATHNAME_MAXLEN = _MAX_PATH };
#else
    enum { PATHNAME_MAXLEN = PATH_MAX };
#endif

    // パス名のバッファ
    class BufferType {
    public:
        PathCharType* operator&() { return &m_buffer[0]; }
        const PathCharType* operator&() const { return &m_buffer[0]; }
        size_t size() const { return PATHNAME_MAXLEN; }
    private:
        PathCharType m_buffer[PATHNAME_MAXLEN+1];
    };

private:
    PathStringType m_pathname;

private:
    // 検索結果位は、妥当な文字列位置か ( npos でないか)
    static bool isFound( size_t pos )
    {
        return pos != STRING_NPOS;
    }

    // 与えられたパスから、逆順にパス区切り文字を探す (endpos=開始位置) (@pre endpos > 0)
    static size_t rfindPathDelimiter( const PathStringType& path,
                                      size_t endpos = STRING_NPOS )
    {
#if defined(_WIN32) && defined(_MBCS)
        const PathCharType* startp = path.c_str();
        size_t pos = path.rfind(PATH_DELIMITER, endpos);
        while (pos > 0 && isFound(pos) &&
               !sjis_traits::is_valid_point(startp, startp+pos))
        {
            pos = path.rfind(PATH_DELIMITER, pos);
        }
        return pos;
#else
        return path.rfind( PATH_DELIMITER, endpos );
#endif
    }

    // 逆順にパス区切り文字を探す (endpos=開始位置) (@pre endpos > 0)
    size_t rfindPathDelimiter( size_t endpos = STRING_NPOS ) const
    {
        return rfindPathDelimiter( m_pathname, endpos );
    }

    //----------------------------------------------------------------------
public:
    /// デフォルトコンストラクタ
    PathName() { }

    /** パス名(Char*)で初期化されるコンストラクタ.
     * @param path パス名 (Char*)
     */
    explicit PathName( const PathCharType* path ) : m_pathname( path ) { }

    /** パス名(String)で初期化されるコンストラクタ.
     * @param path パス名 (String)
     */
    explicit PathName( const PathStringType& path ) : m_pathname( path ) { }

    /// コピーコンストラクタ
    PathName( const PathName& path ) : m_pathname( path.toString() ) { }

    //----------------------------------------------------------------------
    /// パス名(Char*)の代入
    const PathName& operator=( const PathCharType* path )
    {
        m_pathname = path;
        return *this;
    }

    /// パス名(String)の代入
    const PathName& operator=( const PathStringType& path )
    {
        m_pathname = path;
        return *this;
    }

    /// パス名(PathName)の代入
    const PathName& operator=( const PathName& path )
    {
        m_pathname = path.toString();
        return *this;
    }

    //----------------------------------------------------------------------
    /** パス名(Char*)を付加するオペレータ.
     * 必要なら、間にパスデリミタを挿入する。自身が空文字列なら引数のパス名がコピーされるだけ。
     * @param path 付加されるパス名 (Char*)
     * @return pathが付加された自身への参照
     */
    PathName& operator+=( const PathCharType* path )
    {
        return operator+=( PathStringType(path) );
    }

    /** パス名(String)を付加するオペレータ.
     * 必要なら、間にパスデリミタを挿入する。自身が空文字列なら引数のパス名がコピーされるだけ。
     * @param path 付加されるパス名 (String)
     * @return pathが付加された自身への参照
     */
    PathName& operator+=( const PathStringType& path )
    {
        if (!path.empty()) {
            if (isTailCharPathDelimiter() && path[0] == PATH_DELIMITER) {
                m_pathname += path.substr(1);
            }
            else {
                if (!empty() && !isTailCharPathDelimiter() && path[0] != PATH_DELIMITER) {
                    m_pathname += PATH_DELIMITER;
                }
                m_pathname += path;
            }
        }
        return *this;
    }

    /** パス名(PathName)を付加するオペレータ.
     * 必要なら、間にパスデリミタを挿入する。自身が空文字列なら引数のパス名がコピーされるだけ。
     * @param path 付加されるパス名 (PathName)
     * @return pathが付加された自身への参照
     */
    PathName& operator+=( const PathName& path )
    {
        return operator+=( path.toString() );
    }

    //----------------------------------------------------------------------
    /** 与えられたパス名(Char*)を付加した結果のパス名を返す.
     * 自身は変化しない。
     * @param path 付加するパス名 (Char*)
     * @return pathが付加された新しいパス名
     */
    PathName operator+( const PathCharType* path ) const
    {
        return operator+( PathStringType(path) );
    }

    /** 与えられたパス名(String)を付加した結果のパス名を返す.
     * 自身は変化しない。
     * @param path 付加するパス名 (String)
     * @return pathが付加された新しいパス名
     */
    PathName operator+( const PathStringType& path ) const
    {
        PathName result( toString() );
        result += path;
        return result;
    }

    /** 与えられたパス名(PathName)を付加した結果のパス名を返す.
     * 自身は変化しない。
     * @param path 付加するパス名 (PathName)
     * @return pathが付加された新しいパス名
     */
    PathName operator+( const PathName& path ) const
    {
        return operator+( path.toString() );
    }

    //----------------------------------------------------------------------
    /// 等値比較
    bool operator==( const PathCharType* path ) const
    {
        return m_pathname == path;
    }

    /// 等値比較
    bool operator==( const PathStringType& path ) const
    {
        return m_pathname == path;
    }

    /// 等値比較
    bool operator==( const PathName& path ) const
    {
        return m_pathname == path.toString();
    }

    /// 不等比較
    bool operator!=( const PathCharType* path ) const
    {
        return m_pathname != path;
    }

    /// 不等比較
    bool operator!=( const PathStringType& path ) const
    {
        return m_pathname != path;
    }

    /// 不等比較
    bool operator!=( const PathName& path ) const
    {
        return m_pathname != path.toString();
    }

    //----------------------------------------------------------------------
    /// 空パスか
    bool empty() const
    {
        return m_pathname.empty();
    }

    /// 長さを得る
    size_t length() const
    {
        return m_pathname.length();
    }

    /// パス名を C-like な文字列で得る
    const PathCharType* c_str() const
    {
        return m_pathname.c_str();
    }

    /// パス名を C-like な文字列で得る
    operator const PathCharType* () const
    {
        return c_str();
    }

    /// パス名を保持する String メンバへの参照を得る
    const PathStringType& toString() const
    {
        return m_pathname;
    }

    /// パス名を保持する String メンバへの参照を得る
    operator const PathStringType& () const
    {
        return toString();
    }

    /// パス名のn要素目を得る
    PathCharType getCharAt( size_t n ) const
    {
        return m_pathname[n];
    }

public:
    //----------------------------------------------------------------------
private:
    // パスは、先頭がDOS形式のルートディレクトリか (@pre length() >= 3)
    static bool isDosRootDirectoryHead( const PathStringType& path )
    {
        return CtypeUtil::is8bitChar( path[0] )
            && CtypeUtil::isAlpha( path[0] )
            && path[1] == PATH_TEXT(':')
            && path[2] == PATH_DELIMITER;
    }

public:
    /** 与えられたパスは、DOS形式のルートディレクトリか.
     * DOS形式: ドライブ文字:\\
     * @param path パス名
     * @retval true DOS形式のルートディレクトリである
     * @retval false DOS形式のルートディレクトリではない
     */
    static bool isDosRootDirectory( const PathStringType& path )
    {
        return path.length() == 3 && isDosRootDirectoryHead(path);
    }

    /** パスは、DOS形式のルートディレクトリか.
     * DOS形式: ドライブ文字:\\
     * @retval true DOS形式のルートディレクトリである
     * @retval false DOS形式のルートディレクトリではない
     */
    bool isDosRootDirectory() const
    {
        return isDosRootDirectory( toString() );
    }

    /** 与えられたパスは、ルートディレクトリか.
     * DOS形式(C:\\ など)も含む。
     * @param path パス名
     * @retval true ルートディレクトリである
     * @retval false ルートディレクトリではない
     */
    static bool isRootDirectory( const PathStringType& path )
    {
        return (path.length() == 1 && path[0] == PATH_DELIMITER)
            || isDosRootDirectory( path );
    }

    /** 自パスは、ルートディレクトリか.
     * DOS形式(C:\\ など)も含む。
     * @retval true ルートディレクトリである
     * @retval false ルートディレクトリではない
     */
    bool isRootDirectory() const
    {
        return isRootDirectory( toString() );
    }

    /** 与えられたパスは、絶対パス形式か.
     * 先頭がパスデリミタで始まるか。(DOS形式(C:\\ など)も含む)
     * @param path パス名
     * @retval true 絶対パス形式である
     * @retval false 絶対パス形式ではない
     */
    static bool isAbsolutePath( const PathStringType& path )
    {
        return (path.length() >= 1 && path[0] == PATH_DELIMITER)
            || (path.length() >= 3 && isDosRootDirectoryHead(path));
    }

    /** 与えられたパスは、絶対パス形式か.
     * 先頭がパスデリミタで始まるか。(DOS形式(C:\\ など)も含む)
     * @retval true 絶対パス形式である
     * @retval false 絶対パス形式ではない
     */
    bool isAbsolutePath() const
    {
        return isAbsolutePath( toString() );
    }

    //----------------------------------------------------------------------
    /** 与えられたパス名の末尾がパス区切り文字か.
     * @param path パス名
     * @retval true パス区切り文字である
     * @retval false パス区切り文字ではない
     */
    static bool isTailCharPathDelimiter( const PathStringType& path )
    {
        if (path.empty()) return false;

        const PathCharType* startp = path.data();
        const PathCharType* tailp = startp + path.length()-1;
#if defined(_WIN32) && defined(_MBCS)
        return *tailp == PATH_DELIMITER && sjis_traits::is_valid_point(startp, tailp);
#else
        return *tailp == PATH_DELIMITER;
#endif
    }

    /** パス名の末尾がパス区切り文字か.
     * @retval true パス区切り文字である
     * @retval false パス区切り文字ではない
     */
    bool isTailCharPathDelimiter() const
    {
        return isTailCharPathDelimiter( toString() );
    }

    //----------------------------------------------------------------------
    /** 与えられたパス名の末尾にパス区切り文字を付加したパスを返す.
     * すでに末尾にパス区切り文字が付加されてあれば何もしない
     * @param path パス名
     * @return パス区切り文字を付加したパス
     */
    static PathName appendPathDelimiter( const PathStringType& path )
    {
        PathStringType tmppath = path;
        if (!isTailCharPathDelimiter(tmppath)) {
            tmppath += PATH_DELIMITER;
        }
        return PathName(tmppath);
    }

    /** 自パス名の末尾にパス区切り文字を付加したパスを返す.
     * すでに末尾にパス区切り文字が付加されてあれば何もしない
     * @return パス区切り文字を付加したパス
     */
    PathName appendPathDelimiter() const
    {
        return appendPathDelimiter( toString() );
    }

    //----------------------------------------------------------------------
    /** 与えられたパス名の末尾のパス区切りを削除したパスを返す.
     * ルートディレクトリ以外のパスで、末尾にパス区切りが付加されていた場合、
     * その文字を削除する。
     * @param path パス名
     * @return 末尾のパス区切り文字を削除したパス
     */
    static PathName chopPathDelimiter( const PathStringType& path )
    {
        PathStringType tmppath = path;
        if (isTailCharPathDelimiter(tmppath) && !isRootDirectory(tmppath)) {
            tmppath.resize( tmppath.length()-1 );
        }
        return PathName(tmppath);
    }

    /** 自パス名の末尾のパス区切りを削除したパスを返す.
     * ルートディレクトリ以外のパスで、末尾にパス区切りが付加されていた場合、
     * その文字を削除する。
     * @return 末尾のパス区切り文字を削除したパス
     */
    PathName chopPathDelimiter() const
    {
        return chopPathDelimiter( toString() );
    }

    //---------------------------------------------------------------------
    /** 与えられたパスに、拡張子を付加したパスを返す.
     * 付加する拡張子の先頭がピリオドでなければ、自動的にピリオドを挿入する。<br>
     * extension が空文字列なら、何もしない。
     * @param path パス名
     * @param extension 付加する拡張子
     * @return 拡張子を付加したパス
     */
    static PathName appendExtension( const PathStringType& path,
                                     const PathStringType& extension )
    {
        PathStringType result = path;
        if (!extension.empty()) {
            if (extension[0] != PATH_TEXT('.')) {
                result += PATH_TEXT(".");
            }
            result += extension;
        }
        return PathName(result);
    }

    /** 自パスに、拡張子を付加したパスを返す.
     * 付加する拡張子の先頭がピリオドでなければ、自動的にピリオドを挿入する。<br>
     * extension が空文字列なら、何もしない。
     * @param extension 付加する拡張子
     * @return 拡張子を付加したパス
     */
    PathName appendExtension( const PathName& extension ) const
    {
        return appendExtension( toString(), extension.toString() );
    }

    //---------------------------------------------------------------------
    /** 与えられたパスに、サフィックスを付加したパスを返す.
     * 付加するサフィックスにピリオドが含まれていなければ、
     * そのサフィックスを拡張子とみなして、自動的にピリオドを挿入する。<br>
     * ピリオドが含まれていれば、そのまま付加する。<br>
     * 例1: foo + c ⇒ foo.c <br>
     * 例2: foo + _bar.c ⇒ foo_bar.c
     * @param path パス名
     * @param suffix 付加するサフィックス
     * @return サフィックスが付加されたパス
     */
    static PathName appendSuffixAsExtension( const PathStringType& path,
                                             const PathStringType& suffix )
    {
        PathStringType result = path;
        if (!suffix.empty()) {
            if (!isFound( suffix.find(PATH_TEXT('.')) )) {
                result += PATH_TEXT(".");
            }
            result += suffix;
        }
        return PathName(result);
    }

    /** 自パスに、サフィックスを付加したパスを返す.
     * 付加するサフィックスにピリオドが含まれていなければ、
     * そのサフィックスを拡張子とみなして、自動的にピリオドを挿入する。<br>
     * ピリオドが含まれていれば、そのまま付加する。<br>
     * 例1: foo + c ⇒ foo.c <br>
     * 例2: foo + _bar.c ⇒ foo_bar.c
     * @param suffix 付加するサフィックス
     * @return サフィックスが付加されたパス
     */
    PathName appendSuffixAsExtension( const PathName& suffix ) const
    {
        return appendSuffixAsExtension( toString(), suffix.toString() );
    }

    //---------------------------------------------------------------------
    /** 与えられたパスから、拡張子を取り出す.
     * 例1: foo.c ⇒ c <br>
     * 例2: foo.bar.hoge ⇒ hoge
     * @param path パス名
     * @return 拡張子
     */
    static PathStringType extractExtension( const PathStringType& path )
    {
        size_t dotpos = path.rfind(PATH_TEXT('.'));
        if (isFound(dotpos)) {
            size_t delpos = rfindPathDelimiter( path );
            if (!isFound(delpos) || delpos < dotpos) {
                return path.substr(dotpos+1);
            }
        }
        return PathStringType( PATH_TEXT("") );
    }

    /** 自パスから、拡張子を取り出す.
     * 例1: foo.c ⇒ c <br>
     * 例2: foo.bar.hoge ⇒ hoge
     * @return 拡張子
     */
    PathStringType extractExtension() const
    {
        return extractExtension( toString() );
    }

    //---------------------------------------------------------------------
    /** 与えられたパスから、拡張子を削除したパスを返す.
     * 例1: foo.c ⇒ foo <br>
     * 例2: foo.bar.hoge ⇒ foo.bar
     * @param path パス名
     * @return 拡張子を削除したパス
     */
    static PathName removeExtension( const PathStringType& path )
    {
        PathStringType tmppath = path;
        size_t dotpos = tmppath.rfind(PATH_TEXT('.'));
        if (isFound(dotpos)) {
            size_t delpos = rfindPathDelimiter( tmppath );
            if (!isFound(delpos) || delpos < dotpos) {
                tmppath.resize(dotpos);
            }
        }
        return PathName( tmppath );
    }

    /** 自パスから、拡張子を削除したパスを返す.
     * 例1: foo.c ⇒ foo <br>
     * 例2: foo.bar.hoge ⇒ foo.bar
     * @return 拡張子を削除したパス
     */
    PathName removeExtension() const
    {
        return removeExtension( toString() );
    }

    //----------------------------------------------------------------------
    /** 与えられたパスから、ディレクトリ部を削除して、末尾のファイル名部分のパスを返す.
     * 例: /foo/bar/hoge.c ⇒ hoge.c を残す。
     * @param path パス名
     * @return 末尾のファイル名部分
     */
    static PathName extractFileName( const PathStringType& path )
    {
        PathStringType tmppath = path;
        size_t pos = rfindPathDelimiter( tmppath );
        if (isFound(pos)) {
            tmppath.erase(0, pos+1);
        }
        return PathName( tmppath );
    }

    /** 自パスから、ディレクトリ部を削除して、末尾のファイル名部分のパスを返す.
     * 例: /foo/bar/hoge.c ⇒ hoge.c を残す。
     * @return 末尾のファイル名部分
     */
    PathName extractFileName() const
    {
        return extractFileName( toString() );
    }

    //---------------------------------------------------------------------
    /** 与えられたパス名を結合する.
     * 必要なら、間にパスデリミタを挿入する。
     * 逆に、各パスの末尾にパスデリミタが付加されていてもよい。<br>
     * 先頭部のパスが空白なら、パスデリミタから始まるパス (すなわち絶対パス)
     * が生成される。
     * @param path1 先頭部
     * @param path2 2番目
     * @param path3 3番目
     * @param path4 4番目
     * @param path5 5番目
     */
    static PathName joinPathName( const PathStringType& path1,
                                  const PathStringType& path2,
                                  const PathStringType& path3 = PATH_TEXT(""),
                                  const PathStringType& path4 = PATH_TEXT(""),
                                  const PathStringType& path5 = PATH_TEXT("") )
    {
		PathName head(path1);
		if (head.empty()) head = delimStr();
        return (((head + path2) + path3) + path4) + path5;
    }

    //----------------------------------------------------------------------
    /** 与えられたパスから、下位ディレクトリ部を削除して、上位ディレクトリ部のパスを返す.
     * 指定されたレベルだけ、下位ディレクトリ部を削除する。<br>
     * 例1: /foo/bar/hoge.c, level=1 ⇒ /foo/bar <br>
     * 例2: /foo/bar/hoge.c, level=2 ⇒ /foo
     * @param path パス名
     * @param level いくつ上のものを返すかを示すレベル。1以下ならすぐ上の親ディレクトリ。
     * @return 上位ディレクトリのパス
     * @note level だけたどる途中でルートディレクトリに行き着いたら、
     *       ルートディレクトリを返す。<br>
     *       パスが相対パスの場合、途中のノード数より祖先レベルが大きいときは、
     *       空文字列を返す。<br>
     *       結果がルートディレクトリになる場合を除き、末尾にパス区切り文字は付加されない。
     */
    static PathName superDirectory( const PathStringType& path, int level = 1 )
    {
        PathStringType tmppath = chopPathDelimiter(path).toString();
        if (!tmppath.empty() && !isRootDirectory(tmppath)) {
            size_t len = tmppath.length();
            size_t endpos = len;
            if (level < 1) level = 1;
            for (int i = 0; i < level && 0 < endpos; ++i) {
                size_t pos = rfindPathDelimiter( tmppath, endpos );
                if (!isFound(pos)) {
                    // 相対パスの先頭まで行き着いてしまった
                    len = 0;
                    break;
                }
                if (pos == 0 || (pos == 2 && isDosRootDirectoryHead(tmppath))) {
                    // ルートディレクトリか?
                    len = pos + 1;
                    break;
                }
                len = pos;
                endpos = pos-1;
            }
            tmppath.resize( len );
        }
        return PathName(tmppath);
    }

    /** 自パスから、下位ディレクトリ部を削除して、上位ディレクトリ部のパスを返す.
     * 指定されたレベルだけ、下位ディレクトリ部を削除する。<br>
     * 例1: /foo/bar/hoge.c, level=1 ⇒ /foo/bar <br>
     * 例2: /foo/bar/hoge.c, level=2 ⇒ /foo
     * @param level いくつ上のものを返すかを示すレベル。1以下ならすぐ上の親ディレクトリ。
     * @return 上位ディレクトリのパス
     * @note level だけたどる途中でルートディレクトリに行き着いたら、
     *       ルートディレクトリを返す。<br>
     *       パスが相対パスの場合、途中のノード数より祖先レベルが大きいときは、
     *       空文字列を返す。<br>
     *       結果がルートディレクトリになる場合を除き、末尾にパス区切り文字は付加されない。
     */
    PathName superDirectory( int level = 1 ) const
    {
        return superDirectory( toString(), level );
    }

    /** 与えられたパスの、親ディレクトリのパスを返す.
     * @param path パス名
     * @return 親ディレクトリのパス。<br>
     *         パスに区切り文字が含まれない場合は、空文字列を返す。
     */
    static PathName parentDirectory( const PathStringType& path )
    {
        return superDirectory( path, 1 );
    }

    /** 自パスの、親ディレクトリのパスを返す.
     * @return 親ディレクトリのパス。<br>
     *         パスに区切り文字が含まれない場合は、空文字列を返す。
     */
    PathName parentDirectory() const
    {
        return parentDirectory( toString() );
    }

}; // class PathName

} // namespace langedge

#endif //LANGEDGE_PATH_NAME_HPP

// For Emacs:
// Local Variables:
// mode: C++
// tab-width:4
// End:

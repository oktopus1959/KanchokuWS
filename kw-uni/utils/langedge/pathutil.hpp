/** 
 * @file  pathutil.hpp
 * @brief ファイル/ディレクトリ操作関係のユーティリティ群
 *
 * @author OKA Toshiyuki (LangEdge, Inc.)
 * @date 2005-01-14
 * @version $Id: pathutil.hpp,v 1.5 2006/02/06 02:57:38 oka Exp $
 *
 * Copyright (C) 2005 LangEdge, Inc. All rights reserved.
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

#ifndef LANGEDGE_PATH_UTIL_HPP
#define LANGEDGE_PATH_UTIL_HPP

#ifdef __unix__
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <errno.h>
#elif defined(_WIN32)
#pragma warning ( disable : 4996 )
#include <sys/stat.h>
#include <io.h>
#include <direct.h>
#include <errno.h>
#endif

#include "array_size.hpp"
#include "tstring.hpp"
#include "pathname.hpp"
#include "exception.hpp"

namespace langedge {

//----------------------------------------------------------------------
/** ファイルが存在するか.
 * 指されたパス名を持つ、通常のファイルが存在するかどうかをチェックする。
 * @param filename 存在チェックを行うファイルのパス名 (PathStringType)
 * @retval true ファイルが存在
 * @retval false ファイルが存在しないか、または指定された名前はファイルではない
 */
inline bool isFileExistent( const PathStringType& filename )
{
#ifdef __unix__
    return access(filename.c_str(), F_OK) == 0;
#elif defined(_WIN32)
    return _taccess(filename.c_str(), 0) == 0;
#else
    assert(false);
    return false;
#endif
}

//----------------------------------------------------------------------
/** ディレクトリが存在するか.
 * 指されたパス名を持つディレクトリが存在するかどうかをチェックする。
 * @param dirname 存在チェックを行うディレクトリのパス名 (PathStringType)
 * @retval true ディレクトリが存在
 * @retval false ディレクトリが存在しないか、または指定された名前はディレクトリではない
 */
inline bool isDirExistent( const PathStringType& dirname )
{
#ifdef __unix__
    struct stat buf;
    return stat( dirname.c_str(), &buf ) == 0 && (buf.st_mode & S_IFDIR) != 0;
#elif defined(_WIN32)
    struct _stat buf;
    return _tstat( dirname.c_str(), &buf ) == 0 && (buf.st_mode & _S_IFDIR) != 0;
#else
    assert(false);
    return false;
#endif
}

//----------------------------------------------------------------------
/** ファイル削除.
 * @param filename 削除するファイルのファイル名 (PathStringType)
 * @retval 0 成功
 * @retval 非0 失敗
 */
inline int removeFile( const PathStringType& filename )
{
#ifdef _WIN32
    return _tremove( filename.c_str() );
#else
    return remove( filename.c_str() );
#endif
}

//----------------------------------------------------------------------
/** カレントディレクトリを得る.
 * @return カレントディレクトリ文字列
 */
inline PathStringType getCurrentDirectory()
{
    PathCharType buffer[PathName::PATHNAME_MAXLEN+1];
#ifdef _WIN32
    if (_tgetcwd(buffer, (int)ARRAY_SIZE(buffer)) == NULL)
#else
    if (getcwd(buffer, ARRAY_SIZE(buffer)) == NULL)
#endif
	{
        buffer[0] = 0;
    }
    return PathStringType( buffer );
}

//----------------------------------------------------------------------
/** カレントディレクトリを移動する (PathStringType文字列).
 * @param dirpath 移動先のディレクトリ文字列
 * @retval true 移動に成功した
 * @retval false 移動できなかった
 */
inline bool changeCurrentDirectory( const PathStringType& dirpath )
{
#ifdef _WIN32
    return _tchdir(dirpath.c_str()) == 0;
#else
    return chdir(dirpath.c_str()) == 0;
#endif
}

//----------------------------------------------------------------------
// フルパス変換のヘルパー.
inline PathName full_path_helper_sub( const PathName& path )
{
    if (path.empty() || path.isRootDirectory()) {
        return path;
    }
    PathName parent = path.parentDirectory();
    PathName tail = path.extractFileName();
    if (tail.empty() || tail == PATH_TEXT(".")) {
        return full_path_helper_sub(parent);
    }
    if (tail == PATH_TEXT("..")) {
        return full_path_helper_sub( parent.parentDirectory() );
    }
    return full_path_helper_sub(parent) + tail;
}

inline PathStringType full_path_helper( const PathStringType& path )
{
    PathName tmppath;

    if (!PathName::isAbsolutePath(path)) {
        PathStringType cwd = getCurrentDirectory();
        if (path.empty()) {
            return cwd;
        }
        tmppath = PathName::appendPathDelimiter(cwd);
    }
    return full_path_helper_sub( tmppath + path ).toString();
}

//----------------------------------------------------------------------
/** 与えられたパスをフルパス (正規化されたパス) に変換したパスを返す.
 * パスが絶対パスでなければ、カレントディレクリを先頭に付加する。<br>
 * 正規化に失敗した場合は、空のパスを返す。
 * @param path パス名
 * @return 正規化されたパス
 */
inline PathStringType canonicalizePath( const PathStringType& path )
{
    PathCharType pathbuf[PathName::PATHNAME_MAXLEN];
    const PathCharType* canonpath;
#ifdef _WIN32
# ifdef _UNICODE
    canonpath = _wfullpath( pathbuf, path.c_str(), PathName::PATHNAME_MAXLEN );
# else 
    canonpath = _fullpath( pathbuf, path.c_str(), PathName::PATHNAME_MAXLEN );
# endif
#else //!_WIN32
    canonpath = realpath( path.c_str(), pathbuf );
#endif //_WIN32

    if (canonpath != NULL) {
        // システムAPIによる正規化に成功
        return canonpath;
    }
    // システムAPIによる正規化に失敗したので自前の正規化を行う
    return full_path_helper( path );
}

/** 与えられたパスをフルパス (正規化されたパス) に変換したパスを返す.
 * 正規化に失敗した場合は、空のパスを返す。
 * canonicalizePath() と同じ。
 * @param path パス名
 * @return 正規化されたパス
 */
inline PathStringType fullPath( const PathCharType* path )
{
    return canonicalizePath( path );
}

//----------------------------------------------------------------------
// ディレクトリ作成のヘルパー関数
inline int make_dir_helper( const PathCharType* path )
{
    int res;
#ifdef __unix__
    res = mkdir( path, 0777 );
#elif defined(_WIN32)
    res = _tmkdir( path );
#else
    assert(false);
    return -1;
#endif
    if (res == 0 || errno == EEXIST) {
        return 0;
    }
    if (errno == ENOENT) {
        return 1;
    }
    return -1;
}

//----------------------------------------------------------------------
/** 複数階層に渡ってディレクトリを作成する.
 * @retval true 成功
 * @retval false 失敗
 */
inline bool makeDirectoryRecursively( const PathCharType* path )
{
    int res = make_dir_helper( path );
    if (res == 0) {
        return true;
    }
    if (res < 0) {
        return false;
    }
    PathName parent = PathName::parentDirectory( path );
    if (parent.empty() || parent == path || parent.isRootDirectory()) {
        return false;
    }
    if (!makeDirectoryRecursively( parent.c_str() )) {
        return false;
    }
    res = make_dir_helper( path );
    return res == 0;
}

//----------------------------------------------------------------------
/** ディレクトリを作成する.
 * 複数階層に渡ってディレクトリを作成する.
 * @param path ディレクトリパス (PathStringType 文字列)
 * @retval true 成功
 * @retval false 失敗
 */
inline bool makeDirectory( const PathStringType& path )
{
    return makeDirectoryRecursively( path.c_str() );
}

//----------------------------------------------------------------------
/** ディレクトリを削除する.
 * @param path ディレクトリパス (PathStringType 文字列)
 * @retval true 成功
 * @retval false 失敗
 */
inline bool removeDirectory( const PathStringType& path )
{
    int res;
#ifdef __unix__
    res = rmdir( path.c_str() );
#elif defined(_WIN32)
    res = _trmdir( path.c_str() );
#else
    assert(false);
    return false;
#endif
	return res == 0;
}

//----------------------------------------------------------------------
/** ディレクトリ操作関係の例外クラス.
 * 例外要因の説明やパス名の受渡しができる。
 */
class DirectoryError : public Exception {
public:
    /** ディレクトリ操作エラーコード.
     */
    enum {
        GETCWD_ERROR = 1,
        CHDIR_ERROR,
    };

public:
    /** エラーコードを与えるデフォルトコンストラクタ.
     * @param errcode エラーコード (デフォルトは -1)
     * @note エラーコードは、上位クラスから継承する getErrorCode() により取得できる。
     *       例外要因文字列は、"directory error" になる
     */
    explicit DirectoryError( int errcode = -1 )
        : Exception( errcode, "directory error" )
    {
    }

    /** 例外要因を与えるコンストラクタ.
     * @param what 例外要因を説明する文字列
     * @note エラーコードは、-1 になる。<br>
     *       例外要因文字列は、上位クラスから継承する what()
     *       メソッドにより取得できる。
     */
    explicit DirectoryError( const std::string& what )
        : Exception( -1, what )
    {
    }

    /** エラーコードと例外要因を与えるコンストラクタ.
     * @param errcode エラーコード
     * @param what 例外要因を説明する文字列
     * @note エラーコードは、上位クラスから継承する getErrorCode() により、
     *       また、例外要因文字列は、上位クラスから継承する what()
     *       メソッドにより取得できる。
     */
    explicit DirectoryError( int errcode, const std::string& what )
        : Exception( errcode, what )
    { }

    /// デストラクタ
    virtual ~DirectoryError() noexcept { }

};

//----------------------------------------------------------------------
/** カレントディレクトリの変更・自動復帰クラス.
 * コンストラクタでカレントディレクトリを保存・変更し、デストラクタで元のディレクトリに戻す。
 */
class PushDirectory {
public:
    /** コンストラクタ.
     * カレントディレクトリを保存する。
     * 必要なら、後で changeCurrentDirectory() でカレントディレクトリを移動する。
     */
    PushDirectory() { }

    /** コンストラクタ.
     * カレントディレクトリを保存し、指定されたディレクトリに移動する。
     * @param path 移動先のディレクトリ
     * @exception DirectoryError( DirectoryError::CHDIR_ERROR )
     */
    PushDirectory( const PathStringType& path )
    {
        if (!changeCurrentDirectory(path))
            throw DirectoryError( DirectoryError::CHDIR_ERROR );
    }

    /** デストラクタ.
     * 保存しておいた元のカレントディレクトリに移動する。
     */
    ~PushDirectory() { }

private:
    // カレントディレクトリを自動的に保存・復帰するためのプライベートクラス
    class PushDirBase {
    public:
        /* コンストラクタ。カレントディレクトリを保存する。
         * @exception DirectoryError( DirectoryError::GETCWD_ERROR )
         */
        PushDirBase() : m_cwd( getCurrentDirectory() ) {
            if (m_cwd.empty())
                throw DirectoryError( DirectoryError::GETCWD_ERROR );
        }

        /** デストラクタ.
         * 保存しておいた元のカレントディレクトリに移動する。
         */
        ~PushDirBase() {
            changeCurrentDirectory( m_cwd );
        }

    private:
        PathStringType m_cwd;
    };

    // 初期化時にカレントディレクトリを保存し、終了時に復帰する
    PushDirBase m_pdbase;

}; // class PushDirectory

#define PUSH_DIR(dir) try { PushDirectory __auto_push_dir(dir)
#define PUSH_DIR_T(dir) try { PushDirectory __auto_push_dir(_T(dir))
#define POP_DIR() } catch (langedge::DirectoryError&)

} // namespace langedge

#endif // LANGEDGE_PATH_UTIL_HPP

// For Emacs:
// Local Variables:
// mode: C++
// tab-width:4
// End:

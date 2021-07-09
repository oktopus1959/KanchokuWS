/** 
 * @file  exception.hpp
 * @brief 例外クラス
 *
 * @author OKA Toshiyuki (LangEdge, Inc.) <oka@langedge.com>
 * @date 2001-10-11
 * @version $Id: exception.hpp,v 1.1.1.1 2005/03/30 11:33:30 exoka Exp $
 *
 * Copyright (C) 2001-2002 LangEdge, Inc. All rights reserved.
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

#ifndef LANGEDGE_EXCEPTION_H
#define LANGEDGE_EXCEPTION_H

#include <stdexcept>
#include <string>

namespace langedge {

//----------------------------------------------------------------------
/** 例外クラス階層の基底.
 * 例外要因を説明する文字列の受渡しができる。
 */
class Exception : public std::exception {
public:
    /** エラーコードを与えるデフォルトコンストラクタ.
     * @param errcode エラーコード (デフォルトは -1)
     * @note 例外要因文字列は、"other error" になる
     */
    explicit Exception( int errcode = -1 )
        : a_errcode( errcode ),
          a_what( "other error" )
    {
    }

    /** 例外要因を与えるコンストラクタ.
     * @param what 例外要因を説明する文字列
     * @note エラーコードは、-1 になる
     */
    explicit Exception( const std::string& what ) : a_errcode(0), a_what( what ) { }

    /** エラーコードと例外要因を与えるコンストラクタ.
     * @param errcode エラーコード
     * @param what 例外要因を説明する文字列
     */
    explicit Exception( int errcode, const std::string& what )
        : a_errcode( errcode),
          a_what( what )
    { }

    /// デストラクタ
    virtual ~Exception() noexcept { }

    /** 例外要因文字列取得.
     * @return 例外要因を説明する文字列
     */
    virtual const char* what() const throw() { return a_what.c_str(); }

    /** std::string による例外要因文字列取得.
     * @return 例外要因を説明する文字列
     */
    const std::string& getErrorMessage() const throw() { return a_what; }

    /** エラーコードの取得.
     * @return エラーコード
     */
    int getErrorCode() const throw() { return a_errcode; }

private:
    int a_errcode;
    const std::string a_what;
};

} // namespace langedge

#endif // LANGEDGE_EXCEPTION_H

// For Emacs:
// Local Variables:
// mode: C++
// tab-width:4
// End:

/** 
 * @file  ctypeutil.hpp
 * @brief 自家製 ctype.h
 *
 * @author OKA Toshiyuki (LangEdge, Inc.) <oka@langedge.com>
 * @date 2001-10-22
 * @version $Id: ctypeutil.hpp,v 1.1.1.1 2005/03/30 11:33:30 exoka Exp $
 *
 * Copyright (C) 2001-2003 LangEdge, Inc. All rights reserved.
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

#ifndef LANGEDGE_CTYPE_HPP
#define LANGEDGE_CTYPE_HPP

namespace langedge {

//----------------------------------------------------------------------
/** 文字種判定関数群クラス.
 * std::isalpah() などは、呼び出されるたびに、毎回、オブジェクトを生成しているらしく、
 * 非常に遅い。使うべきではない。
 * (参考: http://www.fides.dti.ne.jp/~oka-t/cpplab-ctype.html)<br>
 * 当クラスは、std::isalpha() などの高速な代替関数を提供する。
 * <p>
 * テンプレート引数の LOCALE は、いまのところダミー。<br>
 * 0 を指定して特殊化した Ctype_Utilities<0> を使う。<br>
 * これは、 langedge::CtypeUtil と typedef されている。
 * <p>
 * 使用例:<pre>
 *     bool myIsAlpha( int ch ) {
 *         return langedge::CtypeUtil::isAlpha( ch );
 *     }</pre>
 *
 * 当クラステンプレートは、static 変数として文字種テーブル CharClassTable[]
 * の定義を含んでいるが、
 * テンプレートにしておくと、複数のファイルからインクルードされたとしても、
 * プログラム中で1か所にまとめられることが保証される。
 */
template<int LOCALE>
struct Ctype_Utilities {
    /// 文字種定義
    enum CharacterClassDef {
        CharClass_CNTRL = (1 << 0),
        CharClass_SPACE = (1 << 1),
        CharClass_BLANK = (1 << 2),
        CharClass_PRINT = (1 << 3),
        CharClass_GRAPH = (1 << 4),
        CharClass_PUNCT = (1 << 5),
        CharClass_DIGIT = (1 << 6),
        CharClass_UPPER = (1 << 7),
        CharClass_LOWER = (1 << 8),
        CharClass_ALPHA = (CharClass_UPPER + CharClass_LOWER),
        CharClass_ALNUM = (CharClass_DIGIT + CharClass_ALPHA),
        CharClass_KANA  = (1 << 9),
        CharClass_SJIS1 = (1 << 10),
        CharClass_SJIS2 = (1 << 11),
        CharClass_SJIS2_PROPER = (1 << 12),
        CharClass_EUC   = (1 << 13),
    };

    static unsigned short CharClassTable[];

    /// 8ビット文字か (0 〜 255 の範囲か)
    static inline bool is8bitChar( int ch ) {
        return ch >= 0 && ch <= 255;
    }

    // 文字種判定関数
    /// 空白文字か (ブランクとタブ)
    static inline int isBlank( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_BLANK;
    }

    /// 制御文字か
    static inline int isCntrl( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_CNTRL;
    }

    /// 図形文字か
    static inline int isGraph( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_GRAPH;
    }

    /// 印字可能文字か
    static inline int isPrint( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_PRINT;
    }

    /// 区切り文字か
    static inline int isPunct( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_PUNCT;
    }

    /// スペース文字か (改行等を含む)
    static inline int isSpace( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_SPACE;

    }

    /// 英数字か
    static inline int isAlnum( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & (CharClass_ALNUM);
    }

    /// 英文字か
    static inline int isAlpha( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_ALPHA;
    }

    /// 数字か
    static inline int isDigit( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_DIGIT;
    }

    /// 英小文字か
    static inline int isLower( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_LOWER;
    }

    /// 英大文字か
    static inline int isUpper( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_UPPER;
    }

    /// 半角カナ文字か
    static inline int isKana( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_KANA;
    }

    /// sjis 文字の第1バイト目か
    static inline int isSJIS1( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_SJIS1;
    }

    /// sjis 文字の第2バイト目か
    static inline int isSJIS2( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_SJIS2;
    }

    /** sjis 文字の第1バイトではないことが保証されているときに、
     * 第2バイト目であることが確実にわかる文字
     */
    static inline int isProperSJIS2( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_SJIS2_PROPER;
    }

    /// sjis 文字の第1バイト目または英数字か
    static inline int isWordCharSJIS( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & (CharClass_SJIS1 + CharClass_ALNUM);
    }

    /// euc 文字の第1バイト目か
    static inline int isEUC1( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_EUC;
    }

    /// euc 文字の第2バイト目か
    static inline int isEUC2( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_EUC;
    }

    /** euc 文字の第1バイトではないことが保証されているときに、
     * 第2バイト目であることが確実にわかる文字
     */
    static inline int isProperEUC2( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & CharClass_EUC;
    }

    /// euc 文字の第1バイト目または英数字か
    static inline int isWordCharEUC( int ch ) {
        return is8bitChar(ch) && CharClassTable[ ch ] & (CharClass_EUC + CharClass_ALNUM);
    }

    // 文字変換関数
    /// 英大文字に変換
    static inline int toUpper( int x ) {
        return isLower(x) ? x - 0x20 : x;
    }

    /// 英小文字に変換
    static inline int toLower( int x ) {
        return isUpper(x) ? x + 0x20 : x;
    }

    /// 文字種の短縮名を定義
    enum CharClass_ShortName {
        _CC_CTL = CharClass_CNTRL,
        _CC_SPC = CharClass_SPACE + CharClass_CNTRL,
        _CC_TAB = CharClass_SPACE + CharClass_BLANK + CharClass_CNTRL,
        _CC_WSP = CharClass_SPACE + CharClass_BLANK + CharClass_PRINT,
        _CC_PUN = CharClass_PUNCT + CharClass_PRINT + CharClass_GRAPH,
        _CC_PN2 = _CC_PUN + CharClass_SJIS2,

        _CC_DIG = CharClass_DIGIT + CharClass_PRINT + CharClass_GRAPH,
        _CC_UP2 = CharClass_UPPER + CharClass_PRINT + CharClass_GRAPH + CharClass_SJIS2,
        _CC_LO2 = CharClass_LOWER + CharClass_PRINT + CharClass_GRAPH + CharClass_SJIS2,

        _CC_SKN  = CharClass_KANA + CharClass_SJIS2,
        _CC_SKE  = _CC_SKN + CharClass_EUC,
        _CC_S2P  = CharClass_SJIS2 + CharClass_SJIS2_PROPER,
        _CC_SJP  = CharClass_SJIS1 + _CC_S2P,
        _CC_SJE  = _CC_SJP + CharClass_EUC,
        _CC_EUC  = CharClass_EUC
    };
};

//----------------------------------------------------------------------
/// 英数字以外の文字種
template<int LOCALE>
unsigned short Ctype_Utilities<LOCALE>::CharClassTable[256] = {
    _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL,  // 00 - 07
    _CC_CTL, _CC_TAB, _CC_SPC, _CC_SPC, _CC_SPC, _CC_SPC, _CC_CTL, _CC_CTL,  // 08 - 0f
    _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL,  // 10 - 1f
    _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL, _CC_CTL,  // 18 - 1f

    _CC_WSP, _CC_PUN, _CC_PUN, _CC_PUN, _CC_PUN, _CC_PUN, _CC_PUN, _CC_PUN,  // 20 - 27
    _CC_PUN, _CC_PUN, _CC_PUN, _CC_PUN, _CC_PUN, _CC_PUN, _CC_PUN, _CC_PUN,  // 28 - 2f
    _CC_DIG, _CC_DIG, _CC_DIG, _CC_DIG, _CC_DIG, _CC_DIG, _CC_DIG, _CC_DIG,  // 30 - 37
    _CC_DIG, _CC_DIG, _CC_PUN, _CC_PUN, _CC_PUN, _CC_PUN, _CC_PUN, _CC_PUN,  // 38 - 3f

    _CC_PN2, _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2,  // 40 - 47
    _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2,  // 48 - 4f
    _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2, _CC_UP2,  // 50 - 57
    _CC_UP2, _CC_UP2, _CC_UP2, _CC_PN2, _CC_PN2, _CC_PN2, _CC_PN2, _CC_PN2,  // 58 - 5f

    _CC_PN2, _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2,  // 60 - 67
    _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2,  // 68 - 6f
    _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2, _CC_LO2,  // 70 - 77
    _CC_LO2, _CC_LO2, _CC_LO2, _CC_PN2, _CC_PN2, _CC_PN2, _CC_PN2, _CC_CTL,  // 78 - 7f

    _CC_S2P, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP,  // 80 - 87
    _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP,  // 88 - 8f
    _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP,  // 90 - 97
    _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP, _CC_SJP,  // 98 - 9f

    _CC_SKN, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE,  // a0 - a7
    _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE,  // a8 - af
    _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE,  // b0 - b7
    _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE,  // b8 - bf

    _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE,  // c0 - c7
    _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE,  // c8 - cf
    _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE,  // d0 - d7
    _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE, _CC_SKE,  // d8 - df

    _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE,  // e0 - e7
    _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE,  // e8 - ef
    _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE,  // f0 - f7
    _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_SJE, _CC_EUC, _CC_EUC, _CC_CTL,  // f8 - ff
};

/** Ctype_Utilities のテンプレート引数 LOCALE にダミーの 0 を指定して特殊化したもの.
 * Ctype_Utilities<0> を直接使ってもよいが、こちらの typedef されたものを使ったほうが便利。
 */
typedef Ctype_Utilities<0> CtypeUtil;

} // namespace langedge


#endif // LANGEDGE_CTYPE_HPP

// For Emacs:
// Local Variables:
// mode: C++
// tab-width:4
// End:

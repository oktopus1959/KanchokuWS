/** 
 * @file  xsvparser.hpp
 * @brief CSV/TSV パーサ
 *
 * @author OKA Toshiyuki (LangEdge, Inc.)
 * @date 2003-06-28
 * @version $Id: xsvparser.hpp,v 1.3 2005/08/03 23:51:43 exoka Exp $
 *
 * Copyright (C) 2003 LangEdge, Inc. All rights reserved.
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

#ifndef LANGEDGE_XSV_PARSER_HPP
#define LANGEDGE_XSV_PARSER_HPP

#include <vector>
#include <string>
#include <iterator>

// work-around for VC6
namespace {
    template<class IteratorT> struct IteratorCharTypeExtractor {
        typedef typename std::iterator_traits<IteratorT>::value_type CharType;
    };
    template<> struct IteratorCharTypeExtractor<char*> {
        typedef char CharType;
    };
    template<> struct IteratorCharTypeExtractor<const char*> {
        typedef char CharType;
    };
    template<> struct IteratorCharTypeExtractor<unsigned char*> {
        typedef unsigned char CharType;
    };
    template<> struct IteratorCharTypeExtractor<const unsigned char*> {
        typedef unsigned char CharType;
    };
    template<> struct IteratorCharTypeExtractor<wchar_t*> {
        typedef wchar_t CharType;
    };
    template<> struct IteratorCharTypeExtractor<const wchar_t*> {
        typedef wchar_t CharType;
    };
}

namespace langedge {

/** CSV/TSV データの解析クラス.
 * 入力反復子 (InputIteratorT) で指定された範囲のCSV (Comma Separated Values)
 * または TSV (Tab Separated Values) データを解析し、
 * カンマまたはタブで区切られた各値を内部の文字列ベクタに格納するメソッド
 * parse() を提供する。
 * <p>
 * テンプレートパラメータの SEPARATOR の値が ',' か '\\t' かによって、
 * CSV/TSV のどちらを対象にするかが決定される。
 */
template<int SEPARATOR, class InputIteratorT>
class XSVParser {
public:
    typedef typename IteratorCharTypeExtractor<InputIteratorT>::CharType CharT;
    typedef std::basic_string<CharT> StringT;

private:
    std::vector<StringT> xsvalues;
    bool stop;
    bool fail;
    size_t length;
    size_t linecount;
    InputIteratorT nextIter;
    InputIteratorT endIter;

    // ヘルパー関数
    void renew_xsvalues( std::vector<StringT>& tokens, StringT& token, StringT& spaces, size_t maxCount ) {
        if (tokens.empty() || tokens.size() < maxCount)
            tokens.push_back(token);
        else
            tokens.back().append(1, SEPARATOR).append(token);
        token.erase();
        spaces.erase();
    }

public:
    /// コンストラクタ
    XSVParser() : stop(false), fail(false), linecount(0) { }

    /** CSV/TSV データを解析し、値を取得する.
     * CSV (Comma Separated Value) または TSV (Tab Separated Value) データを解析し、
     * カンマで区切られた値を内部の文字列ベクタに格納する。
     * 文字列ベクタは、getValues() によって取得できる。
     * <p>
     * 解析対象となる CSV/TSVデータは、2つの入力反復子によって指定される。
     * CSV/TSVファイルを直接解析するのであれば、istreambuf_iterator を入力反復子として
     * 使用することができる。あるいは、getline() で1行ずつバッファに読み出して、
     * parse( buffer, buffer+strlen(buffer) ); のような呼び出しもできる。
     * <p>
     * parse() は、入力反復子が改行コード位置または end 位置に到達した時に、
     * そこで解析を終了する。
     * ただし、改行コード位置がダブルクォートで囲まれた値の処理中であった場合は、
     * そのまま処理が続行される。<br>
     * end 位置がダブルクォートで囲まれた値の処理中であった場合、
     * そこで処理を終了するが full() あるいは stopped() は false を返す。
     * それ以外の場合は、full() あるいは stopped() は true を返す。
     * <p>
     * 処理の終了位置 (すなわち次回解析の開始位置) は、getNextIterator()
     * によって取得することができる。
     *
     * @param begin CSV/TSVデータの開始位置を示す入力反復子
     * @param end   CSV/TSVデータの終了位置を示す入力反復子
     * @param maxCount 最大CSV個数
     * @return 入力データにエラーがなかったか (succeeded() の返す値と同じ)
     */
    bool parse( InputIteratorT begin, InputIteratorT end, size_t maxCount = (size_t)INT_MAX );

    /** 解析結果の値を格納した文字列ベクタを得る.
     * @return 文字列ベクタ
     */
    const std::vector<StringT>& getValues() const {
        return xsvalues;
    }

    /** parse() の結果、1列分の値をすべて取得できたか.
     * @retval true 1列分の値をすべて取得できた
     * @retval false parse() に渡された反復子で指定された範囲のデータは、
     *               途中で終わっている。たとえば、ダブルクォートが閉じていない。
     * @note stopped() と同じ。<br>
     *       full() == true であっても、反復子で指定された範囲のデータを
     *       すべて処理したとは限らないことに注意。getLength() を見よ。
     */
    bool full() const {
        return stop;
    }

    /** parse() の結果、1列分の値をすべて取得できたか.
     * @retval true 1列分の値をすべて取得できた
     * @retval false parse() に渡された反復子で指定された範囲のデータは、
     *               途中で終わっている。たとえば、ダブルクォートが閉じていない。
     * @note full() と同じ。<br>
     *       stopped() == true であっても、反復子で指定された範囲のデータを
     *       すべて処理したとは限らないことに注意。getLength() を見よ。
     */
    bool stopped() const {
        return stop;
    }

    /** parse() は成功したか.
     * @retval true 入力の CSV/TSVデータにエラーがなかった
     * @retval false 入力の CSV/TSVデータにエラーがあった
     * @note !failed() に同じ。<br>
     *       succeeded() == true であっても full() == true とは限らないことに注意。
     */
    bool succeeded() const {
        return !fail;
    }

    /** parse() は失敗したか.
     * @retval true 入力の CSV/TSVデータにエラーがあった
     * @retval false 入力の CSV/TSVデータにエラーがなかった
     * @note !succeeded() に同じ。
     */
    bool failed() const {
        return fail;
    }

    /** 直前の parse() の呼び出しで処理された文字数を返す.
     */
    size_t getLength() const {
        return length;
    }

    /** これまで処理された行数を返す.
     * エラーが生じた行番号は、getLines() + 1 となる。
     */
    size_t getLines() const {
        return linecount;
    }

    /** 直前の parse() の呼び出しで処理を終了した位置を示す入力反復子を返す.
     * たとえば、入力反復子として istreambuf_iterator などを使用している場合は、
     * 次の parse() の呼び出しには、getNextIterator() の返す反復子を用いるとよい。
     * @return 次の入力位置を示す入力反復子
     */
    InputIteratorT getNextIterator() const {
        return nextIter;
    }

    /** エラー状態をクリアする.
     * 入力のCSV/TSVデータに誤りがあって、エラーが返った場合、解析処理を続行するには、
     * 本メソッドを呼んでそのエラー状態をクリアする必要がある。<br>
     * 本メソッドが呼ばれると、次の改行コードまで入力が読み飛ばされる。
     */
    void clearError() {
        stop = false;
        fail = false;
        xsvalues.clear();
        while (nextIter != endIter) {
            if (*nextIter++ == '\n') {
                ++linecount;
                break;
            }
        }
    }

public:
    /** 一行のみの CSV/TSV データを解析して、結果の文字列べクタを返す.
     * @param begin CSV/TSVデータの開始位置を示す入力反復子
     * @param end   CSV/TSVデータの終了位置を示す入力反復子
     * @return 文字列べクタ (入力データにエラーがあれば空ベクタを返す)
     */
    static inline const std::vector<StringT> parseLine( InputIteratorT begin, InputIteratorT end, size_t maxCount = (size_t)INT_MAX )
    {
        XSVParser<SEPARATOR, InputIteratorT> xsv;
        if (xsv.parse(begin, end, maxCount)) {
            return xsv.getValues();
        }
        else {
            return std::vector<StringT>();
        }
    }

}; // template class XSVParser

//----------------------------------------------------------------------
// parse() の実装
template<int SEPARATOR, class InputIteratorT>
inline bool XSVParser<SEPARATOR, InputIteratorT>::parse( InputIteratorT begin, InputIteratorT end, size_t maxCount )
{
    // 字句解析の状態
    enum ScannerState {
        BEFORE_VALUE,
        INNER_NAKED_VALUE,
        INNER_QUOTED_VALUE,
        AFTER_DOUBLE_QUOTE,
        AFTER_QUOTED_VALUE
        //AFTER_MAX_COUNT
    };

    endIter = end;

    std::basic_string<CharT> token;
    std::basic_string<CharT> spaces;
    ScannerState state = BEFORE_VALUE;

    if (stop || fail) {
        xsvalues.clear();
    } else if (!xsvalues.empty()) {
        // 前行の解析で、ダブルクォートが閉じられずに終わった (stop == false) ため、
        // 解析結果の値を本行に持ち越している
        token = xsvalues.back();
        xsvalues.pop_back();
        state = INNER_QUOTED_VALUE;
    }
    size_t count = 0;
    stop = false;
    fail = false;
    length = 0;

    while (!stop && !fail && begin != endIter && count < maxCount) {
        CharT ch = *begin++;
        ++length;
        switch (state) {
          case BEFORE_VALUE:
            if (ch == ' ' || ch == '\r' || (SEPARATOR != '\t' && ch == '\t')) {
                /* ignored */
            } else if (ch == '\n') {
                ++linecount;
                if (!xsvalues.empty() || !token.empty()) {
                    renew_xsvalues( xsvalues, token, spaces, maxCount );
                    stop = true;
                }
            } else if (ch == '"') {
                state = INNER_QUOTED_VALUE;
            } else if (ch == SEPARATOR) {
                renew_xsvalues( xsvalues, token, spaces, maxCount );
            } else {
                token += ch;
                state = INNER_NAKED_VALUE;
            }
            break;

          case INNER_NAKED_VALUE:
            if (ch == ' ' || ch == '\r' || (SEPARATOR != '\t' && ch == '\t')) {
                spaces += ch;
            } else if (ch == '\n') {
                ++linecount;
                renew_xsvalues( xsvalues, token, spaces, maxCount );
                stop = true;
            } else if (ch == '"') {
                fail = true;
            } else if (ch == SEPARATOR) {
                renew_xsvalues( xsvalues, token, spaces, maxCount );
                //state = xsvalues.size() >= maxCount ? AFTER_MAX_COUNT : BEFORE_VALUE;
                //if (state == AFTER_MAX_COUNT) token += ch;
                state = BEFORE_VALUE;
            } else {
                if (!spaces.empty()) {
                    token += spaces;
                    spaces.erase();
                }
                token += ch;
            }
            break;

          case INNER_QUOTED_VALUE:
            if (ch == '"') {
                state = AFTER_DOUBLE_QUOTE;
            } else {
                token += ch;
            }
            break;

          case AFTER_DOUBLE_QUOTE:
            if (ch == '"') {
                token += ch;
                state = INNER_QUOTED_VALUE;
                break;
            }

            renew_xsvalues( xsvalues, token, spaces, maxCount );
            //state = xsvalues.size() >= maxCount ? AFTER_MAX_COUNT : AFTER_QUOTED_VALUE;
            state = AFTER_QUOTED_VALUE;
            /* through */

          case AFTER_QUOTED_VALUE:
            if (ch == ' ' || ch == '\r' || (SEPARATOR != '\t' && ch == '\t')) {
                /* ignored */
            } else if (ch == '\n') {
                ++linecount;
                stop = true;
            } else if (ch == SEPARATOR) {
                //if (state == AFTER_MAX_COUNT) {
                //    token += ch;
                //} else {
                //    state = BEFORE_VALUE;
                //}
                state = BEFORE_VALUE;
            } else {
                fail = true;
            }
            break;

          //case AFTER_MAX_COUNT:
          //  if (ch == ' ' || ch == '\r' || (SEPARATOR != '\t' && ch == '\t')) {
          //      /* ignored */
          //  } else if (ch == '\n') {
          //      ++linecount;
          //      stop = true;
          //  } else {
          //      token += ch;
          //  }
          //  break;
        }
    }

    if (!stop && !fail) {
        // token を残して文字列の終わりに達した場合
        switch (state) {
          case BEFORE_VALUE:
            // SEPARATORが末尾にあるか、token が残った
            if (!xsvalues.empty() || !token.empty()) {
                renew_xsvalues( xsvalues, token, spaces, maxCount );
                stop = true;
            }
            break;

          case INNER_NAKED_VALUE:
          case AFTER_DOUBLE_QUOTE:
            renew_xsvalues( xsvalues, token, spaces, maxCount );
            stop = true;
            break;

          case AFTER_QUOTED_VALUE:
            stop = true;
            break;

          case INNER_QUOTED_VALUE:
            renew_xsvalues( xsvalues, token, spaces, maxCount );
            break;

          //case AFTER_MAX_COUNT:
          //  if (xsvalues.empty()) {
          //      renew_xsvalues(xsvalues, token, spaces, maxCount);
          //  } else if (!token.empty()) {
          //      xsvalues.back().append(token);
          //  }
          //  stop = true;
          //  break;
        }
    }

    nextIter = begin;
    return !fail;
}

//----------------------------------------------------------------------
/** CSV データの解析クラス.
 * XSVParser を SEPARATOR = ',' (カンマ) で特殊化したクラス。
 * @see XSVParser
 */
template<class InputIteratorT>
class CSVParser : public XSVParser<',', InputIteratorT> { };

/** TSV データの解析クラス.
 * XSVParser を SEPARATOR = '\\t' (タブ) で特殊化したクラス。
 * @see XSVParser
 */
template<class InputIteratorT>
class TSVParser : public XSVParser<'\t', InputIteratorT> { };

} // namespace langedge

#endif // LANGEDGE_XSV_PARSER_HPP

// For Emacs:
// Local Variables:
// mode: C++
// tab-width:4
// End:

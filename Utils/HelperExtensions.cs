using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace Utils
{
    /// <summary>
    /// 整数型およびその他数値型の拡張メソッドクラス
    /// </summary>
    public static class IntExtensions
    {
        /// <summary>
        /// 3桁ごとにカンマを挿入した文字列を返す
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string _toCommaString(this int val)
        {
            return string.Format("{0:#,0}", val);
        }

        /// <summary>
        /// 3桁ごとにカンマを挿入した文字列を返す
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string _formatComma<T>(this T val)
        {
            return string.Format("{0:#,0}", val);
        }

        /// <summary>
        /// 16進数の文字列(大文字)を返す
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string _toHexString(this int val, int nColumn = 2)
        {
            if (nColumn == 2)
                return $"{val:X2}";
            else if (nColumn == 4)
                return $"{val:X4}";
            else
                return $"{val:X8}";
        }

        /// <summary>
        /// 文字列または数値を指定幅で出力 (widht &gt; 0 なら右詰め、 width &lt; 0 なら左詰め)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="width">出力幅</param>
        /// <returns></returns>
        public static string _format<T>(this T value, int width)
        {
            return string.Format($"{{0,{width}}}", value);
        }

        /// <summary>
        /// 文字列または数値を指定幅で左詰め出力
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="width">出力幅</param>
        /// <returns></returns>
        public static string _formatLeft<T>(this T value, int width)
        {
            return string.Format($"{{0,{-width}}}", value);
        }

        /// <summary>
        /// 数値を 0 埋めして指定幅(zeroWidth)で出力 (widht &gt; 0 なら右詰め、 width &lt; 0 なら左詰め)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="zeroWidth">0埋めする幅</param>
        /// <param name="width">出力幅</param>
        /// <returns></returns>
        public static string _formatZero(this int value, int zeroWidth, int width = 0)
        {
            if (zeroWidth < 1) zeroWidth = 1;
            return string.Format($"{{0,{width}:D{zeroWidth}}}", value);
        }

        /// <summary>
        /// 数値を指定幅(hexWidth)で16進出力(先頭部は0埋め) (bLower=true なら英小文字、 widht &gt; 0 なら右詰め、 width &lt; 0 なら左詰め)
        /// <para>例:$"|{0xa2c4._formatHex(6)}|"⇒"|00A2C4|" ／ "|{0xA2C4._formatHex(6, true, -8)}|"⇒"|00a2c4  |" </para>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="zeroWidth">指定幅</param>
        /// <param name="width">出力幅</param>
        /// <returns></returns>
        public static string _formatHex(this int value, int hexWidth, bool bLower = false, int width = 0)
        {
            if (hexWidth < 1) hexWidth = 1;
            //if (width < hexWidth) width = hexWidth;
            var fmtch = bLower ? 'x' : 'X';
            return string.Format($"{{0,{width}:{fmtch}{hexWidth}}}", value);
        }

        /// <summary>
        /// 0 より大きければ自身を返し、0 以下なら defval を返す
        /// </summary>
        /// <param name="val"></param>
        /// <param name="defval"></param>
        /// <returns></returns>
        public static int _gtZeroOr(this int val, int defval)
        {
            return val > 0 ? val : defval;
        }

        /// <summary>
        /// 0 より大きければ自身を返し、0 以下なら defval を返す
        /// </summary>
        /// <param name="val"></param>
        /// <param name="defval"></param>
        /// <returns></returns>
        public static int _gtZeroOr(this int val, Func<int> deffun)
        {
            return val > 0 ? val : deffun?.Invoke() ?? 0;
        }

        /// <summary>
        /// 0 以上ならば自身を返し、0 未満なら defval を返す
        /// </summary>
        /// <param name="val"></param>
        /// <param name="defval"></param>
        /// <returns></returns>
        public static int _geZeroOr(this int val, int defval)
        {
            return val > 0 ? val : defval;
        }

        /// <summary>
        /// 0 以上ならば自身を返し、0 未満なら defval を返す
        /// </summary>
        /// <param name="val"></param>
        /// <param name="defval"></param>
        /// <returns></returns>
        public static int _geZeroOr(this int val, Func<int> deffun)
        {
            return val > 0 ? val : deffun?.Invoke() ?? 0;
        }

        /// <summary>
        ///  val が ERROR_INT_VAL でなければ true を返す
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool _isValid(this int val)
        {
            return val != Helper.ERROR_INT_VAL;
        }

        /// <summary>
        ///  val が NaN でなければ true を返す
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool _notNaN(this double val)
        {
            return !val._isNaN();
        }

        /// <summary>
        ///  val が NaN ならば true を返す
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool _isNaN(this double val)
        {
            return double.IsNaN(val);
        }

        /// <summary>
        /// カラム位置の値を文字列にする (-1 または正値ならOK、 bZeroable = true なら 0 の場合もOK; それ以外は空白を返す)
        /// </summary>
        /// <param name="col"></param>
        /// <param name="bZeroable"></param>
        /// <returns></returns>
        public static string _columnToString(this int col, bool bZeroable = false)
        {
            return col == -1 || (bZeroable && col == 0) || col > 0 ? col.ToString() : "";
        }

        /// <summary>
        /// 整数 idx が表す変数名 (A ～ Z) を返す。0=>A, 1=>B, ..., 25=>Z となる。idx が範囲外なら '$' を返す。
        /// </summary>
        public static string _toVarName(this int idx)
        {
            return Helper.getVarName(idx);
        }

        /// <summary>
        /// start から end-1 までの整数値をとる IEnumerable を返す。
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<int> _range(this int end, int start = 0)
        {
            return Enumerable.Range(start, end - start);
        }

        /// <summary>
        /// valid な値が格納されていれば、その値を返す。null なら defval を返す。
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static int _value(this int? val, int defval = 0)
        {
            return val.HasValue ? val.Value : defval;
        }

        /// <summary>
        /// valid な値が格納されていれば、その値を返す。null なら 0 を返す。
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static double _value(this double? val)
        {
            return val.HasValue ? val.Value : 0;
        }

        /// <summary>
        /// 引数と比較して大きい方を返す
        /// </summary>
        public static int _max(this int a, int b)
        {
            return a >= b ? a : b;
        }

        /// <summary>
        /// 引数と比較して大きい方を返す
        /// </summary>
        public static double _max(this double a, double b)
        {
            return a >= b ? a : b;
        }

        /// <summary>
        /// 引数と比較して大きい方を返す
        /// </summary>
        public static T _max<T>(this T a, T b) where T : IComparable
        {
            return a.CompareTo(b) >= 0 ? a : b;
        }

        /// <summary>
        /// 引数と比較して大きい方を返す
        /// </summary>
        public static int _lowLimit(this int a, int b)
        {
            return a._max(b);
        }

        /// <summary>
        /// 引数と比較して大きい方を返す
        /// </summary>
        public static double _lowLimit(this double a, double b)
        {
            return a._max(b);
        }

        /// <summary>
        /// 引数と比較して大きい方を返す
        /// </summary>
        public static T _lowLimit<T>(this T a, T b) where T : IComparable
        {
            return a._max(b);
        }

        /// <summary>
        /// 引数と比較して小さい方を返す
        /// </summary>
        public static int _min(this int a, int b)
        {
            return a <= b ? a : b;
        }

        /// <summary>
        /// 引数と比較して小さい方を返す
        /// </summary>
        public static double _min(this double a, double b)
        {
            return a <= b ? a : b;
        }

        /// <summary>
        /// 引数と比較して小さい方を返す
        /// </summary>
        public static T _min<T>(this T a, T b) where T : IComparable
        {
            return a.CompareTo(b) <= 0 ? a : b;
        }

        /// <summary>
        /// 引数と比較して小さい方を返す
        /// </summary>
        public static int _highLimit(this int a, int b)
        {
            return a._min(b);
        }

        /// <summary>
        /// 引数と比較して小さい方を返す
        /// </summary>
        public static double _highLimit(this double a, double b)
        {
            return a._min(b);
        }

        /// <summary>
        /// 引数と比較して小さい方を返す
        /// </summary>
        public static T _highLimit<T>(this T a, T b) where T : IComparable
        {
            return a._min(b);
        }
    }

    /// <summary>
    /// Boolの拡張メソッドクラス
    /// </summary>
    public static class BoolExtension
    {
        public static bool _toBool(this bool? flag)
        {
            return flag == true; 
        }

        public static bool _toSafe(this bool? flag)
        {
            return flag._toBool(); 
        }
       
        /// <summary>
        /// bool vector を '0' と '1' からなる文字列に変換して返す
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
                 
        public static string _toStringVector(this IEnumerable<bool> vector)
        {
            var sb = new StringBuilder();
            foreach (bool flag in vector) {
                sb.Append(flag ? '1' : '0');
            }
            return sb.ToString();
        }

        /// <summary>
        /// '0' と '1' からなる文字列を bool vector に変換して返す
        /// </summary>
        /// <param name="strVector"></param>
        /// <returns></returns>
        public static bool[] _toBoolVector(this string strVector)
        {
            var vector = new bool[strVector.Length];
            for (int i = 0; i < strVector.Length; ++i) {
                vector[i] = strVector[i] == '1';
            }
            return vector;
        }

    }

    /// <summary>
    /// 配列の拡張メソッドクラス
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// 単一要素の配列を作成する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T[] _toArray1<T>(this T obj)
        {
            return new T[] { obj };
        }

        public static bool _isEmpty<T>(this T[] ary)
        {
            return ary == null || ary.Length == 0;
        }

        public static bool _notEmpty<T>(this T[] ary)
        {
            return ary != null && ary.Length > 0;
        }

        /// <summary>
        /// 配列の長さを返す。 null なら 0 を返す。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ary"></param>
        /// <returns></returns>
        public static int _length<T>(this T[] ary)
        {
            if (ary == null) return 0;
            return ary.Length;
        }

        /// <summary>
        /// 配列の先頭の要素を取得する。配列が空なら null を返す。
        /// </summary>
        public static T _getFirst<T>(this T[] ary)
        {
            return ary._isEmpty() ? default(T) : ary[0];
        }

        /// <summary>
        /// 配列の2番目の要素を取得する。配列長が2未満なら null を返す。
        /// </summary>
        public static T _getSecond<T>(this T[] ary)
        {
            return ary._isEmpty() || ary.Length < 2 ? default(T) : ary[1];
        }

        /// <summary>
        /// 配列の3番目の要素を取得する。配列長が3未満なら null を返す。
        /// </summary>
        public static T _getThird<T>(this T[] ary)
        {
            return ary._isEmpty() || ary.Length < 3 ? default(T) : ary[2];
        }

        /// <summary>
        /// 配列のN番目(0始まり)の要素を取得する。配列長がN未満なら defVal を返す。
        /// </summary>
        public static T _getNth<T>(this T[] ary, int n, T defVal = default(T))
        {
            return _safeGet(ary, n, defVal);
        }

        /// <summary>
        /// 配列の idx 番目の要素を返す。idx が範囲外なら defVal を返す。
        /// </summary>
        public static T _safeGet<T>(this T[] ary, int idx, T defVal)
        {
            return (ary != null && idx >= 0 && idx < ary.Length) ? ary[idx] : defVal;
        }

        /// <summary>
        /// 配列の各要素を join する。
        /// </summary>
        public static string _join(this string[] array, string delim)
        {
            return array._notEmpty() ? string.Join(delim, array) : "";
        }

        public static int _findIndex<T>(this T[] array, Func<T, bool> predicate)
        {
            if (array != null)
            {
                for (int idx = 0; idx < array.Length; ++idx)
                {
                    if (predicate(array[idx])) return idx;
                }
            }
            return -1;
        }

        public static int _findIndex<T>(this T[] array, int startPos, Func<T, bool> predicate)
        {
            return array._findIndex(startPos, -1, predicate);
        }

        public static int _findIndex<T>(this T[] array, int startPos, int endPos, Func<T, bool> predicate)
        {
            if (array != null) {
                if (endPos < 0 || endPos > array.Length) endPos = array.Length;
                for (int idx = startPos; idx < endPos; ++idx) {
                    if (predicate(array[idx])) return idx;
                }
            }
            return -1;
        }

        /// <summary>
        /// Array の末尾要素をセットする
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public static T[] _setLast<T>(this T[] array, T val)
        {
            int lastPos = array._length() - 1;
            if (lastPos >= 0) array[lastPos] = val;
            return array;
        }

        /// <summary>
        /// 配列の内容をクリアする
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        public static void _clear<T>(this T[] array)
        {
            for (int i = 0; i < array.Length; ++i) array[i] = default(T);
        }

        public static int _findIndex(this char[] array, char ch)
        {
            if (array != null) {
                return array._findIndex(0, array.Length, ch);
            }
            return -1;
        }

        public static int _findIndex(this char[] array, int startPos, char ch)
        {
            return array._findIndex(startPos, -1, ch);
        }

        public static int _findIndex(this char[] array, int startPos, int endPos, char ch)
        {
            if (array != null) {
                if (endPos < 0 || endPos > array.Length) endPos = array.Length;
                for (int idx = startPos; idx < endPos; ++idx) {
                    if (ch == array[idx]) return idx;
                    if (array[idx] == 0) return -1;
                }
            }
            return -1;
        }

        public static int _strlen(this char[] array)
        {
            int len = array._findIndex('\0');
            return len >= 0 ? len : array._safeLength();
        }

        public static string _toString(this char[] array)
        {
            return new string(array, 0, array._strlen());
        }
    }

    /// <summary>
    /// 例外の拡張メソッドクラス
    /// </summary>
    public static class ExceptionExtension
    {
        /// <summary>
        /// エラーメッセージ作成
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string _getErrorMsg(this Exception e)
        {
            var msg = _getErrorMsgShort(e) + "\n";
            var e1 = e.InnerException;
            if (e1 != null && e1.Message != null)
            {
                msg += e1.Message + "\n";
                var e2 = e1.InnerException;
                if (e2 != null && e1.Message != null)
                {
                    msg += e2.Message + "\n";
                }
            }
            return msg + e.StackTrace;
        }

        /// <summary>
        /// 短いエラーメッセージ作成
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string _getErrorMsgShort(this Exception e)
        {
            return $"{e.GetType()}:{e.Message}";
        }

    }

    public static class ObjcectExtension
    {
        public static string _objToString(this object obj, string defval = "")
        {
            return obj != null ? (string)obj : defval;
        }

        public static bool _objToBool(this object obj)
        {
            return obj != null ? (bool)obj : false;
        }

        public static int _objToInt(this object obj)
        {
            return obj != null ? (int)obj : 0;
        }

        public static string _toYN(this bool flag)
        {
            return flag ? "Y" : "N";
        }
    }

    /// <summary>
    /// Guid の拡張メソッドクラス
    /// </summary>
    public static class GuidExtension
    {
        /// <summary> Guid.Empty か</summary>
        public static bool _isEmpty(this Guid uuid)
        {
            return uuid == Guid.Empty;
        }

        /// <summary> not Guid.Empty か</summary>
        public static bool _notEmpty(this Guid uuid)
        {
            return uuid != Guid.Empty;
        }
    }

    /// <summary>
    /// 文字列の拡張メソッドクラス
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// 文字列が null または empty なら true を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool _isEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// 文字列が null でなく、かつ empty でもなければ true を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool _notEmpty(this string str)
        {
            return !string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// 自身が null または空文字列なら defVal を返す。そうでなければ自身を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _orElse(this string str, string defVal)
        {
            return str._notEmpty() ? str : (defVal != null ? defVal : "");
        }

        /// <summary>
        /// 自身が null または空文字列なら defVal?.Invoke() ?? "" を返す。そうでなければ自身を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _orElse(this string str, Func<string> defVal = null)
        {
            return str._notEmpty() ? str : (defVal?.Invoke() ?? "");
        }

        /// <summary>
        /// 自身が null なら空文字列を返す。そうでなければ自身を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _toSafe(this string str)
        {
            return str._orElse("");
        }

        /// <summary>
        /// 文字列長の範囲を超えても例外を起こさない部分文字列取得。start が 負なら文字列の末尾から。len が負なら末尾まで。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="start"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        static public string _safeSubstring(this string str, int start, int len = -1)
        {
            if (!string.IsNullOrEmpty(str))
            {
                if (start < 0)
                {
                    start += str.Length;
                    if (start < 0) start = 0;
                }
                if (start >= str.Length) return "";

                if (len < 0 || start + len > str.Length)
                {
                    len = str.Length - start;
                }
                return str.Substring(start, len);
            }
            return str;
        }

        /// <summary>
        /// 文字列長の範囲を超えても例外を起こさない部分文字列取得。start が 負なら文字列の末尾から。len が負なら末尾まで。
        /// </summary>
        static public string _substring(this string str, int start, int len = -1)
        {
            return str._safeSubstring(start, len);
        }

        /// <summary>
        /// 例外を起こさない Replace。 str が null なら "" を返す。
        /// </summary>
        /// <returns></returns>
        public static string _safeReplace(this string str, char oldChar, char newChar)
        {
            return str == null ? "" : str.Replace(oldChar, newChar);
        }

        /// <summary>
        /// 例外を起こさない Replace。 str が null なら "" を返す。
        /// </summary>
        /// <returns></returns>
        public static string _safeReplace(this string str, string oldVal, string newVal)
        {
            return str == null ? "" : str.Replace(oldVal, newVal);
        }

        public static char? _getNth(this string str, int idx)
        {
            return str._notEmpty() && idx >= 0 && idx < str.Length ? str[idx] : (char?)null;
        }

        /// <summary>
        /// 文字列が指定の文字列で始まるか
        /// </summary>
        /// <param name="str"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool _startsWith(this string str, string value)
        {
            return str._notEmpty() && str.StartsWith(value);

        }

        /// <summary>
        /// 文字列が指定の文字列で終わるか
        /// </summary>
        /// <param name="str"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool _endsWith(this string str, string value)
        {
            return str._notEmpty() && str.EndsWith(value);

        }

        /// <summary>
        /// 例外を起こさない Contains
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="me"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        static public bool _in<T>(this T me, IEnumerable<T> list)
        {
            return me != null && list != null ? list.Contains(me) : false;
        }

        /// <summary>
        /// 例外を起こさない Count
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        static public int _safeCount(this string list)
        {
            return list == null ? 0 : list.Length;
        }

        /// <summary>
        /// 例外を起こさない Length
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        static public int _safeLength(this string list)
        {
            return list == null ? 0 : list.Length;
        }

        /// <summary>
        /// 例外を起こさない Count
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        static public int _safeCount<T>(this T[] list)
        {
            return list == null ? 0 : list.Length;
        }

        /// <summary>
        /// 例外を起こさない Length
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        static public int _safeLength<T>(this T[] list)
        {
            return list == null ? 0 : list.Length;
        }

        /// <summary>
        /// 例外を起こさない Count
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        static public int _safeCount<T>(this List<T> list)
        {
            return list == null ? 0 : list.Count;
        }

        /// <summary>
        /// 例外を起こさない Count
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        static public int _safeCount<T>(this IEnumerable<T> list)
        {
            return list == null ? 0 : list.Count();
        }

        /// <summary>
        /// 例外を起こさない IndexOf
        /// </summary>
        static public int _safeIndexOf(this string str, char ch, int start = 0)
        {
            if (start < 0) start = 0;
            if (str._isEmpty() || start >= str.Length) return -1;
            return str.IndexOf(ch, start);
        }

        /// <summary>
        /// 例外を起こさない IndexOf
        /// </summary>
        static public int _safeIndexOf(this string str, string tgt, int start = 0)
        {
            if (start < 0) start = 0;
            if (str._isEmpty() || tgt._isEmpty() || start > str.Length - tgt.Length) return -1;
            return str.IndexOf(tgt, start);
        }

        /// <summary>
        /// 例外を起こさない Contains
        /// </summary>
        static public bool _safeContains(this string str, char ch)
        {
            if (str._isEmpty()) return false;
            return str.Contains(ch);
        }

        /// <summary>
        /// 例外を起こさない Contains
        /// </summary>
        static public bool _safeContains(this string str, string tgt)
        {
            if (str._isEmpty() || tgt._isEmpty()) return false;
            return str.Contains(tgt);
        }

        /// <summary>
        /// 例外を起こさない Contains
        /// </summary>
        static public bool _safeContains<T>(this T[] list, T tgt)
        {
            if (list._isEmpty() || tgt == null) return false;
            return list.Contains(tgt);
        }

        /// <summary>
        /// 例外を起こさない Contains
        /// </summary>
        static public bool _safeContains<T>(this List<T> list, T tgt)
        {
            if (list._isEmpty() || tgt == null) return false;
            return list.Contains(tgt);
        }

        /// <summary>
        /// 例外を起こさない Contains
        /// </summary>
        static public bool _safeContains<T>(this HashSet<T> set, T tgt)
        {
            if (set._isEmpty() || tgt == null) return false;
            return set.Contains(tgt);
        }

        /// <summary>
        /// アンカーとして ^ と $ を扱える Contains
        /// </summary>
        /// <param name="str"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        static public bool _reContains(this string str, string pattern)
        {
            if (str._isEmpty() || pattern._isEmpty()) return false;
            if (pattern.StartsWith("^")) {
                pattern = pattern.TrimStart(new char[] { '^' });
                if (pattern.EndsWith("$")) {
                    pattern = pattern.TrimEnd(new char[] { '$' });
                    return str._equalsTo(pattern);
                } else {
                    return str.StartsWith(pattern);
                }
            } else if (pattern.EndsWith("$")) {
                pattern = pattern.TrimEnd(new char[] { '$' });
                return str.EndsWith(pattern);
            } else {
               return  str.Contains(pattern);
            }
        }

        /// <summary>
        /// set に item が格納されていれば false を返す。格納されていなければ true を返して set する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="set"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        static public bool _testAndSet<T>(this HashSet<T> set, T item)
        {
            if (set.Contains(item))
                return false;
            else {
                set.Add(item);
                return true;
            }
        }

        /// <summary>
        /// 例外を起こさない ToLower。null なら空文字列を返す
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _toLower(this string str)
        {
            return str._isEmpty() ? "" : str.ToLower();
        }

        /// <summary>
        /// 例外を起こさない ToUpper。null なら空文字列を返す
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _toUpper(this string str)
        {
            return str._isEmpty() ? "" : str.ToUpper();
        }

        /// <summary>
        /// レシーバとオペランドの両方が null であるか、または両方とも null でなくて等値なら true を返す。
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool _equalsTo(this string lhs, string rhs)
        {
            return (lhs == null && rhs == null) || (lhs != null && rhs != null && lhs == rhs);
        }

        /// <summary>
        /// レシーバとオペランドのいずれかと一致すれば true を返す
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool _containedIn(this string lhs, string rhs1, string rhs2, string rhs3 = null, string rhs4 = null)
        {
            return lhs != null && (lhs == rhs1 || lhs == rhs2 || lhs == rhs3 || lhs == rhs4);
        }

        /// <summary>
        /// レシーバがオペランドに等しくなければ true を返す。
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool _ne(this string lhs, string rhs)
        {
            return !lhs._equalsTo(rhs) ;
        }

        /// <summary>
        /// レシーバがオペランドよりも大きければ true を返す。
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool _gt(this string lhs, string rhs)
        {
            return lhs != null && rhs != null && lhs.CompareTo(rhs) > 0;
        }

        /// <summary>
        /// レシーバがオペランドよりも大きいか等しければ true を返す。
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool _ge(this string lhs, string rhs)
        {
            return lhs != null && rhs != null && lhs.CompareTo(rhs) >= 0;
        }

        /// <summary>
        /// レシーバがオペランドよりも小さければ true を返す。
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool _lt(this string lhs, string rhs)
        {
            return lhs != null && rhs != null && lhs.CompareTo(rhs) < 0;
        }

        /// <summary>
        /// レシーバがオペランドよりも小さいか等しければ true を返す。
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool _le(this string lhs, string rhs)
        {
            return lhs != null && rhs != null && lhs.CompareTo(rhs) <= 0;
        }

        /// <summary>
        /// 例外を起こさない Split。入力が null または空文字列なら空文字列を1つ含む配列を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="delim"></param>
        /// <returns></returns>
        public static string[] _split(this string str, char delim)
        {
            if (str._isEmpty()) return new string[] { "" };

            return str.Split(new char[] { delim });
        }

        /// <summary>
        /// 例外を起こさない二分割 Split。入力が null または空文字列なら空文字列を1つ含む配列を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="delim"></param>
        /// <returns></returns>
        public static string[] _split2(this string str, char delim)
        {
            if (str._isEmpty()) return new string[] { "" };

            int idx = str.IndexOf(delim);
            if (idx < 0) return str._toArray1();

            return new string[] { str._safeSubstring(0, idx), str._safeSubstring(idx + 1) };
        }

        /// <summary>
        /// 例外を起こさないn分割 Split。入力が null または空文字列なら空文字列を1つ含む配列を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="delim"></param>
        /// <returns></returns>
        public static string[] _splitn(this string str, char delim, int n)
        {
            if (str._isEmpty()) return new string[] { "" };

            if (n < 2) return new string[] { str };

            var list = new List<string>();
            string s = str;
            for (int i = 1; i < n; ++i) {
                var items = s._split2(delim);
                list.Add(items._getFirst());
                s = items._getSecond();
                if (s._isEmpty()) break;
            }
            if (s._notEmpty()) list.Add(s);
            return list.ToArray();
        }

        /// <summary>
        /// 入力が null または空文字列なら、空の配列を返す _split
        /// </summary>
        /// <param name="str"></param>
        /// <param name="delim"></param>
        /// <returns></returns>
        public static string[] _splitUnemptyable(this string str, char delim)
        {
            if (str._isEmpty()) return new string[0];

            return str.Split(new char[] { delim });
        }

        /// <summary>
        /// HH:MM 形式の文字列か否かの判定
        /// </summary>
        /// <param name="strTime"></param>
        /// <returns></returns>
        public static bool _isHHMM(this string strTime, string delims = ":")
        {
            return Helper.MatchTimePattern(strTime, delims);
        }

        /// <summary>
        /// 空の時分 (":", "-", "  :  ", "  -  ") 文字列か否かの判定
        /// </summary>
        /// <param name="strTime"></param>
        /// <returns></returns>
        public static bool _isEmptyHHMM(this string strTime)
        {
            return strTime._equalsTo(":") || strTime._equalsTo("-") || strTime._equalsTo("  :  ") || strTime._equalsTo("  -  ");
        }

        /// <summary>
        /// 文字列が HH:MM 形式かどうか判定し、桁が不足していれば先頭に 0 を足して、正規形にして返す。<para/>
        /// 正規形に変換できなければ "00:00" を返す。
        /// </summary>
        /// <param name="strTime"></param>
        /// <param name="delims"></param>
        /// <returns></returns>
        public static string _normalizeHHMM(this string strTime, string delims = ":")
        {
            return strTime._isHHMM(delims) ? strTime : _splitHHMM(strTime)._join(":");
        }

        /// <summary>
        /// 文字列が HH[デリミタ]MM 形式かどうか判定し、桁が不足していれば先頭に 0 を足して、正規形にした上で、{HH, MM, DELIM} に分割して返す。<para/>
        /// 正規形に変換できなければ {"00", "00", ""} を返す。
        /// </summary>
        /// <param name="strTime"></param>
        /// <param name="delims"></param>
        /// <returns></returns>
        public static string[] _splitHHMM(this string strTime, string delims = ":")
        {
            Func<string, string> normalize = (x) => x == null ? "00" : x.Length == 2 ? x : ("00" + x)._safeSubstring(-2);

            var items = strTime._reScan(@"^(\d?\d)([" + delims + @"])(\d?\d)$");
            if (items.Count == 4) {
                return new string[] { normalize(items[1]), normalize(items[3]), items[2] };
            }
            return new string[] { "00", "00", "" };
        }

        /// <summary>
        /// 日時文字列をパースして DateTime を返す。エラーなら MinValue を返す。例外は出さない。
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _parseDateTime(this string dt)
        {
            DateTime result;
            if (DateTime.TryParse(dt, out result))
                return result;
            else
                return DateTime.MinValue;
        }

        /// <summary>
        /// 日付文字列に days を足した日付文字列を返す
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string _addDays(this string dt, int days)
        {
            return dt._parseDateTime().AddDays(days)._dateString();
        }

        /// <summary>
        /// bool値に解釈されるべき文字列を bool 値に変換する。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool _parseBool(this string str, bool defVal = false)
        {
            var s = str?.ToLower();
            if (defVal)
                return !(s._notEmpty() && (s[0] == 'f' || s[0] == 'n' || s == "off"));
            else
                return (s._notEmpty() && (s[0] == 't' || s[0] == 'y' || s == "on"));
        }

        /// <summary>
        /// 整数に解釈されるべき文字列を整数値に変換する。エラーなら errorVal を返し、空文字列なら emptyVal を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int _parseInt(this string str, int emptyVal = 0, int errorVal = 0)
        {
            try {
                return str._isEmpty() ? emptyVal : int.Parse(str);
            } catch {
                return errorVal;
            }
        }

        /// <summary>
        /// 整数に解釈されるべき文字列を整数値に変換する。エラーなら null を返し、空文字列なら emptyVal を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int? _parseInt2(this string str, int? emptyVal = null)
        {
            try {
                return str._isEmpty() ? emptyVal : int.Parse(str);
            } catch {
                return null;
            }
        }

        /// <summary>
        /// long に解釈されるべき文字列を long に変換する。エラーなら null を返し、空文字列なら emptyVal を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static long? _parseLong(this string str, long? emptyVal = null)
        {
            try {
                return str._isEmpty() ? emptyVal : long.Parse(str);
            } catch {
                return null;
            }
        }

        /// <summary>
        /// 16進整数に解釈されるべき文字列を整数値に変更する。エラーなら ERROR_INT_VAL を返し、空文字列なら emptyVal を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int _parseHex(this string str, int emptyVal = Helper.ERROR_INT_VAL)
        {
            try {
                return str._isEmpty() ? emptyVal : Convert.ToInt32(str, 16);
            } catch {
                return Helper.ERROR_INT_VAL;
            }
        }

        /// <summary>
        /// 浮動小数点数に解釈されるべき文字列をdouble値に変更する。エラーなら NaN を返し、空文字列なら emptyVal を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static double _parseDouble(this string str, double emptyVal = double.NaN)
        {
            try {
                return str._isEmpty() ? emptyVal : Convert.ToDouble(str);
            } catch {
                return double.NaN;
            }
        }

        /// <summary>
        /// GUIDを表す文字列から GUID を生成する。空文字なら Guid.Empty を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Guid _toGuid(this string str)
        {
            try {
                return str._isEmpty() ? Guid.Empty : new Guid(str);
            } catch {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// 文字列の配列やリスト list を delim で区切った形に join する。
        /// </summary>
        /// <param name="delim"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string _join(this string delim, IEnumerable<string> list)
        {
            return list._notEmpty() ? string.Join(delim, list) : "";
        }

        /// <summary>
        /// ShiftJIS な文字列に変換する
        /// </summary>
        /// <param name="unicodeStrings"></param>
        /// <returns></returns>
        public static string _toShiftJis(this string unicodeStrings)
        {
            var unicode = Encoding.Unicode;
            var unicodeByte = unicode.GetBytes(unicodeStrings);
            var s_jis = Encoding.GetEncoding("shift_jis");
            var s_jisByte = Encoding.Convert(unicode, s_jis, unicodeByte);
            var s_jisChars = new char[s_jis.GetCharCount(s_jisByte, 0, s_jisByte.Length)];
            s_jis.GetChars(s_jisByte, 0, s_jisByte.Length, s_jisChars, 0);
            return new string(s_jisChars);
        }

        /// <summary>
        /// MS Unicode な文字列に変換する (〜(0x301C) を ～(0xFF5E) に変換するなど)
        /// </summary>
        /// <param name="unicodeStrings"></param>
        /// <returns></returns>
        public static string _convertMSUnicode(this string unicodeStrings)
        {
            return new string(unicodeStrings.ToCharArray().Select(ch => convertMSUniChar(ch)).ToArray());
        }

        private static char convertMSUniChar(char ch)
        {
            switch ((int)ch) {
                // 全角チルダ
                case 0x301C:
                    return (char)0xFF5E;
                // 全角マイナス
                case 0x2212:
                    return (char)0xFF0D;
                // 全角マイナスより少し幅のある文字
                case 0x2014:
                    return (char)0x2015;
                default:
                    return ch;
            }
        }

        /// <summary>
        /// str.Trim()
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _strip(this string str)
        {
            return str._isEmpty() ? "" : str.Trim();
        }

        /// <summary>
        /// str.TrimStart()
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _stripStart(this string str)
        {
            return str._isEmpty() ? "" : str.TrimStart();
        }

        /// <summary>
        /// str.TrimStart()
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _stripLeft(this string str)
        {
            return str._isEmpty() ? "" : str.TrimStart();
        }

        /// <summary>
        /// str.TrimEnd(文字列)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _stripEnd(this string str, string charSet)
        {
            if (str._isEmpty()) return "";
            if (charSet._isEmpty()) return str;
            return str.TrimEnd(charSet.ToCharArray());
        }

        /// <summary>
        /// 末尾の1文字だけを削除する
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _stripEnd1(this string str, string charSet)
        {
            if (str._isEmpty()) return "";
            if (charSet._isEmpty()) return str;
            return charSet.Contains(str.Last()) ? str.Substring(0, str.Length - 1) : str;
        }

        /// <summary>
        /// str.TrimEnd()
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _stripEnd(this string str, bool onlyCrLf = false)
        {
            if (str._isEmpty()) return "";
            if (onlyCrLf)
                return str.TrimEnd(new char[] { '\r', '\n' });
            else
                return str.TrimEnd();
        }

        /// <summary>
        /// str.TrimEnd()
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _stripRight(this string str, bool onlyCrLf = false)
        {
            return str._stripEnd(onlyCrLf);
        }

        /// <summary>
        /// 先頭・未尾のダブルクォートを削除する
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _stripDq(this string str, bool docodeCrLf = false)
        {
            if (str._isEmpty()) return str;
            if (docodeCrLf) str = str.Replace("\\r", "").Replace("\\n", "\n");
            if (str.Length >= 2 && str[0] == '"' && str.Last() == '"')
                return str._safeSubstring(1, str.Length - 2);
            return str;
        }

        /// <summary>
        /// 文字列 str を char array に変換する。str が null なら空配列を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static char[] _toCharArray(this string str)
        {
            return str._isEmpty() ? new char[0] : str.ToCharArray();
        }

        private static Dictionary<char, char> zen2hanDic;
        private static string zenkakuChars = "　ＡＢＣＤＥＦＧＨＩＪＫＬＭＮＯＰＱＲＳＴＵＶＷＸＹＺａｂｃｄｅｆｇｈｉｊｋｌｍｎｏｐｑｒｓｔｕｖｗｘｙｚ０１２３４５６７８９！”＃＄％＆’（）［］｛｝｜＊＋，．／：；＜＝＞？＠＾＿‘～￥";
        private static string hankakuChars = " ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!\"#$%&\'()[]{}|*+,./:;<=>?@^_`~\\";

        /// <summary>
        /// 全角英数字を半角に変換する
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string _zen2han(this string str)
        {
            if (zen2hanDic == null) {
                zen2hanDic = zenkakuChars.ToCharArray().Zip(hankakuChars.ToCharArray(), (k, v) => new { k, v }).ToDictionary(a => a.k, a => a.v);
            }
            if (str._isEmpty()) return str;
            var result = new string(str.ToCharArray().Select(x => zen2hanDic._safeGet(x, x)).ToArray());
            return result;
        }

        /// <summary>
        /// 指定パスのファイル名部分を取得する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string _getFileName(this string path)
        {
            return Helper.GetFileName(path);
        }

        /// <summary>
        /// 指定パスの親ディレクトリを取得する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string _getDirPath(this string path)
        {
            return Helper.GetDirectoryName(path);
        }

        /// <summary>
        /// 指定パスは絶対パスか(ルートディレクトリを含む)か
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool _isAbsPath(this string path)
        {
            return Helper.IsAbsolutePath(path);
        }

        /// <summary>
        /// 2つのパスを結合する
        /// </summary>
        public static string _joinPath(this string path1, string path2)
        {
            return Helper.JoinPath(path1, path2);
        }

        /// <summary>
        /// 3つのパスを結合する
        /// </summary>
        public static string _joinPath(this string path1, string path2, string path3)
        {
            return Helper.JoinPath(path1, path2, path3);
        }

        /// <summary>
        /// 2つのパスを結合する。path2 が絶対パスなら path2自身を返す。
        /// </summary>
        public static string _joinAbsPath(this string path1, string path2)
        {
            return Helper.JoinAbsPath(path1, path2);
        }

        /// <summary>
        /// パスの / を \ に置換する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string _canonicalPath(this string path)
        {
            return path._toSafe().Replace("/", @"\");
        }


        /// <summary>
        /// str から key=value の形の value を抽出する。なければ null を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string _extractKeyValue(this string str, string key)
        {
            // key=(\w+) にマッチさせ、$1 を返す
            return str._reScan($@"\b{key}=(\S+)")._getSecond();
        }

        /// <summary>
        /// 文字列を暗号化する
        /// </summary>
        public static string _encrypt(this string str, string aes_iv, string aes_key)
        {
            return Helper.EncryptText(str, aes_iv, aes_key);
        }

        /// <summary>
        /// 文字列を平文化する
        /// </summary>
        public static string _decrypt(this string str, string aes_iv, string aes_key)
        {
            return Helper.DecryptText(str, aes_iv, aes_key);
        }

        public static bool _isSurrogatePair(this string str, int pos = 0)
        {
            if (str._notEmpty() && pos >= 0 && pos + 1 < str.Length) {
                return Helper.IsSurrogatePair(str[pos], str[pos + 1]);
            }
            return false;
        }

    }

    /// <summary>
    /// List の拡張クラス
    /// </summary>
    public static class ListExtension
    {
        /// <summary>
        /// 単一要素のリストを作成する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static List<T> _toList1<T>(this T obj)
        {
            return new T[] { obj }.ToList();
        }

        /// <summary>
        /// 単一要素のリストを作成する。str._isEmpty() なら null を返す。
        /// </summary>
        public static List<string> _toNullOrList1(this string str)
        {
            return str._isEmpty() ? null : str._toList1();
        }

        /// <summary>
        /// リストが null または empty なら true を返す。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool _isEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }

        /// <summary>
        /// リストが _isEmpty でないなら true を返す。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool _notEmpty<T>(this List<T> list)
        {
            return !list._isEmpty();
        }

        /// <summary>
        /// リストの長さを返す。 null なら 0 を返す。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int _length<T>(this List<T> list)
        {
            if (list == null) return 0;
            return list.Count;
        }

        /// <summary>
        /// list に list2 を AddRange する。(list または list2 が null なら何もしない)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public static List<T> _addRange<T>(this List<T> list, IEnumerable<T> list2)
        {
            if (list != null && list2._notEmpty()) {
                list.AddRange(list2);
            }
            return list;
        }

        /// <summary>
        /// list に list2, list3, list4 を AddRange する。(list または list2, list3, list4 が null なら何もしない)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public static List<T> _addRange<T>(this List<T> list, IEnumerable<T> list2, IEnumerable<T> list3, IEnumerable<T> list4)
        {
            return list._addRange(list2)._addRange(list3)._addRange(list4);
        }

        /// <summary>
        /// list に list2, list3 を AddRange する。(list または list2, list3 が null なら何もしない)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public static List<T> _addRange<T>(this List<T> list, IEnumerable<T> list2, IEnumerable<T> list3)
        {
            return list._addRange(list2)._addRange(list3);
        }

        /// <summary>
        /// 自身(string リスト)をコピーし、それに要素 str を追加したリストを返す。
        /// </summary>
        /// <param name="list"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<string> _copyAdd(this List<string> list, string str)
        {
            var newList = new List<string>(list);
            newList.Add(str);
            return newList;
        }

        /// <summary>
        /// 自身(string リスト)をコピーし、その idx 位置に要素 str を挿入したリストを返す。(idx を省略した場合は先頭に挿入)
        /// </summary>
        /// <param name="list"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<string> _copyInsert(this List<string> list, string str, int idx = 0)
        {
            var newList = new List<string>(list);
            newList.Insert(idx, str);
            return newList;
        }

        /// <summary>
        /// リストの部分を返す。idx &lt; 0 なら末尾から。count &lt; 0 なら末尾まで
        /// </summary>
        public static List<T> _safeGetRange<T>(this List<T> list, int idx, int count)
        {
            int listCount = list._isEmpty() ? 0 : list.Count;
            if (idx < 0) {
                idx += listCount;
                if (idx < 0) idx = 0;
            }
            if (idx >= listCount) return new List<T>();

            if (count < 0 || idx + count > listCount) {
                count = listCount - idx;
            }
            return list.GetRange(idx, count);
        }

        /// <summary>
        /// リストの先頭の要素を取得する。リストが空なら null を返す。
        /// </summary>
        public static T _getFirst<T>(this List<T> list)
        {
            return list._isEmpty() ? default(T) : list[0];
        }

        /// <summary>
        /// リストの2番目の要素を取得する。リスト長が2未満なら null を返す。
        /// </summary>
        public static T _getSecond<T>(this List<T> list)
        {
            return list._isEmpty() || list.Count < 2 ? default(T) : list[1];
        }

        /// <summary>
        /// リストの3番目の要素を取得する。リスト長が3未満なら null を返す。
        /// </summary>
        public static T _getThird<T>(this List<T> list)
        {
            return list._isEmpty() || list.Count < 3 ? default(T) : list[2];
        }

        /// <summary>
        /// リストのN番目(0始まり)の要素を取得する。リスト長がN未満なら defVal を返す。
        /// </summary>
        public static T _getNth<T>(this List<T> list, int n, T defVal = default(T))
        {
            return _safeGet(list, n, defVal);
        }

        /// <summary>
        /// リストの idx 番目の要素を返す。idx が範囲外なら defVal を返す。
        /// </summary>
        public static T _safeGet<T>(this List<T> list, int idx, T defVal)
        {
            return (list != null && idx >= 0 && idx < list.Count) ? list[idx] : defVal;
        }

        /// <summary>
        /// null 以外の obj をリストに追加する。追加されたリストを返す。
        /// </summary>
        public static List<T> _safeAdd<T>(this List<T> list, T obj)
        {
            if (list != null && obj != null) list.Add(obj);
            return list;
        }

        /// <summary>
        /// リストの各要素を join する。
        /// </summary>
        public static string _join(this List<string> list, string delim)
        {
            return list._notEmpty() ? string.Join(delim, list) : "";
        }

        /// <summary>
        /// デバッグ出力用に join する
        /// </summary>
        /// <param name="list"></param>
        /// <param name="delim"></param>
        /// <returns></returns>
        public static string _joinDebug(this List<string> list)
        {
            return list._notEmpty() ? string.Join(",", list) : "null";
        }

        /// <summary>
        /// string リストを sort | uniq した結果を返す。
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<string> _sortUniq(this List<string> list)
        {
            return list.OrderBy(x => x).Distinct().ToList();
        }

        /// <summary>
        /// null でない最後の要素の位置を返す。全部 null または list が空だったら -1 を返す。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static int _findLastNotNull<T>(this List<T> list)
        {
            for (int idx = list.Count - 1; idx >= 0; --idx)
            {
                if (list[idx] != null) return idx;
            }
            return -1;
        }

        /// <summary>
        /// List の末尾要素をセットする
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public static List<T> _setLast<T>(this List<T> list, T val)
        {
            int lastPos = list._length() - 1;
            if (lastPos >= 0) list[lastPos] = val;
            return list;
        }

        /// <summary>
        /// リストの内容をクリアする
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void _clear<T>(this List<T> list)
        {
            for (int i = 0; i < list.Count(); ++i) list[i] = default(T);
        }
    }

    /// <summary>
    /// IEnumerable の拡張クラス
    /// </summary>
    public static class IEnumerableExtension
    {
        /// <summary>
        /// IEnumerable が null または empty なら true を返す。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool _isEmpty<T>(this IEnumerable<T> list)
        {
            return list == null || list.Count() == 0;
        }

        /// <summary>
        /// IEnumerable が _isEmpty でないなら true を返す。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool _notEmpty<T>(this IEnumerable<T> list)
        {
            return !list._isEmpty();
        }

        /// <summary>
        /// IEnumerable の先頭の要素を取得する。リストが空なら null を返す。
        /// </summary>
        public static T _getFirst<T>(this IEnumerable<T> list)
        {
            return list._isEmpty() ? default(T) : list.First();
        }

        /// <summary>
        /// IEnumerable の2番目の要素を取得する。リストが空なら null を返す。
        /// </summary>
        public static T _getSecond<T>(this IEnumerable<T> list)
        {
            if (list._isEmpty()) return default(T);
            list = list.Skip(1);
            return list._isEmpty() ? default(T) : list.First();
        }

        /// <summary>
        /// IEnumerable の各要素を join する。
        /// </summary>
        public static string _join(this IEnumerable<string> list, string delim)
        {
            return list._notEmpty() ? string.Join(delim, list) : "";
        }

        /// <summary>
        /// keys と values から辞書を作成して返す。どちらかが empty なら空辞書を返す。
        /// </summary>
        public static Dictionary<TKey, TValue> _makeDict<TKey, TValue>(this IEnumerable<TKey> keys, IEnumerable<TValue> values)
        {
            if (keys._isEmpty() || values._isEmpty()) return new Dictionary<TKey, TValue>();
            return keys.Zip(values, (k, v) => new { k, v }).ToDictionary(a => a.k, a => a.v);
        }

        /// <summary>
        /// 値の大きいほうから k 個を取得して返す
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        //public static IEnumerable<(int index, T item)> _topk<T>(this IEnumerable<T> list, int k) where T: IComparable
        //{
        //    return list._enumerate().OrderByDescending(x => x.item).Take(k);
        //}

        /// <summary>
        /// python enumerate と同じ動作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IEnumerable<(int index, T value)> _enumerate<T>(this IEnumerable<T> list)
        {
            return list._notEmpty() ? list.Select((value, index) => (index, value)) : new (int index, T value)[0];
        }

    }

    /// <summary>
    /// 辞書の拡張メソッドクラス
    /// </summary>
    public static class DictionaryExtention
    {
        /// <summary>
        /// 辞書が null または empty なら true を返す。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static bool _isEmpty<K,V>(this Dictionary<K,V> dict)
        {
            return dict == null || dict.Count == 0;
        }

        /// <summary>
        /// 辞書が _isEmpty でないなら true を返す。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static bool _notEmpty<K,V>(this Dictionary<K,V> dict)
        {
            return !dict._isEmpty();
        }

        public static Dictionary<K, V> _clone<K, V>(this Dictionary<K, V> dict)
        {
            Dictionary<K, V> newDict = new Dictionary<K, V>();
            foreach (var pair in dict)
            {
                newDict[pair.Key] = pair.Value;
            }
            return newDict;
        }

        public static V _safeGet<K, V>(this Dictionary<K, V> dict, K key, V defval = default(V))
        {
            V val;
            return key != null && dict.TryGetValue(key, out val) ? val : defval;
        }

        public static V _safeGetOrNewInsert<K, V>(this Dictionary<K, V> dict, K key) where V:new()
        {
            if (dict == null || key == null) return default(V);
            lock (dict)
            {
                V val;
                if (!dict.TryGetValue(key, out val))
                {
                    val = new V();
                    dict[key] = val;
                }
                return val;
            }
        }

        public static string _safeGet(this Dictionary<string, string> dict, string key)
        {
            string val;
            return key != null && dict.TryGetValue(key, out val) ? val : "";
        }

        /// <summary>
        /// 辞書からkeyにマッチする値(リスト)を取得し、それに要素を追加する。マッチするリストがなければ、新たにリストを作成して追加する。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="elem"></param>
        public static void _safeAddElement<T>(this Dictionary<string, List<T>> dict, string key, T elem)
        {
            if (key != null)
            {
                List<T> list = null;
                if (dict.TryGetValue(key, out list))
                {
                    list.Add(elem);
                }
                else
                {
                    dict[key] = new T[] { elem }.ToList();
                }
            }
        }

        /// <summary>
        /// 辞書からkeyにマッチする値(リスト)を取得し、それに要素を追加する。マッチするリストがなければ、新たにリストを作成して追加する。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="elem"></param>
        public static void _safeAddElement<T>(this Dictionary<Guid, List<T>> dict, Guid key, T elem)
        {
            if (key._notEmpty())
            {
                List<T> list = null;
                if (dict.TryGetValue(key, out list))
                {
                    list.Add(elem);
                }
                else
                {
                    dict[key] = new T[] { elem }.ToList();
                }
            }
        }

        public static void _safeAdd<T>(this Dictionary<string, T> dict, string key, T val)
        {
            if (dict != null && key != null)
            {
                dict[key] = val;
            }
        }

        public static void _lockedSet<K, T>(this Dictionary<K, T> dict, K key, T val)
        {
            if (dict != null && key != null) {
                lock (dict) {
                    dict[key] = val;
                }
            }
        }

        public static void _safeRemove<T>(this Dictionary<string, T> dict, string key)
        {
            if (key != null)
            {
                dict.Remove(key);
            }
        }

        public static void _lockedRemove<K, T>(this Dictionary<K, T> dict, K key)
        {
            if (dict != null && key != null) {
                lock (dict) {
                    dict.Remove(key);
                }
            }
        }

        public static void _safeRemoveElement<T>(this Dictionary<string, List<T>> dict, string key, T elem)
        {
            List<T> list = null;
            if (dict.TryGetValue(key, out list))
            {
                if (list != null) list.Remove(elem);
            }
        }
    }

    /// <summary>
    /// HashSetの拡張メソッドクラス
    /// </summary>
    public static class HashSetExtention
    {
        public static HashSet<T> _union<T>(this HashSet<T> hs, HashSet<T> hs2)
        {
            if (hs == null) return null;
            if (hs2._isEmpty()) return hs;
            return hs.Union(hs2)._toSet();
        }

        public static HashSet<T> _toSet<T>(this IEnumerable<T> list)
        {
            return (list == null) ? null : new HashSet<T>(list);
        }
    }

    /// <summary>
    /// DateTimeの拡張メソッドクラス
    /// </summary>
    public static class DateTimeExtention
    {
        /// <summary>
        /// その年の1日に変換する
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _truncateMonth(this DateTime dt)
        {
            return new DateTime(dt.Year, 1, 1);
        }

        /// <summary>
        /// その年の1日に変換する
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _toFirstInYear(this DateTime dt)
        {
            return new DateTime(dt.Year, 1, 1);
        }

        /// <summary>
        /// その月の1日に変換する
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _truncateDay(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        /// <summary>
        /// その月の1日に変換する
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _toFirstInMonth(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        /// <summary>
        /// 00:00 を指す DateTime に変換する
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _truncateHour(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day);
        }

        /// <summary>
        /// 00:00 を指す DateTime に変換する
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _toDate(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day);
        }

        /// <summary>
        /// 秒の単位を切り捨てた DateTime に変換する
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _truncateSec(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
        }

        /// <summary>
        /// 分、秒の単位を切り捨てた DateTime に変換する
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _truncateMin(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
        }

        /// <summary>
        /// 指定した月の日数を返す
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int _daysInMonth(this DateTime dt)
        {
            return DateTime.DaysInMonth(dt.Year, dt.Month);
        }

        /// <summary>
        /// 曜日を表する数値を返す。(0:日曜、1:月曜、・・・、6:土曜)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int _dayOfWeek(this DateTime dt)
        {
            return (int)dt.DayOfWeek;
        }

        /// <summary>
        /// yyyy/MM/dd HH:mm 形式の文字列を返す
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string _dateTimeString(this DateTime dt, bool bEmptyUnlessValid = false)
        {
            return (!bEmptyUnlessValid || dt._isValid()) ? dt.ToString("yyyy/MM/dd HH:mm") : "";
        }

        /// <summary>
        /// yyyy/MM/dd HH 形式の文字列を返す
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string _dateHourString(this DateTime dt)
        {
            return dt.ToString("yyyy/MM/dd HH");
        }

        /// <summary>
        /// yyyy/MM/dd 形式の文字列を返す
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string _dateString(this DateTime dt)
        {
            return dt.ToString("yyyy/MM/dd");
        }

        /// <summary>
        /// HH:mm 形式の文字列を返す (bSec = true なら HH:mm:ss を返す)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string _timeString(this DateTime dt, bool bSec = false)
        {
            return dt.ToString(bSec ? "HH:mm:ss" : "HH:mm");
        }

        /// <summary>
        /// Canonical な日時表現文字列を返す (yyyy/MM/dd HH:mm:ss 形式)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string _toString(this DateTime dt)
        {
            return dt.ToString("yyyy/MM/dd HH:mm:ss");
        }

        /// <summary>
        /// valid なデータ(MinValue, MaxValue でない)かどうかを返す
        /// </summary>
        /// <returns></returns>
        public static bool _isValid(this DateTime dt)
        {
            return dt != DateTime.MinValue && dt != DateTime.MaxValue;
        }

        /// <summary>
        /// not valid なデータ(MinValue, MaxValue)かどうかを返す
        /// </summary>
        /// <returns></returns>
        public static bool _notValid(this DateTime dt)
        {
            return !dt._isValid();
        }

        public static DateTime _orNow(this DateTime dt)
        {
            return dt._isValid() ? dt : DateTime.Now;
        }

        public static DateTime _orElse(this DateTime dt, DateTime dtElse)
        {
            return dt._isValid() ? dt : dtElse;
        }

        public static DateTime _safeAddDays(this DateTime dt, int val)
        {
            return (dt._isValid()) ? dt.AddDays(val) : dt;
        }

        public static DateTime _safeAddHours(this DateTime dt, int val)
        {
            return (dt._isValid()) ? dt.AddHours(val) : dt;
        }

        public static DateTime _safeAddMinutes(this DateTime dt, int val)
        {
            return (dt._isValid()) ? dt.AddMinutes(val) : dt;
        }

        /// <summary>
        /// valid なデータ(MinValue, MaxValue でない)ならば日時表現文字列を返す (yyyy/MM/dd HH:mm:ss 形式)。 MinValue, MaxValue なら空文字列を返す。
        /// ss = false なら分まで。
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string _toValidString(this DateTime dt)
        {
            return dt._isValid() ? dt._toString() : "";
        }

        /// <summary>
        /// 日付表現文字列を返す (yyyy/MM/dd 形式)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string _toDateString(this DateTime dt)
        {
            return dt.ToString("yyyy/MM/dd");
        }

        /// <summary>
        /// 短い日付表現文字列を返す (MM/dd 形式)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string _toShortDateString(this DateTime dt)
        {
            return dt.ToString("MM/dd");
        }

        /// <summary>
        /// yyyyMM 形式の文字列を返す
        /// </summary>
        public static string _yyyyMM(this DateTime dt)
        {
            return dt.ToString("yyyyMM");
        }

        /// <summary>
        /// yyyyMMdd 形式の文字列を返す
        /// </summary>
        public static string _yyyyMMdd(this DateTime dt)
        {
            return dt.ToString("yyyyMMdd");
        }

        /// <summary>
        /// MMdd 形式の文字列を返す
        /// </summary>
        public static string _MMdd(this DateTime dt)
        {
            return dt.ToString("MMdd");
        }

        /// <summary>
        /// yyyy 形式の文字列を返す
        /// </summary>
        public static string _toYear(this DateTime dt)
        {
            return dt.ToString("yyyy");
        }

        /// <summary>
        /// yyyy 形式の文字列を返す
        /// </summary>
        public static string _yyyy(this DateTime dt)
        {
            return dt.ToString("yyyy");
        }

        /// <summary>
        /// MM 形式の文字列を返す
        /// </summary>
        public static string _toMonth(this DateTime dt)
        {
            return dt.ToString("MM");
        }

        /// <summary>
        /// MM 形式の文字列を返す
        /// </summary>
        public static string _MM(this DateTime dt)
        {
            return dt.ToString("MM");
        }

        /// <summary>
        /// dd 形式の文字列を返す
        /// </summary>
        public static string _toDay(this DateTime dt)
        {
            return dt.ToString("dd");
        }

        /// <summary>
        /// dd 形式の文字列を返す
        /// </summary>
        public static string _dd(this DateTime dt)
        {
            return dt.ToString("dd");
        }

        /// <summary>
        /// 時刻表現文字列を返す (HH:mm:ss 形式)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string _toTimeString(this DateTime dt)
        {
            return dt.ToString("HH:mm:ss");
        }

        /// <summary>
        /// HH 形式の文字列を返す
        /// </summary>
        public static string _toHour(this DateTime dt)
        {
            return dt.ToString("HH");
        }

        /// <summary>
        /// mm 形式の文字列を返す
        /// </summary>
        public static string _toMinute(this DateTime dt)
        {
            return dt.ToString("mm");
        }

        /// <summary>
        /// ss 形式の文字列を返す
        /// </summary>
        public static string _toSecond(this DateTime dt)
        {
            return dt.ToString("ss");
        }

        /// <summary>
        /// lhs から rhs を引いた日の差を返す。
        /// </summary>
        public static int _diffDay(this DateTime lhs, DateTime rhs)
        {
            return (int)((lhs - rhs).TotalDays);
        }

        /// <summary>
        /// lhs から rhs を引いた月の差を返す。
        /// </summary>
        public static int _diffMonth(this DateTime lhs, DateTime rhs)
        {
            return (lhs.Year - rhs.Year) * 12 + lhs.Month - rhs.Month;
        }

        /// <summary>
        /// lhs から rhs を引いた年の差を返す。
        /// </summary>
        public static int _diffYear(this DateTime lhs, DateTime rhs)
        {
            return lhs.Year - rhs.Year;
        }

        /// <summary>
        /// 指定日の当月1日を返す。
        /// </summary>
        public static DateTime _firstDayOfThisMonth(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        /// <summary>
        /// 指定日の翌月1日を返す。
        /// </summary>
        public static DateTime _firstDayOfNextMonth(this DateTime dt)
        {
            return dt.AddMonths(1)._firstDayOfThisMonth();
        }

        /// <summary>
        /// 指定日の当月末日を返す。
        /// </summary>
        public static DateTime _lastDayOfThisMonth(this DateTime dt)
        {
            return dt._firstDayOfNextMonth().AddDays(-1);
        }

        /// <summary>
        /// 正当な年文字列(yyyy)を取得する。 MinValue または MaxValue の場合は "" を返す。
        /// </summary>
        public static string _getValidYearString(this DateTime dt)
        {
            return dt != DateTime.MinValue && dt != DateTime.MaxValue ? dt.ToString("yyyy") : "";
        }

        /// <summary>
        /// 正当な月文字列(MM)を取得する。 MinValue または MaxValue の場合は "" を返す。
        /// </summary>
        public static string _getValidMonthString(this DateTime dt)
        {
            return dt != DateTime.MinValue && dt != DateTime.MaxValue ? dt.ToString("MM") : "";
        }

        /// <summary>
        /// 正当な日文字列(MM)を取得する。 MinValue または MaxValue の場合は "" を返す。
        /// </summary>
        public static string _getValidDayString(this DateTime dt)
        {
            return dt != DateTime.MinValue && dt != DateTime.MaxValue ? dt.ToString("dd") : "";
        }

        /// <summary>
        /// 指定月に含まれる日数を返す
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int _getDaysInMonth(this DateTime dt)
        {
            return DateTime.DaysInMonth(dt.Year, dt.Month);
        }

        /// <summary>
        /// ((((yyyy-2000)*12+MM)*31+dd)*24+HH)*60+mm なシリアル値に変換する
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int _toSerialInt(this DateTime dt)
        {
            return dt.Year < 2000 ? 0 : dt.Year >= 6000 ? 0x7fffffff : ((((dt.Year - 2000) * 12 + dt.Month - 1) * 31 + dt.Day - 1) * 24 + dt.Hour) * 60 + dt.Minute;
        }

        /// <summary>
        /// HH*60+mm なシリアル値に変換する
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int _toSerialTimeInt(this DateTime dt)
        {
            return dt.Hour * 60 + dt.Minute;
        }

        /// <summary>
        /// 秒の部分を 0 にして次の分に進める
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _roundUpSecond(this DateTime dt)
        {
            return dt.Second > 0 ? dt.AddSeconds(60 - dt.Second) : dt;
        }

        /// <summary>
        /// 分の部分を 0 にして次の時に進める
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _roundUpMinute(this DateTime dt)
        {
            return dt.Minute > 0 ? dt.AddMinutes(60 - dt.Minute) : dt;
        }

        /// <summary>
        /// 時の部分を 0 にして次の日に進める
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _roundUpHour(this DateTime dt)
        {
            return dt.Hour > 0 ? dt.AddHours(24 - dt.Hour) : dt;
        }

        /// <summary>
        /// 日の部分を 1 にして次の月に進める
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _roundUpDay(this DateTime dt)
        {
            return dt.Day > 1 ? dt.AddDays(dt._getDaysInMonth() + 1 - dt.Day) : dt;
        }

        /// <summary>
        /// 月の部分を 1 にして次の年に進める
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime _roundUpMonth(this DateTime dt)
        {
            return dt.Month > 1 ? dt.AddMonths(13 - dt.Month) : dt;
        }

    }
}

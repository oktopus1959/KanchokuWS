using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace KanchokuWS.Handler
{
    public class HandlerUtils
    {
        private static System.Text.RegularExpressions.Regex reTernaryOperator = new System.Text.RegularExpressions.Regex(@"\(([^)]+)\)\?\(([^)]+)\):\(([^)]+)\)");

        public static bool IsFKeySpec(string str)
        {
            return str._getFirst() == '!' && str._getSecond() == '{';
        }

        public static bool IsTernaryOperator(string str)
        {
            return str._getFirst() == '(' && str.Last() == ')' && str._reMatch(reTernaryOperator);
        }

        /// <summary>
        /// (Q)?(A):(B) 形式だったら、Q に該当するウィンドウクラスか否かを判定し、当ならAを、否ならBを返す。<br/>
        /// (Q)?(A):(B) 形式でなければ、null を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ParseTernaryOperator(string str, string localName = null)
        {
            if (str._getFirst() == '(' && str.Last() == ')') {
                var items = str._reScan(reTernaryOperator);
                if (items._safeCount() == 4) {
                    string activeWinClassName = ActiveWindowHandler.Singleton?.ActiveWinClassName._toLower();
                    var names = items[1]._toLower()._split('|');
                    bool checkFunc()
                    {
                        if (names._notEmpty()) {
                            foreach (var name in names) {
                                if (name._notEmpty()) {
                                    if (name == localName) return true;   // for EditBuffer
                                    if (activeWinClassName._notEmpty() && activeWinClassName.StartsWith(name)) return true;
                                    if (name.Last() == '$' && name.Length == activeWinClassName.Length + 1 && name.StartsWith(activeWinClassName)) return true;
                                }
                            }
                        }
                        return false;
                    }
                    return checkFunc() ? items[2] : items[3];
                }
            }
            return null;
        }

    }
}

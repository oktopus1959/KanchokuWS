using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KanchokuWS.Domain;
using Utils;

namespace KanchokuWS.Handler
{

    public class FunctionalKeyInfo
    {
        public static int Ctrl = 1;
        public static int Shift = 2;
        public static int Alt = 3;

        public bool IsRight { get; }
        public int Modifier { get; }
        public string Name { get; }
        public string Alias { get; }
        public uint VKey { get; }
        public int RepeatCount { get; } = 1;
        public int StartPos { get; } = 0;
        public int NextPos { get; } = 0;

        public FunctionalKeyInfo(bool right, int mod, string name, string alis, uint vkey, int count, int start, int next)
        {
            IsRight = right;
            Modifier = mod;
            Name = name;
            Alias = alis;
            VKey = vkey;
            RepeatCount = count;
            StartPos = start;
            NextPos = next;
        }

        public override string ToString()
        {
            return $"FunctionalKeyInfo(Right={IsRight}, Modifier={Modifier}, Name={Name}, Alias={Alias}, VKey={VKey:x}, RepeatCount={RepeatCount}, StartPos={StartPos}, NextPos={NextPos})";
        }
    }

    /// <summary>
    /// 機能記述子("!{...}" )のパーサ
    /// </summary>
    public static class FunctionalDescParser
    {
        private static Logger logger = Logger.GetLogger();

        /// <summary>
        /// 機能キー名の別名定義
        /// </summary>
        private static Dictionary<string, string> functionalKeyAliases = new Dictionary<string, string>();

        /// <summary>
        /// 機能名の別名を追加する (ex: "↑" → "Up")
        /// </summary>
        /// <param name="alias">別名</param>
        /// <param name="name">機能名</param>
        public static void AddFunctionalKeyAlias(string alias, string name)
        {
            logger.Info(() => $"CALLED: alias={alias}, keyname={name}");
            functionalKeyAliases._safeAdd(alias, name);
        }

        /// <summary>
        /// 機能記述子の開始を示すか？ ("!{" で始まる)
        /// </summary>
        /// <param name="str"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static bool IsFunctionalDescStart(string str, int pos)
        {
            return (str != null && (pos + 1) < str.Length && str[pos] == '!' && str[pos + 1] == '{');    // "!{"
        }

        public static string MakeFunctionalDesc(string name, bool right = false, int mod = 0, int repeatCount = 1)
        {
            var sb = new StringBuilder();
            sb.Append("!{");
            if (right) sb.Append('>');
            if (mod == FunctionalKeyInfo.Ctrl) sb.Append('^');
            if (mod == FunctionalKeyInfo.Shift) sb.Append('+');
            if (mod == FunctionalKeyInfo.Alt) sb.Append('!');
            sb.Append(name);
            if (repeatCount > 1) {
                sb.Append(' ');
                sb.Append(repeatCount.ToString());
            }
            sb.Append('}');
            return sb.ToString();
        }

        public static FunctionalKeyInfo Parse(string str, int pos)
        {
            logger.DebugH(() => $"CALLED: str={str}, pos={pos}");

            if (!IsFunctionalDescStart(str, pos)) {
                return null;
            }

            int startPos = pos;
            pos += 2;   // skip "!{"

            bool right = false;
            int mod = 0;
            bool bRepeatCnt = false;
            int repeatCount = 0;
            var sb = new StringBuilder();
            while (pos < str.Length) {
                var ch = str[pos++];
                if (ch == '}') break;
                if (bRepeatCnt) {
                    if (ch >= '0' && ch <= '9') {
                        repeatCount = repeatCount * 10 + (ch - '0');
                    }
                } else {
                    if (ch == '<') {
                        right = false;
                    } else if (ch == '>') {
                        right = true;
                    } else if (ch == '^') {
                        mod = FunctionalKeyInfo.Ctrl;
                    } else if (ch == '+') {
                        mod = FunctionalKeyInfo.Shift;
                    } else if (ch == '!') {
                        mod = FunctionalKeyInfo.Alt;
                    } else if (ch == ' ' || ch == ',' || ch == ':' || ch == '/') {
                        bRepeatCnt = true;
                    } else {
                        sb.Append(ch);
                    }
                }
            }

            if (sb.Length == 0) {
                return new FunctionalKeyInfo(false, 0, null, null, 0, 0, startPos, pos);
            }

            if (repeatCount == 0) repeatCount = 1;

            string alias = sb.ToString();
            string name = functionalKeyAliases._safeGet(alias, alias);
            logger.Info(() => $"alias={alias}, key={name}");
            uint vkey = DecoderKeyVsVKey.GetFuncVkeyByName(name);
            //logger.DebugH(() => $"vkey={vkey:x} by FuncKey");
            if (vkey == 0) vkey = AlphabetVKeys.GetAlphabetVkeyByName(name);

            return new FunctionalKeyInfo(right, mod, name, alias, vkey, repeatCount, startPos, pos);
        }
    }

    /// <summary>
    /// 3項演算子パーサ
    /// </summary>
    public static class TernaryOperatorParser
    {
        //private static Logger logger = Logger.GetLogger();

        private static System.Text.RegularExpressions.Regex reTernaryOperator = new System.Text.RegularExpressions.Regex(@"\(([^)]+)\)\?\(([^)]+)\):\(([^)]+)\)");

        /// <summary>
        /// 三項演算子か
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsTernaryOperator(string str)
        {
            return str._getFirst() == '(' && str.Last() == ')' && str._reMatch(reTernaryOperator);
        }

        /// <summary>
        /// 三項演算子 (Q)?(A):(B) 形式だったら、Q に該当するウィンドウクラスか否かを判定し、当ならAを、否ならBを返す。<br/>
        /// (Q)?(A):(B) 形式でなければ、null を返す。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Parse(string str, string localName = null)
        {
            if (str._getFirst() == '(' && str.Last() == ')') {
                var items = str._reScan(reTernaryOperator);
                //logger.Warn($"items({items._safeCount()})={items._join("||")}");
                if (items._safeCount() == 4) {
                    string activeWinClassName = ActiveWindowHandler.Singleton?.ActiveWinClassName._toLower();
                    var names = items[1]._toLower()._split('|');
                    //logger.Warn($"str={str}, localName={localName}, winName={activeWinClassName}");
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

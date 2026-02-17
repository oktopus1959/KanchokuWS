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
        private static int Ctrl = 1;
        private static int Shift = 2;
        private static int Alt = 4;

        public bool IsRight { get; }
        public int Modifier { get; }
        public string Name { get; }
        public string Alias { get; }
        public uint VKey { get; }
        public KeyOrFunction KeyOrFunc { get; }
        public int RepeatCount { get; } = 1;
        public int StartPos { get; } = 0;
        public int NextPos { get; } = 0;

        public FunctionalKeyInfo(bool right, int mod, string name, string alis, uint vkey, KeyOrFunction keyOrFunc, int count, int start, int next)
        {
            IsRight = right;
            Modifier = mod;
            Name = name;
            Alias = alis;
            VKey = vkey;
            KeyOrFunc = keyOrFunc;
            RepeatCount = count;
            StartPos = start;
            NextPos = next;
        }

        public static bool IsCtrl(int mod)
        {
            return (mod & Ctrl) != 0;
        }

        public static int SetCtrl(int mod)
        {
            return mod | Ctrl;
        }

        public bool IsCtrl()
        {
            return IsCtrl(Modifier);
        }

        public static bool IsShift(int mod)
        {
            return (mod & Shift) != 0;
        }

        public static int SetShift(int mod)
        {
            return mod | Shift;
        }

        public bool IsShift()
        {
            return IsShift(Modifier);
        }

        public static bool IsAlt(int mod)
        {
            return (mod & Alt) != 0;
        }

        public static int SetAlt(int mod)
        {
            return mod | Alt;
        }

        public bool IsAlt()
        {
            return IsAlt(Modifier);
        }

        public override string ToString()
        {
            string modifierStr = "";
            if (IsCtrl()) modifierStr += "Ctrl";
            if (IsShift()) {
                if (modifierStr.Length > 0) modifierStr += "|";
                modifierStr += "Shift";
            }
            if (IsAlt()) {
                if (modifierStr.Length > 0) modifierStr += "|";
                modifierStr += "Alt";
            }
            if (modifierStr.Length == 0) modifierStr = "None";
            return $"FunctionalKeyInfo(Right={IsRight}, Modifier={modifierStr}, Name={Name}, Alias={Alias}, VKey={VKey:x}, KeyOfFunc={(KeyOrFunc != null ? KeyOrFunc.ToString() : "null")}, RepeatCount={RepeatCount}, StartPos={StartPos}, NextPos={NextPos})";
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

        /// <summary>
        /// 機能記述子の作成
        /// </summary>
        /// <param name="name"></param>
        /// <param name="right"></param>
        /// <param name="mod"></param>
        /// <param name="repeatCount"></param>
        /// <returns></returns>
        public static string MakeFunctionalDesc(string name, bool right = false, int mod = 0, int repeatCount = 1)
        {
            var sb = new StringBuilder();
            sb.Append("!{");
            if (right) sb.Append('>');
            if (FunctionalKeyInfo.IsCtrl(mod)) sb.Append('^');
            if (FunctionalKeyInfo.IsShift(mod)) sb.Append('+');
            if (FunctionalKeyInfo.IsAlt(mod)) sb.Append('!');
            sb.Append(name);
            if (repeatCount > 1) {
                sb.Append(' ');
                sb.Append(repeatCount.ToString());
            }
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// 機能記述子のパース
        /// </summary>
        /// <param name="str"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static FunctionalKeyInfo Parse(string str, int pos)
        {
            if (!IsFunctionalDescStart(str, pos)) {
                return null;
            }

            logger.Info(() => $"ENTER: str={str}, pos={pos}");

            int startPos = pos;
            pos += 2;   // skip "!{"

            bool right = false;
            int mod = 0;

            bool checkModifier(char ch)
            {
                bool isModifier = true;
                if (ch == '<') {
                    right = false;
                } else if (ch == '>') {
                    right = true;
                } else if (ch == '^') {
                    mod = FunctionalKeyInfo.SetCtrl(mod);
                } else if (ch == '+') {
                    mod = FunctionalKeyInfo.SetShift(mod);
                } else if (ch == '!') {
                    mod = FunctionalKeyInfo.SetAlt(mod);
                } else {
                    isModifier = false;
                }
                return isModifier;
            }

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
                    if (checkModifier(ch)) {
                        // modifier だったら何もしない
                    } else if (ch == ' ' || ch == ',' || ch == ':' || ch == '/') {
                        bRepeatCnt = true;
                    } else {
                        sb.Append(ch);
                    }
                }
            }

            if (sb.Length == 0) {
                return new FunctionalKeyInfo(false, 0, null, null, 0, null, 0, startPos, pos);
            }

            if (repeatCount == 0) repeatCount = 1;

            string alias = sb.ToString();
            string name = functionalKeyAliases._safeGet(alias, alias);
            logger.Info(() => $"PROGRESS: alias={alias}, name={name}");
            while (name._notEmpty() && checkModifier(name[0])) {
                // 正式名の先頭がモディファイア指定だったら、再度モディファイア指定をチェックする (ex: name="^M" → mod=Ctrl, name="M")
                name = name.Substring(1);
            }
            uint vkey = DecoderKeyVsVKey.GetFuncVkeyByName(name);
            //logger.DebugH(() => $"vkey={vkey:x} by FuncKey");
            if (vkey == 0) vkey = AlphabetVKeys.GetAlphabetVkeyByName(name);
            var keyOrFunc = SpecialKeysAndFunctions.GetKeyOrFuncByName(name);

            var resultInfo = new FunctionalKeyInfo(right, mod, name, alias, vkey, keyOrFunc, repeatCount, startPos, pos);

            logger.Info(() => $"LEAVE: {resultInfo}");

            return resultInfo;
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

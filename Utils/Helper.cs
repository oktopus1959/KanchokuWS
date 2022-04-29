using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Utils
{
    public static partial class Helper
    {
        /// <summary>
        /// 引数を配列にして返す。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T[] Array<T>(params T[] array)
        {
            return array;
        }

        /// <summary>
        /// 引数をListにして返す。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static List<T> MakeList<T>(params T[] array)
        {
            return new List<T>(array);
        }

        /// <summary>
        /// size個の要素(default(T))を持つリストを作成して返す。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="size"></param>
        /// <returns></returns>
        public static List<T> NewList<T>(int size = 0)
        {
            return new List<T>(new T[size]);
        }

        /// <summary>
        /// n個の空文字列からなる配列を作成して返す
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static string[] createEmptyStringArray(int n)
        {
            string[] result = new string[n];
            for (int i = 0; i < n; ++i) result[i] = "";
            return result;
        }

        /// <summary>
        /// 文字列 name が表す変数名のインデックスを返す。"A"->0, "B"->1, ..., "Z"->25 となる。
        /// name が空文字列または範囲外なら -1 を返す。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int getVarNameIndex(string name)
        {
            return name._isEmpty() ? -1 : getVarNameIndex(name[0]);
        }

        /// <summary>
        /// 文字 name が表す変数名のインデックスを返す。'A'->0, 'B'->1, ..., 'Z'->25 となる。
        /// name が空文字列または範囲外なら -1 を返す。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int getVarNameIndex(char name)
        {
            return name >= 'A' && name <= 'Z' ? name - 'A' : name >= 'a' && name <= 'z' ? name - 'a' : -1;
        }

        /// <summary>
        /// 整数 idx が表す変数名 (A ～ Z) を返す。0=>A, 1=>B, ..., 25=>Z となる。idx が範囲外なら '$' を返す。
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public static string getVarName(int idx)
        {
            return idx >= 0 && idx <= 25 ? Convert.ToChar('A' + idx).ToString() : "$";
        }

        public const int ERROR_INT_VAL = -99999999;


        /// <summary>
        /// 標準入力(コンソール)から文字列を入力する。EOFになったら null を返す。
        /// </summary>
        /// <returns></returns>
        public static string readFromConsole()
        {
            return Console.ReadLine();
        }

        /// <summary>
        /// ファイルまたは標準入力から1行ずつ読んで返すイテレータ (path が空または null なら標準入力)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IEnumerable<string> linesInFile(string path = null)
        {
            if (path._notEmpty()) {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(path)) {
                    string line;
                    while ((line = sr.ReadLine()) != null) {
                        yield return line;
                    }
                }
            } else {
                string line;
                while ((line = Console.ReadLine()) != null) {
                    yield return line;
                }
            }
        }

        //-----------------------------------------------------------------------------
        // 文字列関連
        //-----------------------------------------------------------------------------
        private static Regex reNumeric = new Regex(@"\A\d+\z");

        /// <summary>
        /// 文字列が数字列かどうかの判定
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsNumeric(string str)
        {
            return reNumeric.Match(str).Success;
        }

        /// <summary>
        /// 削除マーク ('~') の付加されたIDか
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool isDeletedId(string id)
        {
            return !id._isEmpty() && id[0] == '~';
        }

        /// <summary>
        /// コマンドIDと確認番号からコマンド登録IDを作成して返す
        /// </summary>
        /// <param name="cmdId"></param>
        /// <param name="wchId"></param>
        /// <returns></returns>
        public static string MakeCmdRegId(string cmdId, string wchId)
        {
            return string.Format("{0,-8}{1,2}", cmdId._toSafe(), wchId._isEmpty() ? "  " : ("00" + wchId._toSafe())._safeSubstring(-2));
        }

        /// <summary>
        /// 正規表現パターンを通常文字列としてマッチさせるために、メタ文字をエスケープする
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static string _reEscape(this string pattern)
        {
            return pattern?.
                _escapeChar(@"\").
                _escapeChar(".").
                _escapeChar("?").
                _escapeChar("*").
                _escapeChar("+").
                _escapeChar("-").
                _escapeChar("(").
                _escapeChar(")").
                _escapeChar("[").
                _escapeChar("]").
                _escapeChar("{").
                _escapeChar("}");
        }

        /// <summary>
        /// ch で指定される1文字を \ でエスケープした結果を返す
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static string _escapeChar(this string str, string ch)
        {
            return str?.Replace(ch, @"\" + ch);
        }

        /// <summary>
        /// パターン文字列から正規表現オブジェクトを生成する (bIcase = true なら、大文字小文字を無視する正規表現を生成)
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="bIcase"></param>
        /// <returns></returns>
        public static Regex _toRegex(this string pattern, bool bIcase = false)
        {
            return new Regex(pattern, RegexOptions.ECMAScript | (bIcase ? RegexOptions.IgnoreCase : 0));
        }

        /// <summary>
        /// 正規表現による IsMatch の結果を返す。 (bIcase = true なら、大文字小文字を無視)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static bool _reMatch(this string text, string pattern, bool bIcase = false)
        {
            if (text == null) return false;
            return Regex.IsMatch(text, pattern, RegexOptions.ECMAScript | (bIcase ? RegexOptions.IgnoreCase : 0));
        }

        /// <summary>
        /// 正規表現による Match の結果を返す。
        /// </summary>
        public static bool _reMatch(this string text, Regex pattern)
        {
            return pattern._reMatch(text);
        }

        /// <summary>
        /// 正規表現による Match の結果を返す。
        /// </summary>
        public static bool _reMatch(this Regex pattern, string text)
        {
            if (text == null) return false;
            return pattern.Match(text)?.Success ?? false;
        }

        /// <summary>
        /// 正規表現による IsMatch の結果を返す。(大文字小文字無視)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static bool _reMatchIcase(this string text, string pattern)
        {
            if (text == null) return false;
            return Regex.IsMatch(text, pattern, RegexOptions.ECMAScript | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 正規表現による IsMatch で参照にマッチした文字列のリストを返す。 (bIcase = true なら、大文字小文字を無視)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static List<string> _reScan(this string text, string pattern, bool bIcase = false)
        {
            var result = new List<string>();
            if (text != null) {
                try {
                    var m = Regex.Match(text, pattern, RegexOptions.ECMAScript | (bIcase ? RegexOptions.IgnoreCase : 0));
                    if (m?.Success ?? false) {
                        foreach (Group g in m.Groups) {
                            if (g?.Success ?? false) {
                                result.Add(g.Value._toSafe());
                            }
                        }
                    }
                }
                catch (Exception) {
                }
            }
            return result;
        }

        /// <summary>
        /// 正規表現による Match で参照にマッチした文字列のリストを返す。
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static List<string> _reScan(this string text, Regex pattern)
        {
            return pattern._reScan(text);
        }

        /// <summary>
        /// 正規表現による Match で参照にマッチした文字列のリストを返す。
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static List<string> _reScan(this Regex pattern, string text)
        {
            var result = new List<string>();
            if (text != null) {
                try {
                    var m = pattern.Match(text);
                    if (m?.Success ?? false) {
                        foreach (Group g in m.Groups) {
                            if (g?.Success ?? false) {
                                result.Add(g.Value._toSafe());
                            }
                        }
                    }
                }
                catch (Exception) {
                }
            }
            return result;
        }

        /// <summary>
        /// 正規表現による全置換
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <param name="repl"></param>
        /// <returns></returns>
        public static string _reReplace(this string text, string pattern, string repl)
        {
            if (text._isEmpty()) return "";
            return Regex.Replace(text, pattern, repl, RegexOptions.ECMAScript);
        }

        /// <summary>
        /// 正規表現による全置換(大文字小文字無視)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <param name="repl"></param>
        /// <returns></returns>
        public static string _reReplaceIcase(this string text, string pattern, string repl)
        {
            if (text._isEmpty()) return "";
            return Regex.Replace(text, pattern, repl, RegexOptions.ECMAScript | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 正規表現による Split
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static string[] _reSplit(this string text, string pattern)
        {
            if (text == null) return new string[0];
            return Regex.Split(text, pattern, RegexOptions.ECMAScript);
        }

        /// <summary>
        /// コマンド監視IDからコマンド発行ID部分を切り出す (先頭8文字、または空白の前まで)
        /// </summary>
        /// <param name="cmdRegId"></param>
        /// <returns></returns>
        public static string ExtractCmdId(string cmdRegId)
        {
            return cmdRegId._reSplit(" +")._getFirst()._toSafe()._strip()._safeSubstring(0,8);
        }

        /// <summary>
        /// コマンド監視IDから確認番号部分を切り出す (9文字目以降、または空白の後から2文字分)
        /// </summary>
        /// <param name="cmdRegId"></param>
        /// <returns></returns>
        public static string ExtractWchId(string cmdRegId)
        {
            var wchId = cmdRegId._reSplit(" +")._getSecond()._toSafe()._strip()._safeSubstring(0, 2);
            return wchId._notEmpty() ? wchId : cmdRegId._strip()._safeSubstring(8, 2);
        }

        //-----------------------------------------------------------------------------
        // ファイル・ディレクトリ関連
        //-----------------------------------------------------------------------------
        /// <summary>
        /// 自プロセスのプログラムパスを取得
        /// </summary>
        /// <returns></returns>
        public static string GetExePath()
        {
            try
            {
                return System.Reflection.Assembly.GetEntryAssembly().Location;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 実行プログラムの格納場所(ディレクトリ)を得る
        /// </summary>
        /// <returns></returns>
        public static string GetExeDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        }

        /// <summary>
        /// カレントディレクトリを変更する。path == null なら exe のあるディレクトリに変更する。
        /// 成功したら true, 失敗したら false を返す
        /// </summary>
        /// <param name="path"></param>
        public static bool SetCurrentDirectory(string path = null)
        {
            if (path._isEmpty()) path = GetExeDirectory();
            try
            {
                if (DirectoryExists(path))
                {
                    Directory.SetCurrentDirectory(path);
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        /// <summary>
        /// 指定パスのファイル名部分を取得する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetFileName(string path)
        {
            return path._notEmpty() ? Path.GetFileName(path) : "";
        }

        /// <summary>
        /// 指定パスの親ディレクトリを取得する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetDirectoryName(string path)
        {
            return path._notEmpty() ? Path.GetDirectoryName(path) : "";
        }

        /// <summary>
        /// 指定パスは絶対パスか(ルートディレクトリを含む)か
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsAbsolutePath(string path)
        {
            return path._notEmpty() ? Path.IsPathRooted(path) : false;
        }

        /// <summary>
        /// 2つのパスを結合する
        /// </summary>
        public static string JoinPath(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        /// <summary>
        /// 3つのパスを結合する
        /// </summary>
        public static string JoinPath(string path1, string path2, string path3)
        {
            return Path.Combine(path1, path2, path3);
        }

        /// <summary>
        /// 2つのパスを結合する。path2 が絶対パスなら path2自身を返す。
        /// </summary>
        public static string JoinAbsPath(string path1, string path2)
        {
            return path2._isAbsPath() ? path2 : Path.Combine(path1, path2);
        }

        /// <summary>
        /// ファイルが存在するかテストする
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool FileExists(string filePath)
        {
            return filePath._isEmpty() ? false :File.Exists(filePath);
        }

        /// <summary>
        /// ディレクトリが存在するかテストする
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public static bool DirectoryExists(string dirPath)
        {
            return dirPath._isEmpty() ? false : Directory.Exists(dirPath);
        }

        /// <summary>
        /// ディレクトリが存在しなければ、ディレクトリを作成する (深い階層まで作成する)
        /// </summary>
        public static bool CreateDirectory(string dirPath)
        {
            try
            {
                if (!DirectoryExists(dirPath))
                    Directory.CreateDirectory(dirPath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 指定ディレクトリ直下のファイル一覧を取得する<para/>
        /// dirPath が空ならカレントディレクトリ、 pattern が空なら '*' とする
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static List<string> GetFiles(string dirPath, string pattern, bool bRecursive = false)
        {
            try
            {
                dirPath = dirPath._orElse(".");
                pattern = pattern._orElse("*");
                var opt = bRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                if (DirectoryExists(dirPath))
                    return Directory.GetFiles(dirPath, pattern, opt).ToList();
            }
            catch (Exception)
            {
            }
            return new List<string>();
        }

        /// <summary>
        /// 指定ディレクトリ直下のディレクトリの一覧を取得する<para/>
        /// dirPath が空ならカレントディレクトリ、 pattern が空なら '*' とする
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static List<string> GetDirectories(string dirPath, string pattern, bool bRecursive = false)
        {
            try
            {
                dirPath = dirPath._orElse(".");
                pattern = pattern._orElse("*");
                var opt = bRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetDirectories(dirPath, pattern, opt).ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public static bool CopyFile(string srcPath, string destPath)
        {
            try {
                System.IO.File.Copy(srcPath, destPath);
                return true;
            } catch {
                return false;
            }
        }

        public static bool MoveFile(string srcPath, string destPath)
        {
            try {
                System.IO.File.Move(srcPath, destPath);
                return true;
            } catch {
                return false;
            }
        }


        /// <summary>
        /// データフォルダ配下(または絶対パス)のテキストファイルの行数をカウントする。エラーが発生したら -1 を返す
        /// </summary>
        /// <param name="filePath">Dataフォルダ配下の相対パスまたは絶対パス</param>
        /// <returns></returns>
        public static int GetFileLineNum(string filePath, Action<Exception> errHandler = null)
        {
            try {
                var data = GetFileContent(filePath, errHandler);
                if (data == null) return -1;
                int len = data.Length;
                Func<int, int> findNL = (idx) => {
                    int orig = idx;
                    while (idx < len) {
                        if (data[idx++] == '\n') return idx;
                    }
                    return orig < len ? len : -1;
                };
                int count = 0;
                int pos = 0;
                while ((pos = findNL(pos)) >= 0) ++count;
                return count;
            } catch (Exception e) {
                errHandler?.Invoke(e);
                return -1;
            }
        }

        /// <summary>
        /// データフォルダ配下(または絶対パス)のテキストファイルの全内容を指定のエンコードで取得する。エラーが発生したら null を返す
        /// </summary>
        /// <param name="filePath">Dataフォルダ配下の相対パスまたは絶対パス</param>
        /// <returns></returns>
        public static string GetFileContent(string filePath, Encoding enc, Action<Exception> errHandler = null)
        {
            try {
                using (var fs = new System.IO.FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    using (var sr = new System.IO.StreamReader(fs, enc)) {
                        return sr.ReadToEnd();
                    }
                }
            } catch (Exception e) {
                errHandler?.Invoke(e);
                return null;
            }
        }

        /// <summary>
        /// データフォルダ配下(または絶対パス)のファイルの全内容をデフォルエンコードで取得する。エラーが発生したら null を返す
        /// </summary>
        /// <param name="filePath">Dataフォルダ配下の相対パスまたは絶対パス</param>
        /// <returns></returns>
        public static string GetFileContent(string filePath, Action<Exception> errHandler = null)
        {
            try {
                using (var fs = new System.IO.FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    using (var sr = new System.IO.StreamReader(fs)) {
                        return sr.ReadToEnd();
                    }
                }
            } catch (Exception e) {
                errHandler?.Invoke(e);
                return null;
            }
        }

        /// <summary>
        /// テキストファイルの最後のNバイトを読む。エラーの場合は null を返す
        /// </summary>
        /// <param name="filePath">Dataフォルダ配下の相対パスまたは絶対パス</param>
        /// <returns></returns>
        public static string GetFileTail(string filePath, int size, Action<Exception> errHandler = null)
        {
            try {
                using (var fs = new System.IO.FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    using (var sr = new System.IO.StreamReader(fs)) {
                        fs.Seek(-size, System.IO.SeekOrigin.End);
                        return sr.ReadToEnd();
                    }
                }
            } catch (Exception e) {
                errHandler?.Invoke(e);
                return null;
            }
        }

        /// <summary>
        /// absPath を rootPath から相対パスに変換する。先頭部が一致しなければ、absPath を返す。
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="absPath"></param>
        /// <returns></returns>
        public static string GetRelativePart(string rootPath, string absPath)
        {
            rootPath = rootPath._canonicalPath();
            var len = rootPath.Length;
            absPath = absPath._canonicalPath();
            if (len > 0 && absPath._safeSubstring(0, len)._equalsTo(rootPath))
                return absPath._safeSubstring(len).TrimStart(new char[] { '\\', '/' });
            else
                return absPath;
        }

        /// <summary>
        /// dirPath 配下で filePattern にマッチするファイルを削除する<para/>
        /// エラーが発生したら、エラーメッセージを返す
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="filePattern"></param>
        public static string DeleteFiles(string dirPath, string filePattern)
        {
            return DeleteFiles(dirPath, Directory.GetFiles(dirPath, filePattern));
        }

        /// <summary>
        /// dirPath 配下で files に属するファイルを削除する<para/>
        /// エラーが発生したら、エラーメッセージを返す
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="files"></param>
        public static string DeleteFiles(string dirPath, IEnumerable<string> files)
        {
            if (dirPath._isEmpty() || files._isEmpty()) return null;
            try
            {
                foreach (var name in files)
                {
                    File.Delete(dirPath._joinPath(name));
                }
                return null;
            }
            catch (Exception e)
            {
                return e._getErrorMsg();
            }
        }

        /// <summary>
        /// ファイル path を削除する。エラーが発生したらエラーメッセージそ返す。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
                return null;
            }
            catch (Exception e)
            {
                return e._getErrorMsg();
            }
        }

        /// <summary>
        /// 空のディレクトリを削除する
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public static string RemoveEmptyDirectory(string dirPath)
        {
            try
            {
                bool flag = true;
                foreach (var name in Directory.GetFileSystemEntries(dirPath))
                {
                    if (!(name._equalsTo(".") || name._equalsTo("..")))
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    Directory.Delete(dirPath);
                }
                return null;
            }
            catch (Exception e)
            {
                return e._getErrorMsg();
            }
        }

        //-----------------------------------------------------------------------------
        // プロセス関連
        //-----------------------------------------------------------------------------
        /// <summary>
        /// プロセス起動。起動に失敗したら例外を投げる
        /// </summary>
        public static void StartProcess(string exePath, string arguments, bool withoutConsole = true)
        {
            using (System.Diagnostics.Process p = new System.Diagnostics.Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.FileName = exePath;
                p.StartInfo.Arguments = arguments;

                //起動
                p.Start();
            }
        }

        /// <summary>
        /// 標準入力をリダイレクトしてプロセス起動。起動に失敗したら例外を投げる
        /// </summary>
        /// <param name="exePath"></param>
        /// <param name="arguments"></param>
        public static System.Diagnostics.Process StartProcessWithStdInRedirected(string exePath, string arguments)
        {
            var process = new System.Diagnostics.Process();
            try
            {
                process.StartInfo.FileName = exePath;                      //起動するファイルのパスを指定する
                process.StartInfo.Arguments = arguments;                   //コマンドライン引数を指定する
                process.StartInfo.CreateNoWindow = true;                   // コンソール・ウィンドウを開かない
                process.StartInfo.UseShellExecute = false;                 // シェル機能を使用しない
                process.StartInfo.RedirectStandardInput = true;            // 標準入力を使う
                process.Start();
                return process;
            }
            catch (Exception)
            {
                process?.Dispose();
                throw;
            }
        }


        //-----------------------------------------------------------------------------
        // 日付・時刻関連
        //-----------------------------------------------------------------------------
        /// <summary>
        /// 時刻(HH:mm)文字列 time が from と to の間にある(toを含む)か判定する。from > to なら to &lt; 00:00 &lt; from な時間帯として判定する
        /// </summary>
        /// <param name="time"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static bool IsInTimeRange(string time, string from, string to)
        {
            if (from._le(to))
            {
                return time._ge(from) && time._le(to);
            }
            else
            {
                return time._ge(from) || time._le(to);
            }
        }

        /// <summary>
        /// HH:MM 形式の文字列か否かの判定
        /// </summary>
        /// <param name="strTime"></param>
        /// <returns></returns>
        public static bool MatchTimePattern(string strTime, string delims = ":")
        {
            return strTime._notEmpty() && Regex.IsMatch(strTime, @"([01]\d|2[0-3])[" + delims + @"][0-5]\d");
        }

        /// <summary>
        /// dt で指定された日付と HH:mm[:ss] 形式の時刻から DateTime を構築して返す
        /// </summary>
        /// <returns></returns>
        public static DateTime ParseTime(DateTime dt, string time)
        {
            return ($"{DateString(dt)} {time}")._parseDateTime();
        }

        /// <summary>
        /// dt で指定された月の末尾の日にちを返す
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int LastDayInMonth(DateTime dt)
        {
            return DateTime.DaysInMonth(dt.Year, dt.Month);
        }

        /// <summary>
        /// dt で指定された月の末尾の年月日を返す
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime LastDateOfMonth(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, LastDayInMonth(dt));
        }

        /// <summary>
        /// 秒単位を切り捨てたDateTimeを返す
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime RoundSecond(DateTime dt)
        {
            return DateTime.Parse(DateTimeString(dt));
        }

        /// <summary>
        /// 日時文字列 dt が正しく Parse できるかチェックする。エラーの場合はエラーメッセージを返す。正しくパースできたら null を返す。
        /// </summary>
        /// <param name="dt"></param>
        public static string CheckDateTime(string dt)
        {
            try {
                DateTime.Parse(dt);
                return null;
            } catch (Exception) {
                return $"指定文字列が間違っています: {dt}";
            }
        }

        /// <summary>
        /// 指定日時の翌日の 00:00 の日時を返す
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime InitialTimeOnNextDay(DateTime dt)
        {
            return ParseTime(dt.AddDays(1), "00:00");
        }

        /// <summary>
        /// 現在の yyyy/MM/dd HH:mm 形式の日時文字列を返す
        /// </summary>
        /// <returns></returns>
        public static string DateTimeString()
        {
            return DateTimeString(DateTime.Now);
        }

        /// <summary>
        /// yyyy/MM/dd HH:mm 形式の日時文字列を返す。
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string DateTimeString(DateTime dt)
        {
            return dt.ToString("yyyy/MM/dd HH:mm");
        }

        /// <summary>
        /// 現在の yyyy/MM/dd 形式の日付文字列を返す
        /// </summary>
        /// <returns></returns>
        public static string DateString()
        {
            return DateString(DateTime.Now);
        }

        /// <summary>
        /// yyyy/MM/dd 形式の日付文字列を返す
        /// </summary>
        /// <returns></returns>
        public static string DateString(DateTime dt)
        {
            return dt.ToString("yyyy/MM/dd");
        }

        /// <summary>
        /// 現在の MM/dd 形式の短い日付文字列を返す
        /// </summary>
        /// <returns></returns>
        public static string ShortDateString()
        {
            return ShortDateString(DateTime.Now);
        }

        /// <summary>
        /// MM/dd 形式の短い現在日付文字列を返す
        /// </summary>
        /// <returns></returns>
        public static string ShortDateString(DateTime dt)
        {
            return dt.ToString("MM/dd");
        }

        /// <summary>
        /// 現在の HH:mm:ss 形式の時刻文字列を返す
        /// </summary>
        /// <returns></returns>
        public static string TimeString()
        {
            return TimeString(DateTime.Now);
        }

        /// <summary>
        /// HH:mm:ss 形式の現在時刻文字列を返す
        /// </summary>
        /// <returns></returns>
        public static string TimeString(DateTime dt)
        {
            return dt.ToString("HH:mm:ss");
        }

        /// <summary>
        /// 現在の HH:mm 形式の短い時刻文字列を返す
        /// </summary>
        /// <returns></returns>
        public static string ShortTimeString()
        {
            return ShortTimeString(DateTime.Now);
        }

        /// <summary>
        /// HH:mm 形式の短い現在時刻文字列を返す
        /// </summary>
        /// <returns></returns>
        public static string ShortTimeString(DateTime dt)
        {
            return dt.ToString("HH:mm");
        }
        /// <summary>
        /// 時刻同士の差分秒数を計算する (time1 - time2 の秒数を返す。 time1 &lt; time2 なら (time1 - time2) + 86400 を返す)
        /// <para>time1, time2 は、 HH:mm[:ss] 形式</para>
        /// </summary>
        public static int DiffSeconds(string time1, string time2)
        {
            return DiffSeconds(time1._parseDateTime(), time2._parseDateTime());
        }

        /// <summary>
        /// 時刻同士の差分秒数を計算する (time1 - time2 の秒数を返す。 time1 &lt; time2 なら (time1 - time2) + 86400 を返す)
        /// <para>time1 は DateTime、time2 は HH:mm[:ss] 形式</para>
        /// </summary>
        public static int DiffSeconds(DateTime time1, DateTime time2)
        {
            var sec = (int)(time1 - time2).TotalSeconds;
            if (sec < 0) sec += 86400;
            return sec;
        }

        /// <summary>
        /// 現在時刻と指定時刻の差分秒数を計算する (Now() - time の秒数を返す。 Now() &lt; time なら (Now() - time) + 86400 を返す)
        /// <para>time は、 HH:mm[:ss] 形式</para>
        /// </summary>
        public static int DiffSecondsToNow(string time)
        {
            return DiffSeconds(DateTime.Now, time._parseDateTime());
        }

        /// <summary>
        /// 「dt1の日付 - dt2の日付」を表す TimeSpan を返す。
        /// <para>引数にエラーがあれば TimeSpan.MaxValue を返す。</para>
        /// </summary>
        /// <param name="dt1"></param>
        /// <param name="dt2"></param>
        /// <returns></returns>
        public static TimeSpan DiffTimeSpan(string dt1, string dt2)
        {
            try {
                if (dt1._notEmpty() && dt2._notEmpty()) {
                    return DateTime.Parse(dt1._split(' ')[0]) - DateTime.Parse(dt2._split(' ')[0]);
                }
            } catch (Exception) {
            }
            return TimeSpan.MaxValue;
        }

        /// <summary>
        /// HH:mm[:ss] 形式の時刻を TimeSpan に変換する。
        /// <para>TimeSpan に変換できないときは例外を投げる。</para>
        /// </summary>
        /// <param name="hh_mm"></param>
        /// <returns></returns>
        public static TimeSpan GetTimeSpan(string hh_mm)
        {
            var hhmm = hh_mm._split(':');
            if (hhmm.Length < 2) throw new Exception("Invalid HH:mm format");
            return new TimeSpan(int.Parse(hhmm[0]), int.Parse(hhmm[1]), 0);
        }

        /// <summary>
        /// lhs から rhs を引いた月の差の絶対値を返す。
        /// </summary>
        public static int DiffMonth(DateTime lhs, DateTime rhs)
        {
            var func = new Func<DateTime, DateTime, int>((x, y) => (x.Year - y.Year) * 12 + x.Month - y.Month);
            return (lhs < rhs) ? func(rhs, lhs) : func(lhs, rhs);
        }

        //-----------------------------------------------------------------------------
        // その他
        //-----------------------------------------------------------------------------
        /// <summary>
        /// 自マシンのIPアドレスとホスト名を取得する
        /// </summary>
        /// <returns></returns>
        public static Tuple<string, string> GetMyIpAddressAndHostName(string ip1, string ip2)
        {
            //IPアドレス
            string addr_ip = "";
            //ホスト名
            string hostname = "";

            try
            {

                //ホスト名を取得
                hostname = System.Net.Dns.GetHostName();

                //ホスト名からIPアドレスを取得
                System.Net.IPAddress[] addr_arr = System.Net.Dns.GetHostAddresses(hostname);

                //探す
                addr_ip = "";
                foreach (System.Net.IPAddress addr in addr_arr)
                {
                    string addr_str = addr.ToString();

                    //IPv4 && localhostでない
                    if (addr_str.IndexOf(".") > 0 && !addr_str.StartsWith("127."))
                    {
                        if (addr_ip._isEmpty() || addr_str._equalsTo(ip1) || addr_str._equalsTo(ip2))
                            addr_ip = addr_str;
                    }
                }
            }
            catch (Exception)
            {
                addr_ip = "";
            }

            return new Tuple<string, string>(addr_ip, hostname);
        }

        /// <summary>
        /// Console出力を環境に合わせて適切なエンコードに変更する。
        /// 具体的には、環境変数 TERM が xterm の場合は UTF-8 に変更する。
        /// </summary>
        public static void SetConsoleEncoding()
        {
            if (System.Environment.GetEnvironmentVariable("TERM") == "xterm") Console.OutputEncoding = Encoding.UTF8;
        }

        /// <summary>
        /// 文字列を暗号化する(AES)
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string EncryptText(string text, string aes_iv, string aes_key)
        {
            if (text._isEmpty()) return "";
            try {
                using (var aes = new AesManaged()) {
                    aes.KeySize = 128;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.IV = Encoding.UTF8.GetBytes(aes_iv);
                    aes.Key = Encoding.UTF8.GetBytes(aes_key);
                    aes.Padding = PaddingMode.PKCS7;
                    var byteText = Encoding.UTF8.GetBytes(text);
                    var encryptText = aes.CreateEncryptor().TransformFinalBlock(byteText, 0, byteText.Length);
                    return Convert.ToBase64String(encryptText);
                }
            } catch {
                return text;
            }
        }

        /// <summary>
        /// 文字列を平文化する(AES)
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string DecryptText(string text, string aes_iv, string aes_key)
        {
            if (text._isEmpty()) return "";
            try {
                using (var aes = new AesManaged()) {
                    aes.KeySize = 128;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.IV = Encoding.UTF8.GetBytes(aes_iv);
                    aes.Key = Encoding.UTF8.GetBytes(aes_key);
                    aes.Padding = PaddingMode.PKCS7;
                    byte[] byteText = Convert.FromBase64String(text);
                    var decryptText = aes.CreateDecryptor().TransformFinalBlock(byteText, 0, byteText.Length);
                    return Encoding.UTF8.GetString(decryptText);
                }
            } catch {
                return text;
            }
        }

        public static string decodeOldPassword(string password)
        {
            int nl = password.IndexOf('\n');
            if (nl >= 0) password = password._safeSubstring(0, nl);
            password = password.Trim();
            int cnt = password.Length / 4;
            string[] str = new string[cnt];

            var sb = new StringBuilder();
            int pos = 0;
            for (int i = 0; i < cnt; ++i) {
                int a = password[pos++] - 0x21;
                int b = password[pos++] - 0x21;
                int c = password[pos++] - 0x21;
                int d = password[pos++] - 0x21;
                sb.Append((char)((a * 4) + (b / 16)));
                sb.Append((char)((b % 16) * 16 + (c / 4)));
                sb.Append((char)((c % 4) * 64 + d));
            }
            return sb.ToString().TrimEnd(new char[] { '\0' });
        }

        public static Dictionary<string, int> MakeHeaderColumnDict(string[] headerList)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            for (int i = 0; i < headerList.Length; ++i)
            {
                dict[headerList[i]] = i;
            }
            return dict;
        }

        public static string BoolToYN(bool flag)
        {
            return flag ? "Y" : "N";
        }

        public static string ColumnOrLineToStr(int column)
        {
            return column >= -1 ? column.ToString() : "";
        }

        public static string LengthToStr(int column)
        {
            return column >= 0 ? column.ToString() : "";
        }

        public static bool IsSurrogatePair(char c1, char c2)
        {
            return c1 >= 0xd800 && c1 <= 0xdbff && c2 >= 0xdc00 && c2 <= 0xdfff;
        }

        public static string MakeErrorMsg(int n, string header, string val)
        {
            return $"{n}:「{header}」の値 \"{val}\" が正しくありません。";
        }

        /// <summary>
        /// msec だけ wait する。10msec ごとに DoEvents を呼び出して、アプリが固まらないようにする。<br/>
        /// lock などで排他制御をやっているときに呼んではならない。
        /// </summary>
        /// <param name="msec"></param>
        public static void WaitMilliSeconds(int msec)
        {
            int ticScale = 10;
            if (msec < ticScale) msec = ticScale;
            DateTime _desired = DateTime.Now.AddMilliseconds(msec);
            while (DateTime.Now < _desired) {
                Task.Delay(ticScale).Wait();
                System.Windows.Forms.Application.DoEvents();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Utils
{
    public static class SystemHelper
    {
        /// <summary>
        /// 自プロセスのプログラムパスを取得
        /// </summary>
        /// <returns></returns>
        public static string GetExePath()
        {
            return Helper.GetExePath();
        }

        /// <summary>
        /// 実行プログラムの格納場所(ディレクトリ)を得る
        /// </summary>
        /// <returns></returns>
        public static string GetExeDirectory()
        {
            return Helper.GetExeDirectory();
        }

        private static string s_KanchokuRootDir = null;

        /// <summary>
        /// KanchokuWSのルートディレクトリを取得<br/>
        /// exe のあるディレクトリの1つ上<br/>
        /// ただし …/Debug or …/Release なら 3つ上(プロジェクトのディレクトリのさらに１つうえ)の test (../../../test)
        /// </summary>
        /// <returns></returns>
        public static string FindKanchokuRootDir()
        {
            if (s_KanchokuRootDir._isEmpty())
            {
                var path = GetExeDirectory();
                var basename = path._getFileName().ToUpper();
                if (basename == "DEBUG" || basename == "RELEASE") {
                    var dirPath = path._getDirPath()._getDirPath()._getDirPath();
                    if (GetExePath()._getFileName()._toLower().EndsWith("settingsws.exe")) {
                        dirPath = dirPath._getDirPath();
                    }
                    s_KanchokuRootDir = dirPath._joinPath("test");
                } else {
                    s_KanchokuRootDir = path._getDirPath();
                }
            }
            return s_KanchokuRootDir;
        }

        private static string s_KanchokuDataDir = null;

        /// <summary>
        /// KanchokuWSデータディレクトリの絶対パス取得
        /// </summary>
        /// <returns></returns>
        public static string FindKanchokuDataDir()
        {
            if (s_KanchokuDataDir._isEmpty())
            {
                var exeName = GetExePath()._getFileName().ToUpper();
                var exeDir = GetExeDirectory()._getFileName().ToUpper();
                string dataPath = null;
                Func<string> absPathFinder = FindKanchokuRootDir;
                if (dataPath._isEmpty()) dataPath = "data";         // kanchoku.ini に設定がなければ data とする
                s_KanchokuDataDir = dataPath._isAbsPath()
                    ? dataPath                                      // 絶対パスならそれを採用
                    : absPathFinder()._joinPath(dataPath);          // 相対パスなら KanchokuRoot または AppData/Local 配下に設定
            }
            return s_KanchokuDataDir;
        }

        public static string FindAppDataLocalDir()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)._joinPath("KanchokuWS");
        }

        /// <summary>
        /// 指定のpathが相対パスの場合は、KanchokuRootDir 配下にあるものとして絶対パスに変換する (絶対パスだったらそのまま返す)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string MakeAbsPathUnderKanchokuRootDir(string path)
        {
            return path._isAbsPath() ? path : FindKanchokuRootDir()._joinPath(path);
        }

        /// <summary>
        /// 指定のpathが相対パスの場合は、KanchokuDataDir 配下にあるものとして絶対パスに変換する (絶対パスだったらそのまま返す)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string MakeAbsPathUnderKanchokuDataDir(string path)
        {
            return path._isAbsPath() ? path : FindKanchokuDataDir()._joinPath(path);
        }

        /// <summary>
        /// kanchoku.ini のアクセサを作成して返す。
        /// </summary>
        /// <returns></returns>
        public static IniFileAccessor GetKanchokuIni()
        {
            return new IniFileAccessor(MakeAbsPathUnderKanchokuRootDir(@"kanchoku.ini"));
        }

        /// <summary>
        /// kanchoku.user.ini のアクセサを作成して返す。
        /// </summary>
        /// <returns></returns>
        public static IniFileAccessor GetUserKanchokuIni()
        {
            return new IniFileAccessor(MakeAbsPathUnderKanchokuRootDir(@"kanchoku.user.ini"));
        }

        /// <summary>
        /// 情報メッセージダイアログボックスの表示
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="caption"></param>
        static public void ShowInfoMessageBox(string msg, string caption = null)
        {
            if (string.IsNullOrEmpty(caption)) caption = "情報";
            MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1/*, MessageBoxOptions.DefaultDesktopOnly*/);
        }

        /// <summary>
        /// 警告メッセージダイアログボックスの表示
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="caption"></param>
        static public void ShowWarningMessageBox(string msg, string caption = null)
        {
            if (string.IsNullOrEmpty(caption)) caption = "警告";
            MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1/*, MessageBoxOptions.DefaultDesktopOnly*/);
        }

        /// <summary>
        /// エラーメッセージダイアログボックスの表示
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="caption"></param>
        static public void ShowErrorMessageBox(string msg, string caption = null)
        {
            if (string.IsNullOrEmpty(caption)) caption = "エラー";
            MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1/*, MessageBoxOptions.DefaultDesktopOnly*/);
        }
        /// <summary>
        /// YesNoメッセージダイアログボックスの表示
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="caption"></param>
        static public DialogResult ShowYesNoMessageBox(string msg, string caption = null)
        {
            if (string.IsNullOrEmpty(caption)) caption = "警告";
            return MessageBox.Show(msg, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2/*, MessageBoxOptions.DefaultDesktopOnly*/);
        }

        /// <summary>
        /// YesNoメッセージダイアログボックスの表示。 YES なら true を返す。caption == null なら "警告" になる。"!" マークを表示する。
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="caption"></param>
        static public bool YesNoDialog(string msg, string caption = null)
        {
            return ShowYesNoMessageBox(msg, caption) == DialogResult.Yes;
        }

        /// <summary>
        /// OK/Cancelメッセージダイアログボックスの表示。 OK なら true を返す。caption == null なら "確認" になる。
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="caption"></param>
        static public bool OKCancelDialog(string msg, string caption = null)
        {
            if (string.IsNullOrEmpty(caption)) caption = "確認";
            return MessageBox.Show(msg, caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2/*, MessageBoxOptions.DefaultDesktopOnly*/) == DialogResult.OK;
        }

        /// <summary>
        /// 別スレッドから呼ばれたときに Control に対し Invoke を呼び出すヘルパー。何かエラーがあったらエラー文字列を返す。正常終了の場合は null を返す。
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="action"></param>
        public static string _invoke(this Control ctrl, Action action)
        {
            var sb = new StringBuilder();
            _invokeReent(ctrl, action, sb, false);
            return (sb.Length > 0) ? sb.ToString() : null;
        }

        /// <summary>
        /// 別スレッドから呼ばれたときに Control に対し Invoke を呼び出すヘルパー。何かエラーがあったらloggerにエラー出力し false を返す。正常の場合は true を返す。
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="action"></param>
        public static bool _invoke(this Control ctrl, Action action, Logger logger)
        {
            var sb = new StringBuilder();
            _invokeReent(ctrl, action, sb, false);
            if (sb.Length > 0) {
                logger?.Error(sb.ToString());
                return false;
            }
            return true;
        }

        /// <summary>
        /// 別スレッドから呼ばれたときだけ Invoke を実行する
        /// </summary>
        private static void _invokeReent(Control ctrl, Action action, StringBuilder sb, bool bReent)
        {
            try {
                if (ctrl.InvokeRequired) {
                    if (bReent) {
                        sb.Append("REENTRANT!");
                    } else {
                        ctrl.Invoke(new Action(() => _invokeReent(ctrl, action, sb, true)));
                    }
                } else {
                    action();
                }
            } catch (Exception e) {
                sb.Append(e._getErrorMsgShort());
            }
        }

        // cf. https://www.it-swarm-ja.com/ja/c%23/%E3%83%93%E3%83%AB%E3%83%89%E6%97%A5%E3%82%92%E8%A1%A8%E7%A4%BA%E3%81%99%E3%82%8B/968877930/
        public static DateTime _getLinkerTime(this Assembly assembly, TimeZoneInfo target = null)
        {
            try {
                var filePath = assembly.Location;
                const int c_PeHeaderOffset = 60;
                const int c_LinkerTimestampOffset = 8;

                var buffer = new byte[2048];

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    stream.Read(buffer, 0, 2048);

                var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
                var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
                var Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                var linkTimeUtc = Epoch.AddSeconds(secondsSince1970);

                var tz = target ?? TimeZoneInfo.Local;
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

                return localTime;
            } catch {
                return DateTime.MinValue;
            }
        }
    }
}

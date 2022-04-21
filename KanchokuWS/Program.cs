using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utils;

namespace KanchokuWS
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Logger.LogFilename = "KanchokuWS.log";
            Logger.LogLevel = Settings.GetLogLevel();

            if (args.Length > 0) {
                int idx = 0;
                if (args[idx].StartsWith("--info")) {
                    Logger.EnableInfo();
                    ++idx;
                } else if (args[idx].StartsWith("--debug")) {
                    Logger.EnableDebug();
                    ++idx;
                } else if (args[idx].StartsWith("--trace")) {
                    Logger.EnableTrace();
                    ++idx;
                } else if (args[idx].StartsWith("--warn")) {
                    Logger.EnableWarn();
                    ++idx;
                } else if (args[idx].StartsWith("--error")) {
                    Logger.EnableError();
                    ++idx;
                }
                if (args[idx].EndsWith("sjis")) Logger.UseDefaultEncoding();
            }

            if (!Settings.IsMultiAppEnabled()) {
                if (!MultiAppChecker.CheckMultiApp()) {
                    // 許可されていない多重起動なので、終了
                    return;
                }
            }

            using (var keDispatcher = new KeyboardEventDispatcher()) {
                try {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new FrmKanchoku(keDispatcher));
                } catch (Exception e) {
                    Logger.GetLogger().Error(e._getErrorMsg());
                    MessageBox.Show("エラーが発生しました。詳細はログファイルを参照してください。");
                } finally {
                    MultiAppChecker.Release();
                }
            }
        }
    }

    public static class MultiAppChecker
    {
        static string mutexName = "KanchokuWS-4C1B35AB-0759-41C0-9109-FFFDCE2A0621";

        static System.Threading.Mutex mutex = null;

        public static bool CheckMultiApp()
        {
            //Mutexオブジェクトを作成する
            bool createdNew;
            mutex = new System.Threading.Mutex(true, mutexName, out createdNew);

            //ミューテックスの初期所有権が付与されたか調べる
            if (createdNew == false) {
                //されなかった場合は、すでに起動していると判断して終了
                MessageBox.Show("多重起動はできません。");
                mutex.Close();
                return false;
            }
            return true;
        }

        public static void Release()
        {
            if (mutex != null) {
                //ミューテックスを解放する
                try {
                    mutex.ReleaseMutex();
                } finally {
                    try {
                        mutex.Close();
                    } catch {
                    }
                }
            }
        }
    }
}

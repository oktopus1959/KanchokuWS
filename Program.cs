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
        static string mutexName = "KanchokuWS-4C1B35AB-0759-41C0-9109-FFFDCE2A0621";

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Logger.LogFilename = "KanchokuWS.log";
            Logger.LogLevel = Settings.GetLogLevel();

            if (args.Length > 0) {

                if (args[0].EndsWith("sjis")) Logger.UseDefaultEncoding();
                if (args[0].StartsWith("--info")) {
                    Logger.EnableInfo();
                } else if (args[0].StartsWith("--debug")) {
                    Logger.EnableDebug();
                } else if (args[0].StartsWith("--trace")) {
                    Logger.EnableTrace();
                } else if (args[0].StartsWith("--warn")) {
                    Logger.EnableWarn();
                } else if (args[0].StartsWith("--error")) {
                    Logger.EnableError();
                }
            }

            System.Threading.Mutex mutex = null;
            if (!Settings.IsMultiAppEnabled()) {
                //Mutexオブジェクトを作成する
                bool createdNew;
                mutex = new System.Threading.Mutex(true, mutexName, out createdNew);

                //ミューテックスの初期所有権が付与されたか調べる
                if (createdNew == false) {
                    //されなかった場合は、すでに起動していると判断して終了
                    MessageBox.Show("多重起動はできません。");
                    mutex.Close();
                    return;
                }
            }

            try {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FrmKanchoku());
            } finally {
                if (mutex != null) {
                    //ミューテックスを解放する
                    mutex.ReleaseMutex();
                    mutex.Close();
                }
            }
        }
    }
}

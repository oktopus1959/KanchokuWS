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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmKanchoku());
        }
    }
}

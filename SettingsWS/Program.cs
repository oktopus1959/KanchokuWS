using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using KanchokuWS;

namespace SettingsWS
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!MultiAppChecker.CheckMultiApp()) {
                // 許可されていない多重起動なので、終了
                return;
            }
            try {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                //ScreenInfo.GetScreenInfo();   // ここでやっても正しい値が取得できなかったので、ダイアログのコンストラクタでやるようにした
                Settings.ReadIniFile();
                Application.Run(new KanchokuWS.Gui.DlgSettings(null, null, null));
            } finally {
                MultiAppChecker.Release();
            }
        }
    }

    public static class MultiAppChecker
    {
        static string mutexName = "SettingsWS-4C1B35AB-0759-41C0-9109-FFFDCE2A0621";

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
                mutex.ReleaseMutex();
                mutex.Close();
            }
        }
    }
}

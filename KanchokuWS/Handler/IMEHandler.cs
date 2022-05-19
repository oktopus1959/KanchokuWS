using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Utils;

namespace KanchokuWS.Handler
{
    // cf. [C#でIMEの変換モードを監視・変更する](https://qiita.com/kob58im/items/a1644b36366f4d094a2c)
    class IMEHandler
    {
        private static Logger logger = Logger.GetLogger(true);

        public enum IMEChangeState
        {
            IME_ON = 1,
            IME_OFF = -1,
            NOT_CHANGED = 0
        }

        [DllImport("User32.dll")]
        static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        const int WM_IME_CONTROL = 0x283;
        const int IMC_GETCONVERSIONMODE = 1;
        const int IMC_SETCONVERSIONMODE = 2;
        const int IMC_GETOPENSTATUS = 5;
        const int IMC_SETOPENSTATUS = 6;

        const int IME_CMODE_NATIVE = 1;
        const int IME_CMODE_KATAKANA = 2;
        const int IME_CMODE_FULLSHAPE = 8;
        const int IME_CMODE_ROMAN = 16;

        const int CMode_HankakuKana = IME_CMODE_ROMAN | IME_CMODE_KATAKANA | IME_CMODE_NATIVE;
        const int CMode_ZenkakuEisu = IME_CMODE_ROMAN | IME_CMODE_FULLSHAPE;
        const int CMode_Hiragana = IME_CMODE_ROMAN | IME_CMODE_FULLSHAPE | IME_CMODE_NATIVE;
        const int CMode_ZenkakuKana = IME_CMODE_ROMAN | IME_CMODE_FULLSHAPE | IME_CMODE_KATAKANA | IME_CMODE_NATIVE;
        // 実験してみた結果
        // 19 :カ 半角カナ                     0001 0011
        // 24 :Ａ 全角英数                     0001 1000
        // 25 :あ ひらがな（漢字変換モード）   0001 1001
        // 27 :   全角カナ                     0001 1011

        // 以前の状態
        public static bool ImeEnabled { get; private set; } = false;

        public static IntPtr ImeWnd { get; private set; } = IntPtr.Zero;

        /// <summary>
        /// IMEの状態が変化したかどうかを取得する<br/>
        /// </summary>
        /// <returns></returns>
        public static bool GetImeStateChanged()
        {
            //IME状態の取得
            ImeWnd = new Handler.GUIThreadInfo().GetDefaultIMEWnd();

            if (ImeWnd != IntPtr.Zero) {
                int imeConvMode = SendMessage(ImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETCONVERSIONMODE, IntPtr.Zero);
                bool imeEnabled = SendMessage(ImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETOPENSTATUS, IntPtr.Zero) != 0;

                if (ImeEnabled != imeEnabled) {
                    // 状態が変化した
                    logger.DebugH(() => $"imeEnabled={imeEnabled}, status={imeConvMode}");
                    ImeEnabled = imeEnabled;
                    return true;
                }
            }

            return false;
        }
    }
}

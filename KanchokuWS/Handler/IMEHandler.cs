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

        const int IME_CMODE_KANA = 0;
        const int IME_CMODE_NATIVE = 1;
        const int IME_CMODE_KATAKANA = 2;
        const int IME_CMODE_FULLSHAPE = 8;
        const int IME_CMODE_ROMAN = 16;

        // cf. dj_src_2014-06-07/src/IME/IME.ahk
        // IME 入力モード(どの IMEでも共通っぽい)
        // DEC HEX    BIN
        //     0 (0x00  0000 0000) かな 半英数
        //     3 (0x03  0000 0011)         半ｶﾅ
        //     8 (0x08  0000 1000)         全英数
        //     9 (0x09  0000 1001)         ひらがな
        //    11 (0x0B  0000 1011)         全カタカナ
        //    16 (0x10  0001 0000) ローマ字半英数
        //    19 (0x13  0001 0011)         半ｶﾅ
        //    24 (0x18  0001 1000)         全英数
        //    25 (0x19  0001 1001)         ひらがな
        //    27 (0x1B  0001 1011)         全カタカナ

        // 入力モードの変更中
        public static bool ImeInputModeChanged { get; set; } = false;

        // IMEの状態
        public static bool ImeEnabled { get; private set; } = false;

        // 入力モード
        public static int ImeConversionMode { get; private set; }

        // IMEのWndHandle
        public static IntPtr ImeWnd { get; private set; } = IntPtr.Zero;

        /// <summary>
        /// IMEの状態が変化したかどうかを取得する<br/>
        /// </summary>
        /// <returns></returns>
        public static bool GetImeStateChanged()
        {
            logger.DebugH(() => $"CALLED: ImeInputModeChanged={ImeInputModeChanged}");
            if (!ImeInputModeChanged) {
                //IME状態の取得
                ImeWnd = new Handler.GUIThreadInfo().GetDefaultIMEWnd();

                if (ImeWnd != IntPtr.Zero) {
                    int imeConvMode = SendMessage(ImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETCONVERSIONMODE, IntPtr.Zero);
                    bool imeEnabled = SendMessage(ImeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETOPENSTATUS, IntPtr.Zero) != 0;

                    if (ImeEnabled != imeEnabled) {
                        // 状態が変化した
                        logger.DebugH(() => $"IME State Changed: imeEnabled={imeEnabled}, convMode={imeConvMode}");
                        ImeEnabled = imeEnabled;
                        ImeConversionMode = imeConvMode;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 元の入力モードに戻る
        /// </summary>
        public static void RevertInputMode()
        {
            logger.DebugH(() => $"CALLED: ImeEnabled={ImeEnabled}");
            if (ImeEnabled) {
                SendMessage(ImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETCONVERSIONMODE, (IntPtr)ImeConversionMode); // 元の入力モードに設定
            }
            ImeInputModeChanged = false;
        }

        /// <summary>
        /// かな入力モードに移行
        /// </summary>
        public static void SetKanaInputMode()
        {
            logger.DebugH(() => $"CALLED: ImeEnabled={ImeEnabled}");
            if (ImeEnabled) {
                ImeInputModeChanged = true;
                SendMessage(ImeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETCONVERSIONMODE, (IntPtr)(IME_CMODE_KANA | IME_CMODE_FULLSHAPE | IME_CMODE_NATIVE)); // ひらがなモードに設定
            }
        }
    }

    class IMEInputModeChanger : IDisposable
    {
        private bool IsKanaMode = false;

        public IMEInputModeChanger(bool bKana)
        {
            IsKanaMode = bKana;
            if (IsKanaMode) {
                IMEHandler.SetKanaInputMode();
            }
        }

        public void Dispose()
        {
            if (IsKanaMode) {
                IMEHandler.RevertInputMode();
            }
        }
    }
}

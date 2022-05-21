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

        [DllImport("Imm32.dll", SetLastError = true)]
        public static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("imm32.dll", CharSet = CharSet.Unicode)]

        static extern int ImmGetCompositionString(IntPtr hIMC, uint dwIndex, char[] lpBuf, uint dwBufLen);

        [DllImport("imm32.dll")]
        public static extern int ImmNotifyIME(IntPtr hIMC, int dwAction, int dwIndex, int dwValue);

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

        const int WM_IME_NOTIFY = 0x0282;
        const int IMN_CLOSECANDIDATE = 4;

        const int NI_COMPOSITIONSTR = 0x0015;
        const int CPS_COMPLETE = 1;

        const int GCS_COMPATTR = 0x0010;

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

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        // IMEのWndHandle
        //public static IntPtr ImeWnd { get; private set; } = IntPtr.Zero;

        public static IntPtr MainWnd { get; set; }

        /// <summary>
        /// IMEの状態が変化したかどうかを取得する<br/>
        /// </summary>
        /// <returns></returns>
        public static bool GetImeStateChanged(FrmVirtualKeyboard frmVkb)
        {
            logger.Debug(() => $"CALLED: ImeInputModeChanged={ImeInputModeChanged}");
            if (!ImeInputModeChanged) {
                //IME状態の取得
                IntPtr imeWnd = new Handler.GUIThreadInfo().GetDefaultIMEWnd();

                if (imeWnd != IntPtr.Zero) {
                    int imeConvMode = SendMessage(imeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETCONVERSIONMODE, IntPtr.Zero);
                    bool imeEnabled = SendMessage(imeWnd, WM_IME_CONTROL, (IntPtr)IMC_GETOPENSTATUS, IntPtr.Zero) != 0;

                    if (ImeEnabled != imeEnabled) {
                        // 状態が変化した
                        ActiveWindowHandler.Singleton.GetActiveWindowInfo(null, frmVkb);
                        if (frmVkb == null || !frmVkb.IsMyWinClassName()) {
                            logger.DebugH(() => $"IME State Changed: imeEnabled={imeEnabled}, convMode={imeConvMode}");
                            ImeEnabled = imeEnabled;
                            ImeConversionMode = imeConvMode;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static void NotifyComplete()
        {
            logger.DebugH(() => $"CALLED: ImeEnabled={ImeEnabled}");
            if (ImeEnabled) {
                // 他のプロセスに対してImmGetContext()を呼ぶと Zeroが返るようだ。なので下記処理は無効
                var hImc = ImmGetContext(ActiveWindowHandler.Singleton.ActiveWinHandle);
                logger.DebugH(() => $"hImc={hImc:x}");
                if (hImc != IntPtr.Zero) {
                    int result = ImmNotifyIME(hImc, NI_COMPOSITIONSTR, CPS_COMPLETE, 0);
                    logger.DebugH(() => $"result={result:x}");
                }
            }
        }

        public static bool HasUnconfirmed()
        {
            logger.DebugH(() => $"CALLED: ImeEnabled={ImeEnabled}");
            if (ImeEnabled) {
                var hImc = ImmGetContext(ActiveWindowHandler.Singleton.ActiveWinHandle);
                if (hImc != IntPtr.Zero) {
                    int len = ImmGetCompositionString(hImc, GCS_COMPATTR, null, 0);
                    logger.DebugH(() => $"len={len}");
                    return len > 0;
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
                IntPtr imeWnd = new Handler.GUIThreadInfo().GetDefaultIMEWnd();
                SendMessage(imeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETCONVERSIONMODE, (IntPtr)ImeConversionMode); // 元の入力モードに設定
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
                IntPtr imeWnd = new Handler.GUIThreadInfo().GetDefaultIMEWnd();
                SendMessage(imeWnd, WM_IME_CONTROL, (IntPtr)IMC_SETCONVERSIONMODE, (IntPtr)(IME_CMODE_KANA | IME_CMODE_FULLSHAPE | IME_CMODE_NATIVE)); // ひらがなモードに設定
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

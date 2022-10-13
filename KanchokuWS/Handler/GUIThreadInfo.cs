using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;
using Accessibility;

using Utils;

namespace KanchokuWS.Handler
{
    /// <summary>
    /// Win32 の GetGUIThreadInfo を使うラッパークラス<br/>
    /// cf. https://stackoverflow.com/questions/3072974/how-to-call-getguithreadinfo-in-c-sharp <br/
    /// 「この関数は、アクティブなウィンドウを呼び出し側プロセスが所有していなくても成功する」とのこと。<br/>
    /// cf. https://www.tokovalue.jp/function/GetGUIThreadInfo.htm
    /// </summary>
    public class GUIThreadInfo
    {
        private static Logger logger = Logger.GetLogger();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetGUIThreadInfo(uint hTreadID, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("imm32.dll")]
        static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int iLeft;
            public int iTop;
            public int iRight;
            public int iBottom;

            public bool EqualsToRectangle(Rectangle rect)
            {
                // 微妙な位置の違いを許容する
                return Math.Abs(iLeft - rect.Left) < 10 && Math.Abs(iTop - rect.Top) < 10 && Math.Abs(iRight - rect.Right) < 10 && Math.Abs(iBottom - rect.Bottom) < 10;
            }

            public void CopyFromRectangle(Rectangle rect)
            {
                iLeft = rect.Left;
                iTop = rect.Top;
                iRight = rect.Right;
                iBottom = rect.Bottom;
            }

            public void CopyToRectangle(ref Rectangle rect)
            {
                rect.X = iLeft;
                rect.Y = iTop;
                rect.Width = iRight - iLeft;
                rect.Height = iBottom - iTop;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rectCaret;
        }

        private static bool bLog = false;

        private GUITHREADINFO guiThreadInfo;

        public string className { get; private set; }
        public IntPtr hwndForeground { get; private set; } = IntPtr.Zero;
        public IntPtr hwndActive => guiThreadInfo.hwndFocus;
        public IntPtr hwndFocus => guiThreadInfo.hwndFocus;

        public GUIThreadInfo()
        {
            guiThreadInfo = new GUITHREADINFO();
            guiThreadInfo.cbSize = Marshal.SizeOf(guiThreadInfo);
        }

        public static void SetLogFlag(bool bLog)
        {
            GUIThreadInfo.bLog = bLog;
        }

        public bool GetForegroundWinInfo()
        {
            className = null;
            guiThreadInfo.hwndFocus = IntPtr.Zero;

            // 最前面ウィンドウのハンドルを取得
            hwndForeground = GetForegroundWindow();

            uint threadId = GetWindowThreadProcessId(hwndForeground, 0);

            bool result = GetGUIThreadInfo(threadId, ref guiThreadInfo);
            className = getClassName(result && hwndFocus != IntPtr.Zero ? hwndFocus : hwndForeground);

            if (bLog) logger.Debug(() => $"RESULT: {result}: foreground hWnd={(int)hwndForeground:x}, WndClassName={className}");
            return result;
        }

        public IntPtr GetDefaultIMEWnd()
        {
            bool result = GetGUIThreadInfo(0, ref guiThreadInfo);
            //if (bLog) logger.Debug(() => $"RESULT: {result}: foreground hWnd={(int)hwndForeground:x}, WndClassName={className}");
            return result ? ImmGetDefaultIMEWnd(guiThreadInfo.hwndFocus) : IntPtr.Zero;
        }

        public void GetCaretPos(ref Rectangle rect)
        {
            // やっぱりここで取得できるカレットは、普通のカレットのみ。CMD Prompt や Visual Studio のカレット位置は取れない
            rect.X = guiThreadInfo.rectCaret.iLeft;
            rect.Y = guiThreadInfo.rectCaret.iTop;
            rect.Width = guiThreadInfo.rectCaret.iRight - guiThreadInfo.rectCaret.iLeft;
            rect.Height = guiThreadInfo.rectCaret.iBottom - guiThreadInfo.rectCaret.iTop;
        }

        //RECT prevCaretRect;
        //uint sameCount = 0;
        //uint sameCountMax = 0;
        //uint invalidCaretPosCount = 0;

        /// <summary> カレットのスクリーン座標を取得 </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public bool GetScreenCaretPos(ref Rectangle rect)
        {
            RECT rt;
            if (GetForegroundWindowRect(out rt)) {
                if (hwndFocus != IntPtr.Zero) {
                    // IAccessible を使ってカレット位置を取得する
                    if (getCaretPos(hwndFocus, ref rect, rt)) {
                        if (Logger.IsDebugEnabled) {
                            //if (bLog) logger.Debug($"prev: left={prevCaretRect.iLeft}, top={prevCaretRect.iTop}, right={prevCaretRect.iRight}, bottom={prevCaretRect.iBottom}");
                            if (bLog) logger.Debug($"curr: left={rect.Left}, top={rect.Top}, right={rect.Right}, bottom={rect.Bottom}");
                        }
                        return true;
                    }
                }
                if (className._equalsTo("ConsoleWindowClass")) {
                    // CMD.EXEの場合は右下に移動
                    rect.X = rt.iRight - 250;
                    rect.Y = rt.iBottom - 40;
                    rect.Width = 2;
                    rect.Height = 10;
                    if (bLog) logger.Info($"Set caret pos for CMD.EXE: X={rect.X}, Y={rect.Y}, Width={rect.Width}, Height={rect.Height}");
                }
            }
            return false;

            // 取得に失敗したら、ウィンドウを移動しないようにした(以下、不要。いずれ削除)
            //if (hwndFocus != IntPtr.Zero) {
            //    // IAccessible を使ってカレット位置を取得する
            //    getCaretPos(hwndFocus, ref rect, rt);
            //    if (Logger.IsDebugEnabled) {
            //        if (bLog) logger.Debug($"prev: left={prevCaretRect.iLeft}, top={prevCaretRect.iTop}, right={prevCaretRect.iRight}, bottom={prevCaretRect.iBottom}");
            //        if (bLog) logger.Debug($"curr: left={rect.Left}, top={rect.Top}, right={rect.Right}, bottom={rect.Bottom}");
            //    }
            //    if (rect.Width == 0 && rect.Height == 0) {
            //        ++invalidCaretPosCount;
            //        if (invalidCaretPosCount < 3) {
            //            prevCaretRect.CopyToRectangle(ref rect);
            //        } else {
            //            prevCaretRect.CopyFromRectangle(rect);
            //        }
            //    } else {
            //        invalidCaretPosCount = 0;
            //    }
            //    if (rect.Width != 0 || rect.Height != 0) {
            //        if (!prevCaretRect.EqualsToRectangle(rect)) {
            //            prevCaretRect.CopyFromRectangle(rect);
            //            sameCount = 0;
            //            return;
            //        } else {
            //            // 何回か同じ位置にカレットがある場合は仮想鍵盤をウィンドウ右下に位置させる 
            //            ++sameCount;
            //            if (bLog) logger.Debug(() => $"sameCount={sameCount}");
            //            if (sameCountMax == 0 || sameCount < sameCountMax) return;
            //        }
            //    }
            //}

            //// カレット位置が取得できなかったか、何回か同じ位置にあったら、ウィンドウ右下に位置させる
            //rect.X = rt.iRight - 250;
            //rect.Y = rt.iBottom - 40;
            //rect.Width = 2;
            //rect.Height = 10;
        }

        // アクティブウィンドウの ClassName
        private string getClassName(IntPtr hwnd)
        {
            const int nChars = 1024;
            StringBuilder sb = new StringBuilder(nChars);
            GetClassName(hwnd, sb, nChars);
            return sb.ToString();

        }

        // 前面ウィンドウの位置とサイズ(RECT)を取得
        public bool GetForegroundWindowRect(out RECT rect)
        {
            bool result = GetWindowRect(hwndForeground, out rect);
            if (bLog && !result) logger.Warn("GetWindowRect FAILED");
            return result;
        }

        [DllImport("oleacc.dll")]
        internal static extern int AccessibleObjectFromWindow(
               IntPtr hwnd,
               uint id,
               ref Guid iid,
               [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object ppvObject);

        internal enum OBJID : uint
        {
            WINDOW = 0x00000000,
            SYSMENU = 0xFFFFFFFF,
            TITLEBAR = 0xFFFFFFFE,
            MENU = 0xFFFFFFFD,
            CLIENT = 0xFFFFFFFC,
            VSCROLL = 0xFFFFFFFB,
            HSCROLL = 0xFFFFFFFA,
            SIZEGRIP = 0xFFFFFFF9,
            CARET = 0xFFFFFFF8,
            CURSOR = 0xFFFFFFF7,
            ALERT = 0xFFFFFFF6,
            SOUND = 0xFFFFFFF5,
        }

        /// <summary>
        /// IAccessible を使ってカレット位置を取得する<br/>
        /// cf. https://stackoverflow.com/questions/52592652/get-text-and-caret-from-a-textbox-in-another-application
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        private static bool getCaretPos(IntPtr handle, ref Rectangle rect, RECT winRect)
        {
            if (bLog) logger.Info(() => $"Check Caret for {(int)handle:x}");
            Guid guidIAccessible = new Guid("{618736E0-3C3D-11CF-810C-00AA00389B71}");
            object obj = null;
            int retVal = AccessibleObjectFromWindow(handle, (uint)OBJID.CARET, ref guidIAccessible, ref obj);
            var iAccessible = (IAccessible)obj;
            if (iAccessible == null) {
                logger.Warn("iAccessible is null");
                //if (bLog) logger.Info("iAccessible is null");
                //rect.X = rect.Y = rect.Width = rect.Height = 0;
                return false;
            }

            int left, top, width, height;
            dynamic varCaret = new Int32(); // 上記元ネタでは、 VARIANT を使っている (C++なので)。とりあえず dynamic にしてみたが・・・

            try {
                iAccessible.accLocation(out left, out top, out width, out height, varCaret);
            } catch (Exception ex) {
                logger.Warn(ex.Message);
                if (bLog) logger.Info(ex.StackTrace);
                //rect.X = rect.Y = rect.Width = rect.Height = 0;
                iAccessible = null;
                return false;
            }

            iAccessible = null;

            // Effective Dpi を使ってみたが、Per-Momitor HiRes が true の場合は、dpiRate に応じた位置が返ってくるようなので、調整は不要のようだ
            //double dpiRate = ScreenInfo.GetScreenDpiRate(left, top);
            double dpiRate = 1.0;
            if (bLog) logger.Info(() => $"left={left}, top={top}, width={width}, height={height}, dpiRate={dpiRate:f3}");
            //if (left == 0 && top == 0 && width == 0 && height == 0) {
            //    return false;
            //}

            rect.X = winRect.iLeft + (int)((left - winRect.iLeft) / dpiRate);
            rect.Y = winRect.iTop + (int)((top - winRect.iTop) / dpiRate);
            rect.Width = width;
            rect.Height = height;

            return true;
        }


    }
}

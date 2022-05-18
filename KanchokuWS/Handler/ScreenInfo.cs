using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using Utils;

namespace KanchokuWS.Gui
{
    public static class ScreenInfo
    {
        private static Logger logger = Logger.GetLogger();

        public static List<Rectangle> ScreenRects { get; private set; } = new List<Rectangle>();

        public static List<int> ScreenDpi { get; private set; } = new List<int>();

        public static double PrimaryScreenDpiRate => ScreenDpi._getFirst() / 96.0;

        public static int PrimaryScreenDpi => ScreenDpi._getFirst();

        public static void GetScreenInfo()
        {
            ScreenRects = Screen.AllScreens.Select(s => new Rectangle(s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height)).ToList();
            ScreenDpi = Screen.AllScreens.Select(s => {
                uint x, y;
                s.GetDpi(DpiType.Effective, out x, out y);
                return (int)x;
            }).ToList();
            if (Logger.IsInfoHEnabled) {
                int i = 0;
                foreach (var r in ScreenRects) {
                    //logger.InfoH($"Screen {i}: X={r.X}, Y={r.Y}, W={r.Width}, H={r.Height}");
                    logger.InfoH($"Screen {i}: X={r.X}, Y={r.Y}, W={r.Width}, H={r.Height}, dpi={ScreenDpi[i]}");
                    ++i;
                }
            }
        }

        /// <summary>
        /// (x, y) を含むスクリーンの位置・サイズを返す。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static Rectangle GetScreenContaining(int x, int y)
        {
            return ScreenRects[findContaingScreenIdx(x, y)];
        }

        public static double GetScreenDpiRate(int x, int y)
        {
            return ScreenDpi[findContaingScreenIdx(x, y)] / 96.0;
        }

        private static int findContaingScreenIdx(int x, int y)
        {
            for (int idx = 0; idx < ScreenRects.Count; ++idx) {
                var r = ScreenRects[idx];
                if (x >= r.X && x < r.X + r.Width && y >= r.Y && y < r.Y + r.Height) {
                    // このスクリーンに納まっていた
                    return idx;
                }
            }
            return 0;
        }

        public static int GetScreenIndexByDpi(int dpi)
        {
            return ScreenDpi.IndexOf(dpi)._lowLimit(0);
        }
    }

    // https://stackoverflow.com/questions/29438430/how-to-get-dpi-scale-for-all-screens
    public static class ScreenExtensions
    {
        public static void GetDpi(this System.Windows.Forms.Screen screen, DpiType dpiType, out uint dpiX, out uint dpiY)
        {
            var pnt = new System.Drawing.Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
            var mon = MonitorFromPoint(pnt, 2/*MONITOR_DEFAULTTONEAREST*/);
            GetDpiForMonitor(mon, dpiType, out dpiX, out dpiY);
        }

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062(v=vs.85).aspx
        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint([In] System.Drawing.Point pt, [In] uint dwFlags);

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
        [DllImport("Shcore.dll")]
        private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);
    }

    //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511(v=vs.85).aspx
    public enum DpiType
    {
        Effective = 0,
        Angular = 1,
        Raw = 2,
    }
}

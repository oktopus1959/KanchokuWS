using System;
using System.Runtime.InteropServices;

namespace Utils
{
    /// <summary>
    /// High Resolution DateTime
    /// </summary>
    public static class HRDateTime
    {
        // cf. https://stackoverflow.com/questions/54889988/what-is-the-time-reference-for-getsystemtimepreciseasfiletime
        // cf. https://qiita.com/5view5q/items/6f08aaea7d961fd4cad7

        [DllImport("kernel32.dll")]
        static extern void GetSystemTimePreciseAsFileTime(out long filetime);

        static TimeSpan adjustTs;

        static DateTime systemHiResNow()
        {
            long filetime;
            GetSystemTimePreciseAsFileTime(out filetime);
            return DateTime.FromFileTimeUtc(filetime).ToLocalTime();
        }

        /// <summary>
        /// システムクロックと現在時刻(DateTime.Now)にはズレがあるので、それを補正する。<br/>
        /// プログラム開始時を含め、適当なタイミングで呼び出すこと。
        /// </summary>
        public static void AdjustHiResNow()
        {
            adjustTs = DateTime.Now - systemHiResNow();
        }

        /// <summary>
        /// High Resolution な DateTime.Now
        /// </summary>
        public static DateTime Now => systemHiResNow() + adjustTs;
    }
}

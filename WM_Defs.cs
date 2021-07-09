using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanchokuWS
{
    public static class WM_Defs
    {
        public const int WM_CLOSE = 0x0010;
        public const int WM_COPYDATA = 0x4A;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int WM_UNICHAR = 0x0109;
        public const int WM_IME_CHAR = 0x0286;
        public const int WM_HOTKEY = 0x0312;
    }
}

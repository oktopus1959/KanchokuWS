using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanchokuWS.Domain
{
    static class KeyModifiers
    {
        // VKEY に対する modifier ALT
        public const uint MOD_ALT = 0x0001;

        // VKEY に対する modifier CTRL
        public const uint MOD_CONTROL = 0x0002;

        // VKEY に対する modifier SHIFT
        public const uint MOD_SHIFT = 0x0004;

        // VKEY に対する modifier WIN
        public const uint MOD_WIN = 0x0008;

        // VKEY に対する modifier Space
        public const uint MOD_SPACE = 0x0100;

        // VKEY に対する modifier CapsLock
        public const uint MOD_CAPS = 0x0200;

        // VKEY に対する modifier 英数
        public const uint MOD_ALNUM = 0x0400;

        // VKEY に対する modifier NFER
        public const uint MOD_NFER = 0x0800;

        // VKEY に対する modifier XFER
        public const uint MOD_XFER = 0x1000;

        // VKEY に対する modifier RSHIFT
        public const uint MOD_RSHIFT = 0x2000;

        // VKEY に対する modifier LCTRL
        public const uint MOD_LCTRL = 0x4000;

        // VKEY に対する modifier RCTRL
        public const uint MOD_RCTRL = 0x8000;

        // 単打用キー
        public const uint MOD_SINGLE = 0x10000;

        // VKEY に対する modifier LSHIFT
        public const uint MOD_LSHIFT = 0x20000;

        public static uint MakeModifier(bool ctrl, bool shift)
        {
            return (ctrl ? MOD_CONTROL : 0) + (shift ? MOD_SHIFT : 0);
        }

    }

}

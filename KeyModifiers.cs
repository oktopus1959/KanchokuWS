﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanchokuWS
{
    public static class KeyModifiers
    {
        // HOTKEY に対する modifier ALT
        public const uint MOD_ALT = 0x0001;

        // HOTKEY に対する modifier CTRL
        public const uint MOD_CONTROL = 0x0002;

        // HOTKEY に対する modifier SHIFT
        public const uint MOD_SHIFT = 0x0004;

        // HOTKEY に対する modifier WIN
        public const uint MOD_WIN = 0x0008;
    }
}
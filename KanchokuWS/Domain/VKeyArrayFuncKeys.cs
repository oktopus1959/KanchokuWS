using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Utils;

namespace KanchokuWS.Domain
{
    class VKeyArrayFuncKeys
    {
        public class FuncVKeys
        {
            //public const uint BACK = 0x08;
            public const uint CONTROL = 0x11;
            public const uint ALT = 0x12;
            public const uint LSHIFT = 0xa0;
            public const uint RSHIFT = 0xa1;
            public const uint LCONTROL = 0xa2;
            public const uint RCONTROL = 0xa3;
            public const uint SPACE = (uint)Keys.Space;

            // 以下は JP/US によってキーコードが変わるor無効になる可能性あり
            public static uint CAPSLOCK => vkeyArrayFuncKeys[3];
            public static uint EISU => vkeyArrayFuncKeys[4];
            public static uint MUHENKAN => vkeyArrayFuncKeys[5];
            public static uint HENKAN => vkeyArrayFuncKeys[6];
            public static uint KANA => vkeyArrayFuncKeys[7];
        }

        /// <summary> 機能キー (Esc, 半/全, Tab, Caps, 英数, 無変換, 変換, かな, BS, Enter, Ins, Del, Home, End, PgUp, PgDn, ↑, ↓, ←, →)</summary>
        private static uint[] vkeyArrayFuncKeys = {
            // 0 - 4
            /*Esc*/ 0x1b, /*半/全*/ 0xf3, /*Tab*/ 0x09, /*Caps*/ 0x14, /*英数*/ 0xf0,
            // 5 - 9
            /*無変換*/ 0x1d, /*変換*/ 0x1c, /*かな*/ 0xf2, /*BS*/ 0x08, /*Enter*/ 0x0d,
            // 10 - 14
            /*Ins*/ 0x2d, /*Del*/ 0x2e, /*Home*/ 0x24, /*End*/ 0x23, /*PgUp*/ 0x21,
            // 15 - 19
            /*PgDn*/ 0x22, /*↑*/ 0x26, /*↓*/ 0x28, /*←*/ 0x25, /*→*/ 0x27,
            // 20 - 24
            /*Lctrl*/ FuncVKeys.LCONTROL, /*Rctrl*/ FuncVKeys.RCONTROL, /*Lshift*/ FuncVKeys.LSHIFT, /*Rshift*/ FuncVKeys.RSHIFT, /*ScrLock*/ 0x91,
            // 25 - 27
            /*Pause*/ 0x13, /*IME ON*/ 0x16, /*IME OFF*/ 0x1a,
            // 28 - 37
            /*F1*/ 0x70, /*F2*/ 0x71, /*F3*/ 0x72, /*F4*/ 0x73, /*F5*/ 0x74, /*F6*/ 0x75, /*F7*/ 0x76, /*F8*/ 0x77, /*F9*/ 0x78, /*F10*/ 0x79,
            // 38 - 47
            /*F11*/ 0x7a, /*F12*/ 0x7b, /*F13*/ 0x7c, /*F14*/ 0x7d, /*F15*/ 0x7e, /*F16*/ 0x7f, /*F17*/ 0x80, /*F18*/ 0x81, /*F19*/ 0x82, /*F20*/ 0x83,
            // /*F21*/ 0x84, /*F22*/ 0x85, /*F23*/ 0x86, /*F24*/ 0x87,
        };

        /// <summary>機能キーのインデックスを得る (-1なら該当せず)</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int GetFuncKeyIndexByName(string name)
        {
            int n = -1;
            switch (name._toLower()) {
                case "esc": case "escape": n = 0; break;
                case "zenkaku": n = 1; break;
                case "tab": n = 2; break;
                case "caps": case "capslock": n = 3; break;
                case "alnum": case "alphanum": case "eisu": n = 4; break;
                case "nfer": case "muhenkan": n = 5; break;
                case "xfer": case "henkan": n = 6; break;
                case "kana": case "hiragana": n = 7; break;
                case "bs": case "back": case "backspace": n = 8; break;
                case "enter": n = 9; break;
                case "ins": case "insert": n = 10; break;
                case "del": case "delete": n = 11; break;
                case "home": n = 12; break;
                case "end": n = 13; break;
                case "pgup": case "pageup": n = 14; break;
                case "pgdn": case "pagedown": n = 15; break;
                case "up": case "uparrow": n = 16; break;
                case "down": case "downarrow": n = 17; break;
                case "left": case "leftarrow": n = 18; break;
                case "right": case "rightarrow": n = 19; break;
                case "lctrl": n = 20; break;
                case "rctrl": n = 21; break;
                case "lshift": n = 22; break;
                case "rshift": n = 23; break;
                case "scrlock": n = 24; break;
                case "pause": n = 25; break;
                case "imeon": n = 26; break;
                case "imeoff": n = 27; break;
                case "f1": case "f01": n = 28; break;
                case "f2": case "f02": n = 29; break;
                case "f3": case "f03": n = 30; break;
                case "f4": case "f04": n = 31; break;
                case "f5": case "f05": n = 32; break;
                case "f6": case "f06": n = 33; break;
                case "f7": case "f07": n = 34; break;
                case "f8": case "f08": n = 35; break;
                case "f9": case "f09": n = 36; break;
                case "f10": n = 37; break;
                case "f11": n = 38; break;
                case "f12": n = 39; break;
                case "f13": n = 40; break;
                case "f14": n = 41; break;
                case "f15": n = 42; break;
                case "f16": n = 43; break;
                case "f17": n = 44; break;
                case "f18": n = 45; break;
                case "f19": n = 46; break;
                case "f20": n = 47; break;
                //case "f21": n = 46; break;
                //case "f22": n = 47; break;
                //case "f23": n = 48; break;
                //case "f24": n = 49; break;
                default: n = -1; break;
            }
            return n;
        }

        public static uint getVKey(int fkeyIdx)
        {
            return fkeyIdx >= 0 && fkeyIdx < vkeyArrayFuncKeys.Length ? vkeyArrayFuncKeys[fkeyIdx] : 0;
        }

        public static bool setVKey(int fkeyIdx, uint vkey)
        {
            if (fkeyIdx >= 0 && fkeyIdx < vkeyArrayFuncKeys.Length) {
                vkeyArrayFuncKeys[fkeyIdx] = vkey;
                return true;
            }
            return false;
        }

    }
}

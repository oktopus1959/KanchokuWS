using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utils;

namespace KanchokuWS.Domain
{
    /// <summary>
    /// シフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) に割り当てるシフト面
    /// </summary>
    class ShiftPlaneMapper
    {
        Dictionary<uint, int> shiftPlaneMap = new Dictionary<uint, int>();

        public void Clear()
        {
            shiftPlaneMap.Clear();
        }

        public void Add(uint key, int plane)
        {
            shiftPlaneMap[key] = plane;
        }

        public bool ContainsKey(uint key)
        {
            return shiftPlaneMap.ContainsKey(key);
        }

        public int GetPlane(uint key)
        {
            return shiftPlaneMap._safeGet(key, 0);
        }

        public bool FindPlane(int plane)
        {
            return shiftPlaneMap.Values.Any(x => x == plane);
        }

        public List<KeyValuePair<uint, int>> GetPairs()
        {
            return shiftPlaneMap.ToList();
        }
    }

    static class ShiftPlane
    {
        private static Logger logger = Logger.GetLogger();

        public const int ShiftPlane_NONE = 0;
        public const int ShiftPlane_SHIFT = 1;
        public const int ShiftPlane_A = 2;
        public const int ShiftPlane_B = 3;
        public const int ShiftPlane_C = 4;
        public const int ShiftPlane_D = 5;
        public const int ShiftPlane_E = 6;
        public const int ShiftPlane_F = 7;
        public const int ShiftPlane_NUM = 8;

        public static string GetShiftPlaneName(int plane)
        {
            switch (plane) {
                case 1: return "Shift";
                case 2: return "ShiftA";
                case 3: return "ShiftB";
                case 4: return "ShiftC";
                case 5: return "ShiftD";
                case 6: return "ShiftE";
                case 7: return "ShiftF";
                default: return "none";
            }
        }

        public static string GetShiftPlanePrefix(int plane)
        {
            switch (plane) {
                case 1: return "S";
                case 2: return "A";
                case 3: return "B";
                case 4: return "C";
                case 5: return "D";
                case 6: return "E";
                case 7: return "F";
                default: return "";
            }
        }

        /// <summary> シフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) に割り当てるシフト面</summary>
        public static ShiftPlaneMapper ShiftPlaneForShiftModKey { get; private set; } = new ShiftPlaneMapper();

        /// <summary> DecoderがOffの時のシフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) に割り当てるシフト面</summary>
        public static ShiftPlaneMapper ShiftPlaneForShiftModKeyWhenDecoderOff { get; private set; } = new ShiftPlaneMapper();

        /// <summary> シフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) に割り当てられたシフト面を得る</summary>
        public static int GetShiftPlaneFromShiftModFlag(uint modFlag, bool bDecoderOn)
        {
            return bDecoderOn ? ShiftPlaneForShiftModKey.GetPlane(modFlag) : ShiftPlaneForShiftModKeyWhenDecoderOff.GetPlane(modFlag);
        }

        /// <summary> シフト用の機能キー(space, Caps, alnum, nfer, xfer, Rshift) にシフト面が割り当てられているか</summary>
        public static bool IsShiftPlaneAssignedForShiftModFlag(uint modFlag, bool bDecoderOn)
        {
            return GetShiftPlaneFromShiftModFlag(modFlag, bDecoderOn) != ShiftPlane_NONE;
        }

        public static void InitializeShiftPlaneForShiftModKey()
        {
            ShiftPlaneForShiftModKey.Clear();
            ShiftPlaneForShiftModKeyWhenDecoderOff.Clear();

            // SHIFTなら標準シフト面をデフォルトとしておく
            ShiftPlaneForShiftModKey.Add(KeyModifiers.MOD_SHIFT, ShiftPlane_SHIFT);
            ShiftPlaneForShiftModKeyWhenDecoderOff.Add(KeyModifiers.MOD_SHIFT, ShiftPlane_SHIFT);
        }

        /// <summary>
        /// 拡張シフトキーに対するシフト面の割り当て<br/>
        ///   EXT_MOD_NAME=plane[|plane]
        /// </summary>
        /// <param name="line"></param>
        /// <param name="rawLine"></param>
        /// <returns></returns>
        public static bool AssignShiftPlane(string line, string rawLine = null)
        {
            // NAME=plane[|plane]...
            var items = line._toLower()._split('=');
            if (items._length() != 2) return false;

            if (ExtraModifiers.IsDisabledExtKey(items[0])) {
                ExtraModifiers.AddDisabledExtKeyLine(rawLine._orElse(line));
                return true;
            }

            uint modKey = ExtraModifiers.GetModifierKeyByName(items[0]);
            var planes = items[1]._split('|');
            int shiftPlane = ShiftPlane_NONE;
            switch (planes._getNth(0)) {
                case "shift": shiftPlane = ShiftPlane_SHIFT; break;
                case "shifta": shiftPlane = ShiftPlane_A; break;
                case "shiftb": shiftPlane = ShiftPlane_B; break;
                case "shiftc": shiftPlane = ShiftPlane_C; break;
                case "shiftd": shiftPlane = ShiftPlane_D; break;
                case "shifte": shiftPlane = ShiftPlane_E; break;
                case "shiftf": shiftPlane = ShiftPlane_F; break;
                case "none": shiftPlane = ShiftPlane_NONE; break;
                default: return false;
            }

            var shiftPlaneWhenOff = shiftPlane;
            if (planes.Length > 1) {
                switch (planes._getNth(1)) {
                    case "shift": shiftPlaneWhenOff = ShiftPlane_SHIFT; break;
                    case "shifta": shiftPlaneWhenOff = ShiftPlane_A; break;
                    case "shiftb": shiftPlaneWhenOff = ShiftPlane_B; break;
                    case "shiftc": shiftPlaneWhenOff = ShiftPlane_C; break;
                    case "shiftd": shiftPlaneWhenOff = ShiftPlane_D; break;
                    case "shifte": shiftPlaneWhenOff = ShiftPlane_E; break;
                    case "shiftf": shiftPlaneWhenOff = ShiftPlane_F; break;
                    case "none": shiftPlaneWhenOff = ShiftPlane_NONE; break;
                    default: return false;
                }
            }

            logger.Info(() => $"mod={modKey:x}H({modKey}), shiftPlane={shiftPlane}, shiftPlaneWhenOff={shiftPlaneWhenOff}");
            if (modKey != 0 && shiftPlane > 0) {
                logger.Info(() => $"shiftPlaneForShiftFuncKey[{modKey}] = {shiftPlane}, shiftPlaneForShiftFuncKeyWhenDecoderOff[{modKey}] = {shiftPlaneWhenOff}");
                ShiftPlaneForShiftModKey.Add(modKey, shiftPlane);
                ShiftPlaneForShiftModKeyWhenDecoderOff.Add(modKey, shiftPlaneWhenOff);
            }
            return true;    // OK
        }

        /// <summary>テーブルファイルor設定ダイアログで割り当てたSandSシフト面を優先する</summary>
        public static void AssignSandSPlane(int shiftPlane = 0)
        {
            logger.InfoH(() => $"CALLED: SandSEnabled={Settings.SandSEnabledCurrently}, SandSAssignedPlane={Settings.SandSAssignedPlane}");
            if (Settings.SandSEnabledCurrently) {
                if (shiftPlane <= 0) shiftPlane = Settings.SandSAssignedPlane;
                if (shiftPlane > 0 && shiftPlane < ShiftPlane_NUM) {
                    ShiftPlaneForShiftModKey.Add(KeyModifiers.MOD_SPACE, shiftPlane);
                }
            }
        }

        public static int GetSandSPlane()
        {
            return ShiftPlaneForShiftModKey.GetPlane(KeyModifiers.MOD_SPACE);
        }

    }
}

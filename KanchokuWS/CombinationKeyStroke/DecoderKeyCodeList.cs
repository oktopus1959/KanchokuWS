using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KanchokuWS.CombinationKeyStroke.DeterminerLib;
using Utils;

namespace KanchokuWS.CombinationKeyStroke
{
    using ShiftKeyKind = DeterminerLib.ShiftKeyPool.Kind;

    public class DecoderKeyCodeList
    {
        public ShiftKeyKind ShiftKind { get; private set; }

        public List<int> KeyList { get; private set; } = new List<int>();

        public int Count { get { return KeyList.Count; } }

        public bool IsEmpty => Count == 0;

        public bool IsOneshotShift => ShiftKind == ShiftKeyKind.OneshotShift;

        public int At(int n)
        {
            return n < KeyList.Count ? KeyList[n] : -1;
        }

        public string KeyString()
        {
            return KeyList._keyString();
        }

        /// <summary>コンストラクタ</summary>
        public DecoderKeyCodeList()
        {
        }

        public void Add(List<int> list, ShiftKeyKind shiftKind)
        {
            ShiftKind = shiftKind;
            if (list != null) KeyList.AddRange(list);
        }

        public void Add(List<Stroke> list, ShiftKeyKind shiftKind)
        {
            ShiftKind = shiftKind;
            if (list != null) KeyList.AddRange(list.Select(x => x.DecoderKeyCode));
        }

    }

    public static class DecoderKeyCodeListExtension
    {
        public static bool _isEmpty(this DecoderKeyCodeList list)
        {
            return list == null || list.IsEmpty;
        }

        public static bool _notEmpty(this DecoderKeyCodeList list)
        {
            return !list._isEmpty();
        }

        public static string _keyString(this List<int> list)
        {
            return list._notEmpty() ? list.Select(x => x.ToString())._join(":") : "";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KanchokuWS.CombinationKeyStroke.DeterminerLib;
using Utils;

namespace KanchokuWS.CombinationKeyStroke
{
    public class DecoderKeyCodeList
    {
        public List<int> KeyList { get; private set; } = new List<int>();

        public int Count { get { return KeyList.Count; } }

        public bool IsEmpty => Count == 0;

        public int At(int n)
        {
            return n < KeyList.Count ? KeyList[n] : -1;
        }

        public string KeyString()
        {
            return KeyList.Select(x => x.ToString())._join(":");
        }

        public DecoderKeyCodeList()
        {
        }

        public DecoderKeyCodeList(List<int> list)
        {
            Add(list);
        }

        public DecoderKeyCodeList(List<Stroke> list)
        {
            Add(list);
        }

        public DecoderKeyCodeList(int key)
        {
            Add(key);
        }

        public DecoderKeyCodeList(int key1, int key2)
        {
            Add(key1, key2);
        }

        public void Add(List<int> list)
        {
            if (list != null) KeyList.AddRange(list);
        }

        public void Add(List<Stroke> list)
        {
            if (list != null) KeyList.AddRange(list.Select(x => x.DecoderKeyCode));
        }

        public void Add(int key)
        {
            KeyList.Add(key);
        }

        public void Add(int key1, int key2)
        {
            KeyList.Add(key1);
            KeyList.Add(key2);
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
    }
}

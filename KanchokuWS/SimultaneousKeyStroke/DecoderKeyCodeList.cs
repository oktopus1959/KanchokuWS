using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KanchokuWS.SimultaneousKeyStroke.DeterminerLib;
using Utils;

namespace KanchokuWS.SimultaneousKeyStroke
{
    public class DecoderKeyCodeList
    {
        public List<int> KeyList { get; private set; }

        public int Count { get { return KeyList?.Count ?? 0; } }

        public int At(int n)
        {
            return KeyList != null && n < KeyList.Count ? KeyList[n] : -1;
        }

        public string KeyString()
        {
            return KeyList?.Select(x => x.ToString())._join(":") ?? "";
        }

        public DecoderKeyCodeList()
        {
            KeyList = new List<int>();
        }

        public DecoderKeyCodeList(List<int> list)
        {
            KeyList = list;
        }

        public DecoderKeyCodeList(List<Stroke> list)
        {
            KeyList = list.Select(x => x.KeyCode).ToList();
        }

        public DecoderKeyCodeList(int key)
        {
            KeyList = new List<int>();
            KeyList.Add(key);
        }

        public DecoderKeyCodeList(int key1, int key2)
        {
            KeyList = new List<int>();
            KeyList.Add(key1);
            KeyList.Add(key2);
        }

        public void Add(int key)
        {
            KeyList.Add(key);
        }
    }
}

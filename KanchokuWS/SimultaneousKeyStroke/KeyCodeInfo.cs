using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanchokuWS.SimultaneousKeyStroke
{
    /// <summary>
    /// VirtualKey, DecoderKey, KeyFace などの情報をまとめた構造体
    /// </summary>
    class KeyCodeInfo
    {
        public VirtualKeyCode VKey { get; private set; }

        public DecoderKeyCode DecKey { get; private set; }

        public string KeyFace { get; private set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanchokuWS.CombinationKeyStroke
{
    public struct VirtualKeyCode
    {
        public readonly uint Value;

        public VirtualKeyCode(uint v)
        {
            Value = v;
        }
    }

    public struct DecoderKeyCode
    {
        public readonly int Value;

        public DecoderKeyCode(int v)
        {
            Value = v;
        }
    }
}

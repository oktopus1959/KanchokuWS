using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanchokuWS.OverlappingKeyStroke
{
    /// <summary>
    /// デコーダに渡すキーシーケンス<br/>
    /// 出力文字列を得るためのストローク列となる
    /// </summary>
    class DecoderKeySequence
    {
        public IEnumerator<int> Sequence { get; private set; }
    }
}

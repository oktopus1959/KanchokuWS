using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KanchokuWS.OverlappingKeyStroke
{
    /// <summary>
    /// キーの同時打鍵定義を表すクラス
    /// </summary>
    public class KeyCombination
    {
        /// <summary>
        /// 終端の組合せか。<br/>すなわち、当組合せを含むようなより大きな同時打鍵組合せが無いか。<br/>
        /// たとえば、当組合せが [a, c] だったとして、[a, b, c] という同時打鍵組合せが存在すれば false となる
        /// </summary>
        public bool IsTerminal { get; private set; } = true;

        /// <summary>
        /// 当同時打鍵組合せに割り当てられた出力文字列を得るためにデコーダに送信する DecoderKey のリスト。<br/>
        /// 履歴機能や交ぜ書き機能などを利用できるようにするため、
        /// 当パッケージが直接 TargetString を出力するのではなく、いったんデコーダを経由して文字列を出力させる。<br/>
        /// 空リストの場合は、同時打鍵の組合せが存在しないことを表す。
        /// </summary>
        public DecoderKeyCodeList DecoderKeyList { get; private set; } = new DecoderKeyCodeList();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public KeyCombination()
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public KeyCombination(List<int> keyList)
        {
            DecoderKeyList.Add(keyList);
        }

        public void NotTerminal()
        {
            IsTerminal = false;
        }
    }
}

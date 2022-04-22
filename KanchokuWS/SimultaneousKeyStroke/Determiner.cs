using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanchokuWS.SimultaneousKeyStroke
{
    /// <summary>
    /// キー入力の時系列に対して、同時打鍵などの判定を行って、出力文字列を決定する
    /// </summary>
    class Determiner
    {
        /// <summary>
        /// キーの押下
        /// </summary>
        /// <param name="keyInfo">押下されたキーの情報</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダキー列を返す。<br/>確定しなければ null を返す</returns>
        public List<DecoderKeyCode> KeyDown(KeyCodeInfo keyInfo)
        {
            return null;
        }

        /// <summary>
        /// キーの解放
        /// </summary>
        /// <param name="keyInfo">解放されたキーの情報</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダキー列を返す。<br/>確定しなければ null を返す</returns>
        public List<DecoderKeyCode> KeyUp(KeyCodeInfo keyInfo)
        {
            return null;
        }
    }
}

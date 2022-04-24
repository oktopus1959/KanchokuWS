using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KanchokuWS.SimultaneousKeyStroke.DeterminerLib;

namespace KanchokuWS.SimultaneousKeyStroke
{
    partial class Determiner
    {
        // 同時打鍵組合せ
        private Dictionary<string, KeyCombination> keyComboDict;

        private void initialize(string tableFile)
        {
            keyComboDict = new TableFileParser().ParseTable(tableFile);
        }

        /// <summary>
        /// キーの押下
        /// </summary>
        /// <param name="keyInfo">押下されたキーの情報</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダキー列を返す。<br/>確定しなければ null を返す</returns>
        private List<DecoderKeyCode> keyDown(KeyCodeInfo keyInfo)
        {
            return null;
        }

        /// <summary>
        /// キーの解放
        /// </summary>
        /// <param name="keyInfo">解放されたキーの情報</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダキー列を返す。<br/>確定しなければ null を返す</returns>
        private List<DecoderKeyCode> keyUp(KeyCodeInfo keyInfo)
        {
            return null;
        }
    }
}

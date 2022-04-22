using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanchokuWS.SimultaneousKeyStroke.DeterminerLib
{
    /// <summary>
    /// ストローククラス<br/>
    /// 打鍵されたキーを表すキーコードと、打鍵時刻を保持する
    /// </summary>
    class Stroke
    {
        /// <summary>
        /// 打鍵されたキーのコード
        /// </summary>
        public int KeyCode { get; private set; }

        /// <summary>
        /// キーが押下された時刻
        /// </summary>
        public DateTime DownDt { get; private set; }

        /// <summary>
        /// キーが解放された時刻
        /// </summary>
        public DateTime UpDt { get; private set; }
    }
}

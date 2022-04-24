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
    public class Stroke
    {
        /// <summary>
        /// 打鍵されたキーのデコーダキーコード
        /// </summary>
        public int KeyCode { get; private set; }

        /// <summary>
        /// 同時打鍵シフトキーとして使われ得るか
        /// </summary>
        public int ShiftPriority { get; private set; }

        /// <summary>
        /// キー打鍵時の時刻
        /// </summary>
        public DateTime KeyDt { get; private set; }

        /// <summary>
        /// １つ前のキーが解放された時刻
        /// </summary>
        public DateTime PrevKeyUpDt { get; set; }

        /// <summary>
        /// キーが重複している時間(ミリ秒)
        /// </summary>
        public double TimeSpanMs(Stroke stk)
        {
            return stk.KeyDt >= KeyDt ? (stk.KeyDt - KeyDt).TotalMilliseconds : (KeyDt - stk.KeyDt).TotalMilliseconds;
        }

        /// <summary>
        /// dtまでの経過時間
        /// </summary>
        public double TimeSpanMs(DateTime dt)
        {
            return dt >= KeyDt ? (dt - KeyDt).TotalMilliseconds : (KeyDt - dt).TotalMilliseconds;
        }

        public Stroke() { }

        public Stroke(int code, int shiftPri, DateTime dt)
        {
            KeyCode = code;
            ShiftPriority = shiftPri;
            KeyDt = dt;
        }
    }
}

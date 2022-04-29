using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanchokuWS.OverlappingKeyStroke.DeterminerLib
{
    /// <summary>
    /// 打鍵キューに保持されるストロークを表現するクラス<br/>
    /// 打鍵されたキーを表すキーコードと、打鍵時刻を保持する。また、シフト状態になったキーも表現できるようにする
    /// </summary>
    public class Stroke
    {
        /// <summary>
        /// 打鍵されたキーの正規化キーコード
        /// </summary>
        public int NormalKeyCode { get; private set; }

        /// <summary>
        /// 打鍵されたキーのデコーダキーコード
        /// </summary>
        public int DecoderKeyCode { get; private set; }

        /// <summary>
        /// 同時打鍵シフトキーとしての優先順位
        /// </summary>
        public int ShiftPriority { get; private set; }

        /// <summary>
        /// 同時打鍵シフトキーとして使われ得るか
        /// </summary>
        public bool IsShiftable { get { return ShiftPriority > 0; } }

        /// <summary>
        /// 同時打鍵のシフトキーになったか
        /// </summary>
        public bool IsShifted { get; private set; }

        public void SetShifted() { IsShifted = true; }

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

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Stroke() { }

        /// <summary>
        /// コンストラクタ(引数あり)
        /// </summary>
        public Stroke(int decKey, DateTime dt)
        {
            DecoderKeyCode = decKey;
            NormalKeyCode = decKey % DecoderKeys.NORMAL_DECKEY_NUM;
            ShiftPriority = KeyCombinationPool.Singleton.GetShiftPriority(NormalKeyCode);
            KeyDt = dt;
        }
    }
}

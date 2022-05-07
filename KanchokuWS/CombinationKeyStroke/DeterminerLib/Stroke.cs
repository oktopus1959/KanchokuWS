using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace KanchokuWS.CombinationKeyStroke.DeterminerLib
{
    /// <summary>
    /// 打鍵キューに保持されるストロークを表現するクラス<br/>
    /// 打鍵されたキーを表すキーコードと、打鍵時刻を保持する。また、シフト状態になったキーも表現できるようにする
    /// </summary>
    public class Stroke
    {
        /// <summary>打鍵されたキーのModulo化キーコード(検索キーを生成するのに使用)</summary>
        public int ModuloKeyCode { get; private set; }

        /// <summary>打鍵されたキーのデコーダキーコード</summary>
        public int DecoderKeyCode { get; private set; }

        public bool IsShiftableSpaceKey => ModuloKeyCode == DecoderKeys.STROKE_SPACE_DECKEY && IsShiftable;

        //public bool IsShiftedOrShiftableSpaceKey => IsShifted || IsShiftableSpaceKey;

        /// <summary>同じキーか</summary>
        public bool IsSameKey(int decKey)
        {
            return DecoderKeyCode == decKey || ModuloKeyCode == ModuloizeKey(decKey);
        }

        /// <summary>同時打鍵検索用にモジュロ化したキーコードを返す</summary>
        public static int ModuloizeKey(int decKey) { return decKey % DecoderKeys.NORMAL_DECKEY_NUM; }

        /// <summary>Oneshotな同時打鍵キーか</summary>
        public bool IsOneshotShift { get; private set; }

        /// <summary>同時打鍵の連続シフト可能キーとして使われ得るか</summary>
        public bool IsShiftable { get; private set; }

        /// <summary>同時打鍵のシフトキーになったか</summary>
        public bool IsShifted { get; private set; }

        public void SetShifted() { IsShifted = true; }

        /// <summary>キー打鍵時の時刻</summary>
        public DateTime KeyDt { get; private set; }

        /// <summary>キーが重複している時間(ミリ秒)</summary>
        public double TimeSpanMs(Stroke stk)
        {
            return stk.KeyDt >= KeyDt ? (stk.KeyDt - KeyDt).TotalMilliseconds : (KeyDt - stk.KeyDt).TotalMilliseconds;
        }

        /// <summary>dtまでの経過時間</summary>
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
            ModuloKeyCode = ModuloizeKey(decKey);
            IsShiftable = ShiftKeyPool.IsShiftable(KeyCombinationPool.CurrentPool.GetShiftKeyKind(ModuloKeyCode));
            IsOneshotShift = ShiftKeyPool.IsOneshotShift(KeyCombinationPool.CurrentPool.GetShiftKeyKind(ModuloKeyCode));
            KeyDt = dt;
        }

    }
}

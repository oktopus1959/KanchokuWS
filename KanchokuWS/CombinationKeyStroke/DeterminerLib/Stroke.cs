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
        /// <summary>
        /// 同時打鍵検索用にモジュロ化したキーコードを返す<br/>
        /// これはシフトキーや拡張シフトキーで入力されたコードを、シフトを無視して検索するために必要となる
        /// </summary>
        public static int ModuloizeKey(int decKey) {
            return decKey % DecoderKeys.PLANE_DECKEY_NUM;
        }

        /// <summary>打鍵されたキーのModulo化キーコード(検索キーを生成するのに使用)</summary>
        public int ModuloDecKey => ModuloizeKey(OrigDecoderKey);

        public int ComboShiftDecKey => ModuloDecKey + DecoderKeys.COMBO_DECKEY_START;

        /// <summary>打鍵されたキーのデコーダキーコード</summary>
        public int OrigDecoderKey { get; private set; }

        /// <summary>同時打鍵用のキーコード</summary>
        public int ComboKeyCode => IsComboShift ? OrigDecoderKey : ModuloDecKey;

        public bool IsShiftableSpaceKey => ModuloDecKey == DecoderKeys.STROKE_SPACE_DECKEY && IsSuccessiveShift;

        //public bool IsShiftedOrShiftableSpaceKey => IsShifted || IsShiftableSpaceKey;

        /// <summary>同じキーか</summary>
        public bool IsSameKey(int decKey)
        {
            return OrigDecoderKey == decKey || ModuloDecKey == ModuloizeKey(decKey);
        }

        /// <summary>単打可能なキーか<br/>ただし出力文字列が定義されていない打鍵もある</summary>
        public bool IsSingleHittable { get; private set; }

        /// <summary>順次シフトキーか</summary>
        public bool IsSequentialShift { get; private set; }

        /// <summary>前置シフトな同時打鍵キーか</summary>
        public bool IsPrefixShift { get; private set; }

        /// <summary>Oneshotな同時打鍵キーか</summary>
        public bool IsOneshotShift { get; private set; }

        /// <summary>同時打鍵の連続シフト可能キーとして使われ得るか</summary>
        public bool IsSuccessiveShift { get; private set; }

        /// <summary>同時打鍵のシフトキーか</summary>
        public bool IsComboShift { get; private set; }

        /// <summary>同時打鍵のシフトキーになったか</summary>
        public bool IsCombined { get; private set; }

        public void SetCombined() { IsCombined = true; }

        /// <summary>同時打鍵のキーとしてつかわれたか</summary>
        //public bool IsUsedForOneshot { get; private set; }

        //public void SetUsedForOneshot() { IsUsedForOneshot = true; }

        /// <summary>UPされたキーか</summary>
        public bool IsUpKey { get; private set; }

        public void SetUpKey() { IsUpKey = true; }

        /// <summary>削除されるべきキーか</summary>
        public bool ToBeRemoved => _toBeRemoved;
        //public bool ToBeRemoved => _toBeRemoved || IsUsedForOneshot;

        private bool _toBeRemoved = false;

        public void SetToBeRemoved() { _toBeRemoved = true; }

        public void ResetToBeRemoved() { _toBeRemoved = false; }

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
            OrigDecoderKey = decKey;
            IsSuccessiveShift = KeyCombinationPool.IsComboSuccessive(OrigDecoderKey);
            IsOneshotShift = KeyCombinationPool.IsComboOneshot(OrigDecoderKey);
            IsPrefixShift = KeyCombinationPool.IsComboPrefix(OrigDecoderKey);
            IsComboShift = KeyCombinationPool.IsComboShift(OrigDecoderKey);
            IsSequentialShift = KeyCombinationPool.IsSequential(OrigDecoderKey);
            IsSingleHittable = KeyCombinationPool.CurrentPool.GetEntry(decKey)?.DecKeyList != null;
            KeyDt = dt;
        }

        public string DebugString() => $"DecKeyCode={OrigDecoderKey}, ModKeyCode={ModuloDecKey}, IsComobShift={IsComboShift}";

    }
}

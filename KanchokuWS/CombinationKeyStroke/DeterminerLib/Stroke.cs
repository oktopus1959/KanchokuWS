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

        public int ComboShiftDecKey => ModuloDecKey + DecoderKeys.GetComboDeckeyStart(IsKanchokuMode);

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

        /// <summary>漢直モード(デコーダOn)か</summary>
        public bool IsKanchokuMode { get; private set; }

        /// <summary>スペースキーまたは機能キーか</summary>
        public bool IsSpaceOrFunc => DecoderKeys.IsSpaceOrFuncKey(OrigDecoderKey);

        /// <summary>単打可能なキーか<br/>同時打鍵として使われていない(entry == null)か、出力文字または機能が定義されている</summary>
        public bool IsSingleHittable { get; private set; }

        /// <summary>単打可能なキーか<br/>同時打鍵として使われていない(entry == null)か、出力文字が定義されている</summary>
        public bool HasStringOrSingleHittable { get; private set; }

        /// <summary>出力文字が定義されているか<br/>同時打鍵として使われており(entry != null)、出力文字が定義されている</summary>
        public bool HasString { get; private set; }

        /// <summary>同時打鍵として使われており(entry != null)、デコーダに送られるキーを持つ<br/>ただし、出力される文字や機能が定義されているとは限らない</summary>
        public bool HasDecKeyList { get; private set; }

        /// <summary>単打キーか<br/>同時打鍵として使われていない(entry == null)か、終端キーである</summary>
        public bool IsJustSingleHit { get; private set; }

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

        /// <summary>単打不可の同時打鍵のシフトキーか</summary>
        public bool IsJustComboShift => IsComboShift && !HasStringOrSingleHittable;

        /// <summary>スペースキーまたは機能キーの同時打鍵シフトキーか</summary>
        public bool IsSpaceOrFuncComboShift => IsSpaceOrFunc && IsComboShift;

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

        /// <summary>キーの押下間隔(ミリ秒)</summary>
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
        public Stroke(int decKey, bool bDecoderOn, DateTime dt)
        {
            OrigDecoderKey = decKey;
            IsKanchokuMode = bDecoderOn;
            IsSuccessiveShift = KeyCombinationPool.IsComboSuccessive(OrigDecoderKey);
            IsOneshotShift = KeyCombinationPool.IsComboOneshot(OrigDecoderKey);
            IsPrefixShift = KeyCombinationPool.IsComboPrefix(OrigDecoderKey);
            IsComboShift = KeyCombinationPool.IsComboShift(OrigDecoderKey);
            IsSequentialShift = KeyCombinationPool.IsSequential(OrigDecoderKey);
            var entry = KeyCombinationPool.CurrentPool.GetEntry(decKey);
            IsSingleHittable = entry == null || entry.HasDecoderOutput;         // 同時打鍵として使われていない(entry == null)か、出力文字または機能が定義されている
            HasStringOrSingleHittable = entry == null || entry.HasString;       // 同時打鍵として使われていない(entry == null)か、出力文字が定義されている
            IsJustSingleHit = entry == null || entry.IsTerminal;                // 同時打鍵として使われていない(entry == null)か、終端キーである
            HasDecKeyList = entry != null && entry.DecKeyList != null;          // 同時打鍵として使われており(entry != null)、デコーダに送られるキーを持つ
            HasString = entry != null && entry.HasString;                       // 同時打鍵として使われており(entry != null)、出力文字が定義されている
            KeyDt = dt;
        }

        public string DebugString() =>
            $"DecKeyCode={OrigDecoderKey}, ModuloKeyCode={ModuloDecKey}, IsComobShift={IsComboShift}, HasStringOrSingleHittable={HasStringOrSingleHittable}, HasDecKeyList={HasDecKeyList}, HasString={HasString}";

    }
}

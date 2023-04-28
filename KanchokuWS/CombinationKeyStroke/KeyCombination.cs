using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace KanchokuWS.CombinationKeyStroke
{
    using ShiftKeyKind = DeterminerLib.ComboShiftKeyPool.ComboKind;

    /// <summary>
    /// キーの同時打鍵定義を表すクラス
    /// </summary>
    class KeyCombination
    {
        public static int MakeNonTerminalDuplicatableComboKey(int decKey)
        {
            return (decKey % DecoderKeys.PLANE_DECKEY_NUM) + DecoderKeys.PLANE_DECKEY_NUM;
        }

        public ShiftKeyKind ShiftKind { get; private set; }

        /// <summary>
        /// 終端の組合せか。<br/>すなわち、当組合せを含むようなより大きな同時打鍵組合せが無いか。<br/>
        /// たとえば、当組合せが [a, c] だったとして、[a, b, c] という同時打鍵組合せが存在すれば false となる
        /// </summary>
        public bool IsTerminal { get; private set; } = true;

        public void SetNonTerminal()
        {
            IsTerminal = false;
        }

        /// <summary>
        /// 文字出力のある組合せか
        /// </summary>
        public bool HasString { get; private set; } = false;

        /// <summary>
        /// 機能の定義されている組合せか
        /// </summary>
        public bool HasFunction { get; private set; } = false;

        /// <summary>
        /// デコ－ダからの出力が定義されている組合せか
        /// </summary>
        public bool HasDecoderOutput => HasString || HasFunction;

        /// <summary>
        /// 文字キーのみの組合せか<br/>すなわちスペースキーや機能キーを含まない組合せか
        /// </summary>
        public bool OnlyCharacterKeys { get; private set; } = false;

        /// <summary>
        /// Oneshotの同時打鍵か<br/>すなわち、当組合せの同時打鍵が発生したら、それ打鍵列は次に持ち越さずに破棄されるか
        /// </summary>
        //public bool IsOneshot => ComboShiftedDecoderKeyList.ShiftKind == ShiftKeyKind.OneshotShift;
        public bool IsOneshotShift => ShiftKind == ShiftKeyKind.UnorderedOneshotShift;

        /// <summary>
        /// 順序不定の組合せか
        /// </summary>
        public bool IsUnordered => DeterminerLib.ComboShiftKeyPool.IsUnorderedShift(ShiftKind);

        /// <summary>同時打鍵ブロッカーにより、以降、順次打鍵になるか</summary>
        public bool IsComboBlocked { get; set; } = false;

        ///// <summary>デコーダがOFFの時にも有効な同時打鍵か</summary>
        //public bool IsEffectiveAlways { get; private set; } = false;

        /// <summary>当同時打鍵組合せに割り当てられた出力文字列を得るためにデコーダに送信する DecoderKey のリスト</summary>
        public List<int> DecKeyList { get; private set; }

        /// <summary>当同時打鍵組合せは同時打鍵列の部分キーか</summary>
        public bool IsSubKey => DecKeyList._isEmpty();

        //private List<int> _comboKeyList { get; set; }
        /// <summary>同時打鍵組合せのデバッグ表示用文字列</summary>
        private string _comboKeyStr { get; set; }

        private bool containsOnlyCharKeys(List<int> decKeyList)
        {
            return decKeyList._notEmpty() && decKeyList.All(dk => dk >= 0 && !DecoderKeys.IsSpaceOrFuncKey(DeterminerLib.Stroke.ModuloizeKey(dk)));
        }

        /// <summary>
        /// コンストラクタ(keyListがnullの場合は、同時打鍵集合の部分集合であることを示す)
        /// </summary>
        public KeyCombination(List<int> decKeyList, string comboKeyStr, ShiftKeyKind shiftKind, bool hasStr, bool hasFunc, bool comboBlocked)
        {
            //ComboShiftedDecoderKeyList.Add(decKeyList, shiftKind);
            DecKeyList = decKeyList;
            _comboKeyStr = comboKeyStr;
            ShiftKind = shiftKind;
            HasString = hasStr;
            HasFunction = hasFunc;
            IsComboBlocked = comboBlocked;
            OnlyCharacterKeys = containsOnlyCharKeys(decKeyList);
            //IsEffectiveAlways = effectiveAlways;
        }

        public string DecKeysDebugString()
        {
            return DecKeyList._keyString()._orElse("(empty)");
        }

        public string ComboKeysDebugString()
        {
            return _comboKeyStr._orElse("(empty)");
        }

        public string DebugString()
        {
            return ComboKeysDebugString() + "/" + DecKeysDebugString();
        }
    }

    public static class KeyCombinationExtension
    {
        public static string _keyString(this IEnumerable<int> list)
        {
            return list._notEmpty() ? list.Select(x => x._keyString())._join(":") : "";
        }

        public static string _keyString(this int key)
        {
            return key.ToString();
        }

        public static int _keyLengh(this string key)
        {
            // key は数字列を : で連結した形をしている
            return key._notEmpty() ? key.Length - key.Replace(":", "").Length + 1 : 0;
        }

        public static List<int> _decodeKeyStr(this string key)
        {
            return key._notEmpty() ? key._split(':').Select(x => x._parseInt()).ToList() : new List<int>();
        }

    }
}

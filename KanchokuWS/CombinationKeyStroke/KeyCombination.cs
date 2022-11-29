﻿using System;
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
        public bool HasString { get; set; } = false;

        /// <summary>
        /// Oneshotの同時打鍵か<br/>すなわち、当組合せの同時打鍵が発生したら、それ打鍵列は次に持ち越さずに破棄されるか
        /// </summary>
        //public bool IsOneshot => ComboShiftedDecoderKeyList.ShiftKind == ShiftKeyKind.OneshotShift;
        public bool IsOneshotShift => ShiftKind == ShiftKeyKind.UnorderedOneshotShift;

        ///// <summary>デコーダがOFFの時にも有効な同時打鍵か</summary>
        //public bool IsEffectiveAlways { get; private set; } = false;

        /// <summary>当同時打鍵組合せに割り当てられた出力文字列を得るためにデコーダに送信する DecoderKey のリスト</summary>
        public List<int> DecKeyList { get; private set; }

        //private List<int> _comboKeyList { get; set; }
        /// <summary>同時打鍵組合せのデバッグ表示用文字列</summary>
        private string _comboKeyStr { get; set; }

        /// <summary>
        /// コンストラクタ(keyListがnullの場合は、同時打鍵集合の部分集合であることを示す)
        /// </summary>
        public KeyCombination(List<int> decKeyList, string comboKeyStr, ShiftKeyKind shiftKind, bool hasStr)
        {
            //ComboShiftedDecoderKeyList.Add(decKeyList, shiftKind);
            DecKeyList = decKeyList;
            _comboKeyStr = comboKeyStr;
            ShiftKind = shiftKind;
            HasString = hasStr;
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

        public static List<int> _decodeKeyStr(this string key)
        {
            return key._notEmpty() ? key._split(':').Select(x => x._parseInt()).ToList() : new List<int>();
        }

    }
}

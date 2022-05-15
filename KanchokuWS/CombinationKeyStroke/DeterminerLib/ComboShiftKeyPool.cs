using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace KanchokuWS.CombinationKeyStroke.DeterminerLib
{
    public class ComboShiftKeyPool
    {
        public enum ComboKind {
            None,
            PreShift,
            MutualShift,
            OneshotShift
        }

        public static bool IsComboShift(ComboKind kind) { return kind != ComboKind.None; }

        public static bool IsContinuousShift(ComboKind kind) { return kind == ComboKind.PreShift || kind == ComboKind.MutualShift; }

        public static bool IsOneshotShift(ComboKind kind) { return kind == ComboKind.OneshotShift; }

        public static bool IsMutualOrOneshotShift(ComboKind kind) { return kind == ComboKind.MutualShift || kind == ComboKind.OneshotShift; }

        private Dictionary<int, ComboKind> comboKindDict = new Dictionary<int, ComboKind>();

        public IEnumerable<KeyValuePair<int, ComboKind>> Pairs { get { return comboKindDict.AsEnumerable(); } }

        public bool ContainsMutualOrOneshotShiftKey { get; private set; } = false;

        public bool ContainsContinuousShiftKey { get; private set; } = false;

        /// <summary>
        /// ShiftKeyとして扱いうるキーの設定
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="kind"></param>
        public void AddShiftKey(int keyCode, ComboKind kind)
        {
            if (kind != ComboKind.None && !comboKindDict.ContainsKey(keyCode)) {
                comboKindDict[keyCode] = kind;
                if (IsMutualOrOneshotShift(kind)) ContainsMutualOrOneshotShiftKey = true;
                if (IsContinuousShift(kind)) ContainsContinuousShiftKey = true;
            }
        }

        /// <summary>
        /// keyCode に割り振られた ComboKind を返す。<br/>
        /// None(0) なら ShiftKey としては扱わない<br/>
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        public ComboKind GetComboKind(int keyCode)
        {
            return comboKindDict._safeGet(keyCode);
        }

        public void Clear()
        {
            comboKindDict.Clear();
        }
    }
}

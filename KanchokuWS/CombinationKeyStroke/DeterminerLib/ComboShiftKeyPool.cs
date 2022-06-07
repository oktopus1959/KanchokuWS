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
        private static Logger logger = Logger.GetLogger(true);

        public enum ComboKind {
            None,
            /// <summary>前置連続シフト(テーブルの深さは、シフトキー含め2とする)</summary>
            PrefixSuccessiveShift,
            /// <summary>相互連続シフト</summary>
            UnorderedSuccessiveShift,
            /// <summary>相互ワンショットシフト</summary>
            UnorderedOneshotShift
        }

        public static bool IsComboShift(ComboKind kind) { return kind != ComboKind.None; }

        public static bool IsSuccessiveShift(ComboKind kind) { return kind == ComboKind.PrefixSuccessiveShift || kind == ComboKind.UnorderedSuccessiveShift; }

        public static bool IsOneshotShift(ComboKind kind) { return kind == ComboKind.UnorderedOneshotShift; }

        public static bool IsPrefixShift(ComboKind kind) { return kind == ComboKind.PrefixSuccessiveShift; }

        public static bool IsUnorderedShift(ComboKind kind) { return kind == ComboKind.UnorderedSuccessiveShift || kind == ComboKind.UnorderedOneshotShift; }

        private Dictionary<int, ComboKind> comboKindDict = new Dictionary<int, ComboKind>();

        public IEnumerable<KeyValuePair<int, ComboKind>> Pairs { get { return comboKindDict.AsEnumerable(); } }

        public bool ContainsComboShiftKey => comboKindDict.Count > 0;

        public bool ContainsUnorderedShiftKey { get; private set; } = false;

        public bool ContainsSuccessiveShiftKey { get; private set; } = false;

        /// <summary>
        /// ShiftKeyとして扱いうるキーの設定
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="kind"></param>
        public void AddShiftKey(int keyCode, ComboKind kind)
        {
            if (kind != ComboKind.None && !comboKindDict.ContainsKey(keyCode)) {
                logger.DebugH(() => $"ADD: keyCode={keyCode}, Kind={kind}");
                comboKindDict[keyCode] = kind;
                if (IsUnorderedShift(kind)) ContainsUnorderedShiftKey = true;
                if (IsSuccessiveShift(kind)) ContainsSuccessiveShiftKey = true;
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
            logger.DebugH("CALLED");
            comboKindDict.Clear();
            ContainsUnorderedShiftKey = false;
            ContainsSuccessiveShiftKey = false;
        }
    }
}

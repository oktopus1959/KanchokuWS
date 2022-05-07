using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace KanchokuWS.CombinationKeyStroke.DeterminerLib
{
    public class ShiftKeyPool
    {
        public enum Kind {
            None,
            PreShift,
            MutualShift,
            OneshotShift
        }

        public static bool IsShiftable(Kind kind) { return kind == Kind.PreShift || kind == Kind.MutualShift; }

        public static bool IsOneshotShift(Kind kind) { return kind == Kind.OneshotShift; }

        private Dictionary<int, Kind> shiftKindDict = new Dictionary<int, Kind>();

        public IEnumerable<KeyValuePair<int, Kind>> Pairs { get { return shiftKindDict.AsEnumerable(); } }

        /// <summary>
        /// ShiftKeyとして扱いうるキーの設定
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="kind"></param>
        public void AddShiftKey(int keyCode, Kind kind)
        {
            if (kind != Kind.None) shiftKindDict[Stroke.ModuloizeKey(keyCode)] = kind;
        }

        /// <summary>
        /// keyCode に割り振られた ShiftKeyPool.Kind を返す。<br/>
        /// None(0) なら ShiftKey としては扱わない<br/>
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        public Kind GetShiftKeyKind(int keyCode)
        {
            return shiftKindDict._safeGet(Stroke.ModuloizeKey(keyCode));
        }

        public void Clear()
        {
            shiftKindDict.Clear();
        }
    }
}

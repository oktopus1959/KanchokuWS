using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace KanchokuWS.OverlappingKeyStroke.DeterminerLib
{
    class ShiftKeyPriority
    {
        private Dictionary<int, int> priorityDict = new Dictionary<int, int>();

        public IEnumerable<KeyValuePair<int, int>> Pairs { get { return priorityDict.AsEnumerable(); } }

        /// <summary>
        /// ShiftKeyとして扱いうるキーの設定(priority は 1以上であること。1が最優先)
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="priority"></param>
        public void AddShiftKey(int keyCode, int priority)
        {
            if (priority > 0) priorityDict[Stroke.NormalizeKey(keyCode)] = priority;
        }

        /// <summary>
        /// keyCode が ShiftKeyとしても扱われるか否かを返す。<br/>
        /// 0 なら ShiftKey としては扱わない<br/>
        /// 1以上なら、ShiftKeyとしての優先度となる(1が最優先)
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        public int GetShiftPriority(int keyCode)
        {
            return priorityDict._safeGet(Stroke.NormalizeKey(keyCode));
        }

        public void Clear()
        {
            priorityDict.Clear();
        }
    }
}

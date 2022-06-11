using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utils;

namespace KanchokuWS.CombinationKeyStroke.DeterminerLib
{
    class RepeatableKeyPool
    {
        private HashSet<int> repeatableKeySet = new HashSet<int>();

        public bool IsRepeatable(int key) { return repeatableKeySet.Contains(key); }

        public HashSet<int> GetSet() { return repeatableKeySet; }

        /// <summary>
        /// Repeatableとして扱いうるキーの設定
        /// </summary>
        /// <param name="keyCode"></param>
        public void AddRepeatableKey(int keyCode)
        {
            repeatableKeySet.Add(keyCode);
        }

        public void Clear()
        {
            repeatableKeySet.Clear();
        }

        public string DebugString()
        {
            return repeatableKeySet._isEmpty() ? "empty" : repeatableKeySet.Select(x => x.ToString())._join(",");
        }
    }
}

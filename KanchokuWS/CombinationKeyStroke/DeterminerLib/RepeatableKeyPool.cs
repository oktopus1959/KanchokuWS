using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanchokuWS.CombinationKeyStroke.DeterminerLib
{
    class RepeatableKeyPool
    {
        public bool IsRepeatable(int key) { return repeatableKeySet.Contains(key); }

        private HashSet<int> repeatableKeySet = new HashSet<int>();

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
    }
}

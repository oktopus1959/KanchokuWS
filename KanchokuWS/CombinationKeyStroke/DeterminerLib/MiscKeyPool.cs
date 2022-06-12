using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utils;

namespace KanchokuWS.CombinationKeyStroke.DeterminerLib
{
    class MiscKeyPool
    {
        /// <summary>Repeat可能なキーの集合</summary>
        private HashSet<int> repeatableKeySet = new HashSet<int>();

        public bool IsRepeatable(int key) { return repeatableKeySet.Contains(key); }

        /// <summary>
        /// Repeatableとして扱いうるキーの設定
        /// </summary>
        /// <param name="keyCode"></param>
        public void AddRepeatableKey(int keyCode)
        {
            repeatableKeySet.Add(keyCode);
        }

        /// <summary>前置書き換えなキーの集合</summary>
        private HashSet<int> preRewriteKeySet = new HashSet<int>();

        public bool IsPreRewrite(int key) { return preRewriteKeySet.Contains(key); }

        /// <summary>
        /// 前置書き換えキーの設定
        /// </summary>
        /// <param name="keyCode"></param>
        public void AddPreRewriteKey(int keyCode)
        {
            preRewriteKeySet.Add(keyCode);
        }

        public void Clear()
        {
            repeatableKeySet.Clear();
            preRewriteKeySet.Clear();
        }

        public string DebugString()
        {
            return "RepeatableKeys=" + (repeatableKeySet._isEmpty() ? "empty" : repeatableKeySet.Select(x => x.ToString())._join(",")) +
                "\nPreRewriteKeys=" + (preRewriteKeySet._isEmpty() ? "empty" : preRewriteKeySet.Select(x => x.ToString())._join(","));
        }
    }
}

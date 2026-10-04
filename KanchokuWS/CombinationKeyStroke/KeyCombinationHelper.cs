using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KanchokuWS.CombinationKeyStroke.DeterminerLib;
using Utils;

namespace KanchokuWS.CombinationKeyStroke
{
    static class KeyCombinationHelper
    {
        private static Logger logger = Logger.GetLogger();

        public static bool _isTerminal(this KeyCombination keyCombo)
        {
            return keyCombo?.IsTerminal ?? false;
        }

        /// <summary>
        /// 保持している打鍵列のModuloDecKeyから、キーリストを生成する
        /// </summary>
        public static List<int> _toModuloDecKeyList(this IEnumerable<Stroke> keyList)
        {
            return keyList.Select(x => x.ModuloDecKey).ToList();
        }

        /// <summary>
        /// 保持している打鍵列のOrigDecKeyから、キーリストを生成する
        /// </summary>
        /// <param name="keyList"></param>
        /// <param name="lastKey"></param>
        /// <returns></returns>
        public static List<int> _toOrigDecKeyList(this IEnumerable<Stroke> keyList)
        {
            return keyList.Select(x => x.OrigDecoderKey).ToList();
        }

        /// <summary>
        /// keyList を昇順にソートしたキー列(':'区切りの文字列)を返す
        /// </summary>
        /// <param name="keyList"></param>
        /// <returns></returns>
        public static string _sortedKeyString(this IEnumerable<int> keyList)
        {
            return keyList.OrderBy(x => x)._keyString();
        }

        /// <summary>
        /// 全体よりも短い部分キーを順次返す<br/>
        /// bUnordered=trueなら、元の順序を保つ全ての組合せを重複なく返す
        /// bUnordered=falseなら、順序固定で末尾から1つずつ短くしたものを返す
        /// </summary>
        /// <param name="keyList"></param>
        /// <returns></returns>
        public static IEnumerable<string> _makeSubKeys(this List<int> keyList, bool bUnordered)
        {
            int count = keyList._safeCount();
            if (!bUnordered) {
                // 順序固定で末尾から1つずつ短くしたものを採用
                for (int len = count - 1; len >= 1; --len) {
                    yield return keyList.Take(len)._keyString();
                }
            } else if (count > 1) {
                var indices = new int[count - 1];
                var seen = new HashSet<string>();
                // 長い組合せから列挙し、同じキーコードによる重複も除く
                for (int len = count - 1; len >= 1; --len) {
                    foreach (var subkey in enumerateSubKeys(keyList, indices, len, 0, 0)) {
                        if (seen.Add(subkey)) yield return subkey;
                    }
                }
            }
        }

        // 添字を昇順に選び、同じ部分集合を別の除去順から再生成しない
        private static IEnumerable<string> enumerateSubKeys(List<int> keyList, int[] indices, int length, int depth, int start)
        {
            for (int i = start; i <= keyList.Count - (length - depth); ++i) {
                indices[depth] = i;
                if (depth + 1 == length) {
                    yield return indices.Take(length).Select(index => keyList[index])._keyString();
                } else {
                    foreach (var subkey in enumerateSubKeys(keyList, indices, length, depth + 1, i + 1)) {
                        yield return subkey;
                    }
                }
            }
        }

    }
}

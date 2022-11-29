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
        /// 全体よりも長さの短いリストの順列置換されたキーのリストを返す<br/>
        /// bUnordered=trueなら、順序固定で1つずつ短くした全ての組合せを返す
        /// bUnordered=falseなら、順序固定で末尾から1つずつ短くしたものを返す
        /// </summary>
        /// <param name="keyList"></param>
        /// <returns></returns>
        public static List<string> _makeSubKeys(this List<int> keyList, bool bUnordered)
        {
            var result = new List<string>();
            if (!bUnordered) {
                // 順序固定で末尾から1つずつ短くしたものを採用
                for (int len = keyList._safeCount() - 1; len >= 1; --len) {
                    result.Add(keyList.Take(len)._keyString());
                }
            } else {
                // 順序固定で1つずつ短くした全ての組合せを返す
                if (keyList._safeCount() > 1) {
                    // 長さが2以上になる組合せを登録
                    addSubKeys(keyList, result);
                    // 個々のキーを登録
                    foreach (var k in keyList) {
                        result.Add(k._keyString());
                    }
                }
            }
            return result;
        }

        // keyListが3つ以上のキーを持つ場合に、その部分リスト集合を result に追加する
        // bUnordered = true の時だけ呼ぶこと
        private static void addSubKeys(List<int> keyList, List<string> result)
        {
            if (keyList.Count > 3) {
                for (int i = keyList.Count - 1; i >= 0; --i) {
                    var subList = keyList.Take(i).ToList();
                    if (i < keyList.Count - 1) subList.AddRange(keyList.Skip(i + 1));
                    result.Add(subList._keyString());
                    addSubKeys(subList, result);
                }
            } else if (keyList.Count == 3) {
                result.Add(Helper.MakeList(keyList[0], keyList[1])._keyString());
                result.Add(Helper.MakeList(keyList[0], keyList[2])._keyString());
                result.Add(Helper.MakeList(keyList[1], keyList[2])._keyString());
            }
        }

    }
}

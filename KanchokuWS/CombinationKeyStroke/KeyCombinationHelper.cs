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
        /// 保持している打鍵列と追加の打鍵から、検索キーを生成する
        /// </summary>
        /// <param name="keyList"></param>
        /// <param name="lastKey"></param>
        /// <returns></returns>
        public static string MakePrimaryKey(IEnumerable<int> keyList, int lastKey = -1)
        {
            var sb = new StringBuilder();
            if (keyList._notEmpty()) {
                foreach (var k in keyList) {
                    sb.Append(makeChar(k));
                }
            }
            if (lastKey >= 0) sb.Append(makeChar(lastKey));
            return sb.ToString();
        }

        /// <summary>
        /// 保持している打鍵列のModuloDecKeyと追加の打鍵から、検索キーを生成する
        /// </summary>
        /// <param name="keyList"></param>
        /// <param name="lastKey"></param>
        /// <returns></returns>
        public static string MakePrimaryKeyFromModuloDecKey(IEnumerable<Stroke> keyList, int lastKey = -1)
        {
            return MakePrimaryKey(keyList.Select(x => x.ModuloDecKey), lastKey);
        }

        /// <summary>
        /// 保持している打鍵列のOrigDecKeyと追加の打鍵から、検索キーを生成する
        /// </summary>
        /// <param name="keyList"></param>
        /// <param name="lastKey"></param>
        /// <returns></returns>
        public static string MakePrimaryKeyFromOrigDecKey(IEnumerable<Stroke> keyList, int lastKey = -1)
        {
            return MakePrimaryKey(keyList.Select(x => x.OrigDecoderKey), lastKey);
        }

        /// <summary>
        /// 保持している打鍵列と追加の打鍵から、検索キーを生成する
        /// </summary>
        /// <param name="keyList"></param>
        /// <param name="decKey"></param>
        /// <returns></returns>
        public static string MakePrimaryKey(int decKey)
        {
            return makeChar(decKey).ToString();
        }

        /// <summary>
        /// PrimaryKey以外の順列置換されたキーのリストを返す<br/>ただし、bPrefix=true なら、リストの先頭は固定する
        /// </summary>
        /// <param name="keyList"></param>
        /// <param name="bPrefix"></param>
        /// <returns></returns>
        public static List<string> MakePermutatedKeys(List<int> keyList, bool bPrefix)
        {
            return makePermutatedKeys(keyList, bPrefix, false);
        }

        /// <summary>
        /// PrimaryKey以外の順列置換されたキーのリストを返す<br/>ただし、bPrefix=true なら、リストの先頭は固定する。またbAll=true なら、PrimaryKeyも含めた全キーを返す
        /// </summary>
        /// <param name="keyList"></param>
        /// <param name="bPrefix"></param>
        /// <param name="bAll"></param>
        /// <returns></returns>
        private static List<string> makePermutatedKeys(List<int> keyList, bool bPrefix, bool bAll)
        {
            var result = new List<string>();
            if (keyList._notEmpty()) {
                if (keyList.Count == 1) {
                    if (bAll) result.Add(makeString(keyList[0]));
                } else if (keyList.Count == 2) {
                    if (bAll) result.Add(makeString(keyList[0], keyList[1]));
                    if (!bPrefix) result.Add(makeString(keyList[1], keyList[0]));
                } else if (keyList.Count == 3) {
                    if (bAll) result.Add(makeString(keyList[0], keyList[1], keyList[2]));
                    result.Add(makeString(keyList[0], keyList[2], keyList[1]));
                    if (!bPrefix) {
                        result.Add(makeString(keyList[1], keyList[0], keyList[2]));
                        result.Add(makeString(keyList[1], keyList[2], keyList[0]));
                        result.Add(makeString(keyList[2], keyList[0], keyList[1]));
                        result.Add(makeString(keyList[2], keyList[1], keyList[0]));
                    }
                } else {
                    for (int i = 0; i < keyList.Count; ++i) {
                        if (!bPrefix || i == 0) {
                            // 先頭固定なら i==0 のときだけ
                            string ks = makeString(keyList[i]);
                            var subList = keyList.Take(i).ToList();
                            if (i < keyList.Count - 1) subList.AddRange(keyList.Skip(i + 1));
                            bool bAllSub = bAll || i != 0;
                            foreach (var k in makePermutatedKeys(subList, false, true)) {
                                //logger.DebugH(() => $"ADD: {ks + k}");
                                if (bAllSub) result.Add(ks + k);
                                bAllSub = true;
                            }
                        }
                    }
                    if (!bAll) result.RemoveAt(0);
                }
            }
            return result;
        }

        /// <summary>
        /// 全体よりも長さの短いリストの順列置換されたキーのリストを返す
        /// </summary>
        /// <param name="keyList"></param>
        /// <returns></returns>
        public static List<string> MakeSubKeys(List<int> keyList, bool bPrefix)
        {
            var result = new List<string>();
            if (bPrefix) {
                if (keyList._safeCount() > 2) {
                    makeSubKeys(keyList, result, true);
                    foreach (var k in keyList.Skip(1)) {
                        result.Add(makeString(keyList[0], k));
                    }
                }
                result.Add(makeString(keyList[0]));
            } else {
                if (keyList._safeCount() > 1) {
                    makeSubKeys(keyList, result, false);
                    foreach (var k in keyList) {
                        result.Add(makeString(k));
                    }
                }
            }
            return result;
        }

        private static void makeSubKeys(List<int> keyList, List<string> result, bool bPrefix)
        {
            if (keyList != null && keyList.Count > 2) {
                if (keyList.Count == 3) {
                    result.Add(makeString(keyList[0], keyList[1]));
                    result.Add(makeString(keyList[0], keyList[2]));
                    result.Add(makeString(keyList[1], keyList[0]));
                    result.Add(makeString(keyList[1], keyList[2]));
                    result.Add(makeString(keyList[2], keyList[0]));
                    result.Add(makeString(keyList[2], keyList[1]));
                } else if (bPrefix && keyList.Count == 4) {
                    result.Add(makeString(keyList[0], keyList[1], keyList[2]));
                    result.Add(makeString(keyList[0], keyList[1], keyList[3]));
                    result.Add(makeString(keyList[0], keyList[2], keyList[1]));
                    result.Add(makeString(keyList[0], keyList[2], keyList[3]));
                    result.Add(makeString(keyList[0], keyList[3], keyList[1]));
                    result.Add(makeString(keyList[0], keyList[3], keyList[2]));
                } else {
                    int start = bPrefix ? 1 : 0;
                    for (int i = keyList.Count - 1; i >= start; --i) {
                        var subList = keyList.Take(i).ToList();
                        if (i < keyList.Count - 1) subList.AddRange(keyList.Skip(i + 1));
                        result.AddRange(makePermutatedKeys(subList, false, true));
                        makeSubKeys(subList, result, bPrefix);
                    }
                }
            }
        }

        public static List<int> DecodeKey(string key)
        {
            return key._notEmpty() ? key.Select(x => decodeChar(x)).ToList() : new List<int>();
        }

        public static string DecodeKeyString(string key)
        {
            return key._notEmpty() ? key.Select(x => decodeChar(x).ToString())._join(":") : "";
        }

        public static string EncodeKeyList(IEnumerable<int> keyList)
        {
            return keyList?.Select(x => x.ToString())._join(":") ?? "";
        }

        private static string makeString(int c)
        {
            return makeChar(c).ToString();
        }

        private static string makeString(int c1, int c2)
        {
            return new string(new char[] { makeChar(c1), makeChar(c2)});
        }

        private static string makeString(int c1, int c2, int c3)
        {
            return new string(new char[] { makeChar(c1), makeChar(c2), makeChar(c3)});
        }

        private static string makeString(IEnumerable<int> list)
        {
            var sb = new StringBuilder();
            foreach (var c in list) sb.Append(makeChar(c));
            return sb.ToString();
        }

        private static char makeChar(int ch)
        {
            return (char)(ch + 0x20);
        }

        private static int decodeChar(char ch)
        {
            return (char)(ch - 0x20);
        }
    }
}

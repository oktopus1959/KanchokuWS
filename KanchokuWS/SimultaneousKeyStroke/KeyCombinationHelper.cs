﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace KanchokuWS.SimultaneousKeyStroke
{
    static class KeyCombinationHelper
    {
        public static string MakePrimaryKey(List<int> keyList)
        {
            var sb = new StringBuilder();
            foreach (var k in keyList) {
                sb.Append(makeChar(k));
            }
            return sb.ToString();
        }

        /// <summary>
        /// PrimaryKey以外の順列置換されたキーのリストを返す<br/>ただし、bAll=true なら、PrimaryKeyも含めた全キーを返す
        /// </summary>
        /// <param name="keyList"></param>
        /// <returns></returns>
        public static List<string> MakePermutatedKeys(List<int> keyList, bool bAll = false)
        {
            var result = new List<string>();
            if (keyList._notEmpty()) {
                if (keyList.Count == 1) {
                    if (bAll) result.Add(makeString(keyList[0]));
                } else if (keyList.Count == 2) {
                    if (bAll) result.Add(makeString(keyList[0], keyList[1]));
                    result.Add(makeString(keyList[1], keyList[0]));
                } else if (keyList.Count == 3) {
                    if (bAll) result.Add(makeString(keyList[0], keyList[1], keyList[2]));
                    result.Add(makeString(keyList[0], keyList[2], keyList[1]));
                    result.Add(makeString(keyList[1], keyList[0], keyList[2]));
                    result.Add(makeString(keyList[1], keyList[2], keyList[0]));
                    result.Add(makeString(keyList[2], keyList[0], keyList[1]));
                    result.Add(makeString(keyList[2], keyList[1], keyList[0]));
                } else {
                    for (int i = keyList.Count - 1; i >= 0; --i) {
                        char kc = (char)keyList[i];
                        var subList = keyList.Take(i).ToList();
                        if (i < keyList.Count - 1) subList.AddRange(keyList.Skip(i + 1));
                        foreach (var k in MakePermutatedKeys(subList, true)) {
                            result.Add(k.Append(kc).ToString());
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
        public static List<string> MakeSubKeys(List<int> keyList)
        {
            var result = new List<string>();
            if (keyList != null && keyList.Count > 1) {
                if (keyList.Count == 2) {
                    result.Add(makeString(keyList[0]));
                    result.Add(makeString(keyList[1]));
                } else if (keyList.Count == 3) {
                    result.Add(makeString(keyList[0], keyList[1]));
                    result.Add(makeString(keyList[1], keyList[0]));
                    result.Add(makeString(keyList[0], keyList[2]));
                    result.Add(makeString(keyList[2], keyList[0]));
                    result.Add(makeString(keyList[1], keyList[2]));
                    result.Add(makeString(keyList[2], keyList[1]));
                } else {
                    for (int i = keyList.Count - 1; i >= 0; --i) {
                        var subList = keyList.Take(i).ToList();
                        if (i < keyList.Count - 1) subList.AddRange(keyList.Skip(i + 1));
                        result.AddRange(MakeSubKeys(subList));
                    }
                }
            }
            return result;
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

        private static char makeChar(int ch)
        {
            return (char)(ch + 0x20);
        }
    }
}

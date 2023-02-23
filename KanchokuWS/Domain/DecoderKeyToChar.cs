﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace KanchokuWS
{
    /// <summary>
    /// DecKey から文字への変換を行うクラス
    /// </summary>
    public static class DecoderKeyToChar
    {
        private static Logger logger = Logger.GetLogger();

        private static List<char> normalChars = null;
        private static List<char> shiftedChars = null;

        /// <summary>
        /// キー・文字マップファイル(chars.106.txtとか)を読み込んで、DecKeyから文字への配列を作成する<br/>
        /// </summary>
        public static bool ReadCharsDefFile()
        {
            logger.InfoH("ENTER");

            normalChars = null;
            shiftedChars = null;

            var filename = Settings.GetString("charsDefFile");
            if (filename._notEmpty()) {
                var filePath = KanchokuIni.Singleton.KanchokuDir._joinPath(filename);
                logger.Info($"charsDefFile path={filePath}");

                var allLines = Helper.GetFileContent(filePath, Encoding.UTF8);
                if (allLines == null) {
                    logger.Error($"Can't read charsDefFile: {filePath}");
                    SystemHelper.ShowErrorMessageBox($"キー・文字マップファイル({filePath}の読み込みに失敗しました。");
                    return false;
                }

                normalChars = new List<char>();
                shiftedChars = new List<char>();
                List<char> charList = null;
                foreach (var line in allLines._split('\n')) {
                    if (line.StartsWith("## ")) {
                        if (line.Contains("NORMAL")) {
                            charList = normalChars;
                        } else if (line.Contains("SHIFT")) {
                            charList = shiftedChars;
                        } else if (line.Contains("END")) {
                            charList = null;
                        }
                    } else if (charList != null) {
                        logger.InfoH($"line=|{line}|, len={line.Length}");
                        foreach (var ch in line) {
                            if (ch >= 0x20 && ch < 0x7f) charList.Add(ch);
                        }
                        logger.InfoH($"charList=|{charList.ToArray()._toString()}|, len={charList.Count}");
                    }
                }
            }

            logger.InfoH("LEAVE");
            return true;

        }

        /// <summary>
        /// DecKey からそれに対応する文字を取得する
        /// </summary>
        /// <param name="deckey"></param>
        /// <param name="bShift"></param>
        /// <returns></returns>
        public static char GetCharFromDecKey(int deckey)
        {
            if (deckey >= 0 && deckey < DecoderKeys.NORMAL_DECKEY_NUM) {
                return normalChars._getNth(deckey, '\0');
            } else if (deckey >= DecoderKeys.SHIFT_DECKEY_START && deckey < DecoderKeys.SHIFT_DECKEY_START + DecoderKeys.NORMAL_DECKEY_NUM) {
                return shiftedChars._getNth(deckey - DecoderKeys.SHIFT_DECKEY_START, '\0');
            } else {
                return '\0';
            }
        }
    }
}

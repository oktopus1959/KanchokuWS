using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace KanchokuWS
{
    public static class DecoderKeyToChar
    {
        private static Logger logger = Logger.GetLogger();

        private static char[] decKeyShiftChars = new char[DecoderKeys.SHIFT_DECKEY_NUM];

        public static bool ReadCharsDefFile(string filename)
        {
            logger.Info("ENTER");
            if (filename._notEmpty()) {
                var filePath = KanchokuIni.Singleton.KanchokuDir._joinPath(filename);
                logger.Info($"charsDef file path={filePath}");
                var lines = Helper.GetFileContent(filePath, Encoding.UTF8);
                if (lines == null) {
                    logger.Error($"Can't read charsDef file: {filePath}");
                    SystemHelper.ShowErrorMessageBox($"文字定義ファイル({filePath}の読み込みに失敗しました。");
                    return false;
                }
                storeLines(lines);
            }
            logger.Info("LEAVE");
            return true;

        }

        private static void storeLines(string lines)
        {
            int pos = 0;
            bool shiftStart = false;
            foreach (var line in lines._split('\n')) {
                if (shiftStart) {
                    if (line._startsWith("## END")) return;
                    foreach (char ch in line) {
                        if (ch >= ' ') {
                            decKeyShiftChars[pos++] = ch;
                            if (pos >= decKeyShiftChars.Length) return;
                        }
                    }
                } else if (line._startsWith("## SHIFT")) {
                    shiftStart = true;
                }
            }
        }

        //public static char GetCharFromDecoderKey(int deckey)
        //{
        //    return deckey >= DecoderKeys.SHIFT_DECKEY_START && deckey < DecoderKeys.SHIFT_DECKEY_END ? decKeyShiftChars[deckey - DecoderKeys.SHIFT_DECKEY_NUM] : '\0';
        //}
    }
}

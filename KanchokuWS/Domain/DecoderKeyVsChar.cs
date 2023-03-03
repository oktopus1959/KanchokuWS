using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace KanchokuWS.Domain
{
    /// <summary>
    /// DecKey から文字への変換を行うクラス
    /// </summary>
    public static class DecoderKeyVsChar
    {
        private static Logger logger = Logger.GetLogger();

        private static char[] QwertyCharsJP = {
            '1', '2', '3', '4', '5', '6', '7', '8', '9', '0',
            'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p',
            'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', ';',
            'z', 'x', 'c', 'v', 'b', 'n', 'm', ',', '.', '/',
            ' ', '-', '^', '\\', '@', '[', ':', ']', '\\', '\0'
        };

        private static char[] QwertyShiftedCharsJP = {
            '!', '"', '#', '$', '%', '&', '\'', '(', ')', '\0',
            'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P',
            'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L', '+',
            'Z', 'X', 'C', 'V', 'B', 'N', 'M', '<', '>', '?',
            ' ', '=', '~', '|', '`', '{', '*', '}', '_', '\0'
        };

        private static char[] QwertyCharsUS = {
            '1', '2', '3', '4', '5', '6', '7', '8', '9', '0',
            'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p',
            'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', ';',
            'z', 'x', 'c', 'v', 'b', 'n', 'm', ',', '.', '/',
            ' ', '-', '=', '\\', '[', ']', '\'', '`', '\0', '\0'
        };

        private static char[] QwertyShiftedCharsUS = {
            '!', '@', '#', '$', '%', '^', '&', '*', '(', ')',
            'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P',
            'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L', ':',
            'Z', 'X', 'C', 'V', 'B', 'N', 'M', '<', '>', '?',
            ' ', '_', '+', '|', '{', '}', '"', '~', '\0', '\0'
        };

        private static char[] FuncChars = {
            '脱', '全', 'Ｔ', '大', '英', '無', '変', 'カ', '削', '入'
        };

        private static char[] QwertyChars() {
            return (DecoderKeyVsVKey.IsJPmode ? QwertyCharsJP : QwertyCharsUS);
        }

        private static char[] QwertyShiftedChars() {
            return (DecoderKeyVsVKey.IsJPmode ? QwertyShiftedCharsJP : QwertyShiftedCharsUS);
        }

        private static List<char> normalChars = null;
        private static List<string> normalKeyNames = null;
        private static List<char> shiftedChars = null;

        public static List<string> NormalKeyNames => normalKeyNames;

        /// <summary>
        /// キー・文字マップファイル(chars.106.txtとか)を読み込んで、DecKeyから文字への配列を作成する<br/>
        /// </summary>
        public static bool ReadCharsDefFile()
        {
            logger.InfoH("ENTER");

            Settings.ShortcutKeyConversionEnabled = true;
            normalChars = null;
            shiftedChars = null;
            int yenPos = 43;

            var filename = Settings.GetString("charsDefFile");
            if (filename._notEmpty()) {
                var filePath = KanchokuIni.Singleton.KanchokuDir._joinPath(Settings.KeyboardFileDir, filename);
                logger.InfoH($"charsDefFile path={filePath}");

                var allLines = Helper.GetFileContent(filePath, Encoding.UTF8);
                if (allLines == null) {
                    logger.Error($"Can't read charsDefFile: {filePath}");
                    SystemHelper.ShowErrorMessageBox($"キー・文字マップファイル({filePath})の読み込みに失敗しました。");
                    return false;
                }

                normalChars = new List<char>();
                shiftedChars = new List<char>();
                List<char> charList = null;
                foreach (var line in allLines._split('\n')) {
                    if (line.StartsWith("## ")) {
                        if (line.StartsWith("## NORMAL")) {
                            charList = normalChars;
                        } else if (line.StartsWith("## SHIFT")) {
                            charList = shiftedChars;
                        } else if (line.StartsWith("## END")) {
                            charList = null;
                        } else if (line._startsWith("## YEN=")) {
                            yenPos = line._safeSubstring(7)._parseInt(-1);
                        } else if (line.StartsWith("## SHORTCUT=disabl")) {
                            Settings.ShortcutKeyConversionEnabled = false;
                            logger.InfoH("ShortcutKeyConversion: Disabled");
                        }
                    } else {
                        if (charList != null) {
                            logger.InfoH($"line=|{line}|, len={line.Length}");
                            foreach (var ch in line) {
                                if (ch >= 0x20 && ch < 0x7f) charList.Add(ch);
                            }
                            logger.InfoH($"charList=|{charList.ToArray()._toString()}|, len={charList.Count}");
                        }
                    }
                }
                if (shiftedChars._isEmpty()) {
                    var normalQwerty = QwertyChars();
                    var shiftedQwerty = QwertyShiftedChars();
                    for (int i = 0; i < normalChars.Count; ++i) {
                        char ch = normalChars[i];
                        char sc = '\0';
                        if (ch == '\\') {
                            sc = i == yenPos ? '|' : '_';
                        } else {
                            for (int j = 0; j < normalQwerty.Length; ++j) {
                                if (ch == normalQwerty[j]) {
                                    sc = shiftedQwerty[j];
                                    break;
                                }
                            }
                        }
                        shiftedChars.Add(sc);
                    }
                }
            }

            normalKeyNames = new List<string>();
            string nameSpace = "Space";
            foreach (char ch in normalChars != null ? normalChars.ToArray() : QwertyChars()) {
                if (ch == ' ') {
                    normalKeyNames.Add(nameSpace);
                    nameSpace = "N/A";  // 2つ目以降のSpaceはN/Aとする
                } else if (ch == '\\') {
                    normalKeyNames.Add("＼");
                } else {
                    normalKeyNames.Add(ch.ToString()._toUpper());
                }
            }
            if (yenPos >= 0 && yenPos < normalKeyNames.Count) {
                normalKeyNames[yenPos] = "￥";
            }

            logger.InfoH("LEAVE");
            return true;

        }

        /// <summary>
        /// DecKey からそれに対応する配列変換された文字を取得する<br/>
        /// デフォルトのJP/US配列の場合は 0 を返す
        /// </summary>
        /// <param name="deckey"></param>
        /// <param name="bShift"></param>
        /// <returns></returns>
        public static char GetArrangedCharFromDecKey(int deckey)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"ENTER: deckey={deckey}");

            char result = '\0';
            if (deckey >= 0 && deckey < DecoderKeys.NORMAL_DECKEY_NUM) {
                result = normalChars._getNth(deckey, '\0');
            } else if (deckey >= DecoderKeys.FUNC_DECKEY_START && deckey < DecoderKeys.FUNC_DECKEY_END) {
                result = FuncChars._getNth(deckey - DecoderKeys.FUNC_DECKEY_START, '\0');
            } else if (deckey >= DecoderKeys.SHIFT_DECKEY_START && deckey < DecoderKeys.SHIFT_DECKEY_START + DecoderKeys.NORMAL_DECKEY_NUM) {
                result = shiftedChars._getNth(deckey - DecoderKeys.SHIFT_DECKEY_START, '\0');
            }

            if (Settings.LoggingDecKeyInfo) logger.InfoH($"LEAVE: result={result}");
            return result;
        }

        /// <summary>
        /// DECKEY から文字コードを得る。デフォルトのJP/US配列の場合でも文字が返る。打鍵ログ出力で使用される
        /// </summary>
        public static char GetArrangedFaceCharFromDecKey(int decKey)
        {
            //return GetArrangedCharFromDecKey(decKey)._gtZeroOr(CharVsVKey.GetFaceCharFromVKey(VKeyComboRepository.GetVKeyComboFromDecKey(decKey)?.vkey ?? 0));
            var ch = GetArrangedCharFromDecKey(decKey);
            if (ch == '\0') {
                if (decKey >= 0 && decKey < DecoderKeys.NORMAL_DECKEY_NUM) {
                    ch = QwertyChars()._getNth(decKey);
                } else {
                    decKey -= DecoderKeys.SHIFT_DECKEY_START;
                    if (decKey >= 0 && decKey < DecoderKeys.NORMAL_DECKEY_NUM) {
                        ch = QwertyShiftedChars()._getNth(decKey);
                    }
                }
            }
            return ch;
        }

        /// <summary>
        /// 文字コードから、それに対応する生のDecKeyを得る<br/>対応するものがなければ -1 を返す
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        public static int GetRawDecKeyFromFaceChar(char face)
        {
            // dvorak など、生であっても noramlChars が notEmpty() なものがあるので、下のような回り道が必要
            return DecoderKeyVsVKey.GetDecKeyFromVKey(CharVsVKey.GetVKeyFromFaceChar(face));
        }

        /// <summary>
        /// 文字コードから、それに対応する、配列変換されたDecKeyを得る<br/>対応するものがなければ -1 を返す
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        public static int GetArrangedDecKeyFromFaceChar(char face)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"ENTER: face={face}, ShortcutKeyConversionEnabled={Settings.ShortcutKeyConversionEnabled}, normalChars={normalChars._notEmpty()}");

            if (face == '\0') {
                if (Settings.LoggingDecKeyInfo) logger.InfoH($"LEAVE: error result=-1");
                return -1;
            }

            if (Settings.ShortcutKeyConversionEnabled && normalChars._notEmpty()) {
                if (Settings.LoggingDecKeyInfo) logger.InfoH($"LEAVE: conv result={normalChars.FindIndex(ch => ch == face)}");
                return normalChars.FindIndex(ch => ch == face);
            } else {
                if (Settings.LoggingDecKeyInfo) logger.InfoH($"LEAVE: raw result={QwertyChars()._findIndex(face)}");
                return QwertyChars()._findIndex(face);
            }
        }

        /// <summary>
        /// キー名から、それに対応する、配列変換されたDecKeyを得る<br/>対応するものがなければ -1 を返す
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        public static int GetArrangedDecKeyFromFaceStr(string keyFace)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH($"ENTER: keyFace={keyFace}, ShortcutKeyConversionEnabled={Settings.ShortcutKeyConversionEnabled}, normalChars={normalChars._notEmpty()}");

            int deckey = GetArrangedDecKeyFromFaceChar(CharVsVKey.GetCharFromFaceStr(keyFace));

            if (Settings.LoggingDecKeyInfo) logger.InfoH($"LEAVE: deckey={deckey}");
            return deckey;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Utils;

namespace KanchokuWS.Domain
{
    static class StrokeVKeys
    {
        private static Logger logger = Logger.GetLogger();

        /// <summary> 打鍵で使われる仮想キー配列(DecKeyId順に並んでいる) </summary>
        private static uint[] strokeVKeys;

        private static uint[] VKeyArrayJP = new uint[] {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
            0x51, 0x57, 0x45, 0x52, 0x54, 0x59, 0x55, 0x49, 0x4f, 0x50,
            0x41, 0x53, 0x44, 0x46, 0x47, 0x48, 0x4a, 0x4b, 0x4c, 0xbb,
            0x5a, 0x58, 0x43, 0x56, 0x42, 0x4e, 0x4d, 0xbc, 0xbe, 0xbf,
            0x20, 0xbd, 0xde, 0xdc, 0xc0, 0xdb, 0xba, 0xdd, 0xe2, 0x00,
        };

        private static uint[] VKeyArrayUS = new uint[] {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
            0x51, 0x57, 0x45, 0x52, 0x54, 0x59, 0x55, 0x49, 0x4f, 0x50,
            0x41, 0x53, 0x44, 0x46, 0x47, 0x48, 0x4a, 0x4b, 0x4c, 0xba,
            0x5a, 0x58, 0x43, 0x56, 0x42, 0x4e, 0x4d, 0xbc, 0xbe, 0xbf,
            0x20, 0xbd, 0xbb, 0xdc, 0xdb, 0xdd, 0xde, 0xc0, 0xc1, 0x00,
        };

        private const uint capsVkeyWithShift = 0x14;    // 日本語キーボードだと Shift + 0x14 で CapsLock になる

        public static bool IsJPmode { get; private set; } = true;

        public static uint getVKeyFromDecKey(int deckey)
        {
            if (deckey < 0) return 0;

            bool bFunc = false;
            if (deckey >= DecoderKeys.SHIFT_DECKEY_START && deckey < DecoderKeys.SHIFT_DECKEY_END) {
                // SHIFT修飾DECKEY
                deckey -= DecoderKeys.SHIFT_DECKEY_START;
            }  else if (deckey >= DecoderKeys.FUNC_DECKEY_START && deckey < DecoderKeys.FUNC_DECKEY_END) {
                // 機能DECKEY
                deckey -= DecoderKeys.FUNC_DECKEY_START;
                bFunc = true;
            }  else if (deckey >= DecoderKeys.CTRL_DECKEY_START && deckey < DecoderKeys.CTRL_DECKEY_END) {
                // Ctrl修飾DECKEY
                deckey -= DecoderKeys.CTRL_DECKEY_START;
            }  else if (deckey >= DecoderKeys.CTRL_FUNC_DECKEY_START && deckey < DecoderKeys.CTRL_FUNC_DECKEY_END) {
                // Ctrl修飾機能DECKEY
                deckey -= DecoderKeys.CTRL_FUNC_DECKEY_START;
                bFunc = true;
            }  else if (deckey >= DecoderKeys.CTRL_SHIFT_DECKEY_START && deckey < DecoderKeys.CTRL_SHIFT_DECKEY_END) {
                // Ctrl+Shift修飾DECKEY
                deckey -= DecoderKeys.CTRL_SHIFT_DECKEY_START;
            }  else if (deckey >= DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_START && deckey < DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_END) {
                // Ctrl+Shift修飾機能DECKEY
                deckey -= DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_START;
                bFunc = true;
            }
            return bFunc ? VKeyArrayFuncKeys.getVKey(deckey) : strokeVKeys._getNth(deckey);
        }

        /// <summary>
        /// 仮想キーコードからなるキーボードファイル(106.keyとか)を読み込んで、仮想キーコードの配列を作成する<br/>
        /// 仮想キーコードはDecKeyId順に並んでいる必要がある。<br/>
        /// NAME=xx の形式で、機能キーの仮想キーコード定義を記述できる
        /// </summary>
        /// <returns></returns>
        public static bool ReadKeyboardFile()
        {
            logger.InfoH("ENTER");

            var kbName = Settings.GetString("keyboard", "JP");
            if (kbName._isEmpty() || kbName._toUpper() == "JP") {
                strokeVKeys = VKeyArrayJP;
            } else if (kbName._toUpper() == "US") {
                strokeVKeys = VKeyArrayUS;
                IsJPmode = false;
            } else {
                var filePath = KanchokuIni.Singleton.KanchokuDir._joinPath(Settings.KeyboardFileDir, kbName);
                logger.Info($"keyboard file path={filePath}");
                var allLines = Helper.GetFileContent(filePath, Encoding.UTF8);
                if (allLines == null) {
                    logger.Error($"Can't read keyboard file: {filePath}");
                    SystemHelper.ShowErrorMessageBox($"キーボード定義ファイル({filePath}の読み込みに失敗しました。");
                    return false;
                }
                var lines = allLines._split('\n').Select(line => line.Trim().Replace(" ", "")).Where(line => line._notEmpty() && line[0] != '#' && line[0] != ';').ToArray();

                List<uint> list = null;

                // MODE=JP or MODE=US
                if (lines._notEmpty()) {
                    var items = lines._getFirst()._toUpper()._split('=');
                    if (items._safeLength() == 2 && items[0] == "MODE") {
                        if (items[1] == "JP") {
                            list = VKeyArrayJP.ToList();
                        } else if (items[1] == "US") {
                            list = VKeyArrayUS.ToList();
                            IsJPmode = false;
                        }
                    }
                }
                // ストロークキーの仮想キーコードを得る
                var hexes = lines.Where(line => line.IndexOf('=') < 0)._join("").TrimEnd(',')._split(',').ToArray();
                if (hexes._notEmpty()) {
                    list = hexes.Select(x => (uint)x._parseHex(0)).ToList();
                    int idx = list.FindIndex(x => x < 0 || x >= 0x100);
                    if (idx >= 0 && idx < list.Count) {
                        logger.Warn($"Invalid keyboard def: file={filePath}, {idx}th: {hexes[idx]}");
                        SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}の{idx}番目のキー定義({hexes[idx]})が誤っています。");
                        return false;
                    }
                    //if (list.Count < DecoderKeys.NORMAL_DECKEY_NUM) {
                    //    logger.Warn($"No sufficient keyboard def: file={filePath}, total {list.Length} defs");
                    //    SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}のキー定義の数({list.Length})が不足しています。");
                    //    return false;
                    //}
                    for (int i = list.Count; i < DecoderKeys.NORMAL_DECKEY_NUM; ++i) list.Add(0);
                }

                if (list._isEmpty()) list = VKeyArrayJP.ToList();

                // NAME=xx の形式で、機能キー(Esc, BS, Enter, 矢印キーなど)の仮想キーコード定義を得る
                foreach (var line in lines) {
                    var items = line._toLower()._split('=');
                    if (items._safeLength() == 2 && items[0]._notEmpty() && items[1]._notEmpty()) {
                        int n = -1;
                        int vk = items[1]._parseHex();
                        if (vk >= 0 && vk < 0x100) {
                            n = VKeyArrayFuncKeys.GetFuncKeyIndexByName(items[0]);
                        }
                        if (!VKeyArrayFuncKeys.setVKey(n, (uint)vk)) {
                            logger.Warn($"Invalid functional key def: file={filePath}, line: {line}");
                            SystemHelper.ShowWarningMessageBox($"キーボード定義ファイル({filePath}の行 {line} が誤っています。");
                            return false;
                        }
                    }
                }
                strokeVKeys = list.ToArray();
            }
            logger.Info(() => $"keyboard keyNum={strokeVKeys.Length}, array={strokeVKeys.Select(x => x.ToString("x"))._join(", ")}");

            setupDecKeyAndComboTable();

            logger.InfoH("LEAVE");
            return true;
        }

        private static void setupDecKeyAndComboTable()
        {
            logger.InfoH($"ENTER");
            // 通常文字ストロークキー
            for (int id = 0; id < DecoderKeys.NORMAL_DECKEY_NUM; ++id) {
                uint vkey = getVKeyFromDecKey(id);
                if (vkey > 0) {
                    // Normal
                    VirtualKeys.AddDecKeyAndCombo(id, 0, vkey);
                    // Shifted
                    VirtualKeys.AddDecKeyAndCombo(DecoderKeys.SHIFT_DECKEY_START + id, KeyModifiers.MOD_SHIFT, vkey);
                    // Ctrl
                    //AddDecKeyAndCombo(DecoderKeys.CTRL_DECKEY_START + id, KeyModifiers.MOD_CONTROL, vkey);
                    // Ctrl+Shift
                    //AddDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_DECKEY_START + id, KeyModifiers.MOD_CONTROL + KeyModifiers.MOD_SHIFT, vkey);
                }
            }

            // 機能キー(RSHFTも登録される)
            for (int id = 0; id < DecoderKeys.FUNC_DECKEY_NUM; ++id) {
                uint vkey = getVKeyFromDecKey(DecoderKeys.FUNC_DECKEY_START + id);
                if (vkey > 0) {
                    // Normal
                    VirtualKeys.AddDecKeyAndCombo(DecoderKeys.FUNC_DECKEY_START + id, 0, vkey);
                    // Shift
                    if (vkey == capsVkeyWithShift) VirtualKeys.AddDecKeyAndCombo(DecoderKeys.FUNC_DECKEY_START + id, KeyModifiers.MOD_SHIFT, vkey);
                    // Ctrl
                    //AddDecKeyAndCombo(DecoderKeys.CTRL_FUNC_DECKEY_START + id, KeyModifiers.MOD_CONTROL, vkey);
                    // Ctrl+Shifted
                    //AddDecKeyAndCombo(DecoderKeys.CTRL_SHIFT_FUNC_DECKEY_START + id, KeyModifiers.MOD_CONTROL + KeyModifiers.MOD_SHIFT, vkey);
                }
            }

            // Shift+Tab
            VirtualKeys.AddDecKeyAndCombo(DecoderKeys.SHIFT_TAB_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.Tab);
            //AddModConvertedDecKeyFromCombo(DecoderKeys.SHIFT_TAB_DECKEY, KeyModifiers.MOD_SHIFT, (uint)Keys.Tab);
            logger.InfoH($"LEAVE");
        }

    }

}

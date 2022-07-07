using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Utils;

namespace KanchokuWS.Gui
{
    class TestRunner
    {
        private static Logger logger = Logger.GetLogger(true);

        FrmKanchoku frmMain = null;

        public TestRunner(FrmKanchoku frm)
        {
            frmMain = frm;
        }

        public void RunTest(bool bAll)
        {
            if (frmMain == null) return;

            var testFilename = $@"test_script{(bAll ? "_all" : "")}.txt";
            var testFilePath = Helper.JoinPath("src", testFilename);
            var lines = readAllLines(testFilePath);
            if (lines._isEmpty()) {
                testFilePath = Helper.JoinPath("bin", testFilename);
                lines = readAllLines(testFilePath);
                if (lines._isEmpty()) {
                    SystemHelper.ShowWarningMessageBox($"ファイルが見つかりません: {testFilePath}");
                    return;
                }
            }

            Settings.CombinationKeyMinTimeOnlyAfterSecond = false;
            Settings.UseCombinationKeyTimer1 = false;
            Settings.UseCombinationKeyTimer2 = false;

            var regex = new Regex(@"^\s*(\w+)(?:\(([^)]*)\)(?:\s*=\s*([^\s]+))?)?");
            var sb = new StringBuilder();

            int lineNum = 0;
            int numErrors = 0;
            bool bBreak = false;
            foreach (var line in lines) {
                if (numErrors >= 10) break;

                ++lineNum;
                void appendError(string msg)
                {
                    logger.Warn(msg);
                    sb.Append(msg).Append($":\n>> ").Append(lineNum).Append(": ").Append(line).Append("\n\n");
                }

                var trimmedLine = line.Trim();
                if (trimmedLine._isEmpty() || trimmedLine._startsWith("#") || trimmedLine._startsWith(";")) continue;

                var items = trimmedLine._reScan(regex);
                if (items._safeCount() < 2) {
                    appendError($"Illegal format");
                    ++numErrors;
                    continue;
                }

                var command = items[1];
                var arg = items._getNth(2);
                var expected = items._getNth(3);
                logger.WriteInfo($"\n==== TEST({lineNum}): command={command}, arg={arg}, expected={(expected._notEmpty() ? expected : "null")} ====");
                switch (command) {
                    case "logLevel":
                        Logger.LogLevel = arg._parseInt(Logger.LogLevelWarn);
                        break;

                    case "loadTable":
                        //Settings.TableFile2 = arg;
                        CombinationKeyStroke.Determiner.Singleton.Initialize(Settings.TableFile, arg);
                        frmMain.ExecCmdDecoder("createStrokeTrees", "both"); // ストローク木の再構築
                        frmMain.ExecCmdDecoder("useCodeTable2", null);
                        callDecoderWithKey(DecoderKeys.FULL_ESCAPE_DECKEY);
                        CombinationKeyStroke.Determiner.Singleton.UseSecondaryPool();
                        Settings.CombinationKeyMinTimeOnlyAfterSecond = false;
                        Settings.UseCombinationKeyTimer1 = false;
                        Settings.UseCombinationKeyTimer2 = false;
                        Settings.CombinationKeyMaxAllowedLeadTimeMs = arg._parseInt(100);
                        Settings.CombinationKeyMinOverlappingTimeMs = arg._parseInt(70);
                        if (Logger.LogLevel >= Logger.LogLevelInfoH) {
                            KanchokuWS.CombinationKeyStroke.DeterminerLib.KeyCombinationPool.Singleton2?.DebugPrint(true);
                        }
                        break;

                    case "test":
                    case "convert":
                        if (expected == null || expected._equalsTo("\"\"") || expected._equalsTo("''")) expected = "";
                        var result = convertKeySequence(arg, bAll);
                        if (result != expected) {
                            appendError($"Expected={expected}, but Result={result}");
                            ++numErrors;
                        }
                        break;

                    case "clear":
                        callDecoderWithKey(DecoderKeys.FULL_ESCAPE_DECKEY);
                        break;

                    case "comboMinTime":
                        Settings.CombinationKeyMinOverlappingTimeMs = arg._parseInt(70);
                        break;

                    case "comboMaxTime":
                        Settings.CombinationKeyMaxAllowedLeadTimeMs = arg._parseInt(70);
                        break;

                    case "rewriteMaxTime":
                        Settings.PreRewriteAllowedDelayTimeMs = arg._parseInt(200);
                        break;

                    case "enableFirstComboCheck":
                        Settings.CombinationKeyMinTimeOnlyAfterSecond = false;
                        break;

                    case "disableFirstComboCheck":
                        Settings.CombinationKeyMinTimeOnlyAfterSecond = true;
                        break;

                    case "enableTimer1":
                        Settings.UseCombinationKeyTimer1 = true;
                        break;

                    case "disableTimer1":
                        Settings.UseCombinationKeyTimer1 = false;
                        break;

                    case "enableTimer2":
                        Settings.UseCombinationKeyTimer2 = true;
                        break;

                    case "disableTimer2":
                        Settings.UseCombinationKeyTimer2 = false;
                        break;

                    case "break":
                    case "exit":
                    case "quit":
                        bBreak = true;
                        logger.WriteInfo("\nBREAK");
                        break;

                    default:
                        appendError($"Illegal command={command}");
                        ++numErrors;
                        break;
                }
                if (bBreak) break;
            }

            logger.WriteInfo("DONE");

            frmMain.ReloadSettingsAndDefFiles();

            if (numErrors == 0) {
                SystemHelper.ShowInfoMessageBox("All tests passed!");
            } else {
                SystemHelper.ShowWarningMessageBox(sb.ToString());
            }
        }

        void callDecoderWithKey(int dk, StringBuilder sb = null)
        {
            int numBS = 0;
            var result = frmMain.CallDecoderWithKey(dk, 0, out numBS);
            if (sb != null) {
                sb.Length = (sb.Length - numBS)._lowLimit(0);
                sb.Append(result);
            }
        }

        static Dictionary<char, int> keyToDeckey = new Dictionary<char, int>() {
            {'1', 0 }, {'2', 1 }, {'3', 2 }, {'4', 3 }, {'5', 4 }, {'6', 5 }, {'7', 6 }, {'8', 7 }, {'9', 8 }, {'0', 9 }, 
            {'Q', 10 }, {'W', 11 }, {'E', 12 }, {'R', 13 }, {'T', 14 }, {'Y', 15 }, {'U', 16 }, {'I', 17 }, {'O', 18 }, {'P', 19 }, 
            {'A', 20 }, {'S', 21 }, {'D', 22 }, {'F', 23 }, {'G', 24 }, {'H', 25 }, {'J', 26 }, {'K', 27 }, {'L', 28 }, {';', 29 }, 
            {'Z', 30 }, {'X', 31 }, {'C', 32 }, {'V', 33 }, {'B', 34 }, {'N', 35 }, {'M', 36 }, {',', 37 }, {'.', 38 }, {'/', 39 }, 
            {' ', 40 }, {'-', 41 }, {'^', 42 }, {'\\', 43 }, {'@', 44 }, {'[', 45 }, {':', 46 }, {']', 47 }, 
        };

        string convertKeySequence(string keys, bool bAll)
        {
            int prevLogLevel = Logger.LogLevel;
            if (bAll) {
                Logger.LogLevel = Logger.LogLevelWarn;
            } else if (prevLogLevel < Logger.LogLevelInfoH) {
                Logger.LogLevel = Logger.LogLevelInfoH;
            }

            var sb = new StringBuilder();
            void callDecoder(List<int> list)
            {
                if (list._notEmpty()) {
                    foreach (var dk in list) {
                        callDecoderWithKey(dk, sb);
                    }
                }
            }

            char toUpper(char c) { return (char)(c - 0x20); }

            int keysLen = keys._safeLength();

            int getDecKeyOrInt(ref int pos, char ch, bool bOnlyInt = false)
            {
                int start = ++pos;
                while (pos < keysLen) {
                    if (keys[pos] == ch) break;
                    ++pos;
                }
                if (pos > start) {
                    var s = keys._safeSubstring(start, pos - start);
                    if (!bOnlyInt) {
                        int dk = -1;
                        switch (s) {
                            case ";": dk = 29; break;
                            case ",": dk = 37; break;
                            case ".": dk = 38; break;
                            case "/": dk = 39; break;
                            case "nfer": dk = 55; break;
                            case "xfer": dk = 56; break;
                        }
                        if (dk >= 0) return dk;
                    }
                    return s._parseInt(0);
                }
                return 0;
            }

            var oldFunc = CombinationKeyStroke.Determiner.Singleton.KeyProcHandler;
            CombinationKeyStroke.Determiner.Singleton.KeyProcHandler = callDecoder;
            try {
                for (int pos = 0; pos < keysLen; ++pos) {
                    char k = keys[pos];
                    if (k >= '0' && k <= '9' || k >= 'A' && k <= 'Z') {
                        CombinationKeyStroke.Determiner.Singleton.KeyDown(keyToDeckey._safeGet(k), null);
                    } else if (k >= 'a' && k <= 'z') {
                        CombinationKeyStroke.Determiner.Singleton.KeyUp(keyToDeckey._safeGet(toUpper(k)));
                    } else if (k == '<') {
                        int ms = getDecKeyOrInt(ref pos, '>', true);
                        if (ms > 0) Helper.WaitMilliSeconds(ms);
                    } else if (k == '{') {
                        int dk = getDecKeyOrInt(ref pos, '}');
                        CombinationKeyStroke.Determiner.Singleton.KeyDown(dk, null);
                    } else if (k == '[') {
                        int dk = getDecKeyOrInt(ref pos, ']');
                        CombinationKeyStroke.Determiner.Singleton.KeyUp(dk);
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex._getErrorMsg());
            } finally {
                CombinationKeyStroke.Determiner.Singleton.KeyProcHandler = oldFunc;
            }

            Logger.LogLevel = prevLogLevel;
            return sb.ToString();
        }

        List<string> readAllLines(string filepath)
        {
            var lines = new List<string>();
            if (filepath._notEmpty()) {
                var absPath = KanchokuIni.Singleton.KanchokuDir._joinPath(filepath);
                logger.WriteInfo($"ENTER: absFilePath={absPath}");
                var contents = Helper.GetFileContent(absPath, (e) => logger.Error(e._getErrorMsgShort()));
                if (contents._notEmpty()) {
                    lines.AddRange(contents._safeReplace("\r", "")._split('\n'));
                }
            }
            logger.WriteInfo($"LEAVE: num of lines={lines.Count}");
            return lines;
        }

    }
}

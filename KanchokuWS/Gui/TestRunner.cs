﻿using System;
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

            var lines = readAllLines($@"src\test_script{(bAll ? "_all" : "")}.txt");
            if (lines._isEmpty()) {
                SystemHelper.ShowWarningMessageBox(@"ファイルが見つかりません: bin\test_script.txt");
                return;
            }

            var regex = new Regex(@"^\s*(\w+)\(([^)]+)\)(\s*=\s*([^\s]+))?");
            var sb = new StringBuilder();

            int lineNum = 0;
            int numErrors = 0;
            foreach (var line in lines) {
                if (numErrors >= 10) break;

                ++lineNum;
                void appendError(string msg) { sb.Append(msg).Append($":\n>> ").Append(lineNum).Append(": ").Append(line).Append("\n\n"); }

                var trimmedLine = line.Trim();
                if (trimmedLine._isEmpty() || trimmedLine._startsWith("#") || trimmedLine._startsWith(";")) continue;

                var items = trimmedLine._reScan(regex);
                if (items._safeCount() < 3) {
                    appendError($"Illegal format");
                    ++numErrors;
                    continue;
                }

                var command = items[1];
                var arg = items[2];
                var expected = items._getNth(4);
                logger.WriteInfo($"command={command}, arg={arg}, expected={(expected._notEmpty() ? expected : "null")}");
                switch (command) {
                    case "loadTable":
                        //Settings.TableFile2 = arg;
                        CombinationKeyStroke.Determiner.Singleton.Initialize(Settings.TableFile, arg);
                        frmMain.ExecCmdDecoder("createStrokeTrees", "both"); // ストローク木の再構築
                        frmMain.ExecCmdDecoder("useCodeTable2", null);
                        CombinationKeyStroke.Determiner.Singleton.UseSecondaryPool();
                        break;

                    case "convert":
                        if (expected._isEmpty()) {
                            appendError($"Illegal arguments");
                            ++numErrors;
                        } else {
                            var result = convertKeySequence(arg);
                            if (result != expected) {
                                appendError($"Expected={expected}, but Result={result}");
                                ++numErrors;
                            }
                        }
                        break;

                    case "comboMinTime":
                        Settings.CombinationKeyMinOverlappingTimeMs = arg._parseInt(70);
                        break;

                    case "comboMaxTime":
                        Settings.CombinationKeyMaxAllowedLeadTimeMs = arg._parseInt(70);
                        break;

                    default:
                        appendError($"Illegal command={command}");
                        ++numErrors;
                        break;
                }
            }

            logger.WriteInfo("DONE");

            frmMain.ReloadSettingsAndDefFiles();

            if (numErrors == 0) {
                SystemHelper.ShowInfoMessageBox("All tests passed!");
            } else {
                SystemHelper.ShowWarningMessageBox(sb.ToString());
            }
        }

        static Dictionary<char, int> keyToDeckey = new Dictionary<char, int>() {
            {'1', 0 }, {'2', 1 }, {'3', 2 }, {'4', 3 }, {'5', 4 }, {'6', 5 }, {'7', 6 }, {'8', 7 }, {'9', 8 }, {'0', 9 }, 
            {'Q', 10 }, {'W', 11 }, {'E', 12 }, {'R', 13 }, {'T', 14 }, {'Y', 15 }, {'U', 16 }, {'I', 17 }, {'O', 18 }, {'P', 19 }, 
            {'A', 20 }, {'S', 21 }, {'D', 22 }, {'F', 23 }, {'G', 24 }, {'H', 25 }, {'J', 26 }, {'K', 27 }, {'L', 28 }, {';', 29 }, 
            {'Z', 30 }, {'X', 31 }, {'C', 32 }, {'V', 33 }, {'B', 34 }, {'N', 35 }, {'M', 36 }, {',', 37 }, {'.', 38 }, {'/', 39 }, 
            {' ', 40 }, {'-', 41 }, {'^', 42 }, {'\\', 43 }, {'@', 44 }, {'[', 45 }, {':', 46 }, {']', 47 }, 
        };

        string convertKeySequence(string keys)
        {
            int prevLogLevel = Logger.LogLevel;
            if (prevLogLevel < Logger.LogLevelInfoH) Logger.LogLevel = Logger.LogLevelInfoH;

            var sb = new StringBuilder();
            void callDecoder(List<int> list)
            {
                if (list._notEmpty()) {
                    foreach (var dk in list) {
                        int numBS = 0;
                        var result = frmMain.CallDecoderWithKey(dk, 0, out numBS);
                        sb.Length = (sb.Length - numBS)._lowLimit(0);
                        sb.Append(result);
                    }
                }
            }

            char toUpper(char c) { return (char)(c - 0x20); }

            int keysLen = keys._safeLength();

            int getInt(ref int pos, char ch)
            {
                int start = ++pos;
                while (pos < keysLen) {
                    if (keys[pos] == ch) break;
                    ++pos;
                }
                if (pos > start) return keys._safeSubstring(start, pos - start)._parseInt(0);
                return 0;
            }

            for (int pos = 0; pos < keysLen; ++pos) {
                char k = keys[pos];
                if (k >= 'A' && k <= 'Z') {
                    callDecoder(CombinationKeyStroke.Determiner.Singleton.KeyDown(keyToDeckey._safeGet(k), null));
                } else if (k >= 'a' && k <= 'z') {
                    callDecoder(CombinationKeyStroke.Determiner.Singleton.KeyUp(keyToDeckey._safeGet(toUpper(k))));
                } else if (k == '<') {
                    int ms = getInt(ref pos, '>');
                    if (ms > 0) Helper.WaitMilliSeconds(ms);
                } else if (k == '{') {
                    int dk = getInt(ref pos, '}');
                    callDecoder(CombinationKeyStroke.Determiner.Singleton.KeyDown(dk, null));
                } else if (k == '[') {
                    int dk = getInt(ref pos, ']');
                    callDecoder(CombinationKeyStroke.Determiner.Singleton.KeyUp(dk));
                }
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
                var contents = Helper.GetFileContent(absPath, (e) => logger.Error(e._getErrorMsg()));
                if (contents._notEmpty()) {
                    lines.AddRange(contents._safeReplace("\r", "")._split('\n'));
                }
            }
            logger.WriteInfo($"LEAVE: num of lines={lines.Count}");
            return lines;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KanchokuWS.CombinationKeyStroke.DeterminerLib;
using Utils;

namespace KanchokuWS.TableParser
{
    using ShiftKeyKind = ComboShiftKeyPool.ComboKind;

    /// <summary>
    /// テーブル解析器のコンテキストデータ
    /// </summary>
    class TableParserContext
    {
        private static Logger logger = Logger.GetLogger(true);

        protected ParserContext context;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public TableParserContext(ParserContext ctx)
        {
            context = ctx;
        }

        protected TableLines tableLines => context.tableLines;

        protected TOKEN currentToken {
            get { return context.currentToken;}
            set { context.currentToken = value; }
        }
        protected int arrowIndex {
            get { return context.arrowIndex; }
            set { context.arrowIndex = value;}
        }
        protected bool bPrimary => context.bPrimary;
        protected bool bRewriteEnabled {
            get { return context.bRewriteEnabled; }
            set { context.bRewriteEnabled = value; }
        }
        protected bool isInCombinationBlock => context.isInCombinationBlock;
        protected ShiftKeyKind shiftKeyKind {
            get { return context.shiftKeyKind; }
            set { context.shiftKeyKind = value; }
        }
        protected Dictionary<string, List<string>> linesMap => context.linesMap;
        protected KeyCombinationPool keyComboPool => context.keyComboPool;
        protected List<string> OutputLines => context.OutputLines;
        protected Dictionary<string, int> placeHolders => context.placeHolders;
        protected bool bRewriteTable {
            get { return context.bRewriteTable; }
            set { context.bRewriteTable = value; }
        }

        protected bool bIgnoreWarningAll { get { return context.bIgnoreWarningAll; } set { context.bIgnoreWarningAll = value; } }
        protected bool bIgnoreWarningBraceLevel { get { return context.bIgnoreWarningBraceLevel; } set { context.bIgnoreWarningBraceLevel = value; } }
        protected bool bIgnoreWarningOverwrite { get { return context.bIgnoreWarningOverwrite; } set { context.bIgnoreWarningOverwrite = value; } }
        protected int braceLevel { get { return context.braceLevel; } set { context.braceLevel = value; } }

        protected HashSet<int> sequentialShiftKeys => context.sequentialShiftKeys;

        protected bool Empty => tableLines.Empty;
        protected bool NotEmpty => tableLines.NotEmpty;
        protected string CurrentLine => tableLines.CurrentLine;
        protected int LineNumber => tableLines.LineNumber;
        protected bool IsCurrentPosHeadOfLine => tableLines.IsCurrentPosHeadOfLine;
        protected char CurrentChar => tableLines.CurrentChar;
        protected string CurrentStr => tableLines.CurrentStr;
        protected void ClearCurrentStr() { tableLines.ClearCurrentStr(); }
        protected string RewriteTargetStr { get { return tableLines.RewriteTargetStr; } set { tableLines.RewriteTargetStr = value; } }
        protected void ReadAllLines(string filename) { tableLines.ReadAllLines(filename); }
        protected void IncludeFile() { tableLines.IncludeFile(); }
        protected void EndInclude() { tableLines.EndInclude(); }
        protected void StoreLineBlock() { tableLines.StoreLineBlock(); }
        protected void LoadLineBlock() { tableLines.LoadLineBlock(); }
        protected void ReadString() { tableLines.ReadString(); }
        protected void ReadBareString(char c = '\0') { tableLines.ReadBareString(c); }
        protected void ReadStringUpto(params char[] array) { tableLines.ReadStringUpto(array); }
        protected void ReadMarker() { tableLines.ReadMarker(); }
        protected string ReadWord() { return tableLines.ReadWord(); }
        protected string ReadWordOrString() { return tableLines.ReadWordOrString(); }
        protected char PeekNextChar() { return tableLines.PeekNextChar(); }
        protected char GetNextChar() { return tableLines.GetNextChar(); }
        protected bool GetNextLine() { return tableLines.GetNextLine(); }
        protected void SkipToEndOfLine() { tableLines.SkipToEndOfLine(); }
        protected char SkipSpace() { return tableLines.SkipSpace(); }
        protected void RewindChar() { tableLines.RewindChar(); }
        protected string MakeErrorLines() { return tableLines.MakeErrorLines(); }
        protected void ParseError(string msg = null) { tableLines.ParseError(msg); }
        protected void ArgumentError(string arg) { tableLines.ArgumentError(arg); }
        protected void LoadLoopError(string name) { tableLines.LoadLoopError(name); }
        protected void NoSuchBlockError(string name) { tableLines.NoSuchBlockError(name); }
        protected void FileOpenError(string filename) { tableLines.FileOpenError(filename); }
        protected void NodeDuplicateWarning() { tableLines.NodeDuplicateWarning(); }
        protected void UnexpectedLeftBraceAtColumn0Warning() { tableLines.UnexpectedLeftBraceAtColumn0Warning(); }
        protected void UnexpectedRightBraceAtColumn0Warning() { tableLines.UnexpectedRightBraceAtColumn0Warning(); }
        protected void showErrorMessage() { tableLines.showErrorMessage(); }
        protected void Error(string msg) { tableLines.Error(msg); }
        protected void Warn(string msg) { tableLines.Warn(msg); }

    }

}

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
    /// テーブル解析器のコンテキストデータへのアクセッサ
    /// </summary>
    class ContextAccessor
    {
        private static Logger logger = Logger.GetLogger();

        protected ParserContext Context => ParserContext.Singleton;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pool">対象となる KeyComboPool</param>
        public ContextAccessor(/* ParserContext ctx */)
        {
            //context = ctx;
        }

        protected TableLines tableLines => Context.tableLines;

        protected TOKEN currentToken {
            get { return Context.currentToken;}
            set { Context.currentToken = value; }
        }
        protected int ArrowIndex {
            get { return Context.arrowIndex; }
            set { Context.arrowIndex = value;}
        }
        protected bool bPrimary => Context.bPrimary;
        protected bool bRewriteEnabled {
            get { return Context.bRewriteEnabled; }
            set { Context.bRewriteEnabled = value; }
        }
        protected HashSet<string> definedNames => Context.definedNames;
        protected bool isInCombinationBlock => Context.isInCombinationBlock;
        protected bool isInSuccCombinationBlock => Context.isInSuccCombinationBlock;
        protected ShiftKeyKind shiftKeyKind {
            get { return Context.shiftKeyKind; }
            set { Context.shiftKeyKind = value; }
        }
        //protected bool bComboEffectiveAlways {
        //    get { return Context.bComboEffectiveAlways; }
        //    set { Context.bComboEffectiveAlways = value; }
        //}
        //protected bool bComboEffectiveOnKanchokuMode {
        //    get { return Context.bComboEffectiveOnKanchokuMode; }
        //    set { Context.bComboEffectiveOnKanchokuMode = value; }
        //}
        //protected bool bComboEffectiveOnEisuMode {
        //    get { return Context.bComboEffectiveOnEisuMode; }
        //    set { Context.bComboEffectiveOnEisuMode = value; }
        //}
        protected Dictionary<string, List<string>> linesMap => Context.linesMap;
        protected KeyCombinationPool keyComboPool => Context.keyComboPool;
        protected int comboDeckeyStart => Context.comboDeckeyStart;
        protected List<string> OutputLines => Context.OutputLines;
        protected PlaceHolders placeHolders => Context.placeHolders;

        protected bool bIgnoreWarningAll { get { return Context.bIgnoreWarningAll; } set { Context.bIgnoreWarningAll = value; } }
        protected bool bIgnoreWarningBraceLevel { get { return Context.bIgnoreWarningBraceLevel; } set { Context.bIgnoreWarningBraceLevel = value; } }
        protected bool bIgnoreWarningOverwrite { get { return Context.bIgnoreWarningOverwrite; } set { Context.bIgnoreWarningOverwrite = value; } }
        protected int braceLevel { get { return Context.braceLevel; } set { Context.braceLevel = value; } }

        protected HashSet<int> sequentialShiftKeys => Context.sequentialShiftKeys;

        protected Dictionary<string, string> kanjiConvMap => Context.kanjiConvMap;
        public string ConvertKanji(string k) { return Context.ConvertKanji(k); }

        protected bool IsPrimary => tableLines.IsPrimary;
        protected bool Empty => tableLines.Empty;
        protected bool NotEmpty => tableLines.NotEmpty;
        protected string CurrentLine => tableLines.CurrentLine;
        protected int LineNumber => tableLines.LineNumber;
        protected bool IsCurrentPosHeadOfLine => tableLines.IsCurrentPosHeadOfLine;
        protected char CurrentChar => tableLines.CurrentChar;
        protected string CurrentStr => tableLines.CurrentStr;
        protected void ClearCurrentStr() { tableLines.ClearCurrentStr(); }
        protected string RewritePreTargetStr { get { return tableLines.RewritePreTargetStr; } set { tableLines.RewritePreTargetStr = value; } }
        protected void ReadAllLines(string filename, bool bPrimary, bool bForKanchoku) { tableLines.ReadAllLines(filename, bPrimary, bForKanchoku); }
        protected void IncludeFile() { tableLines.IncludeFile(); }
        protected void EndInclude() { tableLines.EndInclude(); }
        protected void StoreLineBlock() { tableLines.StoreLineBlock(); }
        protected void LoadLineBlock() { tableLines.LoadLineBlock(); }
        protected void RewriteIfdefBlock(bool flag) { tableLines.RewriteIfdefBlock(flag); }
        protected void ReadString() { tableLines.ReadString(); }
        protected void ReadBareString(char c = '\0') { tableLines.ReadBareString(c); }
        protected void ReadStringUpto(bool bInclude, params char[] array) { tableLines.ReadStringUpto(bInclude, array); }
        protected void ReadPlaceHolderName() { tableLines.ReadPlaceHolderName(); }
        protected void ReadMarker() { tableLines.ReadMarker(); }
        protected string ReadWord() { return tableLines.ReadWord(); }
        protected OutputString ReadWordOrString() { return tableLines.ReadWordOrString(); }
        protected char PeekNextChar(int offset = 0) { return tableLines.PeekNextChar(offset); }
        protected char GetNextChar() { return tableLines.GetNextChar(); }
        protected bool GetNextLine() { return tableLines.GetNextLine(); }
        protected void AdvanceCharPos(int offset) { tableLines.AdvanceCharPos(offset); }
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
        //protected void showErrorMessage() { tableLines.showErrorMessage(); }
        //protected void Error(string msg) { tableLines.Error(msg); }
        //protected void Warn(string msg) { tableLines.Warn(msg); }

    }

}

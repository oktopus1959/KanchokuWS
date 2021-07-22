using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace KanchokuWS
{
    public static class Settings
    {
        //-------------------------------------------------------------------------------------
        /// <summary> バージョン </summary>
        public static string Version => "1.0.6";

        /// <summary> ドキュメントへのURL </summary>
        public static string DocumentUrl => "https://github.com/oktopus1959/KanchokuWS#readme";

        //-------------------------------------------------------------------------------------
        /// <summary>Ctrl修飾なしで Decoder をアクティブにするホットキーの仮想キーコード</summary> 
        public static uint ActiveKey { get; private set; } = 0x1c;
        /// <summary>Ctrl修飾ありで Decoder をアクティブにするホットキーの仮想キーコード</summary> 
        public static uint ActiveKeyWithCtrl { get; private set; } = 0x1c;

        /// <summary>Ctrl修飾なしで Decoder を非アクティブにするホットキーの仮想キーコード</summary> 
        public static uint DeactiveKey { get; private set; } = 0;
        /// <summary>Ctrl修飾ありで Decoder を非アクティブにするホットキーの仮想キーコード</summary> 
        public static uint DeactiveKeyWithCtrl { get; private set; } = 0;

        public static uint DeactiveKeyEffective => DeactiveKey != 0 ? DeactiveKey : ActiveKey;
        public static uint DeactiveKeyWithCtrlEffective => DeactiveKeyWithCtrl != 0 ? DeactiveKeyWithCtrl : ActiveKeyWithCtrl;

        //-------------------------------------------------------------------------------------
        /// <summary>HotKeyの無限ループを検出する回数</summary>
        public static int HotkeyInfiniteLoopDetectCount { get; private set; } = 1000;

        /// <summary>キーリピートが発生したことを認識するまでの時間(ミリ秒)</summary>
        public static int KeyRepeatDetectMillisec { get; private set; } = 100;

        ///// <summary>キーリピートなどで短時間に大量のキー入力があったら強制的にデコーダをOFFにする</summary>
        //public static bool AutoOffWhenBurstKeyIn { get; private set; } = false;

        /// <summary>スプラッシュウィンドウの表示時間</summary>
        public static int SplashWindowShowDuration { get; private set; } = 60;

        /// <summary>終了時に確認ダイアログを出す</summary>
        public static bool ConfirmOnClose { get; private set; } = true;

        /// <summary>再起動時に確認ダイアログを出す</summary>
        public static bool ConfirmOnRestart { get; private set; } = true;

        //-------------------------------------------------------------------------------------
        /// <summary>ログレベル</summary> 
        public static int LogLevel { get; private set; } = 0;

        /// <summary>true の場合、入力されたホットキーに関する情報を Warn レベルでログ出力する</summary>
        public static bool LoggingHotKeyInfo { get; private set; }

        /// <summary>true の場合、アクティブウィンドウに関する情報を Warn レベルでログ出力する</summary>
        public static bool LoggingActiveWindowInfo { get; set; } = false;

        /// <summary>ホットキー処理後にウェイトを入れる(開発用; バグ等により処理対象ホットキーを keybd_event で送出することによる無限ループに対応する時間をかせぐ)</summary>
        public static bool DelayAfterProcessHotkey { get; private set; } = false;

        /// <summary>二重起動を許可する</summary>
        public static bool MultiAppEnabled { get; private set; } = false;

        /// <summary>部首合成ログを有効にする</summary>
        public static bool BushuDicLogEnabled { get; private set; }

        public static bool IsAnyDevFlagEnabled => LogLevel > 2 || LoggingHotKeyInfo || LoggingActiveWindowInfo || DelayAfterProcessHotkey || BushuDicLogEnabled;

        //-------------------------------------------------------------------------------------
        // フォントと色
        public static string NormalVkbFontSpec { get; private set; } = "@MS Gothic|9|0|0";
        public static string CenterVkbFontSpec { get; private set; } = "@MS Gothic|9|0|0";
        public static string VerticalVkbFontSpec { get; private set; } = "@MS Gothic|9|0|0";
        public static string HorizontalVkbFontSpec { get; private set; } = "MS Gothic|9";
        public static string MiniBufVkbFontSpec { get; private set; } = "MS Gothic|9";

        //-------------------------------------------------------------------------------------
        /// <summary>キーボードファイル</summary>
        public static string KeyboardFile { get; private set; }

        /// <summary>打鍵ヘルプ定義ファイル</summary>
        public static string StrokeHelpFile { get; private set; }

        // 各種辞書ファイル
        public static string BushuAssocFile { get; private set; }
        public static string BushuFile { get; private set; }
        public static string CharsDefFile { get; private set; }
        public static string EasyCharsFile { get; private set; }
        public static string TableFile { get; private set; }
        public static string HistoryFile { get; private set; }
        //public static string HistoryUsedFile {get; private set; }
        //public static string HistoryExcludeFile {get; private set; }
        //public static string HistoryNgramFile {get; private set; }
        public static string MazegakiFile { get; private set; }

        public static int BackFileRotationGeneration { get; private set; } = 3;

        //-------------------------------------------------------------------------------------
        /// <summary> 文字送出時にコピー&ペーストを行う文字数の閾値 </summary>
        public static int MinLeghthViaClipboard { get; set; } = 5;

        //-------------------------------------------------------------------------------------
        /// <summary>仮想鍵盤の表示モード</summary>
        public enum VkbShowMode
        {
            /// <summary>常にカレットの近くに表示する</summary>
            Always,
            /// <summary>何らかの選択状態にあるときのみ表示する</summary>
            OnSelect,
        }
        /// <summary>N打鍵目に仮想鍵盤を表示する(0なら仮想鍵盤を表示しない)</summary>
        public static int VirtualKeyboardShowStrokeCount { get; set; } = 1;
        public static int VirtualKeyboardShowStrokeCountTemp { get; set; } = 0;
        public static int VirtualKeyboardShowStrokeCountEffective =>
            VirtualKeyboardShowStrokeCount <= 0 || (VirtualKeyboardShowStrokeCountTemp > 0 && VirtualKeyboardShowStrokeCountTemp < VirtualKeyboardShowStrokeCount)
            ? VirtualKeyboardShowStrokeCountTemp
            : VirtualKeyboardShowStrokeCount;

        ///// <summary>上部テキストボックスの表示モード</summary>
        //public enum TopTextboxMode
        //{
        //    /// <summary>常に表示する</summary>
        //    ShowAlways,
        //    /// <summary>何らかの選択状態にあるときは隠す</summary>
        //    HideOnSelect,
        //}
        ///// <summary>上部テキストボックスの表示モード</summary>
        //public static TopTextboxMode TopTextboxShowMode { get; private set; } = TopTextboxMode.ShowAlways;

        ///// <summary>上部テキストボックスの表示モード(一時的な参照用)</summary>
        //public static TopTextboxMode TopTextboxShowModeTemp { get; private set; } = TopTextboxMode.HideOnSelect;

        //-------------------------------------------------------------------------------------
        /// <summary>カレットと仮想鍵盤の相対位置: X方向</summary>
        public static int VirtualKeyboardOffsetX { get; private set; } = 2;

        /// <summary>カレットと仮想鍵盤の相対位置: Y方向</summary>
        public static int VirtualKeyboardOffsetY { get; private set; } = 2;

        ///// <summary>ディスプレイのDPI比(標準96dpiとの比)</summary>
        //public static double DisplayScale { get; private set; } = 1.0;

        /// <summary>仮想鍵盤の最上段セルの背景色</summary>
        public static string BgColorTopLevelCells { get; private set; } = "GhostWhite";
        /// <summary>仮想鍵盤の中央寄りセルの背景色</summary>
        public static string BgColorCenterSideCells { get; private set; } = "FloralWhite";
        /// <summary>仮想鍵盤の上下段セルの背景色</summary>
        public static string BgColorHighLowLevelCells { get; private set; } = "LightCyan";
        /// <summary>仮想鍵盤の中段セルの背景色</summary>
        public static string BgColorMiddleLevelCells { get; private set; } = "PaleGreen";

        /// <summary>第2打鍵以降を待っているときの仮想中央鍵盤の背景色</summary>
        public static string BgColorOnWaiting2ndStroke { get; private set; }

        /// <summary>交ぜ書き変換時の中央鍵盤背景色 </summary>
        public static string BgColorForMazegaki { get; private set; }

        /// <summary>履歴・連想選択時の中央鍵盤背景色 </summary>
        public static string BgColorForHistOrAssoc { get; private set; }

        /// <summary>履歴候補選択を待っているときの選択対象候補の背景色</summary>
        public static string BgColorForFirstCandidate { get; private set; }

        /// <summary>選択された候補の背景色</summary>
        public static string BgColorOnSelected { get; private set; }

        //-------------------------------------------------------------------------------------
        /// <summary>漢直モード標識の文字色</summary>
        public static string KanjiModeMarkerForeColor { get; private set; }

        /// <summary>第2打鍵待ち時のモード標識の文字色</summary>
        public static string KanjiModeMarker2ndForeColor { get; private set; }

        /// <summary>英字モード標識表示色</summary>
        public static string AlphaModeForeColor { get; private set; } = "Blue";

        /// <summary>入力モードの標識の表示までのインターバル秒数</summary>
        public static int KanjiModeMarkerShowIntervalSec { get; private set; } = 5;

        ///// <summary>漢直モード標識表示のミリ秒</summary>
        //public static int KanjiModeMarkerShowMillisec { get; private set; } = -1;

        /// <summary>英字モード標識表示のミリ秒</summary>
        public static int AlphaModeMarkerShowMillisec { get; private set; } = 1000;

        /// <summary>モード標識表示のためのループ処理におけるポーリングインターバルミリ秒 (1000固定)</summary>
        public static int ModeMarkerProcLoopPollingMillisec { get; private set; } = 1000;

        //-------------------------------------------------------------------------------------
        /// <summary>Ctrlキーを keybd_event でKEY_UP してから BS を PostMessage するまでの待ち時間(ミリ秒)</summary>
        public static int CtrlKeyUpGuardMillisec { get; private set; } = 10;

        /// <summary>BS を PostMessage してから Ctrlキーを keybd_event でKEY_DOWN するまでの待ち時間(ミリ秒)</summary>
        public static int CtrlKeyDownGuardMillisec { get; private set; } = 10;

        /// <summary>部首合成時など、BSを送出してから少し待ってからWM_CHARによる文字送出をしないと先にWM_CHARが処理されたりするので、少し待ち時間が必要(ミリ秒)</summary>
        public static int PreWmCharGuardMillisec { get; private set; } = 25;

        /// <summary>文字数を減少させるための指数</summary>
        public static double ReductionExponet { get; private set; } = 0.7;

        /// <summary>キー入力後に仮想鍵盤をカレット位置に移動するまでの待ち時間(ミリ秒)</summary>
        public static int VirtualKeyboardMoveGuardMillisec { get; private set; } = 500;

        /// <summary>アクティブウィンドウの情報を取得する間隔(ミリ秒)</summary>
        public static int GetActiveWindowInfoIntervalMillisec { get; private set; } = 200;

        //-------------------------------------------------------------------------------------
        // キー割当
        /// <summary>全角変換(モード)を呼び出す打鍵列</summary>
        public static string ZenkakuModeKeySeq { get; set; }
        /// <summary>全角変換(1文字)を呼び出す打鍵列</summary>
        public static string ZenkakuOneCharKeySeq { get; set; }
        /// <summary>次打鍵スルーを呼び出す打鍵列</summary>
        public static string NextThroughKeySeq { get; set; }
        /// <summary>履歴検索を呼び出す打鍵列</summary>
        public static string HistoryKeySeq { get; set; }
        /// <summary>履歴検索(1文字)を呼び出す打鍵列</summary>
        public static string HistoryOneCharKeySeq { get; set; }
        /// <summary>交ぜ書きを呼び出す打鍵列</summary>
        public static string MazegakiKeySeq { get; set; }
        /// <summary>部首合成を呼び出す打鍵列</summary>
        public static string BushuCompKeySeq { get; set; }
        /// <summary>連想文字検索を呼び出す打鍵列</summary>
        public static string BushuAssocKeySeq { get; set; }
        /// <summary>連想直接変換を呼び出す打鍵列</summary>
        public static string BushuAssocDirectKeySeq { get; set; }
        /// <summary>カタカナ変換(モード)を呼び出す打鍵列</summary>
        public static string KatakanaModeKeySeq { get; set; }
        /// <summary>カタカナ変換(一括)を呼び出す打鍵列</summary>
        public static string KatakanaOneShotKeySeq { get; set; }

        /// <summary>全エスケープおよび出力文字列検索ブロッカー設定</summary>
        public static string FullEscapeKey { get; set; }

        /// <summary>ストロークヘルプローテーション</summary>
        public static string StrokeHelpRotationKey { get; set; }

        public static HashSet<int> DecoderSpecialHotkeys { get; set; } = new HashSet<int>();

        //-------------------------------------------------------------------------------------
        // Ctrlキー
        /// <summary>グローバルなコントロールキーを有効にするか </summary>
        public static bool GlobalCtrlKeysEnabled { get; set; } = false;

        /// <summary>左コントロールキーを変換に使う</summary>
        public static bool UseLeftControlToConversion { get; private set; } = false;

        /// <summary>右コントロールキーを変換に使う</summary>
        public static bool UseRightControlToConversion { get; private set; } = false;

        /// <summary>Ctrl-H を BackSpace に変換する </summary>
        public static bool ConvertCtrlHtoBackSpace { get; private set; } = false;
        public static bool ConvertCtrlHtoBackSpaceEffective => ConvertCtrlHtoBackSpace && GlobalCtrlKeysEnabled;

        /// <summary>Ctrl-B,F,N,P を矢印キーに変換する </summary>
        public static bool ConvertCtrlBFNPtoArrowKey { get; private set; } = false;
        public static bool ConvertCtrlBFNPtoArrowKeyEffective => ConvertCtrlBFNPtoArrowKey && GlobalCtrlKeysEnabled;

        /// <summary>Ctrl-A を Home に変換する </summary>
        public static bool ConvertCtrlAtoHome { get; private set; } = false;
        public static bool ConvertCtrlAtoHomeEffective => ConvertCtrlAtoHome && GlobalCtrlKeysEnabled;

        /// <summary>Ctrl-D を Delete に変換する </summary>
        public static bool ConvertCtrlDtoDelete { get; private set; } = false;
        public static bool ConvertCtrlDtoDeleteEffective => ConvertCtrlDtoDelete && GlobalCtrlKeysEnabled;

        /// <summary>Ctrl-E を End に変換する </summary>
        public static bool ConvertCtrlEtoEnd { get; private set; } = false;
        public static bool ConvertCtrlEtoEndEffective => ConvertCtrlEtoEnd && GlobalCtrlKeysEnabled;

        /// <summary>Ctrl-G を FullEscape に変換する </summary>
        public static bool ConvertCtrlGtoEscape { get; private set; } = false;

        /// <summary>ウィンドウClassNameリストを対象リストとして扱うか</summary>
        public static bool UseClassNameListAsInclusion { get; private set; } = false;

        /// <summary>Ctrl修飾キー変換の対象(または対象外)となるウィンドウのClassName</summary>
        public static HashSet<string> CtrlKeyTargetClassNames { get; private set; } = new HashSet<string>();

        /// <summary>Ctrl-J を Enter と同じように扱う </summary>
        public static bool UseCtrlJasEnter { get; private set; } = false;

        /// <summary>Ctrl-M を Enter と同じように扱う </summary>
        public static bool UseCtrlMasEnter { get; private set; } = false;

        /// <summary>Ctrl-Shift-Semicolon で今日の日付を出力する </summary>
        public static bool ConvertCtrlSemiColonToDate { get; private set; } = false;
        public static bool ConvertCtrlSemiColonToDateEffective => ConvertCtrlSemiColonToDate && GlobalCtrlKeysEnabled;

        /// <summary>Ctrl-Semicolon で今日の日付を出力する </summary>
        public static string DateStringFormat { get; private set; } = "yyyy/M/d";

        //------------------------------------------------------------------------------
        // 履歴
        public static int HistKatakanaWordMinLength { get; private set; } = 0;
        public static int HistKanjiWordMinLength { get; private set; } = 0;
        public static int HistKanjiWordMinLengthEx { get; private set; } = 0;
        public static int HistHiraganaKeyLength { get; private set; } = 0;
        public static int HistKatakanaKeyLength { get; private set; } = 0;
        public static int HistKanjiKeyLength { get; private set; } = 0;
        public static bool AutoHistSearchEnabled { get; private set; } = false;
        public static bool HistSearchByCtrlSpace { get; private set; } = false;
        //public static bool HistSearchByShiftSpace { get; private set; } = false;
        public static bool SelectFirstCandByEnter { get; private set; } = false;
        public static bool HistAllowFromMiddleChar { get; private set; } = false;
        public static int HistDelHotkeyId { get; private set; } = 0;
        public static int HistNumHotkeyId { get; private set; } = 0;

        public static bool UseArrowKeyToSelectCandidate { get; set; } = true;
        public static bool HandleShiftSpaceAsNormalSpace { get; set; } = true;

        //------------------------------------------------------------------------------
        // スペースキー
        public static bool UseShiftSpaceAsHotkey49 => MazegakiByShiftSpace;
        public static bool UseCtrlSpaceKey => HistSearchByCtrlSpace;
        //public static bool UseShiftSpaceAsSpecialHotKey => (HistSearchByShiftSpace || HandleShiftSpaceAsNormalSpace) && !UseShiftSpaceAsHotkey49;
        public static bool UseShiftSpaceAsSpecialHotKey => (HandleShiftSpaceAsNormalSpace) && !UseShiftSpaceAsHotkey49;

        //------------------------------------------------------------------------------
        // 交ぜ書き
        public static bool MazegakiByShiftSpace { get; set; } = true;
        public static int MazeYomiMaxLen { get; private set; } = 10;
        public static int MazeGobiMaxLen { get; private set; } = 3;

        //------------------------------------------------------------------------------
        // 各種変換
        // 平仮名⇒カタカナ変換
        public static bool ConvertShiftedHiraganaToKatakana { get; set; } = false;
        // 「。」⇔「．」
        public static bool ConvertJaPeriod { get; set; } = false;
        // 「、」⇔「，」
        public static bool ConvertJaComma { get; set; } = false;

        // BS で全打鍵を取り消すか
        public static bool RemoveOneStrokeByBackspace { get; set; } = true;

        //------------------------------------------------------------------------------
        // ウィンドウClassNameごとの設定
        public class WindowsClassSettings
        {
            public int[] ValidCaretMargin;
            public int[] CaretOffset;
            public int CtrlUpWaitMillisec = -1;
            public int CtrlDownWaitMillisec = -1;
        }

        private static Dictionary<string, WindowsClassSettings> winClassSettings = new Dictionary<string, WindowsClassSettings>();

        public static WindowsClassSettings GetWinClassSettings(string name)
        {
            if (name._notEmpty()) {
                var lowerName = name._toLower();
                foreach (var pair in winClassSettings) {
                    if (lowerName.StartsWith(pair.Key)) {
                        return pair.Value;
                    }
                }
            }
            return null;
        }

        //------------------------------------------------------------------------------
        /// <summary>デコーダ用の設定辞書</summary>
        public static Dictionary<string, string> DecoderSettings { get; private set; } = new Dictionary<string, string>();

        public static string SerializedDecoderSettings => DecoderSettings.Select(pair => $"{pair.Key}={pair.Value}")._join("\n");

        //------------------------------------------------------------------------------
        public static string GetString(string attr, string defval = "")
        {
            return UserKanchokuIni.Singleton.GetString(attr)._orElse(() => KanchokuIni.Singleton.GetString(attr, defval));
        }

        // kanchoku.use.ini が存在しない時のデフォルト値を設定できる(デフォルトの辞書ファイルなどを設定して、それが存在しなくてもエラーにしない処理をするため)
        public static string GetStringEx(string attr, string defvalInit, string defval = "")
        {
            return UserKanchokuIni.Singleton.GetStringEx(attr, defvalInit)._orElse(() => KanchokuIni.Singleton.GetString(attr, defval));
        }

        public static string GetString(string attr, string attrOld, string defval)
        {
            return UserKanchokuIni.Singleton.GetString(attr)._orElse(() => KanchokuIni.Singleton.GetString(attrOld, defval));
        }

        // kanchoku.use.ini が存在しない時のデフォルト値を設定できる(デフォルトの辞書ファイルなどを設定して、それが存在しなくてもエラーにしない処理をするため)
        public static string GetStringEx(string attr, string attrOld, string defvalInit, string defval)
        {
            return UserKanchokuIni.Singleton.GetStringEx(attr, defvalInit)._orElse(() => KanchokuIni.Singleton.GetString(attrOld, defval));
        }

        public static string GetStringFromSection(string section, string attr, string defval = "")
        {
            return UserKanchokuIni.Singleton.GetStringFromSection(section, attr)._orElse(() => KanchokuIni.Singleton.GetStringFromSection(section, attr, defval));
        }

        public static int GetLogLevel()
        {
            return GetString("logLevel")._parseInt(Logger.LogLevelWarn)._lowLimit(0)._highLimit(Logger.LogLevelTrace);   // デフォルトは WARN
        }

        public static bool IsMultiAppEnabled()
        {
            return GetString("multiAppEnabled")._parseBool();   // デフォルトは false
        }

        private static string[] GetSectionNames()
        {
            var set = UserKanchokuIni.Singleton.GetSectionNames().ToHashSet();
            set.UnionWith(KanchokuIni.Singleton.GetSectionNames().ToHashSet());
            return set.ToArray();
        }

        // デコーダ用設定
        private static string addDecoderSetting(string attr, string defval = "")
        {
            return DecoderSettings[attr] = GetString(attr, defval);
        }

        // kanchoku.use.ini が存在しない時のデフォルト値を設定できる(デフォルトの辞書ファイルなどを設定して、それが存在しなくてもエラーにしない処理をするため)
        private static string addDecoderSettingEx(string attr, string defvalInit, string defval = "")
        {
            return DecoderSettings[attr] = GetStringEx(attr, defvalInit, defval);
        }

        private static string addDecoderSetting(string attr, string attrOld, string defval)
        {
            return DecoderSettings[attr] = GetString(attr, attrOld, defval);
        }

        // kanchoku.use.ini が存在しない時のデフォルト値を設定できる(デフォルトの辞書ファイルなどを設定して、それが存在しなくてもエラーにしない処理をするため)
        private static string addDecoderSettingEx(string attr, string attrOld, string defvalInit, string defval)
        {
            return DecoderSettings[attr] = GetStringEx(attr, attrOld, defvalInit, defval);
        }

        private static int addDecoderSetting(string attr, int defval, int lowLimit = 0)
        {
            int result = GetString(attr)._parseInt(defval)._lowLimit(lowLimit);
            DecoderSettings[attr] = GetString(attr, $"{result}");
            return result;
        }

        private static bool addDecoderSetting(string attr, bool defval)
        {
            bool result = GetString(attr)._parseBool(defval);
            DecoderSettings[attr] = GetString(attr, $"{result}"._toLower());
            return result;
        }

        private static bool addDecoderSetting(string attr, string attrOld, bool defval)
        {
            var resultStr = GetString(attr, attrOld, "");
            bool result = resultStr._equalsTo("1") || resultStr._parseBool(defval);
            DecoderSettings[attr] = GetString(attr, $"{result}"._toLower());
            return result;
        }

        // kanchoku.use.ini が存在しない時のデフォルト値を設定できる(デフォルトの辞書ファイルなどを設定して、それが存在しなくてもエラーにしない処理をするため)
        private static string addDecoderSettingByGettingFiles(string attr, string defval)
        {
            //string filePattern = GetStringEx(attr, defval);
            string filePattern = GetString(attr, defval);
            string names = filePattern._isEmpty() ? "" : getFiles(filePattern);
            if (filePattern.Contains('*') && !names.Contains('*')) names = names + $"|{filePattern}";
            DecoderSettings[attr] = names;
            return filePattern;
        }

        /// <summary> パターンによるファイル名の取得 (複数ある場合は、| で連結する) </summary>
        private static string getFiles(string filePattern)
        {
            return Helper.GetFiles(KanchokuIni.Singleton.KanchokuDir, filePattern)._join("|");
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// kanchoku.ini から各種設定を読み込む
        /// </summary>
        /// <returns></returns>
        public static bool ReadIniFile()
        {
            //-------------------------------------------------------------------------------------
            // 基本設定
            HotkeyInfiniteLoopDetectCount = GetString("hotkeyInfiniteLoopDetectCount")._parseInt(1000)._lowLimit(100);
            KeyRepeatDetectMillisec = GetString("keyRepeatDetectMillisec")._parseInt(100)._lowLimit(50);
            //AutoOffWhenBurstKeyIn = GetString("autoOffWhenBurstKeyIn")._parseBool();
            SplashWindowShowDuration = GetString("splashWindowShowDuration")._parseInt(60)._lowLimit(0);
            ConfirmOnClose = GetString("confirmOnClose")._parseBool(true);

            //-------------------------------------------------------------------------------------
            LogLevel = GetLogLevel();
            LoggingHotKeyInfo = GetString("loggingHotKeyInfo")._parseBool();
            //LoggingActiveWindowInfo = GetString("loggingActiveWindowInfo")._parseBool();
            DelayAfterProcessHotkey = GetString("delayAfterProcessHotkey")._parseBool();
            MultiAppEnabled = IsMultiAppEnabled();

            //-------------------------------------------------------------------------------------
            // ファイル設定
            KeyboardFile = GetString("keyboard", "106.key");

            StrokeHelpFile = GetString("strokeHelpFile", "stroke-help.txt");

            //-------------------------------------------------------------------------------------
            // 漢直モードトグルキー
            ActiveKey = (uint)GetString("unmodifiedHotKey")._parseHex(0)._lowLimit(0);
            ActiveKeyWithCtrl = (uint)GetString("hotKey")._parseHex(0)._lowLimit(0);
            if (ActiveKey == 0 && ActiveKeyWithCtrl == 0) ActiveKeyWithCtrl = 0xdc;

            // 漢直モードOFFキー
            DeactiveKey = (uint)GetString("unmodifiedOffHotKey")._parseHex(0)._lowLimit(0);
            DeactiveKeyWithCtrl = (uint)GetString("offHotKey")._parseHex(0)._lowLimit(0);

            //-------------------------------------------------------------------------------------
            // 文字送出時にコピー&ペーストを行う文字数の閾値
            MinLeghthViaClipboard = GetString("minLeghthViaClipboard")._parseInt(0)._lowLimit(0);

            //-------------------------------------------------------------------------------------
            // フォントと色の設定
            NormalVkbFontSpec = GetString("normalFont", "MS Gothic | 10");
            CenterVkbFontSpec = GetString("centerFont", "@MS Gothic | 9");
            VerticalVkbFontSpec = GetString("verticalFont", "@MS Gothic | 9");
            HorizontalVkbFontSpec = GetString("horizontalFont", "MS Gothic | 9");
            MiniBufVkbFontSpec = GetString("minibufFont", "MS Gothic | 9");

            //-------------------------------------------------------------------------------------
            VirtualKeyboardShowStrokeCount = GetString("vkbShowStrokeCount")._parseInt(1);

            //-------------------------------------------------------------------------------------
            VirtualKeyboardOffsetX = GetString("vkbOffsetX")._parseInt(2)._lowLimit(0);
            VirtualKeyboardOffsetY = GetString("vkbOffsetY")._parseInt(2)._lowLimit(0);
            //DisplayScale = GetString("displayScale")._parseDouble(1.0)._lowLimit(1.0);

            BgColorTopLevelCells = GetString("bgColorTopLevelCells", "GhostWhite");
            BgColorCenterSideCells = GetString("bgColorCenterSideCells", "FloralWhite");
            BgColorHighLowLevelCells = GetString("bgColorHighLowLevelCells", "LightCyan");
            BgColorMiddleLevelCells = GetString("bgColorMiddleLevelCells", "PaleGreen");

            BgColorOnWaiting2ndStroke = GetString("bgColorOnWaiting2ndStroke", "Yellow");
            BgColorForMazegaki = GetString("bgColorForMazegaki", "Plum");
            BgColorForHistOrAssoc = GetString("bgColorForHistOrAssoc", "PaleTurquoise");
            BgColorForFirstCandidate = GetString("bgColorForFirstCandidate", "PaleGreen");
            BgColorOnSelected = GetString("bgColorOnSelected", "LightPink");

            //-------------------------------------------------------------------------------------
            KanjiModeMarkerForeColor = GetString("kanjiModeMarkerForeColor", "Blue");
            KanjiModeMarker2ndForeColor = GetString("kanjiModeMarker2ndForeColor", "LightSeaGreen");
            AlphaModeForeColor = GetString("alphaModeForeColor", "CadetBlue");

            //KanjiModeMarkerShowMillisec = GetString("kanjiModeMarkerShowMillisec")._parseInt(-1)._lowLimit(-1);
            KanjiModeMarkerShowIntervalSec = GetString("kanjiModeMarkerShowIntervalSec")._parseInt(0)._lowLimit(-1);
            AlphaModeMarkerShowMillisec = GetString("alphaModeMarkerShowMillisec")._parseInt(1000)._lowLimit(0);
            ModeMarkerProcLoopPollingMillisec = GetString("modeMarkerProcLoopPollingMillisec")._parseInt(1000)._lowLimit(100);

            //-------------------------------------------------------------------------------------
            CtrlKeyUpGuardMillisec = GetString("ctrlKeyUpGuardMillisec")._parseInt(15)._lowLimit(0);
            CtrlKeyDownGuardMillisec = GetString("ctrlKeyDownGuardMillisec")._parseInt(0)._lowLimit(0);     // これが 0 より大きいとCTRLキーDOWNと誤認識される可能性が高まる
            PreWmCharGuardMillisec = GetString("preWmCharGuardMillisec")._parseInt(25)._lowLimit(0);
            ReductionExponet = GetString("reductionExponent")._parseDouble(0.7)._lowLimit(0.5);

            VirtualKeyboardMoveGuardMillisec = GetString("virtualKeyboardMoveGuardMillisec")._parseInt(500)._lowLimit(0);
            GetActiveWindowInfoIntervalMillisec = GetString("activeWindowInfoIntervalMillisec")._parseInt(200)._lowLimit(100);

            //-------------------------------------------------------------------------------------
            // Ctrlキー変換
            GlobalCtrlKeysEnabled = GetString("globalCtrlKeysEnabled")._parseBool(false);

            ConvertCtrlHtoBackSpace = GetString("convertCtrlHtoBackSpace")._parseBool(true);
            ConvertCtrlBFNPtoArrowKey = GetString("convertCtrlBFNPtoArrowKey")._parseBool(true);
            ConvertCtrlAtoHome = GetString("convertCtrlAtoHome")._parseBool(true);
            ConvertCtrlDtoDelete = GetString("convertCtrlDtoDelete")._parseBool(true);
            ConvertCtrlEtoEnd = GetString("convertCtrlEtoEnd")._parseBool(true);
            ConvertCtrlGtoEscape = true;

            UseLeftControlToConversion = GetString("useLeftControlToConversion")._parseBool(true);
            UseRightControlToConversion = GetString("useRightControlToConversion")._parseBool(false);
            UseClassNameListAsInclusion = GetString("useClassNameListAsInclusion")._parseBool(false);
            CtrlKeyTargetClassNames = new HashSet<string>(GetString("ctrlKeyTargetlassNames").Trim()._toLower()._split('|'));

            UseCtrlJasEnter = GetString("useCtrlJasEnter")._parseBool(false);
            UseCtrlMasEnter = GetString("useCtrlMasEnter")._parseBool(false);

            ConvertCtrlSemiColonToDate = GetString("convertCtrlSemicolonToDate")._parseBool(true);
            DateStringFormat = GetString("dateStringFormat", "yyyy/M/d|yyyyMMdd");

            //-------------------------------------------------------------------------------------
            // ClassName ごとの設定
            winClassSettings.Clear();
            foreach (var name in GetSectionNames()) {
                if (name._ne("kanchoku")) {
                    winClassSettings[name._toLower()] = new WindowsClassSettings() {
                        ValidCaretMargin = GetStringFromSection(name, "validCaretMargin", "").Trim()._split(',').Select(x => x._parseInt(0)._lowLimit(0)).ToArray(),
                        CaretOffset = GetStringFromSection(name, "caretOffset", "").Trim()._split(',').Select(x => x._parseInt(0)._lowLimit(0)).ToArray(),
                        CtrlUpWaitMillisec = GetStringFromSection(name, "ctrlUpWaitMillisec", "-1")._parseInt(-1),
                        CtrlDownWaitMillisec = GetStringFromSection(name, "ctrlDownWaitMillisec", "-1")._parseInt(-1),
                    };
                }
            }

            //-------------------------------------------------------------------------------------
            // デコーダ設定
            DecoderSettings.Clear();
            DecoderSettings["logLevel"] = LogLevel.ToString();
            DecoderSettings["rootDir"] = KanchokuIni.Singleton.KanchokuDir;
            DecoderSettings["firstUse"] = $"{!UserKanchokuIni.Singleton.IsIniFileExist}";
            BushuAssocFile = addDecoderSetting("bushuAssocFile", "kwassoc.txt");
            BushuFile = addDecoderSetting("bushuFile", "bushu", "kwbushu.rev");
            CharsDefFile = addDecoderSetting("charsDefFile", $"chars.{KeyboardFile._split('.')._getNth(0)._orElse("106")}.txt");
            EasyCharsFile = addDecoderSetting("easyCharsFile", "easy_chars.txt");
            TableFile = addDecoderSetting("tableFile", "t.tbl");
            //addDecoderSetting("strokeHelpFile");
            HistoryFile = addDecoderSetting("historyFile", "kwhist.*.txt");
            //addDecoderSetting("historyUsedFile");
            //addDecoderSetting("historyExcludeFile");
            //addDecoderSetting("historyNgramFile");
            MazegakiFile = addDecoderSettingByGettingFiles("mazegakiFile", "kwmaze.*.dic");

            BackFileRotationGeneration = addDecoderSetting("backFileRotationGeneration", 3, 1); // 辞書ファイルの保存世代数

            HistKatakanaWordMinLength = addDecoderSetting("histKatakanaWordMinLength", 4, 3);   // 履歴登録対象となるカタカナ文字列の最小長
            HistKanjiWordMinLength = addDecoderSetting("histKanjiWordMinLength", 4, 3);         // 履歴登録対象となる漢字文字列の最小長
            HistKanjiWordMinLengthEx = addDecoderSetting("histKanjiWordMinLengthEx", 2, 2);     // 履歴登録対象となる難打鍵文字を含む漢字文字列の最小長
            HistHiraganaKeyLength = addDecoderSetting("histHiraganaKeyLength", 2, 1);           // ひらがな始まり履歴の検索を行う際のキー長
            HistKatakanaKeyLength = addDecoderSetting("histKatakanaKeyLength", 2, 1);           // カタカナ履歴の検索を行う際のキー長
            HistKanjiKeyLength = addDecoderSetting("histKanjiKeyLength", 1, 1);                 // 漢字履歴の検索を行う際のキー長
            AutoHistSearchEnabled = addDecoderSetting("autoHistSearchEnabled", true);           // 自動履歴検索を行う
            HistSearchByCtrlSpace = addDecoderSetting("histSearchByCtrlSpace", true);           // Ctrl-Space で履歴検索を行う
            //HistSearchByShiftSpace = addDecoderSetting("histSearchByShiftSpace", true);         // Shift-Space で履歴検索を行う
            SelectFirstCandByEnter = addDecoderSetting("selectFirstCandByEnter", false);        // Enter で最初の履歴検索候補を選択する
            HistDelHotkeyId = addDecoderSetting("histDelHotkeyId", 41, 41);                     // 履歴削除を呼び出すHotKeyのID
            HistNumHotkeyId = addDecoderSetting("histNumHotkeyId", 45, 41);                     // 履歴文字数指定を呼び出すHotKeyのID
            HistAllowFromMiddleChar = addDecoderSetting("histAllowFromMiddleChar", true);       // 出力漢字列やカタカナ列の途中からでも自動履歴検索を行う(@TODO)
            UseArrowKeyToSelectCandidate = addDecoderSetting("useArrowKeyToSelectCandidate", true);    // 矢印キーで履歴候補選択を行う
            HandleShiftSpaceAsNormalSpace = addDecoderSetting("handleShiftSpaceAsNormalSpace", true);  // Shift+Space を通常 Space しとて扱う(HistSearchByShiftSpaceがfalseの場合)

            MazegakiByShiftSpace  = GetString("mazegakiByShiftSpace  ")._parseBool(true);       // Shift-Space で交ぜ書き変換
            MazeYomiMaxLen = addDecoderSetting("mazeYomiMaxLen", 10, 8);                        // 交ぜ書きの読み入力の最大長
            MazeGobiMaxLen = addDecoderSetting("mazeGobiMaxLen", 3, 0);                         // 交ぜ書きの語尾の最大長

            ConvertShiftedHiraganaToKatakana = addDecoderSetting("convertShiftedHiraganaToKatakana", "shiftKana", false);  // 平仮名をカタカナに変換する
            ConvertJaPeriod = addDecoderSetting("convertJaPeriod", false);                      // 「。」と「．」の相互変換
            ConvertJaComma = addDecoderSetting("convertJaComma", false);                        // 「、」と「，」の相互変換

            RemoveOneStrokeByBackspace = addDecoderSetting("removeOneStrokeByBackspace", "weakBS", false);  // BS で直前打鍵のみを取り消すか

            // キー割当
            FullEscapeKey = GetString("fullEscapeKey", "G").Trim();
            VirtualKeys.AddCtrlHotkey(FullEscapeKey, HotKeys.FULL_ESCAPE_HOTKEY, HotKeys.UNBLOCK_HOTKEY);
            StrokeHelpRotationKey = GetString("strokeHelpRotationKey", "T");
            VirtualKeys.AddCtrlHotkey(StrokeHelpRotationKey, HotKeys.STROKE_HELP_ROTATION_HOTKEY, HotKeys.STROKE_HELP_UNROTATION_HOTKEY);

            DecoderSpecialHotkeys.Clear();
            DecoderSpecialHotkeys.Add(HotKeys.FULL_ESCAPE_HOTKEY);
            DecoderSpecialHotkeys.Add(HotKeys.UNBLOCK_HOTKEY);
            DecoderSpecialHotkeys.Add(HotKeys.STROKE_HELP_ROTATION_HOTKEY);
            DecoderSpecialHotkeys.Add(HotKeys.STROKE_HELP_UNROTATION_HOTKEY);

            ZenkakuModeKeySeq = addDecoderSetting("zenkakuModeKeySeq");
            ZenkakuOneCharKeySeq = addDecoderSetting("zenkakuOneCharKeySeq");
            NextThroughKeySeq = addDecoderSetting("nextThroughKeySeq");
            HistoryKeySeq = addDecoderSetting("historyKeySeq");
            HistoryOneCharKeySeq = addDecoderSetting("historyOneCharKeySeq");
            MazegakiKeySeq = addDecoderSetting("mazegakiKeySeq");
            BushuCompKeySeq = addDecoderSetting("bushuCompKeySeq");
            BushuAssocKeySeq = addDecoderSetting("bushuAssocKeySeq");
            BushuAssocDirectKeySeq = addDecoderSetting("bushuAssocDirectKeySeq");
            KatakanaModeKeySeq = addDecoderSetting("katakanaModeKeySeq");
            KatakanaOneShotKeySeq = addDecoderSetting("katakanaOneShotKeySeq");

            // for Debug
            addDecoderSetting("debughState", false);
            addDecoderSetting("debughMazegaki", false);
            addDecoderSetting("debughHistory", false);
            addDecoderSetting("debughStrokeTable", false);
            addDecoderSetting("debughBushu", false);
            addDecoderSetting("debughZenkaku", false);
            addDecoderSetting("debughKatakana", false);
            BushuDicLogEnabled = addDecoderSetting("bushuDicLogEnabled", false);

            return true;
        }

        //------------------------------------------------------------------------------
        public static string GetUserIni(string key)
        {
            return GetString(key);
        }

        public static void SetUserIni(string key, string value)
        {
            UserKanchokuIni.Singleton.SetString(key, value);
        }

        public static int GetUserIniInt(string key)
        {
            return GetString(key)._parseInt();
        }

        public static void SetUserIni(string key, int value)
        {
            UserKanchokuIni.Singleton.SetInt(key, value);
        }

        public static void SetUserIni(string key, bool value)
        {
            UserKanchokuIni.Singleton.SetBool(key, value);
        }
    }
}

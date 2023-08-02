using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using KanchokuWS.Domain;
using Utils;

namespace KanchokuWS
{
    public static class Settings
    {
        private static Logger logger = Logger.GetLogger(true);

        //-------------------------------------------------------------------------------------
        /// <summary> バージョン </summary>
        public static string Version => "1.2.8";
        public static string Version2 => "";

        //-------------------------------------------------------------------------------------
        /// <summary> テーブルファイルなど内部で設定された値 </summary>
        private static Dictionary<string, string> internalSetValues = new Dictionary<string, string>();

        public static bool IsInternalValueSet(string key) => internalSetValues.ContainsKey(key._toLower());

        /// <summary>
        /// 名前で指定されたプロパティに値を設定する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static void SetInternalValue(string propName, string value)
        {
            logger.DebugH(() => $"CALLED: propName={propName}, value={value}");
            try {
                internalSetValues[propName._toLower()] = value;
            } catch (Exception e) {
                logger.WarnH(e._getErrorMsgShort());
            }
        }

        public static string GetInternalValue(string key)
        {
            return internalSetValues._safeGet(key._toLower());
        }
        //-------------------------------------------------------------------------------------

        /// <summary> ドキュメントへのURL </summary>
        public static string ReadmeUrl => "https://github.com/oktopus1959/KanchokuWS#readme";
        public static string ManualUrl => "https://github.com/oktopus1959/KanchokuWS/blob/main/MANUAL.md#top";
        public static string FaqUrl => "https://github.com/oktopus1959/KanchokuWS/blob/main/FAQ.md#faq-home";
        public static string FaqBasicUrl => "https://github.com/oktopus1959/KanchokuWS/blob/main/FAQ/FAQ-基本.md#faq-home";
        public static string KeyboardUrl => "https://github.com/oktopus1959/KanchokuWS/blob/main/KEYBOARD.md#top";

        //-------------------------------------------------------------------------------------
        // 一時停止
        //-------------------------------------------------------------------------------------
        public static bool DecoderSuspended { get; set; } = false;

        /// <summary>Pauseキーで一時停止・再開する</summary>
        public static bool SuspendByPauseKey { get; set; } = false;

        //-------------------------------------------------------------------------------------
        // CtrlやAltのようなショートカット(アクセラレータ)キーに対する英字変換を有効にするか
        //-------------------------------------------------------------------------------------
        public static bool ShortcutKeyConversionEnabled { get; set; } = true;

        //-------------------------------------------------------------------------------------
        // ログ出力設定
        //-------------------------------------------------------------------------------------
        /// <summary>ログレベル</summary> 
        public static int LogLevel { get; private set; } = 0;

        /// <summary>true の場合、入力されたDecoderキーに関する情報をログ出力する</summary>
        public static bool LoggingDecKeyInfo { get; private set; }

        /// <summary>true の場合、アクティブウィンドウに関する情報をログ出力する</summary>
        public static bool LoggingActiveWindowInfo { get; set; } = false;

        /// <summary>true の場合、仮想キーボードに関する情報をログ出力する</summary>
        public static bool LoggingVirtualKeyboardInfo { get; set; }

        /// <summary>true の場合、テーブルファイルに関する情報をログ出力する</summary>
        public static bool LoggingTableFileInfo { get; set; }

        /// <summary>Decoderキー処理後にウェイトを入れる(開発用; バグ等により処理対象ホットキーを keybd_event で送出することによる無限ループに対応する時間をかせぐ)</summary>
        //public static bool DelayAfterProcessDeckey { get; private set; } = false;

        /// <summary>二重起動を許可する</summary>
        public static bool MultiAppEnabled { get; private set; }

        /// <summary>部首合成ログを有効にする</summary>
        public static bool BushuDicLogEnabled { get; private set; }

        /// <summary>指定個数以上の打鍵が残っていたら警告をログ出力する</summary>        
        public static int WarnThresholdKeyQueueCount { get; private set; }

        /// <summary>デバッグ用のテーブルファイルを出力する</summary>        
        public static bool OutputDebugTableFiles { get; private set; }

        /// <summary>隠しテーブルファイルフォルダを表示する</summary>        
        public static bool ShowHiddleFolder { get; private set; }

        public static bool IsAnyDevFlagEnabled => LogLevel > Logger.LogLevelWarn || LoggingDecKeyInfo || LoggingActiveWindowInfo || LoggingVirtualKeyboardInfo || LoggingTableFileInfo || BushuDicLogEnabled;

        //-------------------------------------------------------------------------------------
        // 基本設定
        //-------------------------------------------------------------------------------------
        /// <summary>Ctrl修飾なしで Decoder をアクティブにするホットキーの仮想キーコード</summary> 
        public static uint ActiveKey { get; private set; }
        /// <summary>Ctrl修飾ありで Decoder をアクティブにするホットキーの仮想キーコード</summary> 
        public static uint ActiveKeyWithCtrl { get; private set; }
        /// <summary>Ctrl修飾ありで Decoder をアクティブにするホットキーの仮想キーコード</summary> 
        public static uint ActiveKeyWithCtrl2 { get; private set; }

        /// <summary>Ctrl修飾なしで Decoder をアクティブにしたときに選択するテーブルの番号</summary> 
        public static int SelectedTableActivatedWithoutCtrl { get; set; }

        /// <summary>Ctrl修飾ありで Decoder をアクティブにしたときに選択するテーブルの番号</summary> 
        public static int SelectedTableActivatedWithCtrl { get; set; }
        /// <summary>Ctrl修飾ありで Decoder をアクティブにしたときに選択するテーブルの番号その</summary> 
        public static int SelectedTableActivatedWithCtrl2 { get; set; }

        /// <summary>Ctrl修飾なしで Decoder を非アクティブにするホットキーの仮想キーコード</summary> 
        public static uint DeactiveKey { get; private set; }
        /// <summary>Ctrl修飾ありで Decoder を非アクティブにするホットキーの仮想キーコード</summary> 
        public static uint DeactiveKeyWithCtrl { get; private set; }

        //public static uint DeactiveKeyEffective => DeactiveKey != 0 ? DeactiveKey : ActiveKey;
        //public static uint DeactiveKeyWithCtrlEffective => DeactiveKeyWithCtrl != 0 ? DeactiveKeyWithCtrl : ActiveKeyWithCtrl;

        /// <summary>スプラッシュウィンドウの表示時間</summary>
        public static int SplashWindowShowDuration { get; private set; }

        /// <summary>終了時に確認ダイアログを出す</summary>
        public static bool ConfirmOnClose { get; private set; }

        /// <summary>再起動時に確認ダイアログを出す</summary>
        public static bool ConfirmOnRestart { get; private set; } = true;

        /// <summary>アイコンクリック時に設定ダイアログを出す</summary>
        public static bool OpenSettingsDlgWhenIconClicked { get; private set; } = false;

        //-------------------------------------------------------------------------------------
        // 各種ファイル
        //-------------------------------------------------------------------------------------
        /// <summary>キーボードファイル</summary>
        public static string KeyboardFile { get; private set; }

        //public static string CharsDefFile { get; private set; }

        public static string TempCharsDefFile { get; set; }

        /// <summary>打鍵ヘルプ定義ファイル</summary>
        public static string StrokeHelpFile { get; private set; }

        /// <summary> 漢字読みファイル</summary>
        public static string KanjiYomiFile { get; private set; }

        // 各種辞書ファイル
        public static string BushuAssocFile { get; private set; }
        public static string BushuFile { get; private set; }
        public static string AutoBushuFile { get; private set; }
        public static string EasyCharsFile { get; private set; }

        public const string TableFileDir = "tables";
        public const string KeyboardFileDir = "tables/_keyboard";

        public static string TableFile { get; private set; }
        public static string TableFile2 { get; private set; }
        public static string TableFile3 { get; private set; }
        public static string HistoryFile { get; private set; }
        //public static string HistoryUsedFile {get; private set; }
        //public static string HistoryExcludeFile {get; private set; }
        //public static string HistoryNgramFile {get; private set; }
        public static string MazegakiFile { get; private set; }

        //-------------------------------------------------------------------------------------
        // 詳細設定
        //-------------------------------------------------------------------------------------
        /// <summary>仮想鍵盤の表示モード</summary>
        public enum VkbShowMode
        {
            /// <summary>常にカレットの近くに表示する</summary>
            Always,
            /// <summary>何らかの選択状態にあるときのみ表示する</summary>
            OnSelect,
        }
        /// <summary>仮想鍵盤またはモード標識を表示する</summary>
        public static bool ShowVkbOrMaker { get; set; }

        /// <summary>英数モード時に仮想鍵盤を表示する</summary>
        public static bool ShowEisuVkb { get; set; }

        /// <summary>N打鍵目に仮想鍵盤を表示する(0なら仮想鍵盤を表示しない)</summary>
        public static int VirtualKeyboardShowStrokeCount { get; set; }
        public static int VirtualKeyboardShowStrokeCountTemp { get; set; } = 0;
        public static int VirtualKeyboardShowStrokeCountEffective =>
            VirtualKeyboardShowStrokeCount <= 0 || (VirtualKeyboardShowStrokeCountTemp > 0 && VirtualKeyboardShowStrokeCountTemp < VirtualKeyboardShowStrokeCount)
            ? VirtualKeyboardShowStrokeCountTemp
            : VirtualKeyboardShowStrokeCount;

        /// <summary>仮想鍵盤の一時的な表示/非表示キー</summary>
        public static string VkbShowHideTemporaryKey { get; set; }

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
        public static int VirtualKeyboardOffsetX { get; private set; }

        /// <summary>カレットと仮想鍵盤の相対位置: Y方向</summary>
        public static int VirtualKeyboardOffsetY { get; private set; }

        /// <summary>仮想鍵盤の固定位置: X方向</summary>
        public static int VirtualKeyboardFixedPosX { get; private set; }

        /// <summary>仮想鍵盤の固定位置: Y方向</summary>
        public static int VirtualKeyboardFixedPosY { get; private set; }

        /// <summary>仮想鍵盤の位置を一時的に固定する</summary>
        public static bool VirtualKeyboardPosFixedTemporarily { get; set; } = false;

        /// <summary>仮想鍵盤の位置を固定するウィンドウのClassName</summary>
        public static string FixedPosClassNames { get; private set; }
        private static HashSet<string> FixedPosClassNamesHash { get; set; } = new HashSet<string>();

        public static bool IsFixedPosWinClass(string className)
        {
            return className._equalsTo("ConsoleWindowClass") || FixedPosClassNamesHash._safeContains(className._toLower());
        }

        ///// <summary>ディスプレイのDPI比(標準96dpiとの比)</summary>
        //public static double DisplayScale { get; private set; } = 1.0;

        /// <summary>ファイル保存世代数</summary>
        public static int BackFileRotationGeneration { get; private set; }

        /// <summary>辞書保存インターバルタイム(分)(負の場合は「保存を行う」チェックボックスをOFFにする)</summary>
        public static int SaveDictsIntervalTime { get; private set; }

        /// <summary>辞書保存に適した平穏な時間(分)</summary>
        public static int SaveDictsCalmTime { get; private set; }

        /// <summary>自身以外のキーボードフックツールからの出力を無視する</summary>
        public static bool IgnoreOtherHooker { get; private set; }

        /// <summary> 文字送出時にコピー&ペーストを行う文字数の閾値 </summary>
        public static int MinLeghthViaClipboard { get; private set; }

        /// <summary> N文字以上の削除にBSではなく Shift+LeftArrow+Deleteを使う </summary>
        public static int NumBSforShiftLeftArrowDelete { get; private set; }

        /// <summary> N文字以上の削除にBSではなく Shift+LeftArrow+Deleteを使うウィンドウ </summary>
        public static string ShiftLeftArrowDeleteClassNames { get; private set; }
        private static HashSet<string> ShiftLeftArrowDeleteClassNamesHash { get; set; } = new HashSet<string>();

        public static bool IsShiftLeftArrowDeleteComboUsed(int numBS, string className)
        {
            return NumBSforShiftLeftArrowDelete > 0 && numBS >= NumBSforShiftLeftArrowDelete && ShiftLeftArrowDeleteClassNamesHash._safeContains(className._toLower());
        }

        /// <summary>BS で全打鍵を取り消すか</summary>
        public static bool RemoveOneStrokeByBackspace { get; set; }

        /// <summary> 同時打鍵ではないテーブルで、ノード重複の警告を表示するか </summary>
        public static bool DuplicateWarningEnabled { get; private set; }

        //-------------------------------------------------------------------------------------
        /// <summary>DecKeyの無限ループを検出する回数</summary>
        public static int DeckeyInfiniteLoopDetectCount { get; private set; }

        /// <summary>キーリピートが発生したことを認識するまでの時間(ミリ秒)</summary>
        public static int KeyRepeatDetectMillisec { get; private set; }

        ///// <summary>キーリピートなどで短時間に大量のキー入力があったら強制的にデコーダをOFFにする</summary>
        //public static bool AutoOffWhenBurstKeyIn { get; private set; }

        //-------------------------------------------------------------------------------------
        // フォントと色
        //-------------------------------------------------------------------------------------
        public static string NormalVkbFontSpec { get; private set; }
        public static string CenterVkbFontSpec { get; private set; }
        public static string VerticalVkbFontSpec { get; private set; }
        public static string HorizontalVkbFontSpec { get; private set; }
        public static string MiniBufVkbFontSpec { get; private set; }
        public static string SpecifiedVerticalFontHeightFactor { get; private set; }
        public static float VerticalFontHeightFactor { get; private set; }

        /// <summary>仮想鍵盤の最上段セルの背景色</summary>
        public static string BgColorTopLevelCells { get; private set; }
        /// <summary>仮想鍵盤の中央寄りセルの背景色</summary>
        public static string BgColorCenterSideCells { get; private set; }
        /// <summary>仮想鍵盤の上下段セルの背景色</summary>
        public static string BgColorHighLowLevelCells { get; private set; }
        /// <summary>仮想鍵盤の中段セルの背景色</summary>
        public static string BgColorMiddleLevelCells { get; private set; }
        /// <summary>次打鍵セルの背景色</summary>
        public static string BgColorNextStrokeCell { get; private set; }

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

        /// <summary>部首合成ヘルプの中央鍵盤背景色 </summary>
        public static string BgColorForBushuCompHelp { get; private set; }

        /// <summary>副ストロークテーブルの中央鍵盤背景色 </summary>
        public static string BgColorForSecondaryTable { get; private set; }

        /// <summary>かな練習モードの中央鍵盤背景色 </summary>
        public static string BgColorForKanaTrainingMode { get; private set; }

        //-------------------------------------------------------------------------------------
        /// <summary>漢直モード標識の文字色</summary>
        public static string KanjiModeMarkerForeColor { get; private set; }

        /// <summary>第2打鍵待ち時のモード標識の文字色</summary>
        public static string KanjiModeMarker2ndForeColor { get; private set; }

        /// <summary>英字モード標識表示色</summary>
        public static string AlphaModeForeColor { get; private set; }

        //-------------------------------------------------------------------------------------
        // 各種時間設定
        //-------------------------------------------------------------------------------------
        /// <summary>入力モードの標識の表示までのインターバル秒数</summary>
        public static int EffectiveKanjiModeMarkerShowIntervalSec => ShowVkbOrMaker ? KanjiModeMarkerShowIntervalSec : -1;
        public static int KanjiModeMarkerShowIntervalSec { get; private set; }

        ///// <summary>漢直モード標識表示のミリ秒</summary>
        //public static int KanjiModeMarkerShowMillisec { get; private set; } = -1;

        /// <summary>英字モード標識表示のミリ秒</summary>
        public static int EffectiveAlphaModeMarkerShowMillisec => ShowVkbOrMaker ? AlphaModeMarkerShowMillisec : 0;
        public static int AlphaModeMarkerShowMillisec { get; private set; }

        /// <summary>モード標識表示のためのループ処理におけるポーリングインターバルミリ秒 (1000固定)</summary>
        public static int ModeMarkerProcLoopPollingMillisec { get; private set; } = 1000;

        //-------------------------------------------------------------------------------------
        /// <summary>Ctrlキーを keybd_event でKEY_UP してから BS を PostMessage するまでの待ち時間(ミリ秒)</summary>
        public static int CtrlKeyUpGuardMillisec { get; private set; }

        /// <summary>BS を PostMessage してから Ctrlキーを keybd_event でKEY_DOWN するまでの待ち時間(ミリ秒)</summary>
        //public static int CtrlKeyDownGuardMillisec { get; private set; }

        /// <summary>Ctrl+Vによる文字列ペーストの前の待ち時間(ミリ秒)</summary>
        public static int PreCtrlVGuardMillisec { get; private set; }

        /// <summary>文字数を減少させるための指数</summary>
        public static double ReductionExponet { get; private set; }

        /// <summary>キー入力後に仮想鍵盤をカレット位置に移動するまでの待ち時間(ミリ秒)</summary>
        public static int VirtualKeyboardMoveGuardMillisec { get; private set; }

        /// <summary>アクティブウィンドウの情報を取得する間隔(ミリ秒)</summary>
        public static int GetActiveWindowInfoIntervalMillisec { get; private set; }

        /// <summary>第2打鍵をキャンセルするまでのする間隔(ミリ秒)</summary>
        public static int CancelSecondStrokeMillisec { get; private set; }

        /// <summary>BSで文字削除してから書き換え文字列を出力するまでの待ち時間(ミリ秒)</summary>
        //public static int AfterBSWaitMillisec { get; private set; }

        //-------------------------------------------------------------------------------------
        // 機能キー割当
        //-------------------------------------------------------------------------------------
        /// <summary>全角変換(モード)を呼び出す打鍵列</summary>
        public static string ZenkakuModeKeySeq { get; set; }
        public static string ZenkakuModeKeySeq_PropName = "zenkakuModeKeySeq";
        public static string ZenkakuModeKeySeq_Preset { get; set; }

        /// <summary>全角変換(1文字)を呼び出す打鍵列</summary>
        public static string ZenkakuOneCharKeySeq { get; set; }
        public static string ZenkakuOneCharKeySeq_PropName = "zenkakuOneCharKeySeq"; 
        public static string ZenkakuOneCharKeySeq_Preset { get; set; }

        /// <summary>次打鍵スルーを呼び出す打鍵列</summary>
        public static string NextThroughKeySeq { get; set; }
        public static string NextThroughKeySeq_PropName = "nextThroughKeySeq"; 
        public static string NextThroughKeySeq_Preset { get; set; }

        /// <summary>履歴検索を呼び出す打鍵列</summary>
        public static string HistoryKeySeq { get; set; }
        public static string HistoryKeySeq_PropName = "historyKeySeq"; 
        public static string HistoryKeySeq_Preset { get; set; }

        /// <summary>履歴検索(1文字)を呼び出す打鍵列</summary>
        public static string HistoryOneCharKeySeq { get; set; }
        public static string HistoryOneCharKeySeq_PropName = "historyOneCharKeySeq"; 
        public static string HistoryOneCharKeySeq_Preset { get; set; }

        /// <summary>履歴検索(1～3文字)を呼び出す打鍵列</summary>
        public static string HistoryFewCharsKeySeq { get; set; }
        public static string HistoryFewCharsKeySeq_PropName = "historyFewCharsKeySeq"; 
        public static string HistoryFewCharsKeySeq_Preset { get; set; }

        /// <summary>交ぜ書きを呼び出す打鍵列</summary>
        public static string MazegakiKeySeq { get; set; }
        public static string MazegakiKeySeq_PropName = "mazegakiKeySeq"; 
        public static string MazegakiKeySeq_Preset { get; set; }

        /// <summary>部首合成を呼び出す打鍵列</summary>
        public static string BushuCompKeySeq { get; set; }
        public static string BushuCompKeySeq_PropName = "bushuCompKeySeq";
        public static string BushuCompKeySeq_Preset { get; set; }

        /// <summary>連想文字検索を呼び出す打鍵列</summary>
        public static string BushuAssocKeySeq { get; set; }
        public static string BushuAssocKeySeq_PropName = "bushuAssocKeySeq";
        public static string BushuAssocKeySeq_Preset { get; set; }

        /// <summary>連想直接変換を呼び出す打鍵列</summary>
        public static string BushuAssocDirectKeySeq { get; set; }
        public static string BushuAssocDirectKeySeq_PropName = "bushuAssocDirectKeySeq";
        public static string BushuAssocDirectKeySeq_Preset { get; set; }

        /// <summary>カタカナ変換(モード)を呼び出す打鍵列</summary>
        public static string KatakanaModeKeySeq { get; set; }
        public static string KatakanaModeKeySeq_PropName = "katakanaModeKeySeq";
        public static string KatakanaModeKeySeq_Preset { get; set; }

        /// <summary>カタカナ変換(一括)を呼び出す打鍵列</summary>
        public static string KatakanaOneShotKeySeq { get; set; }
        public static string KatakanaOneShotKeySeq_PropName = "katakanaOneShotKeySeq";
        public static string KatakanaOneShotKeySeq_Preset { get; set; }

        /// <summary>半角カタカナ変換(一括)を呼び出す打鍵列</summary>
        public static string HankakuKatakanaOneShotKeySeq { get; set; }
        public static string HankakuKatakanaOneShotKeySeq_PropName = "hanKataOneShotKeySeq";
        public static string HankakuKatakanaOneShotKeySeq_Preset { get; set; }

        /// <summary>ブロッカー設定を呼び出す打鍵列</summary>
        public static string BlockerSetterOneShotKeySeq { get; set; }
        public static string BlockerSetterOneShotKeySeq_PropName = "blkSetOneShotKeySeq";
        public static string BlockerSetterOneShotKeySeq_Preset { get; set; }

        public static HashSet<string> FunctionKeySeqSet = new HashSet<string>();

        /// <summary>全エスケープおよび出力文字列検索ブロッカー設定</summary>
        public static string FullEscapeKey { get; set; }

        /// <summary>ストロークヘルプローテーション</summary>
        public static string StrokeHelpRotationKey { get; set; }

        ///// <summary>日付文字列出力ローテーション</summary>
        //public static string DateStringRotationKey { get; set; }

        public static HashSet<int> DecoderSpecialDeckeys { get; set; } = new HashSet<int>();

        /// <summary>配字案内1</summary>
        public static string DefGuide1 { get; set; }

        /// <summary>配字案内2</summary>
        public static string DefGuide2 { get; set; }

        /// <summary>配字案内3</summary>
        public static string DefGuide3 { get; set; }

        /// <summary>デフォルトのストロークヘルプ1</summary>
        public static bool StrokeHelpExtraCharsPosition1 { get; set; }

        /// <summary>デフォルトのストロークヘルプ2</summary>
        public static bool StrokeHelpExtraCharsPosition2 { get; set; }

        /// <summary>デフォルトのストロークヘルプ3</summary>
        public static bool StrokeHelpExtraCharsPosition3 { get; set; }

        //-------------------------------------------------------------------------------------
        // Ctrlキー
        //-------------------------------------------------------------------------------------
        /// <summary>グローバルなコントロールキーを有効にするか </summary>
        public static bool GlobalCtrlKeysEnabled { get; set; }

        /// <summary>左コントロールキーを変換に使う</summary>
        public static bool UseLeftControlToConversion { get; private set; }

        /// <summary>右コントロールキーを変換に使う</summary>
        public static bool UseRightControlToConversion { get; private set; }

        /// <summary>BackSpace に変換するCtrlキー </summary>
        public static string CtrlKeyConvertedToBackSpace { get; private set; }

        /// <summary>Delete に変換するCtrlキー </summary>
        public static string CtrlKeyConvertedToDelete { get; private set; }

        /// <summary>矢印キーに変換するCtrlキー </summary>
        public static string CtrlKeyConvertedToLeftArrow { get; private set; }
        public static string CtrlKeyConvertedToRightArrow { get; private set; }
        public static string CtrlKeyConvertedToUpArrow { get; private set; }
        public static string CtrlKeyConvertedToDownArrow { get; private set; }

        /// <summary>Homeキーに変換するCtrlキー </summary>
        public static string CtrlKeyConvertedToHome { get; private set; }

        /// <summary>Endキーに変換するCtrlキー </summary>
        public static string CtrlKeyConvertedToEnd { get; private set; }

        /// <summary>その他の特殊キーに変換するCtrlキー </summary>
        public static string CtrlKeyConvertedToEsc { get; private set; }
        public static string CtrlKeyConvertedToTab { get; private set; }
        public static string CtrlKeyConvertedToEnter { get; private set; }
        public static string CtrlKeyConvertedToInsert { get; private set; }
        public static string CtrlKeyConvertedToPageUp { get; private set; }
        public static string CtrlKeyConvertedToPageDown { get; private set; }

        /// <summary>ウィンドウClassNameリストを対象リストとして扱うか</summary>
        public static bool UseClassNameListAsInclusion { get; private set; }

        /// <summary>Ctrl修飾キー変換の対象(または対象外)となるウィンドウのClassName</summary>
        public static string CtrlKeyTargetClassNames { get; private set; }
        public static HashSet<string> CtrlKeyTargetClassNamesHash { get; private set; } = new HashSet<string>();

        /// <summary>Ctrl-J を Enter と同じように扱う </summary>
        public static bool UseCtrlJasEnter { get; private set; }

        ///// <summary>Ctrl-M を Enter と同じように扱う </summary>
        //public static bool UseCtrlMasEnter { get; private set; }

        /// <summary>日付に変換するCtrlキー </summary>
        public static string CtrlKeyConvertedToDateString { get; private set; }

        /// <summary>今日の日付を出力フォーマット </summary>
        public static string DateStringFormat { get; private set; }

        //------------------------------------------------------------------------------
        // 履歴
        //------------------------------------------------------------------------------
        public static int HistMaxLength { get; private set; }
        public static int HistKatakanaWordMinLength { get; private set; }
        public static int HistKatakanaWordMaxLength { get; private set; }
        public static int HistKanjiWordMinLength { get; private set; }
        public static int HistKanjiWordMaxLength { get; private set; }
        public static int HistKanjiWordMinLengthEx { get; private set; }
        public static int HistHiraganaKeyLength { get; private set; }
        public static int HistKatakanaKeyLength { get; private set; }
        public static int HistKanjiKeyLength { get; private set; }
        public static int HistRomanKeyLength { get; private set; }
        public static int HistMapKeyMaxLength { get; private set; }             // 変換履歴キーの最大長
        public static int HistMapGobiMaxLength { get; private set; }            // 変換履歴キーに付加できる語尾の最大長

        /// <summary>自動履歴検索</summary>
        public static bool AutoHistSearchEnabled { get; private set; }
        //public static bool HistSearchByCtrlSpace { get; private set; }
        //public static bool HistSearchByShiftSpace { get; private set; }
        /// <summary>Enterで先頭候補を選択</summary>
        public static bool SelectFirstCandByEnter { get; private set; }
        public static bool NewLineWhenHistEnter { get; private set; }
        public static bool HistAllowFromMiddleChar { get; private set; }
        public static int HistDelDeckeyId { get; private set; }
        public static int HistNumDeckeyId { get; private set; }

        public static int HistHorizontalCandMax { get; private set; }
        public static bool HistMoveShortestAt2nd { get; private set; }

        /// <summary>最初の履歴選択から横列候補表示を行うか</summary>
        public static bool ShowHistCandsFromFirst { get; private set; }

        /// <summary>矢印キーで候補を選択</summary>
        public static bool UseArrowKeyToSelectCandidate { get; set; }
        //public static bool HandleShiftSpaceAsNormalSpace { get; set; } = true;
        /// <summary>Tabで候補を選択</summary>
        public static bool SelectHistCandByTab { get; private set; }

        /// <summary>履歴検索&選択するCtrlキー </summary>
        public static string HistorySearchCtrlKey { get; private set; }

        //------------------------------------------------------------------------------
        // スペースキー
        //public static bool UseShiftSpaceAsDeckey49 => MazegakiByShiftSpace;
        //public static bool UseCtrlSpaceKey => HistSearchByCtrlSpace;
        //public static bool UseShiftSpaceAsSpecialDecKey => (HistSearchByShiftSpace || HandleShiftSpaceAsNormalSpace) && !UseShiftSpaceAsDeckey49;
        //public static bool UseShiftSpaceAsSpecialDecKey => (HandleShiftSpaceAsNormalSpace) && !UseShiftSpaceAsDeckey49;

        //------------------------------------------------------------------------------
        // 交ぜ書き
        //------------------------------------------------------------------------------
        //public static bool MazegakiByShiftSpace { get; set; } = true;
        public static bool MazegakiSelectFirstCand { get; set; }
        public static int MazeYomiMaxLen { get; private set; }
        public static int MazeGobiMaxLen { get; private set; }
        //public static int MazeNoIfxGobiMaxLen { get; private set; } = 3;
        public static int MazeGobiLikeTailLen { get; private set; }

        public static bool MazeBlockerTail { get; set; }
        public static bool MazeRemoveHeadSpace { get; set; }

        public static bool MazeRightShiftYomiPos { get; set; }

        /// <summary>無活用語の語尾に漢字を許可する</summary>
        public static bool MazeNoIfxConnectKanji { get; set; }

        /// <summary>無活用語の語尾に任意文字を許可する</summary>
        public static bool MazeNoIfxConnectAny { get; set; }

        /// <summary>交ぜ書き変換での選択を強制的に履歴登録する(除外登録されていたら復活する)</summary>
        public static bool MazeHistRegisterAnyway { get; set; }

        /// <summary>交ぜ書き変換を履歴登録する際の最小長</summary>
        public static int MazeHistRegisterMinLen { get; set; }

        //------------------------------------------------------------------------------
        // 各種変換
        //------------------------------------------------------------------------------
        /// <summary>平仮名⇒カタカナ変換</summary>
        public static bool ConvertShiftedHiraganaToKatakana { get; set; }

        /// <summary>平仮名⇒カタカナ変換を実行するシフト面</summary>
        public static int HiraganaToKatakanaShiftPlane { get; set; }

        /// <summary>通常面の平仮名⇒カタカナ変換を実行</summary>
        public static bool HiraganaToKatakanaNormalPlane { get; set; }

        /// <summary>「。」⇔「．」</summary>
        public static bool ConvertJaPeriod { get; set; }
        /// <summary>「、」⇔「，」</summary>
        public static bool ConvertJaComma { get; set; }

        /// <summary>英大文字入力による英数モード移行が有効か</summary>
        public static bool EisuModeEnabled { get; set; }

        /// <summary>英数モードから履歴検索を呼び出す文字</summary>
        public static string EisuHistSearchChar { get; set; }

        /// <summary>英数モードを自動的に抜けるまでの大文字数</summary>
        public static int EisuExitCapitalCharNum { get; set; }

        /// <summary>英数モードを自動的に抜けるまでのSpaceキー数</summary>
        public static int EisuExitSpaceNum { get; set; }

        /// <summary> 拡張修飾キーを有効にするか</summary>
        public static bool ExtraModifiersEnabled { get; set; }

        /// <summary> 修飾キー定義ファイル</summary>
        public static string ModConversionFile { get; private set; }

        /// <summary> 拡張修飾キー設定ダイアログの幅</summary>
        public static int DlgModConversionWidth { get; set; }

        /// <summary> 拡張修飾キー設定ダイアログの高さ</summary>
        public static int DlgModConversionHeight { get; set; }

        /// <summary> 割り当てキー／機能名カラムの幅</summary>
        public static int AssignedKeyOrFuncNameColWidth { get; set; }

        /// <summary> 割り当てキー／機能説明カラムの幅</summary>
        public static int AssignedKeyOrFuncDescColWidth { get; set; }

        /// <summary> キー／機能名設定ダイアログの副</summary>
        public static int DlgKeywordSelectorWidth { get; set; }

        /// <summary> キー／機能名設定ダイアログの高さ</summary>
        public static int DlgKeywordSelectorHeight { get; set; }

        /// <summary>YAMANOBEアルゴリズムを有効にするか</summary>
        public static bool YamanobeEnabled { get; set; }

        /// <summary>自動部首合成を有効にするか</summary>
        //public static bool AutoBushuComp { get; set; }

        /// <summary>自動部首合成を有効にする最小合成回数</summary>
        public static int AutoBushuCompMinCount { get; set; }

        /// <summary>部首連想直接出力の回数</summary>
        public static int BushuAssocSelectCount { get; set; }

        /// <summary>ローマ字読みによる打鍵ガイドを有効にするか</summary>
        public static bool UpperRomanStrokeGuide { get; set; }

        /// <summary>前打鍵位置の背景色を変えて表示するか</summary>
        public static bool ShowLastStrokeByDiffBackColor { get; set; }

        /// <summary>ローマ字テーブル出力時の部首合成用プレフィックス</summary>
        public static string RomanBushuCompPrefix { get; set; }

        /// <summary>ローマ字テーブル出力時の裏面定義用プレフィックス</summary>
        public static string RomanSecPlanePrefix { get; set; }

        //------------------------------------------------------------------------------
        // SandS
        //------------------------------------------------------------------------------
        /// <summary>SandS を有効にするか</summary>
        public static bool SandSEnabled { get; set; }
        public static string SandSEnabled_PropName = "sandsEnabled";
        public static bool SandSEnabledCurrently { get; set; }
        public static bool SandSEnabledWhenOffMode { get; set; }
        public static string SandSEnabledWhenOffMode_PropName = "sandsEnabledWhenOffMode";

        /// <summary>SandS に割り当てるシフト面</summary>
        public static int SandSAssignedPlane { get; set; }
        public static string SandSAssignedPlane_PropName = "sandsAssignedPlane";

        /// <summary>SandS 時の Space KeyUP を無視するか (Space単打による空白入力をやらない)</summary>
        public static bool OneshotSandSEnabled { get; set; }
        public static string OneshotSandSEnabled_PropName = "oneshotSandSEnabled";
        public static bool OneshotSandSEnabledCurrently { get; set; }

        /// <summary>SandS 時の空白入力またはリピート入力までの時間</summary>
        public static int SandSEnableSpaceOrRepeatMillisec { get; set; }
        public static string SandSEnableSpaceOrRepeatMillisec_PropName = "sandsEnableSpaceOrRepeatMillisec";

        /// <summary>SandS 時の後置シフト出力(疑似同時打鍵サポート)</summary>
        public static bool SandSEnablePostShift { get; set; }
        public static string SandSEnablePostShift_PropName = "sandsEnablePostShift";
        public static bool SandSEnablePostShiftCurrently { get; set; }

        /// <summary>SandS は通常シフトよりも優位か</summary>
        public static bool SandSSuperiorToShift { get; set; }

        /// <summary>Shift+SpaceをSandS として扱う</summary>
        public static bool HandleShiftSpaceAsSandS { get; set; }

        //------------------------------------------------------------------------------
        // 同時打鍵判定
        //------------------------------------------------------------------------------
        /// <summary>同時打鍵判定を行う際の、第１打鍵に許容する最大のリード時間(ミリ秒)<br/>第２打鍵までにこの時間より長くかかったら、第1打鍵は同時とみなさない</summary>
        public static int CombinationKeyMaxAllowedLeadTimeMs { get; set; }
        public static string CombinationKeyMaxAllowedLeadTimeMs_PropName = "combinationMaxAllowedLeadTimeMs";

        /// <summary>同、シフトキーが文字キーだった場合</summary>
        //public static int CombinationKeyMaxAllowedLeadTimeMs2 { get; set; }

        /// <summary>同時打鍵判定を行う際、第2打鍵がシフトキーだった場合に許容する最大のリード時間(ミリ秒)<br/>これにより、シフトキーがその直後の文字キーにかかりやすくなることが期待できる</summary>
        public static int ComboKeyMaxAllowedPostfixTimeMs { get; set; }
        public static string ComboKeyMaxAllowedPostfixTimeMs_PropName = "comboMaxAllowedPostfixTimeMs";

        /// <summary>
        /// 同時打鍵とみなす重複時間<br/>
        /// Nキー同時押しの状態からどれかのキーUPまで重複時間がここで設定した時間(millisec)以上なら、同時打鍵とみなす
        /// </summary>
        public static int CombinationKeyMinOverlappingTimeMs { get; set; }
        public static string CombinationKeyMinOverlappingTimeMs_PropName = "combinationKeyTimeMs";

        /// <summary>
        /// シフトキーが文字キーだった場合の、同時打鍵とみなす重複時間<br/>
        /// Nキー同時押しの状態からどれかのキーUPまで重複時間がここで設定した時間(millisec)以上なら、同時打鍵とみなす
        /// </summary>
        //public static int CombinationKeyMinOverlappingTimeMs2 { get; set; }
        //public static string CombinationKeyMinOverlappingTimeMs2_PropName = "combinationKeyTimeMs2";

        /// <summary>
        /// 3キー以上の同時押しの場合に、同時打鍵とみなす重複時間<br/>
        /// Nキー同時押しの状態からどれかのキーUPまで重複時間がここで設定した時間(millisec)以上なら、同時打鍵とみなす
        /// </summary>
        public static int CombinationKeyMinOverlappingTimeMs3 { get; set; }
        public static string CombinationKeyMinOverlappingTimeMs3_PropName = "combinationKeyTimeMs3";

        /// <summary>２文字目以降についてのみ同時打鍵の重複時間チェックを行う</summary>
        public static bool CombinationKeyMinTimeOnlyAfterSecond { get; set; }
        public static string CombinationKeyMinTimeOnlyAfterSecond_PropName = "combinationKeyTimeOnlyAfterSecond";

        /// <summary>同時打鍵チェック用のタイマーを使用する</summary>
        public static bool UseCombinationKeyTimer1 { get; set; }
        public static string UseCombinationKeyTimer1_PropName = "useCombinationKeyTimer1";
        public static bool UseCombinationKeyTimer2 { get; set; }
        public static string UseCombinationKeyTimer2_PropName = "useCombinationKeyTimer2";

        ///// <summary>同時打鍵とみなす重複率<br/>第１打鍵と第２打鍵の重複時間が第２打鍵の時間に対してここで設定したパーセンテージを超えたら、同時打鍵とみなす</summary>
        //public static int CombinationKeyTimeRate { get; set; }

        /// <summary>同時打鍵キーとして使う「無変換」や「変換」を単打キーとしても使えるようにする</summary>
        public static bool UseComboExtModKeyAsSingleHit { get; set; }
        public static string UseComboExtModKeyAsSingleHit_PropName = "useComboExtModKeyAsSingleHit";

        /// <summary>同時打鍵シフトキーがUPされた後、後置シフトを無効にする時間(ミリ秒)。<br/>
        /// つまり、この時間帯に打鍵された文字キーは、その後どんな短い間隔でシフトキーが押されても単打扱いとなる</summary>
        public static int ComboDisableIntervalTimeMs { get; set; }
        public static string ComboDisableIntervalTimeMs_PropName = "comboDisableIntervalTimeMs";

        /// <summary>同時打鍵よりも順次打鍵のほうを優先させる文字列の並び</summary>
        public static string SequentialPriorityWords { get; set; }
        public static string SequentialPriorityWords_PropName = "sequentialPriorityWords";

        /// <summary>優先される順次打鍵以外の3キー同時打鍵なら無条件に判定</summary>
        public static bool ThreeKeysComboUnconditional { get; set; }
        public static string ThreeKeysComboUnconditional_PropName = "threeKeysComboUnconditional";

        /// <summary>同時打鍵よりも順次打鍵のほうを優先させる文字列の集合</summary>
        public static HashSet<string> SequentialPriorityWordSet { get; } = new HashSet<string>();

        /// <summary>文字キーのみの同時打鍵組合せの場合は、被覆Comboとするか</summary>
        public static bool OnlyCharKeysComboShouldBeCoveringCombo { get; set; }
        public static string OnlyCharKeysComboShouldBeCoveringCombo_PropName = "onlyCharKeysComboShouldBeCoveringCombo";

        /// <summary>同時打鍵よりも順次打鍵のほうを優先させる文字列に対するキーコード列の集合</summary>
        public static HashSet<string> SequentialPriorityWordKeyStringSet { get; } = new HashSet<string>();

        /// <summary>同時打鍵を優先させる先頭キーコード列の集合</summary>
        public static HashSet<string> ThreeKeysComboPriorityHeadKeyStringSet { get; } = new HashSet<string>();

        /// <summary>同時打鍵を優先させる末尾キーコード列の集合</summary>
        public static HashSet<string> ThreeKeysComboPriorityTailKeyStringSet { get; } = new HashSet<string>();

        /// <summary>Spaceまたは機能キーのシフトキーがきたら、使い終わったキー(comboListにたまっているキー)を破棄する</summary>
        public static bool AbandonUsedKeysWhenSpecialComboShiftDown { get; set; } = true;

        /// <summary>3番目以降の同時打鍵キーは最初に解放される必要があるか<br/>
        /// true の場合は、3打鍵同時の場合に、3番目に押されたキーが最初に解放された場合に限り、同時打鍵と判定する。
        /// ⇒ が、これはイマイチ(確実に3番目のキーを最初に解放するのが難しい)ので、廃案とする
        /// </summary>
        //public static bool ThirdComboKeyNeedToBeReleaseFirst { get; set; }

        //------------------------------------------------------------------------------
        // IME連携
        //------------------------------------------------------------------------------
        /// <summary> IMEの状態に連携してON/OFFする </summary>
        public static bool ImeCooperationEnabled { get; set; }
        public static string ImeCooperationEnabled_PropName = "imeCooperationEnabled";

        /// <summary> IMEにカタカナを送るときはひらがなに変換する </summary>
        public static bool ImeKatakanaToHiragana { get; set; }
        public static string ImeKatakanaToHiragana_PropName = "imeKatakanaToHiragana";

        /// <summary> IMEに対してローマ字で送信する </summary>
        public static bool ImeSendInputInRoman { get; set; }
        public static string ImeSendInputInRoman_PropName = "imeSendInputInRoman";

        /// <summary> IMEに対してカナで送信する </summary>
        public static bool ImeSendInputInKana { get; set; }
        public static string ImeSendInputInKana_PropName = "imeSendInputInKana";

        /// <summary>IMEにUnicodeで文字送出する対象となるウィンドウのClassName</summary>
        public static string ImeUnicodeClassNames { get; private set; }
        public static HashSet<string> ImeUnicodeClassNamesHash { get; private set; } = new HashSet<string>();

        /// <summary>かな入力練習モードか</summary>
        public static bool KanaTrainingMode { get; set; }

        //------------------------------------------------------------------------------
        // 書き換えシステム
        //------------------------------------------------------------------------------
        /// <summary>遅延許容時間の適用対象となる前置書き換え対象文字集合</summary>
        public static string PreRewriteTargetChars { get; set; } = "";
        public static string PreRewriteTargetChars_PropName = "preRewriteTargetChars";

        /// <summary>指定の文字集合に対する前置書き換え時の遅延許容時間</summary>
        public static int PreRewriteAllowedDelayTimeMs { get; set; }
        public static string PreRewriteAllowedDelayTimeMs_PropName = "preRewriteAllowedDelayTimeMs";

        /// <summary>上記以外の文字に対する前置書き換え時の遅延許容時間</summary>
        public static int PreRewriteAllowedDelayTimeMs2 { get; set; }
        public static string PreRewriteAllowedDelayTimeMs2_PropName = "preRewriteAllowedDelayTimeMs2";

        /// <summary>かな入力練習モードのときに無視する前置書き換え対象文字</summary>
        //public static string PreRewriteCharsIgnoredWhenTrainingMode { get; set; } = "";

        /// <summary>かな入力練習モードのときの書き換え対象文字の出力待ち時間</summary>
        public static int PreRewriteWaitTimeMsWhenTrainingMode { get; set; }
        public static string PreRewriteWaitTimeMsWhenTrainingMode_PropName = "preRewriteWaitTimeMsWhenTrainingMode";

        //------------------------------------------------------------------------------
        // ウィンドウClassName
        //------------------------------------------------------------------------------
        /// <summary>ウィンドウClassNameごとの設定</summary>
        public class WindowsClassSettings
        {
            public int[] ValidCaretMargin;
            public int[] CaretOffset;
            public int[] VkbFixedPos;
            public int CtrlUpWaitMillisec = -1;
            public int CtrlDownWaitMillisec = -1;
            public int AfterBSWaitMillisec = 0;
        }

        private static Dictionary<string, WindowsClassSettings> winClassSettings = new Dictionary<string, WindowsClassSettings>();

        public static WindowsClassSettings GetWinClassSettings(string name)
        {
            if (name._notEmpty()) {
                var lowerName = name._toLower();
                foreach (var pair in winClassSettings) {
                    if (pair.Key._notEmpty() &&
                        ((pair.Key.EndsWith("$") && lowerName == pair.Key.TrimEnd(Helper.Array('$'))) ||
                        lowerName.StartsWith(pair.Key))) {
                        return pair.Value;
                    }
                }
            }
            return null;
        }

        //------------------------------------------------------------------------------
        /// <summary>デコーダ用の設定辞書</summary>
        private static Dictionary<string, string> DecoderSettings { get; set; } = new Dictionary<string, string>();

        private static Dictionary<string, string> specificDecoderSettings { get; set; } = new Dictionary<string, string>();

        public static void ClearSpecificDecoderSettings()
        {
            logger.DebugH(() => $"CALLED");
            specificDecoderSettings.Clear();
        }

        public static string SerializedDecoderSettings =>
            DecoderSettings.Select(pair => $"{pair.Key}={(specificDecoderSettings.ContainsKey(pair.Key) ? specificDecoderSettings[pair.Key] : pair.Value)}")._join("\n");

        //------------------------------------------------------------------------------
        public static string GetString(string attr, string defval = "")
        {
            return GetInternalValue(attr)._orElse(
                () => UserKanchokuIni.Singleton.GetString(attr)._orElse(
                    () => KanchokuIni.Singleton.GetString(attr, defval)));
        }

        // kanchoku.user.ini が存在しない時のデフォルト値を設定できる(デフォルトの辞書ファイルなどを設定して、それが存在しなくてもエラーにしない処理をするため)
        public static string GetStringEx(string attr, string defvalInit, string defval = "")
        {
            return GetInternalValue(attr)._orElse(
                () => UserKanchokuIni.Singleton.GetStringEx(attr, defvalInit)._orElse(
                    () => KanchokuIni.Singleton.GetString(attr, defval)));
        }

        public static string GetString(string attr, string attrOld, string defval)
        {
            return GetInternalValue(attr)._orElse(
                () => UserKanchokuIni.Singleton.GetString(attr)._orElse(
                    () => UserKanchokuIni.Singleton.GetString(attrOld))._orElse(
                        () => KanchokuIni.Singleton.GetString(attrOld, defval)));
        }

        // kanchoku.user.ini が存在しない時のデフォルト値を設定できる(デフォルトの辞書ファイルなどを設定して、それが存在しなくてもエラーにしない処理をするため)
        public static string GetStringEx(string attr, string attrOld, string defvalInit, string defval)
        {
            return GetInternalValue(attr)._orElse(
                () => UserKanchokuIni.Singleton.GetStringEx(attr, defvalInit)._orElse(
                    () => UserKanchokuIni.Singleton.GetStringEx(attrOld, defvalInit))._orElse(
                        () => KanchokuIni.Singleton.GetString(attrOld, defval)));
        }

        public static string GetStringFromSection(string section, string attr, string defval = "")
        {
            return GetInternalValue(attr)._orElse(
                () => UserKanchokuIni.Singleton.GetStringFromSection(section, attr)._orElse(
                    () => KanchokuIni.Singleton.GetStringFromSection(section, attr, defval)));
        }

        public static int GetLogLevel()
        {
            return GetString("logLevel")._parseInt(Logger.LogLevelWarnH)._lowLimit(0)._highLimit(Logger.LogLevelTrace);   // デフォルトは WarnH
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

        private static string setDecoderSetting(string attr, string val)
        {
            return DecoderSettings[attr] = val;
        }

        // KeySeq 設定
        private static string addDecoderKeySeqSetting(string attr)
        {
            var keyseq = GetString(attr);
            var origKeySeq = keyseq;
            int idx = DecoderKeyVsVKey.GetFuncKeyIndexByName(keyseq);
            if (idx >= 0) {
                // "nfer" や "xfer" などの名前も使用可能とする
                keyseq = $"X{idx}";
            }
            setDecoderSetting(attr, keyseq);
            if (keyseq._safeLength() > 1 && (keyseq[0] == 'X' || keyseq[0] == 'x')) {
                int ix = keyseq._safeSubstring(1)._parseInt(-1);
                if (ix >= 0) ExtraModifiers.AddExModVkeyAssignedForDecoderFuncByIndex(ix);
            }
            return origKeySeq;
        }

        // kanchoku.user.ini が存在しない時のデフォルト値を設定できる(デフォルトの辞書ファイルなどを設定して、それが存在しなくてもエラーにしない処理をするため)
        private static string addDecoderSettingEx(string attr, string defvalInit, string defval = "")
        {
            return DecoderSettings[attr] = GetStringEx(attr, defvalInit, defval);
        }

        private static string addDecoderSetting(string attr, string attrOld, string defval)
        {
            return DecoderSettings[attr] = GetString(attr, attrOld, defval);
        }

        // kanchoku.user.ini が存在しない時のデフォルト値を設定できる(デフォルトの辞書ファイルなどを設定して、それが存在しなくてもエラーにしない処理をするため)
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

        private static int addDecoderSetting(string attr, int defval, int lowLimit, int highLimit)
        {
            int result = GetString(attr)._parseInt(defval)._lowLimit(lowLimit)._highLimit(highLimit);
            DecoderSettings[attr] = GetString(attr, $"{result}");
            return result;
        }

        private static int addDecoderSetting(string attr, string attrOld, int defval, int lowLimit = 0)
        {
            int result = GetString(attr, attrOld, "")._parseInt(defval)._lowLimit(lowLimit);
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
            var rootDir = KanchokuIni.Singleton.KanchokuDir;
            char lastChar = rootDir?.LastOrDefault() ?? '\0';
            if (lastChar != '\\' && lastChar != '/') rootDir = rootDir + "\\";
            return Helper.GetFiles(rootDir, filePattern).Select(x => x.Replace(rootDir, ""))._join("|");
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// kanchoku.ini からデバッグ用設定を読み込む
        /// </summary>
        /// <returns></returns>
        public static void ReadIniFileForDebug()
        {
            logger.InfoH(() => $"CALLED");

            LogLevel = GetLogLevel();
            LoggingDecKeyInfo = GetString("loggingDecKeyInfo")._parseBool();
            LoggingActiveWindowInfo = GetString("loggingActiveWindowInfo")._parseBool();
            LoggingVirtualKeyboardInfo = GetString("loggingVirtualKeyboardInfo")._parseBool();
            LoggingTableFileInfo = GetString("loggingTableFileInfo")._parseBool();
            MultiAppEnabled = IsMultiAppEnabled();
            WarnThresholdKeyQueueCount = GetString("warnThresholdKeyQueueCount")._parseInt(6);
            OutputDebugTableFiles = GetString("outputDebugTableFiles")._parseBool();
            ShowHiddleFolder = GetString("showHiddleFolder")._parseBool();
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// kanchoku.ini からキーボードと英字配列の設定を読み込む
        /// </summary>
        /// <returns></returns>
        public static void ReadIniFileForKeyboardAndCharLayout()
        {
            // キーボードファイル設定
            //KeyboardFile = GetString("keyboard", "JP");
            KeyboardFile = GetString("keyboard", "106.key");

            // 英数文字テーブルファイル
            var charsDefFile = GetString("charsDefFile");
            if (charsDefFile._isEmpty()) {
                var kbName = KeyboardFile._split('.')._getNth(0);
                if (kbName._notEmpty() && (kbName._toLower() == "jp" || kbName._toLower() == "us")) kbName = null;
                if (kbName._notEmpty()) charsDefFile = $"chars.{kbName}.txt";
            }
            //CharsDefFile = charsDefFile._notEmpty() ? KeyboardFileDir._joinPath(charsDefFile) : "";
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// kanchoku.ini から各種設定を読み込む
        /// </summary>
        /// <param name="bFirst">true: 一回目、 false: 二回目</param>
        /// <returns></returns>
        public static bool ReadIniFile(bool bFirst)
        {
            logger.InfoH(() => $"ENTER: {(bFirst ? "First" : "Second")}");

            if (bFirst) {
                // 1回目はテーブルファイルによる設定をクリア（iniファイルによる設定だけを読み込む）
                internalSetValues.Clear();
                DefGuide1 = "";
                DefGuide2 = "";
                StrokeHelpExtraCharsPosition1 = false;
                StrokeHelpExtraCharsPosition2 = false;
                SequentialPriorityWordSet.Clear();
                SequentialPriorityWordKeyStringSet.Clear();

                // テーブルファイルで設定される機能呼び出しキー設定
                ZenkakuModeKeySeq_Preset = "";
                ZenkakuOneCharKeySeq_Preset = "";
                NextThroughKeySeq_Preset = "";
                HistoryKeySeq_Preset = "";
                HistoryOneCharKeySeq_Preset = "";
                HistoryFewCharsKeySeq_Preset = "";
                MazegakiKeySeq_Preset = "";
                BushuCompKeySeq_Preset = "";
                BushuAssocKeySeq_Preset = "";
                BushuAssocDirectKeySeq_Preset = "";
                KatakanaModeKeySeq_Preset = "";
                KatakanaOneShotKeySeq_Preset = "";
                HankakuKatakanaOneShotKeySeq_Preset = "";
                BlockerSetterOneShotKeySeq_Preset = "";
            } else if (internalSetValues._isEmpty()) {
                // 1回目と2回目の間で、テーブルファイルの読み込みが行われている
                // internalSetValues に何も値がセットされていなければ、何もせずに返る
                logger.InfoH("LEAVE: do nothing");
                return false;
            }

            //-------------------------------------------------------------------------------------
            // 基本設定
            DeckeyInfiniteLoopDetectCount = GetString("deckeyInfiniteLoopDetectCount")._parseInt(1000)._lowLimit(100);
            KeyRepeatDetectMillisec = GetString("keyRepeatDetectMillisec")._parseInt(100)._lowLimit(50);
            //AutoOffWhenBurstKeyIn = GetString("autoOffWhenBurstKeyIn")._parseBool();
            SplashWindowShowDuration = GetString("splashWindowShowDuration")._parseInt(60)._lowLimit(0);
            ConfirmOnClose = GetString("confirmOnClose")._parseBool(true);
            SuspendByPauseKey = GetString("suspendByPauseKey")._parseBool(false);
            ShowEisuVkb = GetString("showEisuVkb")._parseBool(false);
            OpenSettingsDlgWhenIconClicked = GetString("openSettingsDlgWhenIconClicked")._parseBool(false);

            //-------------------------------------------------------------------------------------
            // デバッグ用設定
            ReadIniFileForDebug();
            //LogLevel = GetLogLevel();
            //LoggingDecKeyInfo = GetString("loggingDecKeyInfo")._parseBool();
            ////LoggingActiveWindowInfo = GetString("loggingActiveWindowInfo")._parseBool();
            //LoggingVirtualKeyboardInfo = GetString("loggingVirtualKeyboardInfo")._parseBool();
            //MultiAppEnabled = IsMultiAppEnabled();
            //WarnThresholdKeyQueueCount = GetString("warnThresholdKeyQueueCount")._parseInt(6);
            //OutputDebugTableFiles = GetString("outputDebugTableFiles")._parseBool();
            //ShowHiddleFolder = GetString("showHiddleFolder")._parseBool();

            //-------------------------------------------------------------------------------------
            // キーボードと英数字配列設定
            ReadIniFileForKeyboardAndCharLayout();

            // CtrlやAltのようなショートカット(アクセラレータ)キーに対する英字変換を有効にするか
            ShortcutKeyConversionEnabled = GetString("shortcutKeyConversionEnabled")._parseBool(true);

            //-------------------------------------------------------------------------------------
            // ファイル設定
            StrokeHelpFile = GetString("strokeHelpFile", "stroke-help.txt");

            //-------------------------------------------------------------------------------------
            // 漢直モードトグルキー
            ActiveKey = (uint)GetString("unmodifiedHotKey")._parseHex(0)._lowLimit(0);
            ActiveKeyWithCtrl = (uint)GetString("hotKey")._parseHex(0)._lowLimit(0);
            ActiveKeyWithCtrl2 = (uint)GetString("hotKey2")._parseHex(0)._lowLimit(0);
            if (ActiveKey == 0 && ActiveKeyWithCtrl == 0) ActiveKeyWithCtrl = 0xdc;         // 0xdc = ￥

            SelectedTableActivatedWithoutCtrl = GetString("selectedTableActivatedWithoutCtrl")._parseInt(0)._lowLimit(0)._highLimit(3);
            SelectedTableActivatedWithCtrl = GetString("selectedTableActivatedWithCtrl")._parseInt(0)._lowLimit(0)._highLimit(3);
            SelectedTableActivatedWithCtrl2 = GetString("selectedTableActivatedWithCtrl2")._parseInt(0)._lowLimit(0)._highLimit(3);

            // 漢直モードOFFキー
            DeactiveKey = (uint)GetString("unmodifiedOffHotKey")._parseHex(0)._lowLimit(0);
            DeactiveKeyWithCtrl = (uint)GetString("offHotKey")._parseHex(0)._lowLimit(0);

            // デコーダON/OFF系機能の呼び出し
            if (ActiveKey != 0) {
                if (DeactiveKey == 0) {
                    KeyComboRepository.AddDecKeyAndCombo(DecoderKeys.TOGGLE_DECKEY, 0, DecoderKeyVsVKey.GetDecKeyFromVKey(ActiveKey));
                } else {
                    KeyComboRepository.AddDecKeyAndCombo(DecoderKeys.ACTIVE_DECKEY, 0, DecoderKeyVsVKey.GetDecKeyFromVKey(ActiveKey));
                    KeyComboRepository.AddDecKeyAndCombo(DecoderKeys.DEACTIVE_DECKEY, 0, DecoderKeyVsVKey.GetDecKeyFromVKey(DeactiveKey));
                }
            }
            if (ActiveKeyWithCtrl != 0) {
                if (DeactiveKeyWithCtrl == 0) {
                    // Ctrlありの場合はカレットへの追従を再開する
                    KeyComboRepository.AddDecKeyAndCombo(DecoderKeys.MODE_TOGGLE_FOLLOW_CARET_DECKEY, KeyModifiers.MOD_CONTROL, DecoderKeyVsVKey.GetDecKeyFromVKey(ActiveKeyWithCtrl));
                } else {
                    KeyComboRepository.AddDecKeyAndCombo(DecoderKeys.ACTIVE_DECKEY, KeyModifiers.MOD_CONTROL, DecoderKeyVsVKey.GetDecKeyFromVKey(ActiveKeyWithCtrl));
                    KeyComboRepository.AddDecKeyAndCombo(DecoderKeys.DEACTIVE_DECKEY, KeyModifiers.MOD_CONTROL, DecoderKeyVsVKey.GetDecKeyFromVKey(DeactiveKeyWithCtrl));
                }
            }
            if (ActiveKeyWithCtrl2 != 0) {
                KeyComboRepository.AddDecKeyAndCombo(DecoderKeys.MODE_TOGGLE_FOLLOW_CARET_DECKEY2, KeyModifiers.MOD_CONTROL, DecoderKeyVsVKey.GetDecKeyFromVKey(ActiveKeyWithCtrl2));
            }

            //-------------------------------------------------------------------------------------
            // 詳細設定

            // 文字送出時にコピー&ペーストを行う文字数の閾値
            MinLeghthViaClipboard = GetString("minLeghthViaClipboard")._parseInt(0)._lowLimit(0);

            // N文字以上の削除にBSではなく Shift+LeftArrow+Deleteを使う
            NumBSforShiftLeftArrowDelete = GetString("numBSforShiftLeftArrowDelete")._parseInt(0)._lowLimit(0);

            // N文字以上の削除にBSではなく Shift+LeftArrow+Deleteを使うウィンドウ
            ShiftLeftArrowDeleteClassNames = GetString("shiftLeftArrowDeleteClassNames").Trim();
            ShiftLeftArrowDeleteClassNamesHash = new HashSet<string>(ShiftLeftArrowDeleteClassNames._toLower()._split('|'));

            // 自身以外のキーボードフックツールからの出力を無視する
            IgnoreOtherHooker = GetString("ignoreOtherHooker")._parseBool(true);

            // 同時打鍵ではないテーブルで、ノード重複の警告を表示するか
            DuplicateWarningEnabled = GetString("duplicateWarningEnabled")._parseBool(false);

            // 一時的な仮想鍵盤の表示/非表示
            VkbShowHideTemporaryKey = GetString("vkbShowHideTemporaryKey", "").Trim();
            if (VkbShowHideTemporaryKey._notEmpty()) KeyComboRepository.AddCtrlDeckeyAndCombo(VkbShowHideTemporaryKey, DecoderKeys.VKB_SHOW_HIDE_DECKEY, DecoderKeys.VKB_SHOW_HIDE_DECKEY);

            //-------------------------------------------------------------------------------------
            // フォントと色の設定
            NormalVkbFontSpec = GetString("normalFont", "Meiryo | 10");
            CenterVkbFontSpec = GetString("centerFont", "@Meiryo | 9");
            VerticalVkbFontSpec = GetString("verticalFont", "@Meiryo | 9");
            HorizontalVkbFontSpec = GetString("horizontalFont", "Meiryo | 9");
            MiniBufVkbFontSpec = GetString("minibufFont", "Meiryo | 9");
            SpecifiedVerticalFontHeightFactor = GetString("verticalFontHeightFactor");
            {
                var factor = SpecifiedVerticalFontHeightFactor._parseDouble();
                if (factor._isNaN()) {
                    SpecifiedVerticalFontHeightFactor = "";
                    var vfont = VerticalVkbFontSpec._toLower();
                    if (vfont.StartsWith("@meiryo") || vfont.StartsWith("meiryo") || vfont.StartsWith("@yu ") || vfont.StartsWith("yu ") ||
                        vfont.StartsWith("@メイリオ") || vfont.StartsWith("メイリオ") || vfont.StartsWith("@游") || vfont.StartsWith("游")) {
                        VerticalFontHeightFactor = 0.7f;
                    } else {
                        VerticalFontHeightFactor = 1.0f;
                    }
                } else {
                    VerticalFontHeightFactor = (float)factor;
                }
            }

            //-------------------------------------------------------------------------------------
            VirtualKeyboardShowStrokeCount = GetString("vkbShowStrokeCount")._parseInt(1);
            ShowVkbOrMaker = GetString("showVkbOrMaker")._parseBool(true);

            //-------------------------------------------------------------------------------------
            VirtualKeyboardOffsetX = GetString("vkbOffsetX")._parseInt(2);
            VirtualKeyboardOffsetY = GetString("vkbOffsetY")._parseInt(2);

            var fixedPos = GetString("vkbFixedPos").Trim()._split(',').Select(x => x._parseInt(-1)).ToArray();
            VirtualKeyboardFixedPosX = fixedPos._getNth(0, -1);
            VirtualKeyboardFixedPosY = fixedPos._getNth(1, -1);
            FixedPosClassNames = GetString("fixedPosClassNames", "").Trim();
            FixedPosClassNamesHash = new HashSet<string>(FixedPosClassNames._toLower()._split('|'));

            //DisplayScale = GetString("displayScale")._parseDouble(1.0)._lowLimit(1.0);

            BgColorTopLevelCells = GetString("bgColorTopLevelCells", "GhostWhite");
            BgColorCenterSideCells = GetString("bgColorCenterSideCells", "FloralWhite");
            BgColorHighLowLevelCells = GetString("bgColorHighLowLevelCells", "LightCyan");
            BgColorMiddleLevelCells = GetString("bgColorMiddleLevelCells", "PaleGreen");
            BgColorNextStrokeCell = GetString("bgColorNextStrokeCell", "LightPink");

            BgColorOnWaiting2ndStroke = GetString("bgColorOnWaiting2ndStroke", "Yellow");
            BgColorForMazegaki = GetString("bgColorForMazegaki", "Plum");
            BgColorForHistOrAssoc = GetString("bgColorForHistOrAssoc", "PaleTurquoise");
            BgColorForFirstCandidate = GetString("bgColorForFirstCandidate", "PaleGreen");
            BgColorOnSelected = GetString("bgColorOnSelected", "LightPink");
            BgColorForBushuCompHelp = GetString("bgColorForBushuCompHelp", "LightCyan");
            BgColorForSecondaryTable = GetString("bgColorForSecondaryTable", "LightGreen");
            BgColorForKanaTrainingMode = GetString("bgColorForKanaTrainingMode", "LightPink");

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
            //CtrlKeyDownGuardMillisec = GetString("ctrlKeyDownGuardMillisec")._parseInt(0)._lowLimit(0);     // これが 0 より大きいとCTRLキーDOWNと誤認識される可能性が高まる
            PreCtrlVGuardMillisec = GetString("preWmCharGuardMillisec")._parseInt(30)._lowLimit(0);
            //AfterBSWaitMillisec = GetString("afterBSWaitMillisec")._parseInt(0)._lowLimit(0);
            ReductionExponet = GetString("reductionExponent")._parseDouble(0.7)._lowLimit(0.5);

            VirtualKeyboardMoveGuardMillisec = GetString("virtualKeyboardMoveGuardMillisec")._parseInt(500)._lowLimit(0);
            GetActiveWindowInfoIntervalMillisec = GetString("activeWindowInfoIntervalMillisec")._parseInt(200)._lowLimit(100);

            CancelSecondStrokeMillisec = GetString("cancelSecondStrokeMillisec")._parseInt(0);

            //-------------------------------------------------------------------------------------
            // Ctrlキー変換
            GlobalCtrlKeysEnabled = GetString("globalCtrlKeysEnabled")._parseBool(false);

            CtrlKeyConvertedToBackSpace = GetString("ctrlKeyToBackSpace", "#H");
            CtrlKeyConvertedToDelete = GetString("ctrlKeyToDelete", "#D");
            CtrlKeyConvertedToLeftArrow = GetString("ctrlKeyToLeftArrowKey", "#B");
            CtrlKeyConvertedToRightArrow = GetString("ctrlKeyToRightArrowKey", "#F");
            CtrlKeyConvertedToUpArrow = GetString("ctrlKeyToUpArrowKey", "#P");
            CtrlKeyConvertedToDownArrow = GetString("ctrlKeyToDownArrowKey", "#N");
            CtrlKeyConvertedToHome = GetString("ctrlKeyToHome", "#A");
            CtrlKeyConvertedToEnd = GetString("ctrlKeyToEnd", "#E");
            CtrlKeyConvertedToEsc = GetString("ctrlKeyToEsc", "#OEM4");
            CtrlKeyConvertedToTab = GetString("ctrlKeyToTab", "#I");
            CtrlKeyConvertedToEnter = GetString("ctrlKeyToEnter", "#M");
            CtrlKeyConvertedToInsert = GetString("ctrlKeyToInsert", "#");
            CtrlKeyConvertedToPageUp = GetString("ctrlKeyToPageUp", "#");
            CtrlKeyConvertedToPageDown = GetString("ctrlKeyToPageDown", "#");
            CtrlKeyConvertedToDateString = GetString("ctrlKeyToDateString", "#");

            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToBackSpace, DecoderKeys.BS_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToDelete, DecoderKeys.DEL_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToLeftArrow, DecoderKeys.LEFT_ARROW_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToRightArrow, DecoderKeys.RIGHT_ARROW_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToUpArrow, DecoderKeys.UP_ARROW_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToDownArrow, DecoderKeys.DOWN_ARROW_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToHome, DecoderKeys.HOME_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToEnd, DecoderKeys.END_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToEsc, DecoderKeys.ESC_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToTab, DecoderKeys.TAB_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToEnter, DecoderKeys.ENTER_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToInsert, DecoderKeys.INS_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToPageUp, DecoderKeys.PAGE_UP_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToPageDown, DecoderKeys.PAGE_DOWN_DECKEY, 0);
            KeyComboRepository.AddCtrlDeckeyFromCombo(CtrlKeyConvertedToDateString, DecoderKeys.DATE_STRING_ROTATION_DECKEY, DecoderKeys.DATE_STRING_UNROTATION_DECKEY);

            UseLeftControlToConversion = GetString("useLeftControlToConversion")._parseBool(true);
            UseRightControlToConversion = GetString("useRightControlToConversion")._parseBool(false);
            UseClassNameListAsInclusion = GetString("useClassNameListAsInclusion")._parseBool(false);
            CtrlKeyTargetClassNames = GetString("ctrlKeyTargetClassNames", "ctrlKeyTargetlassNames", "").Trim();
            CtrlKeyTargetClassNamesHash = new HashSet<string>(CtrlKeyTargetClassNames._toLower()._split('|'));

            UseCtrlJasEnter = GetString("useCtrlJasEnter")._parseBool(false);
            //UseCtrlMasEnter = GetString("useCtrlMasEnter")._parseBool(false);

            //ConvertCtrlSemiColonToDate = GetString("convertCtrlSemicolonToDate")._parseBool(true);
            DateStringFormat = GetString("dateStringFormat", "yyyy/M/d|yyyyMMdd");

            //-------------------------------------------------------------------------------------
            // その他変換・機能
            ConvertShiftedHiraganaToKatakana = GetString("convertShiftedHiraganaToKatakana", "shiftKana", "")._parseBool(false);  // 平仮名をカタカナに変換する
            ModConversionFile = GetString("modConversionFile");
            bool isModConversionFileEmpty = ModConversionFile._isEmpty();
            if (isModConversionFileEmpty) { ModConversionFile = "mod-conversion.txt"; }
            DlgModConversionWidth = GetString("dlgModConversionWidth")._parseInt(0);
            DlgModConversionHeight = GetString("dlgModConversionHeight")._parseInt(0);
            DlgKeywordSelectorWidth = GetString("dlgKeywordSelectorWidth")._parseInt(0);
            DlgKeywordSelectorHeight = GetString("dlgKeywordSelectorHeight")._parseInt(0);
            AssignedKeyOrFuncNameColWidth = GetString("assignedKeyOrFuncNameColWidth")._parseInt(180);
            AssignedKeyOrFuncDescColWidth = GetString("assignedKeyOrFuncDescColWidth")._parseInt(290);
            ExtraModifiersEnabled = GetString("extraModifiersEnabled")._parseBool(!isModConversionFileEmpty);   // 拡張修飾キーを有効にするか
            UpperRomanStrokeGuide = GetString("upperRomanStrokeGuide")._parseBool(false);       // ローマ字読みによるストロークガイドを有効にするか
            ShowLastStrokeByDiffBackColor = GetString("showLastStrokeByDiffBackColor")._parseBool(false); // 前打鍵位置の背景色を変えて表示するか

            //-------------------------------------------------------------------------------------
            // 辞書保存時間
            SaveDictsIntervalTime = GetString("saveDictsIntervalTime")._parseInt(-60);     // 辞書保存インターバルタイム(分)
            SaveDictsCalmTime = GetString("saveDictsCalmTime")._parseInt(1);                 // 辞書保存に適した平穏な時間(分)

            //-------------------------------------------------------------------------------------
            // SandS
            SandSEnabled = GetString(SandSEnabled_PropName)._parseBool(false);                         // SandS を有効にするか
            SandSEnabledCurrently = SandSEnabled;
            SandSEnabledWhenOffMode = GetString(SandSEnabledWhenOffMode_PropName)._parseBool(false);   // 漢直OFFの時もSandS を有効にするか
            SandSAssignedPlane = GetString(SandSAssignedPlane_PropName)._parseInt(2, 0)._highLimit(7); // SandS に割り当てるシフト面
            OneshotSandSEnabled= GetString(OneshotSandSEnabled_PropName, "ignoreSpaceUpOnSandS", "")._parseBool(false);    // SandSのワンショットシフトを有効にするか
            OneshotSandSEnabledCurrently = OneshotSandSEnabled;
            SandSEnableSpaceOrRepeatMillisec = GetString(SandSEnableSpaceOrRepeatMillisec_PropName)._parseInt(500);        // SandS 時の空白入力またはリピート入力までの時間
            SandSEnablePostShift = GetString(SandSEnablePostShift_PropName)._parseBool(false);         // SandS 時の後置シフト出力(疑似同時打鍵サポート)
            SandSEnablePostShiftCurrently = SandSEnablePostShift;

            //-------------------------------------------------------------------------------------
            // 同時打鍵
            //CombinationKeyTimeRate = GetString("combinationKeyTimeRate")._parseInt(0);                          // 重複時間率
            CombinationKeyMaxAllowedLeadTimeMs = GetString(CombinationKeyMaxAllowedLeadTimeMs_PropName)._parseInt(100);   // 許容リードタイム
            //CombinationKeyMaxAllowedLeadTimeMs2 = GetString("combinationMaxAllowedLeadTimeMs2")._parseInt(0);   // シフトキーが文字キーだった場合の許容リードタイム
            ComboKeyMaxAllowedPostfixTimeMs = GetString(ComboKeyMaxAllowedPostfixTimeMs_PropName)._parseInt(100)._highLimit(CombinationKeyMaxAllowedLeadTimeMs);  // 第2キーの許容リードタイム
            CombinationKeyMinOverlappingTimeMs = GetString(CombinationKeyMinOverlappingTimeMs_PropName)._parseInt(70);               // 重複時間
            //CombinationKeyMinOverlappingTimeMs2 = GetString(CombinationKeyMinOverlappingTimeMs2_PropName)._parseInt(0);              // シフトキーが文字キーだった場合の重複時間
            CombinationKeyMinOverlappingTimeMs3 = GetString(CombinationKeyMinOverlappingTimeMs3_PropName)._parseInt(0)._lowLimit(CombinationKeyMinOverlappingTimeMs);   // 3キー以上同時の場合の重複時間
            ComboDisableIntervalTimeMs = GetString(ComboDisableIntervalTimeMs_PropName)._parseInt(0);                  // 同時打鍵シフトキーがUPされた後、後置シフトを無効にする時間
            CombinationKeyMinTimeOnlyAfterSecond = GetString(CombinationKeyMinTimeOnlyAfterSecond_PropName)._parseBool(false);    // ２文字目以降についてのみ同時打鍵チェックを行う
            UseCombinationKeyTimer1 = GetString(UseCombinationKeyTimer1_PropName)._parseBool(false);                   // 同時打鍵判定用タイマーを使用する
            UseCombinationKeyTimer2 = GetString(UseCombinationKeyTimer2_PropName)._parseBool(false);                   // 同時打鍵判定用タイマーを使用する
            UseComboExtModKeyAsSingleHit = GetString(UseComboExtModKeyAsSingleHit_PropName)._parseBool(true);          // 同時打鍵キーとして使う「無変換」や「変換」を単打キーとしても使えるようにする

            ThreeKeysComboUnconditional = GetString(ThreeKeysComboUnconditional_PropName)._parseBool(false);        // 優先される順次打鍵以外の3キー同時打鍵なら無条件に判定
            SequentialPriorityWords = GetString(SequentialPriorityWords_PropName, "てない").Trim();                 // 同時打鍵よりも順次打鍵のほうを優先させる文字列の並び
            SequentialPriorityWordSet.UnionWith(SequentialPriorityWords._reSplit(@"[ ,\|]+"));               // 同時打鍵よりも順次打鍵のほうを優先させる文字列の集合
            OnlyCharKeysComboShouldBeCoveringCombo = GetString(OnlyCharKeysComboShouldBeCoveringCombo_PropName)._parseBool(false);     // 文字キーのみの同時打鍵組合せの場合は、被覆Comboとするか

            //-------------------------------------------------------------------------------------
            // IME連携
            ImeCooperationEnabled = GetString(ImeCooperationEnabled_PropName)._parseBool(false);
            ImeKatakanaToHiragana = GetString(ImeKatakanaToHiragana_PropName)._parseBool(false);
            ImeSendInputInRoman = GetString(ImeSendInputInRoman_PropName)._parseBool(false);
            ImeSendInputInKana = GetString(ImeSendInputInKana_PropName)._parseBool(false);
            //ImeUnicodeClassNames = GetString("imeUnicodeClassNames")._orElse("Edit|_WwG|SakuraView*").Trim();
            //ImeUnicodeClassNames = GetString("imeUnicodeClassNames").Trim();
            //ImeUnicodeClassNamesHash = new HashSet<string>(ImeUnicodeClassNames.Trim()._toLower()._split('|'));

            //------------------------------------------------------------------------------
            // 書き換えシステム
            PreRewriteTargetChars  = GetString(PreRewriteTargetChars_PropName)._orElse("。、");                       // 遅延許容時間の適用対象となる前置書き換え対象文字集合
            PreRewriteAllowedDelayTimeMs = GetString(PreRewriteAllowedDelayTimeMs_PropName)._parseInt(0);             // 指定の文字集合に対する前置書き換え時の遅延許容時間
            PreRewriteAllowedDelayTimeMs2 = GetString(PreRewriteAllowedDelayTimeMs2_PropName)._parseInt(0);           // 上記以外の文字に対する前置書き換え時の遅延許容時間
            //PreRewriteCharsIgnoredWhenTrainingMode  = GetString("preRewriteCharsIgnoredWhenTrainingMode");          // かな入力練習モードのときに無視する前置書き換え対象文字
            PreRewriteWaitTimeMsWhenTrainingMode  = GetString(PreRewriteWaitTimeMsWhenTrainingMode_PropName)._parseInt(100);  // かな入力練習モードのときの書き換え対象文字の出力待ち時間

            //-------------------------------------------------------------------------------------
            // ClassName ごとの設定
            winClassSettings.Clear();
            foreach (var name in GetSectionNames()) {
                if (name._ne("kanchoku")) {
                    int[] parseIntArray(string str) {
                        string s = str._strip();
                        if (s._isEmpty()) return null;
                        return s._split(',').Select(x => x._parseInt(0)._lowLimit(0)).ToArray();
                    }
                    winClassSettings[name._toLower()] = new WindowsClassSettings() {
                        ValidCaretMargin = parseIntArray(GetStringFromSection(name, "validCaretMargin", "")),
                        CaretOffset = parseIntArray(GetStringFromSection(name, "caretOffset", "")),
                        VkbFixedPos = parseIntArray(GetStringFromSection(name, "vkbFixedPos", "")),
                        CtrlUpWaitMillisec = GetStringFromSection(name, "ctrlUpWaitMillisec", "-1")._parseInt(-1),
                        CtrlDownWaitMillisec = GetStringFromSection(name, "ctrlDownWaitMillisec", "-1")._parseInt(-1),
                        AfterBSWaitMillisec = GetStringFromSection(name, "afterBSWaitMillisec", "0")._parseInt(0),
                    };
                }
            }

            //-------------------------------------------------------------------------------------
            // デコーダ設定
            DecoderSettings.Clear();
            DecoderSettings["logLevel"] = LogLevel.ToString();
            DecoderSettings["rootDir"] = KanchokuIni.Singleton.KanchokuDir;
            DecoderSettings["firstUse"] = $"{!UserKanchokuIni.Singleton.IsIniFileExist}";
            DecoderSettings["isJPmode"] = $"{Domain.DecoderKeyVsVKey.IsJPmode}";
            BushuAssocFile = addDecoderSetting("bushuAssocFile", "kwassoc.txt");
            BushuFile = addDecoderSetting("bushuFile", "bushu", "kwbushu.rev");
            AutoBushuFile = addDecoderSetting("autoBushuFile", "bushuAuto", "kwbushu.aut");
            //var charsDefFile = GetString("charsDefFile");
            //if (charsDefFile._isEmpty()) {
            //    var kbName = KeyboardFile._split('.')._getNth(0);
            //    if (kbName._notEmpty() && (kbName._toLower() == "jp" || kbName._toLower() == "us")) kbName = null;
            //    if (kbName._notEmpty()) charsDefFile = $"chars.{kbName}.txt";
            //}
            setDecoderSetting("charsDefFile", TempCharsDefFile);
            EasyCharsFile = addDecoderSetting("easyCharsFile", "easy_chars.txt");
            TableFile = addDecoderSetting("tableFile", $"{TableFileDir}\\漢直系\\tutr.tbl");
            TableFile2 = addDecoderSetting("tableFile2", "");
#if DEBUG
#else
            TableFile3 = addDecoderSetting("tableFile3", "");
#endif
            KanjiYomiFile = addDecoderSetting("kanjiYomiFile", "kanji-yomi.txt");
            //addDecoderSetting("strokeHelpFile");
            HistoryFile = addDecoderSetting("historyFile", "kwhist.*.txt");
            //addDecoderSetting("historyUsedFile");
            //addDecoderSetting("historyExcludeFile");
            //addDecoderSetting("historyNgramFile");
            MazegakiFile = addDecoderSettingByGettingFiles("mazegakiFile", "kwmaze.*.dic");

            BackFileRotationGeneration = addDecoderSetting("backFileRotationGeneration", 3, 1); // 辞書ファイルの保存世代数

            HistKatakanaWordMinLength = addDecoderSetting("histKatakanaWordMinLength", 4, 3);   // 自動履歴登録対象となるカタカナ文字列の最小長
            HistKatakanaWordMaxLength = addDecoderSetting("histKatakanaWordMaxLength", 8, 8);   // 自動履歴登録対象となるカタカナ文字列の最大長
            HistKanjiWordMinLength = addDecoderSetting("histKanjiWordMinLength", 4, 3);         // 自動履歴登録対象となる漢字文字列の最小長
            HistKanjiWordMaxLength = addDecoderSetting("histKanjiWordMaxLength", 6, 6);         // 自動履歴登録対象となる漢字文字列の最大長
            HistKanjiWordMinLengthEx = addDecoderSetting("histKanjiWordMinLengthEx", 2, 2);     // 自動履歴登録対象となる難打鍵文字を含む漢字文字列の最小長
            //HistMaxLength = addDecoderSetting("histMaxLength", 10, 4);                          // 自動履歴登録対象となる文字列の最大長
            HistMaxLength = HistKatakanaWordMaxLength._max(HistKanjiWordMaxLength);             // 自動履歴登録対象となる文字列の最大長
            setDecoderSetting("histMaxLength", $"{HistMaxLength}");
            HistHiraganaKeyLength = addDecoderSetting("histHiraganaKeyLength", 2, 1);           // ひらがな始まり履歴の自動検索を行う際のキー長
            HistKatakanaKeyLength = addDecoderSetting("histKatakanaKeyLength", 2, 1);           // カタカナ履歴の自動検索を行う際のキー長
            HistKanjiKeyLength = addDecoderSetting("histKanjiKeyLength", 1, 1);                 // 漢字履歴の自動検索を行う際のキー長
            HistRomanKeyLength = addDecoderSetting("histRomanKeyLength", 4, 1);                 // ローマ字履歴の自動検索を行う際のキー長
            HistMapKeyMaxLength = addDecoderSetting("histMapKeyMaxLength", 16, 4);              // 変換履歴キーの最大長
            HistMapGobiMaxLength = addDecoderSetting("histMapGobiMaxLength", 2, 0);             // 変換履歴キーに付加できる語尾の最大長
            AutoHistSearchEnabled = addDecoderSetting("autoHistSearchEnabled", false);          // 自動履歴検索を行う
            //HistSearchByCtrlSpace = addDecoderSetting("histSearchByCtrlSpace", true);           // Ctrl-Space で履歴検索を行う
            //VKeyComboRepository.AddCtrlDeckeyAndCombo(" ", DecoderKeys.CTRL_SPACE_DECKEY, 0);           // 登録
            //HistSearchByShiftSpace = addDecoderSetting("histSearchByShiftSpace", true);         // Shift-Space で履歴検索を行う
            SelectFirstCandByEnter = addDecoderSetting("selectFirstCandByEnter", false);        // Enter で最初の履歴検索候補を選択する
            NewLineWhenHistEnter = addDecoderSetting("newLineWhenHistEnter", false);            // 履歴候補選択時のEnterではつねに改行する
            HistDelDeckeyId = addDecoderSetting("histDelDeckeyId", "histDelHotkeyId", 41, 41);  // 履歴削除を呼び出すDecKeyのID
            HistNumDeckeyId = addDecoderSetting("histNumDeckeyId", "histNumHotkeyId", 45, 41);  // 履歴文字数指定を呼び出すDecKeyのID
            HistHorizontalCandMax = addDecoderSetting("histHorizontalCandMax", 10, 1, 10);      // 履歴候補の横列鍵盤表示の際の最大候補数
            HistMoveShortestAt2nd = addDecoderSetting("histMoveShortestAt2nd", false);          // 最短長履歴文字列を2番目に表示する
            ShowHistCandsFromFirst = addDecoderSetting("showHistCandsFromFirst", true);         // 最初の履歴選択から横列候補表示を行うか
            HistAllowFromMiddleChar = addDecoderSetting("histAllowFromMiddleChar", true);       // 出力漢字列やカタカナ列の途中からでも自動履歴検索を行う(@TODO)
            UseArrowKeyToSelectCandidate = addDecoderSetting("useArrowToSelCand", true);        // 矢印キーで履歴候補選択を行う
            SelectHistCandByTab = addDecoderSetting("selectHistCandByTab", true);               // Tabキーで履歴候補選択を行う
            //HandleShiftSpaceAsNormalSpace = addDecoderSetting("handleShiftSpaceAsNormalSpace", true);  // Shift+Space を通常 Space しとて扱う(HistSearchByShiftSpaceがfalseの場合)

            //MazegakiByShiftSpace = GetString("mazegakiByShiftSpace")._parseBool(true);          // Shift-Space で交ぜ書き変換
            MazegakiSelectFirstCand = addDecoderSetting("mazegakiSelectFirstCand", false);      // 交ぜ書き変換で先頭の候補を自動選択
            MazeBlockerTail = addDecoderSetting("mazeBlockerTail", true);                       // 交ぜ書き変換で、変換後のブロッカーの位置
            MazeRemoveHeadSpace = addDecoderSetting("mazeRemoveHeadSpace", true);               // 交ぜ書き変換で、空白文字を変換開始位置とする
            MazeRightShiftYomiPos = addDecoderSetting("mazeRightShiftYomiPos", false);          // 交ぜ書き変換で、読みの開始位置を右移動する
            MazeNoIfxConnectKanji = addDecoderSetting("mazeNoIfxConnectKanji", true);           // 無活用語の語尾に漢字を許可する
            MazeNoIfxConnectAny = addDecoderSetting("mazeNoIfxConnectAny", false);              // 無活用語の語尾に任意文字を許可する
            MazeHistRegisterAnyway = addDecoderSetting("mazeHistRegisterAnyway", false);        // 交ぜ書き変換での選択を強制的に履歴登録する(除外登録されていたら復活する)
            MazeHistRegisterMinLen = addDecoderSetting("mazeHistRegisterMinLen", 1, 0);         // 交ぜ書き変換を履歴登録する際の最小語幹長
            MazeYomiMaxLen = addDecoderSetting("mazeYomiMaxLen", 10, 8);                        // 交ぜ書きの読み入力の最大長
            MazeGobiMaxLen = addDecoderSetting("mazeGobiMaxLen", 5, 0);                         // 交ぜ書きの語尾の最大長
            MazeGobiLikeTailLen = addDecoderSetting("mazeGobiLikeTailLen", 2, 0);               // 交ぜ書き変換で、語尾に含めてしまう末尾の長さ

            HiraganaToKatakanaShiftPlane = GetString("hiraToKataShiftPlane")._parseInt(1);      // 平仮名をカタカナに変換する際に使用するシフト面(0:None, 1:通常、2:A、3:B)
            DecoderSettings["hiraToKataShiftPlane"] = (!ConvertShiftedHiraganaToKatakana ? 0 : HiraganaToKatakanaShiftPlane).ToString();
            HiraganaToKatakanaNormalPlane = addDecoderSetting("hiraToKataNormalPlane", false);                      // 「。」と「．」の相互変換
            ConvertJaPeriod = addDecoderSetting("convertJaPeriod", false);                      // 「。」と「．」の相互変換
            ConvertJaComma = addDecoderSetting("convertJaComma", false);                        // 「、」と「，」の相互変換

            EisuModeEnabled = addDecoderSetting("eisuModeEnabled", false);                      // 英大文字入力による英数モード移行が有効か
            EisuHistSearchChar = addDecoderSetting("eisuHistSearchChar", "");                   // 英数モードから履歴検索を呼び出す文字
            EisuExitCapitalCharNum = addDecoderSetting("eisuExitCapitalCharNum", 3, 2);         // 英数モードを自動的に抜けるまでの大文字数
            EisuExitSpaceNum = addDecoderSetting("eisuExitSpaceNum", 2, 0);                     // 英数モードを自動的に抜けるまでのSpaceキー数

            RemoveOneStrokeByBackspace = addDecoderSetting("removeOneByBS", "weakBS", false);   // BS で直前打鍵のみを取り消すか

            YamanobeEnabled = addDecoderSetting("yamanobeEnabled", false);                      // YAMANOBEアルゴリズムを有効にするか
            bool bAutoBushuComp = GetString("autoBushuComp")._parseBool(false);                 // 自動首部合成を有効にするか(旧フラグ)
            AutoBushuCompMinCount = addDecoderSetting("autoBushuCompMinCount", bAutoBushuComp ? 1 : 0, 0);   // 自動首部合成を有効にする最小合成回数
            BushuAssocSelectCount = addDecoderSetting("bushuAssocSelectCount", 1, 1, 10);       // 部首連想直接出力の回数

            RomanBushuCompPrefix = addDecoderSetting("romanBushuCompPrefix", "");               // ローマ字テーブル出力時の部首合成用プレフィックス
            RomanSecPlanePrefix = addDecoderSetting("romanSecPlanePrefix", ":");                // 裏面定義文字に対するローマ字出力時のプレフィックス

            // キー割当
            HistorySearchCtrlKey = GetString("histSearchCtrlKey");                              // 履歴検索&選択を行うCtrlキー
            KeyComboRepository.AddCtrlDeckeyAndCombo(HistorySearchCtrlKey, DecoderKeys.HISTORY_NEXT_SEARCH_DECKEY, DecoderKeys.HISTORY_PREV_SEARCH_DECKEY);   // 登録
            FullEscapeKey = GetString("fullEscapeKey", "G").Trim();
            KeyComboRepository.AddCtrlDeckeyAndCombo(FullEscapeKey, DecoderKeys.FULL_ESCAPE_DECKEY, DecoderKeys.UNBLOCK_DECKEY);
            StrokeHelpRotationKey = GetString("strokeHelpRotationKey", "T").Trim();   // T
            KeyComboRepository.AddCtrlDeckeyAndCombo(StrokeHelpRotationKey, DecoderKeys.STROKE_HELP_ROTATION_DECKEY, DecoderKeys.STROKE_HELP_UNROTATION_DECKEY);

            DecoderSpecialDeckeys.Clear();
            DecoderSpecialDeckeys.Add(DecoderKeys.FULL_ESCAPE_DECKEY);
            DecoderSpecialDeckeys.Add(DecoderKeys.UNBLOCK_DECKEY);
            DecoderSpecialDeckeys.Add(DecoderKeys.STROKE_HELP_ROTATION_DECKEY);
            DecoderSpecialDeckeys.Add(DecoderKeys.STROKE_HELP_UNROTATION_DECKEY);

            FunctionKeySeqSet.Clear();
            ZenkakuModeKeySeq = addDecoderKeySeqSetting(ZenkakuModeKeySeq_PropName);
            if (ZenkakuModeKeySeq._notEmpty()) FunctionKeySeqSet.Add(ZenkakuModeKeySeq);
            ZenkakuOneCharKeySeq = addDecoderKeySeqSetting(ZenkakuOneCharKeySeq_PropName);
            if (ZenkakuOneCharKeySeq._notEmpty()) FunctionKeySeqSet.Add(ZenkakuOneCharKeySeq);
            NextThroughKeySeq = addDecoderKeySeqSetting(NextThroughKeySeq_PropName);
            if (NextThroughKeySeq._notEmpty()) FunctionKeySeqSet.Add(NextThroughKeySeq);
            HistoryKeySeq = addDecoderKeySeqSetting(HistoryKeySeq_PropName);
            if (HistoryKeySeq._notEmpty()) FunctionKeySeqSet.Add(HistoryKeySeq);
            HistoryOneCharKeySeq = addDecoderKeySeqSetting(HistoryOneCharKeySeq_PropName);
            if (HistoryOneCharKeySeq._notEmpty()) FunctionKeySeqSet.Add(HistoryOneCharKeySeq);
            HistoryFewCharsKeySeq = addDecoderKeySeqSetting(HistoryFewCharsKeySeq_PropName);
            if (HistoryFewCharsKeySeq._notEmpty()) FunctionKeySeqSet.Add(HistoryFewCharsKeySeq);
            MazegakiKeySeq = addDecoderKeySeqSetting(MazegakiKeySeq_PropName);
            if (MazegakiKeySeq._notEmpty()) FunctionKeySeqSet.Add(MazegakiKeySeq);
            BushuCompKeySeq = addDecoderKeySeqSetting(BushuCompKeySeq_PropName);
            if (BushuCompKeySeq._notEmpty()) FunctionKeySeqSet.Add(BushuCompKeySeq);
            BushuAssocKeySeq = addDecoderKeySeqSetting(BushuAssocKeySeq_PropName);
            if (BushuAssocKeySeq._notEmpty()) FunctionKeySeqSet.Add(BushuAssocKeySeq);
            BushuAssocDirectKeySeq = addDecoderKeySeqSetting(BushuAssocDirectKeySeq_PropName);
            if (BushuAssocDirectKeySeq._notEmpty()) FunctionKeySeqSet.Add(BushuAssocDirectKeySeq);
            KatakanaModeKeySeq = addDecoderKeySeqSetting(KatakanaModeKeySeq_PropName);
            if (KatakanaModeKeySeq._notEmpty()) FunctionKeySeqSet.Add(KatakanaModeKeySeq);
            KatakanaOneShotKeySeq = addDecoderKeySeqSetting(KatakanaOneShotKeySeq_PropName);
            if (KatakanaOneShotKeySeq._notEmpty()) FunctionKeySeqSet.Add(KatakanaOneShotKeySeq);
            HankakuKatakanaOneShotKeySeq = addDecoderKeySeqSetting(HankakuKatakanaOneShotKeySeq_PropName);
            if (HankakuKatakanaOneShotKeySeq._notEmpty()) FunctionKeySeqSet.Add(HankakuKatakanaOneShotKeySeq);
            BlockerSetterOneShotKeySeq = addDecoderKeySeqSetting(BlockerSetterOneShotKeySeq_PropName);
            if (BlockerSetterOneShotKeySeq._notEmpty()) FunctionKeySeqSet.Add(BlockerSetterOneShotKeySeq);

            // for Debug
            //addDecoderSetting("debughState", false);
            //addDecoderSetting("debughMazegaki", false);
            //addDecoderSetting("debughMazegakiDic", false);
            //addDecoderSetting("debughHistory", false);
            //addDecoderSetting("debughStrokeTable", false);
            //addDecoderSetting("debughBushu", false);
            //addDecoderSetting("debughString", false);
            //addDecoderSetting("debughZenkaku", false);
            //addDecoderSetting("debughKatakana", false);
            BushuDicLogEnabled = addDecoderSetting("bushuDicLogEnabled", false);

            logger.InfoH(() => $"LEAVE");

            return true;
        }

        public static string GetVerticalFontHeightFactorStr(string strFactor)
        {
            var factor = strFactor._parseDouble();
            return !factor._isNaN() && factor != VerticalFontHeightFactor ? strFactor : "";
        }

        //------------------------------------------------------------------------------
        public static string GetUserIni(string key)
        {
            return GetString(key);
        }

        public static void SetUserIni(string key, string value)
        {
            if (!IsInternalValueSet(key)) UserKanchokuIni.Singleton.SetString(key, value);
        }

        public static int GetUserIniInt(string key)
        {
            return GetString(key)._parseInt();
        }

        public static void SetUserIni(string key, int value)
        {
            if (!IsInternalValueSet(key)) UserKanchokuIni.Singleton.SetInt(key, value);
        }

        public static void SetUserIni(string key, bool value)
        {
            if (!IsInternalValueSet(key)) UserKanchokuIni.Singleton.SetBool(key, value);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace KanchokuWS
{
    //------------------------------------------------------------------
    /// <summary> Decoder からの出力情報を保持する構造体 </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DecoderOutParams
    {
        /// <summary> アクティブウィンドウに対して文字列を送信する前に送りつけるBSの数 </summary>
        public int numBackSpaces;

        /// <summary> class ResultKeys で値を定義</summary>
        public uint resultFlags;

        /// <summary> 次の入力で期待されるキー (第2ストローク、履歴入力でのEnter、など)</summary>
        public int nextExpectedKeyType;

        /// <summary> 文字を入力する際の現在の打鍵数</summary>
        public int strokeCount;

        /// <summary> 指定文字の次の打鍵位置</summary>
        public int nextStrokeDeckey;

        /// <summary> 次の選択候補位置</summary>
        public int nextSelectPos;

        /// <summary> 使用中のストロークテーブルの番号(1 or 2)</summary>
        public int strokeTableNum;

        /// <summary> アクティブウィンドウに送信する文字列(または制御キー)<br/>
        /// '\0' 終端であること </summary>
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 100)]
        public char[] outString;

        /// <summary> 表示する仮想キーボードのレイアウト (10:縦10列、50:通常50キー) </summary>
        public int layout;

        // 仮想キーボードの上部に表示する文字列
        // 32文字未満の場合は '\\0' 終端であること
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 32)]
        public char[] topString;

        /// <summary> 仮想キーボードの中央に表示する文字列<br/>
        /// 10文字未満の場合は '\0' 終端であること </summary>
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 20)]
        public char[] centerString;

        /// <summary> 仮想キーボードに表示する文字列 (レイアウトにより、 20x10 または 2x100 として扱う)<br/>
        /// 配列文字長未満の場合は '\0' 終端であること </summary>
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U2, SizeConst = 200)]
        public char[] faceStrings;
    }

    /// <summary>仮想鍵盤レイアウトタイプ</summary>
    public enum VkbLayout
    {
        None = 0,
        TwoSides = 2,
        Vertical = 10,
        BushuCompHelp = 11,
        Horizontal = 20,
        Normal = 50,
        StrokeHelp = 51,
        KanaTable = 52,
    }

    /// <summary>
    /// 次の入力で期待されるキーの型
    /// </summary>
    public static class ExpectedKeyType
    {
        /// <summary> 特になし</summary>
        public const int None = 0;

        /// <summary> 第2ストローク</summary>
        public const int SecondStroke = 1;

        /// <summary> 交ぜ書き変換候補選択中</summary>
        public const int MazeCandSelecting = 2;

        /// <summary> 履歴候補選択中</summary>
        public const int HistCandSelecting = 3;

        /// <summary> 連想候補選択中</summary>
        public const int AssocCandSelecting = 4;

        /// <summary>部首合成ヘルプ</summary>
        public const int BushuCompHelp = 5;

        /// <summary> その他の状態</summary>
        public const int OtherStatus = 6;
    }

    /// <summary>
    /// resultFlags の詳細
    /// </summary>
    public static class ResultFlags
    {
        /// <summary> DECKEYを仮想キーに変換してアクティブウィンドウに対して送信する</summary>
        public const uint DeckeyToVkey = 1;
        /// <summary> Ctrl-H や BS などの特殊キーをDECKEYで受け取る必要あり</summary>
        public const uint SpecialDeckeyRequired = 2;
        /// <summary> 全角モード標識の表示が必要</summary>
        public const uint ShowZenkakuModeMarker = 4;
        /// <summary> 全角モード標識の表示をやめる</summary>
        public const uint ClearZenkakuModeMarker = 8;
        ///// <summary>待ち受け画面のトグル</summary>
        //public const uint ToggleInitialStrokeHelp = 128;
        /// <summary> 仮想鍵盤を移動しない</summary>
        public const uint DontMoveVirtualKeyboard = 0x100;
    }

    /// <summary> 拡張メソッド</summary>
    public static class DecoderOutParamsExtension
    {
        /// <summary>第1ストローク待ちか</summary>
        public static bool IsWaitingFirstStroke(this DecoderOutParams output) { return output.GetStrokeCount() < 1; }

        /// <summary>第2ストローク待ちか</summary>
        public static bool IsWaiting2ndStroke(this DecoderOutParams output) { return output.nextExpectedKeyType == ExpectedKeyType.SecondStroke; }

        /// <summary>交ぜ書き変換候補選択中か</summary>
        public static bool IsMazeCandSelecting(this DecoderOutParams output) { return output.nextExpectedKeyType == ExpectedKeyType.MazeCandSelecting; }

        /// <summary>履歴候補選択中か</summary>
        public static bool IsHistCandSelecting(this DecoderOutParams output) { return output.nextExpectedKeyType == ExpectedKeyType.HistCandSelecting; }

        /// <summary>連想候補選択中か</summary>
        public static bool IsAssocCandSelecting(this DecoderOutParams output) { return output.nextExpectedKeyType == ExpectedKeyType.AssocCandSelecting; }

        /// <summary>部首合成ヘルプか</summary>
        public static bool IsBushuCompHelp(this DecoderOutParams output) { return output.nextExpectedKeyType == ExpectedKeyType.BushuCompHelp; }

        /// <summary>その他の状態か</summary>
        public static bool IsOtherStatus(this DecoderOutParams output) { return output.nextExpectedKeyType == ExpectedKeyType.OtherStatus; }

        /// <summary>矢印キーの要求されるレイアウトか</summary>
        public static bool IsArrowKeysRequired(this DecoderOutParams output) { return output.layout >= (int)VkbLayout.Vertical && output.layout <= (int)VkbLayout.Horizontal; }

        /// <summary>DecKeyを仮想キーに戻して出力するか</summary>
        public static bool IsDeckeyToVkey(this DecoderOutParams output) { return (output.resultFlags & ResultFlags.DeckeyToVkey) != 0; }
        //private bool isSpecialDeckeyRequired(this DecoderOutParams output) { return (output.resultFlags &  ResultFlags.SpecialDeckeyRequired) != 0; }

        /// <summary>全角モード標識を表示するか</summary>
        public static bool IsZenkakuModeMarkerShow(this DecoderOutParams output) { return (output.resultFlags & ResultFlags.ShowZenkakuModeMarker) != 0; }

        /// <summary>全角モード標識をクリアするか</summary>
        public static bool IsZenkakuModeMarkerClear(this DecoderOutParams output) { return (output.resultFlags & ResultFlags.ClearZenkakuModeMarker) != 0; }
        //private bool isToggleInitialStrokeHelp(this DecoderOutParams output) { return (output.resultFlags & ResultFlags.ToggleInitialStrokeHelp) != 0; }

        /// <summary>仮想鍵盤を移動させないか(仮想鍵盤自身がアクティブになっているなど)</summary>
        public static bool IsVirtualKeyboardFreezed(this DecoderOutParams output) { return (output.resultFlags & ResultFlags.DontMoveVirtualKeyboard) != 0; }

        /// <summary> 当文字の何打鍵目か</summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public static int GetStrokeCount(this DecoderOutParams output) { return output.strokeCount; }

        /// <summary> 1番目のストロークテーブルか</summary>
        public static bool IsCurrentStrokeTablePrimary(this DecoderOutParams output) { return output.strokeTableNum != 2; }
    }

}

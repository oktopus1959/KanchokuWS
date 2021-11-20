using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using Utils;

namespace KanchokuWS
{
    public partial class FrmVirtualKeyboard : Form
    {
        private static Logger logger = Logger.GetLogger();

        private FrmKanchoku frmMain;

        public static int CurrentScreen = 0;

        private static float VkbNormalWidth = 201;       // = Vkb5x10CellWidth * 10 + 1 = VkbCellWidth * 10 + VkbCenterWidth + 1
        private static float Vkb5x10Width = 201;       // = Vkb5x10CellWidth * 10 + 1 = VkbCellWidth * 10 + VkbCenterWidth + 1

        private static float VkbCellHeight = 18;
        private static float VkbCellWidth = 18;
        private static float VkbCenterWidth = 20;
        private static float VkbBottomOffset = VkbCenterWidth / 2;

        private static float Vkb5x10CellWidth = 20;
        private static float Vkb5x10CellHeight = 37;
        private static float Vkb5x10FaceYOffset = 3;
        private static float Vkb5x10KeyYOffset = Vkb5x10CellHeight - 18;

        private static float VkbPictureBoxHeight_Normal = VkbCellHeight * 5 + 1;
        private static float VkbPictureBoxHeight_5x10Table = Vkb5x10CellHeight * 5 + 1;

        private static float VkbCenterBoxHeight_Normal = VkbCellHeight * 4;
        private static float VkbCenterBoxHeight_5x10Table = VkbPictureBoxHeight_5x10Table;

        private const int LongVkeyNum = 10;
        private const int LongVkeyCharSize = 20;

        private const int MinVerticalChars = 2;
        private const int MinCenterChars = 2;

        /// <summary> ストローク文字横書きフォント </summary>
        private Font strokeCharFont;
        /// <summary> ストロークキー横書きフォント </summary>
        private Font strokeKeyFont;

        //------------------------------------------------------------------------------------
        /// <summary>
        /// フォント情報
        /// </summary>
        public class CFontInfo : IDisposable
        {
            private string myName;

            private bool useVertical = false;
            private bool usePadding = false;

            private string currentFontSpec;

            /// <summary> 横書き用フォント </summary>
            public Font HorizontalFont = null;
            /// <summary> 縦書き用フォント </summary>
            public Font VerticalFont = null;
            /// <summary> 縦書きフォントの高さ </summary>
            public float CharHeight = 13;

            public Font MyFont => useVertical ? VerticalFont : HorizontalFont;

            /// <summary> 縦書きフォントの余白 </summary>
            public struct Padding
            {
                public float Left;
                public float Top;

                public Padding(float left, float top)
                {
                    Left = left;
                    Top = top;
                }
            }

            /// <summary> 縦書き時の左・上の余白 </summary>
            private List<Padding> paddings { get; set; } = new List<Padding>();

            public int PaddingsNum => paddings.Count;

            public Padding GetNthPadding(int nth)
            {
                if (paddings.Count == 0) return new Padding(0, 0);
                return paddings[nth._highLimit(paddings.Count - 1)];
            }

            // コンストラクタ
            public CFontInfo(string name, bool vertical, bool padding)
            {
                myName = name;
                this.useVertical = vertical;
                this.usePadding = padding;
            }

            public bool RenewFontSpec(string fontSpec, float cellWidth, float cellHeight, PictureBox picBox)
            {
                if (MyFont != null && fontSpec._equalsTo(currentFontSpec)) return false;    // 同じフォント指定なので、何もしない

                currentFontSpec = fontSpec;

                HorizontalFont?.Dispose();
                VerticalFont?.Dispose();
                paddings.Clear();

                var fontItems = fontSpec._split('|').Select(x => x._strip()).ToArray();
                string fontName = fontItems._getNth(0)._orElse(useVertical ? "@MS Gothic" : "MS UI Gothic");
                int fontSize = fontItems._getNth(1)._parseInt(9)._lowLimit(8);
                if (useVertical) {
                    VerticalFont = new Font(fontName, fontSize);
                }
                fontName = fontName._safeReplace("@", "");          // 先頭の @ を削除しておく
                HorizontalFont = new Font(fontName, fontSize);

                if (usePadding) {
                    (float fw, float fh) = measureFontSize(MyFont, picBox);

                    CharHeight = fh + 1;

                    Padding makePadding(int n)
                    {
                        if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"new {myName} Font Name={fontName}, Size={fontSize}, useVertical={useVertical}, cw={cellWidth:f1}, ch={cellHeight:f1},fw={fw:f1}, fh={fh:f1}");
                        float fw_ = fw;
                        if (fontName._startsWith("Yu ") || fontName._startsWith("游")) {
                            fw_ = fw <= 16 ? 18 : 16;
                        } else if (fontName._startsWith("Meiryo") || fontName._startsWith("メイリオ")) {
                            if (useVertical)
                                fw_ = fw + 8;
                            else
                                fw_ = fw + 3;
                        } else {
                            // MS Gothic と想定
                            fw_ = fw < 13 ? fw : (fw <= 13 ? 14 : 16);
                        }

                        float leftPadding = fontItems._getNth(n)._parseInt(-999);
                        if (leftPadding < -50) {
                            leftPadding = (cellWidth - fw_) / 2;
                        }

                        float topPadding = fontItems._getNth(n + 1)._parseInt(-999);
                        if (topPadding < -50) {
                            float fh_ = (fh - fw_) / 2;
                            if (useVertical) {
                                topPadding = fh_._lowLimit(2.0f);
                            } else {
                                if (fontName._startsWith("Yu ") || fontName._startsWith("游")) {
                                    fh_ = fh <= 16 ? 14 : 15;
                                    topPadding = cellHeight - fh_ - 2;  // 2～4 になるようにする
                                } else if (fontName._startsWith("Meiryo") || fontName._startsWith("メイリオ")) {
                                    fh_ = fw_;
                                    topPadding = (cellHeight - fh_) / 2;
                                } else {
                                    // MS Gothic と想定
                                    fh_ = fh < 13 ? fh : (fw <= 13 ? 13 : 14);
                                    topPadding = (cellHeight - fh_) / 2;
                                }
                            }
                        }
                        if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"new {myName} Font Width={fw:f3}, Height={fh:f3}, charHeight={CharHeight}, padLeft={leftPadding:f3}, padTop={topPadding:f3}");
                        return new Padding(leftPadding, topPadding);
                    }

                    paddings.Add(makePadding(2));
                    for (int n = 4; n < fontItems.Length; n += 2) {
                        paddings.Add(makePadding(n));
                    }
                }

                return true;
            }

            public void Dispose()
            {
                HorizontalFont?.Dispose();
                VerticalFont?.Dispose();
            }

            // フォントのピクセルサイズを計測する
            private (float, float) measureFontSize(Font font, PictureBox picBox)
            {
                //表示する文字
                string s = "亜";

                picBox.Width = 50;
                picBox.Height = 50;
                using (Bitmap canvas = new Bitmap(picBox.Width, picBox.Height)) {
                    using (Graphics g = Graphics.FromImage(canvas)) {
                        using (StringFormat sf = new StringFormat()) {
                            g.DrawString(s, font, Brushes.Black, 0, 0, sf);
                            //計測する文字の範囲を指定する
                            sf.SetMeasurableCharacterRanges(Helper.Array(new CharacterRange(0, 1)));
                            Region[] stringRegions = g.MeasureCharacterRanges(s, font, new RectangleF(1, 0, 50, 50), sf);
                            if (stringRegions.Length > 0) {
                                var rect = stringRegions[0].GetBounds(g);
                                return (rect.Width, rect.Height);
                            }
                            return (0, 0);
                        }
                    }
                }
            }
        }
        private bool renewFontInfo(CFontInfo fontInfo, string fontSpec)
        {
            return fontInfo.RenewFontSpec(fontSpec, VkbCellWidth, VkbCellHeight, pictureBox_measureFontSize);
        }

        // 縦列鍵盤サポート
        /// <summary> 縦列鍵盤ボックス </summary>
        public struct VerticalBox
        {
            public float X;
            public float Y;
            public float Width;
            public float Height;
            public Color BackColor;
        }

        private VerticalBox[] verticalBoxes = new VerticalBox[LongVkeyNum];
        private VerticalBox centerBox;

        /// <summary> 縦列鍵盤で用いるフォント情報 </summary>
        public class VerticalFontInfo : IDisposable
        {
            /// <summary> フォント情報 </summary>
            public CFontInfo FontInfo { get; private set; }

            /// <summary> 横書き用フォント </summary>
            public Font HorizontalFont => FontInfo?.HorizontalFont;

            /// <summary> 縦書き用フォント </summary>
            public Font VerticalFont => FontInfo?.VerticalFont;

            ///// <summary> フォント指定 </summary>
            //public string FontSpec = "";

            /// <summary> 縦書き時の左余白 </summary>
            public float LeftPadding => FontInfo?.GetNthPadding(CurrentScreen).Left ?? 0f;

            /// <summary> 縦書き時の上部余白 </summary>
            public float TopPadding => FontInfo?.GetNthPadding(CurrentScreen).Top ?? 0f;

            private float _charHeight = 13;
            /// <summary> 縦書きフォントの高さ </summary>
            public float CharHeight {
                get { return FontInfo?.CharHeight ?? _charHeight; }
                set { _charHeight = value; }
            }

            public float FontSizeThreshold1 = 9.5f;
            public float FontSizeThreshold2 = 11.0f;

            public VerticalFontInfo(string name)
            {
                FontInfo = new CFontInfo(name, true, true);
            }

            // 縦列鍵盤用フォントの更新
            public void renewFontInfo(string newSpec, float boxWidth, PictureBox picBox)
            {
                FontInfo.RenewFontSpec(newSpec, boxWidth, 18, picBox);
            }

            public float AdjustedLeftPadding(string str)
            {
                float leftPadding = LeftPadding;
                if (str._safeLength() == 1) {
                    leftPadding = VerticalFont.Size >= FontSizeThreshold2 ? 0 : VerticalFont.Size >= FontSizeThreshold1 ? 1 : 2;
                }
                return leftPadding;
            }

            public void Dispose()
            {
                FontInfo?.Dispose();
            }

        }

        [DllImport("user32.dll")]
        private static extern void ShowWindow(IntPtr hWnd, int nCmdShow);

        // ウィンドウをアクティブにせずに表示する
        private const int SW_SHOWNA = 8;

        private void showNonActive()
        {
            //topTextBox.Width = (int)(VkbNormalWidth);
            ShowWindow(this.Handle, SW_SHOWNA);   // NonActive
            logger.Info(() => $"LEAVE: this.Width={this.Width}, this.Height={this.Height}, tex.Height={topTextBox.Height}, pic.top={pictureBox_Main.Top}");
        }

        //------------------------------------------------------------------------------------
        /// <summary> コンストラクタ </summary>
        /// <param name="form"></param>
        public FrmVirtualKeyboard(FrmKanchoku form)
        {
            frmMain = form;

            InitializeComponent();

            // タイトルバーを消す
            FormBorderStyle = FormBorderStyle.None;

            // マウスホイール
            //pictureBox_Main.MouseWheel += new System.Windows.Forms.MouseEventHandler(pictureBox_Main_MouseWheel);

            // 各種パラメータの初期化
            resetDrawParameters(ScreenInfo.PrimaryScreenDpi);

            // モニタのDPIが変化したたときに呼ばれるハンドラを登録
            DpiChanged += dpiChangedHandler;
        }

        const int CS_DROPSHADOW = 0x00020000;

        /// <summary> フォームに影をつける </summary>
        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        /// <summary>フォームのクローズ</summary>
        private void FrmVirtualKeyboard_FormClosing(object sender, FormClosingEventArgs e)
        {
            normalFontInfo?.Dispose();
            centerFontInfo.Dispose();
            verticalFontInfo.Dispose();
            horizontalFontInfo?.Dispose();
            minibufFontInfo?.Dispose();
            strokeCharFont?.Dispose();
            strokeKeyFont?.Dispose();
        }

        /// <summary> フォームのロード </summary>
        private void DlgVirtualKeyboard_Load(object sender, EventArgs e)
        {
            // focus move
            topTextBox.actionOnPaste = sendWord;        // 上部出力文字列に何かをペーストされたときのアクション

            this.Width = (int)(VkbNormalWidth + 2);

            // 横列鍵盤用グリッドの初期化
            initializeHorizontalKeyboard(pictureBox_Main.Width - 1);

            // 縦書き用オブジェクトの生成
            createObjectsForDrawingVerticalChars();

            // 仮想鍵盤の初期化
            DrawInitailVkb();
        }

        /// <summary>通常・中央・縦列鍵盤の余白情報を文字列として取得</summary>
        /// <returns></returns>
        public string GetPaddingsDesc()
        {
            string makePaddings(CFontInfo info, int n)
            {
                var paddings = info.GetNthPadding(n);
                return $"(左={paddings.Left}, 上={paddings.Top})";
            }

            string makePaddingsDesc(CFontInfo info)
            {
                if (info.PaddingsNum < 1) {
                    return "まだ一度も表示されていないので情報がありません";
                }
                var _sb = new StringBuilder();
                _sb.Append(makePaddings(info, 0));
                for (int n = 1; n < info.PaddingsNum; ++n) {
                    _sb.Append(" | ").Append(makePaddings(info, n));
                }
                return _sb.ToString();
            }

            renewNormalFont();
            renewCenterVerticalFont();
            renewCandidateVerticalFont();

            var sb = new StringBuilder();
            sb.Append("通常鍵盤: ").Append(makePaddingsDesc(normalFontInfo)).Append("\r\n");
            sb.Append("中央鍵盤: ").Append(makePaddingsDesc(centerFontInfo.FontInfo)).Append("\r\n");
            sb.Append("縦列鍵盤: ").Append(makePaddingsDesc(verticalFontInfo.FontInfo));

            return sb.ToString();
        }

        /// <summary> 中央鍵盤フォント情報 </summary>
        private VerticalFontInfo centerFontInfo = new VerticalFontInfo("Center") {
            CharHeight = 15,
            FontSizeThreshold1 = 10.5f,
            FontSizeThreshold2 = 11.5f,
        };

        /// <summary> 横列鍵盤用フォント情報 </summary>
        private CFontInfo horizontalFontInfo = new CFontInfo("Horizontal", false, false);

        private bool renewHorizontalFontInfo()
        {
            return renewFontInfo(horizontalFontInfo, Settings.HorizontalVkbFontSpec);
        }

        // 横列鍵盤用グリッドの初期化
        private void initializeHorizontalKeyboard(int cellWidth)
        {
            var dgv = dgvHorizontal;
            dgv._defaultSetup(0, (int)VkbCellHeight);       // headerHeight=0 -> ヘッダーを表示しない
            dgv._setSelectionColorReadOnly();
            renewHorizontalFontInfo();
            dgv._setDefaultFont(horizontalFontInfo.MyFont);
            //dgv._setDefaultFont(DgvHelpers.FontMSG8);
            dgv._disableToolTips();
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly("horizontal", "", cellWidth)._setUnresizable());

            //dgv.Rows.Add(nRow);

            renewHorizontalKeyboard();
        }

        private void renewHorizontalKeyboard()
        {
            var dgv = dgvHorizontal;
            int cellWidth = (int)VkbNormalWidth - 1;
            int cellHeight = (int)VkbCellHeight;
            dgv.RowTemplate.Height = cellHeight;
            if (dgv.Columns.Count > 0) { dgv.Columns[0].Width = cellWidth; }
            if (renewHorizontalFontInfo()) { dgv._setDefaultFont(horizontalFontInfo.MyFont); }
            //dgv.Top = topTextBox.Top + topTextBox.Height - 1;
            dgv.Width = cellWidth + 1;
            //dgv.Height = dgv.Rows.Count * cellHeight + 1;

            if (dgv.Rows.Count == 0) dgv.Rows.Add(LongVkeyNum);

            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"dgv.Top={dgv.Top}, dgv.Width={dgv.Width}, cellHeight={cellHeight}, cellWidth={cellWidth}");
                
        }

        /// <summary> 縦書き用オブジェクトの生成 </summary>
        private void createObjectsForDrawingVerticalChars()
        {
            if (strokeCharFont == null) strokeCharFont = new Font("MS UI Gothic", 12);
            if (strokeKeyFont == null) strokeKeyFont = new Font("MS Gothic", 12);

            renewObjectsForDrawingVerticalChars();
        }

        /// <summary> 縦書き用オブジェクトの再作成 </summary>
        private void renewObjectsForDrawingVerticalChars()
        {
            float verticalBoxHeight = verticalFontInfo.CharHeight * 7 + 3;
            for (int i = 0; i < 5; ++i) {
                verticalBoxes[i] = new VerticalBox {
                    X = VkbCellWidth * i,
                    Y = 0,
                    Width = VkbCellWidth,
                    Height = verticalBoxHeight,
                    BackColor = i < 4 ? SystemColors.Window : SystemColors.ButtonFace
                };
            }
            for (int i = 5; i < 10; ++i) {
                verticalBoxes[i] = new VerticalBox {
                    X = VkbCenterWidth + VkbCellWidth * i,
                    Y = 0,
                    Width = VkbCellWidth,
                    Height = verticalBoxHeight,
                    BackColor = i > 5 ? SystemColors.Window : SystemColors.ButtonFace
                };
            }

            centerBox = new VerticalBox { X = VkbCellWidth * 5, Y = 0, Width = VkbCenterWidth, Height = VkbCellHeight * 4, BackColor = Color.White };
        }

        /// <summary>
        /// 仮想鍵盤が表示されているディスプレイのDPIを取得し、デフォルトDPI(96)との比を返す
        /// </summary>
        public double GetDeviceDpiRatio()
        {
            return DeviceDpi / 96.0;
        }

        /// <summary>
        /// モニタが切り替わってDPIが変化したときに呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dpiChangedHandler(object sender, DpiChangedEventArgs e)
        {
            logger.Info(() => $"\nCALLED: new dpi={e.DeviceDpiNew}");

            CurrentScreen = ScreenInfo.GetScreenIndexByDpi(e.DeviceDpiNew);

            if (frmMain.IsDecoderActive) {
                this.Hide();
                frmMain.DeactivateDecoder();
            }

            dgvHorizontal.Rows.Clear();

            resetDrawParameters(e.DeviceDpiNew);

            redrawVkb();
        }

        private void resetDrawParameters(int dpi)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"CALLED: dpi={dpi}");
            //float rate = (float)ScreenInfo.PrimaryScreenDpiRate._lowLimit(1.0);
            float rate = dpi / 96.0f;

            Func<float, float> mulRate = (float x) => (int)(x * rate);

            VkbCellHeight = mulRate(18);
            VkbCellWidth = mulRate(18);
            VkbCenterWidth = mulRate(20);
            VkbBottomOffset = VkbCenterWidth / 2;

            Vkb5x10CellWidth = mulRate(20);
            Vkb5x10CellHeight = mulRate(37);
            Vkb5x10FaceYOffset = mulRate(3);
            Vkb5x10KeyYOffset = Vkb5x10CellHeight - VkbCellHeight;

            VkbNormalWidth = VkbCellWidth * 10 + VkbCenterWidth + 1;
            Vkb5x10Width = Vkb5x10CellWidth * 10 + 1; // = VkbCellWidth * 10 + VkbCenterWidth + 1

            VkbPictureBoxHeight_Normal = VkbCellHeight * 5 + 1;
            VkbPictureBoxHeight_5x10Table = Vkb5x10CellHeight * 5 + 1;

            VkbCenterBoxHeight_Normal = VkbCellHeight * 4;
            VkbCenterBoxHeight_5x10Table = VkbPictureBoxHeight_5x10Table;

            this.Width = (int)(VkbNormalWidth + 2);
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"LEAVE: this.Width={this.Width}");
        }

        /// <summary>
        /// 仮想鍵盤の再描画
        /// </summary>
        private void redrawVkb()
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"CALLED: VkbNormalWidth={VkbNormalWidth}, VkbCellHeight={VkbCellHeight}");

            //this.Width = (int)(VkbNormalWidth + 2);
            //topTextBox.Width = (int)(VkbNormalWidth);
            //topTextBox.Height = (int)(VkbCellHeight + 1);
            //logger.Info($"topTextBox.Width={topTextBox.Width}, topTextBox.Height={topTextBox.Height}");
            pictureBox_Main.Width = (int)(VkbNormalWidth);
            pictureBox_Main.BackColor = Color.White;

            // 横書き鍵盤の再初期化
            //renewHorizontalKeyboard();

            // 縦書き用オブジェクトの再作成
            renewObjectsForDrawingVerticalChars();

            // 仮想鍵盤の再描画
            if (frmMain.IsDecoderActive) DrawVirtualKeyboardChars();
        }

        //-----------------------------------------------------------------------------------------
        // 第1打鍵待ちのときに表示されるストロークテーブル
        public class StrokeTableDef
        {
            public bool KanaAlign;
            public bool ShiftPlane;
            public string Faces;
            public string[] CharOrKeys;
        }

        private int selectedTable = 0;
        private List<StrokeTableDef> StrokeTables = new List<StrokeTableDef>();

        private StrokeTableDef StrokeTables2 = null;

        private string[] initialVkbChars = new string[DecoderKeys.NORMAL_DECKEY_NUM] {
            "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
            "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
            "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
            "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
            "・", "・", "・", "・", "・", "・", "・", "・", "・", "・",
        };

        private string[] kanaOutChars = {
            "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもや ゆ よらりるれろわ ん を",
            "ぁぃぅぇぉがぎぐげござじずぜぞだぢづでど     ばびぶべぼぱぴぷぺぽゃ ゅ ょゕ  ゖ ゎゐゔゑ ",
            "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤ ユ ヨラリルレロワ ン ヲ",
            "ァィゥェォガギグゲゴザジズゼゾダヂヅデド     バビブベボパピプペポャ ュ ョヵ  ヶ ヮヰヴヱ ",
        };

        //private string[][] kanaVkbChars = {
        //    new string[DecoderKeys.NUM_STROKE_DECKEY] {
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "・", "・", "・", "・", "・", "・", "・", "・", "・", "・",
        //    },
        //    new string[DecoderKeys.NUM_STROKE_DECKEY] {
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "・", "・", "・", "・", "・", "・", "・", "・", "・", "・",
        //    },
        //    new string[DecoderKeys.NUM_STROKE_DECKEY] {
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "・", "・", "・", "・", "・", "・", "・", "・", "・", "・",
        //    },
        //    new string[DecoderKeys.NUM_STROKE_DECKEY] {
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "・", "・", "・", "・", "・", "・", "・", "・", "・", "・",
        //    },
        //};

        public void MakeStrokeTables(string defFile)
        {
            var filePath = KanchokuIni.Singleton.KanchokuDir._joinPath(defFile);
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info(() => $"ENTER: filePath={filePath}");
            if (Helper.FileExists(filePath)) {
                StrokeTables.Clear();
                try {
                    foreach (var line in System.IO.File.ReadAllLines(filePath)) {
                        var items = line.Trim()._reReplace("  +", " ")._split(' ');
                        if (items._notEmpty() && items[0]._notEmpty() && !items[0].StartsWith("#")) {
                            var cmd = items[0]._toLower();
                            var chars = items.Length > 1 ? items.Skip(1)._join(" ") : "";
                            if (Settings.LoggingVirtualKeyboardInfo) logger.Info(() => $"cmd={cmd}, param={chars}");
                            if (cmd == "initialtable") {
                                // 初期表示を追加(初期表示は事前に作成されている)
                                StrokeTables.Add(new StrokeTableDef {
                                    KanaAlign = false,
                                    Faces = null,
                                    CharOrKeys = initialVkbChars,
                                });
                            } else if (cmd == "extracharsposition") {
                                makeVkbStrokeTable("makeExtraCharsStrokePositionTable", null);
                            } else if (cmd == "keycharsposition") {
                                makeVkbStrokeTable("makeStrokePosition", null, false, false, false);
                            } else if (cmd == "keycharsposition2") {
                                makeVkbStrokeTable2("makeStrokePosition2", null);
                            } else if (cmd == "shiftkeycharsposition") {
                                makeVkbStrokeTable("makeShiftStrokePosition", null, false, false, true);
                            } else if (cmd == "shiftakeycharsposition") {
                                makeVkbStrokeTable("makeShiftAStrokePosition", null, false, false, true);
                            } else if (cmd == "shiftbkeycharsposition") {
                                makeVkbStrokeTable("makeShiftBStrokePosition", null, false, false, true);
                            } else if (cmd == "hiraganakey1") {
                                makeVkbStrokeTable("makeStrokeKeysTable", kanaOutChars[0], true, true);
                            } else if (cmd == "hiraganakey2") {
                                makeVkbStrokeTable("makeStrokeKeysTable", kanaOutChars[1], true, true);
                            } else if (cmd == "katakanakey1") {
                                makeVkbStrokeTable("makeStrokeKeysTable", kanaOutChars[2], true, true);
                            } else if (cmd == "katakanakey2") {
                                makeVkbStrokeTable("makeStrokeKeysTable", kanaOutChars[3], true, true);
                            } else if (chars._notEmpty()) {
                                if (cmd == "strokeposition") {
                                    makeVkbStrokeTable("reorderByFirstStrokePosition1", chars);
                                } else if (cmd == "strokeposition2") {
                                    makeVkbStrokeTable2("reorderByFirstStrokePosition2", chars);
                                } else if (cmd == "strokepositionfixed") {
                                    makeVkbStrokeTableFixed(chars);
                                } else if (cmd == "strokekey") {
                                    makeVkbStrokeTable("makeStrokeKeysTable", chars, true, false);
                                }
                            }
                        }
                    }
                } catch (Exception e) {
                    logger.Error($"Cannot read file: {filePath}: {e.Message}");
                }
            }
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info("LEAVE");
        }

        private void makeVkbStrokeTable(string cmd, string faces, bool drawFaces = false, bool kana = false, bool shiftPlane = false)
        {
            var charOrKeys = makeCharOrKeys(cmd, faces);
            if (charOrKeys != null) {
                StrokeTables.Add(new StrokeTableDef {
                    KanaAlign = kana,
                    ShiftPlane = shiftPlane,
                    Faces = drawFaces ? faces : null,
                    CharOrKeys = charOrKeys,
                });
            }
        }

        private void makeVkbStrokeTable2(string cmd, string faces)
        {
            var charOrKeys = makeCharOrKeys(cmd, faces);
            if (charOrKeys != null) {
                StrokeTables2 = new StrokeTableDef {
                    KanaAlign = false,
                    ShiftPlane = false,
                    Faces = null,
                    CharOrKeys = charOrKeys,
                };
            }
        }

        private string[] makeCharOrKeys(string cmd, string faces)
        {
            var result = frmMain.CallDecoderFunc(cmd, faces);
            if (result == null) return null;

            var charOrKeys = new string[DecoderKeys.NORMAL_DECKEY_NUM];
            for (int i = 0; i < charOrKeys.Length; ++i) {
                charOrKeys[i] = makeMultiCharStr(result, i * 2);
            }
            return charOrKeys;
        }

        private void makeVkbStrokeTableFixed(string faces)
        {
            var charOrKeys = new string[DecoderKeys.NORMAL_DECKEY_NUM];
            for (int i = 0; i < charOrKeys.Length; ++i) {
                charOrKeys[i] = faces._safeSubstring(i, 1);
            }
            StrokeTables.Add(new StrokeTableDef {
                KanaAlign = false,
                Faces = null,
                CharOrKeys = charOrKeys,
            });
        }

        private string makeMultiCharStr(char[] chars, int pos)
        {
            var sb = new StringBuilder();
            if (pos < chars.Length) {
                sb.Append(chars[pos]);
                if (pos + 1 < chars.Length && chars[pos + 1] != 0) sb.Append(chars[pos + 1]);
            }
            return sb.ToString();
        }

        public void CopyInitialVkbTable(char[] table)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"CALLED");
            int len = Math.Min(table.Length / 2, initialVkbChars.Length);
            for (int i = 0; i < len; ++i) {
                initialVkbChars[i] = makeMultiCharStr(table, i * 2);
            }
        }

        //public void CopyHiraganaVkbTable(char[] table)
        //{
        //    copyKanaVkbTable(table, kanaVkbChars[0]);
        //    if (table.Length > 100) copyKanaVkbTable(table.Skip(100).ToArray(), kanaVkbChars[1]);
        //}

        //public void CopyKatakanaVkbTable(char[] table)
        //{
        //    copyKanaVkbTable(table, kanaVkbChars[2]);
        //    if (table.Length > 100) copyKanaVkbTable(table.Skip(100).ToArray(), kanaVkbChars[3]);
        //}

        //private void copyKanaVkbTable(char[] table, string[] kanaTable)
        //{
        //    for (int i = 0; i < kanaTable.Length; ++i) {
        //        int x = i / 5;
        //        int y = i % 5;
        //        kanaTable[(y + 1) * 10 - (x + 1)] = makeMultiCharStr(table, i * 2);
        //    }
        //}

        //-------------------------------------------------------------------------------
        /// <summary> 第1打鍵待ち状態の仮想キーボード表示 </summary>
        public void DrawInitailVkb(int lastDeckey = -1)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info(() => $"CALLED: EffectiveCount={Settings.VirtualKeyboardShowStrokeCountEffective}");
            if (Settings.VirtualKeyboardShowStrokeCountEffective == 1) {
                // 第1打鍵待ちである
                StrokeTableDef tblDef = null;
                // 主コードテーブルか
                bool isPrimary = frmMain.DecoderOutput.IsCurrentStrokeTablePrimary();
                if ((isPrimary && StrokeTables._isEmpty()) || (!isPrimary && StrokeTables2 == null)) {
                    tblDef = null;
                } else {
                    tblDef = isPrimary ? StrokeTables[selectedTable._lowLimit(0) % StrokeTables.Count] : StrokeTables2;
                }
                if (tblDef == null) {
                    drawNormalVkb(initialVkbChars, true);
                } else if (tblDef.Faces == null) {
                    drawNormalVkb(tblDef.CharOrKeys, isPrimary && !tblDef.ShiftPlane, lastDeckey);
                } else {
                    drawVkb5x10Table(tblDef);
                }
                showNonActive();
            } else {
                this.Hide();
            }
        }

        public string[] DrawStrokeHelp(char[] chars)
        {
            char[] result = chars._isEmpty() ? null : frmMain.CallDecoderFunc("reorderByFirstStrokePosition", chars._toString());

            var charOrKeys = new string[DecoderKeys.NORMAL_DECKEY_NUM];
            if (result._notEmpty()) {
                for (int i = 0; i < charOrKeys.Length; ++i) {
                    charOrKeys[i] = makeMultiCharStr(result, i * 2);
                }
            }
            drawNormalVkb(charOrKeys, false);
            return charOrKeys;
        }

        private void drawNormalVkb(string[] strokeTable, bool bNormalPlane, int lastDeckey = -1)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.DebugH($"\nlastDeckey={lastDeckey}");

            resetVkbControls("", VkbNormalWidth, VkbPictureBoxHeight_Normal, VkbCenterBoxHeight_Normal);
            using (PictureBoxDrawer drawer = new PictureBoxDrawer(pictureBox_Main)) {
                drawNormalVkbFrame(drawer.Gfx, lastDeckey);
                drawCenterChars(drawer.Gfx);
                drawNormalVkbStrings(drawer.Gfx, i => strokeTable[i], bNormalPlane);
            }
            changeFormHeight(pictureBox_Main.Top + pictureBox_Main.Height + 1);
        }

        private void drawVkb5x10Table(StrokeTableDef def)
        {
            resetVkbControls("", Vkb5x10Width, VkbPictureBoxHeight_5x10Table, VkbCenterBoxHeight_5x10Table);
            using (PictureBoxDrawer drawer = new PictureBoxDrawer(pictureBox_Main)) {
                drawVkb5x10TableFrame(drawer.Gfx);
                //drawCenterChars(drawer.Gfx);
                drawVkb5x10TableStrings(drawer.Gfx, def);
            }
            changeFormHeight(pictureBox_Main.Top + pictureBox_Main.Height + 1);
        }

        /// <summary> 仮想キーボードの上部出力領域に文字列を出力する </summary>        
        public void SetTopText(string text, bool bRightAlign = false)
        {
            if (text != null) topTextBox.Text = text;
            if (bRightAlign) {
                topTextBox.SelectionStart = text._safeLength();
                topTextBox.SelectionLength = 0;
            }
        }

        public void SetTopText(char[] text)
        {
            int maxTopLen = LongVkeyCharSize - 2;

            int i = 0;
            for (; i < 32; ++i) {
                if (text[i] == 0) break;
            }

            int s = i > maxTopLen ? i - maxTopLen : 0;
            SetTopText(new string(text, s, i - s), true);
        }

        /// <summary> 第1打鍵待ち受け時に表示するストロークテーブルの切り替え </summary>
        public void RotateStrokeTable(int delta = 1)
        {
            if (frmMain.DecoderOutput.IsCurrentStrokeTablePrimary() && StrokeTables._notEmpty()) {
                if (delta < 0) delta = StrokeTables.Count - ((-delta) % StrokeTables.Count);
                selectedTable = (selectedTable + delta) % StrokeTables.Count;
                DrawVirtualKeyboardChars();
            }
        }

        // ASCII文字は 0.5文字としてカウント
        private static float calcCharsAsFullwide(string str)
        {
            if (str._isEmpty()) return 0;
            float ascCnt = str.Count(x => x <= 0x7f);
            return str.Length - ascCnt * 0.5f; 
        }

        /// <summary> 仮想キーボードにヘルプや文字候補を表示 </summary>
        public void DrawVirtualKeyboardChars(int lastDeckey = -1)
        {
            var decoderOutput = frmMain.DecoderOutput;

            if (Settings.LoggingVirtualKeyboardInfo) logger.Info(() => $"CALLED: layout={decoderOutput.layout}, center={CommonState.CenterString}, strokeCount={decoderOutput.strokeCount}, nextDeckey={decoderOutput.nextStrokeDeckey}, lastDeckey={lastDeckey}");

            if (decoderOutput.topString._isEmpty()) return;

            const int maxTopLen = LongVkeyCharSize - 2;

            string makeTopString()
            {
                int i = 0;
                for (; i < 32; ++i) {
                    if (decoderOutput.topString[i] == 0) break;
                }
                int s = i > maxTopLen ? i - maxTopLen : 0;
                return new string(decoderOutput.topString, s, i - s);

            }
            var topText = makeTopString();

            if (decoderOutput.layout >= (int)VkbLayout.Horizontal && decoderOutput.layout < (int)VkbLayout.Normal) {
                // 10件横列配列
                CommonState.CenterString = "";  // 中央鍵盤文字列をクリアしておく(後で部首合成ヘルプのときに関係ない文字を拾ったりしないように)
                resetVkbControls(topText, 0, 0, 0);
                int nRow = 0;
                for (int i = 0; i < LongVkeyNum; ++i) {
                    //logger.Info(decoderOutput.faceStrings.Skip(i*20).Take(20).Select(c => c.ToString())._join(""));
                    if (drawHorizontalCandidateCharsWithColor(decoderOutput, i, decoderOutput.faceStrings)) ++nRow;
                }
                dgvHorizontal.CurrentCell = null;   // どのセルも選択されていない状態にする
                dgvHorizontal.Height = (int)(VkbCellHeight * nRow + 1);
                changeFormHeight(dgvHorizontal.Top + dgvHorizontal.Height + 1);
                showNonActive();
                return;
            }

            pictureBox_Main.Show();
            dgvHorizontal.Hide();

            if (decoderOutput.layout >= (int)VkbLayout.Vertical && decoderOutput.layout < (int)VkbLayout.Horizontal) {
                // 10件縦列配列(部首合成ヘルプを含む)
                renewCandidateVerticalFont();
                renewCenterVerticalFont();
                var candArray = getCandidateStrings(decoderOutput.faceStrings);
                float height = (int)(candArray.Select(s => calcCharsAsFullwide(s)).Max()._lowLimit(MinVerticalChars) * verticalFontInfo.CharHeight) + 5;
                float centerHeight = height._max(CommonState.CenterString._safeLength()._lowLimit(MinCenterChars) * centerFontInfo.CharHeight + 5);
                resetVkbControls(topText, VkbNormalWidth, height, centerHeight);
                using (PictureBoxDrawer drawer = new PictureBoxDrawer(pictureBox_Main)) {
                    drawVerticalVkbFrame(drawer.Gfx);
                    drawCenterCharsWithColor(drawer.Gfx, decoderOutput);
                    for (int i = 0; i < LongVkeyNum; ++i) {
                        drawVerticalCandidateCharsWithColor(drawer.Gfx, decoderOutput, i, decoderOutput.faceStrings);
                    }
                }
                changeFormHeight(pictureBox_Main.Top + pictureBox_Main.Height + 1);
                showNonActive();
                return;
            }
            if (decoderOutput.layout >= (int)VkbLayout.Normal && decoderOutput.layout < (int)VkbLayout.KanaTable) {
                // 2打鍵目以降の通常配列
                if (frmMain.IsVkbShown) {
                    resetVkbControls(topText, VkbNormalWidth, VkbPictureBoxHeight_Normal, VkbCenterBoxHeight_Normal);
                    using (PictureBoxDrawer drawer = new PictureBoxDrawer(pictureBox_Main)) {
                        topTextBox.Show();
                        SetTopText(topText, true);
                        drawNormalVkbFrame(drawer.Gfx, decoderOutput.nextStrokeDeckey._geZeroOr(lastDeckey));
                        drawCenterCharsWithColor(drawer.Gfx, decoderOutput);
                        drawNormalVkbStrings(drawer.Gfx, i => makeMultiCharStr(decoderOutput.faceStrings, i * 2), false);
                    }
                    changeFormHeight(pictureBox_Main.Top + pictureBox_Main.Height + 1);
                    showNonActive();
                } else {
                    this.Hide();
                }
                return;
            }

            if (decoderOutput.layout >= (int)VkbLayout.KanaTable) {
                // 50音図配列
                return;
            }

            // 初期状態に戻す
            DrawInitailVkb(lastDeckey);
            SetTopText(topText, true);
        }

        // 仮想鍵盤の高さを変更し、必要ならウィンドウを移動する
        private void changeFormHeight(int newHeight)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"ENTER: oldHeight={this.Height}, newHeight={newHeight}");
            int oldHeight = this.Height;
            this.Invalidate();
            this.Height = newHeight;
            if (newHeight != oldHeight) {
                // ウィンドウ位置の再取得を行わずに移動するので正しくない場所に表示される可能性はあるが、たいていの場合は大丈夫だろう
                frmMain.MoveFormVirtualKeyboard();
            }
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"LEAVE: this.Width={this.Width}, this.Height={this.Height}");
        }

        public class PictureBoxDrawer : IDisposable
        {
            public PictureBoxDrawer(PictureBox picBox)
            {
                box = picBox;
                img = new Bitmap(box.Width, box.Height);
                Gfx = Graphics.FromImage(img);
            }
            private PictureBox box;
            private Bitmap img;
            public Graphics Gfx;

            public void Dispose()
            {
                if (Gfx != null) {
                    Gfx.Dispose();
                    Gfx = null;
                    var oldImg = box.Image;
                    box.Image = img;
                    oldImg?.Dispose();
                }
            }
        }

        /// <summary>選択候補文字列の配列を取得 </summary>
        /// <param name="candChars"></param>
        /// <returns></returns>
        private string[] getCandidateStrings(char[] candChars)
        {
            return LongVkeyNum._range().Select(n => {
                int pos = LongVkeyCharSize * n;
                int end = (pos + LongVkeyCharSize)._highLimit(candChars.Length);
                int len = candChars._findIndex(pos, end, x => x == 0) - pos;
                if (len < 0) len = end - pos;
                return new string(candChars, pos, len);
            }).ToArray();
        }

        /// <summary> ミニバッファフォント </summary>
        private CFontInfo minibufFontInfo = new CFontInfo("MiniBuf", false, false);

        // ミニバッフ用フォントの更新
        private void renewMinibufFont()
        {
            if (renewFontInfo(minibufFontInfo, Settings.MiniBufVkbFontSpec)) {
                topTextBox.Font = minibufFontInfo.MyFont;
            }
            //logger.Info(() => $"CALLED: new minibufFontSpec={Settings.MiniBufVkbFontSpec}, old={minibufFontSpec}");
            //if (minibufFont == null || minibufFontSpec._ne(Settings.MiniBufVkbFontSpec)) {
            //    minibufFontSpec = Settings.MiniBufVkbFontSpec;
            //    var fontItems = minibufFontSpec._split('|').Select(x => x._strip()).ToArray();
            //    minibufFont?.Dispose();
            //    string fontName = fontItems._getNth(0)._orElse("MS UI Gothic");
            //    int fontSize = fontItems._getNth(1)._parseInt(9)._lowLimit(8);
            //    minibufFont = new Font(fontName, fontSize);
            //    topTextBox.Font = new Font(fontName, fontSize);
            //    logger.Info(() => $"new minibufFont={fontName}|{fontSize}");
            //}
        }

        /// <summary>
        /// 仮想鍵盤を構成するコントロールの再配置
        /// </summary>
        private void resetVkbControls(string topText, float picBoxWidth, float picBoxHeight, float centerHeight)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"picBoxWidth={picBoxWidth:f3}, picBoxHeight={picBoxHeight:f3}, centerHeight={centerHeight:f3}");
            renewMinibufFont();
            topTextBox.Width = (int)(VkbNormalWidth);
            topTextBox.Show();
            SetTopText(topText, true);
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"topTextBox.Width={topTextBox.Width}, topTextBox.Height={topTextBox.Height}");
            renewHorizontalKeyboard();
            //dgvHorizontal.Top = topTextBox.Height;

            if (picBoxWidth > 0 && picBoxHeight > 0) {
                pictureBox_Main.Top = topTextBox.Height;
                pictureBox_Main.Width = (int)picBoxWidth;
                var height = picBoxHeight._max(centerHeight);
                pictureBox_Main.Height = (int)height;
                pictureBox_Main.Show();
                dgvHorizontal.Hide();
                setVerticalBoxHeight(height, centerHeight);
            } else {
                pictureBox_Main.Hide();
                dgvHorizontal.Top = topTextBox.Height;
                //dgvHorizontal.Width = topTextBox.Width;
                dgvHorizontal.Width = (int)VkbNormalWidth;
                dgvHorizontal.Show();
                //dgvHorizontal.Top = topTextBox.Height;
                if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"dgv.Top={dgvHorizontal.Top}, dgv.Width={dgvHorizontal.Width}");
            }
            this.Width = (int)(VkbNormalWidth + 2);
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"LEAVE: this.Width={this.Width}, topText.Width={topTextBox.Width}");
        }

        //-------------------------------------------------------------------------------
        // 通常仮想鍵盤サポート

        /// <summary> 通常鍵盤横書きフォント </summary>
        private CFontInfo normalFontInfo = new CFontInfo("Normal", false, true);

        //private string normalFontSpec = "";
        private Font normalFont => normalFontInfo.MyFont;

        //float normalFontLeftPadding = 2;
        //float normalFontTopPadding = 4;

        // 通常仮想鍵盤用フォントの更新
        private CFontInfo.Padding renewNormalFont()
        {
            renewFontInfo(normalFontInfo, Settings.NormalVkbFontSpec);
            return normalFontInfo.GetNthPadding(CurrentScreen);
        }

        /// <summary>
        /// 通常仮想鍵盤文字列の表示<br/>
        /// nthString は全角1文字分だけを返すこと。
        /// </summary>
        private void drawNormalVkbStrings(Graphics g, Func<int, string> nthString, bool bFirstStrokeAndNormalPlane)
        {
            // フォントの更新
            var paddings = renewNormalFont();

            // 通常ストローク
            for (int i = 0; i < 4; ++i) {
                for (int j = 0; j < 10; ++j) {
                    float x = VkbCellWidth * j + paddings.Left + (j >= 5 ? VkbCenterWidth : 0);
                    float y = VkbCellHeight * i + paddings.Top;
                    g.DrawString(nthString(i * 10 + j), normalFont, Brushes.Black, (float)x, (float)y);
                }
            }
            // 下端機能キー
            for (int j = 0; j < 10; ++j) {
                float x = VkbBottomOffset + VkbCellWidth * j + paddings.Left;
                float y = VkbCellHeight * 4 + paddings.Top;
                var face = bFirstStrokeAndNormalPlane ? initialVkbChars[40 + j] : nthString(40 + j);
                g.DrawString(face, normalFont, Brushes.Black, (float)x, (float)y);
            }
        }

        /// <summary>
        /// 通常の仮想鍵盤の枠線と背景色の描画
        /// </summary>
        /// <param name="g"></param>
        private void drawNormalVkbFrame(Graphics g, int nextDeckey = -1)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.DebugH($"\nnextDecke={nextDeckey}");

            // 背景色
            Color getColor(string name)
            {
                var color = Color.FromName(name);
                return !color.IsEmpty ? color : SystemColors.Window;
            }

            var bgColorTop = getColor(Settings.BgColorTopLevelCells);
            var bgColorCenter = getColor(Settings.BgColorCenterSideCells);
            var bgColorHighLow = getColor(Settings.BgColorHighLowLevelCells);
            var bgColorMiddle = getColor(Settings.BgColorMiddleLevelCells);

            Brush b1 = new SolidBrush(bgColorTop);
            g.FillRectangle(b1, 1, 1, (float)(VkbCellWidth * 10 + VkbCenterWidth - 1), (float)(VkbCellHeight - 1));
            b1.Dispose();
            b1 = new SolidBrush(bgColorHighLow);
            g.FillRectangle(b1, 1, (float)(VkbCellHeight + 1), (float)(VkbCellWidth * 10 + VkbCenterWidth - 1), (float)(VkbCellHeight * 3 - 1));
            b1.Dispose();
            b1 = new SolidBrush(bgColorMiddle);
            g.FillRectangle(b1, 1, (float)(VkbCellHeight * 2 + 1), (float)(VkbCellWidth * 10 + VkbCenterWidth - 1), (float)(VkbCellHeight - 1));
            b1.Dispose();
            b1 = new SolidBrush(bgColorCenter);
            g.FillRectangle(b1, (float)(VkbCellWidth * 4 + 1), (float)(VkbCellHeight + 1), (float)(VkbCellWidth * 2 + VkbCenterWidth - 1), (float)(VkbCellHeight * 3 - 1));
            b1.Dispose();

            // 打鍵ガイド
            if (nextDeckey >= 0 && nextDeckey < 40) {
                int x = nextDeckey % 10;
                int y = nextDeckey / 10;
                float xOff = x < 5 ? 0f : VkbCenterWidth;
                b1 = new SolidBrush(getColor(Settings.BgColorNextStrokeCell));
                g.FillRectangle(b1, (float)(VkbCellWidth * x + xOff + 1), (float)(VkbCellHeight * y + 1), (float)(VkbCellWidth - 1), (float)(VkbCellHeight - 1));
                b1.Dispose();
            }

            // 中央鍵盤
            g.FillRectangle(Brushes.White, (float)(VkbCellWidth * 5 + 1), 1, (float)(VkbCenterWidth - 1), (float)(VkbCellHeight * 4 - 1));

            // 下部拡張部
            g.FillRectangle(Brushes.WhiteSmoke, (float)(VkbBottomOffset + 1), (float)(VkbCellHeight * 4 + 1), (float)(VkbCellWidth * 10 - 1), (float)(VkbCellHeight - 1));

            // 枠線
            Pen pen = Pens.DarkGray;

            // 上端横線
            float x1 = 0, y1 = 0, y2 = 0;
            float x2 = pictureBox_Main.Width;
            g.DrawLine(pen, x1, y1, x2, y2);

            // 左側横線
            x2 = VkbCellWidth * 5;
            for (int i = 1; i < 4; ++i) {
                y1 = y2 = VkbCellHeight * i;
                g.DrawLine(pen, x1, y1, x2, y2);
            }

            // 右側横線
            x1 = x2 + VkbCenterWidth;
            x2 = x1 + VkbCellWidth * 5;
            for (int i = 1; i < 4; ++i) {
                y1 = y2 = VkbCellHeight * i;
                g.DrawLine(pen, x1, y1, x2, y2);
            }

            // 下端横線
            x1 = 0;
            x2 = pictureBox_Main.Width;
            y1 = y2 = VkbCellHeight * 4;
            g.DrawLine(pen, x1, y1, x2, y2);

            // 左側縦線
            y1 = 0;
            y2 = VkbCellHeight * 4;
            for (int i = 0; i < 6; ++i) {
                x1 = x2 = VkbCellWidth * i;
                g.DrawLine(pen, x1, y1, x2, y2);
            }
            // 右側縦線
            for (int i = 0; i < 6; ++i) {
                x1 = x2 = VkbCellWidth * (i + 5) + VkbCenterWidth;
                g.DrawLine(pen, x1, y1, x2, y2);
            }

            // 下部拡張キー部
            x1 = VkbBottomOffset;
            x2 = x1;
            y1 = y2;
            y2 = y1 + VkbCellHeight;
            for (int i = 0; i <= 10; ++i) {
                g.DrawLine(pen, x1, y1, x2, y2);      // 下部縦線
                x1 += VkbCellWidth;
                x2 = x1;
            }
            x1 = VkbBottomOffset;
            x2 = x1 + VkbCellWidth * 10;
            y1 = y2;
            g.DrawLine(pen, x1, y1, x2, y2);      // 下端横線
        }

        //--------------------------------------------------------------------------------------
        // 5x10 仮想鍵盤サポート
        /// <summary>
        /// 5x10のテーブルの描画
        /// </summary>
        /// <param name="g"></param>
        private void drawVkb5x10TableFrame(Graphics g)
        {
            float width = pictureBox_Main.Width;
            float height = pictureBox_Main.Height;

            // 1段おきの背景色
            g.FillRectangle(Brushes.AliceBlue, 1, Vkb5x10CellHeight, width - 1, Vkb5x10CellHeight);
            g.FillRectangle(Brushes.AliceBlue, 1, Vkb5x10CellHeight * 3, width - 1, Vkb5x10CellHeight);

            // 枠線と縦横線の描画
            Pen pen = Pens.DarkGray;
            // 横線 
            float x1 = 0;
            float x2 = width - 1;
            for (float y = 0; y < height; y += Vkb5x10CellHeight) {
                g.DrawLine(pen, x1, y, x2, y);
            }
            // 縦線 
            float y1 = 0;
            float y2 = height - 1;
            for (float x = 0; x < width; x += Vkb5x10CellWidth) {
                g.DrawLine(pen, x, y1, x, y2);
            }
        }

        /// <summary>
        /// 5x10のテーブルに打鍵ヘルプ(キー文字x2)を表示。bKanaAlign = true なら 50音図形式で表示
        /// </summary>
        /// <param name="g"></param>
        /// <param name="outChars"></param>
        /// <param name="strokeTable"></param>
        /// <param name="bKanaAlign"></param>
        private void drawVkb5x10TableStrings(Graphics g, StrokeTableDef def)
        {
            for (int i = 0; i < def.Faces.Length && i < def.CharOrKeys.Length; ++i) {
                int x = i % 10;
                int y = i / 10;
                int cx = def.KanaAlign ? 9 - (i / 5) : x;
                int cy = def.KanaAlign ? i % 5 : y;
                g.DrawString(def.Faces.Substring(i, 1), strokeCharFont, Brushes.DarkGray, cx * Vkb5x10CellWidth + 1, cy * Vkb5x10CellHeight + Vkb5x10FaceYOffset);
                g.DrawString(def.CharOrKeys[i], strokeKeyFont, Brushes.Blue, cx * Vkb5x10CellWidth + 0, cy * Vkb5x10CellHeight + Vkb5x10KeyYOffset);
            }
        }

        //--------------------------------------------------------------------------------------
        // 横列鍵盤サポート

        /// <summary> 出力内容に応じて背景色を変えて横列鍵盤に文字列を出力する</summary>
        private bool drawHorizontalCandidateCharsWithColor(DecoderOutParams decoderOutput, int nth, char[] chars)
        {
            //bool isWaitingCandSelect() { return decoderOutput.nextExpectedKeyType == DlgKanchoku.ExpectedKeyType.CandSelect; }

            Color makeSpecifiedColor()
            {
                if (decoderOutput.IsArrowKeysRequired()) {
                    string name = null;
                    if (decoderOutput.nextSelectPos == nth) {
                        name = Settings.BgColorOnSelected;
                    } else if (decoderOutput.nextSelectPos < 0 && nth == 0) {
                        name = Settings.BgColorForFirstCandidate;
                    }
                    if (name._notEmpty()) {
                        var color = Color.FromName(name);
                        if (!color.IsEmpty) return color;
                    }
                }
                return Color.GhostWhite;
            }

            //logger.Info($"chars.Length={chars.Length}, rows={dgvHorizontal._rowsCount()}");
            if (nth >= 0 && nth < dgvHorizontal._rowsCount()) {
                int pos = nth * LongVkeyCharSize;
                int len = chars._findIndex(pos, pos + LongVkeyCharSize, x => x == 0) - pos;
                if (len < 0) len = LongVkeyCharSize;
                StringBuilder sb = new StringBuilder();
                sb.Append((nth + 1) % 10).Append(' ').Append(chars, pos, len);
                //logger.Info($"drawString={drawString}, nth={nth}, pos={pos}, len={len}");
                if (sb.Length > 2) {
                    dgvHorizontal.Rows[nth].Cells[0].Value = sb.ToString();
                    dgvHorizontal.Rows[nth].Cells[0].Style.BackColor = makeSpecifiedColor();
                    return true;
                }
            }
            return false;
        }


        //--------------------------------------------------------------------------------------
        /// <summary> 縦列鍵盤フォント情報 </summary>
        private VerticalFontInfo verticalFontInfo = new VerticalFontInfo("Vertical") {
            CharHeight = 13,
            FontSizeThreshold1 = 9.5f,
            FontSizeThreshold2 = 11.0f,
        };

        // 縦列中央鍵盤用フォントの更新
        private void renewCenterVerticalFont()
        {
            centerFontInfo.renewFontInfo(Settings.CenterVkbFontSpec, VkbCenterWidth, pictureBox_measureFontSize);
        }

        // 縦列候補鍵盤用フォントの更新
        private void renewCandidateVerticalFont()
        {
            verticalFontInfo.renewFontInfo(Settings.VerticalVkbFontSpec, VkbCellWidth, pictureBox_measureFontSize);
        }

        // 縦列鍵盤用フォントの更新
        private void renewVerticalFont(string newSpec, float boxWidth, VerticalFontInfo info)
        {
            info.renewFontInfo(newSpec, boxWidth, pictureBox_measureFontSize);
            //if (info.VerticalFont == null || info.FontSpec._ne(newSpec)) {
            //    logger.Info(() => $"CALLED: new fontSpec={newSpec}");
            //    info.FontSpec = newSpec;
            //    var fontItems = info.FontSpec._split('|').Select(x => x._strip()).ToArray();
            //    info.VerticalFont?.Dispose();
            //    info.HorizontalFont?.Dispose();
            //    string fontName = fontItems._getNth(0)._orElse("@MS Gothic");
            //    info.VerticalFont = new Font(fontName, fontItems._getNth(1)._parseInt(9)._lowLimit(8));
            //    fontName = fontName._safeReplace("@", "");          // 先頭の @ を削除しておく
            //    info.HorizontalFont = new Font(fontName, fontItems._getNth(1)._parseInt(9)._lowLimit(8));
            //    // MS Gothic(9) -> (13, 12), Meiryo(9) -> (13, 18), Yu Gothic(9) -> (13, 16)
            //    (int fh, int fw) = measureFontSize(info.VerticalFont);
            //    info.CharHeight = fh + 1;
            //    info.LeftPadding = fontItems._getNth(2)._parseInt(-999);
            //    if (info.LeftPadding < -50) {
            //        info.LeftPadding = (boxWidth - fw - (fw >= 15 ? 3 : fw == 14 ? 2 : 0)) / 2;
            //    }
            //    info.TopPadding = fontItems._getNth(3)._parseInt(-999);
            //    if (info.TopPadding < -50) {
            //        if (fontName._startsWith("Yu ") || fontName._startsWith("游")) {
            //            info.TopPadding = fh <= 16 ? 2 : 1;
            //        } else if (fontName._startsWith("Meiryo") || fontName._startsWith("メイリオ")) {
            //            info.TopPadding = fh <= 18 ? 1 : 0;
            //        } else {
            //            // MS Gothic と想定
            //            info.TopPadding = fh < 13 ? 3 : (fw <= 13 ? 3 : 2);
            //        }
            //    }
            //    if (Logger.IsInfoHEnabled) logger.Info($"new verticalFont Width={fw}, Height={fh}, charHeight={info.CharHeight}, padLeft={info.LeftPadding}, padTop={info.TopPadding}");
            //}
        }

        /// <summary> 出力文字数に応じて縦列鍵盤の高さを設定</summary>
        private void setVerticalBoxHeight(float height, float centerHeight)
        {
            for (int i = 0; i < 10; ++i) {
                verticalBoxes[i].Height = height;
            }
            centerBox.Height = centerHeight;
        }

        /// <summary> n番目の縦列鍵盤の背景色を取得</summary>
        private Color nthVerticalBgColor(int nth)
        {
            return verticalBoxes._getNth(nth, verticalBoxes[0]).BackColor;
        }

        /// <summary> 縦列鍵盤の背景色と枠線の描画 </summary>
        /// <param name="g"></param>
        private void drawVerticalVkbFrame(Graphics g)
        {
            // 背景色
            g.FillRectangle(Brushes.White, 1, 1, pictureBox_Main.Width - 2, pictureBox_Main.Height - 2);

            // 枠線
            Pen pen = Pens.DarkGray;

            // 上端横線
            float x1 = 0, y1 = 0, y2 = 0;
            float x2 = pictureBox_Main.Width - 1;
            g.DrawLine(pen, x1, y1, x2, y2);

            // 下端横線
            x1 = 0;
            x2 = pictureBox_Main.Width - 1;
            y1 = y2 = pictureBox_Main.Height - 1;
            g.DrawLine(pen, x1, y1, x2, y2);

            // 左側縦線
            y1 = 0;
            y2 = pictureBox_Main.Height - 1;
            for (int i = 0; i < 6; ++i) {
                x1 = x2 = VkbCellWidth * i;
                g.DrawLine(pen, x1, y1, x2, y2);
            }
            // 右側縦線
            for (int i = 0; i < 6; ++i) {
                x1 = x2 = VkbCellWidth * (i + 5) + VkbCenterWidth;
                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }

        /// <summary> 出力内容に応じて背景色を変えて中央鍵盤に文字列を出力する</summary>
        private void drawCenterCharsWithColor(Graphics g, DecoderOutParams decoderOutput)
        {
            Color makeSpecifiedColor()
            {
                string name = null;
                if (decoderOutput.IsWaiting2ndStroke()) {
                    name = Settings.BgColorOnWaiting2ndStroke;
                } else if (decoderOutput.IsMazeCandSelecting()) {
                    name = Settings.BgColorForMazegaki;
                } else if (decoderOutput.IsHistCandSelecting()) {
                    name = Settings.BgColorForHistOrAssoc;
                } else if (decoderOutput.IsAssocCandSelecting()) {
                    name = Settings.BgColorForHistOrAssoc;
                } else if (decoderOutput.IsBushuCompHelp()) {
                    name = Settings.BgColorForBushuCompHelp;
                } else if (decoderOutput.IsOtherStatus()) {
                    name = "Yellow";    // とりあえず Yellow 固定
                }
                if (name._notEmpty()) {
                    var color = Color.FromName(name);
                    if (!color.IsEmpty) return color;
                }
                return decoderOutput.IsCurrentStrokeTablePrimary() ? SystemColors.Window : Color.FromName(Settings.BgColorForSecondaryTable);
            }

            drawCenterChars(g, makeSpecifiedColor());
        }

        /// <summary> デフォルト背景色で中央鍵盤に文字列を出力する</summary>
        private void drawCenterChars(Graphics g)
        {
            //drawCenterChars(g, SystemColors.Window);
            drawCenterCharsWithColor(g, frmMain.DecoderOutput);
        }

        /// <summary> 指定の背景色で中央鍵盤に文字列を出力する</summary>
        private void drawCenterChars(Graphics g, Color bgColor)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Debug(() => $"center={CommonState.CenterString}");
            renewCenterVerticalFont();
            //int leftPadding = centerFontInfo.LeftPadding;
            //if (CommonState.CenterString._safeLength() == 1) {
            //    leftPadding = centerFontInfo.VerticalFont.Size >= 11.5 ? 0 : centerFontInfo.VerticalFont.Size >= 10.5 ? 1 : 2;
            //}
            drawVerticalString(g, centerBox, CommonState.CenterString, centerFontInfo, bgColor);
        }


        /// <summary> 出力内容に応じて背景色を変えて縦列鍵盤に文字列を出力する</summary>
        private void drawVerticalCandidateCharsWithColor(Graphics g, DecoderOutParams decoderOutput, int nth, char[] chars)
        {
            Color makeSpecifiedColor()
            {
                //logger.Debug(() => $"candSelecting={mainDlg.IsCandSelecting}, nextSelectPos={decoderOutput.nextSelectPos}, nth={nth}");
                if (decoderOutput.layout != (int)VkbLayout.BushuCompHelp && decoderOutput.IsArrowKeysRequired()) {
                    // 部首合成ヘルプでなく、矢印キー(つまり候補選択)が要求されているとき
                    string name = null;
                    if (decoderOutput.nextSelectPos == nth) {
                        name = Settings.BgColorOnSelected;
                        //logger.Debug(() => $"ColorName={name}");
                    } else if (decoderOutput.nextSelectPos == -1 && nth == 0) {
                        // Vertical では -1 の場合のみ、選択待ちの色付けをする
                        name = Settings.BgColorForFirstCandidate;
                        //logger.Debug(() => $"ColorName={name}");
                    }
                    if (name._notEmpty()) {
                        var color = Color.FromName(name);
                        if (!color.IsEmpty) return color;
                    }
                }
                return nthVerticalBgColor(nth);
            }

            if (nth >= 0 && nth < verticalBoxes.Length) {
                //renewCandidateVerticalFont();
                var drawStr = charsToString(chars, nth * LongVkeyCharSize);
                //int leftPadding = verticalFontInfo.LeftPadding;
                //if (drawStr._safeLength() == 1) {
                //    leftPadding = verticalFontInfo.VerticalFont.Size >= 11.0 ? 0 : verticalFontInfo.VerticalFont.Size >= 9.5 ? 1 : 2;
                //}
                drawVerticalString(g, verticalBoxes[nth], drawStr, verticalFontInfo, makeSpecifiedColor());
            }
        }

        private string charsToString(char[] chars, int startPos)
        {
            int len = 0;
            string drawString = "";
            if (chars._notEmpty()) {
                len = chars._findIndex(startPos, startPos + LongVkeyCharSize, x => x == 0) - startPos;
                if (len < 0) len = LongVkeyCharSize._highLimit(chars.Length);
                drawString = new string(chars, startPos, len);
            }
            return drawString;
        }

        /// <summary> 指定のフォント、背景色で縦列または中央鍵盤に文字列を出力する</summary>
        private void drawVerticalString(Graphics g, VerticalBox box, string drawStr, VerticalFontInfo info, Color bgColor)
        {
            // 背景色
            var brush = new SolidBrush(bgColor);
            g.FillRectangle(brush, box.X + 1, box.Y + 1, box.Width - 1, box.Height - 2);
            brush.Dispose();

            //int len = 0;
            //string drawString = "";
            //if (chars._notEmpty()) {
            //    len = chars._findIndex(startPos, startPos + LongVkeyCharSize, x => x == 0) - startPos;
            //    if (len < 0) len = LongVkeyCharSize._highLimit(chars.Length);
            //    drawString = new string(chars, startPos, len);
            //}
            //StringFormatを作成
            StringFormat sf = new StringFormat();
            Font font = info.HorizontalFont;
            float topPadding = info.TopPadding;
            if (drawStr._safeLength() > 1) {
                //2文字以上なら縦書きにする
                sf.FormatFlags = StringFormatFlags.DirectionVertical;
                font = info.VerticalFont;
            } else {
                // 1文字は横書きなので、topPadding を別に設定
                topPadding = 4;
            }
            //文字を表示
            g.DrawString(drawStr, font, Brushes.Black, box.X + info.AdjustedLeftPadding(drawStr), box.Y + topPadding, sf);
        }

        //------------------------------------------------------------------
        // イベントハンドラ
        //------------------------------------------------------------------
        private Point mousePoint = new Point();

        private void pictureBox_Main_Click(object sender, EventArgs e)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.DebugH("CALLED");
            if (((MouseEventArgs)e).Button == MouseButtons.Left) {
                if (!Settings.VirtualKeyboardPosFixedTemporarily) {
                    frmMain.ToggleActiveState();
                    if (Settings.LoggingVirtualKeyboardInfo) logger.DebugH("ToggleActiveState");
                }
            }
        }

        private void pictureBox_Main_MouseDown(object sender, MouseEventArgs e)
        {
            //if (Settings.LoggingVirtualKeyboardInfo) logger.DebugH($"\nMouseDown: X={e.X}, Y={e.Y}");
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left) {
                //位置を記憶する
                mousePoint.X = e.X;
                mousePoint.Y = e.Y;
                Settings.VirtualKeyboardPosFixedTemporarily = false;
            }
        }

        private void pictureBox_Main_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left) {
                if (Settings.LoggingVirtualKeyboardInfo) logger.DebugH($"\nMouseMovePos: X={e.X}, Y={e.Y}");
                this.Left += e.X - mousePoint.X;
                this.Top += e.Y - mousePoint.Y;
                Settings.VirtualKeyboardPosFixedTemporarily = true;
            }
        }

        private void pictureBox_Main_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //logger.Info($"CALLED: e.Delta={e.Delta}, scrollLines={SystemInformation.MouseWheelScrollLines}");
            //frmMain.RotateStrokeTable(e.Delta * SystemInformation.MouseWheelScrollLines / 120);
            frmMain.RotateStrokeTable(-e.Delta / 120);
        }

        // 終了
        private void Exit_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Debug("ENTER");
            //frmMain.DeactivateDecoder();
            //logger.Debug("Decoder OFF");
            //if (!Settings.ConfirmOnClose || SystemHelper.OKCancelDialog("漢直窓を終了します。\r\nよろしいですか。")) {
            //    this.Close();
            //    logger.Debug("this.Closed");
            //    frmMain.Close();
            //    logger.Debug("Main.Closed");
            //}
            frmMain.Terminate();
            if (Settings.LoggingVirtualKeyboardInfo) logger.Debug("LEAVE");
        }

        private void BushuAssocReload_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmMain.ExecCmdDecoder("mergeBushuAssoc", null);
        }

        // 上部出力文字列に何かをペーストされたときのアクション
        private void sendWord(string str)
        {
            str = str._strip()._reReplace("  +", " ");
            if (str._notEmpty()) {
                if (str.Length == 1 || (str.Length == 2 && str._isSurrogatePair())) {
                    frmMain.ShowStrokeHelp(str);
                } else if (str[1] == '=') {
                    frmMain.ExecCmdDecoder("mergeBushuAssocEntry", str);
                } else if (str._reMatch("^[^ ]+ /")) {
                    frmMain.ExecCmdDecoder("addMazegakiEntry", str);
                } else {
                    frmMain.ExecCmdDecoder("addHistEntry", str);
                }
            }
        }

        private void Settings_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmMain.DeactivateDecoder();
            if (!DlgSettings.BringTopMostShownDlg()) {
                var dlg = new DlgSettings(frmMain, this, null);
                dlg.ShowDialog();
                bool bRestart = dlg.RestartRequired;
                bool bNoSave = dlg.NoSave;
                dlg.Dispose();
                if (bRestart) frmMain.Restart(bNoSave);
            }
        }

        private void dgvHorizontal_SelectionChanged(object sender, EventArgs e)
        {
            dgvHorizontal.CurrentCell = null;   // どのセルも選択されていない状態に戻す
        }

        private void DlgVirtualKeyboard_VisibleChanged(object sender, EventArgs e)
        {
            CommonState.VkbVisible = this.Visible;
            CommonState.VkbVisibiltyChangedDt = DateTime.Now;
        }

        // 辞書内容を保存して再起動
        private void RestartWithSave_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Debug("ENTER");
            frmMain.Restart(false);
            if (Settings.LoggingVirtualKeyboardInfo) logger.Debug("LEAVE");
        }

        // 辞書内容を破棄して再起動
        private void RestartWithDiscard_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Debug("ENTER");
            frmMain.Restart(true);
            if (Settings.LoggingVirtualKeyboardInfo) logger.Debug("LEAVE");
        }

        private void ReadBushuDic_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmMain.ReloadBushuDic();
        }

        private void ReadMazeWikipediaDic_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmMain.ReadMazegakiWikipediaDic();
        }

        private void ReloadSettings_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmMain.ReloadSettingsAndDefFiles();
        }

        private void FollowCaret_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.VirtualKeyboardPosFixedTemporarily = false;
        }
    }
}

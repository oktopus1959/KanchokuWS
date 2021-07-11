﻿using System;
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

        private const int VkbNormalWidth = 201;       // = Vkb5x10CellWidth * 10 + 1 = VkbCellWidth * 10 + VkbCenterWidth + 1

        private const int VkbCellHeight = 18;
        private const int VkbCellWidth = 18;
        private const int VkbCenterWidth = 20;
        private const int VkbBottomOffset = VkbCenterWidth / 2;

        private const int Vkb5x10CellWidth = 20;
        private const int Vkb5x10CellHeight = 37;
        private const int Vkb5x10FaceYOffset = 3;
        private const int Vkb5x10KeyYOffset = Vkb5x10CellHeight - 18;

        private const int VkbPictureBoxHeight_Normal = VkbCellHeight * 5 + 1;
        private const int VkbPictureBoxHeight_5x10Table = Vkb5x10CellHeight * 5 + 1;

        private const int VkbCenterBoxHeight_Normal = VkbCellHeight * 4;
        private const int VkbCenterBoxHeight_5x10Table = VkbPictureBoxHeight_5x10Table;

        private const int LongVkeyNum = 10;
        private const int LongVkeyCharSize = 20;

        private const int MinVerticalChars = 2;
        private const int MinCenterChars = 2;

        [DllImport("user32.dll")]
        private static extern void ShowWindow(IntPtr hWnd, int nCmdShow);

        // ウィンドウをアクティブにせずに表示する
        private const int SW_SHOWNA = 8;

        private void showNonActive()
        {
            ShowWindow(this.Handle, SW_SHOWNA);   // NonActive
        }

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
            normalFont?.Dispose();
            centerFontInfo.Dispose();
            verticalFontInfo.Dispose();
            strokeCharFont?.Dispose();
            strokeKeyFont?.Dispose();
        }

        /// <summary> フォームのロード </summary>
        private void DlgVirtualKeyboard_Load(object sender, EventArgs e)
        {
            // focus move
            topTextBox.actionOnPaste = sendWord;        // 上部出力文字列に何かをペーストされたときのアクション

            this.Width = VkbNormalWidth + 2;
            topTextBox.Width = VkbNormalWidth;
            pictureBox_Main.Width = VkbNormalWidth;
            pictureBox_Main.BackColor = Color.White;

            // 横書き鍵盤の初期化
            drawHorizontalKeyboard(dgvHorizontal, LongVkeyNum, pictureBox_Main.Width - 1);

            // 縦書き用オブジェクトの生成
            createObjectsForDrawingVerticalChars();

            // 仮想鍵盤の初期化
            DrawInitailVkb();
        }

        // 横列鍵盤用グリッドの表示
        private void drawHorizontalKeyboard(DataGridView dgv, int nRow, int cellWidth)
        {
            dgv.Height = nRow * VkbCellHeight + 1;
            dgv._defaultSetup(0, VkbCellHeight);       // headerHeight=0 -> ヘッダーを表示しない
            dgv._setSelectionColorReadOnly();
            dgv._setDefaultFont(DgvHelpers.FontUIG9);
            //dgv._setDefaultFont(DgvHelpers.FontMSG8);
            dgv._disableToolTips();
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly("horizontal", "", cellWidth)._setUnresizable());

            dgv.Rows.Add(nRow);
        }

        //-----------------------------------------------------------------------------------------
        // 第1打鍵待ちのときに表示されるストロークテーブル
        public class StrokeTableDef
        {
            public bool KanaAlign;
            public string Faces;
            public string[] CharOrKeys;
        }

        private int selectedTable = 0;
        private List<StrokeTableDef> StrokeTables = new List<StrokeTableDef>();

        private string[] initialVkbChars = new string[HotKeys.NUM_STROKE_HOTKEY] {
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
        //    new string[HotKeys.NUM_STROKE_HOTKEY] {
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "・", "・", "・", "・", "・", "・", "・", "・", "・", "・",
        //    },
        //    new string[HotKeys.NUM_STROKE_HOTKEY] {
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "・", "・", "・", "・", "・", "・", "・", "・", "・", "・",
        //    },
        //    new string[HotKeys.NUM_STROKE_HOTKEY] {
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "　", "　", "　", "　", "　", "　", "　", "　", "　", "　",
        //        "・", "・", "・", "・", "・", "・", "・", "・", "・", "・",
        //    },
        //    new string[HotKeys.NUM_STROKE_HOTKEY] {
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
            logger.InfoH(() => $"ENTER: filePath={filePath}");
            if (Helper.FileExists(filePath)) {
                try {
                    foreach (var line in System.IO.File.ReadAllLines(filePath)) {
                        var items = line.Trim()._reReplace("  +", " ")._split(' ');
                        if (items._notEmpty() && items[0]._notEmpty() && !items[0].StartsWith("#")) {
                            var cmd = items[0]._toLower();
                            logger.InfoH(() => $"cmd={cmd}, param={items._getNth(1)}");
                            if (cmd == "initialtable") {
                                // 初期表示を追加(初期表示は事前に作成されている)
                                StrokeTables.Add(new StrokeTableDef {
                                    KanaAlign = false,
                                    Faces = null,
                                    CharOrKeys = initialVkbChars,
                                });
                            } else if (cmd == "extracharsposition") {
                                makeVkbStrokeTable("makeExtraCharsStrokePositionTable", null);
                            } else if (cmd == "hiraganakey1") {
                                makeVkbStrokeTable("makeStrokeKeysTable", kanaOutChars[0], true, true);
                            } else if (cmd == "hiraganakey2") {
                                makeVkbStrokeTable("makeStrokeKeysTable", kanaOutChars[1], true, true);
                            } else if (cmd == "katakanakey1") {
                                makeVkbStrokeTable("makeStrokeKeysTable", kanaOutChars[2], true, true);
                            } else if (cmd == "katakanakey2") {
                                makeVkbStrokeTable("makeStrokeKeysTable", kanaOutChars[3], true, true);
                            } else if (items.Length == 2 && items[1]._notEmpty()) {
                                if (cmd == "strokeposition") {
                                    makeVkbStrokeTable("reorderByFirstStrokePosition", items[1]);
                                } else if (cmd == "strokepositionfixed") {
                                    makeVkbStrokeTableFixed(items[1]);
                                } else if (cmd == "strokekey") {
                                    makeVkbStrokeTable("makeStrokeKeysTable", items[1], true, false);
                                }
                            }
                        }
                    }
                } catch (Exception e) {
                    logger.Error($"Cannot read file: {filePath}: {e.Message}");
                }
            }
            logger.InfoH("LEAVE");
        }

        private void makeVkbStrokeTable(string cmd, string faces, bool drawFaces = false, bool kana = false)
        {
            var result = frmMain.CallDecoderFunc(cmd, faces);
            if (result != null) {
                var charOrKeys = new string[HotKeys.NUM_STROKE_HOTKEY];
                for (int i = 0; i < charOrKeys.Length; ++i) {
                    charOrKeys[i] = makeMultiCharStr(result, i * 2);
                }
                StrokeTables.Add(new StrokeTableDef {
                    KanaAlign = kana,
                    Faces = drawFaces ? faces : null,
                    CharOrKeys = charOrKeys,
                });
            }
        }

        private void makeVkbStrokeTableFixed(string faces)
        {
            var charOrKeys = new string[HotKeys.NUM_STROKE_HOTKEY];
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
            logger.InfoH($"CALLED");
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
        public void DrawInitailVkb()
        {
            if (Settings.VirtualKeyboardShowStrokeCountEffective == 1) {
                if (StrokeTables._isEmpty()) {
                    drawNormalVkb(initialVkbChars);
                } else {
                    var def = StrokeTables[selectedTable._lowLimit(0) % StrokeTables.Count];
                    if (def.Faces == null) {
                        drawNormalVkb(def.CharOrKeys);
                    } else {
                        drawVkb5x10Table(def);
                    }
                }
                showNonActive();
            } else {
                this.Hide();
            }
        }

        private void drawNormalVkb(string[] strokeTable)
        {
            resetVkbControls("", VkbPictureBoxHeight_Normal, VkbCenterBoxHeight_Normal);
            using (PictureBoxDrawer drawer = new PictureBoxDrawer(pictureBox_Main)) {
                drawNormalVkbFrame(drawer.Gfx);
                drawCenterChars(drawer.Gfx);
                drawNormalVkbStrings(drawer.Gfx, i => strokeTable[i], true);
            }
            changeFormHeight(pictureBox_Main.Top + pictureBox_Main.Height + 1);
        }

        private void drawVkb5x10Table(StrokeTableDef def)
        {
            resetVkbControls("", VkbPictureBoxHeight_5x10Table, VkbCenterBoxHeight_5x10Table);
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
            topTextBox.Text = text;
            if (bRightAlign) {
                topTextBox.SelectionStart = text._safeLength();
                topTextBox.SelectionLength = 0;
            }
        }

        /// <summary> 第1打鍵待ち受け時に表示するストロークテーブルの切り替え </summary>
        /// <param name="decoderOutput"></param>
        public void RotateStrokeTable(DecoderOutParams decoderOutput, int delta = 1)
        {
            if (StrokeTables._notEmpty()) {
                if (delta < 0) delta = StrokeTables.Count - ((-delta) % StrokeTables.Count);
                selectedTable = (selectedTable + delta) % StrokeTables.Count;
                DrawVirtualKeyboardChars(decoderOutput);
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
        public void DrawVirtualKeyboardChars(DecoderOutParams decoderOutput)
        {
            logger.Info(() => $"CALLED: layout={decoderOutput.layout}, center={CommonState.CenterString}");

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
                resetVkbControls(topText, 0, 0);
                int nRow = 0;
                for (int i = 0; i < LongVkeyNum; ++i) {
                    //logger.InfoH(decoderOutput.faceStrings.Skip(i*20).Take(20).Select(c => c.ToString())._join(""));
                    if (drawHorizontalCandidateCharsWithColor(decoderOutput, i, decoderOutput.faceStrings)) ++nRow;
                }
                dgvHorizontal.CurrentCell = null;   // どのセルも選択されていない状態にする
                dgvHorizontal.Height = VkbCellHeight * nRow + 1;
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
                int height = (int)(candArray.Select(s => calcCharsAsFullwide(s)).Max()._lowLimit(MinVerticalChars) * verticalFontInfo.CharHeight) + 5;
                int centerHeight = height._max(CommonState.CenterString._safeLength()._lowLimit(MinCenterChars) * centerFontInfo.CharHeight + 5);
                resetVkbControls(topText, height, centerHeight);
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
                    resetVkbControls(topText, VkbPictureBoxHeight_Normal, VkbCenterBoxHeight_Normal);
                    using (PictureBoxDrawer drawer = new PictureBoxDrawer(pictureBox_Main)) {
                        topTextBox.Show();
                        SetTopText(topText, true);
                        drawNormalVkbFrame(drawer.Gfx);
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
            DrawInitailVkb();
            SetTopText(topText, true);
        }

        // 仮想鍵盤の高さを変更し、必要ならウィンドウを移動する
        private void changeFormHeight(int newHeight)
        {
            int oldHeight = this.Height;
            this.Height = newHeight;
            if (newHeight != oldHeight) {
                // ウィンドウ位置の再取得を行わずに移動するので正しくない場所に表示される可能性はあるが、たいていの場合は大丈夫だろう
                frmMain.MoveFormVirtualKeyboard();
            }
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
        private string minibufFontSpec = "";
        private Font minibufFont = null;

        // 通常仮想鍵盤用フォントの更新
        private void renewMinibufFont()
        {
            logger.InfoH(() => $"CALLED: new minibufFontSpec={Settings.MiniBufVkbFontSpec}, old={minibufFontSpec}");
            if (minibufFont == null || minibufFontSpec._ne(Settings.MiniBufVkbFontSpec)) {
                minibufFontSpec = Settings.MiniBufVkbFontSpec;
                var fontItems = minibufFontSpec._split('|').Select(x => x._strip()).ToArray();
                minibufFont?.Dispose();
                string fontName = fontItems._getNth(0)._orElse("MS UI Gothic");
                int fontSize = fontItems._getNth(1)._parseInt(9)._lowLimit(8);
                minibufFont = new Font(fontName, fontSize);
                topTextBox.Font = new Font(fontName, fontSize);
                logger.InfoH(() => $"new minibufFont={fontName}|{fontSize}");
            }
        }

        /// <summary>
        /// 仮想鍵盤を構成するコントロールの再配置
        /// </summary>
        private void resetVkbControls(string topText, int picBoxHeight, int centerHeight)
        {
            renewMinibufFont();
            topTextBox.Show();
            SetTopText(topText, true);
            pictureBox_Main.Top = topTextBox.Height;
            dgvHorizontal.Top = topTextBox.Height;

            if (picBoxHeight > 0) {
                int height = picBoxHeight._max(centerHeight);
                pictureBox_Main.Height = height;
                pictureBox_Main.Show();
                dgvHorizontal.Hide();
                setVerticalBoxHeight(height, centerHeight);
            } else {
                pictureBox_Main.Hide();
                dgvHorizontal.Show();
            }
        }

        //-------------------------------------------------------------------------------
        // 通常仮想鍵盤サポート

        /// <summary> 通常鍵盤横書きフォント </summary>
        private string normalFontSpec = "";
        private Font normalFont = null;

        int normalFontLeftPadding = 2;
        int normalFontTopPadding = 4;

        // フォントのピクセルサイズを計測する
        private (int, int) measureFontSize(Font font)
        {
            //表示する文字
            string s = "亜";

            pictureBox_measureFontSize.Width = 50;
            pictureBox_measureFontSize.Height = 50;
            using (Bitmap canvas = new Bitmap(pictureBox_measureFontSize.Width, pictureBox_measureFontSize.Height)) {
                using (Graphics g = Graphics.FromImage(canvas)) {
                    using (StringFormat sf = new StringFormat()) {
                        g.DrawString(s, font, Brushes.Black, 0, 0, sf);
                        //計測する文字の範囲を指定する
                        sf.SetMeasurableCharacterRanges(Helper.Array(new CharacterRange(0, 1)));
                        Region[] stringRegions = g.MeasureCharacterRanges(s, font, new RectangleF(1, 0, 50, 50), sf);
                        if (stringRegions.Length > 0) {
                            var rect = stringRegions[0].GetBounds(g);
                            return ((int)rect.Width, (int)rect.Height);
                        }
                        return (0, 0);
                    }
                }
            }
        }

        // 通常仮想鍵盤用フォントの更新
        private void renewNormalFont()
        {
            if (normalFont == null || normalFontSpec._ne(Settings.NormalVkbFontSpec)) {
                logger.InfoH(() => $"CALLED: new normalFontSpec={Settings.NormalVkbFontSpec}");
                normalFontSpec = Settings.NormalVkbFontSpec;
                var fontItems = normalFontSpec._split('|').Select(x => x._strip()).ToArray();
                normalFont?.Dispose();
                string fontName = fontItems._getNth(0)._orElse("MS Gothic");
                normalFont = new Font(fontName, fontItems._getNth(1)._parseInt(9)._lowLimit(8));
                // MS Gothic(9) -> (13, 12), Meiryo(9) -> (13, 18), Yu Gothic(9) -> (13, 16)
                (int fw, int fh) = measureFontSize(normalFont);
                normalFontLeftPadding = fontItems._getNth(2)._parseInt(-999);
                if (normalFontLeftPadding < -50) {
                    normalFontLeftPadding = (VkbCellWidth - fw - 1) / 2;            // 2 になるようにする
                }
                normalFontTopPadding = fontItems._getNth(3)._parseInt(-999);
                if (normalFontTopPadding < -50) {
                    int fh_;
                    if (fontName._startsWith("Yu ") || fontName._startsWith("游")) {
                        fh_ = fh <= 16 ? 14 : 15;
                    } else if (fontName._startsWith("Meiryo") || fontName._startsWith("メイリオ")) {
                        fh_ = fh <= 18 ? 15 : 16;
                    } else {
                        // MS Gothic と想定
                        fh_ = fh < 13 ? fh : (fw <= 13 ? 13 : 14);
                    }
                    normalFontTopPadding = VkbCellHeight - fh_  - 2;  // 2～4 になるようにする
                }
                logger.InfoH(() => $"new normalFont Width={fw}, Height={fh}, padLeft={normalFontLeftPadding}, padTop={normalFontTopPadding}");
            }
        }

        /// <summary>
        /// 通常仮想鍵盤文字列の表示<br/>
        /// nthString は全角1文字分だけを返すこと。
        /// </summary>
        private void drawNormalVkbStrings(Graphics g, Func<int, string> nthString, bool bFirst)
        {
            // フォントの更新
            renewNormalFont();

            // 通常ストローク
            for (int i = 0; i < 4; ++i) {
                for (int j = 0; j < 10; ++j) {
                    int x = VkbCellWidth * j + normalFontLeftPadding + (j >= 5 ? VkbCenterWidth : 0);
                    int y = VkbCellHeight * i + normalFontTopPadding;
                    g.DrawString(nthString(i * 10 + j), normalFont, Brushes.Black, x, y);
                }
            }
            // 下端機能キー
            for (int j = 0; j < 10; ++j) {
                int x = VkbBottomOffset + VkbCellWidth * j + normalFontLeftPadding;
                int y = VkbCellHeight * 4 + normalFontTopPadding;
                var face = bFirst ? initialVkbChars[40 + j] : nthString(40 + j);
                g.DrawString(face, normalFont, Brushes.Black, x, y);
            }
        }

        /// <summary>
        /// 通常の仮想鍵盤の枠線と背景色の描画
        /// </summary>
        /// <param name="g"></param>
        private void drawNormalVkbFrame(Graphics g)
        {
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
            g.FillRectangle(b1, 1, 1, VkbCellWidth * 10 + VkbCenterWidth - 1, VkbCellHeight - 1);
            b1.Dispose();
            b1 = new SolidBrush(bgColorHighLow);
            g.FillRectangle(b1, 1, VkbCellHeight + 1, VkbCellWidth * 10 + VkbCenterWidth - 1, VkbCellHeight * 3 - 1);
            b1.Dispose();
            b1 = new SolidBrush(bgColorMiddle);
            g.FillRectangle(b1, 1, VkbCellHeight * 2 + 1, VkbCellWidth * 10 + VkbCenterWidth - 1, VkbCellHeight - 1);
            b1.Dispose();
            b1 = new SolidBrush(bgColorCenter);
            g.FillRectangle(b1, VkbCellWidth * 4 + 1, VkbCellHeight + 1, VkbCellWidth * 2 + VkbCenterWidth - 1, VkbCellHeight * 3 - 1);
            b1.Dispose();

            // 中央鍵盤
            g.FillRectangle(Brushes.White, VkbCellWidth * 5 + 1, 1, VkbCenterWidth - 1, VkbCellHeight * 4 - 1);

            // 下部拡張部
            g.FillRectangle(Brushes.WhiteSmoke, VkbBottomOffset + 1, VkbCellHeight * 4 + 1, VkbCellWidth * 10 - 1, VkbCellHeight - 1);

            // 枠線
            Pen pen = Pens.DarkGray;

            // 上端横線
            int x1 = 0, y1 = 0, y2 = 0;
            int x2 = pictureBox_Main.Width;
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
            int width = pictureBox_Main.Width;
            int height = pictureBox_Main.Height;

            // 1段おきの背景色
            g.FillRectangle(Brushes.AliceBlue, 1, Vkb5x10CellHeight, width - 1, Vkb5x10CellHeight);
            g.FillRectangle(Brushes.AliceBlue, 1, Vkb5x10CellHeight * 3, width - 1, Vkb5x10CellHeight);

            // 枠線と縦横線の描画
            Pen pen = Pens.DarkGray;
            // 横線 
            int x1 = 0;
            int x2 = width - 1;
            for (int y = 0; y < height; y += Vkb5x10CellHeight) {
                g.DrawLine(pen, x1, y, x2, y);
            }
            // 縦線 
            int y1 = 0;
            int y2 = height - 1;
            for (int x = 0; x < width; x += Vkb5x10CellWidth) {
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

            //logger.InfoH($"chars.Length={chars.Length}, rows={dgvHorizontal._rowsCount()}");
            if (nth >= 0 && nth < dgvHorizontal._rowsCount()) {
                int pos = nth * LongVkeyCharSize;
                int len = chars._findIndex(pos, pos + LongVkeyCharSize, x => x == 0) - pos;
                if (len < 0) len = LongVkeyCharSize;
                StringBuilder sb = new StringBuilder();
                sb.Append((nth + 1) % 10).Append(' ').Append(chars, pos, len);
                //logger.InfoH($"drawString={drawString}, nth={nth}, pos={pos}, len={len}");
                if (sb.Length > 2) {
                    dgvHorizontal.Rows[nth].Cells[0].Value = sb.ToString();
                    dgvHorizontal.Rows[nth].Cells[0].Style.BackColor = makeSpecifiedColor();
                    return true;
                }
            }
            return false;
        }


        //--------------------------------------------------------------------------------------
        // 縦列鍵盤サポート
        /// <summary> 縦列鍵盤で用いるフォント情報 </summary>
        public class VerticalFontInfo : IDisposable
        {
            /// <summary> 横書き用フォント </summary>
            public Font HorizontalFont = null;
            /// <summary> 縦書き用フォント </summary>
            public Font VerticalFont = null;
            /// <summary> フォント指定 </summary>
            public string FontSpec = "";
            /// <summary> 縦書き時の左余白 </summary>
            public int LeftPadding = 2;
            /// <summary> 縦書き時の上部余白 </summary>
            public int TopPadding = 3;
            /// <summary> 縦書きフォントの高さ </summary>
            public int CharHeight = 13;

            public float FontSizeThreshold1 = 9.5f;
            public float FontSizeThreshold2 = 11.0f;

            public int AdjustedLeftPadding(string str)
            {
                int leftPadding = LeftPadding;
                if (str._safeLength() == 1) {
                    leftPadding = VerticalFont.Size >= FontSizeThreshold2 ? 0 : VerticalFont.Size >= FontSizeThreshold1 ? 1 : 2;
                }
                return leftPadding;
            }

            public void Dispose()
            {
                HorizontalFont?.Dispose(); HorizontalFont = null;
                VerticalFont?.Dispose(); VerticalFont = null;
            }

        }

        /// <summary> ストローク文字横書きフォント </summary>
        private Font strokeCharFont;
        /// <summary> ストロークキー横書きフォント </summary>
        private Font strokeKeyFont;

        /// <summary> 中央鍵盤フォント情報 </summary>
        private VerticalFontInfo centerFontInfo = new VerticalFontInfo() {
            CharHeight = 15,
            FontSizeThreshold1 = 10.5f,
            FontSizeThreshold2 = 11.5f,
        };

        /// <summary> 縦列鍵盤フォント情報 </summary>
        private VerticalFontInfo verticalFontInfo = new VerticalFontInfo() {
            CharHeight = 13,
            FontSizeThreshold1 = 9.5f,
            FontSizeThreshold2 = 11.0f,
        };

        /// <summary> 縦列鍵盤ボックス </summary>
        public struct VerticalBox
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public Color BackColor;
        }
        VerticalBox[] verticalBoxes = new VerticalBox[LongVkeyNum];
        VerticalBox centerBox = new VerticalBox { X = VkbCellWidth * 5, Y = 0, Width = VkbCenterWidth, Height = VkbCellHeight * 4, BackColor = Color.White };

        // 縦列中央鍵盤用フォントの更新
        private void renewCenterVerticalFont()
        {
            renewVerticalFont(Settings.CenterVkbFontSpec, VkbCenterWidth, centerFontInfo);
        }

        // 縦列候補鍵盤用フォントの更新
        private void renewCandidateVerticalFont()
        {
            renewVerticalFont(Settings.VerticalVkbFontSpec, VkbCellWidth, verticalFontInfo);
        }

        // 縦列鍵盤用フォントの更新
        private void renewVerticalFont(string newSpec, int boxWidth, VerticalFontInfo info)
        {
            if (info.VerticalFont == null || info.FontSpec._ne(newSpec)) {
                logger.InfoH(() => $"CALLED: new fontSpec={newSpec}");
                info.FontSpec = newSpec;
                var fontItems = info.FontSpec._split('|').Select(x => x._strip()).ToArray();
                info.VerticalFont?.Dispose();
                info.HorizontalFont?.Dispose();
                string fontName = fontItems._getNth(0)._orElse("@MS Gothic");
                info.VerticalFont = new Font(fontName, fontItems._getNth(1)._parseInt(9)._lowLimit(8));
                fontName = fontName._safeReplace("@", "");          // 先頭の @ を削除しておく
                info.HorizontalFont = new Font(fontName, fontItems._getNth(1)._parseInt(9)._lowLimit(8));
                // MS Gothic(9) -> (13, 12), Meiryo(9) -> (13, 18), Yu Gothic(9) -> (13, 16)
                (int fh, int fw) = measureFontSize(info.VerticalFont);
                info.CharHeight = fh + 1;
                info.LeftPadding = fontItems._getNth(2)._parseInt(-999);
                if (info.LeftPadding < -50) {
                    info.LeftPadding = (boxWidth - fw - (fw >= 15 ? 3 : fw == 14 ? 2 : 0)) / 2;
                }
                info.TopPadding = fontItems._getNth(3)._parseInt(-999);
                if (info.TopPadding < -50) {
                    if (fontName._startsWith("Yu ") || fontName._startsWith("游")) {
                        info.TopPadding = fh <= 16 ? 2 : 1;
                    } else if (fontName._startsWith("Meiryo") || fontName._startsWith("メイリオ")) {
                        info.TopPadding = fh <= 18 ? 1 : 0;
                    } else {
                        // MS Gothic と想定
                        info.TopPadding = fh < 13 ? 3 : (fw <= 13 ? 3 : 2);
                    }
                }
                if (Logger.IsInfoHEnabled) logger.InfoH($"new verticalFont Width={fw}, Height={fh}, charHeight={info.CharHeight}, padLeft={info.LeftPadding}, padTop={info.TopPadding}");
            }
        }

        /// <summary> 縦書き用オブジェクトの生成 </summary>
        private void createObjectsForDrawingVerticalChars()
        {
            strokeCharFont = new Font("MS UI Gothic", 12);
            strokeKeyFont = new Font("MS Gothic", 12);

            int verticalBoxHeight = verticalFontInfo.CharHeight * 7 + 3;
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
        }

        /// <summary> 出力文字数に応じて縦列鍵盤の高さを設定</summary>
        private void setVerticalBoxHeight(int height, int centerHeight)
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
            int x1 = 0, y1 = 0, y2 = 0;
            int x2 = pictureBox_Main.Width - 1;
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
                } else if (decoderOutput.IsOtherStatus()) {
                    name = "Yellow";    // とりあえず Yellow 固定
                }
                if (name._notEmpty()) {
                    var color = Color.FromName(name);
                    if (!color.IsEmpty) return color;
                }
                return SystemColors.Window;
            }

            drawCenterChars(g, makeSpecifiedColor());
        }

        /// <summary> デフォルト背景色で中央鍵盤に文字列を出力する</summary>
        private void drawCenterChars(Graphics g)
        {
            drawCenterChars(g, SystemColors.Window);
        }

        /// <summary> 指定の背景色で中央鍵盤に文字列を出力する</summary>
        private void drawCenterChars(Graphics g, Color bgColor)
        {
            logger.Debug(() => $"center={CommonState.CenterString}");
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
            int topPadding = info.TopPadding;
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
        private void pictureBox_Main_Click(object sender, EventArgs e)
        {
            logger.Debug("CALLED");
            if (((MouseEventArgs)e).Button == MouseButtons.Right) {
                contextMenuStrip1.Show(Cursor.Position);
                logger.Debug("ContextMenu Shown");
            } else {
                frmMain.ToggleActiveState();
                logger.Debug("ToggleActiveState");
            }
        }

        private void pictureBox_Main_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //logger.InfoH($"CALLED: e.Delta={e.Delta}, scrollLines={SystemInformation.MouseWheelScrollLines}");
            //frmMain.RotateStrokeTable(e.Delta * SystemInformation.MouseWheelScrollLines / 120);
            frmMain.RotateStrokeTable(-e.Delta / 120);
        }

        // 終了
        private void Exit_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logger.Debug("ENTER");
            //frmMain.DeactivateDecoder();
            //logger.Debug("Decoder OFF");
            //if (!Settings.ConfirmOnClose || SystemHelper.OKCancelDialog("漢直窓を終了します。\r\nよろしいですか。")) {
            //    this.Close();
            //    logger.Debug("this.Closed");
            //    frmMain.Close();
            //    logger.Debug("Main.Closed");
            //}
            frmMain.Terminate();
            logger.Debug("LEAVE");
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

        private void ReadBushuDic_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmMain.ExecCmdDecoder("readBushuDic", null);
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
            logger.Debug("ENTER");
            frmMain.Restart(false);
            logger.Debug("LEAVE");
        }

        // 辞書内容を破棄して再起動
        private void RestartWithDiscard_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logger.Debug("ENTER");
            frmMain.Restart(true);
            logger.Debug("LEAVE");
        }
    }
}
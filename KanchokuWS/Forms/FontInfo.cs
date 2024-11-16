using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Utils;

namespace KanchokuWS.Forms
{
    //------------------------------------------------------------------------------------
    /// <summary>
    /// フォント情報
    /// </summary>
    public class FontInfo : IDisposable
    {
        private static Logger logger = Logger.GetLogger();

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
        public FontInfo(string name, bool vertical, bool padding)
        {
            myName = name;
            this.useVertical = vertical;
            this.usePadding = padding;
        }

        public bool RenewFontSpec(string fontSpec, float cellWidth = 0f, float cellHeight = 0f, PictureBox picBox = null)
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

            if (usePadding && picBox != null) {
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
}

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

using KanchokuWS.Domain;
using KanchokuWS.Gui;
using KanchokuWS.Handler;
using Utils;

namespace KanchokuWS.Forms
{
    public partial class FrmCandidateSelector : Form
    {
        private static Logger logger = Logger.GetLogger();

        private FrmKanchoku frmMain;

        private FrmEditBuffer frmEditBuf;

        public static int CurrentScreen = 0;

        private static float DgvNormalWidth = 201;

        private static float DgvCellHeight = 18;
        private static float DgvCellWidth = 18;

        private const int LongVkeyNum = 10;
        private const int LongVkeyCharSize = 20;


        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        //------------------------------------------------------------------------------------

        private bool renewFontInfo(FontInfo fontInfo, string fontSpec)
        {
            return fontInfo.RenewFontSpec(fontSpec, DgvCellWidth, DgvCellHeight);
        }

        [DllImport("user32.dll")]
        private static extern void ShowWindow(IntPtr hWnd, int nCmdShow);

        // ウィンドウをアクティブにせずに表示する
        private const int SW_SHOWNA = 8;

        public void ShowNonActive()
        {
            ShowWindow(this.Handle, SW_SHOWNA);   // NonActive
        }

        //------------------------------------------------------------------------------------
        /// <summary> コンストラクタ </summary>
        /// <param name="frmMain"></param>
        public FrmCandidateSelector(FrmKanchoku frmMain, FrmEditBuffer frmEditBuf)
        {
            this.frmMain = frmMain;
            this.frmEditBuf = frmEditBuf;
            frmEditBuf.SetFrmCands(this);

            InitializeComponent();

            // タイトルバーや境界線を消す
            FormBorderStyle = FormBorderStyle.None;

            // 各種パラメータの初期化
            resetDrawParameters(ScreenInfo.Singleton.PrimaryScreenDpi);

            // 横列鍵盤用グリッドの初期化
            initializeHorizontalDgv((int)DgvNormalWidth);

            // モニタのDPIが変化したたときに呼ばれるハンドラを登録
            DpiChanged += dpiChangedHandler;
        }

        /// <summary> フォームのロード </summary>
        private void FrmCandidateSelector_Load(object sender, EventArgs e)
        {
            //logger.WarnH($"ENTER");

            this.Width = (int)(DgvNormalWidth + 2);
            this.Height = 0;

            //logger.WarnH($"LEAVE");
        }

        /// <summary>フォームのクローズ</summary>
        private void FrmDisplayBuffer_FormClosing(object sender, FormClosingEventArgs e)
        {
            horizontalFontInfo?.Dispose();
        }

        /// <summary>
        /// モニタが切り替わってDPIが変化したときに呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dpiChangedHandler(object sender, DpiChangedEventArgs e)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info(() => $"\nCALLED: new dpi={e.DeviceDpiNew}");

            CurrentScreen = ScreenInfo.Singleton.GetScreenIndexByDpi(e.DeviceDpiNew);

            dgvHorizontal.Rows.Clear();

            resetDrawParameters(e.DeviceDpiNew);

            redrawDgv();
        }

        //-------------------------------------------------------------------------------
        /// <summary> 横列鍵盤用フォント情報 </summary>
        private FontInfo horizontalFontInfo = new FontInfo("Horizontal", false, false);

        private bool renewHorizontalFontInfo()
        {
            return renewFontInfo(horizontalFontInfo, Settings.HorizontalVkbFontSpec);
        }

        // 横列鍵盤用グリッドの初期化
        private void initializeHorizontalDgv(int cellWidth)
        {
            //logger.WarnH($"ENTER");
            var dgv = dgvHorizontal;
            dgv._defaultSetup(0, (int)DgvCellHeight);       // headerHeight=0 -> ヘッダーを表示しない
            dgv._setSelectionColorReadOnly();
            renewHorizontalFontInfo();
            dgv._setDefaultFont(horizontalFontInfo.MyFont);
            //dgv._setDefaultFont(DgvHelpers.FontMSG8);
            dgv._disableToolTips();
            dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly("horizontal", "", cellWidth)._setUnresizable());

            //dgv.Rows.Add(nRow);

            renewHorizontalDgv();
            //logger.WarnH($"LEAVE");
        }

        private void renewHorizontalDgv()
        {
            var dgv = dgvHorizontal;
            //logger.WarnH($"ENTER: dgv.Rows.Count={dgv.Rows.Count}, dgv.Columns.Count={dgv.Columns.Count}");

            int cellWidth = (int)DgvNormalWidth - 1;
            int cellHeight = (int)DgvCellHeight;
            dgv.RowTemplate.Height = cellHeight;
            if (dgv.Columns.Count <= 0) {
                //logger.WarnH($"LEAVE");
                return;
            }

            if (dgv.Columns.Count > 0) { dgv.Columns[0].Width = cellWidth; }
            if (renewHorizontalFontInfo()) { dgv._setDefaultFont(horizontalFontInfo.MyFont); }
            //dgv.Top = topTextBox.Top + topTextBox.Height - 1;
            dgv.Width = cellWidth + 1;
            //dgv.Height = dgv.Rows.Count * cellHeight + 1;

            if (dgv.Rows.Count == 0) dgv.Rows.Add(LongVkeyNum);

            //logger.WarnH($"LEAVE: dgv.Rows.Count={dgv.Rows.Count}, dgv.Top={dgv.Top}, dgv.Width={dgv.Width}, cellHeight={cellHeight}, cellWidth={cellWidth}");
                
        }

        private void resetDrawParameters(int dpi)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"CALLED: dpi={dpi}");
            //float rate = (float)ScreenInfo.Singleton.PrimaryScreenDpiRate._lowLimit(1.0);
            float rate = dpi / 96.0f;

            Func<float, float> mulRate = (float x) => (int)(x * rate);

            DgvCellHeight = mulRate(18);
            DgvCellWidth = mulRate(18);


            DgvNormalWidth = DgvCellWidth * 10 + 1;



            this.Width = (int)(DgvNormalWidth + 2);
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"LEAVE: this.Width={this.Width}");
        }

        /// <summary>
        /// 選択候補の再描画
        /// </summary>
        private void redrawDgv()
        {
            //横書き鍵盤の再初期化
            renewHorizontalDgv();

            // 仮想鍵盤の再描画
            if (frmMain.IsDecoderActive) DrawCandidates();
        }

        //-------------------------------------------------------------------------------
        /// <summary> 融合時の選択候補を表示 </summary>
        public void DrawCandidates(int lastDeckey = -1)
        {
            var decoderOutput = frmMain.DecoderOutput;

            //logger.WarnH(() => $"CALLED: layout={decoderOutput.layout}, faceString={decoderOutput.faceStrings._toString()}");

            if (frmEditBuf.IsEmpty || decoderOutput.layout != (int)VkbLayout.MultiStreamCandidates || decoderOutput.faceStrings._isEmpty() || decoderOutput.faceStrings[0] == 0) {
                this.Hide();
                //logger.WarnH("Hide");
                return;
            }

            // 選択候補を表示
            resetControls(0, 0, 0);
            int nRow = 0;
            for (int i = 0; i < LongVkeyNum; ++i) {
                //logger.Info(decoderOutput.faceStrings.Skip(i*20).Take(20).Select(c => c.ToString())._join(""));
                if (drawHorizontalCandidateCharsWithColor(decoderOutput, i, decoderOutput.faceStrings)) ++nRow;
            }
            //logger.WarnH($"nRow={nRow}");
            dgvHorizontal.CurrentCell = null;   // どのセルも選択されていない状態にする
            dgvHorizontal.Height = (int)(DgvCellHeight * nRow + 1);
            //changeFormHeight(dgvHorizontal.Top + dgvHorizontal.Height + 1);
            MoveWindow();
            ShowNonActive();
        }

        // 仮想鍵盤の高さを変更し、必要ならウィンドウを移動する
        public void MoveWindow()
        {
            int oldHeight = this.Height;
            //this.Invalidate();
            int fX = frmEditBuf.Location.X;
            int fY = frmEditBuf.Location.Y + frmEditBuf.Height + 4;
            int fW = dgvHorizontal.Width + 2;
            int fH = dgvHorizontal.Height + 2;
            //logger.WarnH(() => $"MoveWindow: fX={fX}, fY={fY}, fW={fW}, fH={fH}");
            MoveWindow(this.Handle, fX, fY, fW, fH, true);
        }

        /// <summary>
        /// 仮想鍵盤を構成するコントロールの再配置
        /// </summary>
        private void resetControls(float picBoxWidth, float picBoxHeight, float centerHeight)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"picBoxWidth={picBoxWidth:f3}, picBoxHeight={picBoxHeight:f3}, centerHeight={centerHeight:f3}");
            //renewMinibufFont();
            renewHorizontalDgv();
            //dgvHorizontal.Top = topTextBox.Height;

            if (picBoxWidth > 0 && picBoxHeight > 0) {
                var height = picBoxHeight._max(centerHeight);
                dgvHorizontal.Hide();
            } else {
                //dgvHorizontal.Width = topTextBox.Width;
                dgvHorizontal.Width = (int)DgvNormalWidth;
                dgvHorizontal.Show();
                //dgvHorizontal.Top = topTextBox.Height;
                if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"dgv.Top={dgvHorizontal.Top}, dgv.Width={dgvHorizontal.Width}");
            }
            this.Width = (int)(DgvNormalWidth + 2);
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"LEAVE: this.Width={this.Width}");
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
                int len = chars._findIndex(pos, pos + LongVkeyCharSize, '\0') - pos;
                if (len < 0) len = LongVkeyCharSize;
                StringBuilder sb = new StringBuilder();
                sb.Append((nth + 1) % 10).Append(' ').Append(chars, pos, len);
                if (pos + len < chars.Length && chars[pos + len] != '\0') sb.Append('…');
                //logger.Info($"drawString={drawString}, nth={nth}, pos={pos}, len={len}");
                if (sb.Length > 2) {
                    dgvHorizontal.Rows[nth].Cells[0].Value = sb.ToString();
                    dgvHorizontal.Rows[nth].Cells[0].Style.BackColor = makeSpecifiedColor();
                    return true;
                }
            }
            return false;
        }


        //------------------------------------------------------------------
        // イベントハンドラ
        //------------------------------------------------------------------
        private void dgvHorizontal_SelectionChanged(object sender, EventArgs e)
        {
            dgvHorizontal.CurrentCell = null;   // どのセルも選択されていない状態に戻す
        }

        private void FrmDisplayBuffer_VisibleChanged(object sender, EventArgs e)
        {
            //CommonState.VkbVisible = this.Visible;
            //CommonState.VkbVisibiltyChangedDt = HRDateTime.Now;
        }

    }
}

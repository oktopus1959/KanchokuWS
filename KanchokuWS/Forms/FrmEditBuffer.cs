using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using KanchokuWS.Gui;
using KanchokuWS.Handler;
using Utils;
using System.Text;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Linq;

namespace KanchokuWS.Forms
{
    public partial class FrmEditBuffer : Form
    {
        private static Logger logger = Logger.GetLogger();

        private FrmKanchoku frmMain;

        public static int CurrentScreen = 0;

        //private static float VkbNormalWidth = 201;

        //private static float VkbCellHeight = 18;
        //private static float VkbCellWidth = 18;
        //private static float VkbCenterWidth = 20;

        private const int LongVkeyCharSize = 20;

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        //------------------------------------------------------------------------------------
        /// <summary> コンストラクタ </summary>
        /// <param name="form"></param>
        public FrmEditBuffer(FrmKanchoku form)
        {
            frmMain = form;

            InitializeComponent();

            Width = 10;

            // タイトルバーを消す
            FormBorderStyle = FormBorderStyle.None;

            // 各種パラメータの初期化
            resetDrawParameters(ScreenInfo.Singleton.PrimaryScreenDpi);

            // モニタのDPIが変化したたときに呼ばれるハンドラを登録
            DpiChanged += dpiChangedHandler;
        }

        /// <summary> フォームのロード </summary>
        private void FrmEditBuffer_Load(object sender, EventArgs e)
        {
            this.Width = 8;
            this.Height = 20;
            //frmMain.MoveFormEditBuffer();
            //MoveWindow(this.Handle, this.Location.X, this.Location.Y, this.Width, this.Height, true);
            //ShowNonActive();
            logger.WarnH($"MOVE: X={Location.X}, Y={Location.Y}, W={Size.Width}, H={Size.Height}");
        }

        /// <summary>フォームのクローズ</summary>
        private void FrmEditBuffer_FormClosing(object sender, FormClosingEventArgs e)
        {
            //editBufFontInfo?.Dispose();
        }

        //------------------------------------------------------------------------------------
        private static string CARET = "‸";

        /// <summary>文字列を編集バッファのカーソル位置に挿入する</summary>
        /// <param name="chars"></param>
        public void PutString(char[] chars, int numBS)
        {
            if ((chars._isEmpty() || chars[0] == 0) && numBS <= 0) return;

            if (editTextBox.Text._isEmpty() && (chars._isEmpty() || chars[0] == 0 || chars._safeLength() >= 2 && chars[0] == '!' && chars[1] == '{')) {
                // 何もせずに、呼び出し元に任せる
                SendInputHandler.Singleton.SendStringViaClipboardIfNeeded(chars, numBS, true);
                return;
            }

            var str = chars._toString();

            int prePos = editTextBox.Text._safeIndexOf(CARET[0]);
            if (prePos < 0) prePos = editTextBox.Text.Length;
            int postPos = prePos + 1;
            logger.WarnH(() => $"ENTER: EditText={EditText}, pos={prePos}, str={str}, numBS={numBS}");
            if (numBS > 0) {
                prePos -= numBS;
                if (prePos < 0) prePos = 0;
            }
            //logger.WarnH(() => $"prePos={prePos}, postPos={postPos}");
            string preText = editTextBox.Text._safeSubstring(0, prePos);
            string postText = editTextBox.Text._safeSubstring(postPos);
            //logger.WarnH(() => $"preText={preText}, postTest={postText}");

            bool toFlush = false;

            void handleFunctionalKey(string fkey)
            {
                logger.WarnH($"fkey={fkey}");
                switch (fkey) {
                    case "Left":
                        logger.WarnH($"Left");
                        if (preText._notEmpty()) {
                            postText = preText._safeSubstring(-1) + postText;
                            preText = preText._safeSubstring(0, -1);
                        }
                        break;
                    case "Right":
                        logger.WarnH($"Right");
                        if (postText._notEmpty()) {
                            preText = preText + postText._safeSubstring(0, 1);
                            postText = postText._safeSubstring(1);
                        }
                        break;
                    case "Home":
                        logger.WarnH($"Home");
                        postText = preText + postText;
                        preText = "";
                        break;
                    case "End":
                        logger.WarnH($"End");
                        preText = preText + postText;
                        postText = "";
                        break;
                    case "BS":
                    case "BackSpace":
                        logger.WarnH($"BS");
                        if (preText._notEmpty()) {
                            preText = preText._safeSubstring(0, -1);
                        }
                        break;
                    case "DEL":
                    case "Delete":
                        logger.WarnH($"Delete");
                        if (postText._notEmpty()) {
                            postText = postText._safeSubstring(1);
                        }
                        break;
                    case "Enter":
                        logger.WarnH($"Enter");
                        toFlush = true;
                        break;
                    case "^U":
                        logger.WarnH($"^U");
                        preText = "";
                        postText = "";
                        break;
                }
            }

            if (str._notEmpty()) {
                int i = 0;
                while (i < str.Length) {
                    if (str[i] == '!' && i + 1 < str.Length && str[i + 1] == '{') {
                        // "!{...}"
                        i += 2;
                        var sb = new StringBuilder();
                        while (i < str.Length && str[i] != '}') {
                            sb.Append(str[i++]);
                        }
                        handleFunctionalKey(sb.ToString());
                    } else {
                        if (str[i] == '(' && str[str.Length - 1] == ')') {
                            var value = Handler.HandlerUtils.ParseTernaryOperator(str._safeSubstring(i), "@");
                            logger.WarnH($"value={value}");
                            if (value._notEmpty()) {
                                str = value;
                                i = 0;
                                continue;
                            }
                        }
                        preText += str[i];
                    }
                    ++i;
                }
            }

            editTextBox.Text = makeEditText(preText, postText);
            if (toFlush) FlushBuffer();
            logger.WarnH(() => $"LEAVE: EditText={EditText}, pos={editTextBox.Text._safeIndexOf(CARET[0])}");
        }

        public void PutVkeyCombo(uint modifier, uint vkey)
        {
            if (modifier != 0 || editTextBox.Text._isEmpty()) {
                SendInputHandler.Singleton.SendVKeyCombo(modifier, vkey, 1);
                return;
            }
            switch (vkey) {
                case (uint)Keys.Left:
                    logger.WarnH($"Left");
                    moveCaretLeft();
                    break;
                case (uint)Keys.Right:
                    logger.WarnH($"Right");
                    moveCaretRight();
                    break;
                case (uint)Keys.Home:
                    logger.WarnH($"Home");
                    moveCaretHome();
                    break;
                case (uint)Keys.End:
                    logger.WarnH($"End");
                    moveCaretEnd();
                    break;
                case (uint)Keys.Back:
                    logger.WarnH($"Back");
                    backspace();
                    break;
                case (uint)Keys.Delete:
                    logger.WarnH($"Delete");
                    delete();
                    break;
                case (uint)Keys.Enter:
                    logger.WarnH($"Enter");
                    FlushBuffer();
                    break;
            }
        }

        private string makeEditText(string preText, string postText)
        {
            logger.WarnH(() => $"preText={preText}, preLen={preText.Length}, postText={postText}, postLen={postText}");
            var text = preText + ((preText._notEmpty() || postText._notEmpty()) ? CARET : "") + postText;
            logger.WarnH(() => $"text={text}, len={text.Length}");
            return text;
        }

        private int findCaret()
        {
            return editTextBox.Text._safeIndexOf(CARET[0]);
        }

        private void moveCaretLeft()
        {
            int pos = findCaret();
            if (pos > 0) {
                string preText = editTextBox.Text._safeSubstring(0, pos - 1);
                string postText = editTextBox.Text._safeSubstring(pos - 1).Remove(1, 1);
                editTextBox.Text = makeEditText(preText, postText);
            }
        }

        private void moveCaretRight()
        {
            int pos = findCaret();
            if (pos >= 0 && pos + 1 < editTextBox.Text.Length) {
                string preText = editTextBox.Text._safeSubstring(0, pos + 2).Remove(pos, 1);
                string postText = editTextBox.Text._safeSubstring(pos + 2);
            editTextBox.Text = makeEditText(preText, postText);
            }
        }

        private void moveCaretHome()
        {
            int pos = findCaret();
            if (pos > 0) {
            editTextBox.Text = makeEditText("", editTextBox.Text.Remove(pos, 1));
            }
        }

        private void moveCaretEnd()
        {
            int pos = findCaret();
            if (pos >= 0 && pos + 1 < editTextBox.Text.Length) {
                editTextBox.Text = makeEditText(editTextBox.Text.Remove(pos, 1), "");
            }
        }

        private void backspace()
        {
            int pos = findCaret();
            if (pos > 0) {
                string preText = editTextBox.Text._safeSubstring(0, pos - 1);
                string postText = editTextBox.Text._safeSubstring(pos + 1);
                editTextBox.Text = makeEditText(preText, postText);
            }
        }

        private void delete()
        {
            int pos = findCaret();
            if (pos >= 0 && pos + 1 < editTextBox.Text.Length) {
                string preText = editTextBox.Text._safeSubstring(0, pos);
                string postText = editTextBox.Text._safeSubstring(pos + 2);
                editTextBox.Text = makeEditText(preText, postText);
            }
        }

        /// <summary>編集バッファをフラッシュして、アプリケーションに文字列を送出する</summary>
        public void FlushBuffer()
        {
            string result = editTextBox.Text._safeReplace(CARET, "");
            editTextBox.Text = "";
            editTextBox.SelectionStart = 0;
            this.Hide();
            //Helper.WaitMilliSeconds(10);
            //System.Windows.Forms.Application.DoEvents();
            SendInputHandler.Singleton.SendStringViaClipboardIfNeeded(result._toCharArray(), 0, true);
            //this.ShowNonActive();
            logger.WarnH($"CALLED");
        }

        private void resetFormSize()
        {
            // テキストの幅と高さを取得
            int textWidth = TextRenderer.MeasureText(editTextBox.Text, editTextBox.Font).Width;
            int textHeight = TextRenderer.MeasureText("亜", editTextBox.Font).Height;

            // 余裕を持たせて TextBox の幅を設定(上下左右にアンカーしているので、外側のフォームのサイズを変えればよい)
            this.Width = textWidth + 8;
            this.Height = textHeight + 7;
            logger.WarnH($"Width={Size.Width}, Height={this.Size.Height}");
        }

        //------------------------------------------------------------------------------------
        private bool renewFontInfo(FontInfo fontInfo, string fontSpec)
        {
            return fontInfo.RenewFontSpec(fontSpec);
        }

        [DllImport("user32.dll")]
        private static extern void ShowWindow(IntPtr hWnd, int nCmdShow);

        // ウィンドウをアクティブにせずに表示する
        private const int SW_SHOWNA = 8;

        public void ShowNonActive()
        {
            //editTextBox.Width = (int)(VkbNormalWidth);
            ShowWindow(this.Handle, SW_SHOWNA);   // NonActive
        }

        /// <summary>編集バッファの文字列を返す</summary>
        public string EditText => editTextBox.Text;

        //const int CS_DROPSHADOW = 0x00020000;

        ///// <summary> フォームに影をつける </summary>
        //protected override CreateParams CreateParams {
        //    get {
        //        CreateParams cp = base.CreateParams;
        //        cp.ClassStyle |= CS_DROPSHADOW;
        //        return cp;
        //    }
        //}

        /// <summary>
        /// モニタが切り替わってDPIが変化したときに呼ばれる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dpiChangedHandler(object sender, DpiChangedEventArgs e)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info(() => $"\nCALLED: new dpi={e.DeviceDpiNew}");

            CurrentScreen = ScreenInfo.Singleton.GetScreenIndexByDpi(e.DeviceDpiNew);

            if (frmMain.IsDecoderActive) {
                this.Hide();
                frmMain.DeactivateDecoderWithModifiersOff();
            }

            resetDrawParameters(e.DeviceDpiNew);

            // 編集バッファの再描画
            MoveWindow();
        }

        private void resetDrawParameters(int dpi)
        {
            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"CALLED: dpi={dpi}");
            //float rate = (float)ScreenInfo.Singleton.PrimaryScreenDpiRate._lowLimit(1.0);
            float rate = dpi / 96.0f;

            Func<float, float> mulRate = (float x) => (int)(x * rate);

            //VkbCellHeight = mulRate(18);
            //VkbCellWidth = mulRate(18);
            //VkbCenterWidth = mulRate(20);


            //VkbNormalWidth = VkbCellWidth * 10 + VkbCenterWidth + 1;

            resetFormSize();

            if (Settings.LoggingVirtualKeyboardInfo) logger.Info($"LEAVE: this.Width={this.Width}");
        }

        /// <summary> 編集バッファフォント </summary>
        private FontInfo editBufFontInfo = new FontInfo("EditBuf", false, false);

        // 編集バッフ用フォントの更新
        public void RenewEditBufFont()
        {
            if (renewFontInfo(editBufFontInfo, Settings.MiniBufVkbFontSpec)) {
                editTextBox.Font = editBufFontInfo.MyFont;
            }
        }

        //------------------------------------------------------------------
        // 移動
        //------------------------------------------------------------------
        /// <summary>
        /// 表示・編集バッファをカレットの近くに移動する<br/>
        /// これが呼ばれるのはデコーダがONのときだけ
        /// </summary>
        public void MoveWindow(Settings.WindowsClassSettings activeWinSettings, Rectangle activeWinCaretPos, bool bFixedPosWinClass, bool bLog)
        {
            int xOffset = (activeWinSettings?.CaretOffset)._getNth(0, 2);
            int yOffset = (activeWinSettings?.CaretOffset)._getNth(1, 2);
            //double dpiRatio = 1.0; //FrmVkb.GetDeviceDpiRatio();

            int cX = activeWinCaretPos.X;
            int cY = activeWinCaretPos.Y;
            int cW = activeWinCaretPos.Width;
            int cH = activeWinCaretPos.Height;

            // 表示・編集バッファの移動
            int fW = this.Size.Width;
            int fH = this.Size.Height;

            if (bLog) logger.WarnH($"fW={fW}, fH={fH}, cX={cX}, cY={cY}, cW={cW}, cH={cH}");

            int fX = cX + (xOffset >= 0 ? cW : -fW) + xOffset;
            if (fX < 0) fX = cX + cW + Math.Abs(xOffset);

            int fY = cY + (cH - fH) / 2;      // カレットとTextBoxの中心を合わせる
            if (fY < 0) fY = cY + cH + Math.Abs(yOffset);

            int fRight = fX + fW;
            int fBottom = fY + fH;
            Rectangle rect = ScreenInfo.Singleton.GetScreenContaining(cX, cY);
            //if (fRight >= rect.X + rect.Width) fX = cX - fW - Math.Abs(xOffset);
            //if (fBottom >= rect.Y + rect.Height) fY = cY - fH - Math.Abs(yOffset);
            // スクリーンからはみ出したとき
            if (fRight >= rect.X + rect.Width) {
                fX = cX - fW - Math.Abs(xOffset);
                if (fY >= cY && fY <= cY + cH || fY < cY && fY + fH >= cY) {
                    fY = cY + cH;
                }
            }
            if (bLog) logger.WarnH($"MOVE: X={fX}, Y={fY}, W={fW}, H={fH}");
            MoveWindow(this.Handle, fX, fY, fW, fH, true);

            ShowNonActive();
        }

        public void MoveWindow()
        {
            logger.WarnH($"MOVE before: X={Location.X}, Y={Location.Y}, W={Width}, H={Height}");
            resetFormSize();
            MoveWindow(this.Handle, this.Location.X, this.Location.Y, this.Width, this.Height, true);
            logger.WarnH($"MOVE after: X={Location.X}, Y={Location.Y}, W={Width}, H={Height}");
        }

        //------------------------------------------------------------------
        // イベントハンドラ
        //------------------------------------------------------------------
        private void FrmDisplayBuffer_VisibleChanged(object sender, EventArgs e)
        {
            CommonState.VkbVisible = this.Visible;
            CommonState.VkbVisibiltyChangedDt = HRDateTime.Now;
        }

        private void editTextBox_TextChanged(object sender, EventArgs e)
        {
            //logger.WarnH($"text={EditText}");
            resetFormSize();
            //if (EditText._notEmpty()) ShowNonActive();
            //logger.WarnH($"text={EditText}");
        }
    }
}

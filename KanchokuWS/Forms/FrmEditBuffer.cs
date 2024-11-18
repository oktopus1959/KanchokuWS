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

        private FrmCandidateSelector frmCands;

        public static int CurrentScreen = 0;

        private const int LongVkeyCharSize = 20;

        //------------------------------------------------------------------------------------
        /// <summary>編集バッファの文字列を返す</summary>
        public string EditText => editTextBox.Text;

        public bool IsEmpty => EditText._isEmpty();

        //------------------------------------------------------------------------------------
        /// <summary> コンストラクタ </summary>
        /// <param name="form"></param>
        public FrmEditBuffer(FrmKanchoku form)
        {
            frmMain = form;

            InitializeComponent();

            Width = 10;

            // タイトルバーや境界線を消す
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

        public void SetFrmCands(FrmCandidateSelector frmCands)
        {
            this.frmCands = frmCands;
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
                    case "Flush":
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

            int pos = 0;
            if (str._notEmpty()) {
                while (pos < str.Length && !toFlush) {
                    if (str[pos] == '!' && pos + 1 < str.Length && str[pos + 1] == '{') {
                        // "!{...}"
                        pos += 2;
                        var sb = new StringBuilder();
                        while (pos < str.Length && str[pos] != '}') {
                            sb.Append(str[pos++]);
                        }
                        handleFunctionalKey(sb.ToString());
                    } else {
                        if (str[pos] == '(' && str[str.Length - 1] == ')') {
                            var value = Handler.HandlerUtils.ParseTernaryOperator(str._safeSubstring(pos), "@");
                            logger.WarnH($"value={value}");
                            if (value._notEmpty()) {
                                str = value;
                                pos = 0;
                                continue;
                            }
                        }
                        preText += str[pos];
                    }
                    ++pos;
                }
            }

            editTextBox.Text = makeEditText(preText, postText);
            if (toFlush) FlushBuffer();
            if (EditText._notEmpty()) {
                ShowNonActive();
            } else {
                this.Hide();
            }

            if (pos < str.Length) {
                // 余った入力は、SendInputする
                SendInputHandler.Singleton.SendString(str._safeSubstring(pos)._toCharArray(), str.Length - pos, 0);
            }
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
            if (EditText._notEmpty()) {
                ShowNonActive();
            } else {
                this.Hide();
            }
        }

        private string makeEditText(string preText, string postText)
        {
            logger.WarnH(() => $"preText={preText}, preLen={preText.Length}, postText={postText}, postLen={postText}");
            var text = preText + ((/*preText._notEmpty() ||*/ postText._notEmpty()) ? CARET : "") + postText;       // 文中のときだけカレットを入れる
            logger.WarnH(() => $"text={text}, len={text.Length}");
            return text;
        }

        private string[] splitByCaret()
        {
            string[] result = new string[2];
            int pos = editTextBox.Text._safeIndexOf(CARET[0]);
            if (pos < 0) {
                result[0] = editTextBox.Text;
                result[1] = "";
            } else {
                result[0] = editTextBox.Text._safeSubstring(0, pos);
                result[1] = editTextBox.Text._safeSubstring(pos + 1);
            }
            return result;
        }

        private void moveCaretLeft()
        {
            var ts = splitByCaret();
            if (ts[0]._notEmpty()) {
                string pre = ts[0]._safeSubstring(0, -1);
                string post = ts[0]._safeSubstring(-1) + ts[1];
                editTextBox.Text = makeEditText(pre, post);
            }
        }

        private void moveCaretRight()
        {
            var ts = splitByCaret();
            if (ts[1]._notEmpty()) {
                string pre = ts[0] + ts[1]._safeSubstring(0, 1);
                string post = ts[1]._safeSubstring(1);
                editTextBox.Text = makeEditText(pre, post);
            }
        }

        private void moveCaretHome()
        {
            var ts = splitByCaret();
            editTextBox.Text = makeEditText("", ts[0] + ts[1]);
        }

        private void moveCaretEnd()
        {
            var ts = splitByCaret();
            editTextBox.Text = makeEditText(ts[0] + ts[1], "");
        }

        private void backspace()
        {
            var ts = splitByCaret();
            if (ts[0]._notEmpty()) {
                string pre = ts[0]._safeSubstring(0, -1);
                editTextBox.Text = makeEditText(pre, ts[1]);
            }
        }

        private void delete()
        {
            var ts = splitByCaret();
            if (ts[1]._notEmpty()) {
                string post = ts[1]._safeSubstring(1);
                editTextBox.Text = makeEditText(ts[0], post);
            }
        }

        /// <summary>編集バッファをフラッシュして、アプリケーションに文字列を送出する</summary>
        public void FlushBuffer()
        {
            string result = editTextBox.Text._safeReplace(CARET, "");
            editTextBox.Text = "";
            editTextBox.SelectionStart = 0;
            this.Hide();
            //frmCands?.Hide();
            //Helper.WaitMilliSeconds(10);
            //System.Windows.Forms.Application.DoEvents();
            var winClass = ActiveWindowHandler.Singleton.ActiveWinClassName;
            SendInputHandler.Singleton.SendStringViaClipboardIfNeeded(result._toCharArray(), 0, winClass == "mintty" || winClass == "PuTTY");
            //this.ShowNonActive();
            logger.WarnH($"CALLED");
        }

        /// <summary>Decoderの非活性化時に編集バッファをフラッシュして、アプリケーションに文字列を送出する</summary>
        public void FlushBufferOnDeactivated()
        {
            if (EditText.Length <= 10000) FlushBuffer();
        }

        public bool ClearBuffer()
        {
            if (editTextBox.Text._isEmpty()) return false;

            editTextBox.Text = "";
            editTextBox.SelectionStart = 0;
            this.Hide();
            frmCands?.Hide();
            return true;
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

        //------------------------------------------------------------------------------------
        // 移動・表示
        //------------------------------------------------------------------------------------
        [DllImport("user32.dll")]
        private static extern void ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        // ウィンドウをアクティブにせずに表示する
        private const int SW_SHOWNA = 8;

        /// <summary>バッファが空でない場合に入力フォームを表示する</summary>
        public void ShowNonActive()
        {
            if (EditText._notEmpty()) ShowWindow(this.Handle, SW_SHOWNA);   // NonActive
        }

        /// <summary>
        /// 表示・編集バッファをカレットの近くに移動する<br/>
        /// これが呼ばれるのはデコーダがONのときだけ
        /// </summary>
        public void MoveWindow(Settings.WindowsClassSettings activeWinSettings, Rectangle activeWinCaretPos, bool bDiffWin, bool bFixedPosWinClass, bool bLog)
        {
            //if (bDiffWin) {
            //    var font = FontInfo.GetActiveWindowFont(1.0f);
            //    logger.WarnH($"font.Name={font?.Name}, font.Size ={font.Size}");
            //    if (font != null) editTextBox.Font = font;
            //}

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

            int fY = cY + (cH - fH) / 2 + 1;      // カレットとTextBoxの中心より若干下に位置させる
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

        //------------------------------------------------------------------------------------
        // イベントハンドラ
        //------------------------------------------------------------------------------------
        private void editTextBox_TextChanged(object sender, EventArgs e)
        {
            //logger.WarnH($"text={EditText}");
            resetFormSize();
            //if (EditText._notEmpty()) ShowNonActive();
            //logger.WarnH($"text={EditText}");
        }
    }
}

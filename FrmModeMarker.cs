using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utils;

namespace KanchokuWS
{
    public partial class FrmModeMarker : Form
    {
        private static Logger logger = Logger.GetLogger();

        private static string kanchokuModeFace = "漢";
        private static string zenkakuModeFace = "全";
        private static string alphaModeFace = "Ａ";

        private FrmKanchoku frmMain;

        private FrmVirtualKeyboard frmVkb;

        private bool bTerminated = false;

        public FrmModeMarker(FrmKanchoku fMain, FrmVirtualKeyboard fVkb)
        {
            frmMain = fMain;
            frmVkb = fVkb;

            InitializeComponent();

            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmModeMarker_FormClosing);

            // タイトルバーを消す
            FormBorderStyle = FormBorderStyle.None;
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

        private void FrmModeMarker_Load(object sender, EventArgs e)
        {
            // タスクバーに表示しない
            this.ShowInTaskbar = false;
            // 最前面表示
            this.TopMost = true;
            // サイズ設定
            this.Width = 24;
            this.Height = 24;

            // タイマー開始
            timer1.Interval = Settings.ModeMarkerProcLoopPollingMillisec;
            timer1.Start();
            logger.Info("Timer Started");
        }

        private void FrmModeMarker_FormClosing(object sender, FormClosingEventArgs e)
        {
            logger.InfoH("ENTER");
            bTerminated = true;
            timer1.Stop();
            logger.Info("Timer Stopped");
            Helper.WaitMilliSeconds(300);           // 微妙なタイミングで invoke されるのを防ぐ
            logger.InfoH("LEAVE");
        }

        private int remainingCount = 0;

        private void resetCount()
        {
            remainingCount = Settings.KanjiModeMarkerShowIntervalSec * (1000 / Settings.ModeMarkerProcLoopPollingMillisec);
        }

        private void decrementCount()
        {
            if (Settings.KanjiModeMarkerShowIntervalSec > 0 && remainingCount >= 0) --remainingCount;
        }

        private bool isCountZero()
        {
            return Settings.KanjiModeMarkerShowIntervalSec <= 0 || remainingCount == 0;
        }

        /// <summary>
        /// 自身の表示を消して、カウントをリセットする<br/>
        /// このメソッドは、ユーザが何かキー入力をした時に呼ばれることを想定
        /// </summary>
        public void Vanish()
        {
            if (Settings.KanjiModeMarkerShowIntervalSec > 0) {
                this.Hide();
                resetCount();
            }
        }

        public string FaceString => faceLabel;

        private string newFaceLabel = kanchokuModeFace;
        private string faceLabel = kanchokuModeFace;

        private int iAlphaMode = 0;
        private DateTime alphaModeHideDt;

        private bool foreColorChanged = false;
        private bool waiting2ndStroke = false;

        private bool isFaceLabelChanged()
        {
            return newFaceLabel._ne(faceLabel);
        }

        public void SetAlphaMode()
        {
            foreColorChanged = waiting2ndStroke;
            waiting2ndStroke = false;
            if (Settings.AlphaModeMarkerShowMillisec > 0) {
                newFaceLabel = alphaModeFace;
                iAlphaMode = 1;
                changeShowState();
            }
        }

        public void SetKanjiMode()
        {
            logger.Info("CALLED");
            newFaceLabel = kanchokuModeFace;
            faceLabel = alphaModeFace;
            iAlphaMode = 0;
            foreColorChanged = waiting2ndStroke;
            waiting2ndStroke = false;
            changeShowState();
        }

        public void SetWait2ndStrokeMode(bool flag)
        {
            logger.Debug($"CALLED: flag={flag}");
            if (Settings.KanjiModeMarkerShowIntervalSec == 0) {
                newFaceLabel = kanchokuModeFace;
                faceLabel = alphaModeFace;
                iAlphaMode = 0;
                foreColorChanged = waiting2ndStroke != flag;
                waiting2ndStroke = flag;
                logger.Debug($"foreColorChanged={foreColorChanged}");
                if (foreColorChanged) changeShowState();
            }
        }

        // 全角モードの時は、キー入力のたびに呼ばれることを想定
        public void SetZenkakuMode()
        {
            logger.Info($"CALLED: faceLabel={faceLabel}");
            newFaceLabel = zenkakuModeFace;
            iAlphaMode = 0;
            foreColorChanged = waiting2ndStroke;
            waiting2ndStroke = false;
            if (newFaceLabel != faceLabel) {
                logger.Info("FireSignal");
                changeShowState();
            }
        }

        public void ShowImmediately() {
            logger.Info($"CALLED");
            faceLabel = "";
            changeShowState();
        }

        private void showMyForm()
        {
            if (bTerminated) return;

            if ((iAlphaMode == 0 && Settings.KanjiModeMarkerShowIntervalSec >= 0) ||
                (iAlphaMode > 0 && Settings.AlphaModeMarkerShowMillisec > 0)) {
                frmMain.ShowFrmMode();
            } else {
                // モード標識を非表示の場合
                this.Hide();
            }
        }

        private void setNewFaceLabel()
        {
            if (bTerminated) return;

            this.label1.Text = faceLabel = newFaceLabel;
        }

        /// <summary> 表示状態の変更 </summary>
        private void changeShowState()
        {
            if (iAlphaMode > 0) {
                if (iAlphaMode == 1) {
                    iAlphaMode = 2;
                    showMyForm();
                    alphaModeHideDt = DateTime.Now.AddMilliseconds((Settings.AlphaModeMarkerShowMillisec > 0 ? Settings.AlphaModeMarkerShowMillisec : 1000) - 20);
                } else if (alphaModeHideDt < DateTime.Now) {
                    iAlphaMode = 0;
                    this.Hide();
                    this.label1.Text = kanchokuModeFace;
                }
            } else {
                if (!frmMain.IsDecoderActive || frmVkb.Visible || this.Visible) {
                    // デコーダが非アクティブか、仮想鍵盤が表示されているか、自身が表示されていれば、常にカウントをリセットする
                    resetCount();
                    if ((!frmMain.IsDecoderActive || frmVkb.Visible) && this.Visible) {
                        if (Settings.LoggingActiveWindowInfo) logger.InfoH("Hide ModeMarker");
                        this.Hide();
                    }
                } else {
                    decrementCount();
                }

                if (foreColorChanged) {
                    changeForeColor();
                }
                if (isFaceLabelChanged() || isCountZero()) {
                    if (isFaceLabelChanged()) setNewFaceLabel();
                    if (frmMain.IsDecoderActive && !frmVkb.Visible && !this.Visible) {
                        // 再表示する
                        showMyForm();
                    }
                }
            }
        }

        private void FrmModeMarker_VisibleChanged(object sender, EventArgs e)
        {
            changeForeColor();
            label1.Text = faceLabel = newFaceLabel;
        }

        private void changeForeColor()
        {
            var colorName = iAlphaMode == 2 ? Settings.AlphaModeForeColor : waiting2ndStroke ? Settings.KanjiModeMarker2ndForeColor : Settings.KanjiModeMarkerForeColor;
            if (colorName._notEmpty() && colorName._ne(label1.ForeColor.Name)) {
                label1.ForeColor = Color.FromName(colorName);
            }
        }

        // 終了
        private void Exit_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //frmMain.DeactivateDecoder();
            //if (!Settings.ConfirmOnClose || SystemHelper.OKCancelDialog("漢直窓を終了します。\r\nよろしいですか。")) {
            //    this.Close();
            //    frmMain.Close();
            //}
            logger.Debug("ENTER");
            frmMain.Terminate();
            logger.Debug("LEAVE");
        }

        private void Settings_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmMain.DeactivateDecoder();
            if (!DlgSettings.BringTopMostShownDlg()) {
                var dlg = new DlgSettings(frmMain, null, this);
                dlg.ShowDialog();
                bool bRestart = dlg.RestartRequired;
                bool bNoSave = dlg.NoSave;
                dlg.Dispose();
                if (bRestart) frmMain.Restart(bNoSave);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            changeShowState();
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

        private void ReadBushuDic_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmMain.ReloadBushuDic();
        }
    }
}

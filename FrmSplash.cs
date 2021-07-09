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
    public partial class FrmSplash : Form
    {
        private static Logger logger = Logger.GetLogger();

        private const int timerInterval = 200;

        //private FrmKanchoku frmMain;
        private int showCount = 30 * (1000 / timerInterval);

        public FrmSplash(int sec)
        {
            //frmMain = frm;
            showCount = sec * (1000 / timerInterval);

            InitializeComponent();

            // タイトルバーを消す
            FormBorderStyle = FormBorderStyle.None;

            label_version.Text += Settings.Version;
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

        private void FormSplash_Load(object sender, EventArgs e)
        {
            timer1.Interval = timerInterval;
            timer1.Start();
            logger.Info("Timer Started");
        }

        public bool IsKanchokuReady { get; set; } = false;

        public bool IsKanchokuTerminated { get; set; } = false;

        private void checkKanchokuReady()
        {
            if (IsKanchokuReady) {
                label_initializing.Hide();
                buttonOK.Show();
                buttonSettings.Show();
            }
        }

        public bool SettingsDialogOpenFlag { get; private set; } = false;

        private void buttonSettings_Click(object sender, EventArgs e)
        {
            SettingsDialogOpenFlag = true;
            this.Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!buttonOK.Visible) {
                checkKanchokuReady();
            } else {
                if (showCount > 0) {
                    --showCount;
                    if (showCount == 0 || IsKanchokuTerminated) {
                        this.Close();
                    }
                }
            }
        }

        private void FormSplash_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Stop();
            logger.Info("Timer Stopped");
        }

    }
}

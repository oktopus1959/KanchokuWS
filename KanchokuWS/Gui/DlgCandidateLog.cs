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

namespace KanchokuWS.Gui
{
    public partial class DlgCandidateLog : Form
    {
        private static Logger logger = Logger.GetLogger();

        private Action NotifyToClose;

        private Action RefreshLog;

        private Form frmFocus;

        public DlgCandidateLog(Action notifier, Action refreshLog, Form focusFrm)
        {
            NotifyToClose = notifier;
            RefreshLog = refreshLog;
            frmFocus = focusFrm;

            InitializeComponent();
        }

        public void WriteLog(string msg)
        {
            try {
                richTextBox1.Focus();
                richTextBox1.AppendText(msg);
                //frmFocus?.Focus();
            } catch (Exception ex) {
                logger.Warn(ex._getErrorMsg());
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void DlgCandidateLog_FormClosing(object sender, FormClosingEventArgs e)
        {
            NotifyToClose?.Invoke();
        }

        private void button_refresh_Click(object sender, EventArgs e)
        {
            RefreshLog?.Invoke();
        }
    }
}

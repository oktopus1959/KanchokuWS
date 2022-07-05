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
    public partial class DlgStrokeLog : Form
    {
        private static Logger logger = Logger.GetLogger();

        private Action NotifyToClose;

        private Form frmFocus;

        public DlgStrokeLog(Action notifier, Form focusFrm)
        {
            NotifyToClose = notifier;
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

        private void DlgStrokeLog_FormClosing(object sender, FormClosingEventArgs e)
        {
            NotifyToClose?.Invoke();
        }
    }
}

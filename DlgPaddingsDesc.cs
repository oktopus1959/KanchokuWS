using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KanchokuWS
{
    public partial class DlgPaddingsDesc : Form
    {
        public DlgPaddingsDesc(string desc)
        {
            InitializeComponent();

            textBox1.Text = desc;
        }

        private void button_close_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}

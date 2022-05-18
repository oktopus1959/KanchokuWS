using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

using Utils;

namespace KanchokuWS.Gui
{
    public partial class DlgColorSelector : Form
    {
        private static string[] allColorNames = null;

        private static HashSet<string> darkColors;

        private const int NumColumns = 7;

        private static int numRows = 20;

        private static double GetLightness(Color col)
        {
            return col.R * 0.2989 + col.G * 0.587 + col.B * 0.114;
        }

        protected static void getAllColorNames()
        {
            if (allColorNames == null) {
                allColorNames = typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static).Select(p => p.Name).Skip(1).ToArray();   // Skip(1)で先頭の Transparent をスキップ
                darkColors = new HashSet<string>(allColorNames.OrderBy(x => GetLightness(Color.FromName(x))).Take(40));
                numRows = (allColorNames.Length + (NumColumns - 1)) / NumColumns;
            }
        }

        public string ColorName { get; set; }

        private Font normalFont = null;

        public DlgColorSelector(string colorName)
        {
            ColorName = colorName;

            InitializeComponent();

            AcceptButton = buttonOK;
            CancelButton = buttonCancel;

            normalFont = new Font("Arial", 9);

            getAllColorNames();
        }

        private void DlgColorSelector_Load(object sender, EventArgs e)
        {
            double dpiRate = ScreenInfo.Singleton.PrimaryScreenDpiRate;
            int cellWidth = (int)(100 * dpiRate);
            int cellHeight = (int)(20 * dpiRate);
            var dgv = dataGridView1;
            dgv.Width = NumColumns * cellWidth + 3;
            dgv.Height = numRows * cellHeight + 3;
            dgv._defaultSetup(0, cellHeight);       // headerHeight=0 -> ヘッダーを表示しない
            //dgv._setSelectionColorReadOnly();
            dgv._setSelectionColor(DgvHelpers.BLACK_SELECTION_COLOR);
            dgv._setDefaultFont(normalFont);
            //dgv._setDefaultFont(DgvHelpers.FontMSG8);
            //dgv._disableToolTips();
            for (int i = 0; i < NumColumns; ++i) {
                dgv.Columns.Add(dgv._makeTextBoxColumn_ReadOnly($"color{i}", "", cellWidth)._setUnresizable());
            }

            dgv.Rows.Add(numRows);

            sortAndShowColorNames();
        }

        private void DlgColorSelector_FormClosing(object sender, FormClosingEventArgs e)
        {
            normalFont?.Dispose();
        }

        private void sortAndShowColorNames()
        {
            var names = allColorNames.OrderBy(x => Color.FromName(x).GetHue()).ToArray();
            var sortedNames = new string[names.Length];
            for (int i = 0; i < NumColumns; ++i) {
                Array.Copy(names.Skip(i * 20).Take(20).OrderByDescending(x => GetLightness(Color.FromName(x))).ToArray(), 0, sortedNames, i * 20, 20);
            }
            showColorNames(sortedNames);
        }

        private void showColorNames(string[] names)
        {
            for (int i = 0; i < NumColumns; ++i) {
                for (int j = 0; j < numRows; ++j) {
                    var name = names[i * numRows + j];
                    dataGridView1.Rows[j].Cells[i].Value = name;
                    dataGridView1.Rows[j].Cells[i].Style.BackColor = Color.FromName(name);
                    if (darkColors.Contains(name))
                        dataGridView1.Rows[j].Cells[i].Style.ForeColor = Color.White;
                    else
                        dataGridView1.Rows[j].Cells[i].Style.ForeColor = Color.Black;
                }
            }
            var lowerName = ColorName._toLower();
            int n = names._findIndex(x => x._toLower() == lowerName)._lowLimit(0);
            dataGridView1.CurrentCell = dataGridView1.Rows[n % numRows].Cells[n / numRows];

            dataGridView1.Focus();
        }

        private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell?.Value != null) {
                ColorName = dataGridView1.CurrentCell.Value.ToString();
                var color = Color.FromName(ColorName);
                label1.ForeColor = color;
                label2.Text = ColorName;
                panel2.BackColor = color;
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void buttonSortByName_Click(object sender, EventArgs e)
        {
            showColorNames(allColorNames);
        }

        private void buttonSortByHL_Click(object sender, EventArgs e)
        {
            sortAndShowColorNames();
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Drawing;

namespace Utils
{
    /// <summary>
    /// DataGridView 関連のヘルパー関数および拡張メソッド
    /// </summary>
    public static class DgvHelpers
    {
        //-----------------------------------------------------------------------------
        /// <summary>
        /// 行の一部をクリックしたとき、行全体を選択状態にする
        /// </summary>
        public const bool FULL_ROW_SELECT = true;
        /// <summary>
        /// 複数行の選択を有効にする
        /// </summary>
        public const bool MULTI_SELECT = true;
        /// <summary>
        /// 中央揃えを有効にする
        /// </summary>
        public const bool CENTERING = true;

        /// <summary>
        /// デフォルト(GDV全体設定)の選択色を使う
        /// </summary>
        public const int DEFAULT_SELECTION_COLOR = 0;
        /// <summary>
        /// ハイライトの選択色を使う（背景色が青）
        /// </summary>
        public const int HIGHLIGHT_SELECTION_COLOR = 1;
        /// <summary>
        /// リードオンリー用選択色を使う
        /// </summary>
        public const int READONLY_SELECTION_COLOR = 2;
        /// スモーク選択色を使う
        /// </summary>
        public const int SMOKE_SELECTION_COLOR = 3;
        /// レモン選択色を使う
        /// </summary>
        public const int LEMON_SELECTION_COLOR = 4;
        /// 透明な選択色を使う
        /// </summary>
        public const int TRANSPARENT_SELECTION_COLOR = 5;
        /// 黒の選択色を使う
        /// </summary>
        public const int BLACK_SELECTION_COLOR = 6;

        //-----------------------------------------------------------------------------
        // 使用するフォント
        /// <summary> MS ゴシック 11 ポイント </summary>
        static public Font FontMSG11 = new Font("MS Gothic", 11);
        /// <summary> MS ゴシック 10 ポイント </summary>
        static public Font FontMSG10 = new Font("MS Gothic", 10);
        /// <summary> MS ゴシック 9 ポイント </summary>
        static public Font FontMSG9 = new Font("MS Gothic", 9);
        /// <summary> MS ゴシック 8 ポイント </summary>
        static public Font FontMSG8 = new Font("MS Gothic", 8);
        /// <summary> MS UIゴシック 11 ポイント </summary>
        static public Font FontUIG11 = new Font("MS UI Gothic", 11);
        /// <summary> MS UIゴシック 10 ポイント </summary>
        static public Font FontUIG10 = new Font("MS UI Gothic", 10);
        /// <summary> MS UIゴシック 9 ポイント </summary>
        static public Font FontUIG9 = new Font("MS UI Gothic", 9);
        /// <summary> MS UIゴシック 8 ポイント </summary>
        static public Font FontUIG8 = new Font("MS UI Gothic", 8);

        static public Font FontYUG9 = new Font("Yu Gothic UI", 9);
        //-----------------------------------------------------------------------------
        /// <summary>
        /// ReadOnly セルの背景色
        /// </summary>
        static public Color ReadOnlyBackColor = Color.FromArgb(250, 250, 250);

        //-----------------------------------------------------------------------------

        /// <summary>
        /// DataGridView のデフォルトセットアップ (ヘッダの高さと行の高さを別個に指定)
        /// <para>fullRowSelect=true なら、行の一部をクリックすると行全体が選択される。</para>
        /// </summary>
        static public DataGridView _defaultSetup(this DataGridView dgv, int hdrHeight, int rowHeight, bool fullRowSelect = false, bool multiSelect = false)
        {
            return dgv._defaultSetup(Color.Beige, hdrHeight, rowHeight, fullRowSelect, multiSelect);
        }

        /// <summary>
        /// DataGridView のデフォルトセットアップ (hdrHeight/rowHeight により Header および Row の高さを指定, hdrColor によりヘッダーの背景色指定)
        /// <para>fullRowSelect=true なら、行の一部をクリックすると行全体が選択される。</para>
        /// </summary>
        static public DataGridView _defaultSetup(this DataGridView dgv, Color hdrColor, int hdrHeight, int rowHeight, bool fullRowSelect = false, bool multiSelect = false)
        {
            dgv._setDoubleBuffered();

            dgv.AllowUserToAddRows = false;                                                                 // ユーザによる行の追加不可
            dgv.AllowUserToDeleteRows = false;                                                              // ユーザによる行の削除不可
            dgv.AllowUserToResizeRows = false;                                                              // ユーザによる行のサイズ変更不可
            dgv.RowHeadersVisible = false;                                                                  // 行ヘッダーは表示しない
            dgv.RowTemplate.Height = rowHeight;                                                             // 通常行の高さ

            dgv.EnableHeadersVisualStyles = false;                                                          // ヘッダー背景色を変更可能にする
            dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;                        // ヘッダー複数行表示不可
            dgv.ColumnHeadersDefaultCellStyle.BackColor = hdrColor;                                         // ヘッダー背景色
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;        // ヘッダー中央揃え
            if (hdrHeight > 0)
                dgv.ColumnHeadersHeight = hdrHeight;
            else
                dgv.ColumnHeadersVisible = false;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;      // ヘッダー高さの変更は不可

            if (fullRowSelect)
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;                                // 行全体を選択するモード
            else
                dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;                                   // セル単体を選択するモード
            dgv.MultiSelect = multiSelect;                                                                  // 複数選択を可 or 不可とする
            return dgv;
        }


        /// <summary>
        /// セルのデフォルトフォントを設定する
        /// </summary>
        static public DataGridView _setDefaultFont(this DataGridView dgv, Font font)
        {
            dgv.DefaultCellStyle.Font = font;
            return dgv;
        }


        /// <summary>
        /// DGV全体でツールチップを非表示にする
        /// </summary>
        /// <param name="dgv"></param>
        /// <returns></returns>
        static public DataGridView _disableToolTips(this DataGridView dgv)
        {
            dgv.ShowCellToolTips = false;
            return dgv;
        }


        /// <summary>
        /// 選択時の背景色をリードオンリー色にする。文字色は黒。
        /// </summary>
        /// <param name="dgv"></param>
        static public DataGridView _setSelectionColorReadOnly(this DataGridView dgv)
        {
            return dgv._setSelectionColor(READONLY_SELECTION_COLOR);
        }

        /// <summary>
        /// 選択時の背景色をスモーク色にする。文字色は黒。
        /// </summary>
        /// <param name="dgv"></param>
        static public DataGridView _setSelectionColorSmoke(this DataGridView dgv)
        {
            return dgv._setSelectionColor(SMOKE_SELECTION_COLOR);
        }

        /// <summary>
        /// 選択時の背景色をレモン色にする。文字色は黒。
        /// </summary>
        /// <param name="dgv"></param>
        static public DataGridView _setSelectionColorLemon(this DataGridView dgv)
        {
            return dgv._setSelectionColor(LEMON_SELECTION_COLOR);
        }


        /// <summary>
        /// 表全体で、行選択時の背景色・テキスト色を変更する。
        /// <para>flag = DgvHelpers.DEFAULT_SELECTION_COLOR / DgvHelpers.HIGHLIGHT_SELECTION_COLOR / DgvHelpers.READONLY_SELECTION_COLOR / DgvHelpers.SMOKE_SELECTION_COLOR / DgvHelpers.TRANSPARENT_SELECTION_COLOR</para>
        /// </summary>
        static public DataGridView _setSelectionColor(this DataGridView dgv, int selectionColor)
        {
            dgv.DefaultCellStyle._setCellSelectionColor(selectionColor);
            return dgv;
        }

        /// <summary>
        /// 指定の selectionColor に従い、セルの選択色を設定する。selectionColor == DEFAULT_SELECTION_COLOR なら変更しない。
        /// <para>flag = DgvHelpers.DEFAULT_SELECTION_COLOR / DgvHelpers.HIGHLIGHT_SELECTION_COLOR / DgvHelpers.READONLY_SELECTION_COLOR / DgvHelpers.SMOKE_SELECTION_COLOR / DgvHelpers.TRANSPARENT_SELECTION_COLOR</para>
        /// </summary>
        /// <param name="style"></param>
        /// <param name="selectionColor"></param>
        static public void _setCellSelectionColor(this DataGridViewCellStyle style, int selectionColor)
        {
            if (selectionColor != DEFAULT_SELECTION_COLOR)
            {
                switch (selectionColor)
                {
                    case READONLY_SELECTION_COLOR:
                        style.SelectionBackColor = ReadOnlyBackColor;
                        style.SelectionForeColor = Color.Black;
                        break;
                    case SMOKE_SELECTION_COLOR:
                        style.SelectionBackColor = Color.WhiteSmoke;
                        style.SelectionForeColor = Color.Black;
                        break;
                    case LEMON_SELECTION_COLOR:
                        style.SelectionBackColor = Color.LemonChiffon;
                        style.SelectionForeColor = Color.Black;
                        break;
                    case TRANSPARENT_SELECTION_COLOR:
                        style.SelectionBackColor = Color.Transparent;
                        style.SelectionForeColor = Color.Black;
                        break;
                    case BLACK_SELECTION_COLOR:
                        style.SelectionBackColor = Color.Black;
                        style.SelectionForeColor = Color.White;
                        break;
                    default:
                        style.SelectionBackColor = SystemColors.Highlight;
                        style.SelectionForeColor = Color.White;
                        break;
                }
            }
        }


        /// <summary>
        /// テキストボックスカラムの作成 (ReadOnly, ソート無し、センタリング、デフォルトフォント)
        /// </summary>
        static public DataGridViewTextBoxColumn _makeTextBoxColumn_Centered(this DataGridView dgv,
            string name, string text, int width, int selectionColor = DEFAULT_SELECTION_COLOR)
        {
            return _makeTextBoxColumn(dgv, name, text, width, false, false, selectionColor, true);
        }


        /// <summary>
        /// テキストボックスカラムの作成 (ReadOnly, ソート無し、デフォルトフォント)
        /// </summary>
        static public DataGridViewTextBoxColumn _makeTextBoxColumn_ReadOnly(this DataGridView dgv,
            string name, string text, int width, int selectionColor = DEFAULT_SELECTION_COLOR, bool alignCentered = false)
        {
            return _makeTextBoxColumn(dgv, name, text, width, false, false, selectionColor, alignCentered);
        }


        /// <summary>
        /// テキストボックスカラムの作成 (汎用、デフォルトフォント)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <param name="width"></param>
        /// <param name="writable"></param>
        /// <param name="alignCentered"></param>
        /// <param name="sortable"></param>
        /// <param name="selectionColor"></param>
        /// <returns></returns>
        static public DataGridViewTextBoxColumn _makeTextBoxColumn(this DataGridView dgv,
            string name, string text, int width,
            bool sortable = false, bool writable = true,
            int selectionColor = DEFAULT_SELECTION_COLOR, bool alignCentered = false, bool alignRight = false)
        {
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.Name = name;
            col.HeaderText = text;
            col.ReadOnly = !writable;
            if (!sortable) col.SortMode = DataGridViewColumnSortMode.NotSortable;
            if (alignCentered) {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            } else if (alignRight) {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.DefaultCellStyle.Padding = new Padding(0, 0, 2, 0);
            } else {
                col.DefaultCellStyle.Padding = new Padding(2, 0, 0, 0);
            }
            if (writable)
            {
                // col.DefaultCellStyle.BackColor = Color.Azure;
                col.DefaultCellStyle.BackColor = Color.White;
            }
            else
            {
                col.DefaultCellStyle.BackColor = ReadOnlyBackColor;
            }
            col.DefaultCellStyle._setCellSelectionColor(selectionColor);
            col.Width = width;

            return col;
        }

        /// <summary>
        /// ヘッダー以外のセルの高さを自動調節するのON/OFF
        /// </summary>
        static public DataGridView _setAutoHeightSize(this DataGridView dgv, bool flag = true)
        {
            dgv.AutoSizeRowsMode = flag ? DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders : DataGridViewAutoSizeRowsMode.None;
            return dgv;
        }

        /// <summary>
        /// カラムの WrapMode を true にする
        /// </summary>
        static public DataGridViewColumn _setWrapMode(this DataGridViewColumn col)
        {
            col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            return col;
        }

        /// <summary>
        /// カラム幅のサイズ変更を不可に設定する
        /// </summary>
        /// <param name="col"></param>
        static public DataGridViewColumn _setUnresizable(this DataGridViewColumn col)
        {
            col.Resizable = DataGridViewTriState.False;
            return col;
        }

        /// <summary>
        /// DataGridView の DoubleBuffered プロパティをセットして、表示速度を改善する
        /// </summary>
        /// <param name="dgv"></param>
        static public void _setDoubleBuffered(this DataGridView dgv)
        {
            typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(dgv, true, null);
        }

        /// <summary>
        /// DataGridView の Row.Count を返す
        /// </summary>
        /// <param name="dgv"></param>
        /// <returns></returns>
        public static int _rowsCount(this DataGridView dgv)
        {
            return dgv?.Rows.Count ?? 0;
        }

    }

}

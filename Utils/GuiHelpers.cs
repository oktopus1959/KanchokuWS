using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Utils
{
    public static class GuiHelpers
    {
        /// <summary>
        /// フォーム form に属する TextBox のうち、指定された名前のものから値を取得する
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static public string getTextBoxValue(Form form, string name)
        {
            return getControlText(form, name);
        }

        /// <summary>
        /// フォーム form に属する TextBox のうち、指定された名前のものの Text に値をセットする
        /// </summary>
        static public void setTextBoxValue(Form form, string name, string val)
        {
            setControlText(form, name, val);
        }

        /// <summary>
        /// フォーム form に属する ComboBox のうち、指定された名前のものから値を取得する
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static public string getComboBoxValue(Form form, string name)
        {
            return getControlText(form, name);
        }

        /// <summary>
        /// フォーム form に属する、 指定された名前の ComboBox の Items に値をセットする（既にセットしてあった内容はクリアされる）
        /// </summary>
        /// <param name="form"></param>
        /// <param name="items"></param>
        /// <param name="bFirstBlank">先頭を空白にする</param>
        static public void setComboBoxItems(Form form, string name, IEnumerable<string> items, bool bFirstBlank = true)
        {
            Control c = findControl(form, name);
            if (c != null && c is ComboBox) {
                ((ComboBox)c)._setItems(items, bFirstBlank);
            }
        }

        /// <summary>
        /// フォーム form に属する、指定された名前のコントロールから Text 値を取得する。
        /// コントロールが存在しなければ null を返す。(コントロールが存在すれば null を返すことはない)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static public string getControlText(Form form, string name)
        {
            Control c = findControl(form, name);
            return (c != null) ? c.Text ?? "" : null;
        }

        /// <summary>
        /// フォーム form に属する、指定された名前のコントロールの Text 値をセットする
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static public void setControlText(Form form, string name, string val)
        {
            Control c = findControl(form, name);
            if (c != null) c.Text = val;
        }

        /// <summary>
        /// フォーム form に属する、指定された名前のコントロールにフォーカスを移す
        /// </summary>
        /// <param name="form"></param>
        /// <param name="name"></param>
        static public void focusControl(Form form, string name)
        {
            Control c = findControl(form, name);
            if (c != null) c.Focus();
        }

        /// <summary>
        /// フォーム form に属する、指定された名前のコントロールを Enable/Disable する
        /// </summary>
        /// <param name="form"></param>
        /// <param name="name"></param>
        static public void enableControl(Form form, string name, bool bEnable)
        {
            Control c = findControl(form, name);
            if (c != null) c.Enabled = bEnable;
        }

        /// <summary>
        /// フォーム form に属する、指定された名前のコントロールを取得する。指定の名前のコントロールが存在しなければ null を返す。
        /// </summary>
        /// <param name="form"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static public Control findControl(Form form, string name)
        {
            Control[] cs = form.Controls.Find(name, true);
            return (cs.Length > 0) ? cs[0] : null;
        }

        /// <summary>
        /// コントロール ctrl に属する、指定された名前のコントロールを取得する。指定の名前のコントロールが存在しなければ null を返す。
        /// </summary>
        /// <param name="form"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static public Control findControl(Control ctrl, string name)
        {
            Control[] cs = ctrl.Controls.Find(name, true);
            return (cs.Length > 0) ? cs[0] : null;
        }

        /// <summary>
        /// すべての TextBox を ReadOnly/NoTabStop にし、OK/Cancel 以外の Button, CheckBox/ComboBox/RadioButton を無効にする
        /// </summary>
        static public void disableAllControls(Control ctrl)
        {
            foreach (var obj in ctrl.Controls) {
                if (obj is TextBox) {
                    ((TextBox)obj).ReadOnly = true;
                    ((TextBox)obj).TabStop = false;
                } else if (obj is RichTextBox) {
                    ((RichTextBox)obj).ReadOnly = true;
                    ((RichTextBox)obj).TabStop = false;
                } else if (obj is Button && ((Button)obj).Name != "buttonOK" && ((Button)obj).Name != "buttonCancel") ((Button)obj).Enabled = false;
                else if (obj is CheckBox) ((CheckBox)obj).Enabled = false;
                else if (obj is ComboBox) ((ComboBox)obj).Enabled = false;
                //else if (obj is GroupBox) ((GroupBox)obj).Enabled = false;
                else if (obj is RadioButton) ((RadioButton)obj).Enabled = false;
                else if (obj is Control) disableAllControls((Control)obj);
            }
        }

        /// <summary>
        /// 色を表わす整数を System.Drawing.Color に変換
        /// </summary>
        /// <param name="iColor"></param>
        /// <returns></returns>
        static public System.Drawing.Color GetColorFromInt(int iColor)
        {
            return System.Drawing.Color.FromArgb((int)((uint)iColor | 0xff000000));
        }

        /// <summary>
        /// System.Drawing.Color を、色を表わす整数に変換
        /// </summary>
        /// <param name="iColor"></param>
        /// <returns></returns>
        static public int GetIntFromColor(System.Drawing.Color color)
        {
            return (int)((uint)color.ToArgb() & 0x00ffffff);
        }
    }

    /// <summary>
    /// Control の拡張メソッド
    /// </summary>
    public static class ControlExtension
    {
        /// <summary>
        /// フォーム form に属する、指定された名前のコントロールを取得する。指定の名前のコントロールが存在しなければ null を返す。
        /// </summary>
        public static Control _findControl(this Form form, string name)
        {
            return GuiHelpers.findControl(form, name);
        }

        /// <summary>
        /// コントロール ctrl に属する、指定された名前のコントロールを取得する。指定の名前のコントロールが存在しなければ null を返す。
        /// </summary>
        public static Control _findControl(this Control ctrl, string name)
        {
            return GuiHelpers.findControl(ctrl, name);
        }

        /// <summary>
        /// フォーム form に属する、指定された名前のコントロールから Text 値を取得する
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string _getControlText(this Form form, string name)
        {
            return GuiHelpers.getControlText(form, name);
        }

        /// <summary>
        /// フォーム form に属する、指定された名前のコントロールの Text 値をセットする
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static void _setControlText(this Form form, string name, string val)
        {
            GuiHelpers.setControlText(form, name, val);
        }

        /// <summary>
        /// ctrlで指定されたコントロールの Text 属性に text をセットする
        /// </summary>
        public static void _setText(this Control ctrl, string text)
        {
            if (ctrl != null) ctrl.Text = text;
        }

        /// <summary>
        /// ctrlで指定されたコントロールの BackColor 属性に color をセットする
        /// </summary>
        public static void _setBackColor(this Control ctrl, System.Drawing.Color backColor)
        {
            if (ctrl != null) ctrl.BackColor = backColor;
        }

        /// <summary>
        /// フォーム form に属する、指定された名前のコントロールの Enabled 属性に bool値をセットする
        /// </summary>
        public static void _setControlEnabled(this Form form, string name, bool bEnabled)
        {
            form._findControl(name)?._setEnabled(bEnabled);
        }

        /// <summary>
        /// ctrlで指定されたコントロールの Enabled 属性に bool値をセットする
        /// </summary>
        public static void _setEnabled(this Control ctrl, bool bEnabled)
        {
            if (ctrl != null) ctrl.Enabled = bEnabled;
        }

        /// <summary>
        /// フォーム form に属する、指定された名前の CheckBox コントロールの Checked 属性取得する
        /// </summary>
        public static bool _getCheckBoxChecked(this Form form, string name)
        {
            return form._findControl(name)._getChecked() ?? false;
        }

        /// <summary>
        /// フォーム form に属する、指定された名前の CheckBox コントロールの Checked 属性に bool値をセットする
        /// </summary>
        public static void _setCheckBoxChecked(this Form form, string name, bool bChecked)
        {
            form._findControl(name)._setChecked(bChecked);
        }

        /// <summary>
        /// フォーム form に属する、指定された名前の RadioButton コントロールの Checked 属性を取得する
        /// </summary>
        public static bool _getRadioButtonChecked(this Form form, string name)
        {
            return form._findControl(name)._getChecked() ?? false;
        }

        /// <summary>
        /// フォーム form に属する、指定された名前の RadioButton コントロールの Checked 属性に bool値をセットする
        /// </summary>
        public static void _setRadioButtonChecked(this Form form, string name, bool bChecked)
        {
            form._findControl(name)._setChecked(bChecked);
        }

        /// <summary>
        /// ctrlで指定された CheckBox または RadioButton コントロールの Checked 属性を取得する
        /// </summary>
        public static bool? _getChecked(this Control ctrl)
        {
            if (ctrl != null) {
                if (ctrl is CheckBox) {
                    return ((CheckBox)ctrl).Checked;
                } else if (ctrl is RadioButton) {
                    return ((RadioButton)ctrl).Checked;
                }
            }
            return null;
        }

        /// <summary>
        /// ctrlで指定された CheckBox または RadioButton コントロールの Checked 属性に bool値をセットする
        /// </summary>
        public static void _setChecked(this Control ctrl, bool bChecked)
        {
            if (ctrl != null) {
                if (ctrl is CheckBox) {
                    ((CheckBox)ctrl).Checked = bChecked;
                } else if (ctrl is RadioButton) {
                    ((RadioButton)ctrl).Checked = bChecked;
                }
            }
        }

        /// <summary>
        /// ComboBox から選択されている Item の文字列を取得する。何も選択されていなければ null を返す。
        /// </summary>
        /// <param name="cmb"></param>
        /// <returns></returns>
        public static string _getSelectedItem(this ComboBox cmb, string defval = "")
        {
            return cmb?.SelectedItem._objToString(defval);
        }

        /// <summary>
        /// ComboBox から選択されている Item の、最初の空白文字までの文字列を取得する。何も選択されていなければ null を返す。
        /// </summary>
        /// <param name="cmb"></param>
        /// <returns></returns>
        public static string _getSelectedItemSplittedFirst(this ComboBox cmb, string defval = "")
        {
            var item = cmb._getSelectedItem(defval);
            return item != null ? item._split(' ')._getFirst()._toSafe() : null;
        }

        /// <summary>
        /// 文字列リストを ComboBox の Items にセットする。（既にセットしてあった内容はクリアされる）<para/>
        /// bFirstBlank = true なら先頭要素は空白にする。idx >= 0 なら引数の idx 番目の要素を SelectIndex に設定する。
        /// </summary>
        /// <param name="cmb"></param>
        /// <param name="items"></param>
        public static void _setItems(this ComboBox cmb, IEnumerable<string> items, int idx, bool bFirstBlank = false)
        {
            _setItems(cmb, items, idx, bFirstBlank ? "" : null);
        }

        /// <summary>
        /// 文字列リストを ComboBox の Items にセットする。（既にセットしてあった内容はクリアされる）<para/>
        /// firstAddedItem != null ならそれを先頭要素に追加する。idx >= 0 なら引数 items の idx 番目の要素を SelectIndex に設定する。
        /// firstAddedItem != null && bSetFirst == true なら、追加した先頭要素を Select する。
        /// </summary>
        /// <param name="cmb"></param>
        /// <param name="items"></param>
        public static void _setItems(this ComboBox cmb, IEnumerable<string> items, int idx, string firstAddedItem, bool bSetFirst = false)
        {
            if (cmb != null) {
                cmb.Items.Clear();
                if (firstAddedItem != null) {
                    cmb.Items.Add(firstAddedItem);
                    idx = bSetFirst ? 0 : idx >= 0 ? idx += 1 : -1;
                }
                if (items != null) {
                    cmb.Items.AddRange(items.ToArray());
                    if (idx >= 0 && idx < items.Count()) cmb.SelectedIndex = idx;
                }
            }
        }

        /// <summary>
        /// 文字列リストのペアを ComboBox の Items に "Item1 [Item2]" の形式でセットする。（既にセットしてあった内容はクリアされる）<para/>
        /// bFirstBlank = true なら先頭要素は空白にする。idx >= 0 なら引数の idx 番目の要素を SelectIndex に設定する。
        /// </summary>
        /// <param name="cmb"></param>
        /// <param name="items"></param>
        public static void _setPairItems(this ComboBox cmb, IEnumerable<Tuple<string, string>> items, int idx, bool bFirstBlank = false)
        {
            cmb._setItems(items.Select(x => $"{x.Item1}  " + (x.Item2._notEmpty() ? $"[{x.Item2}]" : "")));
        }

        /// <summary>
        /// 文字列リストを ComboBox の Items にセットする。（既にセットしてあった内容はクリアされる）<para/>
        /// bFirstBlank = true なら先頭要素は空白にする。bSetFirst = true なら引数の先頭要素を SelectIndex に設定する。
        /// </summary>
        /// <param name="cmb"></param>
        /// <param name="items"></param>
        public static void _setItems(this ComboBox cmb, IEnumerable<string> items, bool bFirstBlank = false, bool bSetFirst = false)
        {
            cmb._setItems(items, bSetFirst ? 0 : -1, bFirstBlank);
        }

        /// <summary>
        /// 文字列リストを ComboBox の Items にセットする。（既にセットしてあった内容はクリアされる）<para/>
        /// bFirstBlank = true なら先頭要素は空白にする。selectedItem を SelectIndex に設定する。
        /// </summary>
        /// <param name="cmb"></param>
        /// <param name="items"></param>
        public static void _setItems(this ComboBox cmb, IEnumerable<string> items, string selectedItem, bool bFirstBlank = false)
        {
            if (cmb != null) {
                cmb.Items.Clear();
                if (bFirstBlank) cmb.Items.Add("");
                if (items != null) {
                    //foreach (var item in items)
                    //{
                    //    cmb.Items.Add(item);
                    //}
                    cmb.Items.AddRange(items.ToArray());
                    cmb._selectItem(selectedItem);
                }
            }
        }

        /// <summary>
        /// 指定の文字列を Item として選択する。bSelectFirstIfUnmatched == true なら、選択肢に含まれていない場合は、先頭Itemを選択する。
        /// </summary>
        /// <param name="cmb"></param>
        /// <param name="item"></param>
        public static void _selectItem(this ComboBox cmb, string item, bool bSelectFirstIfUnmatched = true)
        {
            try {
                if (cmb.Items.Count > 0 && item != null) {
                    int idx = cmb.Items.IndexOf(item);
                    if (idx < 0) {
                        if (bSelectFirstIfUnmatched)
                            idx = 0;
                        else
                            cmb.Text = item;
                    }
                    cmb.SelectedIndex = idx;
                }
            } catch (Exception) { }
        }

        /// <summary>
        /// 指定の文字列+空白を先頭部に持つ Item を選択する。bSelectFirstIfUnmatched == true なら、選択肢に含まれていない場合は、先頭Itemを選択する。
        /// </summary>
        /// <param name="cmb"></param>
        /// <param name="item"></param>
        public static void _selectItemStartsWith(this ComboBox cmb, string item, bool bSelectFirstIfUnmatched = true)
        {
            try {
                if (item != null) {
                    if (cmb.Items.Count > 0) {
                        var key = item + " ";
                        for (int idx = 0; idx < cmb.Items.Count; ++idx) {
                            var tgt = (string)cmb.Items[idx];
                            if (tgt._equalsTo(item) || tgt.StartsWith(key)) {
                                cmb.SelectedIndex = idx;
                                return;
                            }
                        }
                        // 選択肢に含まれず
                        if (bSelectFirstIfUnmatched) {
                            cmb.SelectedIndex = 0;
                            return;
                        }
                    } else {
                        cmb.Items.Add(item);
                        cmb.SelectedIndex = 0;
                    }
                }
            } catch { }
        }

        /// <summary>
        /// フォーム form に属する、 指定された名前の ComboBox の Items に値をセットする（既にセットしてあった内容はクリアされる）
        /// bFirstBlank = true なら先頭要素は空白にする。bSetFirst = true なら引数の先頭要素を SelectIndex に設定する。
        /// </summary>
        /// <param name="form"></param>
        /// <param name="items"></param>
        /// <param name="bFirstBlank">先頭を空白にする</param>
        public static void _setComboBoxItems(this Form form, string name, IEnumerable<string> items, bool bFirstBlank = true, bool bSetFirst = false)
        {
            Control c = form._findControl(name);
            if (c != null && c is ComboBox) {
                ((ComboBox)c)._setItems(items, bFirstBlank, bSetFirst);
            }
        }

        /// <summary>
        /// 指定されたコントロールのフォーカス状態を取得する
        /// </summary>
        public static bool _focused(this Control ctl)
        {
            return (ctl != null) ? ctl.Focused : false;
        }

        /// <summary>
        /// フォーム form に属する、 指定された名前のコントロールのフォーカス状態を取得する
        /// </summary>
        public static bool _focused(this Form form, string name)
        {
            return form._findControl(name)._focused();
        }

        /// <summary>
        /// 指定されたコントロールにフォーカスを設定する
        /// </summary>
        public static void _focus(this Control ctl)
        {
            if (ctl != null) ctl.Focus();
        }

        /// <summary>
        /// フォーム form に属する、 指定された名前のコントロールにフォーカスを設定する
        /// </summary>
        public static void _focusControl(this Form form, string name)
        {
            form._findControl(name)._focus();
        }

        /// <summary>
        /// コントロール ctl がクリックされたときに、 ctl にフォーカスを移す (主にグループボックスでの使用を想定)
        /// </summary>
        public static void _addFocusWhenClickedHandler(this Control ctl)
        {
            if (ctl != null) ctl.MouseClick += (sender, e) => ctl.Focus();
        }

        /// <summary>
        /// フォームを Show() して最前面にもってくる
        /// </summary>
        /// <param name="form"></param>
        public static void _showTopMost(this Form form)
        {
            if (form != null) {
                form.Show();
                form.TopMost = true;
                Helper.WaitMilliSeconds(20);
                form.TopMost = false;
            }
        }

        /// <summary>
        /// コントロール ctl の MouseWheel に与えられたハンドラを追加する
        /// </summary>
        public static void _addMouseWheelHandler(this Control ctl, Action<object, MouseEventArgs> handler)
        {
            ctl.MouseWheel += new MouseEventHandler(handler);
        }

    }

}

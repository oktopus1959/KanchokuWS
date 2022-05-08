using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Utils
{
    /// <summary>
    /// GUI画面の各コントロールの Tex 値の変更を検出する<br/>
    /// Reinitialize()およびCheckStatus()呼び出し時に statusSerializer() を実行し、その値を比較する。
    /// 比較結果(等しくなければ true)を値として controlEnabler() を呼び出す。 
    /// 使い方：<br/>
    /// GuiStatusChecker checker = new GuiStatusChecker();<br/>
    /// checker.CtlToBeVisible = label1;<br/>
    /// checker.Add(textBox1);<br/>
    /// checker.Reinitialize();<br/>
    /// checker.CheckStatus();<br/>
    /// ・・・ (textBox1 に値に変更があると label1 が表示される)<br/>
    /// checker.Dispose(); <br/>
    /// </summary>
    public class GuiStatusChecker : IDisposable
    {
        // ロガー
        private static Logger logger = Logger.GetLogger();

        //-----------------------------------------------------------------------------
        // プロパティ
        //-----------------------------------------------------------------------------
        private string InitialSerializedData { get; set; }

        private string CurrentSerializedData { get; set; }

        public string MyName { get; set; } = "No Name";

        /// <summary> 変更を検出したときに true になるフラグ </summary>
        public bool IsDifferent { get; private set; }

        /// <summary> 変更を検出したときに有効状態にするコントロール </summary>
        public Control CtlToBeEnabled { get; set; }

        /// <summary> 変更を検出したときに可視状態にするコントロール </summary>
        public Control CtlToBeVisible { get; set; }

        /// <summary> 追加されたコントロール以外で変更検出のために必要となる値のシリアライザ </summary>
        public Func<string> StatusSerializer { get; set; }

        /// <summary> 変更検出時に、CtlToBeEnabled に設定した以外のコントロールを有効状態にするアクション。変更有無フラグを引数にとる </summary>
        public Action<bool> ControlEnabler { get; set; }

        //-----------------------------------------------------------------------------
        // メンバー
        //-----------------------------------------------------------------------------
        // チェック対象のコントール
        private List<Control> m_controls = new List<Control>();

        // 子のチェッカー
        private List<GuiStatusChecker> m_checkers = new List<GuiStatusChecker>();

        //-----------------------------------------------------------------------------
        // 内部メソッド
        //-----------------------------------------------------------------------------
        private string serializeControlTexts()
        {
            return m_controls.Select(x => { bool? flag = x._getChecked(); return flag != null ? flag.ToString() : x.Text._toSafe(); })._join("\t");
        }

        private string serializer()
        {
            return (StatusSerializer?.Invoke() ?? "") + "\t" + serializeControlTexts();
        }

        //-----------------------------------------------------------------------------
        // コンストラクタ
        //-----------------------------------------------------------------------------
        public GuiStatusChecker(string name = "No Name")
        {
            MyName = name;
        }

        //-----------------------------------------------------------------------------
        // 破棄
        //-----------------------------------------------------------------------------
        public void Dispose()
        {
        }

        //-----------------------------------------------------------------------------
        // メソッド
        //-----------------------------------------------------------------------------
        /// <summary>
        /// シリアライズデータの再初期化
        /// </summary>
        public void Reinitialize()
        {
            InitialSerializedData = serializer();    // 初期データ
            foreach (var checker in m_checkers) {
                checker.Reinitialize();
            }
        }

        /// <summary>
        /// 状態のチェック
        /// </summary>
        public void CheckStatus()
        {
            CurrentSerializedData = serializer();
            bool flag = CurrentSerializedData._ne(InitialSerializedData);
            IsDifferent = flag;
            ControlEnabler?.Invoke(flag);
            if (CtlToBeEnabled != null) {
                CtlToBeEnabled.Enabled = flag;
                CtlToBeEnabled.ForeColor = flag ? System.Drawing.Color.DarkRed : System.Drawing.Color.Black;
            }
            if (CtlToBeVisible != null) CtlToBeVisible.Visible = flag;

            // check for children
            foreach (var checker in m_checkers) {
                checker.CheckStatus();
            }
        }

        public void Add(Control ctl)
        {
            m_controls.Add(ctl);
        }

        public void Add(GuiStatusChecker checker)
        {
            m_checkers.Add(checker);
        }
    }

}

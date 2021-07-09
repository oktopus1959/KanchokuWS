using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    /// <summary>
    /// 同一スレッドでの再入を防ぎかつ、異なるスレッド間での lock にも使えるオブジェクト<br/>
    /// 使い方：
    /// <code>
    /// private BoolObject m_syncBool = new BoolObject();<br/>
    /// 
    /// private void func() {<br/>
    ///     // すでに誰かが処理中なら、重ねて処理を実行しない<br/>
    ///     if (m_syncBool.BusyCheck()) return true;<br/>
    ///     using (m_syncBool) { // 終わりに Reset() を呼ぶため using で囲む<br/>
    ///         lock (m_syncBool) {<br/>
    ///             // 適当な処理<br/>
    ///         }<br/>
    ///     }<br/>
    /// }
    /// </code>
    /// </summary>
    public class BoolObject : IDisposable
    {
        private bool busyFlag = false;

        /// <summary>
        /// 排他的ビジーチェック。ビジー状態でなければビジー状態にセットし、false を返す。ビジー状態なら true を返す。<para/>
        /// </summary>
        /// <returns></returns>
        public bool BusyCheck()
        {
            bool flag = busyFlag;
            busyFlag = true;
            return flag;
        }

        /// <summary>
        /// Disposer -- ビジー状態を解除する
        /// </summary>
        public void Dispose()
        {
            Reset();
        }

        /// <summary>
        /// ビジー状態を解除する
        /// </summary>
        public void Reset()
        {
            busyFlag = false;
        }
    }

}

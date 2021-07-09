using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    /// <summary>
    /// 使い方：
    /// <code>
    /// private SyncBool m_syncBool = new SyncBool();<para/>
    /// 
    /// private void func() {<para/>
    ///     // すでに誰かが処理中なら、重ねて処理を実行しない<para/>
    ///     if (m_syncBool.BusyCheck()) return true;<para/>
    ///     using (m_syncBool) { // 終わりに Reset() を呼ぶため using で囲む<para/>
    ///         // 適当な処理<para/>
    ///     }<para/>
    /// }
    /// </code>
    /// </summary>
    public class SyncBool : IDisposable
    {
        private object syncObj = new object();
        private bool busyFlag = false;

        /// <summary>
        /// 排他的ビジーチェック。ビジー状態でなければビジー状態にセットし、false を返す。ビジー状態なら true を返す。<para/>
        /// </summary>
        /// <returns></returns>
        public bool BusyCheck()
        {
            lock (syncObj) {
                bool flag = busyFlag;
                busyFlag = true;
                return flag;
            }
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

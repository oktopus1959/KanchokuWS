using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanchokuWS.SimultaneousKeyStroke.DeterminerLib
{
    /// <summary>
    /// 出力文字列が未確定の打鍵列を時系列で保持するクラス
    /// </summary>
    class StrokeQueue
    {
        private Queue<Stroke> strokeQueue = new Queue<Stroke>();

        /// <summary>
        /// キューに打鍵を追加する
        /// </summary>
        /// <param name="s"></param>
        public void Push(Stroke s)
        {
            strokeQueue.Enqueue(s);
        }

        /// <summary>
        /// キューの先頭打鍵を削除する
        /// </summary>
        public void Pop()
        {

        }
    }
}

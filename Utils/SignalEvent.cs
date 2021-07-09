using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    public class SignalEvent : IDisposable
    {
        private static Logger logger = Logger.GetLogger();

        // 他のプロセスのイベントと名前を異なるものにするための uuid
        private string m_name = Guid.NewGuid().ToString();

        private EventWaitHandle m_event = null;

        private bool bWaiting = false;

        private bool bSignaled = false;

        public SignalEvent()
        {
            m_event = new EventWaitHandle(false, EventResetMode.AutoReset, m_name);
        }

        ~SignalEvent()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) {
                if (m_event != null) {
                    m_event.Close();
                    m_event = null;
                }
            }
        }

        public bool WaitSignal(int timeoutMs = Timeout.Infinite)
        {
            logger.Trace(() => $"ENTER: name={m_name}");
            lock (m_event) {
                if (bSignaled) {
                    ClearSignal();
                    logger.Trace(() => $"SIGNALED: name={m_name}");
                    return true;
                }
                bWaiting = true;
                // Waiting 状態になってから実際に event.WaitOne()を発行するまでの間に何度も m_event.Set() を呼ばれると
                // 2回目以降の Set は取りこぼすが、まあとりあえずその可能性は無視
            }
            logger.Trace(() => $"WAITING: name={m_name}");
            bool result = m_event.WaitOne(timeoutMs);
            ClearSignal();
            logger.Trace(() => $"FIRED: name={m_name}, result={result}");
            return result;
        }

        /// <summary>
        /// シグナルのクリア
        /// </summary>
        public void ClearSignal()
        {
            m_event.Reset();
            bSignaled = false;
            bWaiting = false;
        }

        /// <summary>
        /// シグナルを発火させる<para/>
        /// イベントが見つかるまで、 retrySpan間隔で retryCount だけループする。デフォルトは 100ms間隔で300回(30秒)。
        /// </summary>
        /// <param name="name"></param>
        public bool FireSignal(int retryCount = 300, int retrySpan = 100)
        {
            logger.Trace(() => $"ENTER: name={m_name}");
            lock (m_event) {
                if (!bWaiting) {
                    bSignaled = true;
                    logger.Trace(() => $"SIGNALED: name={m_name}");
                    return true;
                }
            }
            // 一応、指定の名前を持つ event が存在することを確認してから発火させる
            while (retryCount > 0) {
                try {
                    logger.Trace(() => $"TRY: name={m_name}");
                    using (var ewh = EventWaitHandle.OpenExisting(m_name)) {
                        ewh.Set();
                    }
                    logger.Trace(() => $"FIRED: name={m_name}");
                    return true;
                } catch (WaitHandleCannotBeOpenedException) {
                    --retryCount;
                    Task.Delay(retrySpan).Wait();
                }
            }
            logger.Trace(() => $"FAILED: name={m_name}");
            return false;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;

using KanchokuWS.Gui;
using Utils;

namespace KanchokuWS.Handler
{
    public class ActiveWindowHandler
    {
        private static Logger logger = Logger.GetLogger();

        /// <summary> アクティブ(フォーカスを持つ)ウィンドウの ClassName </summary>
        public string ActiveWinClassName { get; private set; } = "";

        /// <summary> アクティブ(フォーカスを持つ)ウィンドウのハンドル </summary>
        public IntPtr ActiveWinHandle { get; private set; } = IntPtr.Zero;

        /// <summary> アクティブ(フォーカスを持つ)ウィンドウのカレット位置 </summary>
        private Rectangle activeWinCaretPos;

        public Rectangle ActiveWinCaretPos => activeWinCaretPos;

        /// <summary> シングルトンオブジェクト </summary>
        public static ActiveWindowHandler Singleton { get; private set; }

        public static ActiveWindowHandler CreateSingleton()
        {
            Singleton = new ActiveWindowHandler();
            return Singleton;
        }

        //private bool bDisposed = false;

        public static void DisposeSingleton()
        {
            logger.Info("Disposed");
        }

        /// <summary>
        /// private コンストラクタ
        /// </summary>
        private ActiveWindowHandler()
        {
        }


        // cf. http://pgcenter.web.fc2.com/contents/csharp_sendinput.html
        // cf. https://www.pinvoke.net/default.aspx/user32.sendinput

        //-----------------------------------------------------------------------------------------------------
        //private SyncBool syncBS = new SyncBool();

        //// cf. https://stackoverflow.com/questions/4372055/detect-active-window-changed-using-c-sharp-without-polling
        //// cf. https://stackoverflow.com/questions/6548470/getfocus-win32api-help
        //delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        //WinEventDelegate dele = null;

        //[DllImport("user32.dll")]
        //private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        //private const uint WINEVENT_OUTOFCONTEXT = 0;
        //private const uint EVENT_SYSTEM_FOREGROUND = 3;

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, int ProcessId);

        //[DllImport("user32.dll")]
        //static extern IntPtr GetForegroundWindow();

        //[DllImport("user32.dll")]
        //private static extern IntPtr GetFocus();

        //[DllImport("user32.dll")]
        //private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern bool GetCaretPos(out Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        //[DllImport("user32.dll")]
        //private static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        GUIThreadInfo guiThreadInfo = new GUIThreadInfo();

        /// <summary>
        /// アクティブウィンドウハンドルの取得
        /// </summary>
        private void GetActiveWindowHandle(bool bLog)
        {
            if (bLog) logger.Info("ENTER");

            IntPtr fgHan = IntPtr.Zero;
            IntPtr focusHan = IntPtr.Zero;

            // すぐにハンドルを取りに行くと失敗することがある。
            // Focus を持つウィンドウハンドルが 0 以外になるまで、N msecのwaitを入れながら最大3回試行する
            for (int count = 0; count < 3; ++count) {
                // とりあえず20msec待つ
                //Task.Delay(20).Wait();
                Helper.WaitMilliSeconds(20);    // UIスレッドのタイマを使用するようにしたせいか、単なる Task.Delayだと処理が固まることがある気がする

                // 最前面ウィンドウの情報を取得
                guiThreadInfo.GetForegroundWinInfo();

                fgHan = guiThreadInfo.hwndForeground;
                ActiveWinClassName = guiThreadInfo.className;
                focusHan = guiThreadInfo.hwndFocus;
                if (bLog) logger.Info(() => $"fgHan={(int)fgHan:x}H, focusHan={(int)focusHan:x}H");

                if (Settings.IsFixedPosWinClass(ActiveWinClassName)) {
                    // 固定位置に移動するウィンドウクラス
                    activeWinCaretPos.X = Math.Abs(Settings.VirtualKeyboardFixedPosX);
                    activeWinCaretPos.Y = Math.Abs(Settings.VirtualKeyboardFixedPosY);
                    activeWinCaretPos.Width = 2;
                    activeWinCaretPos.Height = 10;
                    break;
                } else {
                    // カレットのスクリーン座標を取得
                    if (guiThreadInfo.GetScreenCaretPos(ref activeWinCaretPos)) {
                        //getScreenCaretPosByOriginalWay(fgHan, ref ActiveWinCaretPos, bLog);   // やっぱりこのやり方だとうまく取れない場合あり
                        if (bLog) logger.Info(() => $"WndClass={ActiveWinClassName}: focus caret=({activeWinCaretPos.X}, {activeWinCaretPos.Y}, {activeWinCaretPos.Width}, {activeWinCaretPos.Height})");

                        if (focusHan != IntPtr.Zero) {
                            // OK
                            break;
                        }
                        // CMD Prompt の場合は Focus が取れないっぽい?
                    }
                    if (bLog || Logger.IsInfoEnabled) logger.Warn($"RETRY: count={count + 1}, WndClass={ActiveWinClassName}");
                }
            }

            if (focusHan == IntPtr.Zero) {
                if (bLog || Logger.IsInfoEnabled) logger.Warn($"Can't get window handle with focus: WndClass={ActiveWinClassName}");
            }
            ActiveWinHandle = (focusHan == IntPtr.Zero) ? fgHan : focusHan;

            if (bLog) logger.Info(() => $"LEAVE: ActiveWinHandle={(int)ActiveWinHandle:x}H");
        }

        // 従来のやり方でカレットのスクリーン座標を取得(やはりうまくいかない)
        private void getScreenCaretPosByOriginalWay(IntPtr fgHan, ref Rectangle rect, bool bLog = false)
        {
            uint targetThread = GetWindowThreadProcessId(fgHan, 0);
            uint selfThread = GetCurrentThreadId();

            //AttachTrheadInput is needed so we can get the handle of a focused window in another app
            AttachThreadInput(selfThread, targetThread, true);

            ////Get the handle of a focused window
            //focusHan = GetFocus();
            //if (bLog) logger.Debug(() => $"focusHan={(int)focusHan:x}");

            Point caretPos;
            GetCaretPos(out caretPos);
            ClientToScreen(fgHan, ref caretPos);
            if (bLog) logger.Info(() => $"focus caret pos=({caretPos.X}, {caretPos.Y})");

            //Now detach since we got the focused handle
            AttachThreadInput(selfThread, targetThread, false);

            rect.X = caretPos.X;
            rect.Y = caretPos.Y;
            rect.Width = 2;
            rect.Height = 20;
        }

        public static string GetWindowClassName(IntPtr hwnd)
        {
            const int nChars = 1024;
            StringBuilder Buff = new StringBuilder(nChars);
            GetClassName(hwnd, Buff, nChars);
            return Buff.ToString();
        }

        private DateTime prevLogDt1;

        public void LoggingCaretInfo(Settings.WindowsClassSettings settings)
        {
            if (prevLogDt1.AddSeconds(3) < DateTime.Now) {
                prevLogDt1 = DateTime.Now;
                var caretMargin = settings?.ValidCaretMargin;
                if (caretMargin != null) {
                    GUIThreadInfo.RECT rect;
                    guiThreadInfo.GetForegroundWindowRect(out rect);
                    logger.Info($"caretPos=(X:{activeWinCaretPos.X}, Y:{activeWinCaretPos.Y}), " +
                        $"validCaretMargin=({caretMargin.Select(m => m.ToString())._join(",")}), " +
                        $"WinRect=(L:{rect.iLeft}, T:{rect.iTop}, R:{rect.iRight}, B:{rect.iBottom}), " +
                        $"validWinRect=(L:{rect.iLeft + caretMargin._getNth(2)}, " +
                        $"T:{rect.iTop + caretMargin._getNth(0)}, " +
                        $"R:{rect.iRight - caretMargin._getNth(3)}, " +
                        $"B:{rect.iBottom - caretMargin._getNth(1)})");
                }
                var caretOffset = settings?.CaretOffset;
                if (caretOffset != null) {
                    logger.Info($"caretOffset=({caretOffset.Select(m => m.ToString())._join(",")})");
                }
                var vkbFixedPos = settings?.VkbFixedPos;
                if (vkbFixedPos != null) {
                    logger.Info($"vkbFixedPos=({vkbFixedPos.Select(m => m.ToString())._join(",")})");
                }
            }
        }

        /// <summary> 指定されたマージンの内側にカレットがあるか</summary>
        /// <returns></returns>
        public bool IsInValidCaretMargin(Settings.WindowsClassSettings settings)
        {
            var caretMargin = settings?.ValidCaretMargin;
            if (caretMargin == null) return true;

            GUIThreadInfo.RECT rect;
            guiThreadInfo.GetForegroundWindowRect(out rect);
            return rect.iTop + caretMargin._getNth(0) <= activeWinCaretPos.Y
                && activeWinCaretPos.Y <= rect.iBottom - caretMargin._getNth(1)
                && rect.iLeft + caretMargin._getNth(2) <= activeWinCaretPos.X
                && activeWinCaretPos.X <= rect.iRight - caretMargin._getNth(3);
        }

        // 同じスレッドで再入するのを防ぐ
        private BoolObject syncObj = new BoolObject();

        private DateTime lastBusyDt;

        private int busyCount = 0;

        public enum MoveWinType
        {
            Freeze = 0,
            MoveIfAny = 1,
            MoveMandatory = 2,
        }

        public bool IsVkbWinActive { get; private set; } = false;

        public void GetActiveWindowInfo(Action<bool, bool, bool> actionMoveWindow, FrmVirtualKeyboard frmVkb)
        {
            getActiveWindowInfo(actionMoveWindow, frmVkb, MoveWinType.MoveIfAny, Settings.LoggingActiveWindowInfo /*&& Logger.IsDebugEnabled*/);
        }

        private void getActiveWindowInfo(Action<bool, bool, bool> actionMoveWindow, FrmVirtualKeyboard frmVkb, MoveWinType moveWin, bool bLog = false)
        {
            GUIThreadInfo.SetLogFlag(bLog);

            if (bLog) logger.Info($"ENTER: moveWin={moveWin}");

            // 異スレッドおよび同一スレッドでの再入を防ぐ
            lock (syncObj) {
                if (syncObj.BusyCheck()) {
                    //logger.Warn("LEAVE: In Progress");
                    // ビジーカウントをインクリメント
                    ++busyCount;
                    if (lastBusyDt._notValid()) {
                        // 初回のビジー
                        lastBusyDt = DateTime.Now;
                    } else if (DateTime.Now >= lastBusyDt.AddSeconds(5)) {
                        // 前回ビジーから5秒経過したら、busyCount をビジー時刻をクリア
                        lastBusyDt = DateTime.Now;
                        if (busyCount >= 5) {
                            // この5秒間にビジーが5回以上あったら、busyFlag をクリアする。
                            // この間、微妙なタイマー割り込みでbusyFlagがONのままになって、ビジーを繰り返している可能性もあるので。
                            logger.Warn("RESET: Busy Flag");
                            syncObj.Reset();
                        }
                        busyCount = 0;
                    }
                    if (Logger.IsInfoEnabled && frmVkb != null && !frmVkb.IsMyWinClassName(ActiveWinClassName)) {
                        logger.InfoH("LEAVE: In Progress");
                    }
                    return;
                }
            }

            bool bOK = false;
            bool bDiffWin = false;
            using (syncObj) {   // 抜けたときにビジー状態が解除される
                try {
                    string prevClassName = ActiveWinClassName;
                    GetActiveWindowHandle(bLog);
                    bOK = true;
                    bDiffWin = ActiveWinClassName._ne(prevClassName);
                    IsVkbWinActive = frmVkb.IsMyWinClassName(ActiveWinClassName);
                    if (bDiffWin && frmVkb != null && !IsVkbWinActive) {
                        // 直前のものとクラス名が異なっていれば、それを仮想鍵盤上部に表示する (ただし、仮想鍵盤自身を除く)
                        frmVkb.SetTopText(ActiveWinClassName);
                    }
                } catch (Exception e) {
                    logger.Warn(e.Message);
                    if (bLog) logger.InfoH(e.StackTrace);
                }
            }
            if (bOK && moveWin != MoveWinType.Freeze) {
                // 強制移動でない場合は、頻繁に移動しないように、最後のキー出力が終わってNms経過したらウィンドウを移動する
                bool bMandatory = moveWin == MoveWinType.MoveMandatory;
                if (bMandatory || DateTime.Now >= (SendInputHandler.Singleton?.LastOutputDt.AddMilliseconds(Settings.VirtualKeyboardMoveGuardMillisec) ?? DateTime.MaxValue))
                    actionMoveWindow?.Invoke(bDiffWin, bMandatory, bLog);
            }
            if (bLog) logger.Info(() => $"LEAVE: ActiveWinClassName={ActiveWinClassName}");
        }

    }

}

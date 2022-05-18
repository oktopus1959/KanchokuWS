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

        public FrmKanchoku FrmMain { get; set; }
        public FrmVirtualKeyboard FrmVkb { get; set; }
        public FrmModeMarker FrmMode { get; set; }

        /// <summary> アクティブ(フォーカスを持つ)ウィンドウの ClassName </summary>
        public string ActiveWinClassName { get; private set; } = "";

        /// <summary> アクティブ(フォーカスを持つ)ウィンドウのハンドル </summary>
        public IntPtr ActiveWinHandle { get; private set; } = IntPtr.Zero;

        /// <summary> アクティブ(フォーカスを持つ)ウィンドウのカレット位置 </summary>
        private Rectangle ActiveWinCaretPos;

        /// <summary> アクティブ(フォーカスを持つ)ウィンドウの固有の設定 </summary>
        private Settings.WindowsClassSettings ActiveWinSettings;

        /// <summary> 仮想鍵盤ウィンドウの ClassName の末尾のハッシュ部分 </summary>
        private string DlgVkbClassNameHash;

        /// <summary> シングルトンオブジェクト </summary>
        public static ActiveWindowHandler Singleton { get; private set; }

        public static ActiveWindowHandler CreateSingleton(FrmKanchoku frmMain, FrmVirtualKeyboard frmVkb, FrmModeMarker frmMode)
        {
            Singleton = new ActiveWindowHandler(frmMain, frmVkb, frmMode);
            return Singleton;
        }

        //private bool bDisposed = false;

        public static void DisposeSingleton()
        {
            logger.Info("Disposed");
        }

        private ActiveWindowHandler(FrmKanchoku frmMain, FrmVirtualKeyboard frmVkb, FrmModeMarker frmMode)
        {
            FrmMain = frmMain;
            FrmVkb = frmVkb;
            FrmMode = frmMode;
            DlgVkbClassNameHash = getWindowClassName(FrmVkb.Handle)._safeSubstring(-16);
            logger.Info(() => $"Vkb ClassName Hash={DlgVkbClassNameHash}");
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

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

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

                // カレットのスクリーン座標を取得
                guiThreadInfo.GetScreenCaretPos(ref ActiveWinCaretPos);
                //getScreenCaretPosByOriginalWay(fgHan, ref ActiveWinCaretPos, bLog);   // やっぱりこのやり方だとうまく取れない場合あり
                if (bLog) logger.Info(() => $"WndClass={ActiveWinClassName}: focus caret pos=({ActiveWinCaretPos.X}, {ActiveWinCaretPos.Y})");

                if (focusHan != IntPtr.Zero || ActiveWinClassName._equalsTo("ConsoleWindowClass")) break;  // CMD Prompt の場合は Focus が取れないっぽい?
                if (bLog || Logger.IsInfoEnabled) logger.Warn($"RETRY: count={count + 1}");
            }

            if (focusHan == IntPtr.Zero) {
                if (bLog || Logger.IsInfoEnabled) logger.Warn("Can't get window handle with focus");
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

        private string getWindowClassName(IntPtr hwnd)
        {
            const int nChars = 1024;
            StringBuilder Buff = new StringBuilder(nChars);
            GetClassName(hwnd, Buff, nChars);
            return Buff.ToString();
        }

        private Rectangle prevCaretPos;

        /// <summary> ウィンドウを移動さ出ない微少変動量 </summary>
        private const int NoMoveOffset = 10;

        private DateTime prevLogDt1;

        private void loggingCaretInfo()
        {
            if (prevLogDt1.AddSeconds(3) < DateTime.Now) {
                prevLogDt1 = DateTime.Now;
                var caretMargin = ActiveWinSettings?.ValidCaretMargin;
                if (caretMargin != null) {
                    GUIThreadInfo.RECT rect;
                    guiThreadInfo.GetForegroundWindowRect(out rect);
                    logger.Info($"caretPos=(X:{ActiveWinCaretPos.X}, Y:{ActiveWinCaretPos.Y}), " +
                        $"validCaretMargin=({caretMargin.Select(m => m.ToString())._join(",")}), " +
                        $"WinRect=(L:{rect.iLeft}, T:{rect.iTop}, R:{rect.iRight}, B:{rect.iBottom}), " +
                        $"validWinRect=(L:{rect.iLeft + caretMargin._getNth(2)}, " +
                        $"T:{rect.iTop + caretMargin._getNth(0)}, " +
                        $"R:{rect.iRight - caretMargin._getNth(3)}, " +
                        $"B:{rect.iBottom - caretMargin._getNth(1)})");
                }
                var caretOffset = ActiveWinSettings?.CaretOffset;
                if (caretOffset != null) {
                    logger.Info($"caretOffset=({caretOffset.Select(m => m.ToString())._join(",")})");
                }
                var vkbFixedPos = ActiveWinSettings?.VkbFixedPos;
                if (vkbFixedPos != null) {
                    logger.Info($"vkbFixedPos=({vkbFixedPos.Select(m => m.ToString())._join(",")})");
                }
            }
        }

        /// <summary> 指定されたマージンの内側にカレットがあるか</summary>
        /// <returns></returns>
        private bool isInValidCaretMargin(Settings.WindowsClassSettings settings)
        {
            var caretMargin = settings?.ValidCaretMargin;
            if (caretMargin == null) return true;

            GUIThreadInfo.RECT rect;
            guiThreadInfo.GetForegroundWindowRect(out rect);
            return rect.iTop + caretMargin._getNth(0) <= ActiveWinCaretPos.Y
                && ActiveWinCaretPos.Y <= rect.iBottom - caretMargin._getNth(1)
                && rect.iLeft + caretMargin._getNth(2) <= ActiveWinCaretPos.X
                && ActiveWinCaretPos.X <= rect.iRight - caretMargin._getNth(3);
        }

        /// <summary>
        /// 仮想鍵盤をカレットの近くに移動する<br/>
        /// </summary>
        public void MoveWindow()
        {
            moveWindow(false, true, true);
        }

        private bool bFirstMove = true;

        /// <summary>
        /// 仮想鍵盤をカレットの近くに移動する (仮想鍵盤自身がアクティブの場合は移動しない)<br/>
        /// これが呼ばれるのはデコーダがONのときだけ
        /// </summary>
        private void moveWindow(bool bDiffWin, bool bMoveMandatory, bool bLog)
        {
            ActiveWinSettings = Settings.GetWinClassSettings(ActiveWinClassName);
            if (bLog || bFirstMove) {
                logger.Info($"CALLED: diffWin={bDiffWin}, mandatory={bMoveMandatory}, firstMove={bFirstMove}");
                loggingCaretInfo();
            }

            if (Settings.VirtualKeyboardPosFixedTemporarily) return;    // 一時的に固定されている

            if (bFirstMove || (!FrmMain.IsVirtualKeyboardFreezed && !ActiveWinClassName.EndsWith(DlgVkbClassNameHash) && ActiveWinClassName._ne("SysShadow"))) {
                if (bFirstMove || bMoveMandatory ||
                    ((Math.Abs(ActiveWinCaretPos.X) >= NoMoveOffset || Math.Abs(ActiveWinCaretPos.Y) >= NoMoveOffset) &&
                     (Math.Abs(ActiveWinCaretPos.X - prevCaretPos.X) >= NoMoveOffset || Math.Abs(ActiveWinCaretPos.Y - prevCaretPos.Y) >= NoMoveOffset) &&
                     isInValidCaretMargin(ActiveWinSettings))
                   ) {
                    int xOffset = (ActiveWinSettings?.CaretOffset)._getNth(0, Settings.VirtualKeyboardOffsetX);
                    int yOffset = (ActiveWinSettings?.CaretOffset)._getNth(1, Settings.VirtualKeyboardOffsetY);
                    int xFixed = (ActiveWinSettings?.VkbFixedPos)._getNth(0, -1)._geZeroOr(Settings.VirtualKeyboardFixedPosX);
                    int yFixed = (ActiveWinSettings?.VkbFixedPos)._getNth(1, -1)._geZeroOr(Settings.VirtualKeyboardFixedPosY);
                    //double dpiRatio = 1.0; //FrmVkb.GetDeviceDpiRatio();
                    if (bLog || bFirstMove) logger.Info($"CaretPos.X={ActiveWinCaretPos.X}, CaretPos.Y={ActiveWinCaretPos.Y}, xOffset={xOffset}, yOffset={yOffset}, xFixed={xFixed}, yFixed={yFixed}");
                    if (ActiveWinCaretPos.X >= 0) {
                        int cX = ActiveWinCaretPos.X;
                        int cY = ActiveWinCaretPos.Y;
                        int cW = ActiveWinCaretPos.Width;
                        int cH = ActiveWinCaretPos.Height;
                        if (bLog) {
                            logger.Info($"MOVE: X={cX}, Y={cY}, W={cW}, H={cH}, OX={xOffset}, OY={yOffset}");
                            if (Settings.LoggingActiveWindowInfo) {
                                var dpis = ScreenInfo.ScreenDpi.Select(x => $"{x}")._join(", ");
                                FrmVkb.SetTopText($"DR={dpis}, CX={cX},CY={cY},CW={cW},CH={cH},OX={xOffset},OY={yOffset}");
                            }
                        }
                        Action<Form> moveAction = (Form frm) => {
                            int fX = 0;
                            int fY = 0;
                            int fW = frm.Size.Width;
                            int fH = frm.Size.Height;
                            if (xFixed >= 0 && yFixed >= 0) {
                                fX = xFixed;
                                fY = yFixed;
                            } else {
                                fX = cX + (xOffset >= 0 ? cW : -fW) + xOffset ;
                                if (fX < 0) fX = cX + cW + Math.Abs(xOffset);
                                fY = cY + (yOffset >= 0 ? cH : -fH) + yOffset;
                                if (fY < 0) fY = cY + cH + Math.Abs(yOffset);
                                int fRight = fX + fW;
                                int fBottom = fY + fH;
                                Rectangle rect = ScreenInfo.GetScreenContaining(cX, cY);
                                if (fRight >= rect.X + rect.Width) fX = cX - fW - Math.Abs(xOffset);
                                if (fBottom >= rect.Y + rect.Height) fY = cY - fH - Math.Abs(yOffset);
                            }
                            MoveWindow(frm.Handle, fX, fY, fW, fH, true);
                        };
                        // 仮想鍵盤の移動
                        moveAction(FrmVkb);

                        // 入力モード標識の移動
                        moveAction(FrmMode);
                        if (bDiffWin && !FrmMain.IsVkbShown) {
                            // 異なるウィンドウに移動したら入力モード標識を表示する
                            FrmMode.ShowImmediately();
                        }
                        prevCaretPos = ActiveWinCaretPos;
                    }
                }
                bFirstMove = false;
            } else {
                logger.Debug(() => $"ActiveWinClassName={ActiveWinClassName}, VkbClassName={DlgVkbClassNameHash}");
            }
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

        public void GetActiveWindowInfo()
        {
            GetActiveWindowInfo(MoveWinType.MoveIfAny, Settings.LoggingActiveWindowInfo /*&& Logger.IsDebugEnabled*/);
        }

        public void GetActiveWindowInfo(MoveWinType moveWin, bool bLog = false)
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
                    if (Logger.IsInfoEnabled && !ActiveWinClassName._endsWith(DlgVkbClassNameHash)) {
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
                    if (bDiffWin && !ActiveWinClassName._endsWith(DlgVkbClassNameHash)) {
                        // 直前のものとクラス名が異なっていれば、それを仮想鍵盤上部に表示する (ただし、仮想鍵盤自身を除く)
                        FrmVkb.SetTopText(ActiveWinClassName);
                    }
                } catch (Exception e) {
                    logger.Error($"{e.Message}\n{e.StackTrace}");
                }
            }
            if (bOK && moveWin != MoveWinType.Freeze) {
                // 強制移動でない場合は、頻繁に移動しないように、最後のキー出力が終わってNms経過したらウィンドウを移動する
                bool bMandatory = moveWin == MoveWinType.MoveMandatory;
                if (bMandatory || DateTime.Now >= (SendInputHandler.Singleton?.LastOutputDt.AddMilliseconds(Settings.VirtualKeyboardMoveGuardMillisec) ?? DateTime.MaxValue))
                    moveWindow(bDiffWin, bMandatory, bLog);
            }
            if (bLog) logger.Info(() => $"LEAVE: ActiveWinClassName={ActiveWinClassName}");
        }

    }

}

﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KanchokuWS
{
    /// <summary>
    /// グローバルキーフッククラス
    /// cf. https://aonasuzutsuki.hatenablog.jp/entry/2018/10/15/170958 
    /// </summary>
    public static class KeyboardHook
    {
        private const int WH_KEYBOARD_LL = 0x000D;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        #region Win32API Structures
        [StructLayout(LayoutKind.Sequential)]
        public class KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public KBDLLHOOKSTRUCTFlags flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [Flags]
        public enum KBDLLHOOKSTRUCTFlags : uint
        {
            KEYEVENTF_EXTENDEDKEY = 0x0001,
            KEYEVENTF_KEYUP = 0x0002,
            KEYEVENTF_SCANCODE = 0x0008,
            KEYEVENTF_UNICODE = 0x0004,
        }
        #endregion

        #region Win32 Methods
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion

        #region Delegate
        private delegate IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        #endregion

        #region Fields
        private static KeyboardProc keyboardProc;
        private static IntPtr hookId = IntPtr.Zero;
        #endregion

        public static Func<int, int, bool> OnKeyDownEvent { get; set; }

        public static Func<int, int, bool> OnKeyUpEvent { get; set; }

        public static void Hook()
        {
            if (hookId == IntPtr.Zero) {
                keyboardProc = HookProcedure;
                using (var curProcess = Process.GetCurrentProcess()) {
                    using (ProcessModule curModule = curProcess.MainModule) {
                        hookId = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, GetModuleHandle(curModule.ModuleName), 0);
                    }
                }
            }
        }

        public static void UnHook()
        {
            UnhookWindowsHookEx(hookId);
            hookId = IntPtr.Zero;
        }

        public class OriginalKeyEventArg : EventArgs
        {
            public int KeyCode { get; }

            public int ExtraInfo { get; }

            public OriginalKeyEventArg(int keyCode, int extraInfo)
            {
                KeyCode = keyCode;
                ExtraInfo = extraInfo;
            }
        }

        public static IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)) {
                var kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                var vkCode = (int)kb.vkCode;
                if (!(OnKeyDownEvent?.Invoke(vkCode, (int)kb.dwExtraInfo) ?? false)) return IntPtr.Zero;
            } else if (nCode >= 0 && (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)) {
                var kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                var vkCode = (int)kb.vkCode;
                if (!(OnKeyUpEvent?.Invoke(vkCode, (int)kb.dwExtraInfo) ?? false)) return IntPtr.Zero;
            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }
    }
}

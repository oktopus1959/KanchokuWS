using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Utils;

namespace KanchokuWS
{
    public class KeyboardEventDispatcher : IDisposable
    {
        private static Logger logger = Logger.GetLogger();

        /// <summary>Ctrlキー変換の有効なウィンドウクラスか</summary>
        public delegate bool DelegateCtrlConversionEffectiveChecker();

        /// <summary>キーイベント</summary>
        public delegate bool DelegateOnKeyEvent(int vkey, int extraInfo);

        /// <summary>デコーダ ON/OFF </summary>
        public delegate void DelegateToggleDecoder();

        /// <summary>デコーダ ON </summary>
        public delegate void DelegateActivateDecoder();

        /// <summary>デコーダ OFF </summary>
        public delegate void DelegateDeactivateDecoder();

        /// <summary>デコーダが ON か</summary>
        public delegate bool DelegateIsDecoderActivated();

        /// <summary>デコーダが第2打鍵以降待ちか </summary>
        public delegate bool DelegateIsDecoderWaitingFirstStroke();

        /// <summary>SandS状態を一時的なシフト状態にする</summary>
        public delegate void DelegateSetSandSShiftedOneshot();

        /// <summary>デコーダ機能のディスパッチ</summary>
        public delegate bool DelegateDecoderFuncDispatcher(int deckey, uint mod);

        /// <summary>修飾キー付きvkeyをSendInputする</summary>
        public delegate bool DelegateSendInputVkeyWithMod(uint mod, uint vkey);

        /// <summary>無条件にデコーダを呼び出す</summary>
        public delegate bool DelegateInvokeDecoderUnconditionally(int deckey, uint mod);

        ///// <summary>打鍵ヘルプのローテーション<br/>ローテーションを行わない場合は false を返す</summary>
        //public delegate bool DelegateRotateStrokeHelp();

        ///// <summary>打鍵ヘルプの逆ローテーション<br/>ローテーションを行わない場合は false を返す</summary>
        //public delegate bool DelegateRotateReverseStrokeHelp();

        ///// <summary>日付出力のローテーション<br/>日付出力を行わない場合は false を返す</summary>
        //public delegate bool DelegateRotateDateString();

        ///// <summary>日付出力の逆ローテーション<br/>日付出力を行わない場合は false を返す</summary>
        //public delegate bool DelegateRotateReverseDateString();

        ///// <summary>デコーダキーに変換してデコーダを呼び出す<br/>デコーダ呼び出しを行わない場合は false を返す</summary>
        //public delegate bool DelegateInvokeDecoder(int decKey);

        ///// <summary>Ctrlキー変換の有効なウィンドウクラスか</summary>
        //public DelegateCtrlConversionEffectiveChecker CtrlConversionEffectiveChecker { get; set; }

        /// <summary>キーダウン</summary>
        public DelegateOnKeyEvent OnKeyDown { get; set; }

        /// <summary>キーアップ</summary>
        public DelegateOnKeyEvent OnKeyUp { get; set; }

        /// <summary>デコーダ ON/OFF </summary>
        public DelegateToggleDecoder ToggleDecoder { get; set; }

        /// <summary>デコーダ ON </summary>
        public DelegateActivateDecoder ActivateDecoder { get; set; }

        /// <summary>デコーダ OFF </summary>
        public DelegateDeactivateDecoder DeactivateDecoder { get; set; }

        /// <summary>デコーダが ON か</summary>
        public DelegateIsDecoderActivated IsDecoderActivated { get; set; }

        private bool isDecoderActivated()
        {
            return IsDecoderActivated?.Invoke() ?? false;
        }

        /// <summary>デコーダが第1打鍵待ちか </summary>
        public DelegateIsDecoderWaitingFirstStroke IsDecoderWaitingFirstStroke { get; set; }

        /// <summary>SandS状態を一時的なシフト状態にする</summary>
        public DelegateSetSandSShiftedOneshot SetSandSShiftedOneshot { get; set; }

        /// <summary>デコーダ機能のディスパッチ</summary>
        public DelegateDecoderFuncDispatcher FuncDispatcher { get; set; }

        /// <summary>修飾キー付きvkeyをSendInputする</summary>
        public DelegateSendInputVkeyWithMod SendInputVkeyWithMod { get; set; }

        /// <summary>無条件にデコーダを呼び出す</summary>
        public DelegateInvokeDecoderUnconditionally InvokeDecoderUnconditionally { get; set; }

        ///// <summary>打鍵ヘルプのローテーション<br/>ローテーションを行わない場合は false を返す</summary>
        //public DelegateRotateStrokeHelp RotateStrokeHelp { get; set; }

        ///// <summary>打鍵ヘルプの逆ローテーション<br/>ローテーションを行わない場合は false を返す</summary>
        //public DelegateRotateReverseStrokeHelp RotateReverseStrokeHelp { get; set; }

        ///// <summary>日付出力のローテーション<br/>日付出力を行わない場合は false を返す</summary>
        //public DelegateRotateDateString RotateDateString { get; set; }

        ///// <summary>日付出力の逆ローテーション<br/>日付出力を行わない場合は false を返す</summary>
        //public DelegateRotateReverseDateString RotateReverseDateString { get; set; }

        ///// <summary>デコーダキーに変換してデコーダを呼び出す<br/>デコーダ呼び出しを行わない場合は false を返す</summary>
        //public DelegateInvokeDecoder InvokeDecoder { get; set; }

        private bool bHooked = false;

        /// <summary>コンストラクタ</summary>
        public KeyboardEventDispatcher()
        {
            //clearKeyCodeTable();
        }

        public void InstallKeyboardHook()
        {
            logger.InfoH($"ENTER");
            KeyboardHook.OnKeyDownEvent = onKeyboardDownHandler;
            KeyboardHook.OnKeyUpEvent = onKeyboardUpHandler;
            KeyboardHook.Hook();
            bHooked = true;
            logger.InfoH($"LEAVE");
        }

        public void ReleaseKeyboardHook()
        {
            logger.InfoH($"ENTER");
            if (bHooked) {
                bHooked = false;
                KeyboardHook.UnHook();
                logger.InfoH($"UNHOOKED");
            }
            logger.InfoH($"LEAVE");
        }

        public void Dispose()
        {
            ReleaseKeyboardHook();
        }

        //----------------------------------------------------------------------------------------------------------
        [DllImport("user32.dll")]
        private static extern ushort GetAsyncKeyState(uint vkey);

        private const int vkeyNum = 256;

        /// <summary>やまぶきRが送ってくる SendInput の scanCode </summary>
        private const int YamabukiRscanCode = 0x7f;

        /// <summary> 漢直として有効なキーか</summary>
        private bool isEffectiveVkey(int vkey, int scanCode, int extraInfo, bool ctrl)
        {
            // 0xa0 = LSHIFT, 0xa1 = RSHIFT, 0xa5 = RMENU, 0xf3 = Zenkaku, 0xf4 = Kanji
            return
                (Settings.IgnoreOtherHooker ? extraInfo == 0 : extraInfo != ActiveWindowHandler.MyMagicNumber) &&
                scanCode != 0 && scanCode != YamabukiRscanCode &&
                ((vkey >= 0 && vkey < 0xa0) ||
                 // RSHIFT の場合は、Ctrlキーが押されておらず、それが漢直トグルキーになっているか、または漢直モードでシフト単打が有効か、または拡張シフト面が定義されているとき
                 //(vkey == VirtualKeys.RSHIFT && !ctrl && (Settings.ActiveKey == (uint)vkey || (isDecoderActivated() && isSingleShiftHitEffeciveOrShiftPlaneAssigned(vkey, KeyModifiers.MOD_RSHIFT, true)))) ||
                 // RSHIFT の場合は、Ctrlキーが押されていないとき
                 (vkey == VirtualKeys.RSHIFT && !ctrl) ||
                 (vkey >= 0xa6 && vkey < 0xf3) ||
                 (vkey >= 0xf5 && vkey < vkeyNum));
        }

        private bool ctrlKeyPressed()
        {
            return (Settings.UseLeftControlToConversion && (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0)
                || (Settings.UseRightControlToConversion && (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0);
        }

        //private bool shiftKeyPressed(bool bWithOutCtrl)
        //{
        //    return (!bWithOutCtrl && spaceKeyState == ExModKeyState.SHIFTED) || (GetAsyncKeyState(VirtualKeys.LSHIFT) & 0x8000) != 0 || (GetAsyncKeyState(VirtualKeys.RSHIFT) & 0x8000) != 0;
        //}

        private bool shiftKeyPressed(uint vkey)
        {
            // RSHIFTが押されている時はシフト状態とは判定しない
            return (GetAsyncKeyState(VirtualKeys.LSHIFT) & 0x8000) != 0 || (vkey != VirtualKeys.RSHIFT && (GetAsyncKeyState(VirtualKeys.RSHIFT) & 0x8000) != 0);
        }

        private bool isLshiftKeyPressed()
        {
            return (GetAsyncKeyState(VirtualKeys.LSHIFT) & 0x8000) != 0;
        }

        private bool isSandSEnabled()
        {
            bool decoderActivated = isDecoderActivated();
            return (Settings.SandSEnabled && decoderActivated) || (Settings.SandSEnabledWhenOffMode && !decoderActivated);
        }

        /// <summary> 特殊キーの押下状態</summary>
        class ExModiferKeyInfo {
            /// <summary> 特殊キーの押下状態</summary>
            public enum ExModKeyState
            {
                RELEASED,
                RELEASED_ONESHOT,   // SandS のみ使用
                PRESSED,
                PRESSED_ONESHOT,    // SandS のみ使用
                SHIFTED,
                SHIFTED_ONESHOT,    // SandS のみ使用
                REPEATED,           // SandS のみ使用
            }

            public uint Vkey = 0;
            public uint ModFlag = 0;
            public ExModKeyState KeyState = ExModKeyState.RELEASED;

            public string Name = "";

            public bool Released { get { return KeyState == ExModKeyState.RELEASED; } }
            public bool ReleasedOneshot { get { return KeyState == ExModKeyState.RELEASED_ONESHOT; } }
            public bool Pressed { get { return KeyState == ExModKeyState.PRESSED; } }
            public bool PressedOneshot { get { return KeyState == ExModKeyState.PRESSED_ONESHOT; } }
            public bool Shifted { get { return KeyState == ExModKeyState.SHIFTED; } }
            public bool ShiftedOneshot { get { return KeyState == ExModKeyState.SHIFTED_ONESHOT; } }
            public bool ShiftedOrOneshot { get { return KeyState == ExModKeyState.SHIFTED || KeyState == ExModKeyState.SHIFTED_ONESHOT; } }
            public bool Repeated { get { return KeyState == ExModKeyState.REPEATED; } }

            public static bool IsReleased(ExModKeyState state) { return state == ExModKeyState.RELEASED; }
            public static bool IsReleasedOneshot(ExModKeyState state) { return state == ExModKeyState.RELEASED_ONESHOT; }
            public static bool IsPressed(ExModKeyState state) { return state == ExModKeyState.PRESSED; }
            public static bool IsPressedOneshot(ExModKeyState state) { return state == ExModKeyState.PRESSED_ONESHOT; }
            public static bool IsShifted(ExModKeyState state) { return state == ExModKeyState.SHIFTED; }
            public static bool IsShiftedOneshot(ExModKeyState state) { return state == ExModKeyState.SHIFTED_ONESHOT; }
            public static bool IsRepeated(ExModKeyState state) { return state == ExModKeyState.REPEATED; }

            public void SetReleased() {
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:Set RELEASED");
                KeyState = ExModKeyState.RELEASED;
            }
            public void SetReleasedOneshot() {
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:Set RELEASED_ONESHOT");
                KeyState = ExModKeyState.RELEASED_ONESHOT;
            }
            public void SetPressed() {
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:Set PRESSED");
                KeyState = ExModKeyState.PRESSED;
            }
            public void SetPressedOneshot() {
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:Set PRESSED_ONESHOT");
                KeyState = ExModKeyState.PRESSED_ONESHOT;
            }
            public void SetShifted() {
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:Set SHIFTED");
                KeyState = ExModKeyState.SHIFTED;
            }
            public void SetShiftedOneshot() {
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:Set SHIFTED_ONESHOT");
                KeyState = ExModKeyState.SHIFTED_ONESHOT;
            }
            public void SetRepeated() {
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:Set REPEATED");
                KeyState = ExModKeyState.REPEATED;
            }

            private bool? isSingleShiftHitEffecive = null;

            /// <summary> シフト単打が有効か</summary>
            public bool IsSingleShiftHitEffecive()
            {
                if (isSingleShiftHitEffecive == null) {
                    isSingleShiftHitEffecive = Settings.ActiveKey == Vkey || VirtualKeys.IsExModKeyIndexAssignedForDecoderFunc(Vkey);
                }
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:IsSingleShiftHitEffecive={isSingleShiftHitEffecive}");
                return isSingleShiftHitEffecive.Value;
            }

            private bool? bShiftPlaneAssignedOn = null;
            private bool? bShiftPlaneAssignedOff = null;

            /// <summary> 拡張シフト面が定義されているか</summary>
            public bool IsShiftPlaneAssigned(bool bDecoderOn)
            {
                return bDecoderOn ? isShiftPlaneAssignedOn() : isShiftPlaneAssignedOff();
            }

            private bool isShiftPlaneAssignedOn()
            {
                if (bShiftPlaneAssignedOn == null) {
                    bShiftPlaneAssignedOn = Settings.ExtraModifiersEnabled && VirtualKeys.IsShiftPlaneAssignedForShiftModFlag(ModFlag, true);
                }
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:decoderOn=True: IsShiftPlaneAssigned={bShiftPlaneAssignedOn}");
                return bShiftPlaneAssignedOn.Value;
            }

            private bool isShiftPlaneAssignedOff()
            {
                if (bShiftPlaneAssignedOff == null) {
                    bShiftPlaneAssignedOff = Settings.ExtraModifiersEnabled && VirtualKeys.IsShiftPlaneAssignedForShiftModFlag(ModFlag, false);
                }
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:decoderOn=False: IsShiftPlaneAssigned={bShiftPlaneAssignedOff}");
                return bShiftPlaneAssignedOff.Value;
            }

            /// <summary> 内部状態の再初期化</summary>
            public void Reinitialize()
            {
                logger.InfoH($"ENTER: {Name}");
                isSingleShiftHitEffecive = null;
                bShiftPlaneAssignedOn = null;
                bShiftPlaneAssignedOff = null;
            }
        }

        /// <summary> 特殊キーの押下状態の管理クラス</summary>
        class ExModiferKeyInfoManager
        {
            /// <summary> スペースキーの押下状態</summary>
            private ExModiferKeyInfo spaceKeyInfo = new ExModiferKeyInfo() { Vkey = VirtualKeys.SPACE, ModFlag = KeyModifiers.MOD_SPACE, Name = "SandS" };

            /// <summary> CapsLockキーの押下状態</summary>
            private ExModiferKeyInfo capsKeyInfo = new ExModiferKeyInfo() { Vkey = VirtualKeys.CapsLock, ModFlag = KeyModifiers.MOD_CAPS, Name = "CapsLock" };

            /// <summary> 英数キーの押下状態</summary>
            private ExModiferKeyInfo alnumKeyInfo = new ExModiferKeyInfo() { Vkey = VirtualKeys.AlphaNum, ModFlag = KeyModifiers.MOD_ALNUM, Name = "AlpahNum" };

            /// <summary> 無変換キーの押下状態</summary>
            private ExModiferKeyInfo nferKeyInfo = new ExModiferKeyInfo() { Vkey = VirtualKeys.Nfer, ModFlag = KeyModifiers.MOD_NFER, Name = "Nfer" };

            /// <summary> 変換キーの押下状態</summary>
            private ExModiferKeyInfo xferKeyInfo = new ExModiferKeyInfo() { Vkey = VirtualKeys.Xfer, ModFlag = KeyModifiers.MOD_XFER, Name = "Xfer" };

            /// <summary> RShiftキーの押下状態</summary>
            private ExModiferKeyInfo rshiftKeyInfo = new ExModiferKeyInfo() { Vkey = VirtualKeys.RSHIFT, ModFlag = KeyModifiers.MOD_RSHIFT, Name = "RShift" };

            /// <summary> その他キーの押下状態</summary>
            private ExModiferKeyInfo otherKeyState = new ExModiferKeyInfo() { Name = "Other" };

            public void Reinitialize()
            {
                logger.InfoH($"ENTER");
                spaceKeyInfo.Reinitialize();
                capsKeyInfo.Reinitialize();
                alnumKeyInfo.Reinitialize();
                nferKeyInfo.Reinitialize();
                xferKeyInfo.Reinitialize();
                rshiftKeyInfo.Reinitialize();
                otherKeyState.Reinitialize();
            }

            /// <summary> 拡張修飾キーからキー状態を得る</summary>
            public ExModiferKeyInfo getModiferKeyInfoByVkey(uint vkey)
            {
                if (vkey == spaceKeyInfo.Vkey) return spaceKeyInfo;
                if (vkey == capsKeyInfo.Vkey) return capsKeyInfo;
                if (vkey == alnumKeyInfo.Vkey) return alnumKeyInfo;
                if (vkey == nferKeyInfo.Vkey) return nferKeyInfo;
                if (vkey == xferKeyInfo.Vkey) return xferKeyInfo;
                if (vkey == rshiftKeyInfo.Vkey) return rshiftKeyInfo;
                return null;
            }

            /// <summary> 拡張修飾キーの修飾フラグを得る</summary>
            public static uint getModFlagForExModVkey(uint vkey)
            {
                if (vkey == VirtualKeys.CapsLock) return KeyModifiers.MOD_CAPS;
                if (vkey == VirtualKeys.AlphaNum) return KeyModifiers.MOD_ALNUM;
                if (vkey == VirtualKeys.Nfer) return KeyModifiers.MOD_NFER;
                if (vkey == VirtualKeys.Xfer) return KeyModifiers.MOD_XFER;
                if (vkey == VirtualKeys.RSHIFT) return KeyModifiers.MOD_RSHIFT;
                if (vkey == VirtualKeys.SPACE) return KeyModifiers.MOD_SPACE;
                return 0;
            }

            /// <summary> 拡張修飾キーの修飾フラグからキー状態を得る</summary>
            public ExModiferKeyInfo getModiferKeyInfoByModFlag(uint modFlag)
            {
                if (modFlag == KeyModifiers.MOD_SPACE) return spaceKeyInfo;
                if (modFlag == KeyModifiers.MOD_CAPS) return capsKeyInfo;
                if (modFlag == KeyModifiers.MOD_ALNUM) return alnumKeyInfo;
                if (modFlag == KeyModifiers.MOD_NFER) return nferKeyInfo;
                if (modFlag == KeyModifiers.MOD_XFER) return xferKeyInfo;
                if (modFlag == KeyModifiers.MOD_RSHIFT) return rshiftKeyInfo;
                return otherKeyState;
            }

            /// <summary> SHIFT状態にある拡張修飾キーの修飾フラグを得る</summary>
            public uint getShiftedExModKey()
            {
                if (spaceKeyInfo.Shifted) {
                    if (Settings.SandSSuperiorToShift) return KeyModifiers.MOD_SPACE;
                    if (!rshiftKeyInfo.Shifted) return KeyModifiers.MOD_SPACE;
                }
                if (capsKeyInfo.Shifted) return KeyModifiers.MOD_CAPS;
                if (alnumKeyInfo.Shifted) return KeyModifiers.MOD_ALNUM;
                if (nferKeyInfo.Shifted) return KeyModifiers.MOD_NFER;
                if (xferKeyInfo.Shifted) return KeyModifiers.MOD_XFER;
                if (rshiftKeyInfo.Shifted) return KeyModifiers.MOD_RSHIFT;
                return 0;
            }

            /// <summary> 拡張修飾キーの押下またシフト状態を得る</summary>
            public uint getPressedOrShiftedExModFlag()
            {
                if (spaceKeyInfo.Pressed || spaceKeyInfo.Shifted) {
                    if (Settings.SandSSuperiorToShift) return KeyModifiers.MOD_SPACE;
                    if (!rshiftKeyInfo.Pressed && !rshiftKeyInfo.Shifted) return KeyModifiers.MOD_SPACE;
                }
                if (capsKeyInfo.Pressed || capsKeyInfo.Shifted) return KeyModifiers.MOD_CAPS;
                if (alnumKeyInfo.Pressed || alnumKeyInfo.Shifted) return KeyModifiers.MOD_ALNUM;
                if (nferKeyInfo.Pressed || nferKeyInfo.Shifted) return KeyModifiers.MOD_NFER;
                if (xferKeyInfo.Pressed || xferKeyInfo.Shifted) return KeyModifiers.MOD_XFER;
                if (rshiftKeyInfo.Pressed || rshiftKeyInfo.Shifted) return KeyModifiers.MOD_RSHIFT;
                return 0;
            }

            /// <summary>すでに押下状態にある拡張修飾キーをSHIFT状態に遷移させる</summary>
            public void makeExModKeyShifted(bool bDecoderOn)
            {
                if (spaceKeyInfo.Pressed || spaceKeyInfo.PressedOneshot) spaceKeyInfo.SetShifted();
                if (capsKeyInfo.Pressed && capsKeyInfo.IsShiftPlaneAssigned(bDecoderOn)) capsKeyInfo.SetShifted();
                if (alnumKeyInfo.Pressed && alnumKeyInfo.IsShiftPlaneAssigned(bDecoderOn)) alnumKeyInfo.SetShifted();
                if (nferKeyInfo.Pressed && nferKeyInfo.IsShiftPlaneAssigned(bDecoderOn)) nferKeyInfo.SetShifted();
                if (xferKeyInfo.Pressed && xferKeyInfo.IsShiftPlaneAssigned(bDecoderOn)) xferKeyInfo.SetShifted();
                if (rshiftKeyInfo.Pressed) rshiftKeyInfo.SetShifted();
            }

            public VirtualKeys.ShiftPlane getShiftPlane(bool bDecoderOn, bool bSandSEnabled)
            {
                if (spaceKeyInfo.ShiftedOrOneshot) {
                    if (Settings.SandSSuperiorToShift || !rshiftKeyInfo.Shifted) {
                        var plane = VirtualKeys.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_SPACE, bDecoderOn);
                        if (plane == VirtualKeys.ShiftPlane.NONE && bSandSEnabled) plane = VirtualKeys.ShiftPlane.NormalPlane;
                        return plane;
                    }
                }
                if (capsKeyInfo.Shifted) return VirtualKeys.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_CAPS, bDecoderOn);
                if (alnumKeyInfo.Shifted) return VirtualKeys.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_ALNUM, bDecoderOn);
                if (nferKeyInfo.Shifted) return VirtualKeys.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_NFER, bDecoderOn);
                if (xferKeyInfo.Shifted) return VirtualKeys.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_XFER, bDecoderOn);
                if (rshiftKeyInfo.Shifted) return VirtualKeys.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_RSHIFT, bDecoderOn);
                return VirtualKeys.ShiftPlane.NONE;
            }

            public ExModiferKeyInfo.ExModKeyState getSandSKeyState()
            {
                return spaceKeyInfo.KeyState;
            }

            public bool isSandSShifted()
            {
                return spaceKeyInfo.Shifted;
            }

            public bool isSandSShiftedOneshot()
            {
                return spaceKeyInfo.ShiftedOneshot;
            }

            public bool resetSandSShiftedOneshot()
            {
                bool bPrevShiftedOneshot = spaceKeyInfo.ShiftedOneshot;
                if (bPrevShiftedOneshot) spaceKeyInfo.SetReleased();
                return bPrevShiftedOneshot;
            }

            public string modifiersStateStr()
            {
                return $"spaceKeyState={spaceKeyInfo.KeyState}"
                + $"\ncapsKeyState={capsKeyInfo.KeyState}"
                + $"\nalnumKeyState={alnumKeyInfo.KeyState}"
                + $"\nnferKeyState={nferKeyInfo.KeyState}"
                + $"\nxferKeyState={xferKeyInfo.KeyState}"
                + $"\nrshiftKeyState={rshiftKeyInfo.KeyState}\n";
            }

        }

        /// <summary> 拡張修飾キーの押下状態の管理オブジェクト</summary>
        ExModiferKeyInfoManager keyInfoManager = new ExModiferKeyInfoManager();

        /// <summary> 内部状態の再初期化</summary>
        public void Reinitialize()
        {
            keyInfoManager.Reinitialize();
        }

        /// <summary>
        /// SandS と同じシフト面を使う拡張シフトキーか
        /// </summary>
        /// <returns></returns>
        private bool isSameShiftKeyAsSandS(uint fkey, bool bDecoderOn)
        {
            if (Settings.LoggingDecKeyInfo) logger.DebugH($"fkey={fkey:x}H");
            var plane_sands = VirtualKeys.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_SPACE, bDecoderOn);
            if (fkey != 0) {
                var plane_fkey = VirtualKeys.GetShiftPlaneFromShiftModFlag(fkey, bDecoderOn);
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"plane_fkey={plane_fkey}, plane_sands={plane_sands}");
                return plane_fkey == plane_sands;
            }
            if (isLshiftKeyPressed()) {
                // 左シフトキーが押されている場合は、SandSが通常シフト面か否かをチェック
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"plane_Lshift={VirtualKeys.ShiftPlane.NormalPlane}, plane_sands={plane_sands}");
                return plane_sands == VirtualKeys.ShiftPlane.NormalPlane;
            }
            return false;
        }

        /// <summary>
        /// SandS と同じシフト面を使う拡張シフトキーが押されているかシフト状態か
        /// </summary>
        /// <returns></returns>
        private bool isSameShiftKeyAsSandSPressedOrShifted(bool bDecoderOn)
        {
            return isSameShiftKeyAsSandS(keyInfoManager.getPressedOrShiftedExModFlag(), bDecoderOn);
        }

        /// <summary>
        /// SandS と同じシフト面を使う拡張シフトキーがシフト状態か
        /// </summary>
        /// <returns></returns>
        private bool isSameShiftKeyAsSandSShifted(bool bDecoderOn)
        {
            return isSameShiftKeyAsSandS(keyInfoManager.getShiftedExModKey(), bDecoderOn);;
        }

        private int getShiftPlaneDeckeyForSandS(bool bDecoderOn)
        {
            switch (VirtualKeys.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_SPACE, bDecoderOn)) {
                case VirtualKeys.ShiftPlane.NormalPlane:
                    return DecoderKeys.POST_NORMAL_SHIFT_DECKEY;
                case VirtualKeys.ShiftPlane.PlaneA:
                    return DecoderKeys.POST_PLANE_A_SHIFT_DECKEY;
                case VirtualKeys.ShiftPlane.PlaneB:
                    return DecoderKeys.POST_PLANE_B_SHIFT_DECKEY;
                default:
                    return 0;
            }
        }

        private void invokeHandlerForPostSandSKey()
        {
            int deckey = getShiftPlaneDeckeyForSandS(true);
            if (deckey > 0) invokeHandler(deckey, 0);
        }

        /// <summary> extraInfo=0 の時のキー押下時のリザルトフラグ </summary>
        //private bool normalInfoKeyDownResult = false;

        /// <summary> キーボードハンドラの処理中か </summary>
        private bool bHandlerBusy = false;

        /// <summary> 前回のSpace打鍵UP時刻 </summary>
        private DateTime prevSpaceUpDt = DateTime.MinValue;

        private const int KEY_REPEAT_INTERVAL = 500;

        /// <summary>キーボード押下時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <param name="extraInfo"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private bool onKeyboardDownHandler(int vkey, int scanCode, int extraInfo)
        {
            if (Settings.LoggingDecKeyInfo) {
                logger.DebugH(() => $"\nENTER: vkey={vkey:x}H({vkey}), scanCode={scanCode:x}H, extraInfo={extraInfo}\n" + keyInfoManager.modifiersStateStr());
            }

            // キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる
            bool handleKeyDown()
            {
                bool leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
                bool rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;
                bool bCtrl = leftCtrl || rightCtrl;

                if (!isEffectiveVkey(vkey, scanCode, extraInfo, bCtrl)) {
                    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"not EffectiveVkey");
                    return false;
                }

                bool bShift = shiftKeyPressed((uint)vkey);
                bool bDecoderOn = isDecoderActivated();
                uint modFlag = ExModiferKeyInfoManager.getModFlagForExModVkey((uint)vkey);
                uint modPressedOrShifted = keyInfoManager.getPressedOrShiftedExModFlag();
                var keyInfo = keyInfoManager.getModiferKeyInfoByVkey((uint)vkey);
                if (keyInfo != null) {
                    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"{keyInfo.Name}Key Pressed: ctrl={bCtrl}, shift={bShift}, decoderOn={bDecoderOn}, modFlag={modFlag:x}, modPressedOrShifted={modPressedOrShifted:x}");
                    if ((uint)vkey == VirtualKeys.SPACE) {
                        // Space
                        if (isSandSEnabled()) {
                            // SandSと同じシフト面を使う左Shiftまたは拡張修飾キーがシフト状態か(何か(拡張)シフトキーが Pressed だったら、Spaceキーが押されたことで Shifted に移行しているはず)
                            bool bShiftOnSamePlane = isSameShiftKeyAsSandSShifted(bDecoderOn);
                            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS Enabled: ShiftOnSamePlane={bShiftOnSamePlane}, SandSEnablePostShift={Settings.SandSEnablePostShift}");
                            if (bShiftOnSamePlane) {
                                // SandSと同じシフト面を使う拡張修飾キーがシフト状態なら、シフト状態に遷移する
                                keyInfo.SetShifted();
                                return true; // keyboardDownHandler() をスキップ、システム側の本来のDOWN処理もスキップ
                            }
                            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS: IgnoreSpaceUpOnSandS={Settings.OneshotSandSEnabled}, ctrl={bCtrl}");
                            if (Settings.OneshotSandSEnabled && bCtrl) {
                                // SandS時に1回目のSpace単打を無視する設定の場合は、Ctrl+Space が打鍵されたらそれをシフト状態に遷移させる
                                keyInfo.SetShifted();
                                return true; // keyboardDownHandler() をスキップ、システム側の本来のDOWN処理もスキップ
                            }
                            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS: keyInfo.Shifted={keyInfo.Shifted}");
                            if (keyInfo.Shifted) {
                                // SHIFT状態なら何もしない
                                return true; // keyboardDownHandler() をスキップ、システム側の本来のDOWN処理もスキップ
                            }
                            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS: keyInfo.PressedOneshot={keyInfo.PressedOneshot}");
                            if (keyInfo.PressedOneshot) {
                                // PRESSED_ONESHOT⇒REPEATED
                                keyInfo.SetRepeated();
                            }
                            if (!keyInfo.Repeated) {
                                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS: keyInfo.ShiftedOneshot={keyInfo.ShiftedOneshot}");
                                if (keyInfo.ShiftedOneshot) {
                                    // SHIFTED_ONESHOT⇒PRESSED_ONESHOT
                                    keyInfo.SetPressedOneshot();
                                    return true; // keyboardDownHandler() をスキップ、システム側の本来のDOWN処理もスキップ
                                }
                                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS: keyInfo.Pressed={keyInfo.Pressed}");
                                if (keyInfo.Pressed) {
                                    // すでにスペースキーが押下されている(キーリピート)
                                    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"prevSpaceUpDt={prevSpaceUpDt}.{prevSpaceUpDt:fff}");
                                    if (Settings.SandSEnableSpaceOrRepeatMillisec <= 0 ||
                                        DateTime.Now > prevSpaceUpDt.AddMilliseconds(Settings.SandSEnableSpaceOrRepeatMillisec + KEY_REPEAT_INTERVAL)) {
                                        // キーリピートに移行しない閾値時間が設定されている or 前回のSpaceキー離放時から閾値時間を超過していた
                                        keyInfo.SetShifted();
                                        // 後置シフトキーを送出する
                                        if (Settings.SandSEnablePostShift && bDecoderOn) invokeHandlerForPostSandSKey();
                                        return true; // keyboardDownHandler() をスキップ、システム側の本来のDOWN処理もスキップ
                                    }
                                    // リピート状態に移行
                                    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS: SetRepeated");
                                    keyInfo.SetRepeated();
                                }
                                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS: keyState={keyInfo.KeyState}");
                                if (!keyInfo.Repeated) {
                                    // RELEASEDのはず
                                    if ((!bCtrl && !bShift && modPressedOrShifted == 0) || (Settings.HandleShiftSpaceAsSandS && (bShift || modPressedOrShifted == KeyModifiers.MOD_RSHIFT))) {
                                        // 1回目の押下で Ctrl も Shift も他のmodiferも押されてない場合、またはShiftが押されていてもSandSがShiftより劣位の場合は、PRESSED に移行
                                        keyInfo.SetPressed();
                                        return true; // keyboardDownHandler() をスキップ、システム側の本来のDOWN処理もスキップ
                                    }
                                }
                            }
                            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS: LEAVE");
                        }
                        // 上記以外はスペース入力として扱う。すでに押下状態にある拡張修飾キーをSHIFT状態に遷移させる
                        keyInfoManager.makeExModKeyShifted(bDecoderOn);
                    } else if ((uint)vkey == VirtualKeys.RSHIFT) {
                        // RSHIFT
                        if (keyInfo.IsShiftPlaneAssigned(bDecoderOn)) {
                            // 拡張シフト面が割り当てられている場合
                            if (keyInfo.Pressed || modPressedOrShifted != 0) {
                                // 当拡張修飾キーが押下されている、またはその他の拡張修飾キーが押下orシフト状態なら、その他の拡張修飾キーを含めてシフト状態に遷移する
                                keyInfo.SetShifted();
                                keyInfoManager.makeExModKeyShifted(bDecoderOn);
                            } else if (keyInfo.Released) {
                                // 最初の押下
                                keyInfo.SetPressed();
                                //} else if (keyInfo.Shifted) {
                                //    // SHIFT状態なら何もしない
                            }
                            return true; // keyboardDownHandler() をスキップ、システム側の本来のSHIFT処理もスキップ
                        }
                        if (keyInfo.IsSingleShiftHitEffecive()) {
                            // 拡張シフト面が割り当てはないが、単打系ありの場合
                            if (keyInfo.Pressed) {
                                // 当拡張修飾キーが押下されているなら、シフト状態に遷移する
                                keyInfo.SetShifted();
                                keyInfoManager.makeExModKeyShifted(bDecoderOn);
                            } else if (keyInfo.Released) {
                                // 最初の押下
                                keyInfo.SetPressed();
                            }
                        }
                        return false; // keyboardDownHandler() をスキップ、システム側で本来のSHIFT処理を実行
                    } else {
                        // Space/RSHIFT 以外
                        if (keyInfo.IsShiftPlaneAssigned(bDecoderOn)) {
                            // 拡張シフト面が割り当てられている拡張修飾キーの場合
                            if (keyInfo.Pressed || modPressedOrShifted != 0) {
                                // 当拡張修飾キーが押下されている、またはその他の拡張修飾キーが押下orシフト状態なら、その他の拡張修飾キーを含めてシフト状態に遷移する
                                keyInfo.SetShifted();
                                keyInfoManager.makeExModKeyShifted(bDecoderOn);
                            } else if (keyInfo.Released) {
                                // 最初の押下
                                keyInfo.SetPressed();
                                //} else if (keyInfo.Shifted) {
                                //    // SHIFT状態なら何もしない
                            }
                            return true; // keyboardDownHandler() をスキップ、システム側の本来のSHIFT処理もスキップ
                        } else if (keyInfo.IsSingleShiftHitEffecive()) {
                            // 拡張シフト面が割り当てはないが、単打系ありの場合
                            if (keyInfo.Released) {
                                if (bCtrl || bShift || modPressedOrShifted != 0) {
                                    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"RELEASED -> PRESSED");
                                    keyInfo.SetPressed();
                                    return true; // keyboardDownHandler() をスキップ、システム側の本来のSHIFT処理もスキップ
                                }
                                // 最初の押下で他のCtrlやShiftや拡張修飾が押されていない場合は、keyboardDownHandler() を呼び出す
                            } else if (keyInfo.Pressed) {
                                keyInfo.SetShifted();
                                keyInfoManager.makeExModKeyShifted(bDecoderOn);
                            }
                        } else {
                            // 拡張シフト面が割り当てられておらず単打系でもない拡張修飾キー
                            // すでに押下状態にある拡張修飾キーをSHIFT状態に遷移させる
                            keyInfoManager.makeExModKeyShifted(bDecoderOn);
                            // 拡張修飾キーがテーブルファイルに記述されている可能性もあるので keyboardDownHandler() を呼び出す
                        }
                    }
                } else {
                    // 通常キーの場合は、すでに押下状態にある拡張修飾キーをSHIFT状態に遷移させる
                    keyInfoManager.makeExModKeyShifted(bDecoderOn);
                    if (keyInfoManager.isSandSShifted() && bDecoderOn && Settings.SandSEnablePostShift) {
                        // SandS が SHIFTED に遷移していれば後置シフトキーも送出する
                        invokeHandlerForPostSandSKey();
                    }
                }
                // keyboardDownHandler()の呼び出し
                if (Settings.LoggingDecKeyInfo) {
                    logger.DebugH(() => $"CALL: keyboardDownHandler({vkey}, {leftCtrl}, {rightCtrl})\n" + keyInfoManager.modifiersStateStr());
                }
                return keyboardDownHandler(vkey, leftCtrl, rightCtrl);
            }

            bool result = handleKeyDown();
            if (Settings.LoggingDecKeyInfo) {
                logger.DebugH(() => $"LEAVE: result={result}, vkey={vkey:x}H({vkey}), extraInfo={extraInfo}\n" + keyInfoManager.modifiersStateStr());
            }
            return result;
        }

        /// <summary>キーボード押下時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private bool keyboardDownHandler(int vkey, bool leftCtrl, bool rightCtrl)
        {
            //bool leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
            //bool rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;
            bool bDecoderOn = isDecoderActivated();
            bool ctrl = leftCtrl || rightCtrl;
            bool shift = shiftKeyPressed((uint)vkey);
            uint mod = KeyModifiers.MakeModifier(ctrl, shift);
            uint modEx = keyInfoManager.getShiftedExModKey();
            if (modEx == 0 && keyInfoManager.isSandSShiftedOneshot()) modEx = KeyModifiers.MOD_SPACE;
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"ENTER: mod={mod:x}H({mod}), modEx={modEx:x}H({modEx}), vkey={vkey:x}H({vkey}), ctrl={ctrl}, shift={shift}");

            int kanchokuCode = VirtualKeys.GetKanchokuToggleDecKey(mod, (uint)vkey); // 漢直モードのトグルをやるキーか
            if (kanchokuCode < 0 && modEx != 0 && !ctrl && !shift) {
                // 拡張シフトが有効なのは、Ctrlキーが押されておらず、Shiftも押されていないか、Shift+SpaceをSandSとして扱わない場合とする
                kanchokuCode = VirtualKeys.GetModConvertedDecKeyFromCombo(modEx, (uint)vkey);
                if (kanchokuCode < 0) {
                    // 拡張シフト面のコードを得る
                    var shiftPlane = keyInfoManager.getShiftPlane(bDecoderOn, isSandSEnabled());
                    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"PATH-A: shiftPlane={shiftPlane}");
                    if (shiftPlane != VirtualKeys.ShiftPlane.NONE) kanchokuCode = VirtualKeys.GetDecKeyFromCombo(0, (uint)vkey) + (int)shiftPlane * DecoderKeys.SHIFT_DECKEY_NUM;
                }
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"PATH-B: kanchokuCode={kanchokuCode:x}H({kanchokuCode}), modEx={modEx:x}, ctrl={ctrl}, shift={shift}");
            }

            if (kanchokuCode < 0) {
                if (leftCtrl) {
                    // mod-conversion.txt で lctrl に定義されているものを検索
                    kanchokuCode = VirtualKeys.GetModConvertedDecKeyFromCombo(KeyModifiers.MOD_LCTRL, (uint)vkey);
                }
                if (kanchokuCode < 0 && rightCtrl) {
                    // mod-conversion.txt で rctrl に定義されているものを検索
                    kanchokuCode = VirtualKeys.GetModConvertedDecKeyFromCombo(KeyModifiers.MOD_RCTRL, (uint)vkey);
                }
                if (kanchokuCode < 0) {
                    kanchokuCode = (Settings.GlobalCtrlKeysEnabled && ((Settings.UseLeftControlToConversion && leftCtrl) || (Settings.UseRightControlToConversion && rightCtrl))) || shift
                        ? VirtualKeys.GetModConvertedDecKeyFromCombo(mod, (uint)vkey)
                        : VirtualKeys.GetDecKeyFromCombo(mod, (uint)vkey);
                }
                if (kanchokuCode >= 0) mod = 0;     // 何かのコードに変換されたら、 Ctrl や Shift の修飾は無かったことにしておく
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"PATH-C: kanchokuCode={kanchokuCode:x}H({kanchokuCode}), ctrl={ctrl}, shift={shift}");
            }

            // SandS の一時シフト状態をリセットする
            keyInfoManager.resetSandSShiftedOneshot();

            //if (kanchokuCode < 0) {
            //    bool result = false;
            //    if (spaceKeyState == ExModKeyState.SHIFTED) {
            //        // SandS により Shift モードになっている場合は、SendInput で Shift down をエミュレートする
            //        if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS");
            //        try {
            //            bHandlerBusy = true;
            //            result = SendInputVkeyWithMod?.Invoke(mod, (uint)vkey) ?? false;
            //        } finally {
            //            bHandlerBusy = false;
            //        }
            //    }
            //    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"LEAVE-A: result={result}");
            //    return result;
            //}

            //if (kanchokuCode == DecoderKeys.HISTORY_NEXT_SEARCH_DECKEY && kanchokuCode != VirtualKeys.GetCtrlDecKeyOf(Settings.HistorySearchCtrlKey)) {
            //    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"LEAVE-B: result=False, historySearchCtrlKey={Settings.HistorySearchCtrlKey}, kanchokuCode={VirtualKeys.GetCtrlDecKeyOf(Settings.HistorySearchCtrlKey)}");
            //    return false;
            //}

            // どうやら KeyboardHook で CallNextHookEx を呼ばないと次のキー入力の処理に移らないみたいだが、
            // 将来必要になるかもしれないので、下記処理を残しておく
            //if (bHandlerBusy) {
            //    logger.Warn(() => "bHandlerBusy=True");
            //    if (vkeyQueue.Count < vkeyQueueMaxSize) {
            //        vkeyQueue.Enqueue(kanchokuCode);
            //        if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"vkeyQueue.Count={vkeyQueue.Count}");
            //    }
            //    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"LEAVE-C: result=True");
            //    return true;
            //}
            //while (true) {
            //    bool result = invokeHandler(kanchokuCode, mod);
            //    if (vkeyQueue.Count == 0) {
            //        if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"LEAVE-D: result={result}");
            //        return result;
            //    }
            //    kanchokuCode = vkeyQueue.Dequeue();
            //}
            if (bHandlerBusy) {
                return true;
            }
            if (bDecoderOn && mod == 0 &&
                (kanchokuCode >= 0 && kanchokuCode < DecoderKeys.NORMAL_DECKEY_END || kanchokuCode >= DecoderKeys.SHIFT_A_DECKEY_START && kanchokuCode < DecoderKeys.SHIFT_A_DECKEY_END) &&
                OverlappingKeyStroke.Determiner.Singleton.KeyDown(kanchokuCode)) {
                return true;
            } else {
                return invokeHandler(kanchokuCode, mod);
            }
        }

        private const int vkeyQueueMaxSize = 4;

        private Queue<int> vkeyQueue = new Queue<int>();

        private int prevUpVkey = -1;

        /// <summary>キーアップ時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <param name="extraInfo"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private bool onKeyboardUpHandler(int vkey, int scanCode, int extraInfo)
        {
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"\nENTER: vkey={vkey:x}H({vkey}), scanCode={scanCode:x}H, extraInfo={extraInfo}");

            int prevVkey = prevUpVkey;
            prevUpVkey = vkey;

            bool leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
            bool rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;

            var keyState = keyInfoManager.getSandSKeyState();
            // spaceKey の shiftedOneshot 状態を解除しておく
            keyInfoManager.resetSandSShiftedOneshot();

            if (!isEffectiveVkey(vkey, scanCode, extraInfo, leftCtrl || rightCtrl)) {
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"LEAVE: result=False, not EffectiveVkey");
                return false;
            }

            bool bDecoderOn = isDecoderActivated();
            uint modFlag = ExModiferKeyInfoManager.getModFlagForExModVkey((uint)vkey);
            var keyInfo = keyInfoManager.getModiferKeyInfoByVkey((uint)vkey);
            //bool result = false;
            if (keyInfo != null) {
                bool bPrevPressed = keyInfo.Pressed;
                bool bPrevPressedOneshot = keyInfo.PressedOneshot;
                keyInfo.SetReleased();
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"{keyInfo.Name}Key up: prevPressed={bPrevPressed}, prevPressedOneshot={bPrevPressedOneshot}, decoderOn={bDecoderOn}, modFlag={modFlag:x}, newKeyState={keyInfo.KeyState}");
                if ((uint)vkey == VirtualKeys.SPACE) {
                    // Space離放
                    if (isSandSEnabled()) {
                        var dtLimit = prevSpaceUpDt.AddMilliseconds(Settings.SandSEnableSpaceOrRepeatMillisec._geZeroOr(0));
                        var dtNow = DateTime.Now;
                        if (bPrevPressed || bPrevPressedOneshot) prevSpaceUpDt = dtNow;
                        if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS UP: IgnoreSpaceUpOnSandS={Settings.OneshotSandSEnabled}, dtLimit={dtLimit}, dtNow={dtNow}, ShiftedExModKey={keyInfoManager.getShiftedExModKey()}");
                        if (keyInfoManager.getShiftedExModKey() != 0) {
                            // 何か拡張シフト状態だったら、Spaceキーは無視
                            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS UP: Ignore Space");
                            return true;
                        } else if (bPrevPressed) {
                            // Spaceキーが1回押されただけの状態
                            if (Settings.OneshotSandSEnabled && (prevVkey != vkey || dtNow > dtLimit) && IsDecoderWaitingFirstStroke?.Invoke() == true) {
                                // SandS時のSpaceUpを一時シフト状態にする設定で、前回のキーがSPACEでないか前回のSpace打鍵から指定のms以上経過しており、今回が第1打鍵である
                                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS UP: SetShiftedOneshot");
                                keyInfo.SetShiftedOneshot();
                                SetSandSShiftedOneshot?.Invoke();
                                return true;
                            }
                            // Spaceキーを送出
                            keyboardDownHandler(vkey, leftCtrl, rightCtrl);
                            keyboardUpHandler(bDecoderOn, vkey, leftCtrl, rightCtrl, 0);
                        } else if (bPrevPressedOneshot) {
                            // ShiftedOneshot の後 Spaceキーが1回押されただけの状態
                            // Spaceキーを送出
                            keyboardDownHandler(vkey, leftCtrl, rightCtrl);
                            keyboardUpHandler(bDecoderOn, vkey, leftCtrl, rightCtrl, 0);
                        }
                    }
                    // 上記以外は何もせず、システムに処理をまかせる
                    return false;
                } else if ((uint)vkey == VirtualKeys.RSHIFT) {
                    // RSHIFT
                    //if (keyInfo.IsShiftPlaneAssigned(bDecoderOn)) {
                    //    // 拡張シフト面が割り当てられている場合
                    //    if (bPrevPressed) {
                    //        keyboardDownHandler(vkey, leftCtrl, rightCtrl);
                    //    }
                    //}
                    if (keyInfo.IsSingleShiftHitEffecive()) {
                        // 拡張シフト面が割り当ての有無にかかわらず、単打系ありの場合
                        if (bPrevPressed) {
                            // PRESSED状態だったら、ハンドラを呼び出す
                            keyboardDownHandler(vkey, leftCtrl, rightCtrl);
                            keyboardUpHandler(bDecoderOn, vkey, leftCtrl, rightCtrl, 0);
                        }
                    }
                    // システムに RSHIFT UP 処理をまかせる
                    return false;
                } else {
                    // Space/RSHIFT 以外
                    if (bPrevPressed && keyInfo.IsShiftPlaneAssigned(bDecoderOn) && keyInfo.IsSingleShiftHitEffecive()) {
                        // 拡張シフト面が割り当てられ、かつ単打系がある拡張修飾キーで、それが押下状態の場合
                        keyboardDownHandler(vkey, leftCtrl, rightCtrl);
                        keyboardUpHandler(bDecoderOn, vkey, leftCtrl, rightCtrl, 0);
                    }
                    return false;
                }
            }

            keyboardUpHandler(bDecoderOn, vkey, leftCtrl, rightCtrl, modFlag);
            return false;
        }

        /// <summary>キーボードUP時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private void keyboardUpHandler(bool bDecoderOn, int vkey, bool leftCtrl, bool rightCtrl, uint modFlag)
        {
            if (bDecoderOn && !leftCtrl && !rightCtrl && modFlag == 0) {
                int deckey = VirtualKeys.GetDecKeyFromCombo(0, (uint)vkey);
                if (deckey >= 0 && (deckey < DecoderKeys.NORMAL_DECKEY_END || deckey >= DecoderKeys.SHIFT_A_DECKEY_START && deckey < DecoderKeys.SHIFT_A_DECKEY_END)) {
                    var keyList = OverlappingKeyStroke.Determiner.Singleton.KeyUp(deckey);
                    if (keyList._notEmpty()) {
                        foreach (var k in keyList) {
                            invokeHandler(k, 0);
                        }
                    }
                }
            }
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"LEAVE: result={false}");
        }

        private bool invokeHandler(int kanchokuCode, uint mod)
        {
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"ENTER: kanchokuCode={kanchokuCode:x}H({kanchokuCode}), mod={mod:x}H({mod}), UNCONDITIONAL_DECKEY_OFFSET={DecoderKeys.UNCONDITIONAL_DECKEY_OFFSET}, UNCONDITIONAL_DECKEY_END={DecoderKeys.UNCONDITIONAL_DECKEY_END}");
            bHandlerBusy = true;
            try {
                switch (kanchokuCode) {
                    case DecoderKeys.TOGGLE_DECKEY:
                        ToggleDecoder?.Invoke();
                        return true;
                    case DecoderKeys.MODE_TOGGLE_FOLLOW_CARET_DECKEY:
                        Settings.VirtualKeyboardPosFixedTemporarily = false;
                        ToggleDecoder?.Invoke();
                        return true;
                    case DecoderKeys.ACTIVE_DECKEY:
                    case DecoderKeys.ACTIVE2_DECKEY:
                        ActivateDecoder?.Invoke();
                        return true;
                    case DecoderKeys.DEACTIVE_DECKEY:
                    case DecoderKeys.DEACTIVE2_DECKEY:
                        DeactivateDecoder?.Invoke();
                        return true;
                    //case DecoderKeys.STROKE_HELP_ROTATION_DECKEY:
                    //    return RotateStrokeHelp?.Invoke() ?? false;
                    //case DecoderKeys.STROKE_HELP_UNROTATION_DECKEY:
                    //    return RotateReverseStrokeHelp?.Invoke() ?? false;
                    //case DecoderKeys.DATE_STRING_ROTATION_DECKEY:
                    //    return RotateDateString?.Invoke() ?? false;
                    //case DecoderKeys.DATE_STRING_UNROTATION_DECKEY:
                    //    return RotateReverseDateString?.Invoke() ?? false;
                    default:
                        if (kanchokuCode >= DecoderKeys.UNCONDITIONAL_DECKEY_OFFSET && kanchokuCode < DecoderKeys.UNCONDITIONAL_DECKEY_END) {
                            return InvokeDecoderUnconditionally?.Invoke(kanchokuCode - DecoderKeys.UNCONDITIONAL_DECKEY_OFFSET, mod) ?? false;
                        }
                        if (kanchokuCode >= 0) {
                            return FuncDispatcher?.Invoke(kanchokuCode, mod) ?? false;
                        }
                        return false;
                }
            } finally {
                bHandlerBusy = false;
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"LEAVE");
            }
        }

    }
}

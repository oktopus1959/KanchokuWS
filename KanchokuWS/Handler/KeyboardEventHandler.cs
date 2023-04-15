using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using KanchokuWS.Domain;
using Utils;

namespace KanchokuWS.Handler
{
    public class KeyboardEventHandler : IDisposable
    {
        private static Logger logger = Logger.GetLogger();

        private FrmKanchoku frmKanchoku = null;

        /// <summary>デコーダが ON か</summary>
        private bool isDecoderActivated() {
            return frmKanchoku?.IsDecoderActivated() ?? false;
        }

        /// <summary>デコーダが第1打鍵待ちか </summary>
        private bool isDecoderWaitingFirstStroke() {
            return frmKanchoku?.IsDecoderWaitingFirstStroke() == true;
        }

        /// <summary>コンストラクタ</summary>
        public KeyboardEventHandler()
        {
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize(FrmKanchoku frm)
        {
            logger.InfoH("ENTER");

            frmKanchoku = frm;

            setInvokeHandlerToDeterminer();

            keyInfoManager = new ExModiferKeyInfoManager();

            // キーボードイベントのディスパッチ開始
            installKeyboardHook();

            logger.InfoH("LEAVE");
        }

        /// <summary> 内部状態の再初期化</summary>
        public void Reinitialize()
        {
            keyInfoManager.Reinitialize();
        }

        /// <summary>
        /// キーボードフックされているか
        /// </summary>
        private bool bHooked = false;

        /// <summary>
        /// キーボードフックを設定する
        /// </summary>
        private void installKeyboardHook()
        {
            logger.InfoH($"ENTER");
            KeyboardHook.OnKeyDownEvent = onKeyboardDownHandler;
            KeyboardHook.OnKeyUpEvent = onKeyboardUpHandler;
            KeyboardHook.Hook();
            bHooked = true;
            logger.InfoH($"LEAVE");
        }

        /// <summary>
        /// キーボードフックを解放する
        /// </summary>
        private void releaseKeyboardHook()
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
            releaseKeyboardHook();
        }

        //----------------------------------------------------------------------------------------------------------
        [DllImport("user32.dll")]
        private static extern ushort GetAsyncKeyState(uint vkey);

        private const int vkeyNum = 256;

        /// <summary>やまぶきRが送ってくる SendInput の scanCode </summary>
        private const int YamabukiRscanCode = 0x7f;

        /// <summary>他のアプリからキーコードがINJECTされた </summary>
        private const uint LLKHF_INJECTED = 0x10;

        /// <summary> 漢直として有効なキーか</summary>
        private bool isEffectiveVkey(uint vkey, int scanCode, uint flags, int extraInfo, bool ctrl)
        {
            // 0xa0 = LSHIFT, 0xa1 = RSHIFT, 0xa2=LCTRL, 0xa3=RCTRL, 0xa4=LALT, 0xa5 = RALT, 0xf3 = Zenkaku, 0xf4 = Kanji
            return
                (Settings.IgnoreOtherHooker ? (flags & LLKHF_INJECTED) == 0 : extraInfo != SendInputHandler.MyMagicNumber) &&
                scanCode != 0 && scanCode != YamabukiRscanCode &&
                ((vkey > 0 && vkey < 0xa0) ||
                 // RSHIFT の場合は、Ctrlキーが押されておらず、それが漢直トグルキーになっているか、または漢直モードでシフト単打が有効か、または拡張シフト面が定義されているとき
                 //(vkey == FuncVKeys.RSHIFT && !ctrl && (Settings.ActiveKey == vkey || (isDecoderActivated() && isSingleShiftHitEffeciveOrShiftPlaneAssigned(vkey, KeyModifiers.MOD_RSHIFT, true)))) ||
                 // RSHIFT の場合は、Ctrlキーが押されていないとき
                 (vkey == FuncVKeys.RSHIFT && (!ctrl || Settings.ActiveKeyWithCtrl == vkey || Settings.ActiveKeyWithCtrl2 == vkey || Settings.SelectedTableActivatedWithCtrl == vkey || Settings.DeactiveKeyWithCtrl == vkey)) ||
                 //(vkey >= 0xa6 && vkey < 0xf3) ||
                 //(vkey >= 0xf5 && vkey < vkeyNum));
                 (vkey >= 0xa6 && vkey < vkeyNum));
        }

        private static bool isAltKeyPressed()
        {
            return (GetAsyncKeyState(FuncVKeys.ALT) & 0x8000) != 0;
        }

        private bool ctrlKeyPressed()
        {
            return (Settings.UseLeftControlToConversion && (GetAsyncKeyState(FuncVKeys.LCONTROL) & 0x8000) != 0)
                || (Settings.UseRightControlToConversion && (GetAsyncKeyState(FuncVKeys.RCONTROL) & 0x8000) != 0);
        }

        //private bool shiftKeyPressed(bool bWithOutCtrl)
        //{
        //    return (!bWithOutCtrl && spaceKeyState == ExModKeyState.SHIFTED) || (GetAsyncKeyState(FuncVKeys.LSHIFT) & 0x8000) != 0 || (GetAsyncKeyState(FuncVKeys.RSHIFT) & 0x8000) != 0;
        //}

        private bool shiftKeyPressed(uint vkey)
        {
            // RSHIFTが押されている時はシフト状態とは判定しない
            return (GetAsyncKeyState(FuncVKeys.LSHIFT) & 0x8000) != 0 || (vkey != FuncVKeys.RSHIFT && (GetAsyncKeyState(FuncVKeys.RSHIFT) & 0x8000) != 0);
        }

        private bool isLshiftKeyPressed()
        {
            return (GetAsyncKeyState(FuncVKeys.LSHIFT) & 0x8000) != 0;
        }

        private bool isSandSEnabled()
        {
            bool decoderActivated = isDecoderActivated();
            return (Settings.SandSEnabledCurrently && decoderActivated) || (Settings.SandSEnabledWhenOffMode && !decoderActivated);
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
            public int Deckey = 0;
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

            /// <summary> シフト単打が有効か</summary>
            public bool IsSingleShiftHitEffecive(bool bCtrl)
            {
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:Vkey={Vkey}, Deckey={Deckey}, bCtrl={bCtrl}, ActiveKey={Settings.ActiveKey}, ActiveKeyWithCtrl={Settings.ActiveKeyWithCtrl}, IsExModKeyIndexAssignedForDecoderFunc={ExtraModifiers.IsExModKeyIndexAssignedForDecoderFunc(Deckey)}");
                bool bEffective = (Settings.ActiveKey == Vkey && (!bCtrl || Settings.ActiveKeyWithCtrl != Vkey)) || ExtraModifiers.IsExModKeyIndexAssignedForDecoderFunc(Deckey);
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:IsSingleShiftHitEffecive={bEffective}");
                return bEffective;
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
                    bShiftPlaneAssignedOn = Settings.ExtraModifiersEnabled && ShiftPlane.IsShiftPlaneAssignedForShiftModFlag(ModFlag, true);
                }
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:decoderOn=True: IsShiftPlaneAssigned={bShiftPlaneAssignedOn}");
                return bShiftPlaneAssignedOn.Value;
            }

            private bool isShiftPlaneAssignedOff()
            {
                if (bShiftPlaneAssignedOff == null) {
                    bShiftPlaneAssignedOff = Settings.ExtraModifiersEnabled && ShiftPlane.IsShiftPlaneAssignedForShiftModFlag(ModFlag, false);
                }
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"{Name}:decoderOn=False: IsShiftPlaneAssigned={bShiftPlaneAssignedOff}");
                return bShiftPlaneAssignedOff.Value;
            }

            /// <summary> 内部状態の再初期化</summary>
            public void Reinitialize(uint vkey)
            {
                logger.InfoH(() => $"ENTER: {Name}, vkey={vkey}");
                Vkey = vkey;
                bShiftPlaneAssignedOn = null;
                bShiftPlaneAssignedOff = null;
            }
        }

        /// <summary> 特殊キーの押下状態の管理クラス</summary>
        class ExModiferKeyInfoManager
        {
            /// <summary> スペースキーの押下状態</summary>
            private ExModiferKeyInfo spaceKeyInfo = new ExModiferKeyInfo() { Vkey = FuncVKeys.SPACE, Deckey = DecoderKeys.STROKE_SPACE_DECKEY, ModFlag = KeyModifiers.MOD_SPACE, Name = "SandS" };

            /// <summary> CapsLockキーの押下状態</summary>
            private ExModiferKeyInfo capsKeyInfo = new ExModiferKeyInfo() { Vkey = FuncVKeys.CAPSLOCK, Deckey = DecoderKeys.CAPS_DECKEY, ModFlag = KeyModifiers.MOD_CAPS, Name = "CapsLock" };

            /// <summary> 英数キーの押下状態</summary>
            private ExModiferKeyInfo alnumKeyInfo = new ExModiferKeyInfo() { Vkey = FuncVKeys.EISU, Deckey = DecoderKeys.ALNUM_DECKEY, ModFlag = KeyModifiers.MOD_ALNUM, Name = "AlpahNum" };

            /// <summary> 無変換キーの押下状態</summary>
            private ExModiferKeyInfo nferKeyInfo = new ExModiferKeyInfo() { Vkey = FuncVKeys.MUHENKAN, Deckey = DecoderKeys.NFER_DECKEY, ModFlag = KeyModifiers.MOD_NFER, Name = "Nfer" };

            /// <summary> 変換キーの押下状態</summary>
            private ExModiferKeyInfo xferKeyInfo = new ExModiferKeyInfo() { Vkey = FuncVKeys.HENKAN, Deckey = DecoderKeys.XFER_DECKEY, ModFlag = KeyModifiers.MOD_XFER, Name = "Xfer" };

            /// <summary> RShiftキーの押下状態</summary>
            private ExModiferKeyInfo rshiftKeyInfo = new ExModiferKeyInfo() { Vkey = FuncVKeys.RSHIFT, Deckey = DecoderKeys.RIGHT_SHIFT_DECKEY, ModFlag = KeyModifiers.MOD_RSHIFT, Name = "RShift" };

            /// <summary> その他キーの押下状態</summary>
            private ExModiferKeyInfo otherKeyState = new ExModiferKeyInfo() { Name = "Other" };

            public void Reinitialize()
            {
                logger.InfoH($"ENTER");
                spaceKeyInfo.Reinitialize(FuncVKeys.SPACE);
                capsKeyInfo.Reinitialize(FuncVKeys.CAPSLOCK);
                alnumKeyInfo.Reinitialize(FuncVKeys.EISU);
                nferKeyInfo.Reinitialize(FuncVKeys.MUHENKAN);
                xferKeyInfo.Reinitialize(FuncVKeys.HENKAN);
                rshiftKeyInfo.Reinitialize(FuncVKeys.RSHIFT);
                otherKeyState.Reinitialize(0);
            }

            /// <summary> 拡張修飾キーからキー状態を得る</summary>
            public ExModiferKeyInfo getModiferKeyInfoByVkey(uint vkey)
            {
                if (Settings.LoggingDecKeyInfo) { logger.DebugH($"CALLED: vkey={vkey}, nfer.Vkey={nferKeyInfo.Vkey}, xfer.Vkey={xferKeyInfo.Vkey}"); }

                if (vkey == capsKeyInfo.Vkey) return capsKeyInfo;
                if (vkey == alnumKeyInfo.Vkey) return alnumKeyInfo;
                if (vkey == nferKeyInfo.Vkey) return nferKeyInfo;
                if (vkey == xferKeyInfo.Vkey) return xferKeyInfo;
                if (vkey == rshiftKeyInfo.Vkey) return rshiftKeyInfo;
                if (vkey == spaceKeyInfo.Vkey) return spaceKeyInfo;

                if (Settings.LoggingDecKeyInfo) { logger.DebugH($"LEAVE: no result"); }
                return null;
            }

            /// <summary> 拡張修飾キーの修飾フラグを得る</summary>
            public static uint getModFlagForExModVkey(uint vkey)
            {
                if (Settings.LoggingDecKeyInfo) { logger.DebugH($"CALLED: vkey={vkey}, MUHENKAN={FuncVKeys.MUHENKAN}, HENKAN={FuncVKeys.HENKAN}"); }

                if (vkey == FuncVKeys.CAPSLOCK) return KeyModifiers.MOD_CAPS;
                if (vkey == FuncVKeys.EISU) return KeyModifiers.MOD_ALNUM;
                if (vkey == FuncVKeys.MUHENKAN) return KeyModifiers.MOD_NFER;
                if (vkey == FuncVKeys.HENKAN) return KeyModifiers.MOD_XFER;
                if (vkey == FuncVKeys.RSHIFT) return KeyModifiers.MOD_RSHIFT;
                if (vkey == FuncVKeys.SPACE) return KeyModifiers.MOD_SPACE;

                if (Settings.LoggingDecKeyInfo) { logger.DebugH($"LEAVE: no result"); }
                return 0;
            }

            /// <summary> 拡張修飾キーの修飾フラグからキー状態を得る</summary>
            public ExModiferKeyInfo getModiferKeyInfoByModFlag(uint modFlag)
            {
                if (modFlag == KeyModifiers.MOD_CAPS) return capsKeyInfo;
                if (modFlag == KeyModifiers.MOD_ALNUM) return alnumKeyInfo;
                if (modFlag == KeyModifiers.MOD_NFER) return nferKeyInfo;
                if (modFlag == KeyModifiers.MOD_XFER) return xferKeyInfo;
                if (modFlag == KeyModifiers.MOD_RSHIFT) return rshiftKeyInfo;
                if (modFlag == KeyModifiers.MOD_SPACE) return spaceKeyInfo;
                return otherKeyState;
            }

            /// <summary> SHIFT状態にある拡張修飾キーの修飾フラグを得る</summary>
            public uint getShiftedExModKey()
            {
                if (capsKeyInfo.Shifted) return KeyModifiers.MOD_CAPS;
                if (alnumKeyInfo.Shifted) return KeyModifiers.MOD_ALNUM;
                if (nferKeyInfo.Shifted) return KeyModifiers.MOD_NFER;
                if (xferKeyInfo.Shifted) return KeyModifiers.MOD_XFER;
                if (rshiftKeyInfo.Shifted) return KeyModifiers.MOD_RSHIFT;
                if (spaceKeyInfo.Shifted) {
                    if (Settings.SandSSuperiorToShift) return KeyModifiers.MOD_SPACE;
                    if (!rshiftKeyInfo.Shifted) return KeyModifiers.MOD_SPACE;
                }
                return 0;
            }

            /// <summary> 拡張修飾キーの押下またシフト状態を得る</summary>
            public uint getPressedOrShiftedExModFlag()
            {
                if (capsKeyInfo.Pressed || capsKeyInfo.Shifted) return KeyModifiers.MOD_CAPS;
                if (alnumKeyInfo.Pressed || alnumKeyInfo.Shifted) return KeyModifiers.MOD_ALNUM;
                if (nferKeyInfo.Pressed || nferKeyInfo.Shifted) return KeyModifiers.MOD_NFER;
                if (xferKeyInfo.Pressed || xferKeyInfo.Shifted) return KeyModifiers.MOD_XFER;
                if (rshiftKeyInfo.Pressed || rshiftKeyInfo.Shifted) return KeyModifiers.MOD_RSHIFT;
                if (spaceKeyInfo.Pressed || spaceKeyInfo.Shifted) {
                    if (Settings.SandSSuperiorToShift) return KeyModifiers.MOD_SPACE;
                    if (!rshiftKeyInfo.Pressed && !rshiftKeyInfo.Shifted) return KeyModifiers.MOD_SPACE;
                }
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

            public int getShiftPlane(bool bDecoderOn, bool bSandSEnabled)
            {
                if (capsKeyInfo.Shifted) return ShiftPlane.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_CAPS, bDecoderOn);
                if (alnumKeyInfo.Shifted) return ShiftPlane.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_ALNUM, bDecoderOn);
                if (nferKeyInfo.Shifted) return ShiftPlane.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_NFER, bDecoderOn);
                if (xferKeyInfo.Shifted) return ShiftPlane.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_XFER, bDecoderOn);
                if (rshiftKeyInfo.Shifted) return ShiftPlane.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_RSHIFT, bDecoderOn);
                if (spaceKeyInfo.ShiftedOrOneshot) {
                    if (Settings.SandSSuperiorToShift || !rshiftKeyInfo.Shifted) {
                        var plane = ShiftPlane.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_SPACE, bDecoderOn);
                        if (plane == ShiftPlane.ShiftPlane_NONE && bSandSEnabled) plane = ShiftPlane.ShiftPlane_SHIFT;
                        return plane;
                    }
                }
                return ShiftPlane.ShiftPlane_NONE;
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
                return Logger.IsInfoEnabled
                    ? $"spaceKeyState={spaceKeyInfo.KeyState}"
                    + $"\ncapsKeyState={capsKeyInfo.KeyState}"
                    + $"\nalnumKeyState={alnumKeyInfo.KeyState}"
                    + $"\nnferKeyState={nferKeyInfo.KeyState}"
                    + $"\nxferKeyState={xferKeyInfo.KeyState}"
                    + $"\nrshiftKeyState={rshiftKeyInfo.KeyState}\n"
                    : "";
            }

        }

        /// <summary> 拡張修飾キーの押下状態の管理オブジェクト</summary>
        ExModiferKeyInfoManager keyInfoManager = null;

        /// <summary>
        /// SandS と同じシフト面を使う拡張シフトキーか
        /// </summary>
        /// <returns></returns>
        private bool isSameShiftKeyAsSandS(uint fkey, bool bDecoderOn)
        {
            if (Settings.LoggingDecKeyInfo) logger.DebugH($"fkey={fkey:x}H");
            var plane_sands = ShiftPlane.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_SPACE, bDecoderOn);
            if (fkey != 0) {
                var plane_fkey = ShiftPlane.GetShiftPlaneFromShiftModFlag(fkey, bDecoderOn);
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"plane_fkey={plane_fkey}, plane_sands={plane_sands}");
                return plane_fkey == plane_sands;
            }
            if (isLshiftKeyPressed()) {
                // 左シフトキーが押されている場合は、SandSが通常シフト面か否かをチェック
                if (Settings.LoggingDecKeyInfo) logger.DebugH($"plane_Lshift={ShiftPlane.ShiftPlane_SHIFT}, plane_sands={plane_sands}");
                return plane_sands == ShiftPlane.ShiftPlane_SHIFT;
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
            switch (ShiftPlane.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_SPACE, bDecoderOn)) {
                case ShiftPlane.ShiftPlane_SHIFT:
                    return DecoderKeys.POST_NORMAL_SHIFT_DECKEY;
                case ShiftPlane.ShiftPlane_A:
                    return DecoderKeys.POST_PLANE_A_SHIFT_DECKEY;
                case ShiftPlane.ShiftPlane_B:
                    return DecoderKeys.POST_PLANE_B_SHIFT_DECKEY;
                case ShiftPlane.ShiftPlane_C:
                    return DecoderKeys.POST_PLANE_C_SHIFT_DECKEY;
                case ShiftPlane.ShiftPlane_D:
                    return DecoderKeys.POST_PLANE_D_SHIFT_DECKEY;
                case ShiftPlane.ShiftPlane_E:
                    return DecoderKeys.POST_PLANE_E_SHIFT_DECKEY;
                case ShiftPlane.ShiftPlane_F:
                    return DecoderKeys.POST_PLANE_F_SHIFT_DECKEY;
                default:
                    return 0;
            }
        }

        private void invokeHandlerForPostSandSKey()
        {
            int deckey = getShiftPlaneDeckeyForSandS(true);
            if (deckey > 0) invokeHandler(deckey, -1, 0);
        }

        /// <summary> extraInfo=0 の時のキー押下時のリザルトフラグ </summary>
        //private bool normalInfoKeyDownResult = false;

        /// <summary> 前回のSpace打鍵UP時刻 </summary>
        private DateTime prevSpaceUpDt = DateTime.MinValue;

        private const int KEY_REPEAT_INTERVAL = 500;

        /// <summary> 同時打鍵キーのリピート中か(仮想鍵盤に表示する打鍵ガイドを切り替えるのに使う) </summary>
        private bool bComboKeyRepeat = false;
        private uint prevComboVkey = 0;

        /// <summary>
        /// 同時打鍵キーのオートリピートが開始されたら打鍵ガイドを切り替える
        /// </summary>
        /// <param name="vkey">同時打鍵キーの仮想キーコード</param>
        /// <param name="decKey">同時打鍵キーのデコーダ用コード</param>
        void handleComboKeyRepeat(uint vkey, List<int> decKeys)
        {
            if (prevComboVkey == vkey) {
                // KeyRepeat
                if (!bComboKeyRepeat) {
                    logger.DebugH(() => $"SetNextStrokeHelpDecKey({decKeys._keyString()})");
                    frmKanchoku?.SetNextStrokeHelpDecKey(decKeys);
                    bComboKeyRepeat = true;
                }
            } else {
                prevComboVkey = vkey;
            }
        }

        /// <summary>
        /// 同時打鍵キーのオートリピートが終了したら打鍵ガイドを元に戻す
        /// </summary>
        /// <param name="vkey"></param>
        void handleComboKeyRepeatStop(uint vkey)
        {
            if (prevComboVkey == vkey) {
                if (bComboKeyRepeat) {
                    bComboKeyRepeat = false;
                    frmKanchoku?.SetNextStrokeHelpDecKey(null);
                }
                prevComboVkey = 0;
            }
        }

        bool bLCtrlShifted = false;
        bool bRCtrlShifted = false;
        bool bLShiftShifted = false;

        /// <summary>キーボード押下時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <param name="extraInfo"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private bool onKeyboardDownHandler(uint vkey, int scanCode, uint flags, int extraInfo)
        {
            // Pauseで一時停止?
            if (Settings.SuspendByPauseKey && vkey == (uint)Keys.Pause) {
                frmKanchoku?.DecoderSuspendToggle();
                return true;
            }
            // 一時停止?
            if (Settings.DecoderSuspended) return false;

            if (Settings.LoggingDecKeyInfo) {
                logger.InfoH(() => $"\nENTER: vkey={vkey:x}H({vkey}), scanCode={scanCode:x}H, extraInfo={extraInfo}\n" + keyInfoManager.modifiersStateStr());
            }

            if (DecoderKeyVsVKey.IsUSonJPmode || DecoderKeyVsVKey.IsEisuDisabled) {
                // EISU disabled
                // 英数キーはCapsに変換
                if (vkey == 0xf0) vkey = 0x14;
            }
            if (DecoderKeyVsVKey.IsUSonJPmode) {
                // US-on-JP Mode
                // 半/全キーは 0xf3 に寄せる
                if (vkey == 0xf4) vkey = 0xf3;
            } else {
                // JP or US Mode
                // 半/全キーは、false(システム側による処理) を返す
                if (vkey == 0xf3 || vkey == 0xf4) return false;
            }

            // キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる
            bool handleKeyDown()
            {
                bool leftShift = (GetAsyncKeyState(FuncVKeys.LSHIFT) & 0x8000) != 0;
                bool leftCtrl = (GetAsyncKeyState(FuncVKeys.LCONTROL) & 0x8000) != 0;
                bool rightCtrl = (GetAsyncKeyState(FuncVKeys.RCONTROL) & 0x8000) != 0;
                bool bCtrl = leftCtrl || rightCtrl;

                // とりあえず、やっつけコード
                if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"vkey={vkey:x}H({vkey}), leftCtrl={leftCtrl}, rightCtrl={rightCtrl}, leftShift={leftShift}");
                if (extraInfo == 0 && leftCtrl) bLCtrlShifted = true;    // 左ＣＴＲＬがＯＮのときに何かキーが押されたら左ＣＴＲＬをシフト状態にする
                if (extraInfo == 0 && rightCtrl) bRCtrlShifted = true;   // 右ＣＴＲＬがＯＮのときに何かキーが押されたら右ＣＴＲＬをシフト状態にする
                if (extraInfo == 0 && leftShift) bLShiftShifted = true;  // 左SHIFTがＯＮのときに何かキーが押されたら左SHIFTをシフト状態にする
                if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"bLCtrlShifted={bLCtrlShifted}, bRCtrlShifted={bRCtrlShifted}, bLShiftShifted={bLShiftShifted}");

                if (!isEffectiveVkey(vkey, scanCode, flags, extraInfo, bCtrl)) {
                    if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"not EffectiveVkey");
                    return false;
                }

                bool bShift = shiftKeyPressed(vkey);
                bool bDecoderOn = isDecoderActivated();
                uint modFlag = ExModiferKeyInfoManager.getModFlagForExModVkey(vkey);
                uint modPressedOrShifted = keyInfoManager.getPressedOrShiftedExModFlag();

                // この処理は、keyboardDownHandler() 内でやるようにした
                //if (!bDecoderOn && !bCtrl && modPressedOrShifted == 0 && vkey >= (int)Keys.Left && vkey <= (int)Keys.Down) {
                //    // デコーダOFFで無修飾の矢印キーなら、システムに任せる
                //    if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"Normal Arrow Key");
                //    return false;
                //}

                var keyInfo = keyInfoManager.getModiferKeyInfoByVkey(vkey);
                if (keyInfo != null) {
                    if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"{keyInfo.Name}Key Pressed: ctrl={bCtrl}, shift={bShift}, decoderOn={bDecoderOn}, modFlag={modFlag:x}, modPressedOrShifted={modPressedOrShifted:x}");
                    if (vkey == FuncVKeys.SPACE) {
                        // Space
                        if (isSandSEnabled()) {
                            void setShifted()
                            {
                                if (!keyInfo.Shifted) {
                                    frmKanchoku?.SetStrokeHelpShiftPlane(ShiftPlane.GetShiftPlaneFromShiftModFlag(KeyModifiers.MOD_SPACE, true));   // SanS用のストロークヘルプ指定
                                }
                                keyInfo.SetShifted();
                            }

                            // SandSと同じシフト面を使う左Shiftまたは拡張修飾キーがシフト状態か(何か(拡張)シフトキーが Pressed だったら、Spaceキーが押されたことで Shifted に移行しているはず)
                            bool bShiftOnSamePlane = isSameShiftKeyAsSandSShifted(bDecoderOn);
                            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS Enabled: ShiftOnSamePlane={bShiftOnSamePlane}, SandSEnablePostShiftCurrently={Settings.SandSEnablePostShiftCurrently}");
                            if (bShiftOnSamePlane) {
                                // SandSと同じシフト面を使う拡張修飾キーがシフト状態なら、シフト状態に遷移する
                                setShifted();
                                return true; // keyboardDownHandler() をスキップ、システム側の本来のDOWN処理もスキップ
                            }
                            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS: IgnoreSpaceUpOnSandS={Settings.OneshotSandSEnabledCurrently}, ctrl={bCtrl}");
                            if (Settings.OneshotSandSEnabledCurrently && bCtrl) {
                                // SandS時に1回目のSpace単打を無視する設定の場合は、Ctrl+Space が打鍵されたらそれをシフト状態に遷移させる
                                setShifted();
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
                                        setShifted();
                                        // 後置シフトキーを送出する
                                        if (Settings.SandSEnablePostShiftCurrently && bDecoderOn) {
                                            logger.DebugH(() => $"CALL-1: invokeHandlerForPostSandSKey");
                                            invokeHandlerForPostSandSKey();
                                        }
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
                    } else if (vkey == FuncVKeys.RSHIFT) {
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
                        if (keyInfo.IsSingleShiftHitEffecive(bCtrl)) {
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
                        } else if (keyInfo.IsSingleShiftHitEffecive(bCtrl)) {
                            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SingleShiftHitEffecive({bCtrl})");
                            // 拡張シフト面が割り当てはないが、単打系ありの場合
                            if (keyInfo.Released) {
                                //if (bCtrl || bShift || modPressedOrShifted != 0) {
                                //    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"RELEASED -> PRESSED");
                                //    keyInfo.SetPressed();
                                //    return true; // keyboardDownHandler() をスキップ、システム側の本来のSHIFT処理もスキップ
                                //}
                                // 最初の押下で他のCtrlやShiftや拡張修飾が押されていない場合は、keyboardDownHandler() を呼び出す
                            } else if (keyInfo.Pressed) {
                                keyInfo.SetShifted();
                                keyInfoManager.makeExModKeyShifted(bDecoderOn);
                            }
                        } else {
                            // 拡張シフト面が割り当てられておらず単打系でもない拡張修飾キー
                            // すでに押下状態にあれば拡張修飾キーをSHIFT状態に遷移させる
                            keyInfoManager.makeExModKeyShifted(bDecoderOn);
                            // 拡張修飾キーがテーブルファイルに記述されている可能性もあるので keyboardDownHandler() を呼び出す
                        }
                    }
                } else {
                    // 通常キーの場合は、すでに押下状態にあれば拡張修飾キーをSHIFT状態に遷移させる
                    keyInfoManager.makeExModKeyShifted(bDecoderOn);
                    if (keyInfoManager.isSandSShifted() && bDecoderOn && Settings.SandSEnablePostShiftCurrently) {
                        // SandS が SHIFTED に遷移していれば後置シフトキーも送出する
                        logger.DebugH(() => $"CALL-2: invokeHandlerForPostSandSKey");
                        invokeHandlerForPostSandSKey();
                    }
                }
                // keyboardDownHandler()の呼び出し
                if (Settings.LoggingDecKeyInfo) {
                    logger.InfoH(() => $"CALL: keyboardDownHandler({vkey}, {leftCtrl}, {rightCtrl})\n" + keyInfoManager.modifiersStateStr());
                }
                return keyboardDownHandler(vkey, leftCtrl, rightCtrl);
            }

            bool result = handleKeyDown();
            if (Settings.LoggingDecKeyInfo) {
                logger.InfoH(() => $"LEAVE: result={result}, vkey={vkey:x}H({vkey}), extraInfo={extraInfo}\n" + keyInfoManager.modifiersStateStr());
            }
            return result;
        }

        int keyDownCount = 0;

        private struct DecKeyInfo
        {
            int kanchokuCode;
            int normalDecKey;
            uint modifiers;

            public DecKeyInfo(int kc, int nc, uint mod)
            {
                kanchokuCode = kc;
                normalDecKey = nc;
                modifiers = mod;
            }
        }

        //private const int vkeyQueueMaxSize = 4;

        //private Queue<int> vkeyQueue = new Queue<int>();

        private static bool bHandlerBusy = false;

        /// <summary>キーボード押下時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private bool keyboardDownHandler(uint vkey, bool leftCtrl, bool rightCtrl)
        {
            //bool leftCtrl = (GetAsyncKeyState(FuncVKeys.LCONTROL) & 0x8000) != 0;
            //bool rightCtrl = (GetAsyncKeyState(FuncVKeys.RCONTROL) & 0x8000) != 0;
            bool bDecoderOn = isDecoderActivated();
            bool ctrl = leftCtrl || rightCtrl;
            bool shift = shiftKeyPressed(vkey);
            bool alt = isAltKeyPressed();
            uint mod = KeyModifiers.MakeModifier(alt, ctrl, shift);
            uint modEx = keyInfoManager.getShiftedExModKey();
            if (modEx == 0 && keyInfoManager.isSandSShiftedOneshot()) modEx = KeyModifiers.MOD_SPACE;

            //int normalDecKey = VKeyComboRepository.GetDecKeyFromVKey(vkey);
            int normalDecKey = DecoderKeyVsVKey.GetDecKeyFromVKey(vkey);
            int kanchokuCode = KeyComboRepository.GetKanchokuToggleDecKey(mod, normalDecKey); // 漢直モードのトグルをやるキーか

            if (Settings.LoggingDecKeyInfo) {
                logger.InfoH(() => $"ENTER: kanchokuCode={kanchokuCode}, normalDecKey={normalDecKey}, mod={mod:x}H({mod}), modEx={modEx:x}H({modEx}), vkey={vkey:x}H({vkey}), ctrl={ctrl}, shift={shift}");
            }

            // 漢直トグルでなく、VirtualKeyboard がActiveの場合は、システムに返す
            if (kanchokuCode < 0 && ActiveWindowHandler.Singleton.IsVkbWinActive) return false;

            if (kanchokuCode < 0 && modEx != 0 && !ctrl && !shift) {
                // 拡張シフトが有効なのは、Ctrlキーが押されておらず、Shiftも押されていないか、Shift+SpaceをSandSとして扱わない場合とする
                kanchokuCode = KeyComboRepository.GetModConvertedDecKeyFromCombo(modEx, normalDecKey);
                if (kanchokuCode < 0) {
                    // 拡張シフト面のコードを得る
                    kanchokuCode = normalDecKey;
                    var shiftPlane = keyInfoManager.getShiftPlane(bDecoderOn, isSandSEnabled());
                    if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"PATH-A: shiftPlane={shiftPlane}, kanchokuCode={kanchokuCode}");
                    if (shiftPlane != ShiftPlane.ShiftPlane_NONE && kanchokuCode < DecoderKeys.NORMAL_DECKEY_NUM) {
                        kanchokuCode += shiftPlane * DecoderKeys.PLANE_DECKEY_NUM;
                    }
                }
                if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"PATH-B: kanchokuCode={kanchokuCode:x}H({kanchokuCode}), modEx={modEx:x}, ctrl={ctrl}, shift={shift}");
            }

            if (kanchokuCode < 0) {
                if (leftCtrl) {
                    // mod-conversion.txt で lctrl に定義されているものを検索
                    kanchokuCode = KeyComboRepository.GetModConvertedDecKeyFromCombo(KeyModifiers.MOD_LCTRL, normalDecKey);
                }
                if (kanchokuCode < 0 && rightCtrl) {
                    // mod-conversion.txt で rctrl に定義されているものを検索
                    kanchokuCode = KeyComboRepository.GetModConvertedDecKeyFromCombo(KeyModifiers.MOD_RCTRL, normalDecKey);
                }
                if (kanchokuCode < 0) {
                    kanchokuCode = (Settings.GlobalCtrlKeysEnabled && ((Settings.UseLeftControlToConversion && leftCtrl) || (Settings.UseRightControlToConversion && rightCtrl))) || shift
                        ? KeyComboRepository.GetModConvertedDecKeyFromCombo(mod, normalDecKey)
                        // : vkey == (int)Keys.Space ? DecoderKeys.STROKE_SPACE_DECKEY     // キーDown時のスペースは、いったんそのまま扱う(Up時に変換する)
                        : KeyComboRepository.GetDecKeyFromCombo(mod, normalDecKey);
                }
                if (kanchokuCode >= 0) mod = 0;     // 何かのコードに変換されたら、 Ctrl や Shift の修飾は無かったことにしておく
                if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"PATH-C: kanchokuCode={kanchokuCode:x}H({kanchokuCode}), ctrl={ctrl}, shift={shift}");
            }

            // SandS の一時シフト状態をリセットする
            keyInfoManager.resetSandSShiftedOneshot();

            if (!bDecoderOn && (kanchokuCode < 0 || normalDecKey <= 0 || (kanchokuCode == normalDecKey && normalDecKey >= DecoderKeys.FUNC_DECKEY_START))) {
                // デコーダーがOFFで、どの DecoderKey にもヒモ付けられていないか、または通常キーでもないキーが押されたら、そのままシステムに処理させる
                // ⇒ Astah など、なぜか自身で キーボード入力を監視していると思われるソフトがあるため
                if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"LEAVE: false: Decoder=OFF, no assigned deckey and not normal key");
                return false;
            }

            bool result = true;
            if (bHandlerBusy) {
                logger.WarnH("Handler Busy");
            } else {
                bHandlerBusy = true;
                ++keyDownCount;
                if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"bDecoderOn={bDecoderOn}, mod={mod:x}H, kanchokuCode={kanchokuCode}, normalDecKey={normalDecKey}, keyDownCount={keyDownCount}");
                var determiner = CombinationKeyStroke.Determiner.Singleton;
                var currentPool = CombinationKeyStroke.DeterminerLib.KeyCombinationPool.CurrentPool;
                if (/*(bDecoderOn || currentPool.HasComboEffectiveAlways) &&*/
                    mod == 0 &&                                                                         // 修飾子がない
                    currentPool.Enabled &&                                                              // 同時打鍵定義が有効
                    kanchokuCode >= 0 && kanchokuCode < DecoderKeys.STROKE_DECKEY_END &&                // 機能コード以外
                    ((kanchokuCode % DecoderKeys.PLANE_DECKEY_NUM) < DecoderKeys.NORMAL_DECKEY_NUM ||   // 通常キーであるか、
                     currentPool.GetEntry(kanchokuCode) != null)                                        // 特殊キーであっても同時打鍵テーブルで使われている
                    ) {
                    // KeyDown時処理を呼び出す。同時打鍵キーのオートリピートが開始されたら打鍵ガイドを切り替える
                    determiner.KeyDown(kanchokuCode, bDecoderOn, keyDownCount, (decKey) => handleComboKeyRepeat(vkey, decKey));
                    result = true;
                } else {
                    // 直接ハンドラを呼び出す
                    result = invokeHandler(kanchokuCode, normalDecKey, mod);
                }
                bHandlerBusy = false;
            }
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"LEAVE: result={result}");
            return result;
        }

        private uint prevUpVkey = 0;

        /// <summary>キーアップ時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <param name="extraInfo"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private bool onKeyboardUpHandler(uint vkey, int scanCode, uint flags, int extraInfo)
        {
            // Pauseで一時停止?
            if (Settings.SuspendByPauseKey && vkey == (uint)Keys.Pause) {
                return true;
            }
            // 一時停止?
            if (Settings.DecoderSuspended) return false;

            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"\nENTER: vkey={vkey:x}H({vkey}), scanCode={scanCode:x}H, extraInfo={extraInfo}");

            // 半/全キーは、US-on-JP モードなら true(入力破棄; つまり無視) JPモードなら false (システム処理; つまりIMEのON/OFF)を返す
            if (vkey == 0xf3 || vkey == 0xf4) return DecoderKeyVsVKey.IsUSonJPmode;

            uint prevVkey = prevUpVkey;
            prevUpVkey = vkey;

            bool leftShift = (GetAsyncKeyState(FuncVKeys.LSHIFT) & 0x8000) != 0;
            bool leftCtrl = (GetAsyncKeyState(FuncVKeys.LCONTROL) & 0x8000) != 0;
            bool rightCtrl = (GetAsyncKeyState(FuncVKeys.RCONTROL) & 0x8000) != 0;
            bool bCtrl = leftCtrl || rightCtrl;

            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"vkey={vkey:x}H({vkey}), leftCtrl={leftCtrl}, rightCtrl={rightCtrl}, leftShift={leftShift}");

            if (extraInfo == 0) {
                // とりあえず、やっつけコード
                void checkAndInvoke(bool bShifted)
                {
                    int normalDecKey = DecoderKeyVsVKey.GetDecKeyFromVKey(vkey);
                    if (!bShifted && /*bDecoderOn &&*/ ExtraModifiers.IsExModKeyIndexAssignedForDecoderFunc(normalDecKey)) {
                        int kanchokuCode = KeyComboRepository.GetDecKeyFromCombo(0, normalDecKey);
                        if (kanchokuCode >= 0) {
                            invokeHandler(kanchokuCode, -1, 0);
                        }
                    }
                }

                if (vkey == FuncVKeys.LCONTROL) {
                    if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"LCONTROL up");
                    checkAndInvoke(bLCtrlShifted);
                    bLCtrlShifted = false;
                } else if (vkey == FuncVKeys.RCONTROL) {
                    if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"RCONTROL up");
                    checkAndInvoke(bRCtrlShifted);
                    bRCtrlShifted = false;
                } else if (vkey == FuncVKeys.LSHIFT) {
                    if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"LSHIFT up");
                    checkAndInvoke(bLShiftShifted);
                    bLShiftShifted = false;
                }
            }

            var keyState = keyInfoManager.getSandSKeyState();
            // spaceKey の shiftedOneshot 状態を解除しておく
            keyInfoManager.resetSandSShiftedOneshot();

            // 同時打鍵キーのオートリピートが終了したら打鍵ガイドを元に戻す
            handleComboKeyRepeatStop(vkey);

            if (!isEffectiveVkey(vkey, scanCode, flags, extraInfo, leftCtrl || rightCtrl)) {
                if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"LEAVE: result=False, not EffectiveVkey");
                return false;
            }

            bool bDecoderOn = isDecoderActivated();

            uint modFlag = ExModiferKeyInfoManager.getModFlagForExModVkey(vkey);
            var keyInfo = keyInfoManager.getModiferKeyInfoByVkey(vkey);
            //bool result = false;
            if (keyInfo != null) {
                bool bPrevPressed = keyInfo.Pressed;
                bool bPrevPressedOneshot = keyInfo.PressedOneshot;
                keyInfo.SetReleased();
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() =>
                    $"{keyInfo.Name}Key up: prevPressed={bPrevPressed}, prevPressedOneshot={bPrevPressedOneshot}, decoderOn={bDecoderOn}, modFlag={modFlag:x}, newKeyState={keyInfo.KeyState}");
                if (vkey == FuncVKeys.SPACE) {
                    // Space離放
                    if (isSandSEnabled()) {
                        frmKanchoku?.SetStrokeHelpShiftPlane(0);
                        var dtLimit = prevSpaceUpDt.AddMilliseconds(Settings.SandSEnableSpaceOrRepeatMillisec._geZeroOr(0));
                        var dtNow = DateTime.Now;
                        if (bPrevPressed || bPrevPressedOneshot) prevSpaceUpDt = dtNow;
                        if (Settings.LoggingDecKeyInfo) {
                            logger.DebugH(() => $"SandS UP: IgnoreSpaceUpOnSandS={Settings.OneshotSandSEnabledCurrently}, " +
                                $"dtLimit={dtLimit}, dtNow={dtNow}, ShiftedExModKey={keyInfoManager.getShiftedExModKey()}");
                        }
                        if (keyInfoManager.getShiftedExModKey() != 0) {
                            // 何か拡張シフト状態だったら、Spaceキーは無視
                            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS UP: Ignore Space");
                            return true;
                        } else if (bPrevPressed) {
                            // Spaceキーが1回押されただけの状態
                            if (Settings.OneshotSandSEnabledCurrently && (prevVkey != vkey || dtNow > dtLimit) && isDecoderWaitingFirstStroke() == true) {
                                // SandS時のSpaceUpを一時シフト状態にする設定で、前回のキーがSPACEでないか前回のSpace打鍵から指定のms以上経過しており、今回が第1打鍵である
                                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"SandS UP: SetShiftedOneshot");
                                keyInfo.SetShiftedOneshot();
                                frmKanchoku?.SetSandSShiftedOneshot();
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
                    } else {
                        // Spaceキーの解放を通知
                        keyboardUpHandler(bDecoderOn, vkey, leftCtrl, rightCtrl, 0);
                    }
                    return false;
                } else if (vkey == FuncVKeys.RSHIFT) {
                    // RSHIFT
                    //if (keyInfo.IsShiftPlaneAssigned(bDecoderOn)) {
                    //    // 拡張シフト面が割り当てられている場合
                    //    if (bPrevPressed) {
                    //        keyboardDownHandler(vkey, leftCtrl, rightCtrl);
                    //    }
                    //}
                    if (keyInfo.IsSingleShiftHitEffecive(bCtrl)) {
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
                    if (bPrevPressed && keyInfo.IsShiftPlaneAssigned(bDecoderOn) && keyInfo.IsSingleShiftHitEffecive(bCtrl)) {
                        // 拡張シフト面が割り当てられ、かつ単打系がある拡張修飾キーで、それが押下状態の場合
                        keyboardDownHandler(vkey, leftCtrl, rightCtrl);
                    }
                    keyboardUpHandler(bDecoderOn, vkey, leftCtrl, rightCtrl, 0);
                    return false;
                }
            }

            // VirtualKeyboard がActiveの場合は、システムに返す
            if (ActiveWindowHandler.Singleton.IsVkbWinActive) return false;

            keyboardUpHandler(bDecoderOn, vkey, leftCtrl, rightCtrl, modFlag);
            return false;
        }

        /// <summary>キーボードUP時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private void keyboardUpHandler(bool bDecoderOn, uint vkey, bool leftCtrl, bool rightCtrl, uint modFlag)
        {
            var currentPool = CombinationKeyStroke.DeterminerLib.KeyCombinationPool.CurrentPool;
            if (/*(bDecoderOn || currentPool.HasComboEffectiveAlways) &&*/
                currentPool.Enabled &&  !leftCtrl && !rightCtrl && modFlag == 0) {
                //int deckey = /* vkey == (int)Keys.Space ? DecoderKeys.STROKE_SPACE_DECKEY :*/ VKeyComboRepository.GetDecKeyFromCombo(0, normalDecKey); /* ここではまだ、Spaceはいったん文字として扱う */
                int deckey = DecoderKeyVsVKey.GetDecKeyFromVKey(vkey);
                if (deckey >= 0 && deckey < DecoderKeys.STROKE_DECKEY_END) {
                    CombinationKeyStroke.Determiner.Singleton.KeyUp(deckey, bDecoderOn);
                }
            }
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"LEAVE: result={false}");
        }

        private void setInvokeHandlerToDeterminer()
        {
            CombinationKeyStroke.Determiner.Singleton.KeyProcHandler = (keyList, bUncond) => invokeHandlerForKeyList(keyList, bUncond);
        }

        private bool invokeHandlerForKeyList(List<int> keyList, bool bUnconditional)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"ENTER: keyList={(keyList._isEmpty() ? "(empty)" : keyList.Select(x => x.ToString())._join(":"))}");
            bool result = true;
            if (keyList._notEmpty()) {
                foreach (var k in keyList) {
                    result = invokeHandler(k, -1, 0, bUnconditional) && result;
                }
            }
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"LEAVE: result={result}");
            return result;
        }

        /// <summary> キーボードハンドラの処理中か </summary>
        private bool bInvokeHandlerBusy = false;

        private bool invokeHandler(int kanchokuCode, int normalDecKey, uint mod, bool bUnconditional = false)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() =>
                $"ENTER: kanchokuCode={kanchokuCode:x}H({kanchokuCode}), mod={mod:x}H({mod}), bUnconditional={bUnconditional}, " +
                $"UNCONDITIONAL_DECKEY_OFFSET={DecoderKeys.UNCONDITIONAL_DECKEY_OFFSET}, UNCONDITIONAL_DECKEY_END={DecoderKeys.UNCONDITIONAL_DECKEY_END}");

            bool result = false;

            if (bInvokeHandlerBusy) {
                logger.WarnH("Handler Busy");
            } else {
                bInvokeHandlerBusy = true;
                result = _invokeHandler(kanchokuCode, normalDecKey, mod, bUnconditional);
            }
            bInvokeHandlerBusy = false;

            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"LEAVE: result={result}");

            return result;
        }

        private bool _invokeHandler(int kanchokuCode, int normalDecKey, uint mod, bool bUnconditional)
        {
            switch (kanchokuCode) {
                case DecoderKeys.TOGGLE_DECKEY:
                    frmKanchoku?.ToggleDecoder(0);
                    return true;
                case DecoderKeys.MODE_TOGGLE_FOLLOW_CARET_DECKEY:
                case DecoderKeys.MODE_TOGGLE_FOLLOW_CARET_DECKEY2:
                    Settings.VirtualKeyboardPosFixedTemporarily = false;
                    frmKanchoku?.ToggleDecoder(kanchokuCode == DecoderKeys.MODE_TOGGLE_FOLLOW_CARET_DECKEY ? 1 : 2);
                    return true;
                case DecoderKeys.ACTIVE_DECKEY:
                case DecoderKeys.ACTIVE2_DECKEY:
                    frmKanchoku?.ActivateDecoder();
                    return true;
                case DecoderKeys.DEACTIVE_DECKEY:
                case DecoderKeys.DEACTIVE2_DECKEY:
                    frmKanchoku?.DeactivateDecoder();
                    return true;
                case -1:
                    return frmKanchoku?.FuncDispatcher(DecoderKeys.UNDEFINED_DECKEY, normalDecKey, mod) ?? false;
                default:
                    if (kanchokuCode >= DecoderKeys.UNCONDITIONAL_DECKEY_OFFSET && kanchokuCode < DecoderKeys.UNCONDITIONAL_DECKEY_END) {
                        if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"InvokeDecoderUnconditionally: kanchokuCode={kanchokuCode}");
                        return frmKanchoku?.InvokeDecoderUnconditionally(kanchokuCode - DecoderKeys.UNCONDITIONAL_DECKEY_OFFSET, mod) ?? false;
                    }
                    if (bUnconditional) {
                        if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"InvokeDecoderUnconditionally: kanchokuCode={kanchokuCode}, bUncond={bUnconditional}");
                        return frmKanchoku?.InvokeDecoderUnconditionally(kanchokuCode, mod) ?? false;
                    }
                    if (kanchokuCode >= 0) {
                        if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"FuncDispatcher: kanchokuCode={kanchokuCode}");
                        return frmKanchoku?.FuncDispatcher(kanchokuCode, normalDecKey, mod) ?? false;
                    }
                    return false;
            }
        }

    }
}

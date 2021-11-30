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

        /// <summary>デコーダ機能のディスパッチ</summary>
        public delegate bool DelegateDecoderFuncDispatcher(int deckey, uint mod);

        /// <summary>修飾キー付きvkeyをSendInputする</summary>
        public delegate bool DelegateSendInputVkeyWithMod(uint mod, uint vkey);

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

        /// <summary>デコーダ機能のディスパッチ</summary>
        public DelegateDecoderFuncDispatcher FuncDispatcher { get; set; }

        /// <summary>修飾キー付きvkeyをSendInputする</summary>
        public DelegateSendInputVkeyWithMod SendInputVkeyWithMod { get; set; }

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

        private bool isEffectiveVkey(int vkey, int scanCode, int extraInfo, bool ctrl)
        {
            // 0xa0 = LSHIFT, 0xa1 = RSHIFT, 0xa5 = RMENU, 0xf3 = Zenkaku, 0xf4 = Kanji
            return
                (Settings.IgnoreOtherHooker ? extraInfo == 0 : extraInfo != ActiveWindowHandler.MyMagicNumber) &&
                scanCode != 0 && scanCode != YamabukiRscanCode &&
                ((vkey >= 0 && vkey < 0xa0) ||
                 (vkey == VirtualKeys.RSHIFT && !ctrl && (Settings.ActiveKey == (uint)vkey || isDecoderActivated()) && Settings.ExtraModifiersEnabled) ||
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
        //    return (!bWithOutCtrl && spaceKeyState == SpecialKeyState.SHIFTED) || (GetAsyncKeyState(VirtualKeys.LSHIFT) & 0x8000) != 0 || (GetAsyncKeyState(VirtualKeys.RSHIFT) & 0x8000) != 0;
        //}

        private bool shiftKeyPressed(uint vkey)
        {
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
        enum SpecialKeyState {
            RELEASED,
            PRESSED,
            SHIFTED,
            REPEATED,
        }
        /// <summary> スペースキーの押下状態</summary>
        private SpecialKeyState spaceKeyState = SpecialKeyState.RELEASED;

        /// <summary> CapsLockキーの押下状態</summary>
        private SpecialKeyState capsKeyState = SpecialKeyState.RELEASED;

        /// <summary> 英数キーの押下状態</summary>
        private SpecialKeyState alnumKeyState = SpecialKeyState.RELEASED;

        /// <summary> 無変換キーの押下状態</summary>
        private SpecialKeyState nferKeyState = SpecialKeyState.RELEASED;

        /// <summary> 変換キーの押下状態</summary>
        private SpecialKeyState xferKeyState = SpecialKeyState.RELEASED;

        /// <summary> RShiftキーの押下状態</summary>
        private SpecialKeyState rshiftKeyState = SpecialKeyState.RELEASED;

        /// <summary> 拡張修飾キーのシフト状態を得る</summary>
        private uint getShiftedSpecialModKey()
        {
            if (spaceKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_SPACE;
            if (capsKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_CAPS;
            if (alnumKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_ALNUM;
            if (nferKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_NFER;
            if (xferKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_XFER;
            if (rshiftKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_RSHIFT;
            return 0;
        }

        /// <summary> 拡張修飾キーの押下またシフト状態を得る</summary>
        private uint getPressedOrShiftedSpecialModKey()
        {
            if (spaceKeyState == SpecialKeyState.PRESSED || spaceKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_SPACE;
            if (capsKeyState == SpecialKeyState.PRESSED || capsKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_CAPS;
            if (alnumKeyState == SpecialKeyState.PRESSED || alnumKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_ALNUM;
            if (nferKeyState == SpecialKeyState.PRESSED || nferKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_NFER;
            if (xferKeyState == SpecialKeyState.PRESSED || xferKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_XFER;
            if (rshiftKeyState == SpecialKeyState.PRESSED || rshiftKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_RSHIFT;
            return 0;
        }

        private void makeSpecialModKeyShifted()
        {
            if (spaceKeyState == SpecialKeyState.PRESSED) spaceKeyState = SpecialKeyState.SHIFTED;
            if (capsKeyState == SpecialKeyState.PRESSED) capsKeyState = SpecialKeyState.SHIFTED;
            if (alnumKeyState == SpecialKeyState.PRESSED) alnumKeyState = SpecialKeyState.SHIFTED;
            if (nferKeyState == SpecialKeyState.PRESSED) nferKeyState = SpecialKeyState.SHIFTED;
            if (xferKeyState == SpecialKeyState.PRESSED) xferKeyState = SpecialKeyState.SHIFTED;
            if (rshiftKeyState == SpecialKeyState.PRESSED) rshiftKeyState = SpecialKeyState.SHIFTED;
        }

        private VirtualKeys.ShiftPlane getShiftPlane()
        {
            bool bDecoderOn = isDecoderActivated();
            if (spaceKeyState == SpecialKeyState.SHIFTED) {
                var plane = VirtualKeys.GetShiftPlaneFromShiftFuncKeyModFlag(KeyModifiers.MOD_SPACE, bDecoderOn);
                if (plane == VirtualKeys.ShiftPlane.NONE && isSandSEnabled()) plane = VirtualKeys.ShiftPlane.NormalPlane;
                return plane;
            }
            if (capsKeyState == SpecialKeyState.SHIFTED) return VirtualKeys.GetShiftPlaneFromShiftFuncKeyModFlag(KeyModifiers.MOD_CAPS, bDecoderOn);
            if (alnumKeyState == SpecialKeyState.SHIFTED) return VirtualKeys.GetShiftPlaneFromShiftFuncKeyModFlag(KeyModifiers.MOD_ALNUM, bDecoderOn);
            if (nferKeyState == SpecialKeyState.SHIFTED) return VirtualKeys.GetShiftPlaneFromShiftFuncKeyModFlag(KeyModifiers.MOD_NFER, bDecoderOn);
            if (xferKeyState == SpecialKeyState.SHIFTED) return VirtualKeys.GetShiftPlaneFromShiftFuncKeyModFlag(KeyModifiers.MOD_XFER, bDecoderOn);
            if (rshiftKeyState == SpecialKeyState.SHIFTED) return VirtualKeys.GetShiftPlaneFromShiftFuncKeyModFlag(KeyModifiers.MOD_RSHIFT, bDecoderOn);
            return VirtualKeys.ShiftPlane.NONE;
        }

        /// <summary>
        /// SandS と同じシフト面を使う拡張シフトキーか
        /// </summary>
        /// <returns></returns>
        private bool isSameShiftKeyAsSandS(uint fkey, bool bDecoderOn)
        {
            if (Settings.LoggingDecKeyInfo) logger.DebugH($"fkey={fkey:x}H");
            var plane_sands = VirtualKeys.GetShiftPlaneFromShiftFuncKeyModFlag(KeyModifiers.MOD_SPACE, bDecoderOn);
            if (fkey != 0) {
                var plane_fkey = VirtualKeys.GetShiftPlaneFromShiftFuncKeyModFlag(fkey, bDecoderOn);
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
            return isSameShiftKeyAsSandS(getPressedOrShiftedSpecialModKey(), bDecoderOn);
        }

        /// <summary>
        /// SandS と同じシフト面を使う拡張シフトキーがシフト状態か
        /// </summary>
        /// <returns></returns>
        private bool isSameShiftKeyAsSandSShifted(bool bDecoderOn)
        {
            return isSameShiftKeyAsSandS(getShiftedSpecialModKey(), bDecoderOn);;
        }

        private int getShiftPlaneDeckeyForSandS(bool bDecoderOn)
        {
            switch (VirtualKeys.GetShiftPlaneFromShiftFuncKeyModFlag(KeyModifiers.MOD_SPACE, bDecoderOn)) {
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
        private bool normalInfoKeyDownResult = false;

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
                logger.DebugH(() => $"\nENTER: vkey={vkey:x}H({vkey}), scanCode={scanCode:x}H, extraInfo={extraInfo}"
                + $"\nspaceKeyState={spaceKeyState}"
                + $"\ncapsKeyState={capsKeyState}"
                + $"\nalnumKeyState={alnumKeyState}"
                + $"\nnferKeyState={nferKeyState}"
                + $"\nxferKeyState={xferKeyState}"
                + $"\nrshiftKeyState={rshiftKeyState}\n");
            }

            normalInfoKeyDownResult = false;

            void handleKeyDown()
            {
                bool leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
                bool rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;
                bool bCtrl = leftCtrl || rightCtrl;

                if (!isEffectiveVkey(vkey, scanCode, extraInfo, bCtrl)) {
                    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"not EffectiveVkey");
                    return;
                }

                if ((uint)vkey == VirtualKeys.RSHIFT && Settings.ActiveKey == (uint)vkey) {
                    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"RShift is ActiveKey");
                    rshiftKeyState = SpecialKeyState.PRESSED;
                    return;
                }
                bool bDecoderOn = isDecoderActivated();
                if (Settings.ExtraModifiersEnabled) {
                    normalInfoKeyDownResult = true;
                    // CapsLock
                    if (vkey == (int)VirtualKeys.CapsLock) {
                        if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"CapsLockKey Pressed");
                        if (VirtualKeys.IsShiftPlaneAssignedForShiftFuncKeyByModFlag(KeyModifiers.MOD_CAPS, bDecoderOn)) {
                            if (capsKeyState == SpecialKeyState.PRESSED || getPressedOrShiftedSpecialModKey() != 0) {
                                // Capsキーが押下されている、またはその他の拡張修飾キーが押下orシフト状態なら、シフト状態に遷移する
                                capsKeyState = SpecialKeyState.SHIFTED;
                                makeSpecialModKeyShifted();
                                return;
                            }
                            if (capsKeyState == SpecialKeyState.SHIFTED) return; // SHIFT状態なら何もしない

                            // RELEASED
                            capsKeyState = SpecialKeyState.PRESSED;
                            return;
                        }
                    } else {
                        if (capsKeyState == SpecialKeyState.PRESSED) {
                            // Capsキーが押下されている状態でその他のキーが押されたら、シフト状態に遷移する
                            capsKeyState = SpecialKeyState.SHIFTED;
                        }
                    }
                    // AlphaNum
                    if (vkey == (int)VirtualKeys.AlphaNum) {
                        if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"AlpahNumKey Pressed");
                        if (VirtualKeys.IsShiftPlaneAssignedForShiftFuncKeyByModFlag(KeyModifiers.MOD_ALNUM, bDecoderOn)) {
                            if (alnumKeyState == SpecialKeyState.PRESSED || getPressedOrShiftedSpecialModKey() != 0) {
                                // 英数キーが押下されている、またはその他の拡張修飾キーが押下orシフト状態なら、シフト状態に遷移する
                                alnumKeyState = SpecialKeyState.SHIFTED;
                                makeSpecialModKeyShifted();
                                return;
                            }
                            if (alnumKeyState == SpecialKeyState.SHIFTED) return; // SHIFT状態なら何もしない

                            // RELEASED
                            alnumKeyState = SpecialKeyState.PRESSED;
                            return;
                        }
                    } else {
                        if (alnumKeyState == SpecialKeyState.PRESSED) {
                            // 英数キーが押下されている状態でその他のキーが押されたら、シフト状態に遷移する
                            alnumKeyState = SpecialKeyState.SHIFTED;
                        }
                    }
                    // Nfer
                    if (vkey == (int)VirtualKeys.Nfer) {
                        if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"NferKey Pressed");
                        if (VirtualKeys.IsShiftPlaneAssignedForShiftFuncKeyByModFlag(KeyModifiers.MOD_NFER, bDecoderOn)) {
                            if (nferKeyState == SpecialKeyState.PRESSED || getPressedOrShiftedSpecialModKey() != 0) {
                                // 無変換キーが押下されている、またはその他の拡張修飾キーが押下orシフト状態なら、シフト状態に遷移する
                                nferKeyState = SpecialKeyState.SHIFTED;
                                makeSpecialModKeyShifted();
                                return;
                            }
                            if (nferKeyState == SpecialKeyState.SHIFTED) return; // SHIFT状態なら何もしない

                            // RELEASED
                            nferKeyState = SpecialKeyState.PRESSED;
                            return;
                        }
                    } else {
                        if (nferKeyState == SpecialKeyState.PRESSED) {
                            // 無変換キーが押下されている状態でその他のキーが押されたら、シフト状態に遷移する
                            nferKeyState = SpecialKeyState.SHIFTED;
                        }
                    }
                    // Xfer
                    if (vkey == (int)VirtualKeys.Xfer) {
                        if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"XferKey Pressed");
                        if (VirtualKeys.IsShiftPlaneAssignedForShiftFuncKeyByModFlag(KeyModifiers.MOD_XFER, bDecoderOn)) {
                            if (xferKeyState == SpecialKeyState.PRESSED || getPressedOrShiftedSpecialModKey() != 0) {
                                // 変換キーが押下されている、またはその他の拡張修飾キーが押下orシフト状態なら、シフト状態に遷移する
                                xferKeyState = SpecialKeyState.SHIFTED;
                                makeSpecialModKeyShifted();
                                return;
                            }
                            if (xferKeyState == SpecialKeyState.SHIFTED) return; // SHIFT状態なら何もしない

                            // RELEASED
                            xferKeyState = SpecialKeyState.PRESSED;
                            return;
                        }
                    } else {
                        if (xferKeyState == SpecialKeyState.PRESSED) {
                            // 変換キーが押下されている状態でその他のキーが押されたら、シフト状態に遷移する
                            xferKeyState = SpecialKeyState.SHIFTED;
                        }
                    }
                    // Rshift
                    if (vkey == (int)VirtualKeys.RSHIFT) {
                        if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"RshiftKey Pressed");
                        if (VirtualKeys.IsShiftPlaneAssignedForShiftFuncKeyByModFlag(KeyModifiers.MOD_RSHIFT, bDecoderOn)) {
                            if (rshiftKeyState == SpecialKeyState.PRESSED || getPressedOrShiftedSpecialModKey() != 0) {
                                // RSHIFTキーが押下されている、またはその他の拡張修飾キーが押下orシフト状態なら、シフト状態に遷移する
                                rshiftKeyState = SpecialKeyState.SHIFTED;
                                return;
                            }
                            if (rshiftKeyState == SpecialKeyState.SHIFTED) return; // SHIFT状態なら何もしない

                            // RELEASED
                            rshiftKeyState = SpecialKeyState.PRESSED;
                            return;
                        }
                    } else {
                        if (rshiftKeyState == SpecialKeyState.PRESSED) {
                            // RSHIFTキーが押下されている状態でその他のキーが押されたら、シフト状態に遷移する
                            rshiftKeyState = SpecialKeyState.SHIFTED;
                        }
                    }
                }
                // SandS
                if (isSandSEnabled()) {
                    normalInfoKeyDownResult = true;
                    if (vkey == (int)Keys.Space) {
                        // スペースキーが押された
                        // SandSと同じシフト面を使う左Shiftまたは拡張修飾キーがシフト状態か(何か(拡張)シフトキーが Pressed だったら、Spaceキーが押されたことで Shifted に移行しているはず)
                        bool bShiftOnSamePlane = isSameShiftKeyAsSandSShifted(bDecoderOn);
                        if (bShiftOnSamePlane) {
                            // SandSと同じシフト面を使う拡張修飾キーがシフト状態なら、シフト状態に遷移する
                            spaceKeyState = SpecialKeyState.SHIFTED;
                            return;
                        }
                        if (Settings.IgnoreSpaceUpOnSandS && bCtrl) {
                            // SandS時に1回目のSpace単打を無視する設定の場合は、Ctrl+Space をシフト状態に遷移させる
                            spaceKeyState = SpecialKeyState.SHIFTED;
                            return;
                        }
                        if (spaceKeyState == SpecialKeyState.PRESSED) {
                            // すでにスペースキーが押下されている
                            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"prevSpaceUpDt={prevSpaceUpDt}.{prevSpaceUpDt:fff}");
                            if (Settings.SandSEnableSpaceOrRepeatMillisec <= 0 ||
                                DateTime.Now > prevSpaceUpDt.AddMilliseconds(Settings.SandSEnableSpaceOrRepeatMillisec + KEY_REPEAT_INTERVAL)) {
                                spaceKeyState = SpecialKeyState.SHIFTED;
                                //makeSpecialModKeyShifted();
                                // 後置シフトキーを送出する
                                if (Settings.SandSEnablePostShift && bDecoderOn) invokeHandlerForPostSandSKey();
                                normalInfoKeyDownResult = true;
                                return;
                            }
                            spaceKeyState = SpecialKeyState.REPEATED;
                        }
                        if (spaceKeyState == SpecialKeyState.SHIFTED) return; // SHIFT状態なら何もしない

                        if (spaceKeyState != SpecialKeyState.REPEATED) {
                            // RELEASED
                            bool bShift = shiftKeyPressed((uint)vkey);
                            bool bShiftEx = getShiftedSpecialModKey() != 0;
                            if (!bCtrl && !bShift && !bShiftEx) {
                                // 1回目の押下で Ctrl も Shift も他のmodiferも押されてない場合は、PRESSED に移行
                                spaceKeyState = SpecialKeyState.PRESSED;
                                return;
                            }
                            //// シフト面が同一である本来のShiftまたは拡張シフト押下時のSpaceは、Shiftとして扱う
                            //if (bShiftOnSamePlane) {
                            //    spaceKeyState = SpecialKeyState.SHIFTED;
                            //    return;
                            //}
                        }
                        // 上記以外はスペース入力として扱う
                    } else {
                        if (spaceKeyState == SpecialKeyState.PRESSED) {
                            // スペースキーが押下されている状態でその他のキーが押されたら、シフト状態に遷移する
                            spaceKeyState = SpecialKeyState.SHIFTED;
                            // 後置シフトキーを送出する
                            if (Settings.SandSEnablePostShift && bDecoderOn) invokeHandlerForPostSandSKey();
                            normalInfoKeyDownResult = true;
                        }
                    }
                }

                if (Settings.LoggingDecKeyInfo) {
                    logger.DebugH(() => $"CALL: keyboardDownHandler({vkey}, {leftCtrl}, {rightCtrl})\nspaceKeyState={spaceKeyState}"
                    + $"\ncapsKeyState={capsKeyState}"
                    + $"\nalnumKeyState={alnumKeyState}"
                    + $"\nnferKeyState={nferKeyState}"
                    + $"\nxferKeyState={xferKeyState}"
                    + $"\nrshiftKeyState={rshiftKeyState}\n");
                }
                normalInfoKeyDownResult = keyboardDownHandler(vkey, leftCtrl, rightCtrl);
            }

            handleKeyDown();
            if (Settings.LoggingDecKeyInfo) {
                logger.DebugH(() => $"LEAVE: result={normalInfoKeyDownResult}, vkey={vkey:x}H({vkey}), extraInfo={extraInfo}, spaceKeyState={spaceKeyState}"
                    + $"\nspaceKeyState={spaceKeyState}"
                    + $"\ncapsKeyState={capsKeyState}"
                    + $"\nalnumKeyState={alnumKeyState}"
                    + $"\nnferKeyState={nferKeyState}"
                    + $"\nxferKeyState={xferKeyState}"
                    + $"\nrshiftKeyState={rshiftKeyState}\n");
            }
            return normalInfoKeyDownResult;
        }

        /// <summary>キーボード押下時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private bool keyboardDownHandler(int vkey, bool leftCtrl, bool rightCtrl)
        {
            //bool leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
            //bool rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;
            bool ctrl = leftCtrl || rightCtrl;
            bool shift = shiftKeyPressed((uint)vkey);
            uint mod = KeyModifiers.MakeModifier(ctrl, shift);
            uint modEx = getShiftedSpecialModKey();
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"ENTER: mod={mod:x}H({mod}), modEx={modEx:x}H({modEx}), vkey={vkey:x}H({vkey}), ctrl={ctrl}, shift={shift}");

            int kanchokuCode = -1;
            if (modEx != 0 && !ctrl) {
                // 拡張シフトが有効なのは、Ctrlキーが押されていない場合とする
                kanchokuCode = VirtualKeys.GetModConvertedDecKeyFromCombo(modEx, (uint)vkey);
                if (kanchokuCode < 0) {
                    // 拡張シフト面のコードを得る
                    var shiftPlane = getShiftPlane();
                    if (shiftPlane != VirtualKeys.ShiftPlane.NONE) kanchokuCode = VirtualKeys.GetDecKeyFromCombo(0, (uint)vkey) + (int)shiftPlane * DecoderKeys.SHIFT_DECKEY_NUM;
                }
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"PATH-A: kanchokuCode={kanchokuCode:x}H({kanchokuCode}), modEx={modEx:x}, ctrl={ctrl}, shift={shift}");
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
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"PATH-B: kanchokuCode={kanchokuCode:x}H({kanchokuCode}), ctrl={ctrl}, shift={shift}");
            }

            //if (kanchokuCode < 0) {
            //    bool result = false;
            //    if (spaceKeyState == SpecialKeyState.SHIFTED) {
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
            if (bHandlerBusy) {
                if (vkeyQueue.Count < vkeyQueueMaxSize) {
                    vkeyQueue.Enqueue(vkey);
                    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"vkeyQueue.Count={vkeyQueue.Count}");
                }
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"LEAVE-C: result=True");
                return true;
            }
            while (true) {
                bool result = invokeHandler(kanchokuCode, mod);
                if (vkeyQueue.Count == 0) {
                    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"LEAVE-D: result={result}");
                    return result;
                }
                kanchokuCode = vkeyQueue.Dequeue();
            }
        }

        private const int vkeyQueueMaxSize = 4;

        private Queue<int> vkeyQueue = new Queue<int>();

        /// <summary>キーアップ時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <param name="extraInfo"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private bool onKeyboardUpHandler(int vkey, int scanCode, int extraInfo)
        {
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"\nENTER: vkey={vkey:x}H({vkey}), scanCode={scanCode:x}H, extraInfo={extraInfo}");

            bool leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
            bool rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;

            if (!isEffectiveVkey(vkey, scanCode, extraInfo, leftCtrl || rightCtrl)) {
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"LEAVE: result=False, not EffectiveVkey");
                return false;
            }

            bool bDecoderOn = isDecoderActivated();
            bool result = false;
            if (Settings.SandSEnabled || Settings.SandSEnabledWhenOffMode) {
                if (vkey == (int)Keys.Space) {
                    var state = spaceKeyState;
                    spaceKeyState = SpecialKeyState.RELEASED;
                    var dtLimit = prevSpaceUpDt.AddMilliseconds(Settings.SandSEnableSpaceOrRepeatMillisec);
                    var dtNow = DateTime.Now;
                    if (state == SpecialKeyState.PRESSED) prevSpaceUpDt = dtNow;
                    if ((Settings.IgnoreSpaceUpOnSandS && dtNow > dtLimit) || getShiftedSpecialModKey() != 0) {
                        // SandS時のSpaceUpを無視する設定で前回のSpace打鍵から指定のms以上経過していたか、または何か拡張シフト状態だったら、Spaceキーは無視
                        return true;
                    } else if (state == SpecialKeyState.PRESSED) {
                        // Spaceキーが1回押されただけの状態なら、Spaceキーを送出
                        return keyboardDownHandler(vkey, leftCtrl, rightCtrl);
                    }
                    return false;
                }
            }
            if (vkey == (int)VirtualKeys.CapsLock) {
                var state = capsKeyState;
                capsKeyState = SpecialKeyState.RELEASED;
                if (Settings.ExtraModifiersEnabled && VirtualKeys.IsShiftPlaneAssignedForShiftFuncKeyByModFlag(KeyModifiers.MOD_CAPS, bDecoderOn)) {
                    if (state == SpecialKeyState.SHIFTED) {
                        return true;
                    } else if (state == SpecialKeyState.PRESSED) {
                        return keyboardDownHandler(vkey, leftCtrl, rightCtrl);
                    }
                }
            }
            if (vkey == (int)VirtualKeys.AlphaNum) {
                var state = alnumKeyState;
                alnumKeyState = SpecialKeyState.RELEASED;
                if (Settings.ExtraModifiersEnabled && VirtualKeys.IsShiftPlaneAssignedForShiftFuncKeyByModFlag(KeyModifiers.MOD_ALNUM, bDecoderOn)) {
                    if (state == SpecialKeyState.SHIFTED) {
                        return true;
                    } else if (state == SpecialKeyState.PRESSED) {
                        return keyboardDownHandler(vkey, leftCtrl, rightCtrl);
                    }
                }
            }
            if (vkey == (int)VirtualKeys.Nfer) {
                var state = nferKeyState;
                nferKeyState = SpecialKeyState.RELEASED;
                if (Settings.ExtraModifiersEnabled && VirtualKeys.IsShiftPlaneAssignedForShiftFuncKeyByModFlag(KeyModifiers.MOD_NFER, bDecoderOn)) {
                    if (state == SpecialKeyState.PRESSED) {
                        keyboardDownHandler(vkey, leftCtrl, rightCtrl);
                    }
                }
                return false;
            }
            if (vkey == (int)VirtualKeys.Xfer) {
                var state = xferKeyState;
                xferKeyState = SpecialKeyState.RELEASED;
                if (Settings.ExtraModifiersEnabled && VirtualKeys.IsShiftPlaneAssignedForShiftFuncKeyByModFlag(KeyModifiers.MOD_XFER, bDecoderOn)) {
                    if (state == SpecialKeyState.PRESSED) {
                        keyboardDownHandler(vkey, leftCtrl, rightCtrl);
                    }
                }
                return false;
            }
            if (vkey == (int)VirtualKeys.RSHIFT) {
                var state = rshiftKeyState;
                rshiftKeyState = SpecialKeyState.RELEASED;
                if (Settings.ActiveKey == (uint)vkey || (Settings.ExtraModifiersEnabled && VirtualKeys.IsShiftPlaneAssignedForShiftFuncKeyByModFlag(KeyModifiers.MOD_RSHIFT, bDecoderOn))) {
                    if (state == SpecialKeyState.PRESSED) {
                        keyboardDownHandler(vkey, leftCtrl, rightCtrl);
                    }
                }
                return false;
            }

            // キーアップ時はなにもしない
            //try {
            //    bHandlerBusy = true;
            //    result = OnKeyUp?.Invoke(vkey, extraInfo) ?? false;
            //} finally {
            //    bHandlerBusy = false;
            //}
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"LEAVE: result={result}");
            return result;
        }

        private bool invokeHandler(int kanchokuCode, uint mod)
        {
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"ENTER: kanchokuCode={kanchokuCode:x}H({kanchokuCode}), mod={mod:x}H({mod})");
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
                        if (kanchokuCode >= 0) return FuncDispatcher?.Invoke(kanchokuCode, mod) ?? false;
                        return false;
                }
            } finally {
                bHandlerBusy = false;
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"LEAVE");
            }
        }

    }
}

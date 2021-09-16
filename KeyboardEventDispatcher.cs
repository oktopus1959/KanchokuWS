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
        public delegate bool DelegateDecoderFuncDispatcher(int deckey);

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

        /// <summary>デコーダ機能のディスパッチ</summary>
        public DelegateDecoderFuncDispatcher FuncDispatcher { get; set; }

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

        private bool isEffectiveVkey(int vkey, int extraInfo)
        {
            return (vkey >= 0 && vkey < 0xa0 || vkey >= 0xa6 && vkey < vkeyNum) && extraInfo != ActiveWindowHandler.MyMagicNumber;    // 0xa0 = LSHIFT, 0xa5 = RMENU
        }
        private bool ctrlKeyPressed()
        {
            return (Settings.UseLeftControlToConversion && (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0)
                || (Settings.UseRightControlToConversion && (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0);
        }

        private bool shiftKeyPressed(bool bCtrl)
        {
            return (!bCtrl && spaceKeyState == SpecialKeyState.SHIFTED) || (GetAsyncKeyState(VirtualKeys.LSHIFT) & 0x8000) != 0 || (GetAsyncKeyState(VirtualKeys.RSHIFT) & 0x8000) != 0;
        }

        /// <summary> 特殊キーの押下状態</summary>
        enum SpecialKeyState {
            RELEASED,
            PRESSED,
            SHIFTED,
        }
        /// <summary> スペースキーの押下状態</summary>
        private SpecialKeyState spaceKeyState = SpecialKeyState.RELEASED;

        /// <summary> 全角キーの押下状態</summary>
        private SpecialKeyState zenkakuKeyState = SpecialKeyState.RELEASED;

        /// <summary> 無変換キーの押下状態</summary>
        private SpecialKeyState nferKeyState = SpecialKeyState.RELEASED;

        /// <summary> 変換キーの押下状態</summary>
        private SpecialKeyState xferKeyState = SpecialKeyState.RELEASED;

        /// <summary> かなキーの押下状態</summary>
        private SpecialKeyState kanaKeyState = SpecialKeyState.RELEASED;

        private uint getShiftedSpecialModKey()
        {
            if (zenkakuKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_ZENKAKU;
            if (nferKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_NFER;
            if (xferKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_XFER;
            if (kanaKeyState == SpecialKeyState.SHIFTED) return KeyModifiers.MOD_KANA;
            return 0;
        }

        /// <summary>キーボード押下時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <param name="extraInfo"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private bool onKeyboardDownHandler(int vkey, int extraInfo)
        {
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"\nCALLED: vkey={vkey:x}H({vkey}), extraInfo={extraInfo}");
            if (isEffectiveVkey(vkey, extraInfo)) {
                if (Settings.SandSEnabled) {
                    if (vkey == (int)Keys.Space) {
                        if (IsDecoderActivated?.Invoke() ?? false) {
                            if (spaceKeyState == SpecialKeyState.PRESSED) {
                                // スペースキーが押下されている状態なら、シフト状態に遷移する
                                spaceKeyState = SpecialKeyState.SHIFTED;
                                return true;
                            }
                            if (spaceKeyState == SpecialKeyState.SHIFTED) return true; // SHIFT状態なら何もしない
                                                                                     // RELEASED
                            if (!ctrlKeyPressed() && !shiftKeyPressed(false)) {
                                // 1回目の押下で Ctrl も Shift も押されてない
                                spaceKeyState = SpecialKeyState.PRESSED;
                                return true;
                            }
                            if (shiftKeyPressed(false)) {
                                spaceKeyState = SpecialKeyState.SHIFTED;
                                return true;
                            }
                            // 上記以外はスペース入力として扱う
                        }
                    } else {
                        if (spaceKeyState == SpecialKeyState.PRESSED) {
                            // スペースキーが押下されている状態でその他のキーが押されたら、シフト状態に遷移する
                            spaceKeyState = SpecialKeyState.SHIFTED;
                        }
                    }
                }
                if (vkey == (int)VirtualKeys.Nfer) {
                    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"NferKey Pressed");
                    if (nferKeyState == SpecialKeyState.PRESSED) {
                        // 全角キーが押下されている状態なら、シフト状態に遷移する
                        nferKeyState = SpecialKeyState.SHIFTED;
                        return true;
                    }
                    if (nferKeyState == SpecialKeyState.SHIFTED) return true; // SHIFT状態なら何もしない
                                                                                 // RELEASED
                    if (!ctrlKeyPressed() && !shiftKeyPressed(false)) {
                        // 1回目の押下で Ctrl も Shift も押されてない
                        nferKeyState = SpecialKeyState.PRESSED;
                        return true;
                    }
                    if (shiftKeyPressed(false)) {
                        nferKeyState = SpecialKeyState.SHIFTED;
                        return true;
                    }
                    // 上記以外は全角キーとして扱う
                } else {
                    if (nferKeyState == SpecialKeyState.PRESSED) {
                        // スペースキーが押下されている状態でその他のキーが押されたら、シフト状態に遷移する
                        nferKeyState = SpecialKeyState.SHIFTED;
                    }
                }
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"nferKeyState={nferKeyState}");
                return keyboardDownHandler(vkey);
            }
            return false;
        }

        /// <summary>キーボード押下時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private bool keyboardDownHandler(int vkey)
        {
            bool leftCtrl = (GetAsyncKeyState(VirtualKeys.LCONTROL) & 0x8000) != 0;
            bool rightCtrl = (GetAsyncKeyState(VirtualKeys.RCONTROL) & 0x8000) != 0;
            bool ctrl = leftCtrl || rightCtrl;
            bool shift = shiftKeyPressed(ctrl);
            uint mod = KeyModifiers.MakeModifier(ctrl, shift);
            if (mod == 0) mod = getShiftedSpecialModKey();
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"mod={mod:x}H({mod}), vkey={vkey:x}H({vkey})");
            int kanchokuCode = (Settings.GlobalCtrlKeysEnabled && ((Settings.UseLeftControlToConversion && leftCtrl) || (Settings.UseRightControlToConversion && rightCtrl))) || mod != 0
                ? VirtualKeys.GetCtrlConvertedDecKeyFromCombo(mod, (uint)vkey)
                : VirtualKeys.GetDecKeyFromCombo(mod, (uint)vkey);

            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"kanchokuCode={kanchokuCode:x}H({kanchokuCode}), ctrl={ctrl}, shift={shift}");
            if (kanchokuCode < 0) return false;

            if (kanchokuCode == DecoderKeys.HISTORY_NEXT_SEARCH_DECKEY && kanchokuCode != VirtualKeys.GetCtrlDecKeyOf(Settings.HistorySearchCtrlKey)) {
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"=historySearchCtrlKey={Settings.HistorySearchCtrlKey}, kanchokuCode={VirtualKeys.GetCtrlDecKeyOf(Settings.HistorySearchCtrlKey)}");
                return false;
            }

            // どうやら KeyboardHook で CallNextHookEx を呼ばないと次のキー入力の処理に移らないみたいだが、
            // 将来必要になるかもしれないので、下記処理を残しておく
            if (busyFlag) {
                if (vkeyQueue.Count < vkeyQueueMaxSize) {
                    vkeyQueue.Enqueue(vkey);
                    if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"vkeyQueue.Count={vkeyQueue.Count}");
                }
                return true;
            }
            while (true) {
                bool result = invokeHandler(kanchokuCode);
                if (vkeyQueue.Count == 0) return result;
                kanchokuCode = vkeyQueue.Dequeue();
            }
        }

        private bool busyFlag = false;

        private const int vkeyQueueMaxSize = 4;

        private Queue<int> vkeyQueue = new Queue<int>();

        /// <summary>キーアップ時のハンドラ</summary>
        /// <param name="vkey"></param>
        /// <param name="extraInfo"></param>
        /// <returns>キー入力を破棄する場合は true を返す。flase を返すとシステム側でキー入力処理が行われる</returns>
        private bool onKeyboardUpHandler(int vkey, int extraInfo)
        {
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"CALLED: vkey={vkey:x}H({vkey}), extraInfo={extraInfo}");
            if (isEffectiveVkey(vkey, extraInfo)) {
                if (Settings.SandSEnabled) {
                    if (vkey == (int)Keys.Space) {
                        var state = spaceKeyState;
                        spaceKeyState = SpecialKeyState.RELEASED;
                        if (state == SpecialKeyState.SHIFTED) {
                            return true;
                        } else if (state == SpecialKeyState.PRESSED) {
                            return keyboardDownHandler(vkey);
                        }
                    }
                }
                if (vkey == (int)VirtualKeys.Nfer) {
                    var state = nferKeyState;
                    nferKeyState = SpecialKeyState.RELEASED;
                    if (state == SpecialKeyState.SHIFTED) {
                        return true;
                    } else if (state == SpecialKeyState.PRESSED) {
                        return keyboardDownHandler(vkey);
                    }
                }
                // キーアップ時はなにもしない
                return OnKeyUp?.Invoke(vkey, extraInfo) ?? false;
            }
            return false;
        }

        private bool invokeHandler(int kanchokuCode)
        {
            if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"ENTER: kanchokuCode={kanchokuCode:x}H({kanchokuCode})");
            busyFlag = true;
            try {
                switch (kanchokuCode) {
                    case DecoderKeys.TOGGLE_DECKEY:
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
                        if (kanchokuCode >= 0) return FuncDispatcher?.Invoke(kanchokuCode) ?? false;
                        return false;
                }
            } finally {
                busyFlag = false;
                if (Settings.LoggingDecKeyInfo) logger.DebugH(() => $"LEAVE");
            }
        }

    }
}

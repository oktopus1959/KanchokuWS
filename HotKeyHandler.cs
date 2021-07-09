using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Utils;

namespace KanchokuWS
{
    public static class HotKeyHandler
    {
        private static Logger logger = Logger.GetLogger();

        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static IntPtr hWnd;

        //private bool bGlobalHotKey = false;

        private static bool bDestroyed = false;

        // グローバルに登録されて特殊ストロークキー
        private static HashSet<int> globalSpecialStrokeHotkeys = new HashSet<int>();

        private static bool[] hotkeyRegistered = new bool[HotKeys.GLOBAL_HOTKEY_ID_END];
        private static bool[] hotkeyUnregisteredTemporary = new bool[HotKeys.GLOBAL_HOTKEY_ID_END];

        private static bool registerHotKey(int id, uint mod, uint vk)
        {
            //if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"id={id:x}H({id}), mod={mod:x}H, vk={vk:x}H({vk})");
            bool result = false;
            if (id >= 0 && id < hotkeyRegistered.Length) {
                if (vk != 0) {
                    result = RegisterHotKey(hWnd, id, mod, vk);
                    if (result) {
                        hotkeyRegistered[id] = result;
                    } else {
                        if (!hotkeyRegistered[id]) logger.Warn(() => $"RESULT={result}: id={id:x}H({id}), mod={mod:x}H, vk={vk:x}H({vk})");
                    }
                } else {
                    hotkeyRegistered[id] = result;
                }
            }
            //if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"RESULT={result}");
            return result;
        }

        private static bool registerHotKey(int id)
        {
            VKeyCombo? combo = VirtualKeys.GetVKeyComboFromHotKey(id);
            return (combo != null) ? registerHotKey(id, combo.Value.modifier, combo.Value.vkey) : false;
        }

        private static bool unregisterHotKey(int id)
        {
            //if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"id={id:x}H({id})");
            bool result = UnregisterHotKey(hWnd, id);
            if (id >= 0 && id < hotkeyRegistered.Length) {
                if (result) {
                    hotkeyRegistered[id] = false;
                } else if (hotkeyRegistered[id]) {
                    logger.Warn(() => $"RESULT={result}: id={id:x}H({id})");
                }
            }
            //if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"RESULT={result}");
            return result;
        }

        public static bool UnregisterHotKeyTemporary(int hotkey)
        {
            if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"CALLED: hotkey={hotkey:x}H({hotkey})");
            if (hotkey >= 0 && hotkey < hotkeyRegistered.Length) {
                if (hotkeyRegistered[hotkey]) {
                    hotkeyUnregisteredTemporary[hotkey] = true;
                    if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"UNREGISTER: hotkey={hotkey:x}H({hotkey})");
                    return unregisterHotKey(hotkey);
                }
            }
            return false;
        }

        public static bool ResumeHotKey(int hotkey)
        {
            if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"CALLED: hotkey={hotkey:x}H({hotkey})");
            if (hotkey >= 0 && hotkey < hotkeyRegistered.Length) {
                if (hotkeyUnregisteredTemporary[hotkey]) {
                    hotkeyUnregisteredTemporary[hotkey] = false;
                    if (Settings.LoggingHotKeyInfo) logger.InfoH(() => $"RESUME: hotkey={hotkey:x}H({hotkey})");
                    return registerHotKey(hotkey);
                }
            }
            return false;
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public static void Initialize(IntPtr hwnd)
        {
            logger.Info($"hWnd={hwnd:x}H");
            hWnd = hwnd;
            EnableGlobalHotKeys();
        }

        public static void Destroy()
        {
            if (!bDestroyed) {
                bDestroyed = true;
                UnregisterCandSelectHotKeys();
                UnregisterSpecialHotKeys();
                UnregisterDecoderHotKeys();
                DisableGlobalHotKeys();
                logger.Info("Disposed");
            }
        }

        /// <summary>
        /// グローバルホットキーの登録
        /// </summary>
        public static void EnableGlobalHotKeys()
        {
            logger.InfoH("ENTER");
            if (Settings.ActiveKey > 0) registerHotKey(HotKeys.ACTIVE_HOTKEY, 0, Settings.ActiveKey);
            if (Settings.ActiveKeyWithCtrl > 0) registerHotKey(HotKeys.ACTIVE2_HOTKEY, KeyModifiers.MOD_CONTROL, Settings.ActiveKeyWithCtrl);
            RegisterSpecialGlobalHotKeys();
            //bGlobalHotKey = true;
            logger.InfoH("LEAVE");
        }

        /// <summary>
        /// グローバルホットキーの解除
        /// </summary>
        public static void DisableGlobalHotKeys()
        {
            logger.InfoH("CALLED");
            unregisterHotKey(HotKeys.ACTIVE_HOTKEY);
            unregisterHotKey(HotKeys.ACTIVE2_HOTKEY);
            unregisterHotKey(HotKeys.INACTIVE_HOTKEY);
            unregisterHotKey(HotKeys.INACTIVE2_HOTKEY);
            UnregisterSpecialGlobalHotKeys();
            //bGlobalHotKey = false;
        }


        /// <summary>
        /// デコーダON用のグローバルホットキーの登録
        /// </summary>
        public static void RegisterActivateHotKeys()
        {
            logger.InfoH("ENTER");
            unregisterHotKey(HotKeys.INACTIVE_HOTKEY);
            unregisterHotKey(HotKeys.INACTIVE2_HOTKEY);
            if (Settings.ActiveKey > 0) registerHotKey(HotKeys.ACTIVE_HOTKEY, 0, Settings.ActiveKey);
            if (Settings.ActiveKeyWithCtrl > 0) registerHotKey(HotKeys.ACTIVE2_HOTKEY, KeyModifiers.MOD_CONTROL, Settings.ActiveKeyWithCtrl);
            logger.InfoH("LEAVE");
        }

        /// <summary>
        /// デコーダOFF用のグローバルホットキーの登録
        /// </summary>
        public static void RegisterDeactivateHotKeys()
        {
            logger.InfoH("ENTER");
            unregisterHotKey(HotKeys.ACTIVE_HOTKEY);
            unregisterHotKey(HotKeys.ACTIVE2_HOTKEY);
            if (Settings.DeactiveKeyEffective > 0) registerHotKey(HotKeys.INACTIVE_HOTKEY, 0, Settings.DeactiveKeyEffective);
            if (Settings.DeactiveKeyWithCtrlEffective > 0) registerHotKey(HotKeys.INACTIVE2_HOTKEY, KeyModifiers.MOD_CONTROL, Settings.DeactiveKeyWithCtrlEffective);
            logger.InfoH("LEAVE");
        }


        /// <summary>
        /// グローバル特殊ホットキーの登録
        /// RegisterやUnregisterは、呼んだThreadに対して有効になるらしい
        /// </summary>
        public static void RegisterSpecialGlobalHotKeys()
        {
            logger.InfoH("ENTER");
            //registerHotKey(HotKeys.CTRL_G_HOTKEY);
            //registerHotKey(HotKeys.CTRL_SHIFT_G_HOTKEY);

            globalSpecialStrokeHotkeys.Clear();

            if (Settings.ConvertCtrlAtoHomeEffective) {
                globalSpecialStrokeHotkeys.Add(HotKeys.HOTKEY_A);
                registerHotKey(HotKeys.HOTKEY_A);
                registerHotKey(HotKeys.CTRL_A_HOTKEY);
            }
            if (Settings.ConvertCtrlDtoDeleteEffective) {
                globalSpecialStrokeHotkeys.Add(HotKeys.HOTKEY_D);
                registerHotKey(HotKeys.HOTKEY_D);
                registerHotKey(HotKeys.CTRL_D_HOTKEY);
            }
            if (Settings.ConvertCtrlEtoEndEffective) {
                globalSpecialStrokeHotkeys.Add(HotKeys.HOTKEY_E);
                registerHotKey(HotKeys.HOTKEY_E);
                registerHotKey(HotKeys.CTRL_E_HOTKEY);
            }
            if (Settings.ConvertCtrlHtoBackSpaceEffective) {
                globalSpecialStrokeHotkeys.Add(HotKeys.HOTKEY_H);
                registerHotKey(HotKeys.HOTKEY_H);
                registerHotKey(HotKeys.CTRL_H_HOTKEY);
            }
            if (Settings.ConvertCtrlBFNPtoArrowKeyEffective) {
                globalSpecialStrokeHotkeys.Add(HotKeys.HOTKEY_B);
                globalSpecialStrokeHotkeys.Add(HotKeys.HOTKEY_F);
                globalSpecialStrokeHotkeys.Add(HotKeys.HOTKEY_N);
                globalSpecialStrokeHotkeys.Add(HotKeys.HOTKEY_P);
                registerHotKey(HotKeys.HOTKEY_B);
                registerHotKey(HotKeys.HOTKEY_F);
                registerHotKey(HotKeys.HOTKEY_N);
                registerHotKey(HotKeys.HOTKEY_P);
                registerHotKey(HotKeys.CTRL_B_HOTKEY);
                registerHotKey(HotKeys.CTRL_F_HOTKEY);
                registerHotKey(HotKeys.CTRL_N_HOTKEY);
                registerHotKey(HotKeys.CTRL_P_HOTKEY);
            }

            if (Settings.ConvertCtrlSemiColonToDateEffective) {
                registerHotKey(HotKeys.CTRL_SEMICOLON_HOTKEY);
                registerHotKey(HotKeys.CTRL_SHIFT_SEMICOLON_HOTKEY);
                //registerHotKey(HotKeys.CTRL_COLON_HOTKEY);
                //registerHotKey(HotKeys.CTRL_SHIFT_COLON_HOTKEY);
            }

            logger.InfoH("LEAVE: Special Global Hotkeys Registered");
        }


        /// <summary>
        /// グローバル特殊ホットキーの解除
        /// </summary>
        public static void UnregisterSpecialGlobalHotKeys()
        {
            unregisterHotKey(HotKeys.HOTKEY_A);
            unregisterHotKey(HotKeys.HOTKEY_B);
            unregisterHotKey(HotKeys.HOTKEY_D);
            unregisterHotKey(HotKeys.HOTKEY_E);
            unregisterHotKey(HotKeys.HOTKEY_F);
            unregisterHotKey(HotKeys.HOTKEY_H);
            unregisterHotKey(HotKeys.HOTKEY_N);
            unregisterHotKey(HotKeys.HOTKEY_P);
            unregisterHotKey(HotKeys.CTRL_A_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_B_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_D_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_E_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_F_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_H_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_N_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_P_HOTKEY);
            //unregisterHotKey(HotKeys.CTRL_G_HOTKEY);
            //unregisterHotKey(HotKeys.CTRL_SHIFT_G_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_SEMICOLON_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_SHIFT_SEMICOLON_HOTKEY);
            //unregisterHotKey(HotKeys.CTRL_COLON_HOTKEY);
            //unregisterHotKey(HotKeys.CTRL_SHIFT_COLON_HOTKEY);
            logger.InfoH("Special Global Hotkeys Unregistered");
        }

        /// <summary>
        /// デコーダ用の HotKey を登録する
        /// </summary>
        public static void RegisterDecoderHotKeys()
        {
            logger.InfoH("CALLED");
            for (int id = 0; id < HotKeys.NUM_STROKE_HOTKEY; ++id) {
                // 登録済みのグローバル特殊ホットキーは登録しない
                if (!globalSpecialStrokeHotkeys.Contains(id)) registerHotKey(id);

                int idShifted = id + HotKeys.SHIFT_FUNC_HOTKEY_ID_BASE;
                // SHIFT+SPACE は特殊キー扱いなのでここでは登録しない
                if (idShifted != HotKeys.SHIFT_SPACE_HOTKEY) registerHotKey(idShifted);
            }
        }

        /// <summary>
        /// デコーダ用の HotKey 登録を解除する
        /// </summary>
        public static void UnregisterDecoderHotKeys()
        {
            logger.InfoH("CALLED");
            for (int id = 0; id < HotKeys.NUM_STROKE_HOTKEY; ++id) {
                // 登録済みのグローバル特殊ホットキーは解除しない
                if (!globalSpecialStrokeHotkeys.Contains(id)) unregisterHotKey(id);

                int idShifted = id + HotKeys.SHIFT_FUNC_HOTKEY_ID_BASE;
                // SHIFT+SPACE は特殊キー扱いなのでここでは解除しない
                if (idShifted != HotKeys.SHIFT_SPACE_HOTKEY) unregisterHotKey(idShifted);
            }
        }


        private static int ArrowHotkeysRegisterCount = 0;

        /// <summary>
        /// BS や Ctrl-G などの特殊キーの登録
        /// </summary>
        public static void RegisterSpecialHotKeys()
        {
            registerHotKey(HotKeys.CTRL_G_HOTKEY);
            registerHotKey(HotKeys.CTRL_SHIFT_G_HOTKEY);
            //registerSpecialHotKey(HotKeys.CTRL_H_HOTKEY);
            if (Settings.UseCtrlJasEnter) registerHotKey(HotKeys.CTRL_J_HOTKEY);
            if (Settings.UseCtrlMasEnter) registerHotKey(HotKeys.CTRL_M_HOTKEY);
            registerHotKey(HotKeys.CTRL_T_HOTKEY);
            registerHotKey(HotKeys.CTRL_SHIFT_T_HOTKEY);
            registerHotKey(HotKeys.CTRL_U_HOTKEY);
            registerHotKey(HotKeys.ENTER_HOTKEY);
            registerHotKey(HotKeys.ESC_HOTKEY);
            registerHotKey(HotKeys.BS_HOTKEY);
            registerHotKey(HotKeys.TAB_HOTKEY);
            if (Settings.HistSearchByShiftSpace) registerHotKey(HotKeys.SHIFT_SPACE_HOTKEY);
            if (Settings.HistSearchByCtrlSpace) registerHotKey(HotKeys.CTRL_SPACE_HOTKEY);
            if (Settings.HistSearchByCtrlSpace || Settings.HistSearchByShiftSpace) registerHotKey(HotKeys.CTRL_SHIFT_SPACE_HOTKEY);
            //RegisterArrowHotKeys();   // 常に必要? 候補選択時だけでよくない?
            logger.InfoH("Special Hotkeys Registered");
        }


        /// <summary>
        /// BS や Ctrl-G などの特殊キーの解除
        /// </summary>
        public static void UnregisterSpecialHotKeys()
        {
            unregisterHotKey(HotKeys.CTRL_G_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_SHIFT_G_HOTKEY);
            //unregisterHotKey(HotKeys.CTRL_H_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_J_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_M_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_T_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_SHIFT_T_HOTKEY);
            unregisterHotKey(HotKeys.CTRL_U_HOTKEY);
            unregisterHotKey(HotKeys.ENTER_HOTKEY);
            unregisterHotKey(HotKeys.ESC_HOTKEY);
            unregisterHotKey(HotKeys.BS_HOTKEY);
            unregisterHotKey(HotKeys.TAB_HOTKEY);
            if (Settings.HistSearchByShiftSpace) unregisterHotKey(HotKeys.SHIFT_SPACE_HOTKEY);
            if (Settings.HistSearchByCtrlSpace) unregisterHotKey(HotKeys.CTRL_SPACE_HOTKEY);
            if (Settings.HistSearchByCtrlSpace || Settings.HistSearchByShiftSpace) unregisterHotKey(HotKeys.CTRL_SHIFT_SPACE_HOTKEY);
            //UnregisterArrowHotKeys();   // 常に必要? 候補選択時だけでよくない?
            logger.InfoH("Special Hotkeys Unregistered");
        }

        /// <summary>
        /// 候補選択用ホットキーの登録 (必要な時だけONにする; xtermなど、矢印キーをエスケープシーケンスに変換しているアプリがあるため)<br/>
        /// RegisterやUnregisterは、呼んだThreadに対して有効になるらしい
        /// </summary>
        public static void RegisterCandSelectHotKeys()
        {
            //if (Settings.AutoHistSearchEnabled) registerHotKey(HotKeys.SHIFT_SPACE_HOTKEY);
            //if (Settings.AutoHistSearchEnabled) registerHotKey(HotKeys.CTRL_SPACE_HOTKEY);
            RegisterArrowHotKeys();
            if (Settings.LoggingHotKeyInfo) logger.InfoH("CandSlect Hotkeys Registered");
        }

        /// <summary>
        /// 候補選択用ホットキーの解除
        /// </summary>
        public static void UnregisterCandSelectHotKeys()
        {
            //if (Settings.AutoHistSearchEnabled) unregisterHotKey(HotKeys.SHIFT_SPACE_HOTKEY);
            //if (Settings.AutoHistSearchEnabled) unregisterHotKey(HotKeys.CTRL_SPACE_HOTKEY);
            UnregisterArrowHotKeys();
            if (Settings.LoggingHotKeyInfo) logger.InfoH("CandSlect Hotkeys Unregistered");
        }

        /// <summary>
        /// 矢印キー関連ホットキーの登録
        /// RegisterやUnregisterは、呼んだThreadに対して有効になるらしい
        /// </summary>
        public static void RegisterArrowHotKeys()
        {
            if (ArrowHotkeysRegisterCount++ == 0) {
                //registerSpecialHotKey(HotKeys.CTRL_B_HOTKEY);
                //registerSpecialHotKey(HotKeys.CTRL_F_HOTKEY);
                //registerSpecialHotKey(HotKeys.CTRL_N_HOTKEY);
                //registerSpecialHotKey(HotKeys.CTRL_P_HOTKEY);
                registerHotKey(HotKeys.LEFT_ARROW_HOTKEY);
                registerHotKey(HotKeys.RIGHT_ARROW_HOTKEY);
                registerHotKey(HotKeys.UP_ARROW_HOTKEY);
                registerHotKey(HotKeys.DOWN_ARROW_HOTKEY);
                if (Settings.LoggingHotKeyInfo) logger.InfoH("Arrow Hotkeys Registered");
            }
        }


        /// <summary>
        /// 矢印キー関連ホットキーの解除
        /// </summary>
        public static void UnregisterArrowHotKeys()
        {
            if (ArrowHotkeysRegisterCount > 0 && --ArrowHotkeysRegisterCount == 0) {
                //unregisterHotKey(HotKeys.CTRL_B_HOTKEY);
                //unregisterHotKey(HotKeys.CTRL_F_HOTKEY);
                //unregisterHotKey(HotKeys.CTRL_N_HOTKEY);
                //unregisterHotKey(HotKeys.CTRL_P_HOTKEY);
                unregisterHotKey(HotKeys.LEFT_ARROW_HOTKEY);
                unregisterHotKey(HotKeys.RIGHT_ARROW_HOTKEY);
                unregisterHotKey(HotKeys.UP_ARROW_HOTKEY);
                unregisterHotKey(HotKeys.DOWN_ARROW_HOTKEY);
                if (Settings.LoggingHotKeyInfo) logger.InfoH("Arrow Hotkeys Unregistered");
            }
        }
    }
}

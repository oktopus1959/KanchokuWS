using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utils;

namespace KanchokuWS.Domain
{
    /// <summary>
    /// 修飾キーと仮想キーの組み合わせ
    /// </summary>
    struct VKeyCombo {
        public uint modifier;
        public uint vkey;

        public VKeyCombo(uint mod, uint vk)
        {
            modifier = mod;
            vkey = vk;
        }

        public uint SerialValue => CalcSerialValue(modifier, vkey);

        public static uint CalcSerialValue(uint mod, uint vkey)
        {
            return ((mod & 0xffff) << 16) + (vkey & 0xffff);
        }

    }

    static class VKeyComboRepository
    {
        private static Logger logger = Logger.GetLogger();

        public static VKeyCombo EmptyCombo = new VKeyCombo(0, 0);

        public static VKeyCombo CtrlC_VKeyCombo = new VKeyCombo(KeyModifiers.MOD_CONTROL, CharVsVKey.GetVKeyFromFaceStr("C"));

        public static VKeyCombo CtrlV_VKeyCombo = new VKeyCombo(KeyModifiers.MOD_CONTROL, CharVsVKey.GetVKeyFromFaceStr("V"));

        /// <summary>
        /// DECKEY id から仮想キーコンビネーションを得るための配列
        /// </summary>
        private static VKeyCombo?[] VKeyComboFromDecKey;

        /// <summary>
        /// 仮想キーコンビネーションのSerial値からDECKEY を得るための辞書
        /// </summary>
        private static Dictionary<uint, int> DecKeyFromVKeyCombo;

        /// <summary>
        /// 仮想キーコンビネーションのSerial値からModキーによるシフト変換されたDECKEY を得るための辞書
        /// </summary>
        private static Dictionary<uint, int> ModConvertedDecKeyFromVKeyCombo;

        public static void Initialize()
        {
            logger.InfoH("ENTER");
            VKeyComboFromDecKey = new VKeyCombo?[DecoderKeys.GLOBAL_DECKEY_ID_END];
            DecKeyFromVKeyCombo = new Dictionary<uint, int>();
            ModConvertedDecKeyFromVKeyCombo = new Dictionary<uint, int>();
            logger.InfoH("LEAVE");
        }

        /// <summary>
        /// xfer, nfer など特殊キーに割り当てられている DecoderKey を登録
        /// </summary>
        /// <param name="deckey"></param>
        public static void AddSpecialDeckey(string name, int deckey)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"name={name}, deckey={deckey:x}H({deckey})");
            if (deckey > 0) {
                uint vk = DecoderKeyVsVKey.GetFuncVkeyByName(name);
                if (vk > 0) {
                    VKeyComboFromDecKey[deckey] = new VKeyCombo(0, vk);
                }
            }
        }

        public static void AddModifiedDeckey(int deckey, uint mod, uint vkey)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"deckey={deckey:x}H({deckey}), mod={mod:x}H, vkey={vkey:x}H({vkey})");
            var combo = new VKeyCombo(mod, vkey);
            VKeyComboFromDecKey[deckey] = combo;
        }

        /// <summary>修飾子と仮想キーコードの組みとDecKeyの間の相互変換を登録する</summary>
        public static void AddDecKeyAndCombo(int deckey, uint mod, uint vkey, bool bFromComboOnly = false)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"deckey={deckey:x}H({deckey}), mod={mod:x}H, vkey={vkey:x}H({vkey})");
            var combo = new VKeyCombo(mod, (uint)vkey);
            if (!bFromComboOnly) VKeyComboFromDecKey[deckey] = combo;
            DecKeyFromVKeyCombo[combo.SerialValue] = deckey;
        }

        /// <summary>
        /// Settingsで設定されたCtrlキー変換やmod-conversionによるキー変換を登録する
        /// </summary>
        /// <param name="deckey">英字変換後のDecKey(TODO: 要確認)</param>
        /// <param name="mod"></param>
        /// <param name="vkey"></param>
        public static void AddModConvertedDecKeyFromCombo(int deckey, uint mod, uint vkey)
        {
            logger.Debug(() => $"deckey={deckey:x}H({deckey}), mod={mod:x}H, vkey={vkey:x}H({vkey})");
            ModConvertedDecKeyFromVKeyCombo[VKeyCombo.CalcSerialValue(mod, vkey)] = deckey;
        }

        private static void RemoveModConvertedDecKeyFromCombo(uint mod, uint vkey)
        {
            logger.Debug(() => $"mod={mod:x}H, vkey={vkey:x}H({vkey})");
            try {
                ModConvertedDecKeyFromVKeyCombo.Remove(VKeyCombo.CalcSerialValue(mod, vkey));
            } catch { }
        }

        /// <summary>
        /// Ctrl修飾されたDecKeyの登録 (Settingsから呼ばれる)
        /// </summary>
        public static void AddCtrlDeckeyFromCombo(string keyFace, int ctrlDeckey, int ctrlShiftDeckey)
        {
            bool bRemove = false;
            if (keyFace._startsWith("#")) {
                bRemove = true;
                keyFace = keyFace.Replace("#", "");
            }
            uint vkey = CharVsVKey.GetVKeyFromFaceStr(keyFace);
            if (vkey > 0 && vkey < 0x100) {
                if (bRemove) {
                    if (ctrlDeckey > 0) RemoveModConvertedDecKeyFromCombo(KeyModifiers.MOD_CONTROL, vkey);
                    if (ctrlShiftDeckey > 0) RemoveModConvertedDecKeyFromCombo(KeyModifiers.MOD_CONTROL | KeyModifiers.MOD_SHIFT, vkey);
                } else {
                    if (ctrlDeckey > 0) AddModConvertedDecKeyFromCombo(ctrlDeckey, KeyModifiers.MOD_CONTROL, vkey);
                    if (ctrlShiftDeckey > 0) AddModConvertedDecKeyFromCombo(ctrlShiftDeckey, KeyModifiers.MOD_CONTROL | KeyModifiers.MOD_SHIFT, vkey);
                }
            }
        }

        /// <summary>
        /// Ctrl修飾されたDecKeyとVKeyの相互登録 (Settingsから呼ばれる)
        /// </summary>
        /// <param name="keyFace"></param>
        /// <param name="ctrlDeckey"></param>
        /// <param name="ctrlShiftDeckey"></param>
        public static void AddCtrlDeckeyAndCombo(string keyFace, int ctrlDeckey, int ctrlShiftDeckey)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"keyFace={keyFace}, ctrlDeckey={ctrlDeckey:x}H({ctrlDeckey}), , ctrlShiftDeckey={ctrlShiftDeckey:x}H({ctrlShiftDeckey})");
            uint vkey = CharVsVKey.GetVKeyFromFaceStr(keyFace);
            if (vkey > 0 && vkey < 0x100) {
                if (ctrlDeckey > 0) AddDecKeyAndCombo(ctrlDeckey, KeyModifiers.MOD_CONTROL, vkey);
                if (ctrlShiftDeckey > 0) AddDecKeyAndCombo(ctrlShiftDeckey, KeyModifiers.MOD_CONTROL | KeyModifiers.MOD_SHIFT, vkey);
            }
        }

        /// <summary>
        /// DECKEY id から仮想キーと修飾子のコンビネーションを得る
        /// </summary>
        public static VKeyCombo? GetVKeyComboFromDecKey(int deckey)
        {
            var combo = VKeyComboFromDecKey._getNth(deckey);
            logger.Info(() => $"deckey={deckey:x}H({deckey}), combo.mod={(combo.HasValue ? combo.Value.modifier : 0):x}, combo.vkey={(combo.HasValue ? combo.Value.vkey : 0)}");
            return combo;
        }

        ///// <summary>仮想キーコードから、DecKey を得る</summary>
        //public static int GetDecKeyFromVKey(uint vkey)
        //{
        //    return GetDecKeyFromCombo(0, vkey);
        //}

        /// <summary>修飾子と仮想キーコードの組みから、DecKey を得る</summary>
        public static int GetDecKeyFromCombo(uint mod, uint vkey)
        {
            if (Settings.LoggingDecKeyInfo) logger.Info(() => $"ENTER: mod={mod:x}H({mod}), vkey={vkey:x}H({vkey})");
            int deckey = DecKeyFromVKeyCombo._safeGet(VKeyCombo.CalcSerialValue(mod, vkey), -1);
            if (Settings.LoggingDecKeyInfo) logger.Info(() => $"LEAVE: deckey={deckey:x}H({deckey})");
            return deckey;
        }

        /// <summary>修飾子と仮想キーコードの組みから、Modキーによるシフト変換されたDECKEY を得る</summary>
        public static int GetModConvertedDecKeyFromCombo(uint mod, uint vkey)
        {
            if (Settings.LoggingDecKeyInfo) logger.Info(() => $"ENTER: mod={mod:x}H({mod}), vkey={vkey:x}H({vkey})");
            int deckey = ModConvertedDecKeyFromVKeyCombo._safeGet(VKeyCombo.CalcSerialValue(mod, vkey), -1);
            if (deckey <= 0) { deckey = GetDecKeyFromCombo(mod, vkey); }
            if (Settings.LoggingDecKeyInfo) logger.Info(() => $"LEAVE: deckey={deckey:x}H({deckey})");
            return deckey;
        }

        ///// <summary>
        ///// キー文字とCtrl/Shiftから、仮想キーコンボを作成する
        ///// </summary>
        ///// <param name="face"></param>
        ///// <param name="ctrl"></param>
        ///// <param name="shift"></param>
        ///// <returns></returns>
        //private static VKeyCombo? getVKeyComboFromFaceString(string face, bool ctrl, bool shift)
        //{
        //    uint vkey = CharVsVKey.FaceToVKey(face);
        //    if (vkey > 0 && vkey < 0x100) {
        //        return new VKeyCombo(KeyModifiers.MakeModifier(ctrl, shift), vkey);
        //    }
        //    return null;
        //}

        /// <summary>
        /// キー文字から、Ctrlキー修飾されたDECKEY を得る<br/>
        /// 現状、使用しているのは Ctrl-J のみ
        /// </summary>
        public static int GetCtrlDecKeyOf(string face)
        {
            uint vkey = CharVsVKey.GetVKeyFromFaceStr(face);
            return (vkey > 0) ? GetModConvertedDecKeyFromCombo(KeyModifiers.MOD_CONTROL, vkey) : -1;
        }

        /// <summary>
        /// 漢直モードのトグルをやるキーコンボならそのDecKeyを返す<br/>
        /// 漢直モードのトグルでなければ -1 を返す
        /// </summary>
        public static int GetKanchokuToggleDecKey(uint mod, uint vkey)
        {
            int kanchokuCodeWithMod = GetModConvertedDecKeyFromCombo(mod, vkey);
            return (kanchokuCodeWithMod >= DecoderKeys.TOGGLE_DECKEY && kanchokuCodeWithMod <= DecoderKeys.DEACTIVE2_DECKEY) ? kanchokuCodeWithMod : -1;
        }

    }
}

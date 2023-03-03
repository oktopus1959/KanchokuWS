using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utils;

namespace KanchokuWS.Domain
{
    /// <summary>
    /// 修飾キーとNormalDecKeyの組み合わせ
    /// </summary>
    struct KeyCombo {
        public uint modifier;
        public int normalDecKey;

        public KeyCombo(uint mod, int dc)
        {
            modifier = mod;
            normalDecKey = dc;
        }

        public uint SerialValue => CalcSerialValue(modifier, normalDecKey);

        public static uint CalcSerialValue(uint mod, int deckey)
        {
            return ((mod & 0xffff) << 16) + ((uint)deckey & 0xffff);
        }

        //public static KeyCombo MakeCtrlKeyCombo(char ch)
        //{
        //    return new KeyCombo(KeyModifiers.MOD_CONTROL, DecoderKeyVsChar.GetArrangedDecKeyFromFaceChar(ch));
        //}
    }

    ///// <summary>
    ///// 修飾キーと仮想キーの組み合わせ
    ///// </summary>
    //struct VKeyCombo {
    //    public uint modifier;
    //    public uint vkey;

    //    public VKeyCombo(uint mod, uint vk)
    //    {
    //        modifier = mod;
    //        vkey = vk;
    //    }

    //    public uint SerialValue => CalcSerialValue(modifier, vkey);

    //    public static uint CalcSerialValue(uint mod, uint vkey)
    //    {
    //        return ((mod & 0xffff) << 16) + (vkey & 0xffff);
    //    }

    //}

    static class KeyComboRepository
    {
        private static Logger logger = Logger.GetLogger();

        public static KeyCombo EmptyCombo = new KeyCombo(0, 0);

        //public static KeyCombo CtrlC_KeyCombo = KeyCombo.MakeCtrlKeyCombo('C');

        //public static KeyCombo CtrlV_KeyCombo = KeyCombo.MakeCtrlKeyCombo('V');

        /// <summary>
        /// DECKEY id から仮想キーコンビネーションを得るための配列
        /// </summary>
        private static KeyCombo?[] KeyComboFromDecKey;

        /// <summary>
        /// 仮想キーコンビネーションのSerial値からDECKEY を得るための辞書
        /// </summary>
        private static Dictionary<uint, int> DecKeyFromKeyCombo;

        /// <summary>
        /// 仮想キーコンビネーションのSerial値からModキーによるシフト変換されたDECKEY を得るための辞書
        /// </summary>
        private static Dictionary<uint, int> ModConvertedDecKeyFromKeyCombo;

        public static void Initialize()
        {
            logger.InfoH("ENTER");
            KeyComboFromDecKey = new KeyCombo?[DecoderKeys.GLOBAL_DECKEY_ID_END];
            DecKeyFromKeyCombo = new Dictionary<uint, int>();
            ModConvertedDecKeyFromKeyCombo = new Dictionary<uint, int>();
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
                    KeyComboFromDecKey[deckey] = new KeyCombo(0, deckey);
                }
            }
        }

        /// <summary>修飾されたDecKeyから、修飾子とNormalDecKeyへの変換を登録する</summary>
        public static void AddModifiedDeckey(int modDeckey, uint mod, int arrgDeckey)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"modDeckey={modDeckey:x}H({modDeckey}), mod={mod:x}H, arrgDeckey={arrgDeckey:x}H({arrgDeckey})");
            var combo = new KeyCombo(mod, arrgDeckey);
            KeyComboFromDecKey[modDeckey] = combo;
        }

        /// <summary>修飾子と仮想キーコードの組みとDecKeyの間の相互変換を登録する</summary>
        public static void AddDecKeyAndCombo(int modDeckey, uint mod, int arrgDeckey, bool bFromComboOnly = false)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"modDeckey={modDeckey:x}H({modDeckey}), mod={mod:x}H, arrgDeckey={arrgDeckey:x}H({arrgDeckey})");
            var combo = new KeyCombo(mod, arrgDeckey);
            if (!bFromComboOnly) KeyComboFromDecKey[modDeckey] = combo;
            DecKeyFromKeyCombo[combo.SerialValue] = modDeckey;
        }

        /// <summary>
        /// Settingsで設定されたCtrlキー変換やmod-conversionによるキー変換を登録する
        /// </summary>
        /// <param name="modDeckey">英字変換後のDecKey(TODO: 要確認)</param>
        /// <param name="mod"></param>
        /// <param name="arrgDeckey"></param>
        public static void AddModConvertedDecKeyFromCombo(int modDeckey, uint mod, int arrgDeckey)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"modDeckey={modDeckey:x}H({modDeckey}), mod={mod:x}H, arrgDeckey={arrgDeckey:x}H({arrgDeckey})");
            ModConvertedDecKeyFromKeyCombo[KeyCombo.CalcSerialValue(mod, arrgDeckey)] = modDeckey;
        }

        private static void RemoveModConvertedDecKeyFromCombo(uint mod, int arrgDeckey)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"mod={mod:x}H, arrgDeckey={arrgDeckey:x}H({arrgDeckey})");
            try {
                ModConvertedDecKeyFromKeyCombo.Remove(KeyCombo.CalcSerialValue(mod, arrgDeckey));
            } catch { }
        }

        /// <summary>
        /// Ctrl修飾されたDecKeyの登録 (Settingsから呼ばれる)
        /// </summary>
        public static void AddCtrlDeckeyFromCombo(string keyFace, int ctrlDeckey, int ctrlShiftDeckey)
        {
            if (Settings.LoggingDecKeyInfo) logger.InfoH(() => $"keyFace={keyFace}, ctrlDeckey={ctrlDeckey}, ctrlShiftDeckey={ctrlShiftDeckey}");
            bool bRemove = false;
            if (keyFace._startsWith("#")) {
                bRemove = true;
                keyFace = keyFace.Replace("#", "");
            }
            int deckey = DecoderKeyVsChar.GetArrangedDecKeyFromFaceStr(keyFace);
            if (deckey >= 0) {
                if (bRemove) {
                    if (ctrlDeckey > 0) RemoveModConvertedDecKeyFromCombo(KeyModifiers.MOD_CONTROL, deckey);
                    if (ctrlShiftDeckey > 0) RemoveModConvertedDecKeyFromCombo(KeyModifiers.MOD_CONTROL | KeyModifiers.MOD_SHIFT, deckey);
                } else {
                    if (ctrlDeckey > 0) AddModConvertedDecKeyFromCombo(ctrlDeckey, KeyModifiers.MOD_CONTROL, deckey);
                    if (ctrlShiftDeckey > 0) AddModConvertedDecKeyFromCombo(ctrlShiftDeckey, KeyModifiers.MOD_CONTROL | KeyModifiers.MOD_SHIFT, deckey);
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
            int deckey = DecoderKeyVsChar.GetArrangedDecKeyFromFaceStr(keyFace);
            if (deckey >= 0) {
                if (ctrlDeckey > 0) AddDecKeyAndCombo(ctrlDeckey, KeyModifiers.MOD_CONTROL, deckey);
                if (ctrlShiftDeckey > 0) AddDecKeyAndCombo(ctrlShiftDeckey, KeyModifiers.MOD_CONTROL | KeyModifiers.MOD_SHIFT, deckey);
            }
        }

        /// <summary>
        /// DECKEY id から仮想キーと修飾子のコンビネーションを得る
        /// </summary>
        public static KeyCombo? GetKeyComboFromDecKey(int deckey)
        {
            var combo = KeyComboFromDecKey._getNth(deckey);
            if (Settings.LoggingDecKeyInfo) logger.Info(() => $"deckey={deckey:x}H({deckey}), combo.mod={(combo.HasValue ? combo.Value.modifier : 0):x}, combo.normalDecKey={(combo.HasValue ? combo.Value.normalDecKey : 0)}");
            return combo;
        }

        ///// <summary>仮想キーコードから、DecKey を得る</summary>
        //public static int GetDecKeyFromVKey(uint vkey)
        //{
        //    return GetDecKeyFromCombo(0, vkey);
        //}

        /// <summary>修飾子と仮想キーコードの組みから、DecKey を得る</summary>
        public static int GetDecKeyFromCombo(uint mod, int noramlDeckey)
        {
            if (Settings.LoggingDecKeyInfo) logger.Info(() => $"ENTER: mod={mod:x}H({mod}), noramlDeckey={noramlDeckey:x}H({noramlDeckey})");
            int deckey = DecKeyFromKeyCombo._safeGet(KeyCombo.CalcSerialValue(mod, noramlDeckey), -1);
            if (Settings.LoggingDecKeyInfo) logger.Info(() => $"LEAVE: deckey={deckey:x}H({deckey})");
            return deckey;
        }

        /// <summary>修飾子と仮想キーコードの組みから、Modキーによるシフト変換されたDECKEY を得る</summary>
        public static int GetModConvertedDecKeyFromCombo(uint mod, int noramlDeckey)
        {
            if (Settings.LoggingDecKeyInfo) logger.Info(() => $"ENTER: mod={mod:x}H({mod}), noramlDeckey={noramlDeckey:x}H({noramlDeckey})");
            int deckey = ModConvertedDecKeyFromKeyCombo._safeGet(KeyCombo.CalcSerialValue(mod, noramlDeckey), -1);
            if (deckey <= 0) { deckey = GetDecKeyFromCombo(mod, noramlDeckey); }
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
        //private static KeyCombo? getKeyComboFromFaceString(string face, bool ctrl, bool shift)
        //{
        //    uint vkey = CharVsVKey.FaceToVKey(face);
        //    if (vkey > 0 && vkey < 0x100) {
        //        return new KeyCombo(KeyModifiers.MakeModifier(ctrl, shift), vkey);
        //    }
        //    return null;
        //}

        /// <summary>
        /// キー文字から、Ctrlキー修飾されたDECKEY を得る<br/>
        /// 現状、使用しているのは Ctrl-J のみ
        /// </summary>
        public static int GetCtrlDecKeyOf(string face)
        {
            int deckey = DecoderKeyVsChar.GetArrangedDecKeyFromFaceStr(face);
            return (deckey > 0) ? GetModConvertedDecKeyFromCombo(KeyModifiers.MOD_CONTROL, deckey) : -1;
        }

        /// <summary>
        /// 漢直モードのトグルをやるキーコンボならそのDecKeyを返す<br/>
        /// 漢直モードのトグルでなければ -1 を返す
        /// </summary>
        public static int GetKanchokuToggleDecKey(uint mod, int deckey)
        {
            int kanchokuCodeWithMod = GetModConvertedDecKeyFromCombo(mod, deckey);
            return (kanchokuCodeWithMod >= DecoderKeys.TOGGLE_DECKEY && kanchokuCodeWithMod <= DecoderKeys.DEACTIVE2_DECKEY) ? kanchokuCodeWithMod : -1;
        }

    }
}

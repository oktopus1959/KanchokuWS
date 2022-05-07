using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace KanchokuWS.CombinationKeyStroke.DeterminerLib
{
    using ShiftKeyKind = ShiftKeyPool.Kind;

    class KeyCombinationPool
    {
        private static Logger logger = Logger.GetLogger(true);

        // 主テーブル用のCombinaitonPool
        public static KeyCombinationPool Singleton1 = new KeyCombinationPool();

        // 副テーブル用のCombinaitonPool
        public static KeyCombinationPool Singleton2 = new KeyCombinationPool();

        // 現在使用中のKeyCombinaitonPool
        public static KeyCombinationPool CurrentPool { get; private set; } = Singleton1;

        public static void Initialize()
        {
            Singleton1.Clear();
            Singleton2.Clear();
            CurrentPool = Singleton1;
        }

        public static void ExchangeCurrentPool()
        {
            CurrentPool = CurrentPool == Singleton1 ? Singleton2 : Singleton1;
            logger.DebugH(() => $"CurrentPool={(CurrentPool == Singleton1 ? 1 : 2)}");
        }

        // 同時打鍵組合せ
        private Dictionary<string, KeyCombination> keyComboDict = new Dictionary<string, KeyCombination>();

        public int Count { get { return keyComboDict.Count; } }

        // 利用可能か
        public bool Enabled => Count > 0;

        // 同時打鍵組合せの部分キーの順列集合(これらは、最後に非終端となるキー順列として使う)
        private HashSet<string> comboSubKeys = new HashSet<string>();

        // ShiftKeyとして扱いうるキー
        public ShiftKeyPool ComboShiftKeys { get; private set; } = new ShiftKeyPool();

        public void Clear()
        {
            keyComboDict.Clear();
            comboSubKeys.Clear();
            ComboShiftKeys.Clear();
        }

        /// <summary>
        /// エントリの追加
        /// </summary>
        /// <param name="comboShiftedKeyList"></param>
        /// <param name="shiftKind">PreShiftの場合は、先頭キーを固定した順列を生成する</param>
        public void AddEntry(List<int> comboShiftedKeyList, ShiftKeyKind shiftKind)
        {
            var keyCombo = new KeyCombination(comboShiftedKeyList, shiftKind);
            var moduloKeyList = comboShiftedKeyList.Select(x => Stroke.ModuloizeKey(x)).ToList();
            var primKey = KeyCombinationHelper.MakePrimaryKey(moduloKeyList);
            keyComboDict[primKey] = keyCombo;
            foreach (var key in KeyCombinationHelper.MakePermutatedKeys(moduloKeyList, shiftKind == ShiftKeyKind.PreShift)) {
                if (!keyComboDict.ContainsKey(key)) { keyComboDict[key] = keyCombo; }
            }
            comboSubKeys.UnionWith(KeyCombinationHelper.MakeSubKeys(moduloKeyList));
        }

        private KeyCombination getEntry(IEnumerable<int> keyList, int lastKey)
        {
            logger.DebugH(() => $"CALLED: keyList={KeyCombinationHelper.EncodeKeyList(keyList)}, lastKey={lastKey}");
            return keyComboDict._safeGet(KeyCombinationHelper.MakePrimaryKey(keyList, lastKey));
        }

        public KeyCombination GetEntry(IEnumerable<Stroke> strokeList, Stroke lastStroke)
        {
            // ストロークリストが空であるか、あるいは全てのストロークがシフトされていたら、null
            if (strokeList._isEmpty() || (strokeList.All(x => x.IsShifted) && (lastStroke?.IsShifted ?? true))) return null;

            return getEntry(strokeList.Select(x => x.ModuloKeyCode), lastStroke?.ModuloKeyCode ?? -1);
        }

        public KeyCombination GetEntry(StrokeList strokeList, int start, int len)
        {
            return GetEntry(strokeList.GetList().Skip(start).Take(len), null);
        }

        public KeyCombination GetEntry(IEnumerable<Stroke> strokeList, int start, int len)
        {
            return GetEntry(strokeList.Skip(start).Take(len), null);
        }

        public KeyCombination GetEntry(StrokeList strokeList, Stroke lastStroke)
        {
            return GetEntry(strokeList.GetList(), lastStroke);
        }

        public KeyCombination GetEntry(int decKey)
        {
            return keyComboDict._safeGet(KeyCombinationHelper.MakePrimaryKey(decKey));
        }

        public KeyCombination GetEntry(Stroke stroke)
        {
            return GetEntry(stroke.ModuloKeyCode);
        }

        /// <summary>
        /// 部分キーに対して、非終端マークをセット<br/>定義されていない部分キーの場合は新しく KeyCombination を生成しておく
        /// </summary>        
        public void SetNonTerminalMarkForSubkeys()
        {
            logger.DebugH($"comboSubKeys.Count={comboSubKeys.Count}");
            foreach (var key in comboSubKeys) {
                // 部分キーに対して、非終端マークをセット
                logger.DebugH(() => $"key={key}, list={KeyCombinationHelper.EncodeKeyList(KeyCombinationHelper.DecodeKey(key))}");
                var keyCombo = keyComboDict._safeGet(key);
                if (keyCombo == null) {
                    // 存在していなかった部分キーを追加
                    keyComboDict[key] = keyCombo = new KeyCombination(null, ShiftKeyKind.None);
                }
                keyCombo.NotTerminal();
            }
            logger.DebugH($"LEAVE");
        }

        /// <summary>
        /// ShiftKeyとして扱いうるキーの設定
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="kind"></param>
        public void AddShiftKey(int keyCode, ShiftKeyKind kind)
        {
            logger.DebugH(() => $"CALLED: keyCode={keyCode}, shiftKey={kind}");
            if (keyCode > 0) ComboShiftKeys.AddShiftKey(keyCode, kind);
        }

        /// <summary>
        /// keyCode が ShiftKeyとしても扱われるか否かを返す。<br/>
        /// 0 なら ShiftKey としては扱わない<br/>
        /// 1以上なら、ShiftKeyとしての優先度となる(1が最優先)
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        public ShiftKeyKind GetShiftKeyKind(int keyCode)
        {
            return ComboShiftKeys.GetShiftKeyKind(keyCode);
        }

        public void DebugPrint()
        {
            foreach (var pair in keyComboDict) {
                var key = KeyCombinationHelper.DecodeKeyString(pair.Key);
                var deckeys = (pair.Value.ComboShiftedDecoderKeyList?.KeyString())._orElse("NONE");
                logger.DebugH($"{key}={deckeys} {pair.Value.IsTerminal}");
            }
            foreach (var pair in ComboShiftKeys.Pairs) {
                logger.DebugH($"ShiftKey: {pair.Key}={pair.Value}");
            }
        }
    }
}

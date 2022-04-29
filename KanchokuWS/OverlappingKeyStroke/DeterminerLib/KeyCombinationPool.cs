using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace KanchokuWS.OverlappingKeyStroke.DeterminerLib
{
    class KeyCombinationPool
    {
        private static Logger logger = Logger.GetLogger(true);

        // 主テーブル用のCombinaitonPool
        public static KeyCombinationPool Singleton1 = new KeyCombinationPool();

        // 副テーブル用のCombinaitonPool
        public static KeyCombinationPool Singleton2 = new KeyCombinationPool();

        public static KeyCombinationPool CurrentPool { get; private set; } = Singleton1;

        public static void Initialize()
        {
            Singleton1.Clear();
            Singleton2.Clear();
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
        public ShiftKeyPriority OverlappingShiftKeys { get; private set; } = new ShiftKeyPriority();

        public void Clear()
        {
            keyComboDict.Clear();
            comboSubKeys.Clear();
            OverlappingShiftKeys.Clear();
        }

        /// <summary>
        /// エントリの追加
        /// </summary>
        /// <param name="keyList"></param>
        /// <param name="keyCombo"></param>
        public void AddEntry(List<int> keyList, KeyCombination keyCombo)
        {
            keyComboDict[KeyCombinationHelper.MakePrimaryKey(keyList)] = keyCombo;
            foreach (var key in KeyCombinationHelper.MakePermutatedKeys(keyList)) {
                if (!keyComboDict.ContainsKey(key)) { keyComboDict[key] = keyCombo; }
            }
            comboSubKeys.UnionWith(KeyCombinationHelper.MakeSubKeys(keyList));
        }

        public KeyCombination GetEntry(IEnumerable<int> keyList)
        {
            logger.DebugH(() => $"CALLED: keyList={KeyCombinationHelper.EncodeKeyList(keyList)}");
            return keyComboDict._safeGet(KeyCombinationHelper.MakePrimaryKey(keyList));
        }

        public KeyCombination GetEntry(List<Stroke> strokeList)
        {
            return GetEntry(strokeList?.Select(x => x.NormalKeyCode));
        }

        public KeyCombination GetEntry(List<Stroke> strokeList, int len)
        {
            return GetEntry(strokeList?.Take(len).Select(x => x.NormalKeyCode));
        }

        public KeyCombination GetEntry(int decKey)
        {
            return keyComboDict._safeGet(KeyCombinationHelper.MakePrimaryKey(decKey));
        }

        public KeyCombination GetEntry(Stroke stroke)
        {
            return GetEntry(stroke.NormalKeyCode);
        }

        /// <summary>
        /// 部分キーに対して、非終端マークをセット<br/>定義されていない部分キーの場合は新しく KeyCombination を生成しておく
        /// </summary>        
        public void SetNonTerminalMarkForSubkeys()
        {
            logger.DebugH($"comboSubKeys.Count={comboSubKeys.Count}");
            foreach (var key in comboSubKeys) {
                // 部分キーに対して、非終端マークをセット
                var list = KeyCombinationHelper.DecodeKey(key);
                logger.DebugH(() => $"key={key}, list={KeyCombinationHelper.EncodeKeyList(list)}");
                var keyCombo = keyComboDict._safeGet(key);
                if (keyCombo == null) {
                    keyComboDict[key] = keyCombo =new KeyCombination(list);
                }
                keyCombo.NotTerminal();
            }
            logger.DebugH($"LEAVE");
        }

        /// <summary>
        /// ShiftKeyとして扱いうるキーの設定(priority は 1以上であること。1が最優先)
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="priority"></param>
        public void AddShiftKey(int keyCode, int priority)
        {
            if (keyCode > 0) OverlappingShiftKeys.AddShiftKey(keyCode, priority);
        }

        /// <summary>
        /// keyCode が ShiftKeyとしても扱われるか否かを返す。<br/>
        /// 0 なら ShiftKey としては扱わない<br/>
        /// 1以上なら、ShiftKeyとしての優先度となる(1が最優先)
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        public int GetShiftPriority(int keyCode)
        {
            return OverlappingShiftKeys.GetShiftPriority(keyCode);
        }

        public void DebugPrint()
        {
            foreach (var pair in keyComboDict) {
                var key = pair.Key.Select(x => ((int)(x - 0x20)).ToString())._join(":");
                var deckeys = pair.Value.DecoderKeyList?.KeyString() ?? "NONE";
                logger.DebugH($"{key}={deckeys} {pair.Value.IsTerminal}");
            }
            foreach (var pair in OverlappingShiftKeys.Pairs) {
                logger.DebugH($"ShiftKey: {pair.Key}={pair.Value}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace KanchokuWS.CombinationKeyStroke.DeterminerLib
{
    using ComboKind = ComboShiftKeyPool.ComboKind;

    class KeyCombinationPool
    {
        private static Logger logger = Logger.GetLogger(true);

        /// <summary>
        /// 主テーブル用のCombinaitonPool
        /// </summary>        
        public static KeyCombinationPool Singleton1 = new KeyCombinationPool();

        /// <summary>
        /// 副テーブル用のCombinaitonPool
        /// </summary>        
        public static KeyCombinationPool Singleton2 = new KeyCombinationPool();

        // 現在使用中のKeyCombinaitonPool
        public static KeyCombinationPool CurrentPool { get; private set; } = Singleton1;

        public static void Initialize()
        {
            logger.DebugH("CALLED");
            Singleton1.Clear();
            Singleton2.Clear();
            CurrentPool = Singleton1;
        }

        public static void ExchangeCurrentPool()
        {
            CurrentPool = CurrentPool == Singleton1 ? Singleton2 : Singleton1;
            logger.DebugH(() => $"CurrentPool={(CurrentPool == Singleton1 ? 1 : 2)}");
        }

        public static void UsePrimaryPool()
        {
            CurrentPool = Singleton1;
        }

        public static void UseSecondaryPool()
        {
            CurrentPool = Singleton2;
        }

        /// <summary>
        /// 同時打鍵組合せ
        /// </summary>        
        private Dictionary<string, KeyCombination> keyComboDict = new Dictionary<string, KeyCombination>();

        public int Count { get { return keyComboDict.Count; } }

        // 利用可能か
        public bool Enabled => Count > 0;

        // 同時打鍵組合せの部分キーの順列集合(これらは、最後に非終端となるキー順列として使う)
        private HashSet<string> comboSubKeys = new HashSet<string>();

        /// <summary>
        /// ShiftKeyとして扱いうるキー
        /// </summary>
        public ComboShiftKeyPool ComboShiftKeys { get; private set; } = new ComboShiftKeyPool();

        // 同時打鍵シフトキーを保持しているか
        public bool ContainsComboShiftKey => ComboShiftKeys.ContainsComboShiftKey;

        // 相互シフトキーを保持しているか
        public bool ContainsUnorderedShiftKey => ComboShiftKeys.ContainsUnorderedShiftKey;

        // 連続シフトキーを保持しているか
        public bool ContainsSuccessiveShiftKey => ComboShiftKeys.ContainsSuccessiveShiftKey;

        public bool ContainsSequentialShiftKey { get; set; }

        public bool IsPrefixedSequentialShift => !ContainsUnorderedShiftKey && ContainsSuccessiveShiftKey;

        /// <summary>
        /// Repeatableなキー
        /// </summary>        
        public RepeatableKeyPool RepeatableKeys { get; private set; } = new RepeatableKeyPool();

        public void Clear()
        {
            keyComboDict.Clear();
            comboSubKeys.Clear();
            ComboShiftKeys.Clear();
        }

        /// <summary>
        /// エントリの追加
        /// </summary>
        /// <param name="deckeyList">デコーダ向けのキーリスト</param>
        /// <param name="comboKeyList">同時打鍵検索用キーのリスト</param>
        /// <param name="shiftKind">Prefixの場合は、先頭キーを固定した順列を生成する</param>
        public void AddEntry(List<int> deckeyList, List<int> comboKeyList, ComboKind shiftKind)
        {
            logger.DebugH(() => $"CALLED: keyList={KeyCombinationHelper.EncodeKeyList(deckeyList)}, comboShiftedKeyList={KeyCombinationHelper.EncodeKeyList(comboKeyList)}, ShiftKeyKind={shiftKind}");
            if (deckeyList._notEmpty() && comboKeyList._notEmpty()) {
                var keyCombo = new KeyCombination(deckeyList, comboKeyList, shiftKind);
                var primKey = KeyCombinationHelper.MakePrimaryKey(comboKeyList);
                keyComboDict[primKey] = keyCombo;
                bool bPrefix = shiftKind == ComboKind.PrefixSuccessiveShift;
                foreach (var key in KeyCombinationHelper.MakePermutatedKeys(comboKeyList, bPrefix)) {
                    if (!keyComboDict.ContainsKey(key)) { keyComboDict[key] = keyCombo; }
                }
                comboSubKeys.UnionWith(KeyCombinationHelper.MakeSubKeys(comboKeyList, bPrefix));
            }
        }

        //private KeyCombination getEntry(IEnumerable<int> keyList, int lastKey)
        //{
        //    logger.DebugH(() => $"CALLED: keyList={KeyCombinationHelper.EncodeKeyList(keyList)}, lastKey={lastKey}");
        //    // まずは打鍵されたキーをそのまま使って検索
        //    var combo = keyComboDict._safeGet(KeyCombinationHelper.MakePrimaryKey(keyList, lastKey));
        //    if (combo == null && (keyList.Any(x => x >= DecoderKeys.PLANE_DECKEY_NUM) || lastKey >= DecoderKeys.PLANE_DECKEY_NUM)) {
        //        // 見つからない、かつ拡張シフトコードが含まれていれば、すべてModuloizeしたキーでも試す
        //        combo = keyComboDict._safeGet(KeyCombinationHelper.MakePrimaryKey(keyList.Select(x => Stroke.ModuloizeKey(x)), Stroke.ModuloizeKey(lastKey)));
        //    }
        //    return combo;
        //}

        public KeyCombination GetEntry(IEnumerable<Stroke> strokeList)
        {
            // ストロークリストが空であるか、あるいは全てのストロークがシフトされていたら、null
            if (strokeList._isEmpty() || strokeList.All(x => x.IsCombined)) return null;

            // まずは打鍵されたキーをそのまま使って検索
            var combo = keyComboDict._safeGet(KeyCombinationHelper.MakePrimaryKeyFromOrigDecKey(strokeList));
            if (combo == null && strokeList.Any(x => x.OrigDecoderKey >= DecoderKeys.PLANE_DECKEY_NUM)) {
                // 見つからない、かつ拡張シフトコードが含まれていれば、すべてModuloizeしたキーでも試す
                combo = keyComboDict._safeGet(KeyCombinationHelper.MakePrimaryKeyFromModuloDecKey(strokeList));
            }
            return combo;
        }

        public KeyCombination GetEntry(Stroke stroke)
        {
            return GetEntry(stroke.OrigDecoderKey, stroke.ModuloDecKey);
        }

        public KeyCombination GetEntry(int decKey)
        {
            return GetEntry(decKey, Stroke.ModuloizeKey(decKey));
        }

        public KeyCombination GetEntry(int origDecKey, int moduloDecKey)
        {
            var combo = getEntry(origDecKey);
            if (combo == null && origDecKey >= DecoderKeys.PLANE_DECKEY_NUM) {
                // 見つからない、かつ拡張シフトコードならば、Moduloizeしたキーでも試す
                combo = getEntry(moduloDecKey);
            }
            return combo;
        }

        public KeyCombination getEntry(int decKey)
        {
            //return keyComboDict._safeGet(KeyCombinationHelper.MakePrimaryKey(Stroke.ModuloizeKey(decKey)));
            return keyComboDict._safeGet(KeyCombinationHelper.MakePrimaryKey(decKey));
        }

        /// <summary>
        /// 部分キーに対して、非終端マークをセット<br/>定義されていない部分キーの場合は新しく KeyCombination を生成しておく
        /// </summary>        
        public void SetNonTerminalMarkForSubkeys()
        {
            logger.DebugH($"ENTER: comboSubKeys.Count={comboSubKeys.Count}");
            int i = 0;
            foreach (var key in comboSubKeys) {
                // 部分キーに対して、非終端マークをセット
                if (i < 100) logger.DebugH(() => $"search keyString={key} => list={KeyCombinationHelper.EncodeKeyList(KeyCombinationHelper.DecodeKey(key))}");
                var keyCombo = keyComboDict._safeGet(key);
                if (keyCombo == null) {
                    // 存在していなかった部分キーを追加
                    if (i < 500) logger.DebugH($"Add non terminal subkey: {key}");
                    keyComboDict[key] = keyCombo = new KeyCombination(null, null, ComboKind.None);
                }
                keyCombo.SetNonTerminal();
                ++i;
            }
            logger.DebugH($"LEAVE");
        }

        /// <summary>
        /// Repeatableとして扱いうるキーの設定
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="kind"></param>
        public void AddRepeatableKey(int keyCode)
        {
            logger.DebugH(() => $"CALLED: keyCode={keyCode}");
            if (keyCode > 0) RepeatableKeys.AddRepeatableKey(keyCode);
        }

        public bool IsRepeatableKey(int keyCode)
        {
            return RepeatableKeys.IsRepeatable(keyCode);
        }

        /// <summary>
        /// ShiftKeyとして扱いうるキーの設定
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="kind"></param>
        public void AddComboShiftKey(int keyCode, ComboKind kind)
        {
            logger.DebugH(() => $"CALLED: keyCode={keyCode}, shiftKey={kind}");
            if (keyCode > 0) ComboShiftKeys.AddShiftKey(keyCode, kind);
        }

        /// <summary>
        /// keyCode の ComboKind を返す。<br/>
        /// 0 なら ShiftKey としては扱わない<br/>
        /// 1以上なら、ShiftKeyとしての優先度となる(1が最優先)
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        public ComboKind GetShiftKeyKind(int keyCode)
        {
            return ComboShiftKeys.GetComboKind(keyCode);
        }

        /// <summary>keyCode が ComboShiftKeyとしても扱われるか否かを返す</summary>
        public static bool IsComboShift(int keyCode)
        {
            return ComboShiftKeyPool.IsComboShift(CurrentPool.GetShiftKeyKind(keyCode));
        }

        /// <summary>keyCode が連続シフトとしても扱われるか否かを返す</summary>
        public static bool IsComboSuccessive(int keyCode)
        {
            return ComboShiftKeyPool.IsSuccessiveShift(CurrentPool.GetShiftKeyKind(keyCode));
        }

        /// <summary>keyCode がワンショットシフトとしても扱われるか否かを返す</summary>
        public static bool IsComboOneshot(int keyCode)
        {
            return ComboShiftKeyPool.IsOneshotShift(CurrentPool.GetShiftKeyKind(keyCode));
        }

        /// <summary>keyCode が前置シフトとしても扱われるか否かを返す</summary>
        public static bool IsComboPrefix(int keyCode)
        {
            return ComboShiftKeyPool.IsPrefixShift(CurrentPool.GetShiftKeyKind(keyCode));
        }

        /// <summary>keyCode が順次シフトか否かを返す</summary>
        public static bool IsSequential(int keyCode)
        {
            return ComboShiftKeyPool.IsSequentialShift(CurrentPool.GetShiftKeyKind(keyCode));
        }

        public void DebugPrint()
        {
            int i = 0;
            foreach (var pair in keyComboDict) {
                var key = KeyCombinationHelper.DecodeKeyString(pair.Key);
                var deckeys = pair.Value.DecKeysDebugString()._orElse("NONE");
                if (i < 500) logger.DebugH($"{key}={deckeys} {pair.Value.IsTerminal}");
                ++i;
            }
            foreach (var pair in ComboShiftKeys.Pairs) {
                logger.DebugH($"ShiftKey: {pair.Key}={pair.Value}");
            }
        }

        public void DebugPringFile(string filename)
        {
            var path = KanchokuIni.Singleton.KanchokuDir._joinPath(filename);
            List<string> lines = new List<string>();
            foreach (var pair in keyComboDict) {
                var key = KeyCombinationHelper.DecodeKeyString(pair.Key);
                var deckeys = pair.Value.DecKeysDebugString()._orElse("NONE");
                lines.Add($"{key}={deckeys} {pair.Value.IsTerminal}");
            }
            foreach (var pair in ComboShiftKeys.Pairs) {
                lines.Add($"ShiftKey: {pair.Key}={pair.Value}");
            }
            Helper.WriteLinesToFile(path, lines, (e) => logger.Error(e._getErrorMsg()));
        }
    }
}

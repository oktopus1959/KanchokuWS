﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace KanchokuWS.CombinationKeyStroke.DeterminerLib
{
    using ComboKind = ComboShiftKeyPool.ComboKind;

    class KeyCombinationPool
    {
        private static Logger logger = Logger.GetLogger();

        /// <summary>主テーブル用のCombinaitonPool</summary>        
        public static KeyCombinationPool SingletonK1 = new KeyCombinationPool() { bEisuMode = false };

        /// <summary>主テーブル・英数用のCombinaitonPool</summary>        
        public static KeyCombinationPool SingletonA1 = new KeyCombinationPool() { bEisuMode = true };

        /// <summary>副テーブル用のCombinaitonPool</summary>        
        public static KeyCombinationPool SingletonK2 = new KeyCombinationPool() { bEisuMode = false };

        /// <summary>副テーブル・英数用のCombinaitonPool</summary>        
        public static KeyCombinationPool SingletonA2 = new KeyCombinationPool() { bEisuMode = true };

        /// <summary>第3テーブル用のCombinaitonPool</summary>        
        public static KeyCombinationPool SingletonK3 = new KeyCombinationPool() { bEisuMode = false };

        /// <summary>第3テーブル・英数用のCombinaitonPool</summary>        
        public static KeyCombinationPool SingletonA3 = new KeyCombinationPool() { bEisuMode = true };

        private static KeyCombinationPool currentPoolK = SingletonK1;

        private static KeyCombinationPool currentPoolA = SingletonA1;

        // 現在使用中のKeyCombinaitonPool
        public static KeyCombinationPool CurrentPool { get; private set; } = SingletonK1;

        private static string detectCurrentPool()
        {
            return CurrentPool == SingletonK1 ? "K1"
                : CurrentPool == SingletonK2 ? "K2"
                : CurrentPool == SingletonK3 ? "K3"
                : CurrentPool == SingletonA1 ? "A1"
                : CurrentPool == SingletonA2 ? "A2"
                : "A3";
        }

        public static void Initialize()
        {
            if (Settings.LoggingTableFileInfo) logger.DebugH("CALLED");
            SingletonK1.Clear();
            SingletonA1.Clear();
            SingletonK2.Clear();
            SingletonA2.Clear();
            SingletonK3.Clear();
            SingletonA3.Clear();
            currentPoolK = SingletonK1;
            currentPoolA = SingletonA1;
            CurrentPool = currentPoolA;
        }

        public static void ChangeCurrentPoolBySelectedTable(int tableNum, bool bDecoderOn)
        {
            if (tableNum == 3) {
                currentPoolK = SingletonK3;
                currentPoolA = SingletonA3;
            } else if (tableNum == 2) {
                currentPoolK = SingletonK2;
                currentPoolA = SingletonA2;
            } else  {
                currentPoolK = SingletonK1;
                currentPoolA = SingletonA1;
            }
            ChangeCurrentPoolByDecoderMode(bDecoderOn);
            if (Settings.LoggingTableFileInfo) logger.DebugH(() => $"CurrentPool={detectCurrentPool()}, Enabled={CurrentPool.Enabled}");
        }

        public static void ChangeCurrentPoolByDecoderMode(bool bDecoderOn)
        {
            if (bDecoderOn) {
                CurrentPool = currentPoolK;
            } else {
                CurrentPool = currentPoolA;
            }
            if (Settings.LoggingTableFileInfo) logger.DebugH(() => $"CurrentPool={detectCurrentPool()}, Enabled={CurrentPool.Enabled}");
        }

        public static void UsePrimaryPool(bool bDecoderOn)
        {
            //CurrentPool = Singleton1;
            ChangeCurrentPoolBySelectedTable(1, bDecoderOn);
        }

        public static void UseSecondaryPool(bool bDecoderOn)
        {
            //CurrentPool = Singleton2;
            ChangeCurrentPoolBySelectedTable(2, bDecoderOn);
        }

        /// <summary>
        /// 同時打鍵組合せ(テーブル記述順とソートされたものを登録)
        /// </summary>        
        class KeyComboDictionary
        {

            private Dictionary<string, KeyCombination> dict = new Dictionary<string, KeyCombination>();

            public int Count { get { return dict.Count; } }

            // 順不同の場合は、key はソートされている
            public void Add(string key, KeyCombination combo, bool bForce)
            {
                if (bForce || !dict.ContainsKey(key)) dict[key] = combo;
            }

            // keyList は打鍵順のまま
            public KeyCombination Get(List<int> keyList, bool bStackLikePreffered)
            {
                // まず打鍵順のままで取得してみる
                var keystr = keyList._keyString();
                var keyCombo = dict._safeGet(keystr);
                var keyComboStackLike = dict._safeGet(keystr + "*");
                if (bStackLikePreffered && keyComboStackLike != null) return keyComboStackLike;
                if (keyCombo != null) return keyCombo;
                if (keyComboStackLike != null) return keyComboStackLike;

                // ダメなら打鍵順をソートして取得してみる
                keystr = keyList._sortedKeyString();
                keyCombo = dict._safeGet(keystr);
                keyComboStackLike = dict._safeGet(keystr + "*");
                if (bStackLikePreffered && keyComboStackLike != null && keyComboStackLike.IsUnordered) return keyComboStackLike;
                if (keyCombo != null && keyCombo.IsUnordered) return keyCombo;
                if (keyComboStackLike != null && keyComboStackLike.IsUnordered) return keyComboStackLike;

                return null;
            }

            public KeyCombination Get(int key)
            {
                return dict._safeGet(key._keyString());
            }

            public KeyCombination Get(string key)
            {
                return dict._safeGet(key);
            }

            public IEnumerable<KeyCombination> GetKeyComboList()
            {
                return dict.Values;
            }

            public void Clear()
            {
                dict.Clear();
            }

            public void DebugPrint(bool bAll = false)
            {
                if (Settings.LoggingTableFileInfo) {
                    int i = 0;
                    foreach (var pair in dict) {
                        if (bAll || i < 500) {
                            logger.DebugH(makeDebugLine(pair));
                        }
                        ++i;
                    }
                }
            }

            public void GetDebugLines(List<string> lines)
            {
                foreach (var pair in dict) {
                    lines.Add(makeDebugLine(pair));
                }
            }

            private string makeDebugLine(KeyValuePair<string, KeyCombination> pair)
            {
                var key = pair.Key;
                var val = pair.Value;
                var deckeys = val.DecKeysDebugString()._orElse("NONE");
                var sb = new StringBuilder();
                sb.Append("Combo=");
                switch (val.ShiftKind) {
                    case ComboKind.None:
                        sb.Append("None");
                        break;
                    case ComboKind.SequentialShift:
                        sb.Append("Sequential");
                        break;
                    case ComboKind.PrefixOrSequentialShift:
                        sb.Append("PrefixSequential");
                        break;
                    case ComboKind.PrefixSuccessiveShift:
                        sb.Append("Ordered");
                        break;
                    case ComboKind.UnorderedSuccessiveShift:
                        sb.Append("Mutual");
                        break;
                    case ComboKind.UnorderedOneshotShift:
                        sb.Append("MutualOneshot");
                        break;
                }
                if (val.IsStackLikeCombo) sb.Append(" StackLike");
                return $"{key}={deckeys} HasString={val.HasString} Terminal={val.IsTerminal} {sb.ToString()}";
            }
        }

        /// <summary>
        /// 同時打鍵組合せ(テーブル記述順とソートされたものを登録)
        /// </summary>        
        private KeyComboDictionary keyComboDict = new KeyComboDictionary();

        public int Count { get { return keyComboDict.Count; } }

        /// <summary>
        /// 固定順の組合せを検索するための辞書
        /// </summary>        
        private KeyComboDictionary orderedComboDict = new KeyComboDictionary();

        private bool bEisuMode = false;

        // 英数用か
        public bool ForEisu => bEisuMode;

        // 利用可能か
        public bool Enabled => Count > 0;

        // 同時打鍵組合せの部分キーの順列集合(これらは、最後に非終端となるキー順列として使う)
        private Dictionary<string, bool> comboSubKeys = new Dictionary<string, bool>();

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

        public bool ContainsSequentialShiftKey { private get; set; }

        public bool IsPrefixedOrSequentialShift => !ContainsUnorderedShiftKey && (ContainsSuccessiveShiftKey || ContainsSequentialShiftKey);

        ///// <summary>(デコーダOFFでも)常に有効な同時打鍵列があるか</summary>
        //public bool HasComboEffectiveAlways { get; set; }

        /// <summary>
        /// Repeatableなキー
        /// </summary>        
        public MiscKeyPool MiscKeys { get; private set; } = new MiscKeyPool();

        public void Clear()
        {
            keyComboDict.Clear();
            comboSubKeys.Clear();
            ComboShiftKeys.Clear();
            MiscKeys.Clear();
            //HasComboEffectiveAlways = false;
        }

        /// <summary>
        /// エントリの追加
        /// </summary>
        /// <param name="deckeyList">デコーダ向けのキーリスト</param>
        /// <param name="comboKeyList">同時打鍵検索用キーのリスト</param>
        /// <param name="shiftKind">Prefixの場合は、与えられたキー順のみ有効</param>
        public void AddEntry(List<int> deckeyList, List<int> comboKeyList, ComboKind shiftKind, bool hasStr, bool hasFunc, bool comboBlocked, bool stackLike)
        {
            if (Settings.LoggingTableFileInfo) logger.DebugH(() =>
                $"CALLED: keyList={deckeyList._keyString()}, comboShiftedKeyList={comboKeyList._keyString()}, ShiftKeyKind={shiftKind}, HasString={hasStr}");

            if (deckeyList._notEmpty() && comboKeyList._notEmpty()) {
                bool bUnordered = shiftKind >= ComboKind.UnorderedSuccessiveShift;

                // 順不同の場合同時打鍵列をソートする
                if (bUnordered) comboKeyList = comboKeyList.OrderBy(x => x).ToList();

                // 順不同の場合はソートされた並びで登録、固定順の場合は配列テーブルに記述された並びで登録
                string comboKeyStr = comboKeyList._keyString();

                // 先押し後離し方式の場合は、キー文字列に '*' を付加しておき、通常方式とは区別できるようにしておく
                if (stackLike) comboKeyStr += "*";

                var keyCombo = new KeyCombination(deckeyList, comboKeyStr, shiftKind, hasStr, hasFunc, comboBlocked, stackLike);
                keyComboDict.Add(comboKeyStr, keyCombo, hasStr);

                if (!bUnordered) {
                    // 固定順のやつは、後で順不定のやつに含まれたら非終端にするために、ここでorderedComboDictに登録しておく
                    orderedComboDict.Add(comboKeyList.OrderBy(x => x)._keyString(), keyCombo, true);
                }

                // 部分キー文字列を蓄積しておく
                foreach (var ks in comboKeyList._makeSubKeys(bUnordered)) {
                    if (!comboSubKeys.ContainsKey(ks) || bUnordered) {
                        // 未登録または順不定だったら上書き登録
                        // (つまり、固定順より順不定を優先させておく; これは SetNonTerminalMarkForSubkeys()と KeyComboDictionary.Get() で必要となる)
                        comboSubKeys[ks] = bUnordered;
                    }
                }
            }
        }

        public KeyCombination GetEntry(IEnumerable<Stroke> strokeList, bool bStackLikePreffered = true)
        {
            // ストロークリストが空であるか、あるいは全てのストロークがシフトされていたら、null
            if (strokeList._isEmpty() || strokeList.All(x => x.IsCombined)) return null;

            // まずは打鍵されたキーをそのまま使って検索
            var combo = keyComboDict.Get(strokeList._toOrigDecKeyList(), bStackLikePreffered);
            if (combo == null && strokeList.Any(x => x.OrigDecoderKey >= DecoderKeys.SHIFT_A_DECKEY_START)) {
                // 見つからない、かつ拡張シフトコードが含まれていれば、すべてModuloizeしたキーでも試す
                combo = keyComboDict.Get(strokeList._toModuloDecKeyList(), bStackLikePreffered);
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
            if (combo == null && origDecKey >= DecoderKeys.SHIFT_A_DECKEY_START) {
                // 見つからない、かつ拡張シフトコードならば、Moduloizeしたキーでも試す
                combo = getEntry(moduloDecKey);
            }
            return combo;
        }

        private KeyCombination getEntry(int decKey)
        {
            //return keyComboDict.Get(KeyCombinationHelper.MakePrimaryKey(Stroke.ModuloizeKey(decKey)));
            return keyComboDict.Get(decKey);
        }

        /// <summary>
        /// 部分キーに対して、非終端マークをセット<br/>定義されていない部分キーの場合は新しく KeyCombination を生成しておく
        /// </summary>        
        public void SetNonTerminalMarkForSubkeys(bool bEisu)
        {
            if (Settings.LoggingTableFileInfo) logger.DebugH($"ENTER: comboSubKeys.Count={comboSubKeys.Count}");
            int i = 0;
            foreach (var pair in comboSubKeys) {
                // 部分キーに対して、非終端マークをセット
                //if (i < 100) if (Settings.LoggingTableFileInfo) logger.DebugH(() => $"search keyString={key} => list={KeyCombinationHelper.EncodeKeyList(KeyCombinationHelper.DecodeKey(key))}");
                string subkey = pair.Key;
                ComboKind shiftKind = pair.Value ? ComboKind.UnorderedSuccessiveShift : ComboKind.None;
                int keylen = subkey._keyLengh();
                var keyCombo = keyComboDict.Get(subkey);
                if (keyCombo == null || keylen < 3) {
                    // 部分キーが存在して、その長さが3以上なら、非終端にはしない
                    // ⇒「Sp,J,K,X」で「ぽ」、「Sp,X,J」で「りゃ」のようなケースで、「りゃ」のほうを出したい
                    if (i < 500 && Settings.LoggingTableFileInfo) logger.DebugH(() => $"Set sub keyString={subkey}");
                    if (keyCombo == null) {
                        // 存在していなかった部分キーを追加
                        if (i < 500 && Settings.LoggingTableFileInfo) logger.DebugH($"Add non terminal subkey: {subkey}");
                        List<int> keyList = null;
                        if (bEisu && keylen == 1) {
                            // 英数モードの場合は、1文字キーを単打可能に設定する
                            // keyListが空でなければ hasStr==trueになる。
                            // 薙刀式で HJ をImeOnに割り当てている場に H, J を単打できるようにするために必要
                            keyList = subkey._decodeKeyStr();
                        }
                        keyCombo = new KeyCombination(keyList, null, shiftKind, keyList._notEmpty(), false, false, false);
                        keyComboDict.Add(subkey, keyCombo, true);
                    }
                    keyCombo.SetNonTerminal();

                    // 順不定のサブキーになっている固定順キーがあったら、それを非終端としてマークしておく
                    if (keylen >= 2) {
                        var orderedSubkey = subkey._split(':').Select(x => x._parseInt()).OrderBy(x => x)._keyString();
                        keyCombo = orderedComboDict.Get(orderedSubkey);
                        if (keyCombo != null) {
                            if (i < 500 && Settings.LoggingTableFileInfo) logger.DebugH(() => $"Set nonterminal for ordered sub keyString={orderedSubkey}");
                            keyCombo.SetNonTerminal();
                        }
                    }
                    ++i;
                }
            }
            if (Settings.LoggingTableFileInfo) logger.DebugH($"LEAVE");
        }

        /// <summary>
        /// Repeatableとして扱いうるキーの設定
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="kind"></param>
        public void AddRepeatableKey(int keyCode)
        {
            if (Settings.LoggingTableFileInfo) logger.DebugH(() => $"CALLED: keyCode={keyCode}");
            if (keyCode >= 0) MiscKeys.AddRepeatableKey(keyCode);
        }

        public bool IsRepeatableKey(int keyCode)
        {
            return MiscKeys.IsRepeatable(keyCode);
        }

        ///// <summary>
        ///// PreRewriteとして扱いうるキーの設定
        ///// </summary>
        ///// <param name="keyCode"></param>
        ///// <param name="kind"></param>
        //public void AddPreRewriteKey(int keyCode)
        //{
        //    logger.DebugH(() => $"CALLED: keyCode={keyCode}");
        //    if (keyCode >= 0) MiscKeys.AddPreRewriteKey(keyCode);
        //}

        //public bool IsPreRewriteKey(int keyCode)
        //{
        //    return MiscKeys.IsPreRewrite(keyCode);
        //}

        /// <summary>
        /// ShiftKeyとして扱いうるキーの設定
        /// </summary>
        /// <param name="keyCodes"></param>
        /// <param name="kind"></param>
        public void AddComboShiftKeys(List<int> keyCodes, ComboKind kind)
        {
            if (Settings.LoggingTableFileInfo) logger.DebugH(() => $"CALLED: keyCode={keyCodes._keyString()}, shiftKey={kind}");
            if (keyCodes._notEmpty()) {
                if (keyCodes[0] >= 0) ComboShiftKeys.AddShiftKey(keyCodes[0], kind);
                if (keyCodes.Count > 1 && !DecoderKeys.IsSpaceOrFuncKey(keyCodes[0]) && DeterminerLib.ComboShiftKeyPool.IsUnorderedSuccessiveShift(kind)) {
                    // 先頭キーが文字キーで、相互連続シフト可能なら、第2打鍵以降もシフトキーとして登録
                    // (薙刀式で「よ」をシフトキーとして拗音定義した場合に、「き」が第1打鍵の場合も連続シフト可能にするため)
                    foreach (var kc in keyCodes.Skip(1)) {
                        if (kc >= 0) ComboShiftKeys.AddShiftKey(kc, kind);
                    }
                }
            }
        }

        /// <summary>
        /// ShiftKeyとして扱いうるキーの設定
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="kind"></param>
        public void AddComboShiftKey(int keyCode, ComboKind kind)
        {
            if (Settings.LoggingTableFileInfo) logger.DebugH(() => $"CALLED: keyCode={keyCode}, shiftKey={kind}");
            if (keyCode >= 0) ComboShiftKeys.AddShiftKey(keyCode, kind);
        }

        /// <summary>
        /// 登録されている ComboShiftキーのうち、4つ以上のcomboの先頭キーになっているもの
        /// </summary>
        public void AddMajorComboShiftKeys()
        {
            if (Settings.LoggingTableFileInfo) logger.DebugH(() => $"CALLED");
            Dictionary<int, int> comboCounts = new Dictionary<int, int>();
            foreach (var keyCombo in keyComboDict.GetKeyComboList()) {
                if (keyCombo.IsUnordered) {
                    int firstKey = keyCombo.DecKeyList._getFirst();
                    if (comboCounts.ContainsKey(firstKey)) {
                        comboCounts[firstKey] += 1;
                    } else {
                        comboCounts[firstKey] = 1;
                    }
                }
            }
            foreach (var pair in comboCounts) {
                if (pair.Value >= 4) {
                    ComboShiftKeys.AddMajorComboKey(Stroke.ModuloizeKey(pair.Key));
                }
            }
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

        /// <summary>keyCode がMajorなComboシフトか否かを返す</summary>
        public bool IsMajorComboShift(int keyCode)
        {
            return ComboShiftKeys.IsMajorComboKey(keyCode);
        }

        /// <summary>keyCode が ComboShiftKeyとしても扱われるか否かを返す</summary>
        public static bool IsComboShift(int keyCode)
        {
            return ComboShiftKeyPool.IsComboShift(CurrentPool.GetShiftKeyKind(keyCode));
        }

        /// <summary>keyCode が SpaceOrFuncComboShiftKeyとしても扱われるか否かを返す</summary>
        public static bool IsSpaceOrFuncComboShift(int keyCode)
        {
            return DecoderKeys.IsSpaceOrFuncKey(keyCode) && IsComboShift(keyCode);
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

        public void DebugPrint(bool bAll = false)
        {
            if (Settings.LoggingTableFileInfo) {
                keyComboDict.DebugPrint();
                foreach (var pair in ComboShiftKeys.Pairs) {
                    logger.DebugH($"ShiftKey: {pair.Key}={pair.Value}");
                }
                logger.DebugH($"{MiscKeys.DebugString()}");
            }
        }

        public void DebugPrintFile(string filename)
        {
            var path = KanchokuIni.Singleton.KanchokuDir._joinPath(filename);
            List<string> lines = new List<string>();
            //lines.Add($"HasComboEffectiveAlways={HasComboEffectiveAlways}");
            keyComboDict.GetDebugLines(lines);
            foreach (var pair in ComboShiftKeys.Pairs) {
                lines.Add($"ShiftKey: {pair.Key}={pair.Value}");
            }
            lines.Add($"{MiscKeys.DebugString()}");
            Helper.WriteLinesToFile(path, lines, (e) => logger.Error(e._getErrorMsg()));
        }
    }
}

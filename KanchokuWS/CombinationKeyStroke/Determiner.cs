using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KanchokuWS.CombinationKeyStroke.DeterminerLib;
using Utils;

namespace KanchokuWS.CombinationKeyStroke
{
    /// <summary>
    /// キー入力の時系列に対して、同時打鍵などの判定を行って、出力文字列を決定する
    /// </summary>
    class Determiner
    {
        private static Logger logger = Logger.GetLogger(true);

        // 同時打鍵保持リスト
        private StrokeList strokeList = new StrokeList();

        /// <summary>
        /// 初期化と同時打鍵組合せ辞書の読み込み
        /// </summary>
        /// <param name="tableFile">主テーブルファイル名</param>
        /// <param name="tableFile2">副テーブルファイル名</param>
        public void Initialize(string tableFile, string tableFile2)
        {
            Settings.ClearSpecificDecoderSettings();
            KeyCombinationPool.Initialize();
            Clear();

            var parser = new TableFileParser(KeyCombinationPool.Singleton1);
            parser.ParseTable(tableFile);

            if (tableFile2._notEmpty()) {
                var parser2 = new TableFileParser(KeyCombinationPool.Singleton2);
                parser2.ParseTable(tableFile2);
            }
        }

        /// <summary>
        /// 選択されたテーブルファイルに合わせて、KeyComboPoolを入れ替える
        /// </summary>
        public void ExchangeKeyCombinationPool()
        {
            KeyCombinationPool.ExchangeCurrentPool();
        }

        public bool IsEnabled => KeyCombinationPool.CurrentPool.Enabled;

        /// <summary>
        /// 同時打鍵リストをクリアする
        /// </summary>
        public void Clear()
        {
            strokeList.Clear();
        }

        /// <summary>
        /// キーの押下<br/>押下されたキーをキューに積むだけ。同時打鍵などの判定はキーの解放時に行う。
        /// </summary>
        /// <param name="decKey">押下されたキーのデコーダコード</param>
        /// <returns>同時打鍵の可能性があるなら true を返す<br/>無効なら false を返す</returns>
        public bool KeyDown(int decKey)
        {
            var dtNow = DateTime.Now;
            logger.DebugH(() => $"ENTER: dt={dtNow.ToString("HH:mm:ss.fff")}, decKey={decKey}");
            bool flag = false;
            var stroke = new Stroke(decKey, dtNow);
            logger.DebugH(() => stroke.DebugString());
            if (strokeList.DetectKeyRepeat(stroke)) {
                // キーリピートが発生した場合
                if (KeyCombinationPool.CurrentPool.IsRepeatableKey(decKey)) {
                    logger.DebugH("Key repeated");
                    flag = false;
                } else {
                    logger.DebugH("Key repeat ignored");
                    flag = true;
                }
            } else if (strokeList.Count > 0) {
                flag = true;
                strokeList.Add(stroke);
                logger.DebugH("Add new stroke: PATH-1");
            } else {
                var combo = KeyCombinationPool.CurrentPool.GetEntry(stroke);
                logger.DebugH(() => $"combo: {(combo == null ? "null" : "FOUND")}, IsTerminal={combo?.IsTerminal ?? true}");
                if (combo != null && !combo.IsTerminal) {
                    flag = true;
                    strokeList.Add(stroke);
                    logger.DebugH("Add new stroke: PATH-2");
                }
            }
            logger.DebugH(() => $"LEAVE: {flag}: {strokeList.ToDebugString()}");
            return flag;
        }

        /// <summary>
        /// キーの解放
        /// </summary>
        /// <param name="decKey">解放されたキーのデコーダコード</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダコード列を返す。<br/>確定しなければ null を返す</returns>
        public List<int> KeyUp(int decKey)
        {
            logger.DebugH(() => $"ENTER: decKey={decKey}");
            return strokeList.GetKeyCombination(decKey, DateTime.Now);
        }

        /// <summary>
        /// Singleton オブジェクトを返す
        /// </summary>
        public static Determiner Singleton { get; private set; } = new Determiner();
    }
}

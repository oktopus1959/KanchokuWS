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
            parser.ParseTable(tableFile, "tmp/tableFile1.tbl");

            if (tableFile2._notEmpty()) {
                var parser2 = new TableFileParser(KeyCombinationPool.Singleton2);
                parser2.ParseTable(tableFile2, "tmp/tableFile2.tbl");
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
        /// キーの押下<br/>押下されたキーをキューに積み、可能であれば同時打鍵判定も行う
        /// </summary>
        /// <param name="decKey">押下されたキーのデコーダコード</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダコード列を返す。<br/>確定しなければ null を返す</returns>
        public List<int> KeyDown(int decKey, Action<int> handleComboKeyRepeat)
        {
            var dtNow = DateTime.Now;
            logger.DebugH(() => $"\nENTER: dt={dtNow.ToString("HH:mm:ss.fff")}, decKey={decKey}");

            List<int> result = null;

            var stroke = new Stroke(decKey, dtNow);
            logger.DebugH(() => stroke.DebugString());

            // キーリピートのチェック
            if (strokeList.DetectKeyRepeat(stroke)) {
                // キーリピートが発生した場合
                if (KeyCombinationPool.CurrentPool.IsRepeatableKey(decKey)) {
                    // キーリピートが可能なキーだった(BacSpaceとか)ので、それを返す
                    logger.DebugH("Key repeated");
                    result = Helper.MakeList(decKey);
                } else {
                    // キーリピートが不可なキーは無視
                    logger.DebugH("Key repeat ignored");
                    // 同時打鍵シフトキーの場合は、リピートハンドラを呼び出す
                    if (stroke.IsComboShift) handleComboKeyRepeat(stroke.ComboShiftDecKey);
                }
            } else {
                // キーリピートではない通常の押下の場合は、同時打鍵判定を行う
                var combo = KeyCombinationPool.CurrentPool.GetEntry(stroke);
                bool isStrokeListEmpty = strokeList.IsEmpty();
                logger.DebugH(() => $"combo: {(combo == null ? "null" : "FOUND")}, IsTerminal={combo?.IsTerminal ?? true}, isStrokeListEmpty={isStrokeListEmpty}");
                if (combo != null || !isStrokeListEmpty) {
                    // 押下されたのは同時打鍵に使われる可能性のあるキーだった、あるいは同時打鍵シフト後の第2打鍵だったので、キューに追加して同時打鍵判定を行う
                    strokeList.Add(stroke);
                    result = strokeList.GetKeyCombinationWhenKeyDown(decKey, DateTime.Now);
                } else {
                    // 同時打鍵には使われないキーなので、そのまま返す
                    logger.DebugH("Return ASIS");
                    result = Helper.MakeList(decKey);
                }
            }

            logger.DebugH(() => $"LEAVE: result={result._keyString()._orElse("empty")}: {strokeList.ToDebugString()}");
            return result;
        }

        /// <summary>
        /// キーの解放。同時打鍵判定も行う。
        /// </summary>
        /// <param name="decKey">解放されたキーのデコーダコード</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダコード列を返す。<br/>確定しなければ null を返す</returns>
        public List<int> KeyUp(int decKey)
        {
            logger.DebugH(() => $"\nENTER: decKey={decKey}");
            var result = strokeList.GetKeyCombinationWhenKeyUp(decKey, DateTime.Now);
            logger.DebugH(() => $"LEAVE: result={result._keyString()._orElse("empty")}: {strokeList.ToDebugString()}");
            return result;
        }

        /// <summary>
        /// Singleton オブジェクトを返す
        /// </summary>
        public static Determiner Singleton { get; private set; } = new Determiner();
    }
}

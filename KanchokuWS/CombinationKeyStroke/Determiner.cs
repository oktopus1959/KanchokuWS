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

        private DeterminerImpl impl = new DeterminerImpl();

        /// <summary>
        /// テーブルファイルを読み込んで同時打鍵定義を初期化する
        /// </summary>
        public void Initialize(string tableFile, string tableFile2)
        {
            logger.InfoH(() => $"CALLED: tableFile={tableFile}, tableFile2={tableFile2}");
            impl.Initialize(tableFile, tableFile2);
        }

        /// <summary>
        /// 選択されたテーブルファイルに合わせて、KeyComboPoolを入れ替える
        /// </summary>
        public void ExchangeKeyCombinationPool()
        {
            impl.ExchangeKeyCombinationPool();
        }

        /// <summary>
        /// 同時打鍵リストをクリアする
        /// </summary>
        public void Clear()
        {
            impl.Clear();
        }

        /// <summary>
        /// キーの押下<br/>押下されたキーをキューに積むだけ。同時打鍵などの判定はキーの解放時に行う。
        /// </summary>
        /// <param name="decKey">押下されたキーのデコーダコード</param>
        /// <returns>同時打鍵が有効なら true を返す<br/>無効なら false を返す</returns>
        public bool KeyDown(int decKey)
        {
            logger.DebugH(() => $"\nCALLED: decKey={decKey}, Determiner.Enabled={impl.IsEnabled}");
            if (!impl.IsEnabled) return false;
            return impl.KeyDown(decKey);
        }

        /// <summary>
        /// キーの解放
        /// </summary>
        /// <param name="decKey">解放されたキーのデコーダコード</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダコード列を返す。<br/>確定しなければ null を返す</returns>
        public List<int> KeyUp(int decKey)
        {
            logger.DebugH(() => $"\nCALLED: decKey={decKey}");
            return impl.IsEnabled ? impl.KeyUp(decKey) : null;
        }

        /// <summary>
        /// Singleton オブジェクトを返す
        /// </summary>
        public static Determiner Singleton { get; private set; } = new Determiner();
    }
}

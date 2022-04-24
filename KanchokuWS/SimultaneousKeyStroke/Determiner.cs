﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KanchokuWS.SimultaneousKeyStroke.DeterminerLib;
using Utils;

namespace KanchokuWS.SimultaneousKeyStroke
{
    /// <summary>
    /// キー入力の時系列に対して、同時打鍵などの判定を行って、出力文字列を決定する
    /// </summary>
    partial class Determiner
    {
        private static Logger logger = Logger.GetLogger();

        /// <summary>
        /// テーブルファイルを読み込んで同時打鍵定義を初期化する
        /// </summary>
        public void Initialize(string tableFile)
        {
            logger.InfoH(() => $"CALLED: tableFile={tableFile}");
            initialize(tableFile);
        }

        /// <summary>
        /// キーの押下
        /// </summary>
        /// <param name="keyInfo">押下されたキーの情報</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダキー列を返す。<br/>確定しなければ null を返す</returns>
        public List<DecoderKeyCode> KeyDown(KeyCodeInfo keyInfo)
        {
            return keyDown(keyInfo);
        }

        /// <summary>
        /// キーの解放
        /// </summary>
        /// <param name="keyInfo">解放されたキーの情報</param>
        /// <returns>出力文字列が確定すれば、それを出力するためのデコーダキー列を返す。<br/>確定しなければ null を返す</returns>
        public List<DecoderKeyCode> KeyUp(KeyCodeInfo keyInfo)
        {
            return keyUp(keyInfo);
        }

        /// <summary>
        /// Singleton オブジェクトを返す
        /// </summary>
        public static Determiner Singleton { get; private set; } = new Determiner();
    }
}

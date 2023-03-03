using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Utils;

namespace KanchokuWS.Domain
{
    static class _FaceCharVKey
    {
        private static Dictionary<string, uint> faceToVkeyJP = new Dictionary<string, uint>() {
            {" ", (uint)Keys.Space },
            {"SPACE", (uint)Keys.Space },
            {"0", (uint)Keys.D0 },
            {"1", (uint)Keys.D1 },
            {"2", (uint)Keys.D2 },
            {"3", (uint)Keys.D3 },
            {"4", (uint)Keys.D4 },
            {"5", (uint)Keys.D5 },
            {"6", (uint)Keys.D6 },
            {"7", (uint)Keys.D7 },
            {"8", (uint)Keys.D8 },
            {"9", (uint)Keys.D9 },
            {"A", (uint)Keys.A },
            {"B", (uint)Keys.B },
            {"C", (uint)Keys.C },
            {"D", (uint)Keys.D },
            {"E", (uint)Keys.E },
            {"F", (uint)Keys.F },
            {"G", (uint)Keys.G },
            {"H", (uint)Keys.H },
            {"I", (uint)Keys.I },
            {"J", (uint)Keys.J },
            {"K", (uint)Keys.K },
            {"L", (uint)Keys.L },
            {"M", (uint)Keys.M },
            {"N", (uint)Keys.N },
            {"O", (uint)Keys.O },
            {"P", (uint)Keys.P },
            {"Q", (uint)Keys.Q },
            {"R", (uint)Keys.R },
            {"S", (uint)Keys.S },
            {"T", (uint)Keys.T },
            {"U", (uint)Keys.U },
            {"V", (uint)Keys.V },
            {"W", (uint)Keys.W },
            {"X", (uint)Keys.X },
            {"Y", (uint)Keys.Y },
            {"Z", (uint)Keys.Z },
            { "COLON", (uint)Keys.Oem1 },       // ba
            { ":", (uint)Keys.Oem1 },           // ba
            { "*", (uint)Keys.Oem1 },           // ba
            { "PLUS", (uint)Keys.Oemplus },     // bb
            { ";", (uint)Keys.Oemplus },        // bb
            { "+", (uint)Keys.Oemplus },        // bb
            { "COMMA", (uint)Keys.Oemcomma },   // bc
            { ",", (uint)Keys.Oemcomma },       // bc
            { "<", (uint)Keys.Oemcomma },       // bc
            { "MINUS", (uint)Keys.OemMinus },   // bd
            { "-", (uint)Keys.OemMinus },       // bd
            { "=", (uint)Keys.OemMinus },       // bd
            { "PERIOD", (uint)Keys.OemPeriod }, // be
            { ".", (uint)Keys.OemPeriod },      // be
            { ">", (uint)Keys.OemPeriod },      // be
            { "SLASH", (uint)Keys.Oem2 },       // bf
            { "/", (uint)Keys.Oem2 },           // bf
            { "?", (uint)Keys.Oem2 },           // bf
            { "BQUOTE", (uint)Keys.Oem3 },      // c0/106
            { "@", (uint)Keys.Oem3 },           // c0/106
            { "`", (uint)Keys.Oem3 },           // c0/106
            { "OEM4", (uint)Keys.Oem4 },        // db
            { "[", (uint)Keys.Oem4 },           // db
            { "{", (uint)Keys.Oem4 },           // db
            { "OEM5", (uint)Keys.Oem5 },        // dc
            { "\\", (uint)Keys.Oem5 },          // dc
            { "|", (uint)Keys.Oem5 },           // dc
            { "OEM6", (uint)Keys.Oem6 },        // dd
            { "]", (uint)Keys.Oem6 },           // dd
            { "}", (uint)Keys.Oem6 },           // dd
            { "OEM7", (uint)Keys.Oem7 },        // de
            { "^", (uint)Keys.Oem7 },           // de
            { "~", (uint)Keys.Oem7 },           // de
            { "OEM8", (uint)Keys.Oem8 },        // df
            { "OEM102", (uint)Keys.Oem102 },    // e2/106
            { "＼", (uint)Keys.Oem102 },        // e2/106
            { "_", (uint)Keys.Oem102 },         // de
        }; // faceToVkeyJP

        private static Dictionary<string, uint> faceToVkeyUS = new Dictionary<string, uint>() {
            {" ", (uint)Keys.Space },
            {"SPACE", (uint)Keys.Space },
            {"0", (uint)Keys.D0 },
            {"1", (uint)Keys.D1 },
            {"2", (uint)Keys.D2 },
            {"3", (uint)Keys.D3 },
            {"4", (uint)Keys.D4 },
            {"5", (uint)Keys.D5 },
            {"6", (uint)Keys.D6 },
            {"7", (uint)Keys.D7 },
            {"8", (uint)Keys.D8 },
            {"9", (uint)Keys.D9 },
            {"A", (uint)Keys.A },
            {"B", (uint)Keys.B },
            {"C", (uint)Keys.C },
            {"D", (uint)Keys.D },
            {"E", (uint)Keys.E },
            {"F", (uint)Keys.F },
            {"G", (uint)Keys.G },
            {"H", (uint)Keys.H },
            {"I", (uint)Keys.I },
            {"J", (uint)Keys.J },
            {"K", (uint)Keys.K },
            {"L", (uint)Keys.L },
            {"M", (uint)Keys.M },
            {"N", (uint)Keys.N },
            {"O", (uint)Keys.O },
            {"P", (uint)Keys.P },
            {"Q", (uint)Keys.Q },
            {"R", (uint)Keys.R },
            {"S", (uint)Keys.S },
            {"T", (uint)Keys.T },
            {"U", (uint)Keys.U },
            {"V", (uint)Keys.V },
            {"W", (uint)Keys.W },
            {"X", (uint)Keys.X },
            {"Y", (uint)Keys.Y },
            {"Z", (uint)Keys.Z },
            { "@", (uint)Keys.D2 },
            { "^", (uint)Keys.D6 },
            { "&", (uint)Keys.D7 },
            { "*", (uint)Keys.D8 },
            { "(", (uint)Keys.D9 },
            { ")", (uint)Keys.D0 },
            { ";", (uint)Keys.Oem1 },           // ba
            { ":", (uint)Keys.Oem1},            // ba
            { "COLON", (uint)Keys.Oem1},        // ba
            { "=", (uint)Keys.Oemplus },        // bb
            { "+", (uint)Keys.Oemplus },        // bb
            { "PLUS", (uint)Keys.Oemplus },     // bb
            { "COMMA", (uint)Keys.Oemcomma },   // bc
            { ",", (uint)Keys.Oemcomma },       // bc
            { "<", (uint)Keys.Oemcomma },       // bc
            { "MINUS", (uint)Keys.OemMinus },   // bd
            { "-", (uint)Keys.OemMinus },       // bd
            { "_", (uint)Keys.OemMinus },       // de
            { "PERIOD", (uint)Keys.OemPeriod }, // be
            { ".", (uint)Keys.OemPeriod },      // be
            { ">", (uint)Keys.OemPeriod },      // be
            { "SLASH", (uint)Keys.Oem2 },       // bf
            { "/", (uint)Keys.Oem2 },           // bf
            { "?", (uint)Keys.Oem2 },           // bf
            { "BQUOTE", (uint)Keys.Oem3 },      // c0
            { "`", (uint)Keys.Oem3 },           // c0
            { "~", (uint)Keys.Oem3 },           // c0
            { "OEM4", (uint)Keys.Oem4 },        // db
            { "[", (uint)Keys.Oem4 },           // db
            { "{", (uint)Keys.Oem4 },           // db
            { "OEM5", (uint)Keys.Oem5 },        // dc
            { "\\", (uint)Keys.Oem5 },          // dc
            { "|", (uint)Keys.Oem5 },           // dc
            { "OEM6", (uint)Keys.Oem6 },        // dd
            { "]", (uint)Keys.Oem6 },           // dd
            { "}", (uint)Keys.Oem6 },           // dd
            { "OEM7", (uint)Keys.Oem7 },        // de
            { "'", (uint)Keys.Oem7 },           // de
            { "\"", (uint)Keys.Oem7 },          // de
            { "OEM8", (uint)Keys.Oem8 },        // df
            { "OEM102", (uint)Keys.Oem102 },    // e2/106
            { "＼", (uint)Keys.Oem102 },    // e2/106
        }; // faceToVkeyUS

        private static Dictionary<char, uint> charToVkeyJP = new Dictionary<char, uint>() {
            {' ', (uint)Keys.Space },
            {'1', (uint)Keys.D1 },
            {'2', (uint)Keys.D2 },
            {'3', (uint)Keys.D3 },
            {'4', (uint)Keys.D4 },
            {'5', (uint)Keys.D5 },
            {'6', (uint)Keys.D6 },
            {'7', (uint)Keys.D7 },
            {'8', (uint)Keys.D8 },
            {'9', (uint)Keys.D9 },
            {'0', (uint)Keys.D0 },
            {'を', (uint)Keys.D0 + 0x100 },
            {'!', (uint)Keys.D1 + 0x100 },
            {'\"', (uint)Keys.D2 + 0x100 },
            {'#', (uint)Keys.D3 + 0x100 },
            {'$', (uint)Keys.D4 + 0x100 },
            {'%', (uint)Keys.D5 + 0x100 },
            {'&', (uint)Keys.D6 + 0x100 },
            {'\'', (uint)Keys.D7 + 0x100 },
            {'(', (uint)Keys.D8 + 0x100 },
            {')', (uint)Keys.D9 + 0x100 },
            {'A', (uint)Keys.A + 0x100 },
            {'a', (uint)Keys.A },
            {'B', (uint)Keys.B + 0x100 },
            {'b', (uint)Keys.B },
            {'C', (uint)Keys.C + 0x100 },
            {'c', (uint)Keys.C },
            {'D', (uint)Keys.D + 0x100 },
            {'d', (uint)Keys.D },
            {'E', (uint)Keys.E + 0x100 },
            {'e', (uint)Keys.E },
            {'ぃ', (uint)Keys.E + 0x100 },
            {'F', (uint)Keys.F + 0x100 },
            {'f', (uint)Keys.F },
            {'G', (uint)Keys.G + 0x100 },
            {'g', (uint)Keys.G },
            {'H', (uint)Keys.H + 0x100 },
            {'h', (uint)Keys.H },
            {'I', (uint)Keys.I + 0x100 },
            {'i', (uint)Keys.I },
            {'J', (uint)Keys.J + 0x100 },
            {'j', (uint)Keys.J },
            {'K', (uint)Keys.K + 0x100 },
            {'k', (uint)Keys.K },
            {'L', (uint)Keys.L + 0x100 },
            {'l', (uint)Keys.L },
            {'M', (uint)Keys.M + 0x100 },
            {'m', (uint)Keys.M },
            {'N', (uint)Keys.N + 0x100 },
            {'n', (uint)Keys.N },
            {'O', (uint)Keys.O + 0x100 },
            {'o', (uint)Keys.O },
            {'P', (uint)Keys.P + 0x100 },
            {'p', (uint)Keys.P },
            {'Q', (uint)Keys.Q + 0x100 },
            {'q', (uint)Keys.Q },
            {'R', (uint)Keys.R + 0x100 },
            {'r', (uint)Keys.R },
            {'S', (uint)Keys.S + 0x100 },
            {'s', (uint)Keys.S },
            {'T', (uint)Keys.T + 0x100 },
            {'t', (uint)Keys.T },
            {'U', (uint)Keys.U + 0x100 },
            {'u', (uint)Keys.U },
            {'V', (uint)Keys.V + 0x100 },
            {'v', (uint)Keys.V },
            {'W', (uint)Keys.W + 0x100 },
            {'w', (uint)Keys.W },
            {'X', (uint)Keys.X + 0x100 },
            {'x', (uint)Keys.X },
            {'Y', (uint)Keys.Y + 0x100 },
            {'y', (uint)Keys.Y },
            {'Z', (uint)Keys.Z + 0x100 },
            {'z', (uint)Keys.Z },
            {'っ', (uint)Keys.Z + 0x100 },
            { ';', (uint)Keys.Oemplus },            // bb
            { '+', (uint)Keys.Oemplus + 0x100 },    // bb
            { ',', (uint)Keys.Oemcomma },           // bc
            { '<', (uint)Keys.Oemcomma + 0x100 },   // bc
            { '.', (uint)Keys.OemPeriod },          // be
            { '>', (uint)Keys.OemPeriod + 0x100 },  // be
            { '-', (uint)Keys.OemMinus },           // bd
            { '=', (uint)Keys.OemMinus + 0x100 },   // bd
            { ':', (uint)Keys.Oem1 },               // ba
            { '*', (uint)Keys.Oem1 + 0x100 },       // ba
            { '/', (uint)Keys.Oem2 },               // bf
            { '?', (uint)Keys.Oem2 + 0x100 },       // bf
            { '@', (uint)Keys.Oem3 },               // c0/106
            { '`', (uint)Keys.Oem3 + 0x100 },       // c0/106
            { '[', (uint)Keys.Oem4 },               // db
            { '{', (uint)Keys.Oem4 + 0x100 },       // db
            { '\\', (uint)Keys.Oem5 },              // dc
            { '|', (uint)Keys.Oem5 + 0x100 },       // dc
            { ']', (uint)Keys.Oem6 },               // dd
            { '}', (uint)Keys.Oem6 + 0x100 },       // dd
            { '^', (uint)Keys.Oem7 },               // de
            { '~', (uint)Keys.Oem7 + 0x100 },       // de
            { '＼', (uint)Keys.Oem102 },            // e2
            { '_', (uint)Keys.Oem102 + 0x100 },     // e2
        }; // charToVkeyJP

        private static Dictionary<char, uint> charToVkeyUS = new Dictionary<char, uint>() {
            {' ', (uint)Keys.Space },
            {'1', (uint)Keys.D1 },
            {'2', (uint)Keys.D2 },
            {'3', (uint)Keys.D3 },
            {'4', (uint)Keys.D4 },
            {'5', (uint)Keys.D5 },
            {'6', (uint)Keys.D6 },
            {'7', (uint)Keys.D7 },
            {'8', (uint)Keys.D8 },
            {'9', (uint)Keys.D9 },
            {'0', (uint)Keys.D0 },
            {')', (uint)Keys.D0 + 0x100 },
            {'を', (uint)Keys.D0 + 0x100 },
            {'!', (uint)Keys.D1 + 0x100 },
            {'@', (uint)Keys.D2 + 0x100 },
            {'#', (uint)Keys.D3 + 0x100 },
            {'$', (uint)Keys.D4 + 0x100 },
            {'%', (uint)Keys.D5 + 0x100 },
            {'^', (uint)Keys.D6 + 0x100 },
            {'&', (uint)Keys.D7 + 0x100 },
            {'*', (uint)Keys.D8 + 0x100 },
            {'(', (uint)Keys.D9 + 0x100 },
            {'A', (uint)Keys.A + 0x100 },
            {'a', (uint)Keys.A },
            {'B', (uint)Keys.B + 0x100 },
            {'b', (uint)Keys.B },
            {'C', (uint)Keys.C + 0x100 },
            {'c', (uint)Keys.C },
            {'D', (uint)Keys.D + 0x100 },
            {'d', (uint)Keys.D },
            {'E', (uint)Keys.E + 0x100 },
            {'e', (uint)Keys.E },
            {'ぃ', (uint)Keys.E + 0x100 },
            {'F', (uint)Keys.F + 0x100 },
            {'f', (uint)Keys.F },
            {'G', (uint)Keys.G + 0x100 },
            {'g', (uint)Keys.G },
            {'H', (uint)Keys.H + 0x100 },
            {'h', (uint)Keys.H },
            {'I', (uint)Keys.I + 0x100 },
            {'i', (uint)Keys.I },
            {'J', (uint)Keys.J + 0x100 },
            {'j', (uint)Keys.J },
            {'K', (uint)Keys.K + 0x100 },
            {'k', (uint)Keys.K },
            {'L', (uint)Keys.L + 0x100 },
            {'l', (uint)Keys.L },
            {'M', (uint)Keys.M + 0x100 },
            {'m', (uint)Keys.M },
            {'N', (uint)Keys.N + 0x100 },
            {'n', (uint)Keys.N },
            {'O', (uint)Keys.O + 0x100 },
            {'o', (uint)Keys.O },
            {'P', (uint)Keys.P + 0x100 },
            {'p', (uint)Keys.P },
            {'Q', (uint)Keys.Q + 0x100 },
            {'q', (uint)Keys.Q },
            {'R', (uint)Keys.R + 0x100 },
            {'r', (uint)Keys.R },
            {'S', (uint)Keys.S + 0x100 },
            {'s', (uint)Keys.S },
            {'T', (uint)Keys.T + 0x100 },
            {'t', (uint)Keys.T },
            {'U', (uint)Keys.U + 0x100 },
            {'u', (uint)Keys.U },
            {'V', (uint)Keys.V + 0x100 },
            {'v', (uint)Keys.V },
            {'W', (uint)Keys.W + 0x100 },
            {'w', (uint)Keys.W },
            {'X', (uint)Keys.X + 0x100 },
            {'x', (uint)Keys.X },
            {'Y', (uint)Keys.Y + 0x100 },
            {'y', (uint)Keys.Y },
            {'Z', (uint)Keys.Z + 0x100 },
            {'z', (uint)Keys.Z },
            {'っ', (uint)Keys.Z + 0x100 },
            { ';', (uint)Keys.Oem1 },               // ba
            { ':', (uint)Keys.Oem1 + 0x100 },       // ba
            { '=', (uint)Keys.Oemplus },            // bb
            { '+', (uint)Keys.Oemplus + 0x100 },    // bb
            { ',', (uint)Keys.Oemcomma },           // bc
            { '<', (uint)Keys.Oemcomma + 0x100 },   // bc
            { '-', (uint)Keys.OemMinus },           // bd
            { '_', (uint)Keys.OemMinus + 0x100 },   // bd
            { '.', (uint)Keys.OemPeriod },          // be
            { '>', (uint)Keys.OemPeriod + 0x100 },  // be
            { '/', (uint)Keys.Oem2 },               // bf
            { '?', (uint)Keys.Oem2 + 0x100 },       // bf
            { '`', (uint)Keys.Oem3 },               // c0
            { '~', (uint)Keys.Oem3 + 0x100 },       // c0
            { '[', (uint)Keys.Oem4 },               // db
            { '{', (uint)Keys.Oem4 + 0x100 },       // db
            { '\\', (uint)Keys.Oem5 },              // dc
            { '＼', (uint)Keys.Oem5 },              // dc
            { '|', (uint)Keys.Oem5 + 0x100 },       // dc
            { ']', (uint)Keys.Oem6 },               // dd
            { '}', (uint)Keys.Oem6 + 0x100 },       // dd
            { '\'', (uint)Keys.Oem7 },              // de
            { '"', (uint)Keys.Oem7 + 0x100 },       // de
        }; // charToVkeyUS

        private static Dictionary<uint, char> vkeyToCharJP = new Dictionary<uint, char>() {
            { (uint)Keys.Space,' ' },
            { (uint)Keys.D1,'1' },
            { (uint)Keys.D2,'2' },
            { (uint)Keys.D3,'3' },
            { (uint)Keys.D4,'4' },
            { (uint)Keys.D5,'5' },
            { (uint)Keys.D6,'6' },
            { (uint)Keys.D7,'7' },
            { (uint)Keys.D8,'8' },
            { (uint)Keys.D9,'9' },
            { (uint)Keys.D0,'0' },
            { (uint)Keys.A,'a' },
            { (uint)Keys.B,'b' },
            { (uint)Keys.C,'c' },
            { (uint)Keys.D,'d' },
            { (uint)Keys.E,'e' },
            { (uint)Keys.F,'f' },
            { (uint)Keys.G,'g' },
            { (uint)Keys.H,'h' },
            { (uint)Keys.I,'i' },
            { (uint)Keys.J,'j' },
            { (uint)Keys.K,'k' },
            { (uint)Keys.L,'l' },
            { (uint)Keys.M,'m' },
            { (uint)Keys.N,'n' },
            { (uint)Keys.O,'o' },
            { (uint)Keys.P,'p' },
            { (uint)Keys.Q,'q' },
            { (uint)Keys.R,'r' },
            { (uint)Keys.S,'s' },
            { (uint)Keys.T,'t' },
            { (uint)Keys.U,'u' },
            { (uint)Keys.V,'v' },
            { (uint)Keys.W,'w' },
            { (uint)Keys.X,'x' },
            { (uint)Keys.Y,'y' },
            { (uint)Keys.Z,'z' },
            { (uint)Keys.Oemplus, ';' },     // bb
            { (uint)Keys.Oemcomma, ',' },   // bc
            { (uint)Keys.OemPeriod, '.' }, // be
            { (uint)Keys.OemMinus, '-' },   // bd
            { (uint)Keys.Oem1, ':' },       // ba
            { (uint)Keys.Oem2, '/' },       // bf
            { (uint)Keys.Oem3, '@' },      // c0/106
            { (uint)Keys.Oem4, '[' },        // db
            { (uint)Keys.Oem5, '\\' },        // dc
            { (uint)Keys.Oem6, ']' },        // dd
            { (uint)Keys.Oem7, '^' },        // de
            { (uint)Keys.Oem102, '＼' },        // e1
            { (uint)Keys.IMEConvert, '変' },        // 1c
            { (uint)Keys.IMENonconvert, '無' },        // 1d
        }; // vkeyToCharJP

        private static Dictionary<uint, char> vkeyToCharUS = new Dictionary<uint, char>() {
            { (uint)Keys.Space,' ' },
            { (uint)Keys.D1,'1' },
            { (uint)Keys.D2,'2' },
            { (uint)Keys.D3,'3' },
            { (uint)Keys.D4,'4' },
            { (uint)Keys.D5,'5' },
            { (uint)Keys.D6,'6' },
            { (uint)Keys.D7,'7' },
            { (uint)Keys.D8,'8' },
            { (uint)Keys.D9,'9' },
            { (uint)Keys.D0,'0' },
            { (uint)Keys.A,'a' },
            { (uint)Keys.B,'b' },
            { (uint)Keys.C,'c' },
            { (uint)Keys.D,'d' },
            { (uint)Keys.E,'e' },
            { (uint)Keys.F,'f' },
            { (uint)Keys.G,'g' },
            { (uint)Keys.H,'h' },
            { (uint)Keys.I,'i' },
            { (uint)Keys.J,'j' },
            { (uint)Keys.K,'k' },
            { (uint)Keys.L,'l' },
            { (uint)Keys.M,'m' },
            { (uint)Keys.N,'n' },
            { (uint)Keys.O,'o' },
            { (uint)Keys.P,'p' },
            { (uint)Keys.Q,'q' },
            { (uint)Keys.R,'r' },
            { (uint)Keys.S,'s' },
            { (uint)Keys.T,'t' },
            { (uint)Keys.U,'u' },
            { (uint)Keys.V,'v' },
            { (uint)Keys.W,'w' },
            { (uint)Keys.X,'x' },
            { (uint)Keys.Y,'y' },
            { (uint)Keys.Z,'z' },
            { (uint)Keys.Oemplus, '=' },    // bb
            { (uint)Keys.Oemcomma, ',' },   // bc
            { (uint)Keys.OemPeriod, '.' },  // be
            { (uint)Keys.OemMinus, '-' },   // bd
            { (uint)Keys.Oem1, ';' },       // ba
            { (uint)Keys.Oem2, '/' },       // bf
            { (uint)Keys.Oem3, '`' },       // c0/106
            { (uint)Keys.Oem4, '[' },       // db
            { (uint)Keys.Oem5, '\\' },      // dc
            { (uint)Keys.Oem6, ']' },       // dd
            { (uint)Keys.Oem7, '\'' },      // de
            { (uint)Keys.Oem102, '＼' },    // e1
            { (uint)Keys.IMEConvert, '変' },        // 1c
            { (uint)Keys.IMENonconvert, '無' },        // 1d
        }; // vkeyToCharUS

        /// <summary>キー文字から、その仮想キーコードを得る</summary>
        public static uint getFaceToVKey(string face)
        {
            return (DecoderKeyVsVKey.IsJPmode ? faceToVkeyJP : faceToVkeyUS)._safeGet(face);
        }

        /// <summary>文字コードから、その仮想キーコードを得る</summary>
        public static uint getCharToVKey(char ch)
        {
            return (DecoderKeyVsVKey.IsJPmode ?  charToVkeyJP : charToVkeyUS)._safeGet(ch);
        }

        /// <summary>仮想キーコードから、その文字コードを得る</summary>
        public static char getVKeyToChar(uint vk)
        {
            return (DecoderKeyVsVKey.IsJPmode ? vkeyToCharJP : vkeyToCharUS)._safeGet(vk);
        }

        /// <summary>文字名から、その文字コードを得る</summary>
        public static char getFaceStrToChar(string face)
        {
            return getVKeyToChar(getFaceToVKey(face));
        }

    } // class _FaceCharVKey

    /// <summary>
    /// JP/US モードに対応した、仮想キーコードと文字との相互変換<br/>
    /// </summary>
    static class CharVsVKey
    {
        /// <summary>JP/US モードに対応して、キー文字からその仮想キーコードを得る</summary>
        public static uint GetVKeyFromFaceStr(string face)
        {
            return _FaceCharVKey.getFaceToVKey(face);
        }

        /// <summary>JP/US モードに対応して、文字コードからその仮想キーコードを得る<br/>対応するものがなければ 0 を返す</summary>
        public static uint GetVKeyFromFaceChar(char face)
        {
            return _FaceCharVKey.getCharToVKey(face);
        }

        /// <summary>JP/US モードに対応して、仮想キーコードからその文字コードを得る<br/>対応するものがなければ '\0' を返す</summary>
        public static char GetFaceCharFromVKey(uint vkey)
        {
            return _FaceCharVKey.getVKeyToChar(vkey);
        }

        /// <summary>JP/US モードに対応して、キー名からその文字コードを得る<br/>対応するものがなければ '\0' を返す</summary>
        public static char GetCharFromFaceStr(string face)
        {
            return _FaceCharVKey.getFaceStrToChar(face);
        }

    }

}

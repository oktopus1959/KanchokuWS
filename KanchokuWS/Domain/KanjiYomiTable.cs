using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utils;

namespace KanchokuWS
{
    public static class KanjiYomiTable
    {
        private static Logger logger = Logger.GetLogger();

        private static Dictionary<string, HashSet<char>> yomiKanjiTbl = new Dictionary<string, HashSet<char>>();

        private static Dictionary<string, string> romanKanaTbl = new Dictionary<string, string>();

        private static Dictionary<char, string> kanaRomanTbl = new Dictionary<char, string>() {
            { 'ぁ', "la" },
            { 'ぃ', "li" },
            { 'ぅ', "lu" },
            { 'ぇ', "le" },
            { 'ぉ', "lo" },
            { 'あ', "a" },
            { 'い', "i" },
            { 'う', "u" },
            { 'え', "e" },
            { 'お', "o" },
            { 'ゕ', "lka" },
            { 'か', "ka" },
            { 'が', "ga" },
            { 'き', "ki" },
            { 'ぎ', "gi" },
            { 'く', "ku" },
            { 'ぐ', "gu" },
            { 'ゖ', "lke" },
            { 'け', "ke" },
            { 'げ', "ge" },
            { 'こ', "ko" },
            { 'ご', "go" },
            { 'さ', "sa" },
            { 'ざ', "za" },
            { 'し', "si" },
            { 'じ', "zi" },
            { 'す', "su" },
            { 'ず', "zu" },
            { 'せ', "se" },
            { 'ぜ', "ze" },
            { 'そ', "so" },
            { 'ぞ', "zo" },
            { 'た', "ta" },
            { 'だ', "da" },
            { 'ち', "ti" },
            { 'ぢ', "di" },
            { 'っ', "ltu" },
            { 'つ', "tu" },
            { 'づ', "du" },
            { 'て', "te" },
            { 'で', "de" },
            { 'と', "to" },
            { 'ど', "do" },
            { 'な', "na" },
            { 'に', "ni" },
            { 'ぬ', "nu" },
            { 'ね', "ne" },
            { 'の', "no" },
            { 'は', "ha" },
            { 'ば', "ba" },
            { 'ぱ', "pa" },
            { 'ひ', "hi" },
            { 'び', "bi" },
            { 'ぴ', "pi" },
            { 'ふ', "hu" },
            { 'ぶ', "bu" },
            { 'ぷ', "pu" },
            { 'へ', "he" },
            { 'べ', "be" },
            { 'ぺ', "pe" },
            { 'ほ', "ho" },
            { 'ぼ', "bo" },
            { 'ぽ', "po" },
            { 'ま', "ma" },
            { 'み', "mi" },
            { 'む', "mu" },
            { 'め', "me" },
            { 'も', "mo" },
            { 'ゃ', "lya" },
            { 'や', "ya" },
            { 'ゅ', "lyu" },
            { 'ゆ', "yu" },
            { 'ょ', "lyo" },
            { 'よ', "yo" },
            { 'ら', "ra" },
            { 'り', "ri" },
            { 'る', "ru" },
            { 'れ', "re" },
            { 'ろ', "ro" },
            { 'ゎ', "lwa" },
            { 'わ', "wa" },
            { 'ゐ', "wyi" },
            { 'ゔ', "vu" },
            { 'ヴ', "vu" },
            { 'ゑ', "wye" },
            { 'を', "wo" },
            { 'ん', "nn" },
            { '、', "," },
            { '。', "." },
            { 'ー', "-" },
            { '＾', "^" },
            { '￥', "\\" },
            { '＠', "@" },
            { '「', "[" },
            { '：', ":" },
            { '」', "]" },
            { '０', "0" },
            { '１', "1" },
            { '２', "2" },
            { '３', "3" },
            { '４', "4" },
            { '５', "5" },
            { '６', "6" },
            { '７', "7" },
            { '８', "8" },
            { '９', "9" },
            { '！', "!" },
            { '”', "\"" },
            { '＃', "#" },
            { '＄', "$" },
            { '％', "%" },
            { '＆', "&" },
            { '’', "'" },
            { '（', "(" },
            { '）', ")" },
            { '＝', "=" },
            { '～', "~" },
            { '‘', "`" },
            { '［', "[" },
            { '］', "]" },
            { '＋', "+" },
            { '＊', "*" },
            { '；', ";" },
            { '／', "/" },
            { '・', "/" },
            { '？', "?" },
            { '．', "." },
            { '，', "," },
            { '＜', "<" },
            { '＞', ">" },
            { '｜', "|" },
            { '｛', "{" },
            { '｝', "}" },
            { '＿', "_" },
        };

        private static Dictionary<char, string> hiraganaKeyfaceDic = new Dictionary<char, string>() {
            {'ぁ', "#"},
            {'ぃ', "E"},
            {'ぅ', "$"},
            {'ぇ', "%"},
            {'ぉ', "&"},
            {'あ', "3"},
            {'い', "e"},
            {'う', "4"},
            {'え', "5"},
            {'お', "6"},
            {'か', "t"},
            {'が', "t@"},
            {'き', "g"},
            {'ぎ', "g@"},
            {'く', "h"},
            {'ぐ', "h@"},
            {'け', ":"},
            {'げ', ":@"},
            {'こ', "b"},
            {'ご', "b@"},
            {'さ', "x"},
            {'ざ', "x@"},
            {'し', "d"},
            {'じ', "d@"},
            {'す', "r"},
            {'ず', "r@"},
            {'せ', "p"},
            {'ぜ', "p@"},
            {'そ', "c"},
            {'ぞ', "c@"},
            {'た', "q"},
            {'だ', "q@"},
            {'ち', "a"},
            {'ぢ', "a@"},
            {'っ', "Z"},
            {'つ', "z"},
            {'づ', "z@"},
            {'て', "w"},
            {'で', "w@"},
            {'と', "s"},
            {'ど', "s@"},
            {'な', "u"},
            {'に', "i"},
            {'ぬ', "1"},
            {'ね', ","},
            {'の', "k"},
            {'は', "f"},
            {'ば', "f@"},
            {'ぱ', "f["},
            {'ひ', "v"},
            {'び', "v@"},
            {'ぴ', "v["},
            {'ふ', "2"},
            {'ぶ', "2@"},
            {'ぷ', "2["},
            {'へ', "^"},
            {'べ', "^@"},
            {'ぺ', "^["},
            {'ほ', "-"},
            {'ぼ', "-@"},
            {'ぽ', "-["},
            {'ま', "j"},
            {'み', "n"},
            {'む', "]"},
            {'め', "/"},
            {'も', "m"},
            {'ゃ', "'"},
            {'や', "7"},
            {'ゅ', "("},
            {'ゆ', "8"},
            {'ょ', ")"},
            {'よ', "9"},
            {'ら', "o"},
            {'り', "l"},
            {'る', "."},
            {'れ', ";"},
            {'ろ', "＼"},
            {'わ', "0"},
            {'ゔ', "4@"},
            {'ヴ', "4@"},
            {'を', "を"},
            {'ん', "y"},
            {'、', "<"},
            {'。', ">"},
            {'・', "?"},
            {'「', "{"},
            {'」', "}"},
            {'゛', "@"},
            {'゜', "["},
            {'ー', "\\"},
        };
        static KanjiYomiTable()
        {
            yomiKanjiTbl["あ"] = new HashSet<char>() { 'あ', 'ぁ', 'ア', 'ァ' };
            yomiKanjiTbl["い"] = new HashSet<char>() { 'い', 'ぃ', 'イ', 'ィ' };
            yomiKanjiTbl["う"] = new HashSet<char>() { 'う', 'ぅ', 'ウ', 'ゥ' };
            yomiKanjiTbl["え"] = new HashSet<char>() { 'え', 'ぇ', 'エ', 'ェ' };
            yomiKanjiTbl["お"] = new HashSet<char>() { 'お', 'ぉ', 'オ', 'ォ' };
            yomiKanjiTbl["か"] = new HashSet<char>() { 'か', 'カ', 'ゕ', 'ヵ' };
            yomiKanjiTbl["が"] = new HashSet<char>() { 'が', 'ガ' };
            yomiKanjiTbl["き"] = new HashSet<char>() { 'き', 'キ' };
            yomiKanjiTbl["ぎ"] = new HashSet<char>() { 'ぎ', 'ギ' };
            yomiKanjiTbl["く"] = new HashSet<char>() { 'く', 'ク' };
            yomiKanjiTbl["ぐ"] = new HashSet<char>() { 'ぐ', 'グ' };
            yomiKanjiTbl["け"] = new HashSet<char>() { 'け', 'ケ', 'ゖ', 'ヶ' };
            yomiKanjiTbl["げ"] = new HashSet<char>() { 'げ', 'ゲ' };
            yomiKanjiTbl["こ"] = new HashSet<char>() { 'こ', 'ク' };
            yomiKanjiTbl["ご"] = new HashSet<char>() { 'ご', 'ゴ' };
            yomiKanjiTbl["さ"] = new HashSet<char>() { 'さ', 'サ' };
            yomiKanjiTbl["ざ"] = new HashSet<char>() { 'ざ', 'ザ' };
            yomiKanjiTbl["し"] = new HashSet<char>() { 'し', 'シ' };
            yomiKanjiTbl["じ"] = new HashSet<char>() { 'じ', 'ジ' };
            yomiKanjiTbl["す"] = new HashSet<char>() { 'す', 'ス' };
            yomiKanjiTbl["ず"] = new HashSet<char>() { 'ず', 'ズ' };
            yomiKanjiTbl["せ"] = new HashSet<char>() { 'せ', 'セ' };
            yomiKanjiTbl["ぜ"] = new HashSet<char>() { 'ぜ', 'ゼ' };
            yomiKanjiTbl["そ"] = new HashSet<char>() { 'そ', 'ソ' };
            yomiKanjiTbl["ぞ"] = new HashSet<char>() { 'ぞ', 'ゾ' };
            yomiKanjiTbl["た"] = new HashSet<char>() { 'た', 'タ' };
            yomiKanjiTbl["だ"] = new HashSet<char>() { 'だ', 'ダ' };
            yomiKanjiTbl["ち"] = new HashSet<char>() { 'ち', 'チ' };
            yomiKanjiTbl["ぢ"] = new HashSet<char>() { 'ぢ', 'ヂ' };
            yomiKanjiTbl["つ"] = new HashSet<char>() { 'つ', 'っ', 'ツ' };
            yomiKanjiTbl["づ"] = new HashSet<char>() { 'づ', 'ヅ' };
            yomiKanjiTbl["て"] = new HashSet<char>() { 'て', 'テ' };
            yomiKanjiTbl["で"] = new HashSet<char>() { 'で', 'デ' };
            yomiKanjiTbl["と"] = new HashSet<char>() { 'と', 'ト' };
            yomiKanjiTbl["ど"] = new HashSet<char>() { 'ど', 'デ' };
            yomiKanjiTbl["な"] = new HashSet<char>() { 'な', 'ナ' };
            yomiKanjiTbl["に"] = new HashSet<char>() { 'に', 'ニ' };
            yomiKanjiTbl["ぬ"] = new HashSet<char>() { 'ぬ', 'ヌ' };
            yomiKanjiTbl["ね"] = new HashSet<char>() { 'ね', 'ネ' };
            yomiKanjiTbl["の"] = new HashSet<char>() { 'の', 'ノ' };
            yomiKanjiTbl["は"] = new HashSet<char>() { 'は', 'ハ' };
            yomiKanjiTbl["ば"] = new HashSet<char>() { 'ば', 'バ' };
            yomiKanjiTbl["ぱ"] = new HashSet<char>() { 'ぱ', 'パ' };
            yomiKanjiTbl["ひ"] = new HashSet<char>() { 'ひ', 'ヒ' };
            yomiKanjiTbl["び"] = new HashSet<char>() { 'び', 'ビ' };
            yomiKanjiTbl["ぴ"] = new HashSet<char>() { 'ぴ', 'ピ' };
            yomiKanjiTbl["ふ"] = new HashSet<char>() { 'ふ', 'フ' };
            yomiKanjiTbl["ぶ"] = new HashSet<char>() { 'ぶ', 'ブ' };
            yomiKanjiTbl["ぷ"] = new HashSet<char>() { 'ぷ', 'プ' };
            yomiKanjiTbl["へ"] = new HashSet<char>() { 'へ', 'ヘ' };
            yomiKanjiTbl["べ"] = new HashSet<char>() { 'べ', 'ベ' };
            yomiKanjiTbl["ぺ"] = new HashSet<char>() { 'ぺ', 'ペ' };
            yomiKanjiTbl["ほ"] = new HashSet<char>() { 'ほ', 'ホ' };
            yomiKanjiTbl["ぼ"] = new HashSet<char>() { 'ぼ', 'ボ' };
            yomiKanjiTbl["ぽ"] = new HashSet<char>() { 'ぽ', 'ポ' };
            yomiKanjiTbl["ま"] = new HashSet<char>() { 'ま', 'マ' };
            yomiKanjiTbl["み"] = new HashSet<char>() { 'み', 'ミ' };
            yomiKanjiTbl["む"] = new HashSet<char>() { 'む', 'ム' };
            yomiKanjiTbl["め"] = new HashSet<char>() { 'め', 'メ' };
            yomiKanjiTbl["も"] = new HashSet<char>() { 'も', 'モ' };
            yomiKanjiTbl["や"] = new HashSet<char>() { 'や', 'ヤ', 'ゃ', 'ャ' };
            yomiKanjiTbl["ゆ"] = new HashSet<char>() { 'ゆ', 'ユ', 'ゅ', 'ュ' };
            yomiKanjiTbl["よ"] = new HashSet<char>() { 'よ', 'ヨ', 'ょ', 'ョ' };
            yomiKanjiTbl["ら"] = new HashSet<char>() { 'ら', 'ラ' };
            yomiKanjiTbl["り"] = new HashSet<char>() { 'り', 'リ' };
            yomiKanjiTbl["る"] = new HashSet<char>() { 'る', 'ル' };
            yomiKanjiTbl["れ"] = new HashSet<char>() { 'れ', 'レ' };
            yomiKanjiTbl["ろ"] = new HashSet<char>() { 'ろ', 'ロ' };
            yomiKanjiTbl["わ"] = new HashSet<char>() { 'わ', 'ワ', 'ゎ', 'ヮ' };
            yomiKanjiTbl["ゐ"] = new HashSet<char>() { 'ゐ', 'ヰ' };
            yomiKanjiTbl["ゔ"] = new HashSet<char>() { 'ゔ', 'ヴ' };
            yomiKanjiTbl["ゑ"] = new HashSet<char>() { 'ゑ', 'ヱ' };
            yomiKanjiTbl["を"] = new HashSet<char>() { 'を', 'ヲ' };
            yomiKanjiTbl["ん"] = new HashSet<char>() { 'ん', 'ン' };

            romanKanaTbl["A"] = "あ";
            romanKanaTbl["I"] = "い";
            romanKanaTbl["U"] = "う";
            romanKanaTbl["E"] = "え";
            romanKanaTbl["O"] = "お";
            romanKanaTbl["KA"] = "か";
            romanKanaTbl["KI"] = "き";
            romanKanaTbl["KU"] = "く";
            romanKanaTbl["KE"] = "け";
            romanKanaTbl["KO"] = "こ";
            romanKanaTbl["GA"] = "が";
            romanKanaTbl["GI"] = "ぎ";
            romanKanaTbl["GU"] = "ぐ";
            romanKanaTbl["GE"] = "げ";
            romanKanaTbl["GO"] = "ご";
            romanKanaTbl["SA"] = "さ";
            romanKanaTbl["SI"] = "し";
            romanKanaTbl["SHI"] = "し";
            romanKanaTbl["SU"] = "す";
            romanKanaTbl["SE"] = "せ";
            romanKanaTbl["SO"] = "そ";
            romanKanaTbl["ZA"] = "ざ";
            romanKanaTbl["ZI"] = "じ";
            romanKanaTbl["JI"] = "じ";
            romanKanaTbl["ZU"] = "ず";
            romanKanaTbl["ZE"] = "ぜ";
            romanKanaTbl["ZO"] = "ぞ";
            romanKanaTbl["TA"] = "た";
            romanKanaTbl["TI"] = "ち";
            romanKanaTbl["CHI"] = "ち";
            romanKanaTbl["TU"] = "つ";
            romanKanaTbl["TE"] = "て";
            romanKanaTbl["TO"] = "と";
            romanKanaTbl["DA"] = "だ";
            romanKanaTbl["DI"] = "ぢ";
            romanKanaTbl["DU"] = "づ";
            romanKanaTbl["DE"] = "で";
            romanKanaTbl["DO"] = "ど";
            romanKanaTbl["NA"] = "な";
            romanKanaTbl["NI"] = "に";
            romanKanaTbl["NU"] = "ぬ";
            romanKanaTbl["NE"] = "ね";
            romanKanaTbl["NO"] = "の";
            romanKanaTbl["HA"] = "は";
            romanKanaTbl["HI"] = "ひ";
            romanKanaTbl["HU"] = "ふ";
            romanKanaTbl["FU"] = "ふ";
            romanKanaTbl["HE"] = "へ";
            romanKanaTbl["HO"] = "ほ";
            romanKanaTbl["BA"] = "ば";
            romanKanaTbl["BI"] = "び";
            romanKanaTbl["BU"] = "ぶ";
            romanKanaTbl["BE"] = "べ";
            romanKanaTbl["BO"] = "ぼ";
            romanKanaTbl["PA"] = "ぱ";
            romanKanaTbl["PI"] = "ぴ";
            romanKanaTbl["PU"] = "ぷ";
            romanKanaTbl["PE"] = "ぺ";
            romanKanaTbl["PO"] = "ぽ";
            romanKanaTbl["MA"] = "ま";
            romanKanaTbl["MI"] = "み";
            romanKanaTbl["MU"] = "む";
            romanKanaTbl["ME"] = "め";
            romanKanaTbl["MO"] = "も";
            romanKanaTbl["YA"] = "や";
            romanKanaTbl["YI"] = "い";
            romanKanaTbl["YU"] = "ゆ";
            romanKanaTbl["YE"] = "え";
            romanKanaTbl["YO"] = "よ";
            romanKanaTbl["RA"] = "ら";
            romanKanaTbl["RI"] = "り";
            romanKanaTbl["RU"] = "る";
            romanKanaTbl["RE"] = "れ";
            romanKanaTbl["RO"] = "ろ";
            romanKanaTbl["WA"] = "わ";
            romanKanaTbl["WI"] = "ゐ";
            romanKanaTbl["WU"] = "う";
            romanKanaTbl["WE"] = "ゑ";
            romanKanaTbl["WO"] = "を";
            romanKanaTbl["KYA"] = "きゃ";
            romanKanaTbl["KYU"] = "きゅ";
            romanKanaTbl["KYO"] = "きょ";
            romanKanaTbl["GYA"] = "ぎゃ";
            romanKanaTbl["GYU"] = "ぎゅ";
            romanKanaTbl["GYO"] = "ぎょ";
            romanKanaTbl["SYA"] = "しゃ";
            romanKanaTbl["SYU"] = "しゅ";
            romanKanaTbl["SYO"] = "しょ";
            romanKanaTbl["SHA"] = "しゃ";
            romanKanaTbl["SHU"] = "しゅ";
            romanKanaTbl["SHO"] = "しょ";
            romanKanaTbl["ZYA"] = "じゃ";
            romanKanaTbl["ZYU"] = "じゅ";
            romanKanaTbl["ZYO"] = "じょ";
            romanKanaTbl["JYA"] = "じゃ";
            romanKanaTbl["JYU"] = "じゅ";
            romanKanaTbl["JYO"] = "じょ";
            romanKanaTbl["JA"] = "じゃ";
            romanKanaTbl["JU"] = "じゅ";
            romanKanaTbl["JO"] = "じょ";
            romanKanaTbl["TYA"] = "ちゃ";
            romanKanaTbl["TYU"] = "ちゅ";
            romanKanaTbl["TYO"] = "ちょ";
            romanKanaTbl["CHA"] = "ちゃ";
            romanKanaTbl["CHU"] = "ちゅ";
            romanKanaTbl["CHO"] = "ちょ";
            romanKanaTbl["NYA"] = "にゃ";
            romanKanaTbl["NYU"] = "にゅ";
            romanKanaTbl["NYO"] = "にょ";
            romanKanaTbl["HYA"] = "ひゃ";
            romanKanaTbl["HYU"] = "ひゅ";
            romanKanaTbl["HYO"] = "ひょ";
            romanKanaTbl["BYA"] = "びゃ";
            romanKanaTbl["BYU"] = "びゅ";
            romanKanaTbl["BYO"] = "びょ";
            romanKanaTbl["PYA"] = "ぴゃ";
            romanKanaTbl["PYU"] = "ぴゅ";
            romanKanaTbl["PYO"] = "ぴょ";
            romanKanaTbl["MYA"] = "みゃ";
            romanKanaTbl["MYU"] = "みゅ";
            romanKanaTbl["MYO"] = "みょ";
            romanKanaTbl["RYA"] = "りゃ";
            romanKanaTbl["RYU"] = "りゅ";
            romanKanaTbl["RYO"] = "りょ";

        }

        public static void ReadKanjiYomiFile(string filename)
        {
            var filePath = KanchokuIni.Singleton.KanchokuDir._joinPath(filename);
            if (Settings.LoggingVirtualKeyboardInfo) logger.DebugH(() => $"ENTER: filePath={filePath}");
            if (Helper.FileExists(filePath)) {
                try {
                    foreach (var line in System.IO.File.ReadAllLines(filePath)) {
                        var items = line.Trim()._reReplace(@"[ \t]+", " ")._split(' ');
                        if (items._safeLength() >= 2 && items[0]._notEmpty() && !items[0].StartsWith("#") && items[1]._notEmpty()) {
                            var kanji = items[0][0];
                            foreach (var yomi in items[1]._split('|')) {
                                if (yomi._notEmpty()) {
                                    var hiraYomi = yomi._toHiragana();
                                    if (IsKatakana(yomi[0]) || yomi.Length < 2) {
                                        yomiKanjiTbl._safeGetOrNewInsert(hiraYomi).Add(kanji);
                                    } else {
                                        for (int len = 2; len <= yomi.Length; ++len) {
                                            yomiKanjiTbl._safeGetOrNewInsert(hiraYomi.Substring(0, len)).Add(kanji);
                                        }
                                    }
                                }
                            }
                        }
                    }
                } catch (Exception e) {
                    logger.Error($"Cannot read file: {filePath}: {e.Message}");
                }
            }
        }

        private static char[] EmptyResult = new char[0];

        public static char[] GetCandidates(string yomi)
        {
            var result = yomiKanjiTbl._safeGet(yomi);
            return result._notEmpty() ? result.ToArray() : EmptyResult;
        }

        public static char[] GetCandidatesFromRoman(string romaYomi)
        {
            return GetCandidates(romaYomi._toUpper()._hiraganaFromRoman());
        }

        public static bool IsKatakana(char ch)
        {
            return ch >= 'ァ' && ch <= 'ヶ';
        }

        public static string _toHiragana(this string str)
        {
            var sb = new StringBuilder();
            if (str._notEmpty()) {
                foreach (var ch in str) {
                    sb.Append(IsKatakana(ch) ? (char)(ch - 0x0060) : ch);
                }
            }
            return sb.ToString();
        }

        public static string _hiraganaFromRoman(this string str)
        {
            var sb = new StringBuilder();
            if (str._notEmpty()) {
                int pos = 0;
                while (pos < str.Length) {
                    bool found = false;
                    for (int n = 1; n <= 3 && n <= str.Length - pos; ++n) {
                        var kana = romanKanaTbl._safeGet(str.Substring(pos, n));
                        if (kana._notEmpty()) {
                            sb.Append(kana);
                            pos += n;
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        sb.Append(str[pos] == 'N' ? "ん" : "っ");
                        pos += 1;
                    }
                }
            }
            return sb.ToString();
        }

        public static string _hiraganaToRoman(this string str)
        {
            var sb = new StringBuilder();
            if (str._notEmpty()) {
                foreach (var c in str) {
                    var rs = _hiraganaToRoman(c);
                    if (rs._notEmpty())
                        sb.Append(rs);
                    else
                        sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string _hiraganaToRoman(this char ch)
        {
            if (ch >= 'Ａ' && ch <= 'Ｚ') {
                return ((char)(ch - 'Ａ' + 'A')).ToString();
            } else if (ch >= 'ａ' && ch <= 'ｚ') {
                return ((char)(ch - 'ａ' + 'a')).ToString();
            } else {
                return kanaRomanTbl._safeGet(ch);
            }
        }

        public static string _hiraganaToKeyface(this char ch)
        {
            return hiraganaKeyfaceDic._safeGet(ch);
        }
    }
}

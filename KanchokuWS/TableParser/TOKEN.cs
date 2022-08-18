using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanchokuWS.TableParser
{
    /// <summary>
    /// トークンの種類
    /// </summary>
    public enum TOKEN {
        IGNORE,
        END,
        LBRACE,         // {
        RBRACE,         // }
        COMMA,          // ,
        VBAR,           // |
        NEW_LINE,
        STRING,         // "str"
        BARE_STRING,    // str
        STRING_PAIR,    // str:str
        FUNCTION,       // @?
        SLASH,          // /
        ARROW,          // -n>
        ARROW_BUNDLE,   // -*>-n>
        REWRITE_PRE,    // %n>
        REWRITE_POST,   // &n>
        PLACE_HOLDER,   // $n
    };

}

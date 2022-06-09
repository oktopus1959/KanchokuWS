using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utils;

namespace UtilsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.LogFilename = "UtilsTest.log";
            Logger.LogLevel = Logger.LogLevelInfo;

            new UtilsTest().loggerTest();
        }
    }
}

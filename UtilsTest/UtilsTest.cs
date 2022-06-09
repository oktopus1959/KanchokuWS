using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utils;

namespace UtilsTest
{
    class UtilsTest
    {
        private static Logger logger = Logger.GetLogger(true);

        public void loggerTest()
        {
            logger.Debug(() => $"CALLED: {sub()}");
        }

        private string sub()
        {
            logger.Info("CALLED");
            return "foo";
        }
    }
}

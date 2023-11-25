using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utils;

namespace KanchokuWS
{
    static class KanchokuHelper
    {
        private static Logger logger = Logger.GetLogger();

        public static void WriteAllLinesToFile(string filename, List<string> lines)
        {
            if (filename._notEmpty()) {
                var path = KanchokuIni.Singleton.KanchokuDir._joinPath(filename);
                Helper.CreateDirectory(path._getDirPath());
                if (Settings.LoggingTableFileInfo) logger.Info(() => $"ENTER: path={path}");
                Helper.WriteLinesToFile(path, lines, (e) => logger.Error(e._getErrorMsg()));
                if (Settings.LoggingTableFileInfo) logger.Info("LEAVE");
            }
        }

    }
}

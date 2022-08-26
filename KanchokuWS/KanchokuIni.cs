using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace KanchokuWS
{
    public class KanchokuIniBase
    {
        private IniFileAccessor m_ini;

        protected KanchokuIniBase(string filename)
        {
            m_ini = new IniFileAccessor(SystemHelper.MakeAbsPathUnderKanchokuRootDir(filename));
        }

        public string IniFilePath { get { return m_ini?.IniFilePath; } }

        public bool IsIniFileExist => Helper.FileExists(IniFilePath);

        public string KanchokuDir => IniFilePath._getDirPath();

        public string GetString(string key, string defval = "")
        {
            return m_ini?.GetString("kanchoku", key, defval) ?? defval;
        }

        public string GetStringEx(string key, string defvalInit, string defval = "")
        {
            if (IsIniFileExist) {
                return m_ini?.GetString("kanchoku", key, defval) ?? defval;
            } else {
                return defvalInit;
            }
        }

        public int GetInt(string key, int defval = 0)
        {
            return m_ini?.GetInt("kanchoku", key, defval) ?? defval;
        }

        public bool GetBool(string key, bool defval = false)
        {
            return m_ini?.GetBool("kanchoku", key, defval) ?? defval;
        }

        public bool SetString(string key, string val)
        {
            return m_ini?.SetString("kanchoku", key, val) ?? false;
        }

        public bool SetInt(string key, int val)
        {
            return m_ini?.SetInt("kanchoku", key, val) ?? false;
        }

        public bool SetBool(string key, bool val)
        {
            return m_ini?.SetBool("kanchoku", key, val) ?? false;
        }

        public string GetStringFromSection(string section, string key, string defval = "")
        {
            return m_ini?.GetString(section, key, defval) ?? defval;
        }

        public string[] GetSectionNames()
        {
            return m_ini?.GetSectionNames() ?? new string[0];
        }
    }

    public class KanchokuIni : KanchokuIniBase
    {
        private static KanchokuIni s_kanchokuIni = null;

        public static KanchokuIni Singleton {
            get {
                if (s_kanchokuIni == null) {
                    s_kanchokuIni = new KanchokuIni();
                }
                return s_kanchokuIni;
            }
        }

        private KanchokuIni()
            : base("kanchoku.ini")
        {
        }

        public static string MakeFullPath(string filename)
        {
            return Singleton.KanchokuDir._joinAbsPath(filename);
        }

    }

    public class UserKanchokuIni : KanchokuIniBase
    {
        private static UserKanchokuIni s_kanchokuIni = null;

        public static UserKanchokuIni Singleton {
            get {
                if (s_kanchokuIni == null) {
                    s_kanchokuIni = new UserKanchokuIni();
                }
                return s_kanchokuIni;
            }
        }

        private UserKanchokuIni()
            : base("kanchoku.user.ini")
        {
        }

    }


}

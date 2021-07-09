using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Utils
{
    /// <summary>
    /// ini ファイルの読み書き
    /// </summary>
    public class IniFileAccessor
    {
        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(
            string lpApplicationName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedstring,
            int nSize,
            string lpFileName);

        [DllImport("kernel32.dll")]
        private static extern int WritePrivateProfileString(
            string lpApplicationName,
            string lpKeyName,
            string lpstring,
            string lpFileName);

        [DllImport("kernel32.dll")]
        static extern int GetPrivateProfileSectionNames(
            IntPtr lpszReturnBuffer,
            uint nSize,
            string lpFileName);

        // iniファイルパス
        public string IniFilePath { get; set; }

        // LogLevel
        public const int LOG_ERROR = 3;
        public const int LOG_WARN = 2;
        public const int LOG_INFO = 1;
        public const int LOG_DEBUG = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public IniFileAccessor(string iniFilePath)
        {
            IniFilePath = iniFilePath;
        }

        /// <summary>
        /// 指定の ini ファイルは存在するか
        /// </summary>
        /// <returns></returns>
        public bool IniFileExists()
        {
            return IniFilePath._notEmpty() && Helper.FileExists(IniFilePath);
        }

        /// <summary>
        /// iniファイルからsectionの一覧を取得
        /// </summary>
        public string[] GetSectionNames()
        {
            try {
                IntPtr ptr = Marshal.StringToHGlobalAnsi(new string('\0', 4096));
                try {
                    int len = GetPrivateProfileSectionNames(ptr, 4096, IniFilePath);
                    if (len > 0) {
                        var names = Marshal.PtrToStringAnsi(ptr, len).Trim('\0');
                        if (names._notEmpty()) return names.Split('\0');
                    }
                } catch {
                } finally {
                    Marshal.FreeHGlobal(ptr);
                }
            } catch { }
            return new string[0];
        }

        /// <summary>
        /// sectionとkeyからiniファイルの設定値(文字列)を取得します。
        /// 指定したsectionとkeyの組合せが無い場合はdefaultvalueで指定した値が返ります。
        /// </summary>
        public string GetString(string section, string key, string defaultvalue = "")
        {
            try
            {
                StringBuilder sb = new StringBuilder(256);
                GetPrivateProfileString(section, key, defaultvalue, sb, sb.Capacity, IniFilePath);
                return sb.ToString();
            }
            catch (Exception)
            {
                return defaultvalue;
            }
        }

        /// <summary>
        /// sectionとkeyからiniファイルの設定値(Int)を取得します。
        /// 指定したsectionとkeyの組合せが無い場合はdefaultvalueで指定した値が返ります。
        /// </summary>
        public int GetInt(string section, string key, int defaultValue = 0)
        {
            var val = GetString(section, key, $"{defaultValue}");
            int result;
            if (int.TryParse(val, out result))
            {
                return result;
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// sectionとkeyからiniファイルの設定値(bool)を取得します。
        /// 指定したsectionとkeyの組合せが無い場合はdefaultvalueで指定した値が返ります。
        /// </summary>
        public bool GetBool(string section, string key, bool defaultValue = false)
        {
            return GetString(section, key)._parseBool(defaultValue);
        }

        /// <summary>
        /// sectionとkeyに対して、iniファイルに設定値(文字列)を設定します。
        /// 成功したら true を返し、失敗したら false を返す。
        /// </summary>
        public bool SetString(string section, string key, string newValue)
        {
            try
            {
                WritePrivateProfileString(section, key, newValue, IniFilePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// sectionとkeyに対して、iniファイルに設定値(Int)を設定します。
        /// </summary>
        public bool SetInt(string section, string key, int newValue)
        {
            return SetString(section, key, $"{newValue}");
        }

        /// <summary>
        /// sectionとkeyに対して、iniファイルに設定値(bool)を設定します。
        /// </summary>
        public bool SetBool(string section, string key, bool newValue)
        {
            return SetString(section, key, newValue.ToString().ToLower());
        }

        public bool RemoveSection(string section)
        {
            return SetString(section, null, null);
        }

        public bool RemoveKey(string section, string key)
        {
            return SetString(section, key, null);
        }
    }
}

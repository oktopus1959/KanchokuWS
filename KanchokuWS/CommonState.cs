using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KanchokuWS
{
    public static class CommonState
    {
        public static string CenterString { get; set; } = "";

        public static bool VkbVisible { get; set; } = false;

        public static DateTime VkbVisibiltyChangedDt { get; set; }
    }
}

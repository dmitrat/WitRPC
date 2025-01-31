using System;
using System.Collections.Generic;
using System.Text;

namespace OutWit.Common.Proxy.Generator.Utils
{
    public static class ValueUtils
    {
        public static string GetBoolString(this bool me)
        {
            return me ? "true" : "false";
        }
    }
}

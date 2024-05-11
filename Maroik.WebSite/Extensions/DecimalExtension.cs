using System.ComponentModel;
using System.Reflection;

namespace Maroik.WebSite.Extensions
{
    public static class DecimalExtension
    {
        public static string TrimTrailingZeros(this decimal value)
        {
            return value % 1 == 0 ? ((int)value).ToString() : value.ToString().TrimEnd('0').TrimEnd('.');
        }
    }
}
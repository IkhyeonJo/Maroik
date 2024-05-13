namespace Maroik.WebSite.Extensions
{
    public static class DecimalExtension
    {
        public static string TrimTrailingZeros(this decimal value)
        {
            try
            {
                return value % 1 == 0 ? ((int)value).ToString() : value.ToString().TrimEnd('0').TrimEnd('.');
            }
            catch
            {
                return default(decimal).ToString();
            }
        }

        public static string TrimTrailingZeros(this decimal? value)
        {
            try
            {
                return value % 1 == 0 ? ((int)value).ToString() : value.ToString().TrimEnd('0').TrimEnd('.');
            }
            catch
            {
                return default(decimal).ToString();
            }
        }
    }
}
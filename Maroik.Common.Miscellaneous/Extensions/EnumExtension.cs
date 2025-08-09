using System.ComponentModel;

namespace Maroik.Common.Miscellaneous.Extensions;

public static class EnumHelper
{
    public static string GetDescription(Enum en)
    {
        var type = en.GetType();

        var memInfo = type.GetMember(en.ToString());

        if (memInfo.Length <= 0) return en.ToString();
        var attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

        return attrs.Length > 0 ? ((DescriptionAttribute)attrs[0]).Description : en.ToString();
    }
}
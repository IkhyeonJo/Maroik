namespace Maroik.Core.Client.Utilities;

public static class StringExtension
{
    public static string GetAmountLabel(this string value, string monetaryUnit)
    {
        return $"{value ?? ""} ({monetaryUnit ?? ""})";
    }
}

using System.Globalization;
using Maroik.Core.Domain.Results;

namespace Maroik.Core.Domain.Tests.Results.Fixtures;

public static class ErrorOrConversions
{
    public static ErrorOr<string> ToString(int num) => num.ToString(CultureInfo.InvariantCulture);

    public static ErrorOr<int> ToInt(string str) => int.Parse(str, CultureInfo.InvariantCulture);

    public static Task<ErrorOr<int>> ToIntAsync(string str) => ErrorOrFactory.FromAsync(int.Parse(str, CultureInfo.InvariantCulture));

    public static Task<ErrorOr<string>> ToStringAsync(int num) => ErrorOrFactory.FromAsync(num.ToString(CultureInfo.InvariantCulture));
}

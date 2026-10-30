using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace Maroik.Website.Tests.Grids.Fixtures;

public static class HttpUtility
{
    public static IQueryCollection ParseQueryString(String query)
    {
        return new QueryCollection(QueryHelpers.ParseQuery(query));
    }
}

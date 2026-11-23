using Maroik.Website.Serializers;

namespace Maroik.Website.Extensions;

/// <summary>Writes a streamed response of newline-delimited JSON (the AI chat's answer).</summary>
public static class HttpResponseExtensions
{
    extension(HttpResponse response)
    {
        /// <summary>
        /// Writes <paramref name="value"/> as one JSON line (<see cref="ClientJsonSerializer"/>) and sends it at once, so the page shows
        /// each piece as it arrives.
        /// </summary>
        /// <param name="value">The line's object.</param>
        /// <param name="ct">Cancels the write (the user left).</param>
        public async Task WriteJsonLineAsync(object value, CancellationToken ct)
        {
            await response.WriteAsync(ClientJsonSerializer.Serialize(value) + "\n", ct);
            await response.Body.FlushAsync(ct);
        }
    }
}

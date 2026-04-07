using Maroik.Core.Client.Clients;
using Maroik.Core.Contract.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Core.Client.Extensions;

/// <summary>
/// Extension methods for registering Maroik.Core.Client services into the DI container.
/// Call <see cref="AddClientServices"/> from Program.cs during application startup.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mail, file-storage, and ClamAV client implementations as scoped services.
    /// </summary>
    public static void AddClientServices(this IServiceCollection services)
    {
        // Register the SMTP email client
        services.AddScoped<IMailClient, MailClient>();

        // Register the HTTP file-storage proxy client
        services.AddScoped<IFileClient, FileClient>();

        // Register the ClamAV antivirus scanning client
        services.AddScoped<IClamavClient, ClamavClient>();
    }
}

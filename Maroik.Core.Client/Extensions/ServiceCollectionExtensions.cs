using Maroik.Core.Client.Clients;
using Maroik.Core.Contract.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Core.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddClientServices(this IServiceCollection services)
    {
        services.AddScoped<IMailClient, MailClient>();
        services.AddScoped<IFileClient, FileClient>();
        services.AddScoped<IClamavClient, ClamavClient>();
    }
}

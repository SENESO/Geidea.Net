using System;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Registers <see cref="Geidea.GeideaClient"/> in the DI container.
    /// </summary>
    public static class GeideaServiceCollectionExtensions
    {
        public static IServiceCollection AddGeidea(
            this IServiceCollection services, Geidea.GeideaClientOptions options)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            services.TryAddSingleton(options ?? throw new ArgumentNullException(nameof(options)));
            services.TryAddSingleton<Geidea.GeideaClient>();
            return services;
        }

        public static IServiceCollection AddGeidea(
            this IServiceCollection services, Action<Geidea.GeideaClientOptions> configure)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var options = new Geidea.GeideaClientOptions();
            configure(options);
            return services.AddGeidea(options);
        }
    }
}

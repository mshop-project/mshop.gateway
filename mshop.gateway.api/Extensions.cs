using Consul;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;

namespace mshop.gateway.api
{
    internal static class Extensions
    {
        public static IReverseProxyBuilder DiscoverFromConsul(this IReverseProxyBuilder builder)
        {
            var services = builder.Services;

            AddConsulIfNotExist(services);

            services.AddOptions<ServiceDiscoveryConfiguration>()
                .BindConfiguration(ServiceDiscoveryConfiguration.SectionName)
                .ValidateOnStart();


            var serviceProvider = services.BuildServiceProvider();


            var proxyConfigProviders = serviceProvider.GetRequiredService<IEnumerable<IProxyConfigProvider>>();

            var consulClient = serviceProvider.GetRequiredService<IConsulClient>();

            var options = serviceProvider.GetRequiredService<IOptionsMonitor<ServiceDiscoveryConfiguration>>();
            var logger = serviceProvider.GetRequiredService<ILogger<ProxyConfigProvider>>();

            services.RemoveAll<IProxyConfigProvider>();

            services.AddSingleton<IProxyConfigProvider, ProxyConfigProvider>(_ => new ProxyConfigProvider(proxyConfigProviders, consulClient, options, logger));

            return builder;
        }

        private static void AddConsulIfNotExist(IServiceCollection services)
        {
            var consulIsAlreadyRegistered = services.Any(service => service.ServiceType == typeof(IConsulClient));

            if (!consulIsAlreadyRegistered)
                services.AddConsul();
        }
        public static IServiceCollection AddConsul(this IServiceCollection services)
        {
            services
                .AddOptions<ConsulClientConfiguration>()
                .BindConfiguration("Consul");

            services.AddSingleton<IConsulClient, ConsulClient>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<ConsulClientConfiguration>>();

                return new ConsulClient(options.Value);
            });

            return services;
        }
    }
}

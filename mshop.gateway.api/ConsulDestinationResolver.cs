using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Net;
using Yarp.ReverseProxy.ServiceDiscovery;

namespace mshop.gateway.api
{

    public class ConsulDestinationResolver : IDestinationResolver
    {
        private readonly IConsulClient _consulClient;
        private static int _currentIndex = -1; // Wskaźnik na aktualną instancję
        public ConsulDestinationResolver(IConsulClient consulClient)
        {
            _consulClient = consulClient;
        }

        public async ValueTask<ResolvedDestinationCollection> ResolveDestinationsAsync(
        IReadOnlyDictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig> destinations,
        CancellationToken cancellationToken)
        {
            var resolvedDestinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>();
            var tasks = destinations.Select(async kvp =>
            {
                var (destinationId, destinationConfig) = kvp;
                var serviceName = destinationConfig.Host;

                // Resolve endpoints from Consul
                var services = await _consulClient.Catalog.Service(serviceName, null, null, cancellationToken);

                var results = services.Response
                    .Select(service => new Yarp.ReverseProxy.Configuration.DestinationConfig
                    {
                        Address = $"http://{service.ServiceAddress}:{service.ServicePort}",
                        Health = null,
                        Host = destinationConfig.Host,
                        Metadata = null
                    })
                    .ToList();

                return (results, new CancellationChangeToken(cancellationToken));
            }).ToList();

            await Task.WhenAll(tasks);

            var allDestinations = tasks
                .SelectMany(task => task.Result.Item1)
                .ToList();

            // Apply round-robin to select one destination
            if (allDestinations.Any())
            {
                lock (_consulClient)
                {
                    // Update the index for round-robin selection
                    _currentIndex = (_currentIndex + 1) % allDestinations.Count;
                    var selectedDestination = allDestinations[_currentIndex];

                    resolvedDestinations.Add("singleDestination", selectedDestination);
                }
            }

            var changeTokens = tasks.Select(task => task.Result.Item2).ToList();
            var compositeChangeToken = new CompositeChangeToken(changeTokens);

            return new ResolvedDestinationCollection(resolvedDestinations, compositeChangeToken);
        }
    }



}

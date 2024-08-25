using Consul;
using Microsoft.Extensions.ServiceDiscovery.Http;
using System.Reflection;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.ServiceDiscovery;

namespace mshop.gateway.api
{
    public class MshopForwarderHttpClientFactory(ILogger<MshopForwarderHttpClientFactory> logger, IConsulClient consulClient)
    : ForwarderHttpClientFactory
    {
        protected override HttpMessageHandler WrapHandler(ForwarderHttpClientContext context, HttpMessageHandler handler)
        {
            return new CustomHandlerWrapper(handler, logger, consulClient);
        }
    }

    public class CustomHandlerWrapper : HttpMessageHandler
    {
        private readonly HttpMessageHandler _innerHandler;
        private readonly IDestinationResolver _destinationResolver;
        ILogger<MshopForwarderHttpClientFactory> _logger;

        public CustomHandlerWrapper(HttpMessageHandler innerHandler, ILogger<MshopForwarderHttpClientFactory> logger, IConsulClient consulClient)
        {
            _innerHandler = innerHandler;
            _destinationResolver = new ConsulDestinationResolver(consulClient);
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
       

            var destinations = new Dictionary<string, DestinationConfig>
            {
                ["myService"] = new DestinationConfig
                {
                    Address = request.RequestUri!.Host,
                    Host = request.RequestUri.Host
                }
            };

            // Rozwiązujemy cele.
            var resolvedDestinations = await _destinationResolver.ResolveDestinationsAsync(destinations, cancellationToken);

            var destination = resolvedDestinations.Destinations.FirstOrDefault();

                var baseUrl = new Uri(destination.Value.Address); 
                request.RequestUri = new Uri(baseUrl, request.RequestUri.PathAndQuery);

                if (!string.IsNullOrEmpty(destination.Value.Host))
                {
                    request.Headers.Host = destination.Value.Host;
                }
        

            var sendAsyncMethod = _innerHandler.GetType().GetMethod("SendAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (sendAsyncMethod == null)
            {
                throw new InvalidOperationException("Nie można znaleźć metody SendAsync w wewnętrznym handlerze.");
            }
            _logger.LogInformation("Request redirected to: {0}", request.RequestUri.OriginalString);

            var task = (Task<HttpResponseMessage>)sendAsyncMethod.Invoke(_innerHandler, new object[] { request, cancellationToken })!;
            var response = await task.ConfigureAwait(false);

            return response;
        }
    }
}

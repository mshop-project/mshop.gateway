namespace mshop.gateway.api
{
    internal sealed class ServiceDiscoveryConfiguration
    {
        public static string SectionName => "ServiceDiscovery";

        public uint PeriodicUpdateIntervalInSeconds { get; init; }
    }
}

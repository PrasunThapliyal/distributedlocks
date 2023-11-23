namespace DistributedLocks.NetworkDesign
{
    public class NetworkManager : INetworkManager
    {
        private readonly ILogger<NetworkManager> logger;

        // This will be a singleton class

        private Dictionary<Guid, Network> _cachedNetworks = new Dictionary<Guid, Network>();

        public NetworkManager(
            ILogger<NetworkManager> logger
            )
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<Network> GetNetworks()
        {
            return _cachedNetworks.Values;
        }

        public Network Create()
        {
            var network = new Network
            {
                Name = "NetworkA",
                ProjectId = Guid.NewGuid()
            };

            _cachedNetworks.Add(network.ProjectId, network);

            return network;
        }

        public Network? Get(Guid id)
        {
            if (_cachedNetworks.TryGetValue(id, out var network)) return network;

            return null;
        }

        public bool UpdateName(Guid networkId, long lockId)
        {
            if (_cachedNetworks.TryGetValue((Guid)networkId, out var network))
            {
                network.Name += $": LockId: {lockId}";
                return true;
            }

            return false;
        }

    }

    public class Network
    {
        public Guid ProjectId { get; set; }
        public string? Name { get; set; }
    }
}

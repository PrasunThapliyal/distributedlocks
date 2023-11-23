namespace DistributedLocks.NetworkDesign
{
    public interface INetworkManager
    {
        Network Create();
        Network? Get(Guid id);
        IEnumerable<Network> GetNetworks();
        bool UpdateName(Guid networkId, long lockId);
    }
}
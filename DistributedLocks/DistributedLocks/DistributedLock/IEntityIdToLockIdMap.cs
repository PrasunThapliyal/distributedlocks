namespace DistributedLocks.DistributedLock
{
    public interface IEntityIdToLockIdMap
    {
        Task<long> GetLockIdAsync(Guid entityId);
    }
}
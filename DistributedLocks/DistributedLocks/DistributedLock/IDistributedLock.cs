namespace DistributedLocks.DistributedLock
{
    public interface IDistributedLock : IDisposable
    {
        Task<bool> AcquireReadLock(Guid entityId);
        Task<bool> AcquireWriteLock(Guid entityId);
        Task<bool> ReleaseReadLock(Guid entityId);
        Task<bool> ReleaseWriteLock(Guid entityId);
    }
}
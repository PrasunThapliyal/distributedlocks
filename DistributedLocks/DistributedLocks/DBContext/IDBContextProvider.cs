namespace DistributedLocks.DBContext
{
    public interface IDBContextProvider
    {
        DistributedLockDBContext? GetNewDBContext();
    }
}
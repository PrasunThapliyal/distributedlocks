using DistributedLocks.DBContext;
using DistributedLocks.Domain;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DistributedLocks.DistributedLock
{
    public class DistributedLockv1 : IDistributedLock
    {
        // Singleton

        private readonly ILogger<DistributedLockv1> logger;
        private readonly IServiceProvider serviceProvider;

        private Dictionary<Guid, long> entityIdToLockIdCache = new Dictionary<Guid, long>();

        public DistributedLockv1(
            ILogger<DistributedLockv1> logger,
            IServiceProvider serviceProvider
            )
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private DistributedLockDBContext? GetNewDBContext()
        {
            var x = this.serviceProvider.GetService<DistributedLockDBContext>();
            if ( x == null )
            {
                this.logger.LogError($"GetNewDBContext: Failed to get instance of DBContext from ServiceProvider");
            }

            return x;
        }

        private async Task<long> GetLockIdAsync(Guid entityId)
        {
            // Get a unique integer number mapped to a Guid - Use DB auto-incrementing field for generating number

            try
            {
                // First check in cache

                var success = this.entityIdToLockIdCache.TryGetValue(entityId, out long lockId);
                if (success)
                {
                    this.logger.LogInformation($"GetLockId: Returning {entityId}, {lockId} from cache");
                    return lockId;
                }

                // Not found in cache .. check in DB

                this.logger.LogInformation($"GetLockId: No cache entry found for {entityId}");

                var dbContext = GetNewDBContext();

                if (dbContext?.EntityIdToLockId != null)
                {
                    var e = await dbContext.EntityIdToLockId.SingleOrDefaultAsync(p => p.EntityId == entityId).ConfigureAwait(false);
                    if (e != null)
                    {
                        this.logger.LogInformation($"GetLockId: EntityId {entityId} found in DB. Returing lockId {e.LockId}");

                        this.entityIdToLockIdCache.TryAdd(entityId, e.LockId);
                        return e.LockId;
                    }
                }

                // Not found in DB .. create one

                this.logger.LogInformation($"GetLockId: EntityId {entityId} not found in DB");

                if (dbContext?.EntityIdToLockId != null)
                {
                    this.logger.LogInformation($"GetLockId: Creating EntityId {entityId} in DB");

                    var e = new EntityIdToLockId
                    {
                        EntityId = entityId
                    };

                    dbContext.EntityIdToLockId.Add(e);
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);

                    this.logger.LogInformation($"GetLockId: Fetch Lock Id for EntityId {entityId} from DB");

                    var x = await dbContext.EntityIdToLockId.SingleOrDefaultAsync(p => p.EntityId == entityId).ConfigureAwait(false);
                    if (x != null)
                    {
                        this.logger.LogInformation($"GetLockId: Found newly created Lock Id for EntityId {entityId} from DB. LockId: {x.LockId}");

                        this.entityIdToLockIdCache.TryAdd(entityId, x.LockId);
                        return x.LockId;
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError($"GetLockId: Failed to generate a Lock Id for EntityId {entityId}. Exception: {ex}");
                throw;
            }

            this.logger.LogError($"GetLockId: Failed to generate a Lock Id for EntityId {entityId}. No exception occured");

            return -1; // This is an error
        }

        public async Task<bool> AcquireReadLock(Guid entityId)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation($"AcquireReadLock: EntityId: {entityId} waiting infinitely to get lock ..");

            var lockId = await GetLockIdAsync(entityId).ConfigureAwait(false);

            var dbContext = GetNewDBContext();

            if (dbContext != null)
            {
                // Obtain a shared lock
                var sql = $"SELECT pg_advisory_lock_shared({lockId})";
                await dbContext.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);

                this.logger.LogInformation($"AcquireReadLock: EntityId: {entityId}, LockId {lockId}. Lock Acquired. Time taken to acquire lock: {sw.ElapsedMilliseconds} ms");

                return true;
            }

            return false;
        }

        public async Task<bool> AcquireWriteLock(Guid entityId)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation($"AcquireWriteLock: EntityId: {entityId} waiting infinitely to get lock ..");

            var lockId = await GetLockIdAsync(entityId).ConfigureAwait(false);

            var dbContext = GetNewDBContext();

            if (dbContext != null)
            {
                // Obtain an exclusive lock
                await Console.Out.WriteLineAsync("====================1==================");

                var sql = $"SELECT pg_advisory_lock({lockId})";
                await dbContext.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);

                await Console.Out.WriteLineAsync("====================2==================");
                
                {
                    // Just for debugging, lets try to get the lock again
                    await dbContext.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);

                    await Console.Out.WriteLineAsync("====================3================== .. dear lord !!");
                }

                this.logger.LogInformation($"AcquireWriteLock: EntityId: {entityId}, LockId {lockId}. Lock Acquired. Time taken to acquire lock: {sw.ElapsedMilliseconds} ms");

                return true;
            }

            return false;
        }

        public async Task<bool> ReleaseReadLock(Guid entityId)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation($"ReleaseReadLock: EntityId: {entityId} started ..");

            var lockId = await GetLockIdAsync(entityId).ConfigureAwait(false);

            var dbContext = GetNewDBContext();

            if (dbContext != null)
            {
                // Unlock a shared lock
                var sql = $"SELECT pg_advisory_unlock_shared({lockId})";
                await dbContext.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);

                this.logger.LogInformation($"ReleaseReadLock: EntityId: {entityId}, LockId {lockId}. Lock Released. Time taken to release lock: {sw.ElapsedMilliseconds} ms");

                return true;
            }

            return false;
        }

        public async Task<bool> ReleaseWriteLock(Guid entityId)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation($"ReleaseWriteLock: EntityId: {entityId} started ..");

            var lockId = await GetLockIdAsync(entityId).ConfigureAwait(false);

            var dbContext = GetNewDBContext();

            if (dbContext != null)
            {
                // Unlock an exclusive lock
                var sql = $"SELECT pg_advisory_unlock({lockId})";
                await dbContext.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);

                this.logger.LogInformation($"ReleaseWriteLock: EntityId: {entityId}, LockId {lockId}. Lock Released. Time taken to release lock: {sw.ElapsedMilliseconds} ms");

                return true;
            }

            return false;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

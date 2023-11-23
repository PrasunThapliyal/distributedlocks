using DistributedLocks.DBContext;
using DistributedLocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace DistributedLocks.DistributedLock
{
    public class EntityIdToLockIdMap : IEntityIdToLockIdMap
    {
        // Register as Singleton

        private readonly ILogger<EntityIdToLockIdMap> logger;
        private readonly IDBContextProvider dBContextProvider;

        private Dictionary<Guid, long> _cache = new Dictionary<Guid, long>();


        public EntityIdToLockIdMap
            (
            ILogger<EntityIdToLockIdMap> logger,
            IDBContextProvider dBContextProvider
            )
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dBContextProvider = dBContextProvider ?? throw new ArgumentNullException(nameof(dBContextProvider));
        }
        public async Task<long> GetLockIdAsync(Guid entityId)
        {
            // Get a unique integer number mapped to a Guid - Use DB auto-incrementing field for generating number

            try
            {
                // First check in cache

                var success = _cache.TryGetValue(entityId, out long lockId);
                if (success)
                {
                    this.logger.LogInformation($"GetLockId: Returning {entityId}, {lockId} from cache");
                    return lockId;
                }

                // Not found in cache .. check in DB

                this.logger.LogInformation($"GetLockId: No cache entry found for {entityId}");

                var dbContext = this.dBContextProvider.GetNewDBContext();

                if (dbContext?.EntityIdToLockId != null)
                {
                    var e = await dbContext.EntityIdToLockId.SingleOrDefaultAsync(p => p.EntityId == entityId).ConfigureAwait(false);
                    if (e != null)
                    {
                        this.logger.LogInformation($"GetLockId: EntityId {entityId} found in DB. Returing lockId {e.LockId}");

                        _cache.TryAdd(entityId, e.LockId);
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

                        _cache.TryAdd(entityId, x.LockId);
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
    }
}

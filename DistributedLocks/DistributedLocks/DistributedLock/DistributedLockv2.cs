using DistributedLocks.DBContext;
using DistributedLocks.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DistributedLocks.DistributedLock
{
    public class DistributedLockv2 : IDistributedLock
    {
        // Register as Transient

        // Usage: var lock = Get new Read or Write lock
        //      Do some work
        // lock.Release()


        private readonly ILogger<DistributedLock> logger;
        private readonly IConfiguration configuration;
        private readonly IEntityIdToLockIdMap entityIdToLockIdMap;
        private readonly IDBContextProvider dBContextProvider;

        private DistributedLockDBContext? dbContext;

        public DistributedLockv2(
            ILogger<DistributedLock> logger,
            IConfiguration configuration,
            IEntityIdToLockIdMap entityIdToLockIdMap,
            IDBContextProvider dBContextProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.entityIdToLockIdMap = entityIdToLockIdMap ?? throw new ArgumentNullException(nameof(entityIdToLockIdMap));
            this.dBContextProvider = dBContextProvider ?? throw new ArgumentNullException(nameof(dBContextProvider));

            dbContext = this.dBContextProvider.GetNewDBContext();
        }

        

        public async Task<bool> AcquireReadLock(Guid entityId)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation($"AcquireReadLock: EntityId: {entityId} waiting infinitely to get lock ..");

            var lockId = await this.entityIdToLockIdMap.GetLockIdAsync(entityId).ConfigureAwait(false);

            if (dbContext != null)
            {
                // Obtain a shared lock
                var sql = $"SELECT pg_advisory_lock_shared({lockId})";
                await ExecuteRawQueryAsync(dbContext, sql).ConfigureAwait(false);

                this.logger.LogInformation($"AcquireReadLock: EntityId: {entityId}, LockId {lockId}. Lock Acquired. Time taken to acquire lock: {sw.ElapsedMilliseconds} ms");

                return true;
            }

            return false;
        }

        public async Task<bool> AcquireWriteLock(Guid entityId)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation($"AcquireWriteLock: EntityId: {entityId} waiting infinitely to get lock ..");

            var lockId = await this.entityIdToLockIdMap.GetLockIdAsync(entityId).ConfigureAwait(false);

            if (dbContext != null)
            {
                // Obtain an exclusive lock

                var sql = $"SELECT pg_advisory_lock({lockId})";
                await ExecuteRawQueryAsync(dbContext, sql).ConfigureAwait(false);

                this.logger.LogInformation($"AcquireWriteLock: EntityId: {entityId}, LockId {lockId}. Lock Acquired. Time taken to acquire lock: {sw.ElapsedMilliseconds} ms");

                return true;
            }

            return false;
        }

        public async Task<bool> ReleaseReadLock(Guid entityId)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation($"ReleaseReadLock: EntityId: {entityId} started ..");

            var lockId = await this.entityIdToLockIdMap.GetLockIdAsync(entityId).ConfigureAwait(false);

            if (dbContext != null)
            {
                // Unlock a shared lock
                var sql = $"SELECT pg_advisory_unlock_shared({lockId})";
                await ExecuteRawQueryAsync(dbContext, sql).ConfigureAwait(false);

                this.logger.LogInformation($"ReleaseReadLock: EntityId: {entityId}, LockId {lockId}. Lock Released. Time taken to release lock: {sw.ElapsedMilliseconds} ms");

                return true;
            }

            return false;
        }

        public async Task<bool> ReleaseWriteLock(Guid entityId)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation($"ReleaseWriteLock: EntityId: {entityId} started ..");

            var lockId = await this.entityIdToLockIdMap.GetLockIdAsync(entityId).ConfigureAwait(false);

            if (dbContext != null)
            {
                // Unlock an exclusive lock
                var sql = $"SELECT pg_advisory_unlock({lockId})";
                await ExecuteRawQueryAsync(dbContext, sql).ConfigureAwait(false);

                this.logger.LogInformation($"ReleaseWriteLock: EntityId: {entityId}, LockId {lockId}. Lock Released. Time taken to release lock: {sw.ElapsedMilliseconds} ms");

                return true;
            }

            return false;
        }

        private static async Task ExecuteRawQueryAsync(DistributedLockDBContext? dbContext, string sql)
        {
            if (dbContext != null)
            {
                await dbContext.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);
            }
            else
            {
                throw new ApplicationException("DBContext is null");
            }

            //if (dbContext != null)
            //{
            //    using (var command = dbContext.Database.GetDbConnection().CreateCommand())
            //    {
            //        command.CommandText = sql;
            //        command.CommandType = System.Data.CommandType.Text;

            //        await dbContext.Database.OpenConnectionAsync().ConfigureAwait(false);

            //        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            //    }
            //}
            //else
            //{
            //    throw new ApplicationException("DBContext is null");
            //}
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

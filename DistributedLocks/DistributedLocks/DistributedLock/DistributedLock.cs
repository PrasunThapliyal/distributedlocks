using DistributedLocks.DBContext;
using DistributedLocks.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DistributedLocks.DistributedLock
{
    public class DistributedLock : IDistributedLock
    {
        // Register as Transient

        // Usage: var lock = Get new Read or Write lock
        //      Do some work
        // lock.Release()


        private readonly ILogger<DistributedLock> logger;
        private readonly IConfiguration configuration;
        private readonly IEntityIdToLockIdMap entityIdToLockIdMap;

        private readonly NpgsqlConnection connection;
        private bool disposedValue;

        public DistributedLock(
            ILogger<DistributedLock> logger,
            IConfiguration configuration,
            IEntityIdToLockIdMap entityIdToLockIdMap,
            IDBContextProvider dBContextProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.entityIdToLockIdMap = entityIdToLockIdMap ?? throw new ArgumentNullException(nameof(entityIdToLockIdMap));

            var connectionString = this.configuration.GetConnectionString("DistributedLockDbConnection");

            this.connection = new NpgsqlConnection(connectionString);
            this.connection.Open();
        }



        public async Task<bool> AcquireReadLock(Guid entityId)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation($"AcquireReadLock: EntityId: {entityId} waiting infinitely to get lock ..");

            var lockId = await this.entityIdToLockIdMap.GetLockIdAsync(entityId).ConfigureAwait(false);

                // Obtain a shared lock
                var sql = $"SELECT pg_advisory_lock_shared({lockId})";
                await ExecuteRawQueryAsync(sql).ConfigureAwait(false);

                this.logger.LogInformation($"AcquireReadLock: EntityId: {entityId}, LockId {lockId}. Lock Acquired. Time taken to acquire lock: {sw.ElapsedMilliseconds} ms");

                return true;
        }

        public async Task<bool> AcquireWriteLock(Guid entityId)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation($"AcquireWriteLock: EntityId: {entityId} waiting infinitely to get lock ..");

            var lockId = await this.entityIdToLockIdMap.GetLockIdAsync(entityId).ConfigureAwait(false);

            // Obtain an exclusive lock

            var sql = $"SELECT pg_advisory_lock({lockId})";
            await ExecuteRawQueryAsync(sql).ConfigureAwait(false);

            this.logger.LogInformation($"AcquireWriteLock: EntityId: {entityId}, LockId {lockId}. Lock Acquired. Time taken to acquire lock: {sw.ElapsedMilliseconds} ms");

            return true;
        }

        public async Task<bool> ReleaseReadLock(Guid entityId)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation($"ReleaseReadLock: EntityId: {entityId} started ..");

            var lockId = await this.entityIdToLockIdMap.GetLockIdAsync(entityId).ConfigureAwait(false);

            // Unlock a shared lock
            var sql = $"SELECT pg_advisory_unlock_shared({lockId})";
            await ExecuteRawQueryAsync(sql).ConfigureAwait(false);

            this.logger.LogInformation($"ReleaseReadLock: EntityId: {entityId}, LockId {lockId}. Lock Released. Time taken to release lock: {sw.ElapsedMilliseconds} ms");

            return true;
        }

        public async Task<bool> ReleaseWriteLock(Guid entityId)
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation($"ReleaseWriteLock: EntityId: {entityId} started ..");

            var lockId = await this.entityIdToLockIdMap.GetLockIdAsync(entityId).ConfigureAwait(false);

            // Unlock an exclusive lock
            var sql = $"SELECT pg_advisory_unlock({lockId})";
            await ExecuteRawQueryAsync(sql).ConfigureAwait(false);

            this.logger.LogInformation($"ReleaseWriteLock: EntityId: {entityId}, LockId {lockId}. Lock Released. Time taken to release lock: {sw.ElapsedMilliseconds} ms");

            return true;
        }

        private async Task ExecuteRawQueryAsync(string sql)
        {
            await using (var cmd = new NpgsqlCommand(sql, this.connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    Console.WriteLine($"Disposing connection ..");
                    this.connection.Close();
                    this.connection.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~DistributedLock()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

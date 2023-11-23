namespace DistributedLocks.DBContext
{
    public class DBContextProvider : IDBContextProvider
    {
        // Register as singleton

        private readonly ILogger<DBContextProvider> logger;
        private readonly IServiceProvider serviceProvider;

        public DBContextProvider(
            ILogger<DBContextProvider> logger,
            IServiceProvider serviceProvider
            )
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }


        public DistributedLockDBContext? GetNewDBContext()
        {
            var x = this.serviceProvider.GetService<DistributedLockDBContext>();
            if (x == null)
            {
                this.logger.LogError($"GetNewDBContext: Failed to get instance of DBContext from ServiceProvider");
            }

            return x;
        }

    }
}

using DistributedLocks.DBContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace DistributedLocks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddDbContext<DistributedLockDBContext>(ServiceLifetime.Transient);

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            InitializeDistributedLockDB(app);

            app.MapControllers();

            app.Run();
        }

        private static void InitializeDistributedLockDB(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<DistributedLockDBContext>();
            var canConnect = false;
            if (dbContext != null)
            {
                try
                {
                    canConnect = dbContext.Database.CanConnect();
                }
                catch
                {
                    canConnect = false;
                }

                if (!canConnect)
                {
                    try
                    {
                        var dbCreator = dbContext.Database.GetService<IRelationalDatabaseCreator>();
                        dbCreator.Create();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to create DB: Exception: {ex}");
                        throw;
                    }
                }

                dbContext.Database.Migrate();
            }
        }

    }
}
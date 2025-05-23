using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nostra.DataLoad.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Apply migrations if they haven't been applied
                await context.Database.MigrateAsync();

                // Check if we need to seed data
                if (!await context.Set<YourEntityType>().AnyAsync())
                {
                    logger.LogInformation("Seeding database...");
                    
                    // Add your seed data here
                    // Example:
                    // await context.YourEntities.AddRangeAsync(
                    //     new YourEntityType { Property1 = "Value1", Property2 = "Value2" },
                    //     new YourEntityType { Property1 = "Value3", Property2 = "Value4" }
                    // );
                    
                    await context.SaveChangesAsync();
                    logger.LogInformation("Database seeding completed successfully.");
                }
                else
                {
                    logger.LogInformation("Database already contains data. Skipping seed operation.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }
        
        public static IHost SeedData(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                
                try
                {
                    InitializeAsync(services, logger).Wait();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }
            
            return host;
        }
    }
}
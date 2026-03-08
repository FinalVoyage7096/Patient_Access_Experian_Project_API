using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Patient_Access_Experian_Project_API.Data;

namespace Patient_Access_Experian_Project_API.Tests.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private SqliteConnection? _connection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // Remove DbContext + options
                services.RemoveAll<PatientAccessDbContext>();
                services.RemoveAll<DbContextOptions<PatientAccessDbContext>>();
                services.RemoveAll<DbContextOptions>();

                // Remove options configuration delegates
                services.RemoveAll<IConfigureOptions<DbContextOptions<PatientAccessDbContext>>>();
                services.RemoveAll<IConfigureNamedOptions<DbContextOptions<PatientAccessDbContext>>>();
                services.RemoveAll<IConfigureOptions<DbContextOptions>>();
                services.RemoveAll<IConfigureNamedOptions<DbContextOptions>>();

                // Create SQLite in-memory DB (connection must remain open)
                _connection = new SqliteConnection("Data Source=:memory:");
                _connection.Open();

                // Remove any EF Core SqlServer provider registrations left over from the app
                var sqlServerDescriptors = services.Where(sd =>
                    (sd.ImplementationType != null && sd.ImplementationType.FullName != null && sd.ImplementationType.FullName.Contains("SqlServer")) ||
                    (sd.ServiceType != null && sd.ServiceType.FullName != null && sd.ServiceType.FullName.Contains("SqlServer"))
                ).ToList();
                foreach (var sd in sqlServerDescriptors)
                {
                    services.Remove(sd);
                }

                // Register SQLite EF provider on the test service collection
                services.AddEntityFrameworkSqlite();

                services.AddDbContext<PatientAccessDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });

                // Create schema using an isolated service provider so SqlServer and Sqlite providers
                // are not registered in the same provider (avoids multiple provider registration error)
                var schemaServices = new ServiceCollection();
                schemaServices.AddEntityFrameworkSqlite();
                schemaServices.AddDbContext<PatientAccessDbContext>(options => options.UseSqlite(_connection));
                using var schemaProvider = schemaServices.BuildServiceProvider();
                using var scope = schemaProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PatientAccessDbContext>();
                db.Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection?.Dispose();
                _connection = null;
            }
        }
    }
}
using backend_spo.Data;
using backend_spo.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace backend_spo.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<ApplicationDbContext>();

            services.RemoveAll<IConnectionMultiplexer>();
            services.RemoveAll<ITokenStoreService>();

            var connectionString =
                Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString =
                    "Host=localhost;Port=5432;Database=backend_spo_test;Username=postgres";
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString)
                    .UseSnakeCaseNamingConvention();
            });

            services.AddScoped<ITokenStoreService, FakeTokenStoreService>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        db.Database.Migrate();

        TestDbSeeder.Seed(db);

        return host;
    }
}
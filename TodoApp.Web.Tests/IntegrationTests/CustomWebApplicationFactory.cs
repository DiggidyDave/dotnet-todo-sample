using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoApp.Web.Data;
using TodoApp.Web.Services;
using Moq;

namespace TodoApp.Web.Tests.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove all existing DbContext registrations
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Remove the DbContext itself
            var dbContextServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbContextServiceDescriptor != null)
            {
                services.Remove(dbContextServiceDescriptor);
            }

            // Create and open a SQLite in-memory connection (shared across the test)
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Add DbContext using in-memory SQLite
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Replace email service with mock
            services.RemoveAll(typeof(IEmailService));
            var mockEmailService = new Mock<IEmailService>();
            services.AddSingleton(mockEmailService.Object);

            // Build service provider and ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}

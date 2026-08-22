using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace OneSwiss.Server;

// Используется командой `dotnet ef migrations` на этапе design-time, когда DI-контейнер
// приложения не собран (не нужен ни запущенный хост, ни живое подключение к БД).
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var options = new DbContextOptionsBuilder<AppDbContext>().Options;
        return new AppDbContext(options, configuration);
    }
}

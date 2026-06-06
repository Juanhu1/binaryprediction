using BinaryPrediction.Infrastructure.Extensions;
using BinaryPrediction.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BinaryPrediction.Api.Tests;

public class UnitTest1
{
    [Fact]
    public void AddInfrastructure_RegistersBinaryPredictionDbContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=binaryprediction;Username=postgres;Password=postgres"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BinaryPredictionDbContext>();

        Assert.NotNull(dbContext);
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Telegram.Bot.States.Tests.StateConfigurations;

public class StateDataProvider
{
    [Fact]
    public async Task AddDefaultProviderWithDelegate_ShouldRegisterProvider()
    {
        // arrange
        var services = new ServiceCollection();
        new StatesConfiguration(services, [], "ru")
            .ConfigureDefaultDataProvider<TestStateData>(() => Task.FromResult(new TestStateData { Value = "My data" }));

        var serviceProvider = services.BuildServiceProvider();

        // act
        var service = serviceProvider.GetService<IStateDataProvider<TestStateData>>();
        var data = await (service?.Get(null!) ?? Task.FromResult<TestStateData>(null!));

        // assert
        Assert.NotNull(service);
        Assert.NotNull(data);
        Assert.Equal("My data", data.Value);
    }
}
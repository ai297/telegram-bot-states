using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Extensions;

namespace Telegram.Bot.States.Tests.StateConfigurations;

public class DefaultState
{
    [Fact]
    public async Task DefaultActionShouldBeExecuted()
    {
        // arrange
        var services = ArrangeHelper.CreateServiceCollection();

        var step = Substitute.For<IStateAction<StateContext>>();
        step.Configure()
            .Execute(Arg.Any<StateContext>())
            .Returns(StateResults.Complete());

        new StatesConfiguration(services, [], "ru")
            .ConfigureDefaultState(state => state
                .WithDefaultAction((_, _) => step));

        var state = ChatState.Default(1);
        var update = ArrangeHelper.CreatePrivateMessageUpdate(1, "hello world");

        // act
        var serviceProvider = services.BuildServiceProvider();
        var defaultStateProcessor = serviceProvider.GetService<IStateProcessor>();
        var result = await (defaultStateProcessor?.Process(update, state) ?? Task.FromResult<ChatState>(null!));

        // assert
        Assert.NotNull(defaultStateProcessor);
        Assert.NotNull(result);
        Assert.Equal(state.StateName, result.StateName);

        await step.Received().Execute(Arg.Any<StateContext>());
    }
}

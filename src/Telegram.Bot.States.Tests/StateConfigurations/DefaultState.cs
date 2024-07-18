using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Extensions;
using Telegram.Bot.Types;

namespace Telegram.Bot.States.Tests.StateConfigurations;

public class DefaultState
{
    [Fact]
    public async Task DefaultActionShouldBeExecuted()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<ILogger<IStateProcessor>>());

        var step = Substitute.For<IStateAction>();
        step.Configure()
            .Execute(Arg.Any<StateContext>())
            .Returns(StateResults.Complete());

        new StatesConfiguration(services, [], "ru")
            .ConfigureDefaultState(state => state
                .WithDefaultAction((_, _) => step));

        var state = ChatState.Default(1);
        var update = CreatePrivateMessageUpdate(1, "hello world");

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

    private static ChatUpdate CreatePrivateMessageUpdate(long userId, string text)
    {
        var user = new User { Id = userId };
        var chat = new Chat { Id = userId };

        return new ChatUpdate(user, chat, new Update
        {
            Message = new Message
            {
                From = user,
                Chat = chat,
                MessageId = Random.Shared.Next(),
                Text = text
            }
        });
    }
}

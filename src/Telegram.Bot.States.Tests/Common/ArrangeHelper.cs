using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot.Types;

namespace Telegram.Bot.States.Tests;

public static class ArrangeHelper
{
    public static ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<ILogger<IStateProcessor>>());

        return services;
    }

    public static ChatUpdate CreatePrivateMessageUpdate(long userId, string text)
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

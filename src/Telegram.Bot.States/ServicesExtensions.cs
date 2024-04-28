using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Telegram.Bot.States;

public static class ServicesExtensions
{
    private static bool isCalled = false;

    public static StatesConfiguration AddBotStates(this IServiceCollection services,
        string token,
        ICollection<string>? commandLanguages = null,
        string? defaultLanguageCode = null)
    {
        if (isCalled) throw new InvalidOperationException("Bot states already configured.");
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        isCalled = true;

        defaultLanguageCode ??= commandLanguages != null && commandLanguages.Count > 0 ? commandLanguages.First() : "en";
        commandLanguages = commandLanguages == null || commandLanguages.Count == 0 ? [ defaultLanguageCode ] : commandLanguages;

        if (!commandLanguages.Contains(defaultLanguageCode))
            commandLanguages.Add(defaultLanguageCode);

        services.AddHttpClient("TgHttpClient").AddTypedClient<ITelegramBotClient>(
            httpClient => new TelegramBotClient(token, httpClient));

        services.AddSingleton<IUpdateHandler, UpdateHandler>();
        services.AddSingleton<IUpdateProcessingQueue, UpdateProcessingQueue>();
        services.AddSingleton<IStateDataProvider<ChatUpdate>, DefaultStateDataProvider>();

        return new StatesConfiguration(services, commandLanguages, defaultLanguageCode);
    }
}

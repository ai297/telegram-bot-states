using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

public static class ServicesExtensions
{
    private static bool isCalled = false;

    public static StatesConfiguration AddBotStates(this IServiceCollection services,
        Action<BotConfiguration, IServiceProvider> configureBot,
        ICollection<string>? commandLanguages = null,
        string? defaultLanguageCode = null)
    {
        ThrowIfConfigured();
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureBot);

        var optionsBuilder = services.AddOptions<BotConfiguration>().Configure(configureBot);

        return GetStatesConfiguration(services, optionsBuilder, commandLanguages, defaultLanguageCode);
    }

    public static StatesConfiguration AddBotStates(this IServiceCollection services,
        string botConfigurationSectionName,
        ICollection<string>? commandLanguages = null,
        string? defaultLanguageCode = null)
    {
        ThrowIfConfigured();
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(botConfigurationSectionName);

        var optionsBuilder = services.AddOptions<BotConfiguration>().BindConfiguration(botConfigurationSectionName);

        return GetStatesConfiguration(services, optionsBuilder, commandLanguages, defaultLanguageCode);
    }

    private static StatesConfiguration GetStatesConfiguration(IServiceCollection services,
        OptionsBuilder<BotConfiguration> optionsBuilder,
        ICollection<string>? commandLanguages = null,
        string? defaultLanguageCode = null)
    {
        isCalled = true;
        defaultLanguageCode ??= commandLanguages != null && commandLanguages.Count > 0 ? commandLanguages.First() : "en";
        commandLanguages = commandLanguages == null || commandLanguages.Count == 0 ? [ defaultLanguageCode ] : commandLanguages;

        if (!commandLanguages.Contains(defaultLanguageCode))
            commandLanguages.Add(defaultLanguageCode);

        optionsBuilder
            .Validate(config => !string.IsNullOrWhiteSpace(config.Token)
                && !string.IsNullOrWhiteSpace(config.HostAddress)
                && !string.IsNullOrWhiteSpace(config.WebHookPath))
            .ValidateOnStart();

        services.AddHttpClient("TgHttpClient").AddTypedClient<ITelegramBotClient>(
            (httpClient, serviceProvider) => new TelegramBotClient(
                serviceProvider.GetRequiredService<IOptions<BotConfiguration>>().Value.Token,
                httpClient));

        var statesConfiguration = new StatesConfiguration(services, commandLanguages, defaultLanguageCode);
        var getMenuButton = new Lazy<Func<string, MenuButton>?>(() => statesConfiguration.MenuButtonFactory);

        services.AddSingleton<IUpdateHandler, UpdateHandler>();
        services.AddSingleton<IUpdateProcessingQueue, UpdateProcessingQueue>();
        services.AddTransient<IBotSetupService>(sp => new BotSetupService(
            sp.GetRequiredService<ITelegramBotClient>(),
            sp.GetService<ICommandDescriptions>(),
            commandLanguages,
            defaultLanguageCode,
            sp.GetRequiredService<ILogger<BotSetupService>>(),
            getMenuButton));

        return statesConfiguration;
    }

    private static void ThrowIfConfigured()
    {
        if (isCalled) throw new InvalidOperationException("Bot states already configured.");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

internal class BotSetupService(ITelegramBotClient botClient,
    ICommandDescriptions? globalCommandDescriptions,
    ICollection<string> commandLanguages,
    string defaultLanguage,
    ILogger<BotSetupService> logger,
    Lazy<Func<string, MenuButton>?> getMenuButton) : IBotSetupService
{
    public async Task Setup()
    {
        if (getMenuButton.Value != null)
        {
            await botClient.SetChatMenuButtonAsync(menuButton: getMenuButton.Value(defaultLanguage));
            logger.LogInformation("Main menu button has been set.");
        }

        var hasMultiLanguageSupport = commandLanguages.Count > 1;

        await botClient.DeleteMyCommandsAsync();
        if (hasMultiLanguageSupport) await Task.WhenAll(
            commandLanguages.Select(language => botClient.DeleteMyCommandsAsync(languageCode: language)));

        logger.LogInformation("Previous global commads have been deleted.");

        if (globalCommandDescriptions == null || globalCommandDescriptions.Count == 0)
            return;

        var globalCommands = globalCommandDescriptions
            .Where(c => c.WithoutCondition)
            .GroupBy(c => c.LanguageCode, StringComparer.OrdinalIgnoreCase);

        foreach (var group in globalCommands)
            await Task.WhenAll(SetCommands(group, hasMultiLanguageSupport));

        logger.LogInformation("Global commands without special conditions have been set.");
    }

    private IEnumerable<Task> SetCommands(IGrouping<string, CommandDescription> commandsGroup, bool hasMultiLanguageSupport)
    {
        var commands = commandsGroup
            .Select(cd => new BotCommand { Command = cd.Command, Description = cd.Description })
            .ToArray();

        if (string.Equals(commandsGroup.Key, defaultLanguage, StringComparison.OrdinalIgnoreCase))
            yield return botClient.SetMyCommandsAsync(commands);

        if (hasMultiLanguageSupport)
            yield return botClient.SetMyCommandsAsync(commands, languageCode: commandsGroup.Key);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.States;

internal class StateSetupService(ITelegramBotClient botClient,
    IReadOnlyCollection<CommandDescription>? commandDescriptions,
    IReadOnlyCollection<string> allLanguageCodes,
    string defaultLanguageCode,
    ILogger<StateSetupService> logger,
    Func<string, MenuButton>? getMenuButton = null)
    : IStateSetupService
{
    public Task Setup(ChatState chatState, ChatUpdate update)
    {
        var menuButton = getMenuButton != null
            ? getMenuButton(update.User.LanguageCode ?? defaultLanguageCode)
            : new MenuButtonDefault();

        if (menuButton.Type == MenuButtonType.WebApp)
            return botClient.SetChatMenuButtonAsync(update.User.Id, menuButton);

        var scope = BotCommandScope.Chat(chatState.ChatId);
        var hasMultiLanguageSupport = allLanguageCodes.Count > 1;

        if (commandDescriptions == null || commandDescriptions.Count == 0)
        {
            logger.LogDebug(
                "There are no commands descriptions for state '{stateName}'. Commands menu for chat " +
                "'{chatId}' will be removed...",
                chatState.StateName, chatState.ChatId);

            return hasMultiLanguageSupport
                ? Task.WhenAll(allLanguageCodes
                    .Select(language => botClient.DeleteMyCommandsAsync(scope, language))
                    .Append(botClient.DeleteMyCommandsAsync(scope))
                    .Append(botClient.SetChatMenuButtonAsync(chatState.ChatId, menuButton)))
                : Task.WhenAll(
                    botClient.DeleteMyCommandsAsync(scope),
                    botClient.SetChatMenuButtonAsync(chatState.ChatId, menuButton));
        }

        logger.LogDebug(
            "Commands menu for chat '{chatId}' will be updated for state '{stateName}'...",
            chatState.ChatId, chatState.StateName);

        var groupedCommands = commandDescriptions
            .Where(c => c.IsApplicable(update, chatState))
            .GroupBy(c => c.LanguageCode, StringComparer.OrdinalIgnoreCase);

        return Task.WhenAll(groupedCommands
            .SelectMany(group => SetCommands(group, scope, hasMultiLanguageSupport))
            .Append(botClient.SetChatMenuButtonAsync(chatState.ChatId, menuButton)));
    }

    private IEnumerable<Task> SetCommands(IGrouping<string, CommandDescription> commandsGroup,
        BotCommandScope scope, bool hasMultiLanguageSupport)
    {
        var commands = commandsGroup
            .Select(cd => new BotCommand { Command = cd.Command, Description = cd.Description })
            .ToArray();

        if (string.Equals(commandsGroup.Key, defaultLanguageCode, StringComparison.OrdinalIgnoreCase))
            yield return botClient.SetMyCommandsAsync(commands, scope);

        if (hasMultiLanguageSupport)
            yield return botClient.SetMyCommandsAsync(commands, scope, commandsGroup.Key);
    }
}

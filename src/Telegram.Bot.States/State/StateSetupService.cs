using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

internal class StateSetupService(ITelegramBotClient botClient,
    ICommandDescriptions? globalCommandDescriptions,
    ICommandDescriptions? stateCommandDescriptions,
    IReadOnlyCollection<string> allLanguageCodes,
    string defaultLanguageCode,
    ILogger<StateSetupService> logger,
    Func<string, MenuButton>? getMenuButton = null)
    : IStateSetupService
{
    public async Task Setup(ChatState chatState, ChatUpdate update)
    {
        var hasNoCommands = !(globalCommandDescriptions != null && globalCommandDescriptions.Count > 0)
                         && !(stateCommandDescriptions != null && stateCommandDescriptions.Count > 0);

        if (getMenuButton != null)
        {
            logger.LogDebug(
                "Menu button for chat '{chatId}' will be updated for state '{stateName}'...",
                chatState.ChatId, chatState.StateName);

            await botClient.SetChatMenuButtonAsync(chatState.ChatId, getMenuButton(update.User.LanguageCode ?? defaultLanguageCode));
        }

        var scope = BotCommandScope.Chat(chatState.ChatId);
        var hasMultiLanguageSupport = allLanguageCodes.Count > 1;

        if (hasNoCommands)
        {
            logger.LogDebug(
                "There are no commands descriptions for state '{stateName}'. Commands menu for chat " +
                "'{chatId}' will be removed...",
                chatState.StateName, chatState.ChatId);

            await botClient.DeleteMyCommandsAsync(scope);

            foreach (var languageCode in allLanguageCodes)
            {
                await botClient.DeleteMyCommandsAsync(scope, languageCode);
            }

            return;
        }

        logger.LogDebug(
            "Commands menu for chat '{chatId}' will be updated for state '{stateName}'...",
            chatState.ChatId, chatState.StateName);

        var applicableCommands = (stateCommandDescriptions ?? Enumerable.Empty<CommandDescription>())
            .Concat(globalCommandDescriptions ?? Enumerable.Empty<CommandDescription>())
            .GroupBy(cd => cd.Command, (_, group) => group.FirstOrDefault(c => c.IsApplicable(update, chatState)))
            .Where(c => c != null)
            .GroupBy(c => c!.LanguageCode, StringComparer.OrdinalIgnoreCase);

        foreach (var group in applicableCommands)
        {
            await Task.WhenAll(SetCommands(group!, scope, hasMultiLanguageSupport));
        }
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

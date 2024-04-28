using System;

namespace Telegram.Bot.States;

internal struct CommandDescription
{
    private readonly Func<ChatUpdate, ChatState, bool> commandCondition;

    public readonly string Command;
    public readonly string LanguageCode;
    public readonly string Description;

    public CommandDescription(string command, string languageCode, string description,
        Func<ChatUpdate, ChatState, bool> commandCondition)
    {
        Command = command;
        LanguageCode = languageCode;
        Description = description;
        this.commandCondition = commandCondition;
    }

    public bool IsApplicable(ChatUpdate update, ChatState state) => commandCondition(update, state);
}

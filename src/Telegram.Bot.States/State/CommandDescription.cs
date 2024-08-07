using System;

namespace Telegram.Bot.States;

public sealed class CommandDescription(string command, string languageCode, string description,
    Func<ChatUpdate, ChatState, bool>? commandCondition = null)
{
    public readonly string Command = command;
    public readonly string LanguageCode = languageCode;
    public readonly string Description = description;
    public readonly bool WithoutCondition = commandCondition is null;

    public bool IsApplicable(ChatUpdate update, ChatState state)
        => WithoutCondition || commandCondition!(update, state);
}

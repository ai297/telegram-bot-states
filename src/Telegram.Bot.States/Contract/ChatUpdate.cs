using System;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.States;

public class ChatUpdate(User user, Chat chat, Update update)
{
    private bool? _isCommand;
    private string? _command;
    private string? _commandData;
    private ChatId? _chatId;

    public User User { get; } = user;
    public Chat Chat { get; } = chat;
    public Update Update { get; } = update;
    public ChatId ChatId => _chatId ??= Chat;

    public string? MessageText => Update.Message?.Text
        ?? Update.CallbackQuery?.Message?.Text
        ?? Update.EditedMessage?.Text
        ?? Update.ChannelPost?.Text
        ?? Update.EditedChannelPost?.Text;

    public int? MessageId => Update.Message?.MessageId
        ?? Update.CallbackQuery?.Message?.MessageId
        ?? Update.EditedMessage?.MessageId
        ?? Update.ChannelPost?.MessageId
        ?? Update.EditedChannelPost?.MessageId;

    public string CallbackData => Update.CallbackQuery?.Data ?? string.Empty;
    public bool IsPrivateChat => Chat.Type == ChatType.Private;
    public string CommandData => _commandData ??= GetCommandData();
    public string Command => _command ??= IsCommand
        ? MessageText!.Split(Constants.CommandSeparatorChars).First()[1..].ToLower()
        : "";

    public bool IsCommand => _isCommand
        ??= Update.Type == UpdateType.Message
        && MessageText!.StartsWith(Constants.CommandPrefix);

    private string GetCommandData()
    {
        if (!IsCommand)
            return string.Empty;

        var data = MessageText
            ?.Split(Constants.CommandDataSeparatorChar, 2, StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .FirstOrDefault()
            ?.Trim();

        return data ?? "";
    }
}

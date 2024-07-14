using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.States;

public static class UpdateExtensions
{
    public static User? GetUser(this Update update) => update.Type switch
    {
        UpdateType.Message => update.Message!.From,
        UpdateType.MyChatMember => update.MyChatMember!.From,
        UpdateType.InlineQuery => update.InlineQuery!.From,
        UpdateType.ChosenInlineResult => update.ChosenInlineResult!.From,
        UpdateType.CallbackQuery => update.CallbackQuery!.From,
        UpdateType.EditedMessage => update.EditedMessage!.From,
        UpdateType.ChannelPost => update.ChannelPost!.From,
        UpdateType.EditedChannelPost => update.EditedChannelPost!.From,
        UpdateType.ShippingQuery => update.ShippingQuery!.From,
        UpdateType.PreCheckoutQuery => update.PreCheckoutQuery!.From,
        UpdateType.ChatMember => update.ChatMember!.From,
        UpdateType.ChatJoinRequest => update.ChatJoinRequest!.From,
        _ => null
    };

    public static Chat? GetChat(this Update update) => update.Type switch
    {
        UpdateType.Message => update.Message!.Chat,
        UpdateType.CallbackQuery => update.CallbackQuery!.Message?.Chat,
        UpdateType.EditedMessage => update.EditedMessage!.Chat,
        UpdateType.MyChatMember => update.MyChatMember!.Chat,
        UpdateType.ChatMember => update.ChatMember!.Chat,
        UpdateType.ChannelPost => update.ChannelPost!.Chat,
        UpdateType.EditedChannelPost => update.EditedChannelPost!.Chat,
        UpdateType.ChatJoinRequest => update.ChatJoinRequest!.Chat,
        _ => null
    };
}

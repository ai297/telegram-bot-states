using System;

namespace Telegram.Bot.States;

public struct StateContext<TData>(TData data, ChatUpdate update, ChatState state, Lazy<ITelegramBotClient> botClient)
{
    public readonly TData Data = data;
    public readonly ChatUpdate Update = update;
    public readonly ChatState State = state;
    public readonly ITelegramBotClient BotClient => botClient.Value;
}

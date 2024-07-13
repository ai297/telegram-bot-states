using System;

namespace Telegram.Bot.States;

public class StateContext<TData>(TData data, ChatUpdate update, ChatState state, Lazy<ITelegramBotClient> botClient)
    : StateContext(update, state, botClient)
{
    public readonly TData Data = data;
}

public class StateContext(ChatUpdate update, ChatState state, Lazy<ITelegramBotClient> botClient)
{
    public readonly ChatUpdate Update = update;
    public readonly ChatState State = state;
    public ITelegramBotClient BotClient => botClient.Value;
}

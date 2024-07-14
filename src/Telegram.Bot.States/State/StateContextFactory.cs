using System;
using System.Threading.Tasks;

namespace Telegram.Bot.States;

internal class StateContextFactory(Lazy<ITelegramBotClient> botClientLazy) : IStateContextFactory<StateContext>
{
    public Task<StateContext> Create(ChatUpdate chatUpdate, ChatState currentState)
        => Task.FromResult(new StateContext(chatUpdate, currentState, botClientLazy));
}

internal class StateContextFactory<TData>(
    IStateDataProvider<TData> dataProvider,
    Lazy<ITelegramBotClient> botClientLazy)
    : IStateContextFactory<StateContext<TData>>
{
    public Task<StateContext<TData>> Create(ChatUpdate chatUpdate, ChatState currentState)
        => dataProvider.Get(chatUpdate).ContinueWithMap(
            data => new StateContext<TData>(data, chatUpdate, currentState, botClientLazy));
}

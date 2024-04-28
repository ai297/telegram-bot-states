using System;

namespace Telegram.Bot.States;

internal sealed class StateCommandFactory<TData>(
    StateServiceFactory<IAsyncCommand<StateContext<TData>, IStateResult>> factory,
    Func<ChatUpdate, ChatState, bool> commanCondition)
{
    public bool IsApplicable(ChatUpdate update, ChatState state) => commanCondition(update, state);

    public IAsyncCommand<StateContext<TData>, IStateResult> Create(IServiceProvider serviceProvider, string stateName)
        => factory(serviceProvider, stateName);
}

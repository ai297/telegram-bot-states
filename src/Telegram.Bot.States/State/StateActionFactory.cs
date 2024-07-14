using System;

namespace Telegram.Bot.States;

public sealed class StateActionFactory<TCtx>(
    StateServiceFactory<IAsyncCommand<TCtx, IStateResult>> factory,
    Func<ChatUpdate, ChatState, bool>? commanCondition = null)
    where TCtx : StateContext
{
    public bool IsApplicable(ChatUpdate update, ChatState state)
        => commanCondition is null || commanCondition(update, state);

    public IAsyncCommand<StateContext, IStateResult> Create(IServiceProvider serviceProvider, string stateName)
        => (IAsyncCommand<StateContext, IStateResult>)factory(serviceProvider, stateName);
}

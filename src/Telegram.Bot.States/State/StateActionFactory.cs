using System;

namespace Telegram.Bot.States;

public sealed class StateActionFactory<TCtx>(
    StateServiceFactory<IAsyncCommand<TCtx, IStateResult>> factory,
    Func<ChatUpdate, ChatState, bool>? actionCondition = null)
    : IStateActionFactory
    where TCtx : StateContext
{
    public bool IsApplicable(ChatUpdate update, ChatState state)
        => actionCondition is null || actionCondition(update, state);

    public IAsyncCommand<StateContext, IStateResult> Create(IServiceProvider serviceProvider, string stateName)
        => (IAsyncCommand<StateContext, IStateResult>)factory(serviceProvider, stateName);
}

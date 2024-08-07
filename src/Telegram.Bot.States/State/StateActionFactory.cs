using System;

namespace Telegram.Bot.States;

public sealed class StateActionFactory<TCtx>(
    StateServiceFactory<IStateAction<TCtx>> factory,
    Func<ChatUpdate, ChatState, bool>? actionCondition = null)
    : IStateActionFactory<TCtx>
    where TCtx : StateContext
{
    public bool IsApplicable(ChatUpdate update, ChatState state)
        => actionCondition is null || actionCondition(update, state);

    public IStateAction<TCtx> Create(IServiceProvider serviceProvider, string stateName)
        => factory(serviceProvider, stateName);
}

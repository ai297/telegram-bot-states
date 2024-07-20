using System;
using System.Threading.Tasks;

namespace Telegram.Bot.States;

internal delegate Task<IStateResult> StateAction<in TCtx>(TCtx context);

internal class DelegateAction<TCtx>(StateAction<TCtx> actionDelegate)
    : IStateAction<TCtx> where TCtx : StateContext
{
    public Task<IStateResult> Execute(TCtx context) => actionDelegate(context);
}

internal class LazyDelegateAction<TCtx>(IServiceProvider serviceProvider,
    Func<IServiceProvider, StateAction<TCtx>> delegateFactory)
    : IStateAction<TCtx> where TCtx : StateContext
{
    private StateAction<TCtx>? action = null;

    public Task<IStateResult> Execute(TCtx context)
    {
        action ??= delegateFactory(serviceProvider);

        return action(context);
    }
}


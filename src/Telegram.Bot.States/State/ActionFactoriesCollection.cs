using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Telegram.Bot.States;

internal class ActionFactoriesCollection<TKey, TCtx>(
    Func<StateContext, TKey> keySelector,
    IDictionary<TKey, StateActionFactory<TCtx>> mainActionFactories,
    IActionFactoriesCollection<StateContext>? secondaryActionFactories = null)
    : ReadOnlyDictionary<TKey, StateActionFactory<TCtx>>(mainActionFactories),
    IActionFactoriesCollection<TCtx>
    where TKey : notnull
    where TCtx : StateContext
{
    public virtual IStateActionFactory<TCtx>? GetApplicableFactoryIfExists(StateContext context)
    {
        var key = keySelector(context);

        if (this.TryGetValue(key, out var actionFactory) && actionFactory.IsApplicable(context.Update, context.State))
            return actionFactory;

        return secondaryActionFactories?.GetApplicableFactoryIfExists(context);
    }

    public IActionFactoriesCollection<TCtx> Merge(IActionFactoriesCollection<StateContext>? actionFactories)
    {
        secondaryActionFactories = secondaryActionFactories != null
            ? secondaryActionFactories.Merge(actionFactories)
            : secondaryActionFactories = actionFactories;

        return this;
    }
}

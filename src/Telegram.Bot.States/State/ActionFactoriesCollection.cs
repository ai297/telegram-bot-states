using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Telegram.Bot.States;

internal class ActionFactoriesCollection<TKey, TCtx>(
    Func<StateContext, TKey> keySelector,
    IDictionary<TKey, StateActionFactory<TCtx>> mainActionFactories,
    IActionFactoriesCollection? secondaryActionFactories = null)
    : ReadOnlyDictionary<TKey, StateActionFactory<TCtx>>(mainActionFactories),
    IActionFactoriesCollection
    where TKey : notnull
    where TCtx : StateContext
{
    public virtual IStateActionFactory? GetApplicableFactoryIfExists(StateContext context)
    {
        var key = keySelector(context);

        if (this.TryGetValue(key, out var actionFactory) && actionFactory.IsApplicable(context.Update, context.State))
            return actionFactory;

        return secondaryActionFactories?.GetApplicableFactoryIfExists(context);
    }

    public IActionFactoriesCollection Merge(IActionFactoriesCollection? actionFactories)
    {
        secondaryActionFactories = secondaryActionFactories != null
            ? secondaryActionFactories.Merge(actionFactories)
            : secondaryActionFactories = actionFactories;

        return this;
    }
}

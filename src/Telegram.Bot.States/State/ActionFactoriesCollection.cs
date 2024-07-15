using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Telegram.Bot.States;

internal class ActionFactoriesCollection<TKey, TCtx>(
    Func<ChatUpdate, TKey> keySelector,
    IDictionary<TKey, StateActionFactory<TCtx>> mainActionFactories,
    IActionFactoriesCollection? secondaryActionFactories = null)
    : ReadOnlyDictionary<TKey, StateActionFactory<TCtx>>(mainActionFactories),
    IActionFactoriesCollection
    where TKey : notnull
    where TCtx : StateContext
{
    public virtual IStateActionFactory? GetApplicableFactoryIfExists(ChatUpdate update, ChatState state)
    {
        var key = keySelector(update);

        if (this.TryGetValue(key, out var actionFactory) && actionFactory.IsApplicable(update, state))
            return actionFactory;

        return secondaryActionFactories?.GetApplicableFactoryIfExists(update, state);
    }

    public IActionFactoriesCollection Merge(IActionFactoriesCollection? actionFactories)
    {
        secondaryActionFactories = secondaryActionFactories != null
            ? secondaryActionFactories.Merge(actionFactories)
            : secondaryActionFactories = actionFactories;

        return this;
    }
}

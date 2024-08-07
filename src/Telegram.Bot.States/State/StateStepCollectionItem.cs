using System;

namespace Telegram.Bot.States;

internal readonly struct StateStepCollectionItem<TCtx>(string key, Func<IServiceProvider, IStateAction<TCtx>> factory)
    where TCtx : StateContext
{
    public readonly string Key = key;
    public readonly Func<IServiceProvider, IStateAction<TCtx>> Factory = factory;
}

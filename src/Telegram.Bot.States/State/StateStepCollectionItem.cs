using System;

namespace Telegram.Bot.States;

internal readonly struct StateStepCollectionItem<TCtx>(string key,
    Func<IServiceProvider, IAsyncCommand<TCtx, IStateResult>> factory)
    where TCtx : StateContext
{
    public readonly string Key = key;
    public readonly Func<IServiceProvider, IAsyncCommand<TCtx, IStateResult>> Factory = factory;
}

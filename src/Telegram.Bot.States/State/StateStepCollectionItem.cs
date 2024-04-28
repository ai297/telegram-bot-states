using System;

namespace Telegram.Bot.States;

internal struct StateStepCollectionItem<TData>(string key,
    Func<IServiceProvider, IAsyncCommand<StateContext<TData>, IStateResult>> factory)
{
    public readonly string Key = key;
    public readonly Func<IServiceProvider, IAsyncCommand<StateContext<TData>, IStateResult>> Factory = factory;
}

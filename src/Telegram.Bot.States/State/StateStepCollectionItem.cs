using System;

namespace Telegram.Bot.States;

internal readonly struct StateStepCollectionItem(string key, Func<IServiceProvider, IStateAction<StateContext>> factory)
{
    public readonly string Key = key;
    public readonly Func<IServiceProvider, IStateAction<StateContext>> Factory = factory;
}

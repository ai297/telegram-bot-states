using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Telegram.Bot.States;

public sealed class CallbacksCollectionBuilder<TKey, TCtx, TAction>
    where TKey : notnull
    where TCtx : StateContext
    where TAction : IAsyncCommand<TCtx, IStateResult>
{
    private readonly string? stateName;
    private readonly IServiceCollection services;

    internal readonly Dictionary<TKey, StateActionFactory<TCtx>> Factories = [];

    internal CallbacksCollectionBuilder(
        IServiceCollection services,
        string? stateName = null)
    {
        this.stateName = stateName;
        this.services = services;
    }

    public CallbacksCollectionBuilder<TKey, TCtx, TAction> Add(TKey key,
        StateServiceFactory<TAction> actionFactory,
        Func<ChatUpdate, ChatState, bool>? actionCcondition = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(actionFactory);
        ThrowIfCallbackConfigured(key);

        Factories.Add(key, new((actionFactory as StateServiceFactory<IAsyncCommand<TCtx, IStateResult>>)!, actionCcondition));

        return this;
    }

    public CallbacksCollectionBuilder<TKey, TCtx, TAction> Add<T>(TKey key,
        Func<ChatUpdate, ChatState, bool>? actionCcondition = null,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        where T : class, TAction
    {
        services.TryAdd(new ServiceDescriptor(typeof(T), typeof(T), serviceLifetime));

        return Add(key, (sp, _) => sp.GetRequiredService<T>(), actionCcondition);
    }

    public CallbacksCollectionBuilder<TKey, TCtx, TAction> Add(TKey key, Delegate @delegate,
        Func<ChatUpdate, ChatState, bool>? actionCcondition = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(@delegate);
        ThrowIfCallbackConfigured(key);

        var delegateFactory = DelegateHelper
            .CreateDelegateFactory<IServiceProvider, Command<TCtx, Task<IStateResult>>>(
                @delegate, (provider, type) => provider.GetRequiredService(type));

        Factories.Add(key, new(
            (serviceProvider, _) => new AsyncDelegateCommand<TCtx, IStateResult>(delegateFactory(serviceProvider)),
            actionCcondition));

        return this;
    }

    private void ThrowIfCallbackConfigured(TKey key)
    {
        if (!Factories.ContainsKey(key)) return;

        var actionsScope = stateName != null ? $"state '{stateName}'" : "global scope";

        throw new ArgumentOutOfRangeException(nameof(key),
            $"Callback with key '{key}' already configured for {actionsScope}.");
    }
}

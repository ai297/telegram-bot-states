using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Telegram.Bot.States;

public sealed class CallbacksCollectionBuilder<TKey, TCtx>
    where TKey : notnull
    where TCtx : StateContext
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

    public CallbacksCollectionBuilder<TKey, TCtx> Add(TKey key,
        StateServiceFactory<IStateAction<TCtx>> actionFactory,
        Func<ChatUpdate, ChatState, bool>? actionCondition = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(actionFactory);
        ThrowIfCallbackConfigured(key);

        Factories.Add(key, new(actionFactory, actionCondition));

        return this;
    }

    public CallbacksCollectionBuilder<TKey, TCtx> Add<T>(TKey key,
        Func<ChatUpdate, ChatState, bool>? actionCondition = null,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        where T : class, IStateAction<TCtx>
    {
        services.TryAdd(new ServiceDescriptor(typeof(T), typeof(T), serviceLifetime));

        return Add(key, (sp, _) => sp.GetRequiredService<T>(), actionCondition);
    }

    public CallbacksCollectionBuilder<TKey, TCtx> Add(TKey key, Delegate @delegate,
        Func<ChatUpdate, ChatState, bool>? actionCondition = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(@delegate);
        ThrowIfCallbackConfigured(key);

        var delegateFactory = DelegateHelper.CreateDelegateFactory<IServiceProvider, StateAction<TCtx>>(
            @delegate, (provider, type) => provider.GetRequiredService(type));

        Factories.Add(key, new(
            (serviceProvider, _) => new DelegateAction<TCtx>(delegateFactory(serviceProvider)),
            actionCondition));

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

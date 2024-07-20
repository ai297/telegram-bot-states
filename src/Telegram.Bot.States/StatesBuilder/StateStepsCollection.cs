using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Telegram.Bot.States;

public sealed class StateStepsCollection<TCtx> : IEnumerable<StateStepCollectionItem> where TCtx : StateContext
{
    private readonly string stateName;
    private readonly IServiceCollection services;
    private readonly List<StateStepCollectionItem> steps = [];

    internal StateStepsCollection(string stateName,
        IServiceCollection services)
    {
        this.stateName = stateName;
        this.services = services;
    }

    public StateStepsCollection<TCtx> Add<TStep>(StateServiceFactory<TStep> stepFactory, string? stepKey = null)
        where TStep : IStateAction<TCtx>
    {
        ArgumentNullException.ThrowIfNull(stepFactory);

        var stateName = this.stateName;

        steps.Add(new StateStepCollectionItem(
            GetStepKey(stepKey, typeof(TStep)),
            sp => (IStateAction<StateContext>)stepFactory(sp, stateName)));

        return this;
    }

    public StateStepsCollection<TCtx> Add<TStep>(string? stepKey = null,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        where TStep : class, IStateAction<TCtx>
    {
        services.TryAdd(new ServiceDescriptor(typeof(TStep), typeof(TStep), serviceLifetime));
        steps.Add(new StateStepCollectionItem(
            GetStepKey(stepKey, typeof(TStep)),
            sp => (IStateAction<StateContext>)sp.GetRequiredService<TStep>()));

        return this;
    }

    public StateStepsCollection<TCtx> Add(Delegate @delegate, string? stepKey = null)
    {
        var delegateFactory = DelegateHelper.CreateDelegateFactory<IServiceProvider, StateAction<TCtx>>(
            @delegate, (provider, type) => provider.GetRequiredService(type));

        Func<IServiceProvider, IStateAction<TCtx>> stepFactory = serviceProvider =>
            new DelegateAction<TCtx>(delegateFactory(serviceProvider));

        steps.Add(new StateStepCollectionItem(
            GetStepKey(stepKey, @delegate.GetType()),
            sp => (IStateAction<StateContext>)stepFactory(sp)));

        return this;
    }

    private string GetStepKey(string? stepKey, Type stepType)
    {
        stepKey ??= $"{steps.Count}-{TypeHelper.GetShortName(stepType)}";

        if (steps.Any(s => s.Key == stepKey))
            throw new ArgumentOutOfRangeException($"Can't add more that one step with key '{stepKey}' for state '{stateName}'");

        return stepKey;
    }

    internal int Count => steps.Count;

    IEnumerator IEnumerable.GetEnumerator()
        => steps.GetEnumerator();

    IEnumerator<StateStepCollectionItem> IEnumerable<StateStepCollectionItem>.GetEnumerator()
        => steps.GetEnumerator();
}
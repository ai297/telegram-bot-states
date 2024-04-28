using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Telegram.Bot.States;

public sealed class StateStepsCollection<TData> : IEnumerable<StateStepCollectionItem<TData>>
{
    private readonly string stateName;
    private readonly IServiceCollection services;
    private readonly List<StateStepCollectionItem<TData>> steps = [];

    internal StateStepsCollection(string stateName,
        IServiceCollection services)
    {
        this.stateName = stateName;
        this.services = services;
    }

    public StateStepsCollection<TData> AddStep<TStep>(StateServiceFactory<TStep> stepFactory, string? stepKey = null)
        where TStep : IStateStep<TData>
    {
        ArgumentNullException.ThrowIfNull(stepFactory);

        var stateName = this.stateName;

        steps.Add(new StateStepCollectionItem<TData>(
            GetStepKey(stepKey, typeof(TStep)),
            sp => stepFactory(sp, stateName)));

        return this;
    }

    public StateStepsCollection<TData> AddStep<TStep>(string? stepKey = null)
        where TStep : class, IStateStep<TData>
    {
        services.TryAddTransient<TStep>();

        steps.Add(new StateStepCollectionItem<TData>(
            GetStepKey(stepKey, typeof(TStep)),
            sp => sp.GetRequiredService<TStep>()));

        return this;
    }

    public StateStepsCollection<TData> AddStep(Delegate @delegate, string? stepKey = null)
    {
        var delegateFactory = DelegateHelper
            .CreateDelegateFactory<IServiceProvider, Command<StateContext<TData>, Task<IStateResult>>>(
                @delegate, (provider, type) => provider.GetRequiredService(type));

        Func<IServiceProvider, IAsyncCommand<StateContext<TData>, IStateResult>> stepFactory = serviceProvider =>
            new AsyncDelegateCommand<StateContext<TData>, IStateResult>(delegateFactory(serviceProvider));

        steps.Add(new StateStepCollectionItem<TData>(GetStepKey(stepKey, @delegate.GetType()), stepFactory));

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

    IEnumerator<StateStepCollectionItem<TData>> IEnumerable<StateStepCollectionItem<TData>>.GetEnumerator()
        => steps.GetEnumerator();
}
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

public abstract class StateBuilderBase<TCtx> where TCtx : StateContext
{
    protected readonly string stateName;
    protected readonly ICollection<string> languageCodes;
    protected readonly bool isDefaultState;

    internal readonly IServiceCollection Services;

    private CommandsCollectionBuilder<TCtx>? commandsCollectionBuilder = null;
    internal CommandsCollectionBuilder<TCtx> CommandsCollectionBuilder => commandsCollectionBuilder
        ??= new CommandsCollectionBuilder<TCtx>(Services, languageCodes, stateName);

    private StateStepsCollection<TCtx>? stepsCollection = null;
    internal StateStepsCollection<TCtx> StepsCollection => stepsCollection
        ??= new StateStepsCollection<TCtx>(stateName, Services);

    private Func<string, MenuButton>? menuButtonFactory = null;
    internal Func<string, MenuButton>? MenuButtonFactory
    {
        get => menuButtonFactory;
        set => menuButtonFactory = menuButtonFactory != null
            ? throw new InvalidOperationException($"Menu button for state '{stateName}' has been already configured.")
            : value;
    }

    private StateServiceFactory<IAsyncCommand<TCtx, IStateResult>>? defaultActionFactory = null;
    internal StateServiceFactory<IAsyncCommand<TCtx, IStateResult>>? DefaultActionFactory
    {
        get => defaultActionFactory;
        set => defaultActionFactory = defaultActionFactory != null
            ? throw new InvalidOperationException($"Default action for state '{stateName}' has been already configured.")
            : value;
    }

    internal StateBuilderBase(string stateName,
        IServiceCollection services,
        ICollection<string> languageCodes,
        bool isDefaultState)
    {
        Services = services;
        this.stateName = stateName;
        this.languageCodes = languageCodes;
        this.isDefaultState = isDefaultState;
    }

    internal void BindProcessor()
    {
        var stateCommandFactories = commandsCollectionBuilder != null && commandsCollectionBuilder.Factories.Count > 0
            ? new CommandFactories<TCtx>(commandsCollectionBuilder.Factories)
            : null;

        Func<IServiceProvider, IStateActionsProvider> actionsProviderFactory =
            sp => new ActionsProvider<TCtx>(sp.GetService<ICommandFactories<StateContext>>(), stateCommandFactories,
                sp, stateName);

        var processorFactory = GetStateProcessorFactory(actionsProviderFactory);
        var stateKey = isDefaultState ? null : stateName.AsStateKey();
        Services.Add(new ServiceDescriptor(typeof(IStateProcessor), stateKey, processorFactory, ServiceLifetime.Scoped));
    }

    protected abstract Func<IServiceProvider, object?, IStateProcessor> GetStateProcessorFactory(
        Func<IServiceProvider, IStateActionsProvider> actionsProviderFactory);

    internal virtual void BindSetupService(string defaultLanguageCode)
    {
        var stateKey = isDefaultState ? null : stateName.AsStateKey();
        var allLanguageCodes = languageCodes.Append(defaultLanguageCode).Distinct().ToArray();
        var stateCommandDescriptions = commandsCollectionBuilder != null && commandsCollectionBuilder.Descriptions.Count > 0
            ? new CommandDescriptions(commandsCollectionBuilder.Descriptions)
            : null;

        Func<IServiceProvider, object?, IStateSetupService> setupServiceFactory =
            (serviceProvider, _) => new StateSetupService(
                serviceProvider.GetRequiredService<ITelegramBotClient>(),
                serviceProvider.GetService<ICommandDescriptions>(),
                stateCommandDescriptions,
                allLanguageCodes,
                defaultLanguageCode,
                serviceProvider.GetRequiredService<ILogger<StateSetupService>>(),
                menuButtonFactory);

        Services.Add(new ServiceDescriptor(typeof(IStateSetupService), stateKey, setupServiceFactory, ServiceLifetime.Scoped));
    }
}

public sealed class StateBuilder(string stateName,
    IServiceCollection services,
    ICollection<string> languageCodes,
    bool isDefaultState = false)
    : StateBuilderBase<StateContext>(stateName, services, languageCodes, isDefaultState)
{
    protected override Func<IServiceProvider, object?, IStateProcessor> GetStateProcessorFactory(
        Func<IServiceProvider, IStateActionsProvider> actionsProviderFactory)
    {
        var stateName = this.stateName;
        var stateSteps = StepsCollection.ToArray().AsReadOnly();
        var defaultActionFactory = DefaultActionFactory;

        return (serviceProvider, _) => new StateProcessor<StateContext>(
            new StateContextFactory(new Lazy<ITelegramBotClient>(() => serviceProvider.GetRequiredService<ITelegramBotClient>())),
            actionsProviderFactory(serviceProvider),
            new StepsCollection<StateContext>(serviceProvider, stateSteps),
            defaultActionFactory != null ? defaultActionFactory(serviceProvider, stateName) : null,
            serviceProvider.GetRequiredService<ILogger<IStateProcessor>>());
    }
}

public sealed class StateBuilder<TData>(string stateName,
    IServiceCollection services,
    ICollection<string> languageCodes,
    bool isDefaultState = false)
    : StateBuilderBase<StateContext<TData>>(stateName, services, languageCodes, isDefaultState)
{
    private StateServiceFactory<IStateDataProvider<TData>>? dataProviderFactory = null;
    internal StateServiceFactory<IStateDataProvider<TData>>? DataProviderFactory
    {
        get => dataProviderFactory;
        set => dataProviderFactory = dataProviderFactory != null
            ? throw new InvalidOperationException($"Data provider for state '{stateName}' has beern already configured.")
            : value;
    }

    protected override Func<IServiceProvider, object?, IStateProcessor> GetStateProcessorFactory(
        Func<IServiceProvider, IStateActionsProvider> actionsProviderFactory)
    {
        var stateName = this.stateName;
        var stateSteps = StepsCollection.ToArray().AsReadOnly();
        var defaultActionFactory = DefaultActionFactory;
        var dataProviderFactory = DataProviderFactory;

        return (serviceProvider, _) => new StateProcessor<StateContext<TData>>(
            new StateContextFactory<TData>(
                dataProviderFactory != null
                    ? dataProviderFactory(serviceProvider, stateName)
                    : serviceProvider.GetRequiredService<IStateDataProvider<TData>>(),
                new Lazy<ITelegramBotClient>(() => serviceProvider.GetRequiredService<ITelegramBotClient>())),
            actionsProviderFactory(serviceProvider),
            new StepsCollection<StateContext<TData>>(serviceProvider, stateSteps),
            defaultActionFactory != null ? defaultActionFactory(serviceProvider, stateName) : null,
            serviceProvider.GetRequiredService<ILogger<IStateProcessor>>());
    }
}

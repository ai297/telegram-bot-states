using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

public abstract class StateBuilderBase<TCtx> where TCtx : StateContext
{
    protected readonly ICollection<string> languageCodes;
    protected readonly bool isDefaultState;

    internal readonly IServiceCollection Services;
    internal readonly string StateName;

    private CommandsCollectionBuilder<TCtx>? commandsCollectionBuilder = null;
    internal CommandsCollectionBuilder<TCtx> CommandsCollectionBuilder => commandsCollectionBuilder
        ??= new CommandsCollectionBuilder<TCtx>(Services, languageCodes, StateName);

    private StateStepsCollection<TCtx>? stepsCollection = null;
    internal StateStepsCollection<TCtx> StepsCollection => stepsCollection
        ??= new StateStepsCollection<TCtx>(StateName, Services);

    private Func<string, MenuButton>? menuButtonFactory = null;
    internal Func<string, MenuButton>? MenuButtonFactory
    {
        get => menuButtonFactory;
        set => menuButtonFactory = menuButtonFactory != null
            ? throw new InvalidOperationException($"Menu button for state '{StateName}' has been already configured.")
            : value;
    }

    private StateServiceFactory<IAsyncCommand<TCtx, IStateResult>>? defaultActionFactory = null;
    internal StateServiceFactory<IAsyncCommand<TCtx, IStateResult>>? DefaultActionFactory
    {
        get => defaultActionFactory;
        set => defaultActionFactory = defaultActionFactory != null
            ? throw new InvalidOperationException($"Default action for state '{StateName}' has been already configured.")
            : value;
    }

    private IActionFactoriesCollection? callbackFactories = null;
    internal IActionFactoriesCollection? CallbackFactories
    {
        get => callbackFactories;
        set => callbackFactories = callbackFactories != null
            ? throw new InvalidOperationException($"Callback query actions for state '{StateName}' has been already configured.")
            : value;
    }

    internal StateBuilderBase(string stateName,
        IServiceCollection services,
        ICollection<string> languageCodes,
        bool isDefaultState)
    {
        Services = services;
        StateName = stateName;
        this.languageCodes = languageCodes;
        this.isDefaultState = isDefaultState;
    }

    internal void BindProcessor()
    {
        var stateName = StateName;
        var stateKey = isDefaultState ? null : stateName.AsStateKey();
        var callbackFactories = CallbackFactories;
        var commandFactories = commandsCollectionBuilder?.Factories != null && commandsCollectionBuilder.Factories.Count > 0
            ? new ActionFactoriesCollection<string, TCtx>(Constants.CommandKeySelector, commandsCollectionBuilder.Factories)
            : null;

        var processorFactory = GetStateProcessorFactory(ActionsProviderFactory);

        Services.Add(new ServiceDescriptor(typeof(IStateProcessor), stateKey, processorFactory, ServiceLifetime.Scoped));

        IStateActionsProvider ActionsProviderFactory(IServiceProvider sp) => new ActionsProvider(
            commandFactories?.Merge(sp.GetKeyedService<IActionFactoriesCollection>(Constants.GlobalCommandsServiceKey))
                ?? sp.GetKeyedService<IActionFactoriesCollection>(Constants.GlobalCommandsServiceKey),
            callbackFactories?.Merge(sp.GetKeyedService<IActionFactoriesCollection>(Constants.GlobalCallbackServiceKey))
                ?? sp.GetKeyedService<IActionFactoriesCollection>(Constants.GlobalCallbackServiceKey),
            sp, stateName);
    }

    protected abstract Func<IServiceProvider, object?, IStateProcessor> GetStateProcessorFactory(
        Func<IServiceProvider, IStateActionsProvider> actionsProviderFactory);

    internal virtual void BindSetupService(string defaultLanguageCode)
    {
        var stateKey = isDefaultState ? null : StateName.AsStateKey();
        var allLanguageCodes = languageCodes.Append(defaultLanguageCode).Distinct().ToArray();
        var stateCommandDescriptions = commandsCollectionBuilder != null && commandsCollectionBuilder.Descriptions.Count > 0
            ? new CommandDescriptions(commandsCollectionBuilder.Descriptions)
            : null;

        Services.Add(new ServiceDescriptor(typeof(IStateSetupService), stateKey, SetupServiceFactory, ServiceLifetime.Scoped));

        IStateSetupService SetupServiceFactory(IServiceProvider serviceProvider, object? _) => new StateSetupService(
            serviceProvider.GetRequiredService<ITelegramBotClient>(),
            serviceProvider.GetService<ICommandDescriptions>(),
            stateCommandDescriptions,
            allLanguageCodes,
            defaultLanguageCode,
            serviceProvider.GetRequiredService<ILogger<StateSetupService>>(),
            menuButtonFactory);
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
        var stateName = StateName;
        var stateSteps = StepsCollection.ToArray().AsReadOnly();
        var defaultActionFactory = DefaultActionFactory;

        return (serviceProvider, _) => new StateProcessor<StateContext>(
            new StateContextFactory(new Lazy<ITelegramBotClient>(() => serviceProvider.GetRequiredService<ITelegramBotClient>())),
            actionsProviderFactory(serviceProvider),
            new StepsCollection<StateContext>(serviceProvider, stateSteps),
            defaultActionFactory == null
                ? serviceProvider.GetService<IAsyncCommand<StateContext, IStateResult>>()
                : defaultActionFactory(serviceProvider, stateName),
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
            ? throw new InvalidOperationException($"Data provider for state '{StateName}' has beern already configured.")
            : value;
    }

    protected override Func<IServiceProvider, object?, IStateProcessor> GetStateProcessorFactory(
        Func<IServiceProvider, IStateActionsProvider> actionsProviderFactory)
    {
        var stateName = StateName;
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
            defaultActionFactory != null
                ? (IAsyncCommand<StateContext, IStateResult>)defaultActionFactory(serviceProvider, stateName)
                : serviceProvider.GetService<IAsyncCommand<StateContext, IStateResult>>(),
            serviceProvider.GetRequiredService<ILogger<IStateProcessor>>());
    }
}

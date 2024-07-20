using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    private StateServiceFactory<IStateAction<TCtx>>? defaultActionFactory = null;
    internal StateServiceFactory<IStateAction<TCtx>>? DefaultActionFactory
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
    #region Methods

    public StateBuilder WithCommands(Action<CommandsCollectionBuilder<StateContext>> configureCommands)
        => StateBuilderMethods.WithCommands(this, configureCommands);

    public StateBuilder WithCallbacks<TKey>(Func<StateContext, TKey> callbackKeySelector,
        Action<CallbacksCollectionBuilder<TKey, StateContext>> configureCallbacks)
        where TKey : notnull
        => StateBuilderMethods.WithCallbacks(this, callbackKeySelector, configureCallbacks);

    public StateBuilder WithSteps(Action<StateStepsCollection<StateContext>> configureSteps)
        => StateBuilderMethods.WithSteps(this, configureSteps);

    public StateBuilder WithWebAppButton(Func<string, (string text, string url)> getLocalizedButton)
        => StateBuilderMethods.WithWebAppButton<StateBuilder, StateContext>(this, getLocalizedButton);

    public StateBuilder WithCommandsMenuButton()
        => StateBuilderMethods.WithCommandsMenuButton<StateBuilder, StateContext>(this);

    public StateBuilder WithDefaultAction(StateServiceFactory<IStateAction<StateContext>> factory)
        => StateBuilderMethods.WithDefaultAction(this, factory);

    public StateBuilder WithDefaultAction(Delegate @delegate)
        => StateBuilderMethods.WithDefaultAction<StateBuilder, StateContext>(this, @delegate);

    public StateBuilder WithDefaultAction<T>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where T : class, IStateAction<StateContext>
        => StateBuilderMethods.WithDefaultAction<StateBuilder, StateContext, T>(this, serviceLifetime);

    #endregion

    protected override Func<IServiceProvider, object?, IStateProcessor> GetStateProcessorFactory(
        Func<IServiceProvider, IStateActionsProvider> actionsProviderFactory)
    {
        var stateName = StateName;
        var stateSteps = StepsCollection.ToArray().AsReadOnly();
        var defaultActionFactory = DefaultActionFactory;

        return (serviceProvider, _) => new StateProcessor<StateContext>(
            new StateContextFactory(new Lazy<ITelegramBotClient>(() => serviceProvider.GetRequiredService<ITelegramBotClient>())),
            actionsProviderFactory(serviceProvider),
            new StepsCollection(serviceProvider, stateSteps),
            defaultActionFactory == null
                ? serviceProvider.GetService<IStateAction<StateContext>>()
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

    #region Methods

    public StateBuilder<TData> WithCommands(Action<CommandsCollectionBuilder<StateContext<TData>>> configureCommands)
        => StateBuilderMethods.WithCommands(this, configureCommands);

    public StateBuilder<TData> WithCallbacks<TKey>(Func<StateContext, TKey> callbackKeySelector,
        Action<CallbacksCollectionBuilder<TKey, StateContext<TData>>> configureCallbacks)
        where TKey : notnull
        => StateBuilderMethods.WithCallbacks(this, callbackKeySelector, configureCallbacks);

    public StateBuilder<TData> WithSteps(Action<StateStepsCollection<StateContext<TData>>> configureSteps)
        => StateBuilderMethods.WithSteps(this, configureSteps);

    public StateBuilder<TData> WithWebAppButton(Func<string, (string text, string url)> getLocalizedButton)
        => StateBuilderMethods.WithWebAppButton<StateBuilder<TData>, StateContext<TData>>(this, getLocalizedButton);

    public StateBuilder<TData> WithCommandsMenuButton()
        => StateBuilderMethods.WithCommandsMenuButton<StateBuilder<TData>, StateContext<TData>>(this);

    public StateBuilder<TData> WithDefaultAction(StateServiceFactory<IStateAction<StateContext<TData>>> factory)
        => StateBuilderMethods.WithDefaultAction(this, factory);

    public StateBuilder<TData> WithDefaultAction(Delegate @delegate)
        => StateBuilderMethods.WithDefaultAction<StateBuilder<TData>, StateContext<TData>>(this, @delegate);

    public StateBuilder<TData> WithDefaultAction<T>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where T : class, IStateAction<StateContext<TData>>
        => StateBuilderMethods.WithDefaultAction<StateBuilder<TData>, StateContext<TData>, T>(this, serviceLifetime);

    public StateBuilder<TData> WithDataProvider(StateServiceFactory<IStateDataProvider<TData>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        DataProviderFactory = factory;

        return this;
    }

    public StateBuilder<TData> WithDataProvider<T>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where T : class, IStateDataProvider<TData>
    {
        DataProviderFactory = (sp, _) => sp.GetRequiredService<T>();
        Services.TryAdd(new ServiceDescriptor(typeof(T), typeof(T), serviceLifetime));

        return this;
    }

    /// <summary>
    /// Your <paramref name="@delegate" /> can receive <seealso cref="Telegram.Bot.States.ChatUpdate" />
    /// as parameter and  must return Task&lt;<typeparamref name="TData" />&rt; as result.
    /// </summary>
    public StateBuilder<TData> WithDataProvider(Delegate @delegate)
    {
        ArgumentNullException.ThrowIfNull(@delegate);

        var delegateFactory = DelegateHelper
            .CreateDelegateFactory<IServiceProvider,Func<ChatUpdate, Task<TData>>>(
                @delegate, StateBuilderMethods.GetServiceExpr);

        DataProviderFactory = (sp, _) => new DelegateDataProvider<TData>(delegateFactory(sp));

        return this;
    }

    #endregion

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
            new StepsCollection(serviceProvider, stateSteps),
            defaultActionFactory != null
                ? (IStateAction<StateContext>)defaultActionFactory(serviceProvider, stateName)
                : serviceProvider.GetService<IStateAction<StateContext>>(),
            serviceProvider.GetRequiredService<ILogger<IStateProcessor>>());
    }
}

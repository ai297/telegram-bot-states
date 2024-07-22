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
        set => menuButtonFactory = menuButtonFactory != null && value != null
            ? throw new InvalidOperationException($"Menu button for state '{StateName}' has been already configured.")
            : value;
    }

    private StateServiceFactory<IStateAction<TCtx>>? defaultActionFactory = null;
    internal StateServiceFactory<IStateAction<TCtx>>? DefaultActionFactory
    {
        get => defaultActionFactory;
        set => defaultActionFactory = defaultActionFactory != null && value != null
            ? throw new InvalidOperationException($"Default action for state '{StateName}' has been already configured.")
            : value;
    }

    private IActionFactoriesCollection<TCtx>? actionFactories = null;
    internal IActionFactoriesCollection<TCtx>? ActionFactories
    {
        get => actionFactories;
        set => actionFactories = actionFactories != null && value != null
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

        var stateSteps = StepsCollection.ToArray().AsReadOnly();
        var actionFactories = ActionFactories;
        var commandFactories = commandsCollectionBuilder?.Factories != null && commandsCollectionBuilder.Factories.Count > 0
            ? new ActionFactoriesCollection<string, TCtx>(Constants.CommandKeySelector, commandsCollectionBuilder.Factories)
            : null;

        Services.Add(new ServiceDescriptor(typeof(IStateActionsProvider<TCtx>), stateKey,
            (sp, _) => new ActionsProvider<TCtx>(stateName,
                commandFactories?.Merge(sp.GetKeyedService<IActionFactoriesCollection<StateContext>>(Constants.GlobalCommandsServiceKey))
                    ?? sp.GetKeyedService<IActionFactoriesCollection<StateContext>>(Constants.GlobalCommandsServiceKey),
                actionFactories?.Merge(sp.GetKeyedService<IActionFactoriesCollection<StateContext>>(Constants.GlobalCallbackServiceKey))
                    ?? sp.GetKeyedService<IActionFactoriesCollection<StateContext>>(Constants.GlobalCallbackServiceKey)),
            ServiceLifetime.Singleton));

        Services.Add(new ServiceDescriptor(typeof(IStateStepsCollection), stateKey,
            (sp, _) => new StepsCollection(stateSteps),
            ServiceLifetime.Singleton));

        Func<IServiceProvider, IStateActionsProvider<TCtx>> actionsProviderFactory = isDefaultState
            ? sp => sp.GetRequiredService<IStateActionsProvider<TCtx>>()
            : sp => sp.GetRequiredKeyedService<IStateActionsProvider<TCtx>>(stateKey);

        Func<IServiceProvider, IStateStepsCollection> stepsCollectionFactory = isDefaultState
            ? sp => sp.GetRequiredService<IStateStepsCollection>()
            : sp => sp.GetRequiredKeyedService<IStateStepsCollection>(stateKey);

        Func<IServiceProvider, IStateAction<StateContext>?> defaultActionFactory = DefaultActionFactory == null
            ? sp => sp.GetService<IStateAction<StateContext>>()
            : sp =>  (IStateAction<StateContext>)DefaultActionFactory(sp, stateName);

        var contextFactory = GetContextFactory();

        Services.Add(new ServiceDescriptor(typeof(IStateProcessor), stateKey,
            (serviceProvider, _) => new StateProcessor<TCtx>(
                contextFactory(serviceProvider),
                actionsProviderFactory(serviceProvider),
                stepsCollectionFactory(serviceProvider),
                defaultActionFactory(serviceProvider),
                serviceProvider,
                serviceProvider.GetRequiredService<ILogger<IStateProcessor>>()),
            ServiceLifetime.Scoped));
    }

    protected abstract Func<IServiceProvider, IStateContextFactory<TCtx>> GetContextFactory();

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

    public StateBuilder WithActions<TKey>(Func<StateContext, TKey> actionKeySelector,
        Action<ActionsCollectionBuilder<TKey, StateContext>> configureActions)
        where TKey : notnull
        => StateBuilderMethods.WithActions(this, actionKeySelector, configureActions);

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

    protected override Func<IServiceProvider, IStateContextFactory<StateContext>> GetContextFactory()
        => sp => new StateContextFactory(new Lazy<ITelegramBotClient>(() => sp.GetRequiredService<ITelegramBotClient>()));
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

    public StateBuilder<TData> WithActions<TKey>(Func<StateContext, TKey> actionKeySelector,
        Action<ActionsCollectionBuilder<TKey, StateContext<TData>>> configureActions)
        where TKey : notnull
        => StateBuilderMethods.WithActions(this, actionKeySelector, configureActions);

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

    protected override Func<IServiceProvider, IStateContextFactory<StateContext<TData>>> GetContextFactory()
    {
        var stateName = StateName;
        var dataProviderFactory = DataProviderFactory;

        return dataProviderFactory != null
            ? sp => new StateContextFactory<TData>(dataProviderFactory(sp, stateName),
                new Lazy<ITelegramBotClient>(() => sp.GetRequiredService<ITelegramBotClient>()))
            : sp => new StateContextFactory<TData>(sp.GetRequiredService<IStateDataProvider<TData>>(),
                new Lazy<ITelegramBotClient>(() => sp.GetRequiredService<ITelegramBotClient>()));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

public sealed class StateBuilder<TData>
{
    private static readonly Expression<Func<IServiceProvider, Type, object>> getServiceExpr
        = (serviceProvider, type) => serviceProvider.GetRequiredService(type);

    private readonly string stateName;
    private readonly IServiceCollection services;
    private readonly ICollection<string> languageCodes;
    private readonly bool isDefaultState;

    private StateActionsCollection<TData>? actionsCollection = null;
    private Func<IServiceProvider, IStateDataProvider<TData>>? dataProvierFactory = null;
    private List<StateStepCollectionItem<TData>>? stepsCollection = null;
    private Func<IServiceProvider, IAsyncCommand<StateContext<TData>, IStateResult>?>? defaultActionFactory = null;
    private Func<string, MenuButton>? menuButtonFactory = null;

    internal StateBuilder(string stateName,
        IServiceCollection services,
        ICollection<string> languageCodes,
        bool isDefaultState = false)
    {
        this.stateName = stateName;
        this.services = services;
        this.languageCodes = languageCodes;
        this.isDefaultState = isDefaultState;
    }

    public StateBuilder<TData> WithActions(Action<StateActionsCollection<TData>> configureActions)
    {
        ArgumentNullException.ThrowIfNull(configureActions);

        actionsCollection ??= new StateActionsCollection<TData>(stateName, services, languageCodes);
        configureActions(actionsCollection);

        return this;
    }

    public StateBuilder<TData> WithSteps(Action<StateStepsCollection<TData>> configureSteps)
    {
        ArgumentNullException.ThrowIfNull(configureSteps);

        var stepsCofiguration = new StateStepsCollection<TData>(stateName, services);
        configureSteps(stepsCofiguration);

        stepsCollection ??= new(stepsCofiguration.Count);
        stepsCollection.AddRange(stepsCofiguration);

        return this;
    }

    #region Menu button
    public StateBuilder<TData> WithWebAppButton(Func<string, (string text, string url)> getLocalizedButton)
    {
        ArgumentNullException.ThrowIfNull(getLocalizedButton);

        if (menuButtonFactory != null) throw new InvalidOperationException(
            $"Menu button for state '{stateName}' has been already configured.");

        menuButtonFactory = languageCode => {
            var (text, url) = getLocalizedButton(languageCode);
            return new MenuButtonWebApp {
                Text = text,
                WebApp = new WebAppInfo {
                    Url = url,
                },
            };
        };

        return this;
    }

    public StateBuilder<TData> WithCommandsMenuButton()
    {
        if (menuButtonFactory != null) throw new InvalidOperationException(
            $"Menu button for state '{stateName}' has been already configured.");

        menuButtonFactory = _ => new MenuButtonCommands();

        return this;
    }
    #endregion

    #region Custom data provider
    public StateBuilder<TData> WithDataProvider(
        StateServiceFactory<IStateDataProvider<TData>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var stateName = this.stateName;

        if (dataProvierFactory != null) throw new InvalidOperationException(
            $"Data provider for state '{stateName}' has beern already configured.");

        dataProvierFactory = sp => factory(sp, stateName);
        return this;
    }

    public StateBuilder<TData> WithDataProvider<T>()
        where T : class, IStateDataProvider<TData>
    {
        if (dataProvierFactory != null) throw new InvalidOperationException(
            $"Data provider for state '{stateName}' has been already configured.");

        services.TryAddScoped<T>();
        dataProvierFactory = sp => sp.GetRequiredService<T>();
        return this;
    }

    /// <summary>
    /// Your <paramref name="@delegate" /> can receive <seealso cref="Telegram.Bot.States.ChatUpdate" />
    /// as parameter and  must return ValueTask&lt;<typeparamref name="TData" />&rt; as result.
    /// </summary>
    public StateBuilder<TData> WithDataProvider(Delegate @delegate)
    {
        var stateName = this.stateName;

        if (dataProvierFactory != null) throw new InvalidOperationException(
            $"Data provider for state '{stateName}' has been already configured.");

        var delegateFactory = DelegateHelper
            .CreateDelegateFactory<IServiceProvider,Func<ChatUpdate, Task<TData>>>(
                @delegate, getServiceExpr);

        dataProvierFactory = sp => new DelegateDataProvider<TData>(delegateFactory(sp));
        return this;
    }
    #endregion

    #region Default Action
    public StateBuilder<TData> WithDefaultAction(
        StateServiceFactory<IStateStep<TData>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var stateName = this.stateName;

        if (defaultActionFactory != null) throw new InvalidOperationException(
            $"Default action for state '{stateName}' has been already configured.");

        defaultActionFactory = sp => factory(sp, stateName);

        return this;
    }

    public StateBuilder<TData> WithDefaultAction<T>() where T : class, IStateStep<TData>
    {
        if (defaultActionFactory != null) throw new InvalidOperationException(
            $"Default action for state '{stateName}' has already configured.");

        services.TryAddTransient<T>();
        defaultActionFactory = sp => sp.GetRequiredService<T>();

        return this;
    }

    public StateBuilder<TData> WithDefaultAction(Delegate @delegate)
    {
        if (defaultActionFactory != null) throw new InvalidOperationException(
            $"Default action for state '{stateName}' has already configured.");

        var delegateFactory = DelegateHelper
            .CreateDelegateFactory<IServiceProvider, Command<StateContext<TData>, Task<IStateResult>>>(
                @delegate, getServiceExpr);

        defaultActionFactory = sp => new AsyncDelegateCommandLazy<StateContext<TData>, IStateResult>(sp, delegateFactory);

        return this;
    }
    #endregion

    #region Bindings
    internal void BindProcessor()
    {
        Func<IServiceProvider, IStateActionsProvider<TData>> actionsProviderFactory = actionsCollection != null
            ? sp => new ActionsProvider<TData>(actionsCollection, sp, stateName)
            : sp => new NoActionsProvider<TData>();

        var dataProviderFactory = this.dataProvierFactory
            ?? (sp => sp.GetRequiredService<IStateDataProvider<TData>>());

        var defaultActionFactory = this.defaultActionFactory
            ?? (_ => null);

        var stateSteps = stepsCollection ?? [];

        Func<IServiceProvider, object?, IStateProcessor> processorFactory =
            (serviceProvider, _) => new StateProcessor<TData>(
                dataProviderFactory(serviceProvider),
                actionsProviderFactory(serviceProvider),
                new StepsCollection<TData>(serviceProvider, stateSteps),
                defaultActionFactory(serviceProvider),
                serviceProvider);

        var stateKey = isDefaultState ? null : stateName.AsStateKey();
        services.Add(new ServiceDescriptor(typeof(IStateProcessor), stateKey, processorFactory, ServiceLifetime.Scoped));
    }

    internal void BindSetupService(string defaultLanguageCode)
    {
        var stateKey = isDefaultState ? null : stateName.AsStateKey();
        var commandDescriptions = actionsCollection?.CommandDescriptions;
        var allLanguageCodes = languageCodes.Append(defaultLanguageCode).Distinct().ToArray();

        Func<IServiceProvider, object?, IStateSetupService> setupServiceFactory =
            (serviceProvider, _) => new StateSetupService(
                serviceProvider.GetRequiredService<ITelegramBotClient>(),
                commandDescriptions,
                allLanguageCodes,
                defaultLanguageCode,
                serviceProvider.GetRequiredService<ILogger<StateSetupService>>(),
                menuButtonFactory);

        services.Add(new ServiceDescriptor(typeof(IStateSetupService), stateKey, setupServiceFactory, ServiceLifetime.Scoped));
    }
    #endregion
}

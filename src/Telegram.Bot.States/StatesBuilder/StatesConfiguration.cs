using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

public sealed class StatesConfiguration(IServiceCollection services,
    ICollection<string> commandLanguages,
    string defaultLanguageCode)
{
    private readonly IServiceCollection services = services;
    private readonly ICollection<string> commandLanguages = commandLanguages;
    private readonly string defaultLanguageCode = defaultLanguageCode;

    private CommandsCollectionBuilder<StateContext>? globalCommandsBuilder = null;

    private Func<string, MenuButton>? menuButtonFactory = null;
    internal Func<string, MenuButton>? MenuButtonFactory
    {
        get => menuButtonFactory;
        set => menuButtonFactory = menuButtonFactory != null
            ? throw new InvalidOperationException($"Main menu button has been already configured.")
            : value;
    }

    public StatesConfiguration ConfigureState(string name, Action<StateBuilder> configureState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configureState);
        ThrowIfStateConfigured(services, name);

        var builder = new StateBuilder(name, services, commandLanguages);
        configureState(builder);

        builder.BindProcessor();
        builder.BindSetupService(defaultLanguageCode);

        return this;
    }

    public StatesConfiguration ConfigureState<TData>(string name, Action<StateBuilder<TData>> configureState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configureState);
        ThrowIfStateConfigured(services, name);

        var builder = new StateBuilder<TData>(name, services, commandLanguages);
        configureState(builder);

        builder.BindProcessor();
        builder.BindSetupService(defaultLanguageCode);

        return this;
    }

    public StatesConfiguration ConfigureDefaultState(Action<StateBuilder> configureState)
    {
        ArgumentNullException.ThrowIfNull(configureState);
        ThrowIfDefaultStateConfigured(services);

        var builder = new StateBuilder(Constants.DefaultStateName, services, commandLanguages, isDefaultState: true);
        configureState(builder);

        builder.BindProcessor();
        builder.BindSetupService(defaultLanguageCode);

        return this;
    }

    public StatesConfiguration ConfigureDefaultState<TData>(Action<StateBuilder<TData>> configureState)
    {
        ArgumentNullException.ThrowIfNull(configureState);
        ThrowIfDefaultStateConfigured(services);

        var builder = new StateBuilder<TData>(Constants.DefaultStateName, services, commandLanguages, isDefaultState: true);
        configureState(builder);

        builder.BindProcessor();
        builder.BindSetupService(defaultLanguageCode);

        return this;
    }

    public StatesConfiguration ConfigureCommands(Action<CommandsCollectionBuilder<StateContext>> configureCommands)
    {
        ArgumentNullException.ThrowIfNull(configureCommands);

        globalCommandsBuilder ??= new CommandsCollectionBuilder<StateContext>(services, commandLanguages);
        configureCommands(globalCommandsBuilder);

        if (globalCommandsBuilder.Descriptions.Count > 0)
        {
            services.RemoveAll<ICommandDescriptions>();
            services.AddSingleton<ICommandDescriptions>(new CommandDescriptions(globalCommandsBuilder.Descriptions));
        }

        if (globalCommandsBuilder.Factories.Count > 0)
        {
            services.RemoveAllKeyed<IActionFactoriesCollection<StateContext>>(Constants.GlobalCommandsServiceKey);
            services.AddKeyedSingleton<IActionFactoriesCollection<StateContext>>(Constants.GlobalCommandsServiceKey,
                new ActionFactoriesCollection<string, StateContext>(Constants.CommandKeySelector, globalCommandsBuilder.Factories));
        }

        return this;
    }

    public StatesConfiguration ConfigureActions<TKey>(Func<StateContext, TKey> keySelector,
        Action<ActionsCollectionBuilder<TKey, StateContext>> configureActions)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(configureActions);
        ThrowIfCallbacksConfigured(services);

        var callbacksBuilder = new ActionsCollectionBuilder<TKey, StateContext>(services);
        configureActions(callbacksBuilder);

        services.AddKeyedSingleton<IActionFactoriesCollection<StateContext>>(Constants.GlobalCallbackServiceKey,
            new ActionFactoriesCollection<TKey, StateContext>(keySelector, callbacksBuilder.Factories));

        return this;
    }

    public StatesConfiguration ConfigureWebAppButton(Func<string, (string text, string url)> getLocalizedButton)
    {
        ArgumentNullException.ThrowIfNull(getLocalizedButton);

        MenuButtonFactory = languageCode => {
            var (text, url) = getLocalizedButton(languageCode);
            return new MenuButtonWebApp
            {
                Text = text,
                WebApp = new WebAppInfo { Url = url, },
            };
        };

        return this;
    }

    public StatesConfiguration ConfigureDefaultDataProvider<TData>(
        Func<IServiceProvider, IStateDataProvider<TData>> factory,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ThrowIfDataProviderAdded<TData>(services);
        services.TryAdd(new ServiceDescriptor(typeof(IStateDataProvider<TData>), factory, serviceLifetime));

        return this;
    }

    public StatesConfiguration ConfigureDefaultDataProvider<TData, TService>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TService : class, IStateDataProvider<TData>
    {
        ThrowIfDataProviderAdded<TData>(services);
        services.TryAdd(new ServiceDescriptor(typeof(IStateDataProvider<TData>), typeof(TService), serviceLifetime));

        return this;
    }

    /// <summary>
    /// Your <paramref name="@delegate" /> can receive <seealso cref="Telegram.Bot.States.ChatUpdate" />
    /// as parameter and  must return ValueTask&lt;<typeparamref name="TStateContext" />&rt; as result.
    /// </summary>
    public StatesConfiguration ConfigureDefaultDataProvider<TData>(Delegate @delegate,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var delegateFactory = DelegateHelper
            .CreateDelegateFactory<IServiceProvider, Func<ChatUpdate, Task<TData>>>(
                @delegate, StateBuilderMethods.GetServiceExpr);
        
        Func<IServiceProvider, IStateDataProvider<TData>> factory = serviceProvider =>
            new DelegateDataProvider<TData>(delegateFactory(serviceProvider));
        
        return ConfigureDefaultDataProvider(factory, serviceLifetime);
    }

    public StatesConfiguration ConfigureDefaultAction(Func<IServiceProvider, IStateAction<StateContext>> factory,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ThrowIfDefaultActionRegistered(services);

        services.TryAdd(new ServiceDescriptor(typeof(IStateAction<StateContext>), factory, serviceLifetime));

        return this;
    }

    public StatesConfiguration ConfigureDefaultAction(Delegate @delegate, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        ArgumentNullException.ThrowIfNull(@delegate);
        ThrowIfDefaultActionRegistered(services);

        var delegateFactory = DelegateHelper.CreateDelegateFactory<IServiceProvider, StateAction<StateContext>>(
            @delegate, StateBuilderMethods.GetServiceExpr);

        services.TryAdd(new ServiceDescriptor(typeof(IStateAction<StateContext>),
            sp => new LazyDelegateAction<StateContext>(sp, delegateFactory),
            serviceLifetime));

        return this;
    }

    public StatesConfiguration ConfigureDefaultAction<T>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        where T : class, IStateAction<StateContext>
    {
        ThrowIfDefaultActionRegistered(services);
        services.TryAdd(new ServiceDescriptor(typeof(IStateAction<StateContext>), typeof(T), serviceLifetime));

        return this;
    }

    private static void ThrowIfDataProviderAdded<TData>(IServiceCollection services)
    {
        if (services.Any(d => d.ServiceKey == null && d.ServiceType == typeof(IStateDataProvider<TData>)))
            throw new InvalidOperationException(
                $"Default state context provider for type '{typeof(TData).Name}' has already registered.");
    }

    private static void ThrowIfStateConfigured(IServiceCollection services, string stateName)
    {
        var stateKey = stateName.AsStateKey();

        if (services.Any(d => string.Equals(d.ServiceKey as string, stateKey) && d.ServiceType == typeof(IStateProcessor)))
            throw new InvalidOperationException($"State '{stateName}' has already configured.");
    }

    private static void ThrowIfDefaultStateConfigured(IServiceCollection services)
    {
        if (services.Any(d => d.ServiceKey == null && d.ServiceType == typeof(IStateProcessor)))
            throw new InvalidOperationException("Default state has already configured.");
    }

    private static void ThrowIfCallbacksConfigured(IServiceCollection services)
    {
        if (services.Any(d => d.ServiceType == typeof(IActionFactoriesCollection<StateContext>)
            && string.Equals(Constants.GlobalCallbackServiceKey, d.ServiceKey)))
            throw new InvalidOperationException("Callback query actions have been already configured.");
    }

    private static void ThrowIfDefaultActionRegistered(IServiceCollection services)
    {
        if (services.Any(d => d.ServiceType == typeof(IStateAction<StateContext>)))
            throw new InvalidOperationException("Default action has been already registered.");
    }
}

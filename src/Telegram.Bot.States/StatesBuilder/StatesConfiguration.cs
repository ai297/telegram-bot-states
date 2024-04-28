using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Telegram.Bot.States;

public sealed class StatesConfiguration
{
    private readonly IServiceCollection services;
    private readonly ICollection<string> commandLanguages;
    private readonly string defaultLanguageCode;

    internal StatesConfiguration(IServiceCollection services,
        ICollection<string> commandLanguages,
        string defaultLanguageCode)
    {
        this.services = services;
        this.commandLanguages = commandLanguages;
        this.defaultLanguageCode = defaultLanguageCode;
    }

    public StatesConfiguration AddState<TData>(string name, Action<StateBuilder<TData>> configureState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configureState);

        var stateKey = name.AsStateKey();
        if (services.Any(d => string.Equals(d.ServiceKey as string, stateKey) && d.ServiceType == typeof(IStateProcessor)))
            throw new InvalidOperationException($"State '{name}' has already registered.");

        var builder = new StateBuilder<TData>(name, services, commandLanguages);
        configureState(builder);

        builder.BindProcessor();
        builder.BindSetupService(defaultLanguageCode);

        return this;
    }

    public StatesConfiguration AddState(string name, Action<StateBuilder<ChatUpdate>> configureState)
        => AddState<ChatUpdate>(name, configureState);

    public StatesConfiguration AddDefaultState<TData>(Action<StateBuilder<TData>> configureState)
    {
        ArgumentNullException.ThrowIfNull(configureState);

        if (services.Any(d => d.ServiceKey == null && d.ServiceType == typeof(IStateProcessor)))
            throw new InvalidOperationException("Default state has already registered.");

        var builder = new StateBuilder<TData>(Constants.DefaultStateName, services, commandLanguages, isDefaultState: true);
        configureState(builder);

        builder.BindProcessor();
        builder.BindSetupService(defaultLanguageCode);

        return this;
    }

    public StatesConfiguration AddDefaultState(Action<StateBuilder<ChatUpdate>> configureState)
        => AddDefaultState<ChatUpdate>(configureState);

    #region Default data provider

    public StatesConfiguration AddDefaultDataProvider<TData>(
        Func<IServiceProvider, IStateDataProvider<TData>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (services.Any(d => d.ServiceKey == null && d.ServiceType == typeof(IStateDataProvider<TData>)))
            throw new InvalidOperationException(
                $"Default state context provider for type '{typeof(TData).Name}' has already registered.");

        services.AddTransient(factory);

        return this;
    }

    public StatesConfiguration AddDefaultDataProvider<TData, TService>()
        where TService : class, IStateDataProvider<TData>
    {
        if (services.Any(d => d.ServiceKey == null && d.ServiceType == typeof(IStateDataProvider<TData>)))
            throw new InvalidOperationException(
                $"Default state context provider for type '{typeof(TData).Name}' has already registered.");

        services.AddTransient<IStateDataProvider<TData>, TService>();

        return this;
    }

    /// <summary>
    /// Your <paramref name="@delegate" /> can receive <seealso cref="Telegram.Bot.States.ChatUpdate" />
    /// as parameter and  must return ValueTask&lt;<typeparamref name="TStateContext" />&rt; as result.
    /// </summary>
    public StatesConfiguration AddDefaultDataProvider<TData>(Delegate @delegate)
    {
        var delegateFactory = DelegateHelper
            .CreateDelegateFactory<IServiceProvider, Func<ChatUpdate, Task<TData>>>(
                @delegate, (serviceProvider, type) => serviceProvider.GetRequiredService(type));
        
        Func<IServiceProvider, IStateDataProvider<TData>> factory = serviceProvider =>
            new DelegateDataProvider<TData>(delegateFactory(serviceProvider));
        
        return AddDefaultDataProvider(factory);
    }

    #endregion
}

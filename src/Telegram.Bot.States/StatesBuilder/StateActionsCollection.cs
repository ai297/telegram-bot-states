using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Telegram.Bot.States;

public sealed class StateActionsCollection<TData>
{
    private readonly string stateName;
    private readonly IServiceCollection services;
    private readonly ICollection<string> languageCodes;

    internal readonly Dictionary<string, StateCommandFactory<TData>> Commands = [];
    internal readonly List<CommandDescription> CommandDescriptions = [];
    internal readonly List<StateCommandFactory<TData>> StateActions = [];

    internal StateActionsCollection(string stateName,
        IServiceCollection services,
        ICollection<string> languageCodes)
    {
        this.stateName = stateName;
        this.services = services;
        this.languageCodes = languageCodes;
    }

    #region Commands
    public StateActionsCollection<TData> AddCommand(string command,
        StateServiceFactory<IStateAction<TData>> actionFactory,
        Func<string, string>? descriptionProvider = null,
        Func<ChatUpdate, ChatState, bool>? commandCondition = null)
    {
        if (string.IsNullOrWhiteSpace(command)) throw new ArgumentException(
            "State command can't be null or empty string.",
            nameof(command));

        if (Commands.ContainsKey(command)) throw new ArgumentOutOfRangeException(nameof(command),
            $"Command '{command}' already configured for state '{stateName}'");

        ArgumentNullException.ThrowIfNull(actionFactory);

        commandCondition ??= DefaultCommandCondition;

        Commands.Add(command, new(actionFactory, commandCondition));

        if (descriptionProvider is not null)
            CommandDescriptions.AddRange(languageCodes
                .Select(lang => new CommandDescription(command, lang, descriptionProvider(lang), commandCondition))
                .Where(d => !string.IsNullOrWhiteSpace(d.Description)));

        return this;
    }

    public StateActionsCollection<TData> AddCommand<T>(string command,
        Func<string, string>? descriptionProvider = null,
        Func<ChatUpdate, ChatState, bool>? commandCondition = null)
        where T : class, IStateAction<TData>
    {
        services.TryAddTransient<T>();
        return AddCommand(command, (sp, _) => sp.GetRequiredService<T>(), descriptionProvider, commandCondition);
    }

    public StateActionsCollection<TData> AddCommand(string command,
        Delegate @delegate,
        Func<string, string>? descriptionProvider = null,
        Func<ChatUpdate, ChatState, bool>? commandCondition = null)
    {
        if (string.IsNullOrWhiteSpace(command)) throw new ArgumentException(
            "State command can't be null or empty string.",
            nameof(command));

        if (Commands.ContainsKey(command)) throw new ArgumentOutOfRangeException(nameof(command),
            $"Command '{command}' already configured for state '{stateName}'");

        commandCondition ??= DefaultCommandCondition;

        var delegateFactory = DelegateHelper
            .CreateDelegateFactory<IServiceProvider, Command<StateContext<TData>, Task<IStateResult>>>(
                @delegate, (provider, type) => provider.GetRequiredService(type));

        Commands.Add(command, new(
            (serviceProvider, _) => new AsyncDelegateCommand<StateContext<TData>, IStateResult>(delegateFactory(serviceProvider)),
            commandCondition));

        if (descriptionProvider is not null)
            CommandDescriptions.AddRange(languageCodes
                .Select(lang => new CommandDescription(command, lang, descriptionProvider(lang), commandCondition))
                .Where(d => !string.IsNullOrWhiteSpace(d.Description)));

        return this;
    }
    #endregion

    #region Callback Actions
    public StateActionsCollection<TData> AddCallbackHandler(
        StateServiceFactory<IStateAction<TData>> actionFactory,
        Func<ChatUpdate, ChatState, bool>? condition = null)
        => AddAction(actionFactory, IsCallbackQuery, condition);

    public StateActionsCollection<TData> AddCallbackHandler<T>(Func<ChatUpdate, ChatState, bool>? condition = null)
        where T : class, IStateAction<TData>
        => AddAction<T>(IsCallbackQuery, condition);

    public StateActionsCollection<TData> AddCallbackHandler(Delegate @delegate,
        Func<ChatUpdate, ChatState, bool>? condition = null)
        => AddAction(@delegate, IsCallbackQuery, condition);
    #endregion

    #region Common Actions
    private StateActionsCollection<TData> AddAction(
        StateServiceFactory<IStateAction<TData>> actionFactory,
        Func<ChatUpdate, ChatState, bool> defaultCondition,
        Func<ChatUpdate, ChatState, bool>? condition = null)
    {
        ArgumentNullException.ThrowIfNull(actionFactory);

        condition = condition != null ? (u, s) => defaultCondition(u, s) && condition(u, s) : defaultCondition;
        StateActions.Add(new(actionFactory, condition));

        return this;
    }

    private StateActionsCollection<TData> AddAction<T>(
        Func<ChatUpdate, ChatState, bool> defaultCondition,
        Func<ChatUpdate, ChatState, bool>? condition = null)
        where T : class, IStateAction<TData>
    {
        services.TryAddTransient<T>();
        return AddAction((sp, _) => sp.GetRequiredService<T>(), defaultCondition, condition);
    }

    public StateActionsCollection<TData> AddAction(Delegate @delegate,
        Func<ChatUpdate, ChatState, bool> defaultCondition,
        Func<ChatUpdate, ChatState, bool>? condition = null)
    {
        condition = condition != null ? (u, s) => defaultCondition(u, s) && condition(u, s) : defaultCondition;
        var delegateFactory = DelegateHelper
            .CreateDelegateFactory<IServiceProvider, Command<StateContext<TData>, Task<IStateResult>>>(
                @delegate, (provider, type) => provider.GetRequiredService(type));

        StateActions.Add(new(
            (serviceProvider, _) => new AsyncDelegateCommand<StateContext<TData>, IStateResult>(delegateFactory(serviceProvider)),
            condition));

        return this;
    }
    #endregion

    private static bool DefaultCommandCondition(ChatUpdate update, ChatState _) => true;
    private static bool IsCallbackQuery(ChatUpdate update, ChatState _)
        => update.Update.CallbackQuery != null;
}

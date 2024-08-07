using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Telegram.Bot.States;

public sealed class CommandsCollectionBuilder<TCtx> where TCtx : StateContext
{
    private readonly string? stateName;
    private readonly IServiceCollection services;
    private readonly ICollection<string> languageCodes;

    internal readonly Dictionary<string, StateActionFactory<TCtx>> Factories = [];
    internal readonly List<CommandDescription> Descriptions = [];

    internal CommandsCollectionBuilder(IServiceCollection services,
        ICollection<string> languageCodes,
        string? stateName = null)
    {
        this.stateName = stateName;
        this.services = services;
        this.languageCodes = languageCodes;
    }

    public CommandsCollectionBuilder<TCtx> Add(string command,
        StateServiceFactory<IStateAction<TCtx>> actionFactory,
        Func<string, string>? descriptionProvider = null,
        Func<ChatUpdate, ChatState, bool>? commandCondition = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(actionFactory);
        ThrowIfCommandConfigured(command);

        Factories.Add(command, new(actionFactory, commandCondition));

        if (descriptionProvider is not null) Descriptions.AddRange(languageCodes
            .Select(lang => new CommandDescription(command, lang, descriptionProvider(lang), commandCondition))
            .Where(d => !string.IsNullOrWhiteSpace(d.Description)));

        return this;
    }

    public CommandsCollectionBuilder<TCtx> Add<T>(string command,
        Func<string, string>? descriptionProvider = null,
        Func<ChatUpdate, ChatState, bool>? commandCondition = null,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        where T : class, IStateAction<TCtx>
    {
        services.TryAdd(new ServiceDescriptor(typeof(T), typeof(T), serviceLifetime));

        return Add(command, (sp, _) => sp.GetRequiredService<T>(), descriptionProvider, commandCondition);
    }

    public CommandsCollectionBuilder<TCtx> Add(string command,
        Delegate @delegate,
        Func<string, string>? descriptionProvider = null,
        Func<ChatUpdate, ChatState, bool>? commandCondition = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(@delegate);
        ThrowIfCommandConfigured(command);

        var delegateFactory = DelegateHelper.CreateDelegateFactory<IServiceProvider, StateAction<TCtx>>(
            @delegate, (provider, type) => provider.GetRequiredService(type));

        Factories.Add(command, new(
            (serviceProvider, _) => new DelegateAction<TCtx>(delegateFactory(serviceProvider)),
            commandCondition));

        if (descriptionProvider is not null) Descriptions.AddRange(languageCodes
            .Select(lang => new CommandDescription(command, lang, descriptionProvider(lang), commandCondition))
            .Where(d => !string.IsNullOrWhiteSpace(d.Description)));

        return this;
    }

    private void ThrowIfCommandConfigured(string command)
    {
        if (!Factories.ContainsKey(command)) return;

        var actionsScope = stateName != null ? $"state '{stateName}'" : "global scope";

        throw new ArgumentOutOfRangeException(nameof(command),
            $"Command '{command}' already configured for {actionsScope}.");
    }
}

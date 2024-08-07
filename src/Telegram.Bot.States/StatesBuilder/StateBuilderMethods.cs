using System;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

internal static class StateBuilderMethods
{
    public static readonly Expression<Func<IServiceProvider, Type, object>> GetServiceExpr
        = (serviceProvider, type) => serviceProvider.GetRequiredService(type);

    public static TBuilder WithCommands<TBuilder, TCtx>(TBuilder builder, Action<CommandsCollectionBuilder<TCtx>> configureCommands)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureCommands);
        configureCommands(builder.CommandsCollectionBuilder);

        return builder;
    }

    public static TBuilder WithActions<TBuilder, TCtx, TKey>(this TBuilder builder,
        Func<StateContext, TKey> actionKeySelector,
        Action<ActionsCollectionBuilder<TKey, TCtx>> configureActions)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(actionKeySelector);
        ArgumentNullException.ThrowIfNull(configureActions);

        var collectionBuilder = new ActionsCollectionBuilder<TKey, TCtx>(builder.Services, builder.StateName);
        builder.ActionFactories = new ActionFactoriesCollection<TKey, TCtx>(actionKeySelector, collectionBuilder.Factories);

        configureActions(collectionBuilder);

        return builder;
    }

    public static TBuilder WithSteps<TBuilder, TCtx>(this TBuilder builder, Action<StateStepsCollection<TCtx>> configureSteps)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureSteps);
        configureSteps(builder.StepsCollection);

        return builder;
    }

    public static TBuilder WithWebAppButton<TBuilder, TCtx>(this TBuilder builder,
        Func<string, (string text, string url)> getLocalizedButton)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(getLocalizedButton);

        builder.MenuButtonFactory = languageCode => {
            var (text, url) = getLocalizedButton(languageCode);
            return new MenuButtonWebApp
            {
                Text = text,
                WebApp = new WebAppInfo { Url = url, },
            };
        };

        return builder;
    }

    public static TBuilder WithCommandsMenuButton<TBuilder, TCtx>(this TBuilder builder)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.MenuButtonFactory = _ => new MenuButtonCommands();

        return builder;
    }

    public static TBuilder WithDefaultAction<TBuilder, TCtx>(this TBuilder builder,
        StateServiceFactory<IStateAction<TCtx>> factory)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.DefaultActionFactory = factory;

        return builder;
    }

    public static TBuilder WithDefaultAction<TBuilder, TCtx>(this TBuilder builder, Delegate @delegate)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(@delegate);

        var delegateFactory = DelegateHelper
            .CreateDelegateFactory<IServiceProvider, StateAction<TCtx>>(
                @delegate, GetServiceExpr);

        builder.DefaultActionFactory = (sp, _) => new LazyDelegateAction<TCtx>(sp, delegateFactory);

        return builder;
    }

    public static TBuilder WithDefaultAction<TBuilder, TCtx, T>(this TBuilder builder, ServiceLifetime serviceLifetime)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
        where T : class, IStateAction<TCtx>
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.DefaultActionFactory = (sp, _) => sp.GetRequiredService<T>();
        builder.Services.TryAdd(new ServiceDescriptor(typeof(T), typeof(T), serviceLifetime));

        return builder;
    }
}

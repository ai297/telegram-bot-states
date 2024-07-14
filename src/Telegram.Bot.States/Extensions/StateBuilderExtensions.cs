﻿using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

public static class StateBuilderExtensions
{
    public static readonly Expression<Func<IServiceProvider, Type, object>> GetServiceExpr
        = (serviceProvider, type) => serviceProvider.GetRequiredService(type);

    public static TBuilder WithCommands<TBuilder, TCtx>(this TBuilder builder, Action<CommandsCollectionBuilder<TCtx>> configureCommands)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
    {
        ArgumentNullException.ThrowIfNull(configureCommands);
        configureCommands(builder.CommandsCollectionBuilder);

        return builder;
    }

    public static TBuilder WithSteps<TBuilder, TCtx>(this TBuilder builder, Action<StateStepsCollection<TCtx>> configureSteps)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
    {
        ArgumentNullException.ThrowIfNull(configureSteps);
        configureSteps(builder.StepsCollection);

        return builder;
    }

    public static TBuilder WithWebAppButton<TBuilder, TCtx>(this TBuilder builder,
        Func<string, (string text, string url)> getLocalizedButton)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
    {
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
        builder.MenuButtonFactory = _ => new MenuButtonCommands();

        return builder;
    }

    public static TBuilder WithDefaultAction<TBuilder, TCtx>(this TBuilder builder,
        StateServiceFactory<IStateStep<TCtx>> factory)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
    {
        ArgumentNullException.ThrowIfNull(factory);
        builder.DefaultActionFactory = factory;

        return builder;
    }

    public static TBuilder WithDefaultAction<TBuilder, TCtx>(this TBuilder builder, Delegate @delegate)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
    {
        var delegateFactory = DelegateHelper
            .CreateDelegateFactory<IServiceProvider, Command<TCtx, Task<IStateResult>>>(
                @delegate, GetServiceExpr);

        builder.DefaultActionFactory = (sp, _) => new AsyncDelegateCommandLazy<TCtx, IStateResult>(sp, delegateFactory);

        return builder;
    }

    public static StepBinder<TBuilder, TCtx> WithDefaultAction<TBuilder, TCtx>(this TBuilder builder)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx>
        => new(builder);

    public static StateBuilder<TData> WithDataProvider<TBuilder, TData>(this StateBuilder<TData> builder,
        StateServiceFactory<IStateDataProvider<TData>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        builder.DataProviderFactory = factory;

        return builder;
    }

    public static DataProviderBinder<TData> WithDataProvider<TData>(this StateBuilder<TData> builder)
        => new(builder);

    /// <summary>
    /// Your <paramref name="@delegate" /> can receive <seealso cref="Telegram.Bot.States.ChatUpdate" />
    /// as parameter and  must return Task&lt;<typeparamref name="TData" />&rt; as result.
    /// </summary>
    public static StateBuilder<TData> WithDataProvider<TData>(this StateBuilder<TData> builder, Delegate @delegate)
    {
        ArgumentNullException.ThrowIfNull(@delegate);

        var delegateFactory = DelegateHelper
            .CreateDelegateFactory<IServiceProvider,Func<ChatUpdate, Task<TData>>>(
                @delegate, GetServiceExpr);

        builder.DataProviderFactory = (sp, _) => new DelegateDataProvider<TData>(delegateFactory(sp));

        return builder;
    }
}

using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

internal static class StateBuilderMethods
{
    public static readonly Expression<Func<IServiceProvider, Type, object>> GetServiceExpr
        = (serviceProvider, type) => serviceProvider.GetRequiredService(type);

    public static TBuilder WithCommands<TBuilder, TCtx, TAction>(TBuilder builder, Action<CommandsCollectionBuilder<TCtx, TAction>> configureCommands)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx, TAction>
        where TAction : IAsyncCommand<TCtx, IStateResult>
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureCommands);
        configureCommands(builder.CommandsCollectionBuilder);

        return builder;
    }

    public static TBuilder WithCallbacks<TBuilder, TCtx, TKey, TAction>(this TBuilder builder,
        Func<StateContext, TKey> callbackKeySelector,
        Action<CallbacksCollectionBuilder<TKey, TCtx, TAction>> configureCallbacks)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx, TAction>
        where TKey : notnull
        where TAction : IAsyncCommand<TCtx, IStateResult>
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(callbackKeySelector);
        ArgumentNullException.ThrowIfNull(configureCallbacks);

        var collectionBuilder = new CallbacksCollectionBuilder<TKey, TCtx, TAction>(builder.Services, builder.StateName);
        builder.CallbackFactories = new ActionFactoriesCollection<TKey, TCtx>(callbackKeySelector, collectionBuilder.Factories);

        configureCallbacks(collectionBuilder);

        return builder;
    }

    public static TBuilder WithSteps<TBuilder, TCtx, TAction>(this TBuilder builder, Action<StateStepsCollection<TCtx, TAction>> configureSteps)
        where TCtx : StateContext
        where TAction : IAsyncCommand<TCtx, IStateResult>
        where TBuilder : StateBuilderBase<TCtx, TAction>
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureSteps);
        configureSteps(builder.StepsCollection);

        return builder;
    }

    public static TBuilder WithWebAppButton<TBuilder, TCtx, TAction>(this TBuilder builder,
        Func<string, (string text, string url)> getLocalizedButton)
        where TCtx : StateContext
        where TBuilder : StateBuilderBase<TCtx, TAction>
        where TAction : IAsyncCommand<TCtx, IStateResult>
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

    public static TBuilder WithCommandsMenuButton<TBuilder, TCtx, TAction>(this TBuilder builder)
        where TCtx : StateContext
        where TAction : IAsyncCommand<TCtx, IStateResult>
        where TBuilder : StateBuilderBase<TCtx, TAction>
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.MenuButtonFactory = _ => new MenuButtonCommands();

        return builder;
    }

    public static TBuilder WithDefaultAction<TBuilder, TCtx, TAction>(this TBuilder builder,
        StateServiceFactory<TAction> factory)
        where TCtx : StateContext
        where TAction : IAsyncCommand<TCtx, IStateResult>
        where TBuilder : StateBuilderBase<TCtx, TAction>
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.DefaultActionFactory = factory as StateServiceFactory<IAsyncCommand<TCtx, IStateResult>>;

        return builder;
    }

    public static TBuilder WithDefaultAction<TBuilder, TCtx, TAction>(this TBuilder builder, Delegate @delegate)
        where TCtx : StateContext
        where TAction : IAsyncCommand<TCtx, IStateResult>
        where TBuilder : StateBuilderBase<TCtx, TAction>
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(@delegate);

        var delegateFactory = DelegateHelper
            .CreateDelegateFactory<IServiceProvider, Command<TCtx, Task<IStateResult>>>(
                @delegate, GetServiceExpr);

        builder.DefaultActionFactory = (sp, _) => new AsyncDelegateCommandLazy<TCtx, IStateResult>(sp, delegateFactory);

        return builder;
    }

    public static TBuilder WithDefaultAction<TBuilder, TCtx, TAction, T>(this TBuilder builder, ServiceLifetime serviceLifetime)
        where TCtx : StateContext
        where TAction : IAsyncCommand<TCtx, IStateResult>
        where TBuilder : StateBuilderBase<TCtx, TAction>
        where T : class, TAction
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.DefaultActionFactory = (sp, _) => sp.GetRequiredService<T>();
        builder.Services.TryAdd(new ServiceDescriptor(typeof(T), typeof(T), serviceLifetime));

        return builder;
    }
}

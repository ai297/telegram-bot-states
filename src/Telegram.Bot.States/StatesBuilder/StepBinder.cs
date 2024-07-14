using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Telegram.Bot.States;

public readonly struct StepBinder<TBuilder, TCtx>(TBuilder builder)
    where TCtx : StateContext
    where TBuilder : StateBuilderBase<TCtx>
{
    public TBuilder Transient<T>() where T : class, IStateStep<TCtx>
    {
        builder.Services.TryAddTransient<T>();
        builder.DefaultActionFactory = (sp, _) => sp.GetRequiredService<T>();

        return builder;
    }

    public TBuilder Scoped<T>() where T : class, IStateStep<TCtx>
    {
        builder.Services.TryAddScoped<T>();
        builder.DefaultActionFactory = (sp, _) => sp.GetRequiredService<T>();

        return builder;
    }

    public TBuilder Singleton<T>() where T : class, IStateStep<TCtx>
    {
        builder.Services.TryAddSingleton<T>();
        builder.DefaultActionFactory = (sp, _) => sp.GetRequiredService<T>();

        return builder;
    }
}

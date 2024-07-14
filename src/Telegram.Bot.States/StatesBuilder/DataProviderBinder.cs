using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Telegram.Bot.States;

public readonly struct DataProviderBinder<TData>(StateBuilder<TData> builder)
{
    public StateBuilder<TData> Transient<T>() where T : class, IStateDataProvider<TData>
    {
        builder.Services.TryAddTransient<T>();
        builder.DataProviderFactory = (sp, _) => sp.GetRequiredService<T>();

        return builder;
    }

    public StateBuilder<TData> Scoped<T>() where T : class, IStateDataProvider<TData>
    {
        builder.Services.TryAddScoped<T>();
        builder.DataProviderFactory = (sp, _) => sp.GetRequiredService<T>();

        return builder;
    }

    public StateBuilder<TData> Singleton<T>() where T : class, IStateDataProvider<TData>
    {
        builder.Services.TryAddSingleton<T>();
        builder.DataProviderFactory = (sp, _) => sp.GetRequiredService<T>();

        return builder;
    }
}

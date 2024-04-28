using System;

namespace Telegram.Bot.States;

internal class LazyDelegateCommand<TParams, TResult>(IServiceProvider serviceProvider,
    Func<IServiceProvider, Command<TParams, TResult>> delegateFactory)
    : ICommand<TParams,TResult>
{
    private Command<TParams, TResult>? command = null;

    public TResult Execute(TParams commandParameters)
    {
        command ??= delegateFactory(serviceProvider);

        return command(commandParameters);
    }
}

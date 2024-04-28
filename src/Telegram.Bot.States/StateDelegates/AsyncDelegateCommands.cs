using System;
using System.Threading.Tasks;

namespace Telegram.Bot.States;

internal class AsyncDelegateCommand<TParams, TResult>(
    Command<TParams, Task<TResult>> commandDelegate)
    : DelegateCommand<TParams, Task<TResult>>(commandDelegate),
    IAsyncCommand<TParams, TResult>
{
}

internal class AsyncDelegateCommandLazy<TParams, TResult>(
    IServiceProvider serviceProvider,
    Func<IServiceProvider, Command<TParams, Task<TResult>>> delegateFactory)
    : LazyDelegateCommand<TParams, Task<TResult>>(serviceProvider, delegateFactory),
    IAsyncCommand<TParams, TResult>
{
}

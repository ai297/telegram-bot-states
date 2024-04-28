using System.Threading.Tasks;

namespace Telegram.Bot.States;

public interface IAsyncCommand<in TParams> : ICommand<TParams, Task> { }

public interface IAsyncCommand<in TParams, TResult> : ICommand<TParams, Task<TResult>> { }

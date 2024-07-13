using System.Threading.Tasks;

namespace Telegram.Bot.States;

public interface IAsyncCommand<in TCtx, TResult> : ICommand<TCtx, Task<TResult>> { }

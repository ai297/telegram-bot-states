namespace Telegram.Bot.States;

public interface IStateStep<TCtx> : IAsyncCommand<TCtx, IStateResult> where TCtx : StateContext
{
}

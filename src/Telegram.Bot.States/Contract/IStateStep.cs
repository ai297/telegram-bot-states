namespace Telegram.Bot.States;

public interface IStateStep<in TCtx> : IAsyncCommand<TCtx, IStateResult> where TCtx : StateContext
{
}

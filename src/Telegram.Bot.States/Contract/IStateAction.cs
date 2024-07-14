namespace Telegram.Bot.States;

public interface IStateAction<TCtx> : IAsyncCommand<TCtx, IStateResult> where TCtx : StateContext
{
}

namespace Telegram.Bot.States;

public interface IStateAction<in TCtx> : IAsyncCommand<TCtx, IStateResult> where TCtx : StateContext
{
}

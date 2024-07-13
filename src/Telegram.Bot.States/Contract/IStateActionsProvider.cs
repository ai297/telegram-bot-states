namespace Telegram.Bot.States;

public interface IStateActionsProvider<TCtx> where TCtx : StateContext
{
    IAsyncCommand<TCtx, IStateResult>? GetAction(ChatUpdate update, ChatState state);
}

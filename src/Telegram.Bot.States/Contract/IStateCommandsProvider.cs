namespace Telegram.Bot.States;

public interface IStateActionsProvider<TData>
{
    IAsyncCommand<StateContext<TData>, IStateResult>? GetAction(ChatUpdate update, ChatState state);
}

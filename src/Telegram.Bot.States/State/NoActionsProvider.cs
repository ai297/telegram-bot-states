namespace Telegram.Bot.States;

internal class NoActionsProvider<T> : IStateActionsProvider<T>
{
    internal NoActionsProvider() {}
    public IAsyncCommand<StateContext<T>, IStateResult>? GetAction(ChatUpdate update, ChatState state) => null;
}

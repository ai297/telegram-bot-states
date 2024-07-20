namespace Telegram.Bot.States;

public interface IStateActionsProvider
{
    IAsyncCommand<StateContext, IStateResult>? GetAction(StateContext context);
}

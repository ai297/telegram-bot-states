namespace Telegram.Bot.States;

public interface ICommand<in TCtx, out TResult>
{
    TResult Execute(TCtx context);
}

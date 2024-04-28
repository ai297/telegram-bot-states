namespace Telegram.Bot.States;

public interface ICommand<in TParams, out TResult>
{
    TResult Execute(TParams parameters);
}

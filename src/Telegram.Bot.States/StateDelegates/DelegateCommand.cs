namespace Telegram.Bot.States;

public delegate TResult Command<TParams, TResult>(TParams parameters);

internal class DelegateCommand<TParams, TResult>(Command<TParams, TResult> commandDelegate)
    : ICommand<TParams, TResult>
{
    public TResult Execute(TParams parameters) => commandDelegate(parameters);
}

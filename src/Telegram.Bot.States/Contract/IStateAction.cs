namespace Telegram.Bot.States;

public interface IStateAction : IAsyncCommand<StateContext, IStateResult>
{
}

public interface IStateAction<TData> : IAsyncCommand<StateContext<TData>, IStateResult>
{
}

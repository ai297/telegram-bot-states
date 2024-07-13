namespace Telegram.Bot.States;

public interface IStateAction<TData> : IAsyncCommand<StateContext<TData>, IStateResult> { }

public interface IStateAction : IAsyncCommand<StateContext, IStateResult> { }

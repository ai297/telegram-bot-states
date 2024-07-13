namespace Telegram.Bot.States;

public interface IStateStep<TData> : IAsyncCommand<StateContext<TData>, IStateResult> { }

public interface IStateStep : IAsyncCommand<StateContext, IStateResult> { }

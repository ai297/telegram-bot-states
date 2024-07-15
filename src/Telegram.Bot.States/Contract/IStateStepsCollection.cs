namespace Telegram.Bot.States;

public interface IStateStepsCollection<in TCtx> where TCtx : StateContext
{
    IAsyncCommand<TCtx, IStateResult>? Get(string stepKey);
    string? GetFirstStepKey();
    string? GetNextStepKey(string currentStepKey);
    int Count { get; }
}

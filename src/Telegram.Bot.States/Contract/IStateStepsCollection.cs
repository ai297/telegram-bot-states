namespace Telegram.Bot.States;

public interface IStateStepsCollection<TData>
{
    IAsyncCommand<StateContext<TData>, IStateResult>? Get(string stepKey);
    string? GetFirstStepKey();
    string? GetNextStepKey(string currentStepKey);
    int Count { get; }
}

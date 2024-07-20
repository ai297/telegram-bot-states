namespace Telegram.Bot.States;

public interface IStateStepsCollection
{
    IStateAction<StateContext>? Get(string stepKey);
    string? GetFirstStepKey();
    string? GetNextStepKey(string currentStepKey);
    int Count { get; }
}

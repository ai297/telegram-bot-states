using System;

namespace Telegram.Bot.States;

public interface IStateStepsCollection
{
    IStateAction<StateContext>? Get(string stepKey, IServiceProvider serviceProvider);
    string? GetFirstStepKey();
    string? GetNextStepKey(string currentStepKey);
    int Count { get; }
}

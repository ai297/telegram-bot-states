using System;

namespace Telegram.Bot.States;

public interface IStateStepsCollection<in TCtx> where TCtx : StateContext
{
    IStateAction<TCtx>? Get(string stepKey, IServiceProvider serviceProvider);
    string? GetFirstStepKey();
    string? GetNextStepKey(string currentStepKey);
    int Count { get; }
}

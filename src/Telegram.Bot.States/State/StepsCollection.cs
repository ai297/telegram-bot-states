using System;
using System.Collections.Generic;
using System.Linq;

namespace Telegram.Bot.States;

internal class StepsCollection<TCtx>(IReadOnlyList<StateStepCollectionItem<TCtx>> stateSteps)
    : IStateStepsCollection<TCtx>
    where TCtx : StateContext
{
    public int Count => stateSteps.Count;

    public IStateAction<TCtx>? Get(string stepKey, IServiceProvider serviceProvider)
    {
        var factory = stateSteps.FirstOrDefault(s => s.Key == stepKey).Factory;

        return factory is not null ? factory(serviceProvider) : null;
    }

    public string? GetFirstStepKey() => stateSteps.Count > 0 ? stateSteps[0].Key : null;

    public string? GetNextStepKey(string currentStepKey)
    {
        for (var i = 0; i < stateSteps.Count; i++)
        {
            if (stateSteps[i].Key != currentStepKey)
                continue;

            if (i < stateSteps.Count - 1)
                return stateSteps[i + 1].Key;

            return null;
        }

        throw new KeyNotFoundException(
            $"Can't return next step key because current step key '{currentStepKey}' not found in collection.");
    }
}
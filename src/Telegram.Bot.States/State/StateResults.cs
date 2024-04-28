using System;
using System.Collections.Generic;

namespace Telegram.Bot.States;

public static class StateResults
{
    public static IStateResult Complete() => new CompleteResult();
    public static IStateResult Complete(params KeyValuePair<string, string?>[] labels)
        => new CompleteResult(labels);

    public static IStateResult Continue() => new ContinueResult();
    public static IStateResult Continue(params KeyValuePair<string, string?>[] labels)
        => new ContinueResult(labels);

    public static IStateResult ChangeState(string stateName,
        params KeyValuePair<string, string?>[] labels)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);
        return new ChangeStateResult(stateName, labels);
    }

    public static IStateResult ChangeStateToDefault()
        => ChangeState(Constants.DefaultStateName);

    public static IStateResult ContinueWithStep(string? stepKey)
        => new ContinueWithStepResult(stepKey);

    public static IStateResult CompleteWithStep(string? stepKey)
        => new SetStepAndCompleteResult(stepKey);

    public static IStateResult CompleteWithNextStep()
        => new CompleteWithNextStepResult();

    internal class CompleteResult(ICollection<KeyValuePair<string, string?>>? labels = null)
        : IStateResult
    {
        public bool Complete { get; } = true;
        public virtual ChatState GetResultState(ChatUpdate _, ChatState state)
            => labels is null ? state : state.WithLabels(labels);
    }

    internal class ContinueResult(ICollection<KeyValuePair<string, string?>>? labels = null)
        : IStateResult
    {
        public bool Complete { get; } = false;
        public virtual ChatState GetResultState(ChatUpdate _, ChatState state)
            => labels is null ? state : state.WithLabels(labels);
    }

    internal class ChangeStateResult(string stateName,
        ICollection<KeyValuePair<string, string?>>? labels)
        : CompleteResult
    {
        public override ChatState GetResultState(ChatUpdate _, ChatState state)
            => state.NewState(stateName).WithLabels(labels).AddOrUpdateLabel(
                Constants.StateChangedKey, state.StateName);
    }

    internal class ContinueWithStepResult(string? stepKey) : ContinueResult
    {
        public readonly string? StepKey = stepKey;
        public override ChatState GetResultState(ChatUpdate _, ChatState state)
            => state.AddOrUpdateLabel(Constants.StateStepKey, StepKey);
    }

    internal class SetStepAndCompleteResult(string? stepKey) : CompleteResult
    {
        public override ChatState GetResultState(ChatUpdate _, ChatState state)
            => state.AddOrUpdateLabel(Constants.StateStepKey, stepKey);
    }

    internal class CompleteWithNextStepResult : CompleteResult
    {
        private string? nextStepKey = Constants.AllStepsCompletedLabel;

        public IStateResult WithNextStepKey(string? stepKey)
        {
            if (!string.IsNullOrWhiteSpace(stepKey))
                nextStepKey = stepKey;

            return this;
        }

        public override ChatState GetResultState(ChatUpdate _, ChatState state)
            => state.AddOrUpdateLabel(Constants.StateStepKey, nextStepKey);
    }
}

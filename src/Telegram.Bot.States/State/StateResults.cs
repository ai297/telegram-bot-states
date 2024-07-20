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

    internal abstract class StateResult(ICollection<KeyValuePair<string, string?>>? labels) : IStateResult
    {
        public virtual bool Complete => true;
        public virtual ChatState GetResultState(ChatUpdate _, ChatState state)
            => labels is null ? state.Same() : state.Same().WithLabels(labels);
    }

    internal class CompleteResult(ICollection<KeyValuePair<string, string?>>? labels = null) : StateResult(labels)
    {
    }

    internal class ContinueResult(ICollection<KeyValuePair<string, string?>>? labels = null) : StateResult(labels)
    {
        public override bool Complete { get; } = false;
    }

    internal class ChangeStateResult(string stateName, ICollection<KeyValuePair<string, string?>>? labels = null) : CompleteResult
    {
        public override ChatState GetResultState(ChatUpdate _, ChatState state)
            => state.New(stateName).WithLabels(labels);
    }

    internal class ContinueWithStepResult(string? stepKey) : ContinueResult
    {
        public readonly string? StepKey = stepKey;
        public override ChatState GetResultState(ChatUpdate _, ChatState state)
            => state.Same().AddOrUpdateLabel(Constants.StateStepKey, StepKey);
    }

    internal class SetStepAndCompleteResult(string? stepKey) : CompleteResult
    {
        public override ChatState GetResultState(ChatUpdate _, ChatState state)
            => state.Same().AddOrUpdateLabel(Constants.StateStepKey, stepKey);
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
            => state.Same().AddOrUpdateLabel(Constants.StateStepKey, nextStepKey);
    }
}

using System;
using System.Collections.Generic;

namespace Telegram.Bot.States;

public static class StateResults
{
    /// <summary>
    /// Complete processing update without changing state.
    /// </summary>
    public static IStateResult Complete() => new CompleteResult();

    /// <summary>
    /// Complete processing update without changing state.
    /// </summary>
    public static IStateResult Complete(params KeyValuePair<string, string?>[] labels)
        => new CompleteResult(labels);

    /// <summary>
    /// Continue processing update by next step of current state.
    /// </summary>
    public static IStateResult Continue() => new ContinueResult();

    /// <summary>
    /// Continue processing update by next step of current state.
    /// </summary>
    public static IStateResult Continue(params KeyValuePair<string, string?>[] labels)
        => new ContinueResult(labels);

    /// <summary>
    /// Switch current state to a new one and continue processing update.
    /// </summary>
    public static IStateResult ChangeState(string stateName,
        params KeyValuePair<string, string?>[] labels)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);
        return new ChangeStateResult(stateName, labels);
    }

    /// <summary>
    /// Switch current state to default and continue processing update.
    /// </summary>
    public static IStateResult ChangeStateToDefault()
        => ChangeState(Constants.DefaultStateName);

    /// <summary>
    /// Continue processing update by concrete step in current state.
    /// </summary>
    public static IStateResult ContinueWithStep(string? stepKey)
        => new ContinueWithStepResult(stepKey);

    /// <summary>
    /// Complete processing update and set concrete step to processing new one.
    /// </summary>
    public static IStateResult CompleteWithStep(string? stepKey)
        => new SetStepAndCompleteResult(stepKey);

    /// <summary>
    /// Complete processing update and set next step to processing new one.
    /// </summary>
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

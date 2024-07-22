using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Telegram.Bot.States;

internal class StateProcessor<TCtx>(
    IStateContextFactory<TCtx> contextFactory,
    IStateActionsProvider<TCtx> actionsProvider,
    IStateStepsCollection stepsCollection,
    IStateAction<TCtx>? defaultAction,
    IServiceProvider serviceProvider,
    ILogger<IStateProcessor> logger)
    : IStateProcessor where TCtx : StateContext
{
    private static readonly IStateResult AllStepsCompleted = StateResults.Complete(
        new KeyValuePair<string, string?>(Constants.StateStepKey, Constants.AllStepsCompletedLabel));

    public async Task<ChatState> Process(ChatUpdate update, ChatState currentState)
    {
        var context = await contextFactory.Create(update, currentState);
        var resultState = currentState.Same();
        IStateResult processingResult;

        // first - process commands or other actions
        if (!currentState.IsChanged)
        {
            var action = actionsProvider.GetAction(context, serviceProvider);

            if (action != null)
            {
                logger.LogDebug("Process {actionType} for chat '{chatId}' in state '{stateName}'...",
                    update.IsCommand ? $"command '{update.Command}'" : (update.IsCallbackQuery ? "callback query" : "action"),
                    update.Chat.Id, currentState.StateName);

                processingResult = await action.Execute(context);
                resultState = processingResult.GetResultState(update, currentState);

                if (processingResult.Complete) return resultState;
            }
            else if (update.IsCommand)
            {
                logger.LogWarning(
                    "Unavailable command '{command}' has been requested for chat '{chatId}' in state '{stateName}'...",
                    update.Command, update.Chat.Id, currentState.StateName);

                return resultState;
            }
        }

        // second - process steps
        if (ShouldProcessStep(currentState, out var stepKey))
        {
            logger.LogDebug("Process step '{stepKey}' for chat '{chatId}' in state '{stateName}'...",
                stepKey, update.Chat.Id, currentState.StateName);

            processingResult = await ProcessStep(context, stepKey);
            resultState = processingResult.GetResultState(update, currentState);

            if (processingResult.Complete) return resultState;
        }

        if (currentState.IsChanged || defaultAction is null)
            return resultState;

        // and last - process default action
        logger.LogDebug("Process default action for chat '{chatId}' in state '{stateName}'...",
            update.Chat.Id, currentState.StateName);

        processingResult = await defaultAction.Execute(context);

        return processingResult.GetResultState(update, resultState);
    }

    private bool ShouldProcessStep(ChatState state, out string? stepKey)
        => state.Labels.TryGetValue(Constants.StateStepKey, out stepKey)
        || (stepKey = stepsCollection.GetFirstStepKey()) != null;

    private async Task<IStateResult> ProcessStep(TCtx context, string? stepKey)
    {
        if (stepKey == Constants.AllStepsCompletedLabel)
            return StateResults.Continue();

        if (string.IsNullOrWhiteSpace(stepKey))
        {
            logger.LogDebug("All steps in state {stateName} for chat '{chatId}' has been completed.",
                context.State.StateName, context.Update.Chat.Id);

            return AllStepsCompleted;
        }

        var step = stepsCollection.Get(stepKey, serviceProvider);

        if (step == null)
        {
            logger.LogError(
                "Can't find step '{stepKey}' for state '{stateName}'. State steps " +
                "for chat '{chatId}' will be marked as all completed.",
                stepKey, context.State.StateName, context.Update.Chat.Id);

            return AllStepsCompleted;
        }

        context.State.AddOrUpdateLabel(Constants.StateStepKey, stepKey);
        var stepResult = await step.Execute(context);

        return stepResult switch
        {
            StateResults.CompleteWithNextStepResult completeResult
                => completeResult.WithNextStepKey(stepsCollection.GetNextStepKey(stepKey)),
            StateResults.ContinueWithStepResult continueResult
                => await ProcessStep(context, continueResult.StepKey),
            { Complete: false } => await ProcessStep(context, stepsCollection.GetNextStepKey(stepKey)),
            _ => stepResult,
        };
    }
}

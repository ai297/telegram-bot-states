using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Telegram.Bot.States;

internal class StateProcessor<TData>(
    IStateDataProvider<TData> dataProvider,
    IStateActionsProvider<TData> actionsProvider,
    IStateStepsCollection<TData> stepsCollection,
    IAsyncCommand<StateContext<TData>, IStateResult>? defaultAction,
    IServiceProvider serviceProvider)
    : IStateProcessor
{
    private static readonly IStateResult AllStepsCompleted = StateResults.Complete(
        new KeyValuePair<string, string?>(Constants.StateStepKey, Constants.AllStepsCompletedLabel));

    private readonly Lazy<ITelegramBotClient> botClientLazy = new(() => serviceProvider
        .GetRequiredService<ITelegramBotClient>());

    private readonly ILogger<StateProcessor<TData>> logger = serviceProvider
        .GetRequiredService<ILogger<StateProcessor<TData>>>();

    public async Task<ChatState> Process(ChatUpdate update, ChatState currentState)
    {
        var isStateNotChanged = !currentState.Labels.ContainsKey(Constants.StateChangedKey);
        var data = await dataProvider.Get(update);
        var resultState = currentState;
        IStateResult processingResult;

        // first - process commands or other actions
        if (isStateNotChanged)
        {
            var action = actionsProvider.GetAction(update, currentState);

            if (action != null)
            {
                logger.LogDebug("Process action for chat '{chatId}' in state '{stateName}'...",
                    update.Chat.Id, currentState.StateName);

                processingResult = await action.Execute(new StateContext<TData>(data, update, currentState, botClientLazy));
                resultState = processingResult.GetResultState(update, resultState);

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

            processingResult = await ProcessStep(data, update, currentState, stepKey);
            resultState = processingResult.GetResultState(update, resultState);

            if (processingResult.Complete) return resultState;
        }

        // and last - process default action
        if (defaultAction is not null && isStateNotChanged)
        {
            logger.LogDebug("Process default action for chat '{chatId}' in state '{stateName}'...",
                update.Chat.Id, currentState.StateName);

            processingResult = await defaultAction.Execute(
                new StateContext<TData>(data, update, currentState, botClientLazy));

            resultState = processingResult.GetResultState(update, resultState);
        }

        return resultState;
    }

    private bool ShouldProcessStep(ChatState state, out string? stepKey)
        => state.Labels.TryGetValue(Constants.StateStepKey, out stepKey)
        || (stepKey = stepsCollection.GetFirstStepKey()) != null;

    private async Task<IStateResult> ProcessStep(TData data, ChatUpdate update, ChatState state, string? stepKey)
    {
        if (stepKey == Constants.AllStepsCompletedLabel)
            return StateResults.Continue();

        if (string.IsNullOrWhiteSpace(stepKey))
        {
            logger.LogDebug("All steps in state {stateName} for chat '{chatId}' has been completed.",
                state.StateName, update.Chat.Id);

            return AllStepsCompleted;
        }

        var step = stepsCollection.Get(stepKey);

        if (step == null)
        {
            logger.LogError(
                "Can't find step '{stepKey}' for state '{stateName}'. State steps " +
                "for chat '{chatId}' will be marked as all completed.",
                stepKey, state.StateName, update.Chat.Id);

            return AllStepsCompleted;
        }

        state.AddOrUpdateLabel(Constants.StateStepKey, stepKey);
        var stepResult = await step.Execute(new StateContext<TData>(data, update, state, botClientLazy));

        return stepResult switch
        {
            StateResults.CompleteWithNextStepResult completeResult
                => completeResult.WithNextStepKey(stepsCollection.GetNextStepKey(stepKey)),
            StateResults.ContinueWithStepResult continueResult
                => await ProcessStep(data, update, state, continueResult.StepKey),
            { Complete: false } => await ProcessStep(data, update, state, stepsCollection.GetNextStepKey(stepKey)),
            _ => stepResult,
        };
    }
}

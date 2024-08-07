using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

internal class UpdateHandler(
    IUpdateProcessingQueue updateProcessingQueue,
    IServiceProvider serviceProvider,
    ILogger<UpdateHandler> logger)
    : IUpdateHandler
{
    public Task Handle(Update update)
    {
        if (update == null)
            return Task.CompletedTask;

        var user = update.GetUser();
        if (user == null)
        {
            logger.LogError(
                "Can't process update '{updateType}' ({updateId}) because user is null.",
                update.Type, update.Id);

            return Task.CompletedTask;
        }

        var chat = update.GetChat() ?? user.ToChat(); // hack for processing inline queries

        return ProcessUpdate(update, user, chat);
    }

    private async Task ProcessUpdate(Update update, User user, Chat chat)
    {
        logger.LogDebug(
            "Start process update '{updateType}' ({updateId}) for user {userId}.",
            update.Type, update.Id, user.Id);

        var processing = updateProcessingQueue.GetAndAdd(chat.Id);

        try
        {
            await processing.PreviousUpadteProcessing;

            using var scope = serviceProvider.CreateScope();
            var stateStorage = scope.ServiceProvider.GetRequiredService<IStateStorage>();
            var chatUpdate = new ChatUpdate(user, chat, update);

            var state = (await stateStorage.Get(chat.Id)) ?? ChatState.Default(chat.Id);
            var initialStateName = state.StateName;
            string? processingStateName;

            do
            {
                logger.LogDebug("Process state '{stateName}' for user {userId}...", state.StateName, user.Id);

                processingStateName = state.StateName;
                state = await ProcessState(chatUpdate, state, scope.ServiceProvider);

                logger.LogDebug("State '{stateName}' for user {userId} has processed. New state - '{newState}'.",
                    processingStateName, user.Id, state.StateName);

                if (state.ChatId != chatUpdate.Chat.Id)
                    throw new InvalidProgramException(
                        $"State processor for state '{processingStateName}' has return a new state '{state.StateName}' " +
                        $"with chat id '{state.ChatId}' which is not matching chat id for processing update " +
                        $"('{chatUpdate.Chat.Id}'). New state has not been saved.");
            }
            while (state.IsChanged && state.StateName != processingStateName);

            if (state.IsDefault && state.Labels.Count == 0)
                await stateStorage.Delete(state.ChatId);
            else
                await stateStorage.AddOrUpdate(state);

            if (state.IsChanged || state.StateName != initialStateName)
                await SetupNewState(state, chatUpdate, scope.ServiceProvider);
        }
        catch (InvalidProgramException ex)
        {
            logger.LogCritical(ex,
                "Processing of update '{updateType}' for user {userId} failed " +
                "because the program has been developed with mistakes. '{exceptionType}': {message}",
                update.Type, user.Id, TypeHelper.GetShortName(ex.GetType()), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Processing of update '{updateType}' for user {userId} failed " +
                "with exception '{exceptionType}': {message}",
                update.Type, user.Id, TypeHelper.GetShortName(ex.GetType()), ex.Message);
        }
        finally
        {
            processing.CompleteCurrentUpadte();

            logger.LogDebug(
                "Update '{updateType}' ({updateId}) for user {userId} has processed.",
                update.Type, update.Id, user.Id);
        }
    }

    private static Task<ChatState> ProcessState(ChatUpdate update, ChatState state,
        IServiceProvider serviceProvider)
    {
        var stateProcessor = state.IsDefault
            ? serviceProvider.GetService<IStateProcessor>()
            : serviceProvider.GetKeyedService<IStateProcessor>(state.StateName.AsStateKey());

        if (stateProcessor == null)
            throw new InvalidProgramException($"Processor for state '{state.StateName}' is not configured.");

        return stateProcessor.Process(update, state);
    }

    private static Task SetupNewState(ChatState state, ChatUpdate update, IServiceProvider serviceProvider)
    {
        var stateSetupService = state.IsDefault
            ? serviceProvider.GetService<IStateSetupService>()
            : serviceProvider.GetKeyedService<IStateSetupService>(state.StateName.AsStateKey());

        if (stateSetupService == null)
            return Task.CompletedTask;

        return stateSetupService.Setup(state, update);
    }
}

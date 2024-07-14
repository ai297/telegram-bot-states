using System;

namespace Telegram.Bot.States;

internal class ActionsProvider<TCtx>(
    ICommandFactories<StateContext>? globalCommands,
    ICommandFactories<TCtx>? stateCommands,
    IServiceProvider serviceProvider,
    string stateName)
    : IStateActionsProvider
    where TCtx : StateContext
{
    public IAsyncCommand<StateContext, IStateResult>? GetAction(ChatUpdate update, ChatState state)
    {
        if (update.IsCommand)
        {
            var command = GetCommandFactory(stateCommands, update, state)?.Create(serviceProvider, stateName)
                ?? GetCommandFactory(globalCommands, update, state)?.Create(serviceProvider, stateName);

            return command;
        }

        //TODO: add callback queries
        return null;
    }

    private static StateActionFactory<T>? GetCommandFactory<T>(ICommandFactories<T>? commands,
        ChatUpdate update, ChatState state)
        where T : StateContext
    {
        if (commands != null && commands.TryGetValue(update.Command, out var factory) && factory.IsApplicable(update, state))
            return factory;

        return null;
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Telegram.Bot.States;

internal class ActionsProvider<TData>(
    StateActionsCollection<TData> actionsCollection,
    IServiceProvider serviceProvider,
    string stateName) : IStateActionsProvider<TData>
{
    private readonly ReadOnlyDictionary<string, StateCommandFactory<TData>> registeredCommands
        = new(actionsCollection.Commands);

    private readonly ReadOnlyCollection<StateCommandFactory<TData>> registeredActions
        = new(actionsCollection.StateActions);

    public IAsyncCommand<StateContext<TData>, IStateResult>? GetAction(ChatUpdate update, ChatState state)
    {
        if (update.IsCommand)
        {
            return registeredCommands.TryGetValue(update.Command, out var commandFactory) && commandFactory.IsApplicable(update, state)
                ? commandFactory.Create(serviceProvider, stateName)
                : null;
        }

        return registeredActions.FirstOrDefault(af => af.IsApplicable(update, state))?.Create(serviceProvider, stateName);
    }
}

using System;

namespace Telegram.Bot.States;

internal class ActionsProvider(string stateName,
    IActionFactoriesCollection? commandFactories,
    IActionFactoriesCollection? actionFactories)
    : IStateActionsProvider
{
    public IStateAction<StateContext>? GetAction(StateContext context, IServiceProvider serviceProvider)
    {
        if (context.Update.IsCommand && commandFactories != null)
            return commandFactories
                .GetApplicableFactoryIfExists(context)
                ?.Create(serviceProvider, stateName);

        return actionFactories
            ?.GetApplicableFactoryIfExists(context)
            ?.Create(serviceProvider, stateName);
    }
}

using System;

namespace Telegram.Bot.States;

internal class ActionsProvider(string stateName,
    IActionFactoriesCollection? commandFactories,
    IActionFactoriesCollection? callbackFactories)
    : IStateActionsProvider
{
    public IStateAction<StateContext>? GetAction(StateContext context, IServiceProvider serviceProvider)
    {
        if (context.Update.IsCommand && commandFactories != null) return commandFactories
            .GetApplicableFactoryIfExists(context)
            ?.Create(serviceProvider, stateName);

        if (context.Update.IsCallbackQuery && callbackFactories != null) return callbackFactories
            .GetApplicableFactoryIfExists(context)
            ?.Create(serviceProvider, stateName);

        return null;
    }
}

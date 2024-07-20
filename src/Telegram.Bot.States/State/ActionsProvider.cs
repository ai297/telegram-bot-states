using System;

namespace Telegram.Bot.States;

internal class ActionsProvider(
    IActionFactoriesCollection? commandFactories,
    IActionFactoriesCollection? callbackFactories,
    IServiceProvider serviceProvider,
    string stateName)
    : IStateActionsProvider
{
    public IStateAction<StateContext>? GetAction(StateContext context)
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

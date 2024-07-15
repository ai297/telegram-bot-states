using System;

namespace Telegram.Bot.States;

internal class ActionsProvider(
    IActionFactoriesCollection? commandFactories,
    IActionFactoriesCollection? callbackFactories,
    IServiceProvider serviceProvider,
    string stateName)
    : IStateActionsProvider
{
    public IAsyncCommand<StateContext, IStateResult>? GetAction(ChatUpdate update, ChatState state)
    {
        if (update.IsCommand && commandFactories != null) return commandFactories
            .GetApplicableFactoryIfExists(update, state)
            ?.Create(serviceProvider, stateName);

        if (update.IsCallbackQuery && callbackFactories != null) return callbackFactories
            .GetApplicableFactoryIfExists(update, state)
            ?.Create(serviceProvider, stateName);

        return null;
    }
}

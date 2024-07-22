using System;

namespace Telegram.Bot.States;

internal class ActionsProvider<TCtx>(string stateName,
    IActionFactoriesCollection<TCtx>? commandFactories,
    IActionFactoriesCollection<TCtx>? actionFactories)
    : IStateActionsProvider<TCtx> where TCtx : StateContext
{
    public IStateAction<TCtx>? GetAction(TCtx context, IServiceProvider serviceProvider)
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

using System;

namespace Telegram.Bot.States;

public interface IStateActionsProvider<in TCtx> where TCtx : StateContext
{
    IStateAction<TCtx>? GetAction(TCtx context, IServiceProvider serviceProvider);
}

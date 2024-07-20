using System;

namespace Telegram.Bot.States;

public interface IStateActionsProvider
{
    IStateAction<StateContext>? GetAction(StateContext context, IServiceProvider serviceProvider);
}

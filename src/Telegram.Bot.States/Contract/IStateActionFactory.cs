using System;

namespace Telegram.Bot.States;

public interface IStateActionFactory<in TCtx> where TCtx : StateContext
{
    bool IsApplicable(ChatUpdate update, ChatState state);
    IStateAction<TCtx> Create(IServiceProvider serviceProvider, string stateName);
}

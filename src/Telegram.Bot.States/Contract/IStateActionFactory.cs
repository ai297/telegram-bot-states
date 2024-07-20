using System;

namespace Telegram.Bot.States;

public interface IStateActionFactory
{
    bool IsApplicable(ChatUpdate update, ChatState state);
    IStateAction<StateContext> Create(IServiceProvider serviceProvider, string stateName);
}

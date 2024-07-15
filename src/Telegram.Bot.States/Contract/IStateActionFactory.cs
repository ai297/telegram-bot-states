using System;

namespace Telegram.Bot.States;

public interface IStateActionFactory
{
    bool IsApplicable(ChatUpdate update, ChatState state);
    IAsyncCommand<StateContext, IStateResult> Create(IServiceProvider serviceProvider, string stateName);
}

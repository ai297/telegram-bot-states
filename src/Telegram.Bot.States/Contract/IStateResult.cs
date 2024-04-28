namespace Telegram.Bot.States;

public interface IStateResult
{
    bool Complete { get; }
    ChatState GetResultState(ChatUpdate update, ChatState state);
}

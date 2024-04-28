using System.Threading.Tasks;

namespace Telegram.Bot.States;

public interface IStateProcessor
{
    Task<ChatState> Process(ChatUpdate update, ChatState currentState);
}

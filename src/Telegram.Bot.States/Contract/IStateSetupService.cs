using System.Threading.Tasks;

namespace Telegram.Bot.States;

public interface IStateSetupService
{
    Task Setup(ChatState chatState, ChatUpdate update);
}

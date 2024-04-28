using System.Threading.Tasks;

namespace Telegram.Bot.States;

public interface IStateStorage
{
    Task<ChatState?> Get(long chatId);
    Task AddOrUpdate(ChatState state);
    Task Delete(long chatId);
}

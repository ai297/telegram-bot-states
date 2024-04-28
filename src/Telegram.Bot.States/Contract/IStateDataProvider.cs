using System.Threading.Tasks;

namespace Telegram.Bot.States;

public interface IStateDataProvider<TData>
{
    Task<TData> Get(ChatUpdate update);
}

using System.Threading.Tasks;

namespace Telegram.Bot.States;

public interface IStateContextFactory<TCtx> where TCtx : StateContext
{
    Task<TCtx> Create(ChatUpdate chatUpdate, ChatState currentState);
}

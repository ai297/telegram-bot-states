using System.Threading.Tasks;

namespace Telegram.Bot.States;

public interface IAction<in TCtx, out TResult>
{
    TResult Execute(TCtx context);
}

public interface IStateAction<in TCtx> : IAction<TCtx, Task<IStateResult>>
    where TCtx : StateContext
{
}

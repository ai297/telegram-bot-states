using System.Collections.Generic;

namespace Telegram.Bot.States;

public interface ICommandFactories<TCtx> : IReadOnlyDictionary<string, StateActionFactory<TCtx>> where TCtx : StateContext
{
}

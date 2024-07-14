using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Telegram.Bot.States;

internal class CommandFactories<TCtx>(IDictionary<string, StateActionFactory<TCtx>> dictionary)
    : ReadOnlyDictionary<string, StateActionFactory<TCtx>>(dictionary), ICommandFactories<TCtx>
    where TCtx : StateContext
{
}

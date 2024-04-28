using System;
using System.Threading.Tasks;

namespace Telegram.Bot.States;

internal class DelegateDataProvider<TData>(
    Func<ChatUpdate, Task<TData>> @delegate)
    : IStateDataProvider<TData>
{
    public Task<TData> Get(ChatUpdate update) => @delegate(update);
}

using System.Threading.Tasks;

namespace Telegram.Bot.States;

internal class DefaultStateDataProvider : IStateDataProvider<ChatUpdate>
{
    public Task<ChatUpdate> Get(ChatUpdate update) => Task.FromResult(update);
}

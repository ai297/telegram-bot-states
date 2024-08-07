using System.Threading;
using System.Threading.Tasks;

namespace Telegram.Bot.States;

public interface IWebhookController
{
    Task Start(bool dropUpdates, CancellationToken cancellationToken);
    Task Stop(bool dropUpdates, CancellationToken cancellationToken);
    Task Restart(bool dropUpdates, CancellationToken cancellationToken);
}

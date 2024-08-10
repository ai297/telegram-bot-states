using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.States;

public interface IWebhookController
{
    bool IsStarted { get; }
    string CurrentUrl { get; }
    UpdateType[] AllowedUpdates { get; }
    Task Start(bool dropUpdates, CancellationToken cancellationToken);
    Task Stop(bool dropUpdates, CancellationToken cancellationToken);
    Task Restart(bool dropUpdates, CancellationToken cancellationToken);
}

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Telegram.Bot.States;

internal class WebhookService(IWebhookController webhookController) : IHostedLifecycleService
{
    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StartingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken)
        => Task.Delay(1000, cancellationToken).ContinueWith(_
        => webhookController.Start(dropUpdates: false, cancellationToken));

    public Task StopAsync(CancellationToken cancellationToken)
        => webhookController.Stop(dropUpdates: false, cancellationToken);

    public Task StoppingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}

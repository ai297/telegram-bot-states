using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Telegram.Bot.States;

internal class WebhookService(IWebhookController webhookController) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
        => webhookController.Start(dropUpdates: false, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken)
        => webhookController.Stop(dropUpdates: false, cancellationToken);
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Telegram.Bot.States;

internal class WebhookService(IWebhookController webhookController, ILogger<WebhookService> logger)
    : IHostedLifecycleService
{
    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StartingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        try
        {
            await webhookController.Start(dropUpdates: true, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Webhook hasn't been set. {error}", ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => webhookController.Stop(dropUpdates: false, cancellationToken);

    public Task StoppingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

internal class WebhookController(
    ITelegramBotClient botClient,
    IOptions<BotConfiguration> options,
    IBotSetupService setupService,
    ILogger<WebhookController> logger)
    : IWebhookController
{
    private bool isStarted = false;

    public async Task Restart(bool dropUpdates, CancellationToken cancellationToken)
    {
        if (!isStarted) return;

        await Stop(dropUpdates, cancellationToken);
        await Start(dropUpdates, cancellationToken);
    }

    public async Task Start(bool dropUpdates, CancellationToken cancellationToken)
    {
        var config = options.Value;

        if (string.IsNullOrEmpty(config.Token) || string.IsNullOrEmpty(config.HostAddress))
        {
            logger.LogError("Tg bot webhook cannot to be used because Token or HostAdress not configured.");
            return;
        }

        await setupService.Setup();

        var certificate = !string.IsNullOrEmpty(config.CertificatePath)
            ? new InputFileStream(System.IO.File.OpenRead(config.CertificatePath))
            : null;

        var webhookAddress = $"{config.HostAddress}/{config.WebHookPath}/update";

        await botClient.SetWebhookAsync(webhookAddress,
            dropPendingUpdates: dropUpdates,
            certificate: certificate,
            allowedUpdates: config.AllowedUpdates,
            secretToken: config.SecretToken,
            cancellationToken: cancellationToken);

        certificate?.Content.Dispose();

        logger.LogInformation("Tg bot webhook has set to '{adress}'.", webhookAddress);
        isStarted = true;
    }

    public async Task Stop(bool dropUpdates, CancellationToken cancellationToken)
    {
        if (!isStarted) return;

        await botClient.DeleteWebhookAsync(dropUpdates, cancellationToken);

        logger.LogInformation($"Tg bot webhook removed.{(dropUpdates ? " Updates dropped." : "")}");
    }
}

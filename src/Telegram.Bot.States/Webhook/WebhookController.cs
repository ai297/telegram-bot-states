using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.States;

internal class WebhookController(
    ITelegramBotClient botClient,
    IOptions<BotConfiguration> options,
    IBotSetupService setupService,
    ILogger<WebhookController> logger)
    : IWebhookController
{
    private Lazy<WebhookInfo> webhookInfo = new(() => botClient.GetWebhookInfoAsync().GetAwaiter().GetResult());

    private bool? isStarted = null;
    public bool IsStarted => isStarted ??= !string.IsNullOrEmpty(webhookInfo.Value.Url);
    public string CurrentUrl => webhookInfo.Value.Url;
    public UpdateType[] AllowedUpdates => webhookInfo.Value.AllowedUpdates ?? [];

    public async Task Restart(bool dropUpdates, CancellationToken cancellationToken)
    {
        if (IsStarted) await Stop(dropUpdates, cancellationToken);

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

        if (IsStarted && !string.Equals(webhookInfo.Value.Url, config.HostAddress))
            await Stop(dropUpdates, cancellationToken);
        else if (IsStarted)
        {
            logger.LogInformation("Tg bot webhook is set already.");
            return;
        }

        var certificate = !string.IsNullOrEmpty(config.CertificatePath)
            ? new InputFileStream(System.IO.File.OpenRead(config.CertificatePath))
            : null;

        var webhookAddress = $"{config.HostAddress}/{BotConfiguration.WebHookPath}/update";

        await botClient.SetWebhookAsync(webhookAddress,
            dropPendingUpdates: dropUpdates,
            certificate: certificate,
            allowedUpdates: config.AllowedUpdates,
            secretToken: config.SecretToken,
            cancellationToken: cancellationToken);

        certificate?.Content.Dispose();

        logger.LogInformation($"Tg bot webhook has set to '{{adress}}'.{(dropUpdates ? " Updates dropped." : "")}", webhookAddress);
        webhookInfo = new(() => botClient.GetWebhookInfoAsync().GetAwaiter().GetResult());
        isStarted = true;
    }

    public async Task Stop(bool dropUpdates, CancellationToken cancellationToken)
    {
        if (!IsStarted) return;

        await botClient.DeleteWebhookAsync(dropUpdates, cancellationToken);
        webhookInfo = new(() => botClient.GetWebhookInfoAsync().GetAwaiter().GetResult());
        isStarted = false;

        logger.LogInformation($"Tg bot webhook removed.{(dropUpdates ? " Updates dropped." : "")}");
    }
}

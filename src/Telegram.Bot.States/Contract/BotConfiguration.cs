using System.Collections.Generic;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.States;

public class BotConfiguration
{
    public static string WebHookPath { get; set; } = "bot";

    public required string Token { get; set; }
    public required string HostAddress { get; set; }

    /// <summary>
    /// A secret token to be sent in a header "X-Telegram-Bot-Api-Secret-Token" in every webhook request, 1-256 characters.
    /// Only characters A-Z, a-z, 0-9, _ and - are allowed.
    /// The header is useful to ensure that the request comes from a webhook set by you.
    /// </summary>
    public string? SecretToken { get; set; }
    public IList<UpdateType>? AllowedUpdates { get; set; }
    public string? CertificatePath { get; set; }
}

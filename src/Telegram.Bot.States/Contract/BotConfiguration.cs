using System.Collections.Generic;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.States;

public class BotConfiguration
{
    public required string Token { get; set; }
    public required string HostAddress { get; set; }
    public string WebHookPath { get; set; } = "bot";
    public string? SecretToken { get; set; }
    public IList<UpdateType>? AllowedUpdates { get; set; }
    public string? CertificatePath { get; set; }
}

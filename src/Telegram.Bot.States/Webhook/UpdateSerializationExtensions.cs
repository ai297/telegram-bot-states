using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

internal static class UpdateSerializationExtensions
{
    private static JsonSerializerOptions serializerOptions = new()
    {
        AllowTrailingCommas = true,
        MaxDepth = 16,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static async ValueTask<Update?> GetUpdateAsync(this Stream? body, ILogger logger)
    {
        if (body == null) return null;

        var json = await new StreamReader(body).ReadToEndAsync();
        logger.LogDebug("Update received:\n\n{json}\n", json);

        var update = JsonSerializer.Deserialize<Update>(json, serializerOptions);

        return update;
    }
}

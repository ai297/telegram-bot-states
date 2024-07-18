using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace Telegram.Bot.States;

internal static class UpdateSerializationExtensions
{
    public static async ValueTask<Update?> GetUpdateAsync(this Stream? body)
    {
        if (body == null)
            return null;

        var json = await new StreamReader(body).ReadToEndAsync();
        var update = JsonConvert.DeserializeObject<Update>(json);

        return update;
    }
}

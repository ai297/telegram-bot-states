using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Telegram.Bot.States;

public static class BotEndpointsExtensions
{
    public static IEndpointConventionBuilder MapBotWebhook(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost($"/{BotConfiguration.WebHookPath}/update", async (HttpRequest request, ILogger<UpdateHandler> logger) =>
        {
            if (!request.HasJsonContentType())
                return Results.BadRequest();

            var update = await request.Body.GetUpdateAsync(logger);

            if (update is null)
                return Results.BadRequest();

            var handler = request.HttpContext.RequestServices
                .GetRequiredService<IUpdateHandler>();

            await handler.Handle(update);

            return Results.Ok();
        })
        .AddEndpointFilter<SecretTokenFilter>();
    }
}

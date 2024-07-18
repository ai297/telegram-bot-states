using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Telegram.Bot.States;

public static class BotEndpointsExtensions
{
    public static IEndpointConventionBuilder MapBotWebhook(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost($"/{BotConfiguration.WebHookPath}/update", async (HttpRequest request) =>
        {
            if (!request.HasJsonContentType())
                return Results.BadRequest();

            var update = await request.Body.GetUpdateAsync();

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

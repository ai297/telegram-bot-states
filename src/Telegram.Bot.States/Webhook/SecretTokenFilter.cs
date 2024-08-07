using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Telegram.Bot.States;

internal class SecretTokenFilter(IOptions<BotConfiguration> options) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (IsValidToken(options.Value.SecretToken, context.HttpContext.Request.Headers))
            return next(context);

        return ValueTask.FromResult((object?)Results.Forbid());
    }

    private static bool IsValidToken(string? secretToken, IHeaderDictionary headers)
        => string.IsNullOrEmpty(secretToken)
        || (headers.TryGetValue(Constants.SecretTokenHeader, out var header)
        && string.Equals(secretToken, header.ToString()));
}
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.States;

public class StateContext<TData>(TData data, ChatUpdate update, ChatState state, Lazy<ITelegramBotClient> botClient)
    : StateContext(update, state, botClient)
{
    public readonly TData Data = data;
}

public class StateContext(ChatUpdate update, ChatState state, Lazy<ITelegramBotClient> botClient) : ITelegramBotClient
{
    public readonly ChatUpdate Update = update;
    public readonly ChatState State = state;

    #region  Bot Client

    public long BotId => botClient.Value.BotId;
    bool ITelegramBotClient.LocalBotServer => botClient.Value.LocalBotServer;
    TimeSpan ITelegramBotClient.Timeout { get => botClient.Value.Timeout; set => botClient.Value.Timeout = value; }
    IExceptionParser ITelegramBotClient.ExceptionsParser
    {
        get => botClient.Value.ExceptionsParser;
        set => botClient.Value.ExceptionsParser = value;
    }

    event AsyncEventHandler<ApiRequestEventArgs>? ITelegramBotClient.OnMakingApiRequest
    {
        add => botClient.Value.OnMakingApiRequest += value;
        remove => botClient.Value.OnMakingApiRequest -= value;
    }

    event AsyncEventHandler<ApiResponseEventArgs>? ITelegramBotClient.OnApiResponseReceived
    {
        add => botClient.Value.OnApiResponseReceived += value;
        remove => botClient.Value.OnApiResponseReceived -= value;
    }

    public Task DownloadFileAsync(string filePath, Stream destination, CancellationToken cancellationToken = default)
        => botClient.Value.DownloadFileAsync(filePath, destination, cancellationToken);
    public Task<TResponse> MakeRequestAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        => botClient.Value.MakeRequestAsync(request, cancellationToken);
    public Task<bool> TestApiAsync(CancellationToken cancellationToken = default)
        => botClient.Value.TestApiAsync(cancellationToken);

    public Task<Message> SendTextMessageBack(string text,
        int? messageThreadId = null,
        ParseMode parseMode = ParseMode.None,
        LinkPreviewOptions? linkPreviewOptions = null,
        bool disableNotification = false,
        bool protectContent = false,
        string? messageEffectId = null,
        ReplyParameters? replyParameters = null,
        IReplyMarkup? replyMarkup = null,
        string? businessConnectionId = null,
        CancellationToken cancellationToken = default)
        => this.SendTextMessageAsync(Update.ChatId, text,
            messageThreadId: messageThreadId,
            parseMode: parseMode,
            entities: null,
            linkPreviewOptions: linkPreviewOptions,
            disableNotification: disableNotification,
            protectContent: protectContent,
            messageEffectId: messageEffectId,
            replyParameters: replyParameters,
            replyMarkup: replyMarkup,
            businessConnectionId: businessConnectionId,
            cancellationToken);

    public Task AnswerCallback(string? text = null, bool showAlert = false, string? url = null, int? cacheTime = null,
        CancellationToken cancellationToken = default)
        => !Update.IsCallbackQuery
        ? Task.CompletedTask
        : this.AnswerCallbackQueryAsync(Update.Update.CallbackQuery!.Id,
            text, showAlert, url, cacheTime, cancellationToken);

    #endregion
}

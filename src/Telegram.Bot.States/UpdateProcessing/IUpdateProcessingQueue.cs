namespace Telegram.Bot.States;

internal interface IUpdateProcessingQueue
{
    UpdateProcessingQueueItem GetAndAdd(long chatId);
}
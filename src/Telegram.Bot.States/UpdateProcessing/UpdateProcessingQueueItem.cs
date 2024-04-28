using System;
using System.Threading.Tasks;

namespace Telegram.Bot.States;

internal struct UpdateProcessingQueueItem(
    Action completeUpdateAction,
    Task previousUpdateTask)
{
    public readonly Task PreviousUpadteProcessing = previousUpdateTask;
    public void CompleteCurrentUpadte() => completeUpdateAction();
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Telegram.Bot.States;

internal class UpdateProcessingQueue : IUpdateProcessingQueue
{
    private static readonly TimeSpan QueueLifeTime = TimeSpan.FromMinutes(10);

    private readonly Dictionary<long, Queue<Task>> chatProcessingQueues = [];
    private readonly object lockObj = new();

    public UpdateProcessingQueueItem GetAndAdd(long chatId)
    {
        Queue<Task> chatProcessingQueue;

        lock(lockObj)
        {
            if (!chatProcessingQueues.TryGetValue(chatId, out chatProcessingQueue!))
            {
                chatProcessingQueue = [];
                chatProcessingQueues.Add(chatId, chatProcessingQueue);
            }
        }

        var currentUpateSource = new TaskCompletionSource();
        var removeTask = Task.Delay(QueueLifeTime).ContinueWith(_ => RemoveIfEmpty(chatId));

        Task previousProcessingTask;

        lock(chatProcessingQueue)
        {
            previousProcessingTask = chatProcessingQueue.Count > 0
                ? chatProcessingQueue.Dequeue()
                : Task.CompletedTask;

            chatProcessingQueue.Enqueue(Task.WhenAny(currentUpateSource.Task, removeTask));
        }

        return new(currentUpateSource.SetResult, previousProcessingTask);
    }

    private void RemoveIfEmpty(long chatId)
    {
        if (!chatProcessingQueues.TryGetValue(chatId, out var queue))
            return;

        if (queue.Count > 0)
            return;

        lock (lockObj)
        {
            chatProcessingQueues.Remove(chatId);
        }
    }
}

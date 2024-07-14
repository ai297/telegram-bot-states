using System;
using System.Threading.Tasks;

namespace Telegram.Bot.States;

public static class TaskExtensions
{
    public static Task<TResult> ContinueWithMap<TResult, TData>(this Task<TData> originalTask, Func<TData, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(originalTask);
        ArgumentNullException.ThrowIfNull(mapper);

        var tcs = new TaskCompletionSource<TResult>();
        originalTask.ContinueWith((task) =>
        {
            if (task.IsCompletedSuccessfully)
            {
                tcs.SetResult(mapper(task.Result));
            }
            else if (task.IsFaulted)
            {
                if (task.Exception?.InnerExceptions != null)
                    tcs.SetException(task.Exception.InnerExceptions);
                else
                    tcs.SetException(new Exception("Original task is faulted without exceptions."));
            }
            else if (task.IsCanceled)
            {
                tcs.SetCanceled();
            }
        });

        return tcs.Task;
    }
}

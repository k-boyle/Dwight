using System;
using System.Threading.Tasks;

namespace Dwight;

public class ScheduledTask : IScheduledTask
{
    private static int _taskCounter;

    public DateTimeOffset ExecuteAt { get; }
    public Func<Task> Callback { get; }
    public string Name { get; }
    public bool IsCancelled { get; private set; }

    private readonly TaskCompletionSource<bool> _taskCompletionSource;

    public ScheduledTask(DateTimeOffset executeAt, Func<Task> callback)
        : this(null, executeAt, callback)
    {
    }

    public ScheduledTask(string? name, DateTimeOffset executeAt,Func<Task> callback)
    {
        Name = name ?? string.Concat("Task: ", _taskCounter++.ToString());
        ExecuteAt = executeAt;
        Callback = callback;
        IsCancelled = false;
        this._taskCompletionSource = new();
    }

    public void Cancel()
    {
        IsCancelled = true;
    }

    public async Task WaitUntilExecutedAsync()
    {
        await this._taskCompletionSource.Task;
    }

    void IScheduledTask.Completed()
    {
        this._taskCompletionSource.SetResult(true);
    }

    public int CompareTo(IScheduledTask? other)
    {
        return ExecuteAt.CompareTo(other!.ExecuteAt);
    }
}
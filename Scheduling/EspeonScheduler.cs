using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Disqord.Bot.Hosting;
using Microsoft.Extensions.Logging;

namespace Dwight;

public sealed class EspeonScheduler : DiscordBotService
{
    private static readonly TimeSpan MAX_DELAY = TimeSpan.FromMilliseconds(int.MaxValue);

    public event Action<Exception> OnError;

    private readonly ILogger<EspeonScheduler> _logger;
    private readonly LockedBinaryHeap<IScheduledTask> _tasks;
    private readonly ConcurrentQueue<IScheduledTask> _doNowTasks;

    private CancellationTokenSource _cts;
    private volatile bool _disposed;

    public EspeonScheduler(ILogger<EspeonScheduler> logger)
    {
        _logger = logger;
        _tasks = LockedBinaryHeap<IScheduledTask>.CreateMinHeap();
        _doNowTasks = new();
        _cts = new();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting up scheduler");
            
        while (!_disposed && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogTrace("Scheduler loop");
                
            try
            {
                _logger.LogDebug("Executing {Count} do now tasks", _doNowTasks.Count);
                    
                while (_doNowTasks.TryDequeue(out var task)) 
                    await ExecuteTaskAsync(task, false);

                if (_tasks.IsEmpty) 
                    await Task.Delay(-1, _cts.Token);

                var nextTask = _tasks.Root;
                    
                TimeSpan executeIn;
                _logger.LogDebug("Waiting for {Task} in {Duration}", nextTask!.Name, nextTask.ExecuteAt);
                while ((executeIn = nextTask.ExecuteAt - DateTimeOffset.UtcNow) > MAX_DELAY) 
                    await Task.Delay(MAX_DELAY, _cts.Token);

                if (executeIn > TimeSpan.Zero) 
                    await Task.Delay(executeIn, _cts.Token);

                await ExecuteTaskAsync(nextTask, true);
            }
            catch (TaskCanceledException)
            {
                _cts.Dispose();
                _cts = new();
            }
        }
    }

    private async Task ExecuteTaskAsync(IScheduledTask task, bool removeRoot)
    {
        try
        {
            if (!task.IsCancelled)
            {
                _logger.LogDebug("Executing {Task}", task.Name);
                await task.Callback();
            }
            else
            {
                _logger.LogDebug("{Task} was cancelled", task.Name);
            }
        }
        catch (Exception eventException)
        {
            try
            {
                OnError?.Invoke(eventException);
            }
            catch (Exception onErrorException)
            {
                _logger.LogError(onErrorException, "Exception thrown by OnError handler");
            }
        }
        finally
        {
            task.Completed();
                
            if (removeRoot)
                _tasks.TryRemoveRoot(out _);
        }
    }

    public ScheduledTask DoNow(Func<Task> callback)
    {
        return DoNow(null!, callback);
    }
        
    public ScheduledTask DoNow(string name, Func<Task> callback)
    {
        CheckNotDisposed();

        var newTask = new ScheduledTask(name, DateTimeOffset.UtcNow, callback);
        _logger.LogDebug("Queueing up {Task}", newTask.Name);
        _doNowTasks.Enqueue(newTask);
        _cts.Cancel(true);
        return newTask;
    }
        
    public ScheduledTask<T> DoNow<T>(T state, Func<T, Task> callback)
    {
        return DoNow(null, state, callback);
    }

    public ScheduledTask<T> DoNow<T>(string? name, T state, Func<T, Task> callback)
    {
        CheckNotDisposed();

        var newTask = new ScheduledTask<T>(name, DateTimeOffset.UtcNow, state, callback);
        _logger.LogDebug("Queueing up {Task}", newTask.Name);
        _doNowTasks.Enqueue(newTask);
        _cts.Cancel(true);
        return newTask;
    }

    public ScheduledTask DoIn(TimeSpan executeIn, Func<Task> callback)
    {
        return DoIn(null, executeIn, callback);
    }
        
    public ScheduledTask DoIn(string? name, TimeSpan executeIn, Func<Task> callback)
    {
        if (executeIn == TimeSpan.MaxValue)
        {
            throw new InvalidOperationException($"Can only execute up to {DateTimeOffset.MaxValue - DateTimeOffset.UtcNow}");
        }

        return DoAt(name, DateTimeOffset.UtcNow + executeIn, callback);
    }
        
    public ScheduledTask<T> DoIn<T>(TimeSpan executeIn, T state, Func<T, Task> callback)
    {
        return DoIn(null, executeIn, state, callback);
    }

    public ScheduledTask<T> DoIn<T>(string? name, TimeSpan executeIn, T state, Func<T, Task> callback)
    {
        if (executeIn == TimeSpan.MaxValue)
        {
            throw new InvalidOperationException($"Can only execute up to {DateTimeOffset.MaxValue - DateTimeOffset.UtcNow}");
        }

        return DoAt(name, DateTimeOffset.UtcNow + executeIn, state, callback);
    }

    public ScheduledTask DoAt(DateTimeOffset executeAt, Func<Task> callback)
    {
        return DoAt(null, executeAt, callback);
    }
        
    public ScheduledTask DoAt(string? name, DateTimeOffset executeAt, Func<Task> callback)
    {
        CheckNotDisposed();

        var newTask = new ScheduledTask(name, executeAt, callback);
        _logger.LogDebug("Queueing up {task}", newTask.Name);
        var root = _tasks.Root;
        _tasks.Insert(newTask);

        if (!ReferenceEquals(root, _tasks.Root))
        {
            _cts.Cancel(true);
        }

        return newTask;
    }
        
    public ScheduledTask<T> DoAt<T>(DateTimeOffset executeAt, T state, Func<T, Task> callback)
    {
        return DoAt(null, executeAt, state, callback);
    }

    public ScheduledTask<T> DoAt<T>(string name, DateTimeOffset executeAt, T state, Func<T, Task> callback)
    {
        CheckNotDisposed();

        var newTask = new ScheduledTask<T>(name, executeAt, state, callback);
        _logger.LogDebug("Queueing up {task}", newTask.Name);
        var root = _tasks.Root;
        _tasks.Insert(newTask);

        if (!ReferenceEquals(root, _tasks.Root))
        {
            _cts.Cancel(true);
        }

        return newTask;
    }

    private void CheckNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EspeonScheduler));
        }
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cts.Cancel(true);
        _cts.Dispose();
        _cts = null;
    }
}
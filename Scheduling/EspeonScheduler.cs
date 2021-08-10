using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Dwight
{
    public sealed class EspeonScheduler : IDisposable
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
            this._logger = logger;
            this._tasks = LockedBinaryHeap<IScheduledTask>.CreateMinHeap();
            this._doNowTasks = new();
            this._cts = new();
            _ = TaskLoopAsync();
        }

        private async Task TaskLoopAsync()
        {
            while (!this._disposed)
            {
                try
                {
                    while (this._doNowTasks.TryDequeue(out var task))
                    {
                        await ExecuteTaskAsync(task, _ => { });
                    }

                    if (this._tasks.IsEmpty)
                    {
                        await Task.Delay(-1, this._cts.Token);
                    }
                    
                    var nextTask = this._tasks.Root;
                    
                    TimeSpan executeIn;
                    this._logger.LogDebug("Waiting for {task} in {duration}", nextTask.Name, nextTask.ExecuteAt);
                    while ((executeIn = nextTask.ExecuteAt - DateTimeOffset.Now) > MAX_DELAY)
                    {
                        await Task.Delay(MAX_DELAY, this._cts.Token);
                    }

                    if (executeIn > TimeSpan.Zero)
                    {
                        await Task.Delay(executeIn, this._cts.Token);
                    }
                    
                    await ExecuteTaskAsync(nextTask, @this => @this._tasks.TryRemoveRoot(out _));
                }
                catch (TaskCanceledException)
                {
                    this._cts.Dispose();
                    this._cts = new();
                }
            }
        }

        private async Task ExecuteTaskAsync(IScheduledTask task, Action<EspeonScheduler> cleanup)
        {
            try
            {
                if (!task.IsCancelled)
                {
                    this._logger.LogDebug("Executing {task}", task.Name);
                    await task.Callback();
                }
                else
                {
                    this._logger.LogDebug("{task} was cancelled", task.Name);
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
                    this._logger.LogError(onErrorException, "Exception thrown by OnError handler");
                }
            }
            finally
            {
                task.Completed();
                cleanup(this);
            }
        }

        public ScheduledTask<T> DoNow<T>(T state, Func<T, Task> callback)
        {
            return DoNow(null, state, callback);
        }

        public ScheduledTask<T> DoNow<T>(string? name, T state, Func<T, Task> callback)
        {
            CheckNotDisposed();

            var newTask = new ScheduledTask<T>(name, DateTimeOffset.Now, state, callback);
            this._logger.LogDebug("Queueing up {task}", newTask.Name);
            this._doNowTasks.Enqueue(newTask);
            this._cts.Cancel(true);
            return newTask;
        }

        public ScheduledTask<T> DoIn<T>(TimeSpan executeIn, T state, Func<T, Task> callback)
        {
            return DoIn(null, executeIn, state, callback);
        }

        public ScheduledTask<T> DoIn<T>(string? name, TimeSpan executeIn, T state, Func<T, Task> callback)
        {
            if (executeIn == TimeSpan.MaxValue)
            {
                throw new InvalidOperationException($"Can only execute up to {DateTimeOffset.MaxValue - DateTimeOffset.Now}");
            }

            return DoAt(name, DateTimeOffset.Now + executeIn, state, callback);
        }

        public ScheduledTask<T> DoAt<T>(DateTimeOffset executeAt, T state, Func<T, Task> callback)
        {
            return DoAt(null, executeAt, state, callback);
        }

        public ScheduledTask<T> DoAt<T>(string? name, DateTimeOffset executeAt, T state, Func<T, Task> callback)
        {
            CheckNotDisposed();

            var newTask = new ScheduledTask<T>(name, executeAt, state, callback);
            this._logger.LogDebug("Queueing up {task}", newTask.Name);
            var root = this._tasks.Root;
            this._tasks.Insert(newTask);

            if (!ReferenceEquals(root, this._tasks.Root))
            {
                this._cts.Cancel(true);
            }

            return newTask;
        }

        private void CheckNotDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(nameof(EspeonScheduler));
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            if (!disposing)
            {
                return;
            }

            this._disposed = true;
            this._cts.Cancel(true);
            this._cts.Dispose();
            this._cts = null!;
        }
    }
}
using System;
using System.Threading.Tasks;

namespace Dwight
{
    public interface IScheduledTask : IComparable<IScheduledTask>
    {
        public DateTimeOffset ExecuteAt { get; }
        public Func<Task> Callback { get; }
        public string Name { get; }
        public bool IsCancelled { get; }

        public Task WaitUntilExecutedAsync();

        internal void Completed();
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterBackoff
{
    public interface IRetrier
    {
        internal int MaxRetryCount { get; set; }
        internal TimeSpan InitialRetryDelay { get; set; }
        internal TimeSpan MaximumRetryDelay { get; set; }
        internal Func<TimeSpan, TimeSpan> GetNextTimeSpan { get; set; }
        internal Func<TimeSpan, CancellationToken, Task> DelayCallback { get; set; }
        internal Func<Random> GetNewRandom { get; set; }

        Task ExecuteAsync(Func<Task> callback, CancellationToken cancellationToken = default);

        Task<T> ExecuteAsync<T>(Func<Task<T>> callback, CancellationToken cancellationToken = default);

        ICollection<(Type, Func<Exception, RetryAction>)> ExceptionHandlers { get; }
    }
}
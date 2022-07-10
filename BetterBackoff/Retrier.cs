using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("BetterBackoff.Tests")]

namespace BetterBackoff
{
    public class Retrier : IRetrier
    {
        int IRetrier.MaxRetryCount { get; set; } = 10;
        TimeSpan IRetrier.InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(.25d);
        TimeSpan IRetrier.MaximumRetryDelay { get; set; } = TimeSpan.FromSeconds(30d);

        Func<TimeSpan, TimeSpan> IRetrier.GetNextTimeSpan { get; set; } = t => TimeSpan.FromSeconds(t.TotalSeconds * 2d);
        Func<TimeSpan, CancellationToken, Task> IRetrier.DelayCallback { get; set; } = async (t, c) => await Task.Delay(t, c);
        Func<Random> IRetrier.GetNewRandom { get; set; } = () => new Random(Guid.NewGuid().GetHashCode());

        ICollection<(Type, Func<Exception, RetryAction>)> IRetrier.ExceptionHandlers { get; }
            = new List<(Type, Func<Exception, RetryAction>)>();

        public async Task ExecuteAsync(Func<Task> callback, CancellationToken cancellationToken = default)
        {
            if (callback is null) { throw new ArgumentNullException(nameof(callback)); }
            var errs = new List<Exception>();
            var @this = this as IRetrier;
            var delay = @this.InitialRetryDelay;

            for (int i = 0; i < @this.MaxRetryCount; i++)
            {
                if (i > 0)
                {
                    await @this.DelayCallback(delay, cancellationToken);
                    delay = @this.GetNextTimeSpan(delay);
                    if (delay > @this.MaximumRetryDelay)
                    { delay = @this.MaximumRetryDelay; }
                }
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await callback();
                    return;
                }
                catch (Exception ex)
                {
                    foreach (var o in @this.ExceptionHandlers)
                    {
                        foreach (var _ex in new[] { ex, ex.GetBaseException() }.Distinct())
                        {
                            if (o.Item1.IsAssignableFrom(_ex.GetType()))
                            {
                                var result = o.Item2(_ex);
                                if (result == RetryAction.Stop) { return; }
                                else if (result == RetryAction.Throw) { throw _ex; }
                            }
                        }
                    }
                    errs.Add(ex);
                }
            }

            throw new AggregateException(errs);
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> callback, CancellationToken cancellationToken = default)
        {
            T result = default;
            await ExecuteAsync
            (
                async () =>
                {
                    result = await callback();
                },
                cancellationToken
            );

            return result;
        }
    }
}
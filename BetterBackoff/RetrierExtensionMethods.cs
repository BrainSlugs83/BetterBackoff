using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterBackoff
{
    public static class RetrierExtensionMethods
    {
        public static IRetrier WithInitialRetryDelay(this IRetrier @this, TimeSpan initialRetryDelay)
        {
            if (@this is null) { throw new NullReferenceException(); }
            if (initialRetryDelay <= TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(initialRetryDelay)); }

            @this.InitialRetryDelay = initialRetryDelay;
            return @this;
        }

        public static IRetrier WithMaximumRetryDelay(this IRetrier @this, TimeSpan maximumRetryDelay)
        {
            if (@this is null) { throw new NullReferenceException(); }
            if (maximumRetryDelay <= TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(maximumRetryDelay)); }

            @this.MaximumRetryDelay = maximumRetryDelay;
            return @this;
        }

        public static IRetrier WithInitialRetryDelay(this IRetrier @this, double initialRetryDelayInSeconds)
            => @this.WithInitialRetryDelay(TimeSpan.FromSeconds(initialRetryDelayInSeconds));

        public static IRetrier WithMaximumRetryDelay(this IRetrier @this, double maximumRetryDelayInSeconds)
            => @this.WithMaximumRetryDelay(TimeSpan.FromSeconds(maximumRetryDelayInSeconds));

        public static IRetrier WithMaximumRetryCount(this IRetrier @this, int maximumRetryCount)
        {
            if (@this is null) { throw new NullReferenceException(); }
            if (maximumRetryCount < 0) { throw new ArgumentOutOfRangeException(nameof(maximumRetryCount), maximumRetryCount, "Argment must be greater than or equal to zero."); }
            @this.MaxRetryCount = maximumRetryCount;
            return @this;
        }

        public static IRetrier WithExponentialBackOff(this IRetrier @this, double stepMultiplier = 2.0d)
        {
            if (@this is null) { throw new NullReferenceException(); }
            @this.GetNextTimeSpan = t => TimeSpan.FromSeconds(t.TotalSeconds * stepMultiplier);
            return @this;
        }

        public static IRetrier WithStochasticExponentialBackOff(this IRetrier @this, double minimumStepMultiplier = 1.5d, double maximumStepMultiplier = 2.5d)
        {
            if (@this is null) { throw new NullReferenceException(); }
            var rnd = @this.GetNewRandom();

            @this.GetNextTimeSpan = t => TimeSpan.FromSeconds
            (
                t.TotalSeconds *
                (
                    minimumStepMultiplier +
                    (
                        rnd.NextDouble() *
                        (maximumStepMultiplier - minimumStepMultiplier)
                    )
                )
            );

            return @this;
        }

        public static void Execute(this IRetrier @this, Action callback, CancellationToken cancellationToken = default)
        {
            if (@this is null) { throw new NullReferenceException(); }
            if (callback is null) { throw new ArgumentNullException(nameof(callback)); }

            @this.ExecuteAsync(() => Task.Run(callback), cancellationToken)
                .ConfigureAwait(true).GetAwaiter().GetResult();
        }

        public static T Execute<T>(this IRetrier @this, Func<T> callback, CancellationToken cancellationToken = default)
        {
            if (@this is null) { throw new NullReferenceException(); }
            if (callback is null) { throw new ArgumentNullException(nameof(callback)); }
            if (typeof(Task).IsAssignableFrom(typeof(T)))
            { throw new InvalidOperationException($"Async methods must be executed with the {nameof(IRetrier.ExecuteAsync)} methods."); }

            return @this.ExecuteAsync(() => Task.Run(() => { return callback(); }), cancellationToken)
                    .ConfigureAwait(true).GetAwaiter().GetResult();
        }

        public static IRetrier FailOn<T>(this IRetrier @this) where T : Exception
        {
            if (@this is null) { throw new NullReferenceException(); }
            @this.ExceptionHandlers.Add((typeof(T), o => RetryAction.Throw));
            return @this;
        }

        public static IRetrier Handle<T>(this IRetrier @this, Func<T, RetryAction> handlerCallback)
            where T : Exception
        {
            if (@this is null) { throw new NullReferenceException(); }
            @this.ExceptionHandlers.Add((typeof(T), ex => handlerCallback((T)ex)));
            return @this;
        }

        public static IRetrier Handle<T>(this IRetrier @this, RetryAction retryAction)
            where T : Exception
        {
            return @this.Handle<T>(ex => retryAction);
        }
    }
}
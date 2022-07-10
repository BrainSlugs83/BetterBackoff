Reusable Retry Logic for transient fault handling with exponential backoff and stochastic exponential backoff.
Example usage:
```
new Retrier().WithMaximumRetryCount(5)
    .WithInitialRetryDelay(TimeSpan.FromSeconds(3))
    .WithStochasticExponentialBackOff()
    .Execute
    (
        () =>
        {
            // stuff that fails sometimes. 😅
        }
    );
```
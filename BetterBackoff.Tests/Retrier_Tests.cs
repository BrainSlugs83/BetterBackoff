using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using BetterBackoff;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BetterBackoff.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class Retrier_Tests
    {
        [TestMethod]
        public void Execute_Test()
        {
            var s = new SucceedAfterNTries(5);
            new Retrier().WithMaximumRetryCount(5)
                .WithInitialRetryDelay(.01d)
                .WithStochasticExponentialBackOff()
                .Handle<InvalidOperationException>(ex => RetryAction.Continue)
                .Execute(s.Run);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ExecuteWithTaskResult_Test()
        {
            await new Retrier()
                .Handle<InvalidOperationException>(ex => RetryAction.Continue)
                .Execute(() => Task.CompletedTask);
        }

        [TestMethod]
        public void ExecuteWithResult_Test()
        {
            var s = new SucceedAfterNTries(5);
            var result = new Retrier().WithMaximumRetryCount(5).WithInitialRetryDelay(.01d).Execute(() => s.RunWithResult(42));
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void ExecuteWithResultNull_Test()
        {
            var s = new SucceedAfterNTries(5);
            var result = ((IRetrier)null).Execute(() => s.RunWithResult(42));
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ExecuteWithResultNullCallback_Test()
        {
            var s = new SucceedAfterNTries(5);
            var result = new Retrier().Execute((Func<int>)null);
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ExecuteNullCallback_Test()
        {
            new RetryWrapper().Retrier.WithMaximumRetryCount(1).Execute(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ExecuteAsyncNullCallback_Test()
        {
            await new RetryWrapper().Retrier.WithMaximumRetryCount(1).ExecuteAsync(null);
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void ExecuteFails_Test()
        {
            var s = new SucceedAfterNTries(5);
            new RetryWrapper().Retrier.WithMaximumRetryCount(4).Execute(s.Run);
        }

        [TestMethod]
        public void ExecuteDelayedRetry_Test()
        {
            var s = new SucceedAfterNTries(5);
            var w = new RetryWrapper();

            w.Retrier.WithMaximumRetryCount(5)
                .WithExponentialBackOff(2)
                .WithInitialRetryDelay(TimeSpan.FromSeconds(1));

            Assert.AreEqual(0d, w.TotalSeconds);
            w.Retrier.Execute(s.Run);
            Assert.AreEqual(15d, w.TotalSeconds);
        }

        [TestMethod]
        public void ExecuteWithMaximumDelayedRetry_Test()
        {
            var s = new SucceedAfterNTries(10);
            var w = new RetryWrapper();

            w.Retrier.WithMaximumRetryCount(10)
                .WithExponentialBackOff(2)
                .WithInitialRetryDelay(1d)
                .WithMaximumRetryDelay(5d);

            Assert.AreEqual(0d, w.TotalSeconds);
            w.Retrier.Execute(s.Run);
            Assert.AreEqual(37d, w.TotalSeconds);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void WithStochasticExponentialBackOffNull_Test()
        {
            ((IRetrier)null).WithStochasticExponentialBackOff();
        }

        [TestMethod]
        public void ExecuteStochasticDelayedRetry_Test()
        {
            var s = new SucceedAfterNTries(5);
            var w = new RetryWrapper();

            w.Retrier.WithMaximumRetryCount(5)
                .WithExponentialBackOff(2)
                .WithInitialRetryDelay(TimeSpan.FromSeconds(1))
                .WithStochasticExponentialBackOff();

            Assert.AreEqual(0d, w.TotalSeconds);
            w.Retrier.Execute(s.Run);
            Assert.AreEqual(12.51d, Math.Round(w.TotalSeconds, 2));
        }

        private class RetryWrapper
        {
            public double TotalSeconds { get; set; } = 0;

            public IRetrier Retrier { get; } = new Retrier();

            public RetryWrapper()
            {
                Retrier.DelayCallback = async (a, b) => { await Task.CompletedTask; this.TotalSeconds += a.TotalSeconds; };
                Retrier.GetNewRandom = () => new Random(42);
            }
        }

        private class SucceedAfterNTries
        {
            private int NumTries;

            public SucceedAfterNTries(int numTries)
            { this.NumTries = numTries; }

            public void Run()
            {
                NumTries--;
                if (NumTries == 4) { throw new EndOfStreamException("Inner", new InvalidOperationException("Outer.")); }
                if (NumTries == 3) { throw new EndOfStreamException(); }
                else if (NumTries > 0) { throw new InvalidOperationException("I failed on purpose."); }
            }

            public T RunWithResult<T>(T result)
            {
                NumTries--;
                if (NumTries == 4) { throw new EndOfStreamException("Inner", new InvalidOperationException("Outer.")); }
                else if (NumTries == 3) { throw new EndOfStreamException(); }
                else if (NumTries > 0) { throw new InvalidOperationException("I failed on purpose."); }
                return result;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void ExecuteNull_Test()
        {
            ((IRetrier)null).Execute(() => { });
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public async Task ExecuteAsyncNull_Test()
        {
            await ((IRetrier)null).ExecuteAsync(async () => { await Task.CompletedTask; });
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void WithInitialRetryDelayNull_Test()
        {
            ((IRetrier)null).WithInitialRetryDelay(5d);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void WithInitialRetryDelayOutOfRange_Test()
        {
            new Retrier().WithInitialRetryDelay(0);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void WithMaximumRetryCountNull_Test()
        {
            ((IRetrier)null).WithMaximumRetryCount(5);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void WithNegativeMaximumRetryCount_Test()
        {
            new Retrier().WithMaximumRetryCount(-5);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void WithExponentialBackOffNull_Test()
        {
            ((IRetrier)null).WithExponentialBackOff();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void FailOn_Test()
        {
            var s = new SucceedAfterNTries(5);
            new Retrier()
                .FailOn<InvalidOperationException>()
                .Execute(() => s.Run());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void FailOnBaseException_Test()
        {
            var s = new SucceedAfterNTries(5);
            new Retrier()
                .FailOn<InvalidOperationException>()
                .Execute(() => { throw new EndOfStreamException("Outer", new InvalidOperationException("Inner")); });
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void FailOnNull_Test()
        {
            ((IRetrier)null).FailOn<InvalidOperationException>();
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void HandleNull_Test()
        {
            ((IRetrier)null).Handle<InvalidOperationException>(e => RetryAction.Stop);
        }

        [TestMethod]
        public void HandleStop_Test()
        {
            var s = new SucceedAfterNTries(5);
            new Retrier().WithMaximumRetryCount(3)
                .WithInitialRetryDelay(.01d)
                .WithStochasticExponentialBackOff()
                .Handle<EndOfStreamException>(RetryAction.Stop)
                .Execute(s.Run);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void WithMaximumRetryDelayNull_Test()
        {
            ((IRetrier)null).WithMaximumRetryDelay(5d);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void WithMaximumRetryDelayOutOfRange_Test()
        {
            new Retrier().WithMaximumRetryDelay(0);
        }
    }
}
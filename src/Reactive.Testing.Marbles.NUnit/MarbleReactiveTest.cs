using System;
using System.Collections.Generic;
using Microsoft.Reactive.Testing;
using NUnit.Framework;

namespace Reactive.Testing.Marbles.NUnit
{
    public abstract class MarbleReactiveTest : ReactiveTest
    {
        readonly long _frameTimeFactor;
        protected MarbleTestScheduler? Scheduler { get; private set; }

        protected MarbleReactiveTest(long frameTimeFactor = MarbleTestScheduler.DefaultFrameTimeFactor)
        {
            _frameTimeFactor = frameTimeFactor;
        }

        [SetUp]
        public void MarbleSetUp()
        {
            Scheduler = new MarbleTestScheduler(_frameTimeFactor, new NUnitAssertFactory());
        }

        [TearDown]
        public void MarbleTearDown()
        {
            Scheduler?.Flush();
        }

        void EnsureScheduler()
        {
            if (Scheduler == null) throw new InvalidOperationException("Scheduler is only constructed during SetUp");
        }

        protected virtual ITestableObservable<T> Hot<T>(string marbles, object? values = null, Exception? error = null)
        {
            EnsureScheduler();
            return Scheduler!.CreateHotObservable<T>(marbles, values, error);
        }

        public virtual ITestableObservable<T> Cold<T>(string marbles, object? values = null, Exception? error = null)
        {
            EnsureScheduler();
            return Scheduler!.CreateColdObservable<T>(marbles, values, error);
        }

        public virtual long Time(string marbles)
        {
            EnsureScheduler();
            return Scheduler!.CreateTime(marbles);
        }

        public virtual IExpectObservable<T> ExpectObservable<T>(IObservable<T> observable, string? subscriptionKey = null)
        {
            EnsureScheduler();
            return Scheduler!.ExpectObservable(observable, subscriptionKey);
        }

        public virtual IExpectSubscriptions ExpectSubscriptions(params Subscription[] actualSubscriptions)
        {
            EnsureScheduler();
            return Scheduler!.ExpectSubscriptions(actualSubscriptions);
        }

        public virtual IExpectSubscriptions ExpectSubscriptions(IEnumerable<Subscription> actualSubscriptions)
        {
            EnsureScheduler();
            return Scheduler!.ExpectSubscriptions(actualSubscriptions);
        }
    }
}
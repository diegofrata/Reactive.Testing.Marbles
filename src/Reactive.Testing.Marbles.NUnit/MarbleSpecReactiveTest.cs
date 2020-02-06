using System;
using System.Collections.Generic;
using Microsoft.Reactive.Testing;

namespace Reactive.Testing.Marbles.NUnit
{
    public abstract class MarbleSpecReactiveTest : MarbleReactiveTest
    {
        MarbleSpec? _spec;

        protected MarbleSpecReactiveTest(long frameTimeFactor = MarbleTestScheduler.DefaultFrameTimeFactor) : base(frameTimeFactor) 
        {
        }

        void EnsureSpec()
        {
            if (_spec == null) throw new InvalidOperationException("A spec must be defined before this method can be invoked.");
        }
        
        protected void Spec(string spec, object? values = null)
        {
            _spec = new MarbleSpec(Scheduler!, spec, values);
        }

        protected override ITestableObservable<T> Hot<T>(string key, object? values = null, Exception? error = null)
        {
            EnsureSpec();
            return _spec!.Hot<T>(key, values, error);
        }

        public override ITestableObservable<T> Cold<T>(string key, object? values = null, Exception? error = null)
        {
            EnsureSpec();
            return _spec!.Cold<T>(key, values, error);
        }

        public override long Time(string key)
        {
            EnsureSpec();
            return _spec!.Time(key);
        }

        public override IExpectObservable<T> ExpectObservable<T>(IObservable<T> observable, string? subscriptionKey = null)
        {
            EnsureSpec();
            return _spec!.ExpectObservable(observable, subscriptionKey);
        }

        public override IExpectSubscriptions ExpectSubscriptions(params Subscription[] actualSubscriptions)
        {
            EnsureSpec();
            return _spec!.ExpectSubscriptions(actualSubscriptions);
        }

        public override IExpectSubscriptions ExpectSubscriptions(IEnumerable<Subscription> actualSubscriptions)
        {
            EnsureSpec();
            return _spec!.ExpectSubscriptions(actualSubscriptions);
        }
    }
}
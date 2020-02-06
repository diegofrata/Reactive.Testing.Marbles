using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Reactive.Testing;

namespace Reactive.Testing.Marbles
{
    public class MarbleSpec
    {
        readonly object? _values;
        readonly MarbleTestScheduler _scheduler;
        readonly IReadOnlyDictionary<string, string> _marblesLookup;

        public MarbleSpec(MarbleTestScheduler scheduler, string spec, object? values = null) : this(scheduler, MarbleSpecParser.Parse(spec))
        {
            _values = values;
        }
        
        public MarbleSpec(MarbleTestScheduler scheduler, IReadOnlyDictionary<string, string> marblesLookup)
        {
            _scheduler = scheduler;
            _marblesLookup = marblesLookup;
        }

        public string this[string key] => _marblesLookup[key];

        public ITestableObservable<T> Hot<T>(string key, object? values = null, Exception? error = null)
        {
            return _scheduler.CreateHotObservable<T>(_marblesLookup[key], values ?? _values, error);
        }

        public ITestableObservable<T> Cold<T>(string key, object? values = null, Exception? error = null)
        {
            return _scheduler.CreateColdObservable<T>(_marblesLookup[key], values ?? _values, error);
        }

        public long Time(string key)
        {
            return _scheduler.CreateTime(_marblesLookup[key]);
        }

        public IExpectObservable<T> ExpectObservable<T>(IObservable<T> observable, string? subscriptionKey = null)
        {
            return new ExpectableObservableWrapper<T>(this, _scheduler.ExpectObservable(observable, subscriptionKey != null ? _marblesLookup[subscriptionKey] : null));
        }
        
        public IExpectSubscriptions ExpectSubscriptions(params Subscription[] actualSubscriptions)
        {
            return ExpectSubscriptions(actualSubscriptions.AsEnumerable());
        }

        public IExpectSubscriptions ExpectSubscriptions(IEnumerable<Subscription> actualSubscriptions)
        {
            return new ExpectableSubscriptionsWrapper(this, _scheduler.ExpectSubscriptions(actualSubscriptions));
        }

        class ExpectableObservableWrapper<T> : IExpectObservable<T>
        {
            readonly MarbleSpec _spec;
            readonly IExpectObservable<T> _expectObservable;

            public ExpectableObservableWrapper(MarbleSpec spec, IExpectObservable<T> expectObservable)
            {
                _spec = spec;
                _expectObservable = expectObservable;
            }

            public void ToBe(string marbles, object? values = null, Exception? error = null, IComparer<T>? valueComparer = null, IComparer<Exception>? errorComparer = null)
            {
                _expectObservable.ToBe(_spec[marbles], values ?? _spec._values, error, valueComparer, errorComparer);
            }
        }

        class ExpectableSubscriptionsWrapper : IExpectSubscriptions
        {
            readonly MarbleSpec _spec;
            readonly IExpectSubscriptions _expectSubscriptions;

            public ExpectableSubscriptionsWrapper(MarbleSpec spec, IExpectSubscriptions expectSubscriptions)
            {
                _spec = spec;
                _expectSubscriptions = expectSubscriptions;
            }
            
            public void ToBe(params string[] marbles)
            {
                _expectSubscriptions.ToBe(marbles.Select(x => _spec[x]).ToArray());
            }
        }
    }
}
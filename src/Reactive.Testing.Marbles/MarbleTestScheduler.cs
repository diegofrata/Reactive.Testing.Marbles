using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.Reactive.Testing;

namespace Reactive.Testing.Marbles
{
    public class MarbleTestScheduler : TestScheduler
    {
        public const long DefaultFrameTimeFactor = TimeSpan.TicksPerMillisecond;

        readonly List<object> _hotObservables = new List<object>();
        readonly List<object> _coldObservables = new List<object>();
        readonly List<IFlushableTest> _flushTests = new List<IFlushableTest>();
        readonly IAssertFactory _assertFactory;
        internal IReadOnlyList<IFlushableTest> FlushTests => _flushTests;
        public long FrameTimeFactor { get; }

        public MarbleTestScheduler(long frameTimeFactor = DefaultFrameTimeFactor, IAssertFactory? assertFactory = null)
        {
            _assertFactory = assertFactory ?? new DefaultAssertFactory();
            FrameTimeFactor = frameTimeFactor;
        }

        public long CreateTime(string marbles)
        {
            var indexOf = marbles.Trim().IndexOf('|');
            if (indexOf == -1)
            {
                throw new Exception("marble diagram for time should have a completion marker \"|\"");
            }

            return indexOf * FrameTimeFactor;
        }

        public ITestableObservable<T> CreateColdObservable<T>(string marbles, object? values = null, Exception? error = null)
        {
            if (marbles.IndexOf("^", StringComparison.Ordinal) != -1)
            {
                throw new Exception("cold observable cannot have subscription offset \"^\"");
            }

            if (marbles.IndexOf("!", StringComparison.Ordinal) != -1)
            {
                throw new Exception("cold observable cannot have unsubscription marker \"!\"");
            }

            var messages = MarbleParser.Instance.Parse<T>(marbles, values, error, FrameTimeFactor);

            return base.CreateColdObservable(messages.ToArray());
        }

        public ITestableObservable<T> CreateHotObservable<T>(string marbles, object? values = null, Exception? error = null)
        {
            if (marbles.IndexOf("!", StringComparison.Ordinal) != -1)
            {
                throw new Exception("hot observable cannot have unsubscription marker \"!\"");
            }

            var messages = MarbleParser.Instance.Parse<T>(marbles, values, error, FrameTimeFactor);

            return base.CreateHotObservable(messages.ToArray());
        }

        public IExpectObservable<T> ExpectObservable<T>(IObservable<T> observable, string? subscriptionMarbles = null)
        {
            var actual = new List<Recorded<Notification<T>>>();
            var flushTest = new FlushableTest<Recorded<Notification<T>>>(actual);
            var subscriptionParsed = MarbleParser.Instance.ParseAsSubscriptions(subscriptionMarbles, FrameTimeFactor);
            var subscriptionFrame = subscriptionParsed.Subscribe == Subscription.Infinite ? 0 : subscriptionParsed.Subscribe;
            var unsubscriptionFrame = subscriptionParsed.Unsubscribe;

            IDisposable? subscription = null;
            Schedule(Unit.Default, TimeSpan.FromTicks(subscriptionFrame - 1), (a, b) =>
            {
                 subscription = observable
                    .Materialize()
                    .Where(x => Clock >= subscriptionFrame && Clock <= unsubscriptionFrame)
                    .Subscribe(x => { actual.Add(new Recorded<Notification<T>>(Clock, x)); });

                 return subscription;
            });

            Schedule(Unit.Default, TimeSpan.FromTicks(unsubscriptionFrame), (a, b) =>
            {
                Interlocked.Exchange(ref subscription, null)?.Dispose();
                return Disposable.Empty;
            });

            _flushTests.Add(flushTest);

            return new ExpectableObservable<T>(flushTest, FrameTimeFactor, MarbleParser.Instance, _assertFactory);
        }

        public IExpectSubscriptions ExpectSubscriptions(params Subscription[] actualSubscriptions)
        {
            return ExpectSubscriptions(actualSubscriptions.AsEnumerable());
        }

        public IExpectSubscriptions ExpectSubscriptions(IEnumerable<Subscription> actualSubscriptions)
        {
            var flushTest = new FlushableTest<Subscription>(actualSubscriptions);
            _flushTests.Add(flushTest);
            return new ExpectableSubscriptions(flushTest, FrameTimeFactor, MarbleParser.Instance, _assertFactory);
        }

        public override IDisposable ScheduleAbsolute<TState>(TState state, long dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            var rewind = false;
            if (dueTime <= Clock)
            {
                Clock -= 1;
                rewind = true;
            }

            var result = base.ScheduleAbsolute(state, dueTime, action);

            if (rewind) Clock += 1;

            return result;
        }

        public void Flush()
        {
            base.Start();

            var readyTests = _flushTests.Where(x => x.Ready).ToList();

            foreach (var test in readyTests)
            {
                _flushTests.Remove(test);
                test.Assert();
            }
        }
    }
}
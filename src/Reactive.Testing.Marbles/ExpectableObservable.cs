using System;
using System.Collections.Generic;
using System.Reactive;
using Microsoft.Reactive.Testing;

namespace Reactive.Testing.Marbles
{
    public interface IExpectObservable<T>
    {
        void ToBe(string marbles, object? values = null, Exception? error = null, IComparer<T>? valueComparer = null, IComparer<Exception>? errorComparer = null);
    }

    class ExpectableObservable<T> : IExpectObservable<T>
    {
        readonly IFlushableTest<Recorded<Notification<T>>> _flushableTest;
        readonly long _frameTimeFactor;
        readonly IMarbleParser _parser;
        readonly IAssertFactory _assertFactory;

        public ExpectableObservable(IFlushableTest<Recorded<Notification<T>>> flushableTest,
            long frameTimeFactor,
            IMarbleParser parser,
            IAssertFactory assertFactory)
        {
            _flushableTest = flushableTest;
            _frameTimeFactor = frameTimeFactor;
            _parser = parser;
            _assertFactory = assertFactory;
        }

        public void ToBe(string marbles, object? values = null, Exception? error = null, IComparer<T>? valueComparer = null, IComparer<Exception>? errorComparer = null)
        {
            _flushableTest.Expected = _parser.Parse<T>(marbles, values, error, _frameTimeFactor);
            _flushableTest.AssertCallback = _assertFactory.CreateForObservable(valueComparer, errorComparer);
        }
    }
}
using System;
using System.Collections.Generic;

namespace Reactive.Testing.Marbles
{
    interface IFlushableTest
    {
        bool Ready { get; }
        void Assert();
    }

    interface IFlushableTest<T> : IFlushableTest
    {
        IEnumerable<T> Actual { get; }
        IEnumerable<T>? Expected { get; set; }
        Action<IEnumerable<T>, IEnumerable<T>>? AssertCallback { get; set; }
    }

    class FlushableTest<T> : IFlushableTest<T>
    {
        public IEnumerable<T> Actual { get; }
        public IEnumerable<T>? Expected { get; set; }
        public Action<IEnumerable<T>, IEnumerable<T>>? AssertCallback { get; set; }
        public bool Ready => Actual != null && Expected != null && AssertCallback != null;

        public FlushableTest(IEnumerable<T> actual)
        {
            Actual = actual;
        }

        public void Assert()
        {
            if (!Ready) throw new Exception("Flushable test not ready.");
            AssertCallback!(Actual, Expected!);
        }
    }
}
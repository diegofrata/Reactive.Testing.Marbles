using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Microsoft.Reactive.Testing;

namespace Reactive.Testing.Marbles
{
    public interface IAssertFactory
    {
        Action<IEnumerable<Recorded<Notification<T>>>, IEnumerable<Recorded<Notification<T>>>> CreateForObservable<T>(
            IComparer<T>? valueComparer = null,
            IComparer<Exception>? errorComparer = null);

        Action<IEnumerable<Subscription>, IEnumerable<Subscription>> CreateForSubscription();
    }
    
    public class DefaultAssertFactory : IAssertFactory
    {
        public Action<IEnumerable<Recorded<Notification<T>>>, IEnumerable<Recorded<Notification<T>>>> CreateForObservable<T>(
            IComparer<T>? valueComparer = null,
            IComparer<Exception>? errorComparer = null)
        {
            return (actual, expected) =>
            {
                var notificationComparer = new RecordedNotificationComparer<T>(
                    ComparerPicker.Pick(valueComparer),
                    errorComparer
                );

                if (!actual.SequenceEqual(expected, new EqualityComparer<Recorded<Notification<T>>>(notificationComparer)))
                    throw new Exception("Assertion failed.");
            };
        }

        public Action<IEnumerable<Subscription>, IEnumerable<Subscription>> CreateForSubscription()
        {
            return (actual, expected) =>
            {
                if (!actual.SequenceEqual(expected))
                    throw new Exception("Assertion failed.");
            };
        }
    }
    
    class EqualityComparer<T> : IEqualityComparer<T>
    {
        readonly IComparer<T> _comparer;

        public EqualityComparer(IComparer<T> comparer)
        {
            _comparer = comparer;
        }

        public bool Equals(T x, T y) => _comparer.Compare(x, y) == 0;

        public int GetHashCode(T obj) => 0;
    }
}
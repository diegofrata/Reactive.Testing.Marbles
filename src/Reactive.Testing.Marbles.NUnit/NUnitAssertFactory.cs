using System;
using System.Collections.Generic;
using System.Reactive;
using Microsoft.Reactive.Testing;
using NUnit.Framework;

namespace Reactive.Testing.Marbles.NUnit
{
    public class NUnitAssertFactory : IAssertFactory
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

                CollectionAssert.AreEqual(expected, actual, notificationComparer);
            };
        }

        public Action<IEnumerable<Subscription>, IEnumerable<Subscription>> CreateForSubscription()
        {
            return (actual, expected) =>
            {
                CollectionAssert.AreEqual(expected, actual);
            };
        }
    }
}
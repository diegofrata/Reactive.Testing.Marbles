using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Microsoft.Reactive.Testing;

namespace Reactive.Testing.Marbles
{
    public static class ComparerPicker
    {
        public static IComparer<T> Pick<T>(IComparer<T>? valueComparer)
        {
            if (valueComparer != null) return valueComparer;

            if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)) || typeof(IComparable).IsAssignableFrom(typeof(T)))
                return Comparer<T>.Default;

            if (typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                var interfaces = typeof(T).IsInterface ? typeof(T).GetInterfaces().Prepend(typeof(T)) : typeof(T).GetInterfaces();
                var enumerableType = interfaces.First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                var itemType = enumerableType.GenericTypeArguments[0];
                var comparerType = typeof(SequenceComparer<,>).MakeGenericType(enumerableType, itemType);
                var comparer = Activator.CreateInstance(comparerType);
                return (IComparer<T>) comparer;
            }


            return EqualityComparer<T>.Instance;
        }

        class SequenceComparer<TCollection, TItem> : IComparer<TCollection> where TCollection : IEnumerable<TItem>
        {
            public int Compare(TCollection x, TCollection y)
            {
                if (ReferenceEquals(x, null) && ReferenceEquals(y, null)) return 0;
                if (ReferenceEquals(x, null)) return -1;
                if (ReferenceEquals(y, null)) return 1;
                return x.SequenceEqual(y) ? 0 : -1;
            }
        }

        class EqualityComparer<T> : IComparer<T>
        {
            public static readonly EqualityComparer<T> Instance = new EqualityComparer<T>();
            public int Compare(T x, T y) => Equals(x, y) ? 0 : -1;
        }
    }

    public class RecordedNotificationComparer<T> : IComparer<Recorded<Notification<T>>>, IComparer
    {
        readonly IComparer<T> _valueComparer;
        readonly IComparer<Exception>? _errorComparer;

        public RecordedNotificationComparer(IComparer<T> valueComparer, IComparer<Exception>? errorComparer = null)
        {
            _valueComparer = valueComparer;
            _errorComparer = errorComparer;
        }

        public int Compare(Recorded<Notification<T>> x, Recorded<Notification<T>> y)
        {
            var compare = x.Time.CompareTo(y.Time);
            if (compare != 0) return compare;

            compare = x.Value.Kind.CompareTo(y.Value.Kind);
            if (compare != 0) return compare;

            switch (x.Value.Kind)
            {
                case NotificationKind.OnNext:
                    compare = _valueComparer.Compare(x.Value.Value, y.Value.Value);
                    break;
                case NotificationKind.OnError:
                    switch (ReferenceEquals(x.Value.Exception, null), ReferenceEquals(y.Value.Exception, null))
                    {
                        case (true, true):
                            compare = 0;
                            break;
                        case (false, true):
                            compare = 1;
                            break;
                        case (true, false):
                            compare = -1;
                            break;
                        case (false, false):
                            if (_errorComparer != null)
                                compare = _errorComparer.Compare(x.Value.Exception!, y.Value.Exception!);
                            else
                                compare = x.Value?.Exception?.GetType() == y.Value?.Exception?.GetType() ? 0 : -1;
                            break;
                    }

                    break;
            }

            return compare;
        }

        public int Compare(object x, object y)
        {
            if (ReferenceEquals(x, null) && ReferenceEquals(y, null)) return 0;
            if (ReferenceEquals(x, null)) return -1;
            if (ReferenceEquals(y, null)) return 1;

            if (x is Recorded<Notification<T>> xr && y is Recorded<Notification<T>> xy)
                return Compare(xr, xy);

            return -1;
        }
    }
}
using System;
using System.Reactive;
using Microsoft.Reactive.Testing;

namespace Reactive.Testing.Marbles.Tests
{
    public abstract class TestingBase
    {
        public static Recorded<Notification<T>> OnNext<T>(long frame, T value) => new Recorded<Notification<T>>(frame, Notification.CreateOnNext(value));
        public static Recorded<Notification<T>> OnError<T>(long frame, Exception error) => new Recorded<Notification<T>>(frame, Notification.CreateOnError<T>(error));
        public static Recorded<Notification<T>> OnError<T>(long frame, string errorMessage) => new Recorded<Notification<T>>(frame, Notification.CreateOnError<T>(new Exception(errorMessage)));
        public static Recorded<Notification<T>> OnCompleted<T>(long frame) => new Recorded<Notification<T>>(frame, Notification.CreateOnCompleted<T>());
    }
}
using System;
using System.Collections.Generic;
using System.Reactive;
using Microsoft.Reactive.Testing;
using NSubstitute;
using NUnit.Framework;

namespace Reactive.Testing.Marbles.Tests
{
    [TestFixture]
    public class ExpectableObservableTests
    {
        IFlushableTest<Recorded<Notification<char>>> _flushable;
        IMarbleParser _parser;
        IAssertFactory _assertFactory;

        [SetUp]
        public void SetUp()
        {
            _parser = Substitute.For<IMarbleParser>();
            _flushable = Substitute.For<IFlushableTest<Recorded<Notification<char>>>>();
            _assertFactory = Substitute.For<IAssertFactory>();
        }

        [Test]
        public void Should_set_expected_of_flush_test_when_to_be_is_called()
        {
            var valueComparer = Comparer<char>.Create((a, b) => 0);
            var errorComparer = Comparer<Exception>.Create((a, b) => 0);
            var values = new { a = 'a' };
            var error = new Exception("error!");

            var notifications = new[] { new Recorded<Notification<char>>(0, Notification.CreateOnNext('a')) };
            _parser.Parse<char>("a|", values, error, 10).Returns(notifications);
            _assertFactory.CreateForObservable(valueComparer, errorComparer).Returns((a, b) => { });

            var sut = new ExpectableObservable<char>(_flushable, 10, _parser, _assertFactory);
            sut.ToBe("a|", values, error, valueComparer, errorComparer);
            Assert.That(_flushable.Expected, Is.EquivalentTo(notifications));
        }
    }
}
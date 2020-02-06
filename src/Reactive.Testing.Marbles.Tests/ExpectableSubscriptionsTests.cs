using Microsoft.Reactive.Testing;
using NSubstitute;
using NUnit.Framework;

namespace Reactive.Testing.Marbles.Tests
{
    [TestFixture]
    public class ExpectableSubscriptionsTests
    {
        IFlushableTest<Subscription> _flushable;
        IMarbleParser _parser;
        IAssertFactory _assertFactory;

        [SetUp]
        public void SetUp()
        {
            _parser = Substitute.For<IMarbleParser>();
            _flushable = Substitute.For<IFlushableTest<Subscription>>();
            _assertFactory = Substitute.For<IAssertFactory>();
        }

        [Test]
        public void Should_set_expected_of_flush_test_when_to_be_is_called()
        {
            var notifications = new[]
            {
                new Subscription(40, 120),
                new Subscription(40, 170)
            };
            _parser.ParseAsSubscriptions("----^-------!", 10).Returns(notifications[0]);
            _parser.ParseAsSubscriptions("----^------------!", 10).Returns(notifications[1]);
            _assertFactory.CreateForSubscription().Returns((a, b) => { });

            var sut = new ExpectableSubscriptions(_flushable, 10, _parser, _assertFactory);
            sut.ToBe("----^-------!", "----^------------!");
            Assert.That(_flushable.Expected, Is.EquivalentTo(notifications));
        }
    }
}
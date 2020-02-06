using System.Reactive.Linq;
using NUnit.Framework;

namespace Reactive.Testing.Marbles.Tests
{
    [TestFixture]
    public class MarbleSpecTests
    {
        MarbleTestScheduler _scheduler;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new MarbleTestScheduler();
        }

        [Test]
        public void Should_execute_the_spec()
        {
            var spec = new MarbleSpec(_scheduler, @"
                e1  : ---a-------c----
                e2  : -------b-------d
                r   : ---a---b---c----
                sub : ^------------!
            ");

            var e1 = spec.Cold<char>("e1");
            var e2 = spec.Cold<char>("e2");
            var r = e1.Merge(e2);

            spec.ExpectObservable(r, "sub").ToBe("r");

            Assert.That(_scheduler.FlushTests.Count, Is.EqualTo(1));
            _scheduler.Flush();
            Assert.That(_scheduler.FlushTests.Count, Is.EqualTo(0));
        }

        [Test]
        public void Should_allow_values_to_be_defined_in_the_spec()
        {
            var spec = new MarbleSpec(_scheduler, @"
                e1  : ---a-------c----
                e2  : -------b-------d
                r   : ---e---f---g----
                sub : ^------------!
            ", new { a = 'a', b = 'b', c = 'c', d = 'd', e = 10, f = 11, g = 12 });

            var e1 = spec.Cold<char>("e1");
            var e2 = spec.Cold<char>("e2");
            var r = e1.Merge(e2).Select((x, i) => i + 10);

            spec.ExpectObservable(r, "sub").ToBe("r");

            Assert.That(_scheduler.FlushTests.Count, Is.EqualTo(1));
            _scheduler.Flush();
            Assert.That(_scheduler.FlushTests.Count, Is.EqualTo(0));
        }

        [TearDown]
        public void TearDown()
        {
            _scheduler.Flush();
        }
    }
}
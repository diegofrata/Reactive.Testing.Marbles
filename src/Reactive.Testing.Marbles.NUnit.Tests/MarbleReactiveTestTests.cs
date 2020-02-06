using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using NUnit.Framework;

namespace Reactive.Testing.Marbles.NUnit.Tests
{
    public class MarbleReactiveTestTests : MarbleReactiveTest
    {
        List<MarbleTestScheduler> _list;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _list = new List<MarbleTestScheduler>();
        }

        [SetUp]
        public void SetUp()
        {
            _list.Add(Scheduler);
        }

        [Test]
        public void Should_pass_a_passing_test()
        {
            var e1 = Cold<char>("---a-------c----");
            var e2 = Cold<char>("-------b-------d");
            var r = e1.Merge(e2);

            ExpectObservable(r, "^------------!").ToBe("---a---b---c----");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Assert.That(_list.All(x => x.FlushTests.Count == 0), Is.True);
        }
    }
}
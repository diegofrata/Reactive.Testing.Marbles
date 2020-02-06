using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using Microsoft.Reactive.Testing;
using NUnit.Framework;

namespace Reactive.Testing.Marbles.Tests
{
    [TestFixture]
    public class TestSchedulerBug
    {
        [Test]
        public void Test()
        {
            var list = new List<char>();
            var scheduler = new TestScheduler();

            scheduler.Schedule(Unit.Default, TimeSpan.FromMilliseconds(2), (a, b) =>
            {
                list.Add('a');
                return Disposable.Empty;;
            });
            
            scheduler.Schedule(Unit.Default, TimeSpan.FromMilliseconds(3), (a, b) =>
            {
                list.Add('c');
                return Disposable.Empty;;
            });
            
            scheduler.Schedule(Unit.Default, TimeSpan.FromMilliseconds(4), (a, b) =>
            {
                list.Add('d');
                return Disposable.Empty;;
            });
            
            scheduler.Schedule(Unit.Default, TimeSpan.FromMilliseconds(2), (a, b) =>
            {
                list.Add('b');
                return Disposable.Empty;;
            });

            scheduler.Start();
            
            Assert.That(list, Is.EqualTo(new [] { 'a', 'b', 'c', 'd'}).AsCollection);
        }
    }
}
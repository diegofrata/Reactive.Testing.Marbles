using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NUnit.Framework;

namespace Reactive.Testing.Marbles.Tests
{
    [TestFixture]
    public class MarbleTestSchedulerTests
    {
        [TestFixture]
        public class Constructor
        {
            [Test]
            public void Should_have_FrameTimeFactor_set_initially()
            {
                var scheduler = new MarbleTestScheduler();
                Assert.That(scheduler.FrameTimeFactor, Is.EqualTo(10_000));
            }

            [Test]
            public void Should_allow_FrameTimeFactor_to_be_overriden()
            {
                var scheduler = new MarbleTestScheduler(152);
                Assert.That(scheduler.FrameTimeFactor, Is.EqualTo(152));
            }
        }

        [TestFixture]
        public class CreateTime
        {
            [Test]
            public void Should_parse_a_simple_time_marble_string_to_a_number()
            {
                var scheduler = new MarbleTestScheduler();
                var time = scheduler.CreateTime("-----|");
                Assert.That(time, Is.EqualTo(50_000));
            }

            [Test]
            public void Should_throw_if_not_given_good_marble_input()
            {
                var scheduler = new MarbleTestScheduler();
                Assert.Throws<Exception>(() => scheduler.CreateTime("-a-b-#"));
            }
        }

        [TestFixture]
        public class CreateColdObservable
        {
            [Test]
            public void Should_create_a_cold_observable()
            {
                var expected = new Queue<char>(new[] { 'A', 'B' });
                var scheduler = new MarbleTestScheduler();
                var source = scheduler.CreateColdObservable<char>("--a---b--|", new { a = 'A', b = 'B' });
                source.Subscribe(x => { Assert.That(x, Is.EqualTo(expected.Dequeue())); });
                scheduler.Flush();
                Assert.That(expected.Count, Is.Zero);
            }
        }

        [TestFixture]
        public class CreateHotObservable
        {
            [Test]
            public void Should_create_a_hot_observable()
            {
                var expected = new Queue<char>(new[] { 'A', 'B' });
                var scheduler = new MarbleTestScheduler();
                var source = scheduler.CreateHotObservable<char>("--a---b--|", new { a = 'A', b = 'B' });
                source.Subscribe(x => { Assert.That(x, Is.EqualTo(expected.Dequeue())); });
                scheduler.Flush();
                Assert.That(expected.Count, Is.Zero);
            }
        }

        [TestFixture]
        public class ExpectObservable
        {
            MarbleTestScheduler _scheduler;

            [SetUp]
            public void SetUp()
            {
                _scheduler = new MarbleTestScheduler();
            }

            [Test]
            public void Should_return_an_IExpectObservable()
            {
                var expectable = _scheduler.ExpectObservable(Observable.Return(1));
                Assert.That(expectable, Is.Not.Null);
                Assert.That(expectable, Is.AssignableTo<IExpectObservable<int>>());
            }

            [Test]
            public void Should_add_to_FlushTests()
            {
                _scheduler.ExpectObservable(Observable.Return(1));
                Assert.That(_scheduler.FlushTests.Count, Is.EqualTo(1));
            }

            [Test]
            public void Should_make_FlushTest_ready_when_ToBe_is_called()
            {
                _scheduler.ExpectObservable(Observable.Return('a')).ToBe("a|");
                Assert.That(_scheduler.FlushTests.Single().Ready, Is.True);
            }
        }

        [TestFixture]
        public class ExpectSubscriptions
        {
            MarbleTestScheduler _scheduler;

            [SetUp]
            public void SetUp()
            {
                _scheduler = new MarbleTestScheduler();
            }

            [Test]
            public void Should_return_an_IExpectSubscriptions()
            {
                var expectable = _scheduler.ExpectSubscriptions();
                Assert.That(expectable, Is.Not.Null);
                Assert.That(expectable, Is.AssignableTo<IExpectSubscriptions>());
            }

            [Test]
            public void Should_add_to_FlushTests()
            {
                _scheduler.ExpectSubscriptions();
                Assert.That(_scheduler.FlushTests.Count, Is.EqualTo(1));
            }

            [Test]
            public void Should_make_FlushTest_ready_when_ToBe_is_called()
            {
                _scheduler.ExpectSubscriptions().ToBe("^--!");
                Assert.That(_scheduler.FlushTests.Single().Ready, Is.True);
            }
        }

        [TestFixture]
        public class MarbleDiagrams
        {
            MarbleTestScheduler _scheduler;

            [SetUp]
            public void SetUp()
            {
                _scheduler = new MarbleTestScheduler();
            }

            [TearDown]
            public void TearDown()
            {
                _scheduler.Flush();
            }

            [Test]
            public void Should_handle_empty()
            {
                _scheduler.ExpectObservable(Observable.Empty<char>()).ToBe("|");
            }

            [Test]
            public void Should_handle_never()
            {
                _scheduler.ExpectObservable(Observable.Never<char>()).ToBe("-");
                _scheduler.ExpectObservable(Observable.Never<char>()).ToBe("--");
            }

            [Test]
            public void Should_accept_an_unsubscription_marble_diagram()
            {
                var source = _scheduler.CreateHotObservable<char>("---^-a-b-|");
                var unsubscribe = "---!";
                var expected = "--a";
                _scheduler.ExpectObservable(source, unsubscribe).ToBe(expected);
            }

            [Test]
            public void Should_accept_a_subscription_marble_diagram()
            {
                var source = _scheduler.CreateHotObservable<char>("-a-b-c|");
                var subscribe = "---^";
                var expected = "---b-c|";
                _scheduler.ExpectObservable(source, subscribe).ToBe(expected);
            }

            [Test]
            public void Should_assert_subscriptions_of_a_cold_observable()
            {
                var source = _scheduler.CreateColdObservable<char>("---a---b-|");
                var subs = "^--------!";
                var unsub = "---------|";
                var subscription = source.Subscribe();

                _scheduler.ScheduleAbsolute(subscription, _scheduler.CreateTime(unsub), (_, sub) =>
                {
                    sub.Dispose();
                    return Disposable.Empty;
                });

                _scheduler.ExpectSubscriptions(source.Subscriptions).ToBe(subs);
            }

            [Test]
            public void Should_ignore_whitespace()
            {
                var input = _scheduler.CreateColdObservable<char>("  -a - b -    c |       ");
                var output = input.Select(x => Observable.Return(x).Delay(TimeSpan.FromMilliseconds(10), _scheduler)).Concat();
                var expected = "     -- 9ms a 9ms b 9ms (c|) ";

                _scheduler.ExpectObservable(output).ToBe(expected);
                _scheduler.ExpectSubscriptions(input.Subscriptions).ToBe("  ^- - - - - !");
            }

            [Test]
            public void Should_support_time_progression_syntax()
            {
                var output = _scheduler.CreateColdObservable<char>("10.2ms a 1.2s b 1m c|");
                var expected = "10.2ms a 1.2s b 1m c|";
                _scheduler.ExpectObservable(output).ToBe(expected);
            }

            [Test]
            public void Should_have_each_frame_represent_a_single_virtual_millisecond()
            {
                var output = _scheduler.CreateColdObservable<char>("-a-b-c--------|").Throttle(TimeSpan.FromMilliseconds(5), _scheduler);
                _scheduler.ExpectObservable(output).ToBe("          ------ 4ms c---|");
            }

            [Test]
            public void Should_have_no_maximum_frame_count()
            {
                var output = _scheduler.CreateColdObservable<char>("-a|").Delay(TimeSpan.FromSeconds(10), _scheduler);
                _scheduler.ExpectObservable(output).ToBe("   - 10s a|");
            }

            [Test]
            public void Should_not_emit_items_before_subscription_point_in_a_hot_observable()
            {
                var e1 = _scheduler.CreateHotObservable<char>("----a--^--b-------c--|");
                var e2 = _scheduler.CreateHotObservable<char>("---d-^--e---------f-----|");
                var expected = "---(be)----c-f-----|";

                _scheduler.ExpectObservable(e1.Merge(e2)).ToBe(expected);
            }


            [Test]
            public void Should_expect_one_value_observable()
            {
                _scheduler.ExpectObservable(Observable.Return("hello")).ToBe("(h|)", new { h = "hello" });
            }

            [Test]
            public void Should_fail_when_event_values_differ()
            {
                Assert.Throws<Exception>(() =>
                {
                    var scheduler = new MarbleTestScheduler();
                    scheduler.ExpectObservable(Observable.Return("hello")).ToBe("h", new { h = "bye" });
                    scheduler.Flush();
                });
            }

            [Test]
            public void Should_fail_when_event_timing_differs()
            {
                Assert.Throws<Exception>(() =>
                {
                    var scheduler = new MarbleTestScheduler();
                    scheduler.ExpectObservable(Observable.Return("hello")).ToBe("--h", new { h = "hello" });
                    scheduler.Flush();
                });
            }

            [Test]
            public void Should_expect_observable_on_error()
            {
                var source = _scheduler.CreateColdObservable<Unit>("---#", null, new Exception());
                _scheduler.ExpectObservable(source).ToBe("---#", null, new Exception());
            }

            [Test]
            public void Should_fail_when_observables_end_with_different_error_types()
            {
                Assert.Throws<Exception>(() =>
                {
                    var scheduler = new MarbleTestScheduler();
                    var source = scheduler.CreateColdObservable<Unit>("---#", null, new ArgumentException());
                    scheduler.ExpectObservable(source).ToBe("---#", null, new Exception());
                    scheduler.Flush();
                });
            }

            [Test]
            public void Should_demo_with_a_simple_operator()
            {
                var sourceEvents = _scheduler.CreateColdObservable<string>("a-b-c-|");

                var upperEvents = sourceEvents.Select(s => s.ToUpper());

                _scheduler.ExpectObservable(upperEvents).ToBe("A-B-C-|");
            }

            [Test]
            public void Should_use_unsubscription_diagram()
            {
                var source = _scheduler.CreateHotObservable<char>("---^-a-b-|");
                var unsubscribe = "---!";
                var expected = "--a";

                _scheduler.ExpectObservable(source, unsubscribe).ToBe(expected);
            }

            [Test]
            public void Should_expect_subscription_on_a_cold_observable()
            {
                var source = _scheduler.CreateColdObservable<char>("---a---b-|");
                var subscription = source.Subscribe();

                _scheduler.ScheduleAbsolute(subscription, TimeSpan.FromMilliseconds(9).Ticks, (_, sub) =>
                {
                    sub.Dispose();
                    return Disposable.Empty;
                });

                var subs = "^--------!";

                _scheduler.ExpectSubscriptions(source.Subscriptions).ToBe(subs);
            }

            [Test]
            public void Should_project_the_next_character_in_the_alphabet()
            {
                var source = _scheduler.CreateHotObservable<char>("--^-a-b-c-|");
                var subs = "^-------!";
                var expected = "--b-c-d-|";

                var destination = source.Select(x => (char) (x + 1));

                _scheduler.ExpectObservable(destination).ToBe(expected);
                _scheduler.ExpectSubscriptions(source.Subscriptions).ToBe(subs);

                _scheduler.Flush();
            }

            [Test]
            public void Should_project_the_sum_of_a_cold_sequence()
            {
                var source = _scheduler.CreateColdObservable<int>("-a-b-c-|", new { a = 10, b = 20, c = 40 });
                var expected = "-------(d|)";
                var destination = source.Sum();

                _scheduler.ExpectObservable(destination).ToBe(expected, new { d = 70 });

                _scheduler.Flush();
            }


            [Test]
            public void Should_work_with_lists_of_different_types()
            {
                var a = Enumerable.Range(1, 5).ToList();
                var b = a.ToArray();

                var source = _scheduler.CreateColdObservable<IEnumerable<int>>("-a-|", new { a });
                _scheduler.ExpectObservable(source).ToBe("-b-|", new { b });

                _scheduler.Flush();
            }


            [Test]
            public void Should_unsubscribe_infinite_stream()
            {
                var source = Observable.Interval(TimeSpan.FromMilliseconds(10), _scheduler);
                _scheduler.ExpectObservable(source, "-------------!").ToBe("----------a---", new { a = 0L });

                _scheduler.Flush();
            }
        }
    }
}
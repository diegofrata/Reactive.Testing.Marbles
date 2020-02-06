using System;
using Microsoft.Reactive.Testing;
using NUnit.Framework;

namespace Reactive.Testing.Marbles.Tests
{
    [TestFixture]
    public class MarbleParserTests : TestingBase
    {
        MarbleParser _sut;

        [SetUp]
        public void SetUp()
        {
            _sut = new MarbleParser();       
        }
        
        [TestFixture]
        public class Parse : MarbleParserTests
        {
            [Test]
            public void Should_parse_a_marble_string_into_a_series_of_notifications_and_types()
            {
                var result = _sut.Parse<char>("-------a---b---|", new { a = 'A', b = 'B' });

                Assert.That(result, Is.EquivalentTo(new[]
                {
                    OnNext(70_000, 'A'),
                    OnNext(110_000, 'B'),
                    OnCompleted<char>(150_000)
                }));
            }

            [Test]
            public void Should_parse_a_marble_string_allowing_spaces_too()
            {
                var result = _sut.Parse<char>("--a--b--|   ", new { a = 'A', b = 'B' });

                Assert.That(result, Is.EquivalentTo(new[]
                {
                    OnNext(20_000, 'A'),
                    OnNext(50_000, 'B'),
                    OnCompleted<char>(80_000)
                }));
            }

            [Test]
            public void Should_parse_a_marble_string_with_a_subscription_point()
            {
                var result = _sut.Parse<char>("---^---a---b---|", new { a = 'A', b = 'B' });

                Assert.That(result, Is.EquivalentTo(new[]
                {
                    OnNext(40_000, 'A'),
                    OnNext(80_000, 'B'),
                    OnCompleted<char>(120_000)
                }));
            }

            [Test]
            public void Should_parse_a_marble_string_with_an_error()
            {
                var error = new Exception("omg error!");
                var result = _sut.Parse<char>("-------a---b---#", new { a = 'A', b = 'B' }, error);

                Assert.That(result, Is.EquivalentTo(new[]
                {
                    OnNext(70_000, 'A'),
                    OnNext(110_000, 'B'),
                    OnError<char>(150_000, error)
                }));
            }

            [Test]
            public void Should_default_in_the_letter_as_Char_for_the_value_if_no_value_object_was_passed()
            {
                var result = _sut.Parse<char>("--a--b--c--");

                Assert.That(result, Is.EquivalentTo(new[]
                {
                    OnNext(20_000, 'a'),
                    OnNext(50_000, 'b'),
                    OnNext(80_000, 'c'),
                }));
            }

            [Test]
            public void Should_default_in_the_letter_as_String_for_the_value_if_no_value_object_was_passed()
            {
                var result = _sut.Parse<string>("--a--b--c--");

                Assert.That(result, Is.EquivalentTo(new[]
                {
                    OnNext(20_000, "a"),
                    OnNext(50_000, "b"),
                    OnNext(80_000, "c"),
                }));
            }

            [Test]
            public void Should_handle_grouped_values()
            {
                var result = _sut.Parse<char>("---(abc)---");

                Assert.That(result, Is.EquivalentTo(new[]
                {
                    OnNext(30_000, 'a'),
                    OnNext(30_000, 'b'),
                    OnNext(30_000, 'c'),
                }));
            }

            [Test]
            public void Should_ignore_whitespace()
            {
                var result = _sut.Parse<char>(
                    "  -a - b -    c |       ",
                    new { a = 'A', b = 'B', c = 'C' }
                );

                Assert.That(result, Is.EquivalentTo(new[]
                {
                    OnNext(10_000, 'A'),
                    OnNext(30_000, 'B'),
                    OnNext(50_000, 'C'),
                    OnCompleted<char>(60_000),
                }));
            }


            [Test]
            public void Should_support_time_progression_syntax()
            {
                var result = _sut.Parse<char>(
                    "10.2ms a 1.2s b 1m c|",
                    new { a = 'A', b = 'B', c = 'C' }
                );

                Assert.That(result, Is.EqualTo(new[]
                {
                    // https://github.com/ReactiveX/rxjs/blob/8c32ed0e0038147f40f905e41ec9b0c512e6fb32/spec/schedulers/TestScheduler-spec.ts
                    // This test was ported from TestScheduler-spec.ts so the expected frames have to be cast to long.
                    OnNext(100_000, 'A'),
                    OnNext(12_110_000, 'B'),
                    OnNext(612_120_000, 'C'),
                    OnCompleted<char>(612_130_000),
                }).AsCollection);
            }
        }

        [TestFixture]
        public class ParseAsSubscriptions: MarbleParserTests
        {
            [Test]
            public void Should_parse_a_subscription_marble_string_into_a_subscription()
            {
                var result = _sut.ParseAsSubscriptions("---^---!-");

                Assert.That(result.Subscribe, Is.EqualTo(30_000));
                Assert.That(result.Unsubscribe, Is.EqualTo(70_000));
            }

            [Test]
            public void Should_parse_a_subscription_marble_string_without_an_unsubscription()
            {
                var result = _sut.ParseAsSubscriptions("---^-");

                Assert.That(result.Subscribe, Is.EqualTo(30_000));
                Assert.That(result.Unsubscribe, Is.EqualTo(Subscription.Infinite));
            }

            [Test]
            public void Should_parse_a_subscription_marble_string_with_a_synchronous_unsubscription()
            {
                var result = _sut.ParseAsSubscriptions("---(^!)-");

                Assert.That(result.Subscribe, Is.EqualTo(30_000));
                Assert.That(result.Unsubscribe, Is.EqualTo(30_000));
            }

            [Test]
            public void Should_ignore_whitespace()
            {
                var result = _sut.ParseAsSubscriptions("  - -  - -  ^ -   - !  -- -      ");

                Assert.That(result.Subscribe, Is.EqualTo(40_000));
                Assert.That(result.Unsubscribe, Is.EqualTo(70_000));
            }

            [Test]
            public void Should_support_time_progression_syntax()
            {
                var result = _sut.ParseAsSubscriptions("10.2ms ^ 1.2s - 1m !");
                
                // https://github.com/ReactiveX/rxjs/blob/8c32ed0e0038147f40f905e41ec9b0c512e6fb32/spec/schedulers/TestScheduler-spec.ts
                // This test was ported from TestScheduler-spec.ts so the expected frames have to be cast to long.
                Assert.That(result.Subscribe, Is.EqualTo(100_000L));
                Assert.That(result.Unsubscribe, Is.EqualTo(612_120_000));
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text.RegularExpressions;
using Microsoft.Reactive.Testing;

namespace Reactive.Testing.Marbles
{
    public interface IMarbleParser
    {
        Subscription ParseAsSubscriptions(string? marbles, long frameTimeFactor = MarbleTestScheduler.DefaultFrameTimeFactor);

        IReadOnlyList<Recorded<Notification<T>>> Parse<T>(
            string marbles,
            object? values = null,
            Exception? error = null,
            long frameTimeFactor = MarbleTestScheduler.DefaultFrameTimeFactor);
    }

    public class MarbleParser : IMarbleParser
    {
        public static readonly IMarbleParser Instance = new MarbleParser();

        public Subscription ParseAsSubscriptions(string? marbles, long frameTimeFactor = MarbleTestScheduler.DefaultFrameTimeFactor)
        {
            if (marbles == null)
            {
                return new Subscription(Subscription.Infinite);
            }

            var len = marbles.Length;
            var groupStart = -1L;
            var subscriptionFrame = Subscription.Infinite;
            var unsubscriptionFrame = Subscription.Infinite;
            var frame = 0L;

            for (var i = 0; i < len; i++)
            {
                var nextFrame = frame;

                void AdvanceFrameBy(long count) => nextFrame += count * frameTimeFactor;

                var c = marbles[i];
                switch (c)
                {
                    case ' ':
                        break;
                    case '-':
                        AdvanceFrameBy(1);
                        break;
                    case '(':
                        groupStart = frame;
                        AdvanceFrameBy(1);
                        break;
                    case ')':
                        groupStart = -1;
                        AdvanceFrameBy(1);
                        break;
                    case '^':
                        if (subscriptionFrame != Subscription.Infinite)
                            throw new Exception("found a second subscription point '^' in a subscription marble diagram. There can only be one.");

                        subscriptionFrame = groupStart > -1 ? groupStart : frame;
                        AdvanceFrameBy(1);
                        break;
                    case '!':
                        if (unsubscriptionFrame != Subscription.Infinite)
                            throw new Exception("found a second subscription point '^' in a subscription marble diagram. There can only be one.");

                        unsubscriptionFrame = groupStart > -1 ? groupStart : frame;
                        break;
                    default:
                        // Time progression syntax
                        var progression = AdvanceTimeProgression(marbles, frameTimeFactor, c, i);

                        if (progression != default)
                        {
                            i += progression.Offset;
                            AdvanceFrameBy(progression.Duration);
                            break;
                        }

                        throw new Exception($"there can only be '^' and '!' markers in a subscription marble diagram. Found instead '{c}'.");
                }

                frame = nextFrame;
            }

            return unsubscriptionFrame < 0
                ? new Subscription(subscriptionFrame)
                : new Subscription(subscriptionFrame, unsubscriptionFrame);
        }


        public IReadOnlyList<Recorded<Notification<T>>> Parse<T>(
            string marbles,
            object? values = null,
            Exception? error = null,
            long frameTimeFactor = MarbleTestScheduler.DefaultFrameTimeFactor)
        {
            if (marbles.IndexOf("!", StringComparison.Ordinal) != -1)
                throw new Exception("conventional marble diagrams cannot have the unsubscription marker \"!\"");

            var len = marbles.Length;
            var testMessages = new List<Recorded<Notification<T>>>();
            var subIndex = marbles.TrimStart().IndexOf("^", StringComparison.Ordinal);
            var frame = subIndex == -1 ? 0 : subIndex * -frameTimeFactor;

            var isString = typeof(T) == typeof(string);
            var getValue = values == null
                ? new Func<char, object>(x => isString ? (object) x.ToString() : x)
                : x => values!.GetReflectedProperty<object>(x.ToString());

            var groupStart = -1L;

            for (var i = 0; i < len; i++)
            {
                var nextFrame = frame;

                void AdvanceFrameBy(long count) => nextFrame += count * frameTimeFactor;

                Notification<T>? notification = null;

                var c = marbles[i];
                switch (c)
                {
                    case ' ':
                        break;
                    case '-':
                        AdvanceFrameBy(1);
                        break;
                    case '(':
                        groupStart = frame;
                        AdvanceFrameBy(1);
                        break;
                    case ')':
                        groupStart = -1;
                        AdvanceFrameBy(1);
                        break;
                    case '|':
                        notification = Notification.CreateOnCompleted<T>();
                        AdvanceFrameBy(1);
                        break;
                    case '^':
                        AdvanceFrameBy(1);
                        break;
                    case '#':
                        notification = Notification.CreateOnError<T>(error ?? new Exception("error"));
                        AdvanceFrameBy(1);
                        break;
                    default:
                        // Might be time progression syntax, or a value literal
                        var progression = AdvanceTimeProgression(marbles, frameTimeFactor, c, i);

                        if (progression != default)
                        {
                            i += progression.Offset;
                            AdvanceFrameBy(progression.Duration);
                            break;
                        }

                        notification = Notification.CreateOnNext((T) getValue(c));
                        AdvanceFrameBy(1);
                        break;
                }

                if (notification != null)
                {
                    testMessages.Add(new Recorded<Notification<T>>(groupStart > -1 ? groupStart : frame, notification));
                }

                frame = nextFrame;
            }

            return testMessages;
        }

        static (long Duration, int Offset) AdvanceTimeProgression(string marbles, long frameTimeFactor, char c, int i)
        {
            if (char.IsDigit(c))
            {
                // Time progression must be preceded by at least one space
                // if it's not at the beginning of the diagram
                if (i == 0 || marbles[i - 1] == ' ')
                {
                    var buffer = marbles.Substring(i);
                    var match = Regex.Match(buffer, @"^([0-9]+(?:\.[0-9]+)?)(ms|s|m) ");

                    if (match.Success)
                    {
                        var duration = double.Parse(match.Groups[1].Value);
                        var unit = match.Groups[2].Value;
                        var durationInMs = TimeSpan.TicksPerMillisecond * unit switch
                        {
                            "ms" => duration,
                            "s" => duration * 1000,
                            "m" => duration * 1000 * 60,
                            _ => throw new Exception($"Invalid unit \"{unit}\"")
                        };

                        var durationInTicks = (long) (durationInMs / frameTimeFactor);
                        var offset = match.Groups[0].Value.Length - 1;
                        return (durationInTicks, offset);
                    }
                }
            }

            return default;
        }
    }
}
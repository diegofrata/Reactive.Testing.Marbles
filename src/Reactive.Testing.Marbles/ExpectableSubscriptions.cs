using System.Linq;
using Microsoft.Reactive.Testing;

namespace Reactive.Testing.Marbles
{
    public interface IExpectSubscriptions
    {
        void ToBe(params string[] marbles);
    }
    
    class ExpectableSubscriptions : IExpectSubscriptions
    {
        readonly IFlushableTest<Subscription> _flushableTest;
        readonly long _frameTimeFactor;
        readonly IMarbleParser _parser;
        readonly IAssertFactory _assertFactory;

        public ExpectableSubscriptions(IFlushableTest<Subscription> flushableTest, long frameTimeFactor, IMarbleParser parser, IAssertFactory assertFactory)
        {
            _flushableTest = flushableTest;
            _frameTimeFactor = frameTimeFactor;
            _parser = parser;
            _assertFactory = assertFactory;
        }

        public void ToBe(params string[] marbles)
        {
            _flushableTest.Expected = marbles.Select(x => _parser.ParseAsSubscriptions(x, _frameTimeFactor)).ToList();
            _flushableTest.AssertCallback = _assertFactory.CreateForSubscription();
        }
    }
}
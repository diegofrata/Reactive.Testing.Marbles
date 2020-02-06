using System.Reactive.Linq;
using NUnit.Framework;

namespace Reactive.Testing.Marbles.NUnit.Tests
{
    [TestFixture]
    public class MarbleSpecReactiveTestTests : MarbleSpecReactiveTest
    {
        [Test]
        public void Should_expect_uppercase_chars()
        {
            Spec(@"
                e1  : ---a-------c----
                e2  : -------b-------d
                r   : ---A---B---C--
                sub : ^------------!
            ");

            var e1 = Cold<char>("e1");
            var e2 = Cold<char>("e2");
            var r = e1.Merge(e2).Select(x => x.ToString().ToUpper()[0]);

            ExpectObservable(r, "sub").ToBe("r");
        }
    }
}
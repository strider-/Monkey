using Monkey.Evaluation;
using Xunit;

namespace Monkey.Tests
{
    [Trait("Object", "")]
    public class ObjectTests
    {
        [Fact]
        public void StringHashKeys_Work_As_Expected()
        {
            var hello1 = new String("Hello World");
            var hello2 = new String("Hello World");
            var diff1 = new String("My Name Is Johnny");
            var diff2 = new String("My Name Is Johnny");

            Assert.Equal(hello1.GetHashCode(), hello2.GetHashCode());
            Assert.Equal(diff1.GetHashCode(), diff2.GetHashCode());
            Assert.NotEqual(hello1.GetHashCode(), diff1.GetHashCode());
        }
    }
}

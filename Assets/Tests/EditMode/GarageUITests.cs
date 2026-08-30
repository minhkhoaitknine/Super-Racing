using NUnit.Framework;
using SuperRacing.UI;

namespace SuperRacing.Tests
{
    public sealed class GarageUITests
    {
        [TestCase(-1, 3, 2)]
        [TestCase(0, 3, 0)]
        [TestCase(3, 3, 0)]
        [TestCase(4, 3, 1)]
        [TestCase(10, 0, 0)]
        public void WrapsSelectionIndex(int index, int count, int expected)
        {
            Assert.That(GarageUI.WrapIndex(index, count), Is.EqualTo(expected));
        }
    }
}

using NUnit.Framework;
using SuperRacing.UI;

namespace SuperRacing.Tests
{
    public sealed class RaceHUDTests
    {
        [TestCase(0f, "00:00.000")]
        [TestCase(-5f, "00:00.000")]
        [TestCase(9.25f, "00:09.250")]
        [TestCase(60f, "01:00.000")]
        [TestCase(125.5f, "02:05.500")]
        public void FormatsRaceTime(float seconds, string expected)
        {
            Assert.That(RaceHUD.FormatTime(seconds), Is.EqualTo(expected));
        }
    }
}

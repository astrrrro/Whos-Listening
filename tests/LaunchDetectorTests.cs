using System;

namespace WhosListening
{
    public static class LaunchDetectorTests
    {
        public static void TestFiresOnlyOnRisingEdge()
        {
            LaunchDetector d = new LaunchDetector();
            DateTime now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            bool[] input = new bool[] { false, true, true, false, true };
            bool[] expected = new bool[] { false, true, false, false, true };
            for (int i = 0; i < input.Length; i++)
            {
                bool got = d.Poll(input[i], now.AddMilliseconds(700 * i), DateTime.MinValue);
                Assert.Equal(expected[i], got, "paso " + i);
            }
        }

        public static void TestSuppressedWithin10sOfSelfLaunch()
        {
            DateTime now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            LaunchDetector a = new LaunchDetector();
            Assert.Equal(false, a.Poll(false, now, DateTime.MinValue), "a sin Spotify");
            Assert.Equal(false, a.Poll(true, now, now.AddSeconds(-3)), "hace 3 s");

            LaunchDetector b = new LaunchDetector();
            Assert.Equal(false, b.Poll(false, now, DateTime.MinValue), "b sin Spotify");
            Assert.Equal(true, b.Poll(true, now, now.AddSeconds(-11)), "hace 11 s");
        }

        public static void TestFirstPollRunningFires()
        {
            LaunchDetector d = new LaunchDetector();
            DateTime now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            Assert.Equal(true, d.Poll(true, now, DateTime.MinValue), "primer poll con Spotify abierto");
        }
    }
}

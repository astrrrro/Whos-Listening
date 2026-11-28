using System;

namespace WhosListening
{
    public class LaunchDetector
    {
        private static readonly TimeSpan SelfLaunchWindow = TimeSpan.FromSeconds(10);
        private bool wasRunning;

        public bool Poll(bool spotifyRunning, DateTime nowUtc, DateTime lastSelfLaunchUtc)
        {
            bool rising = spotifyRunning && !wasRunning;
            wasRunning = spotifyRunning;
            if (!rising) return false;
            return (nowUtc - lastSelfLaunchUtc) >= SelfLaunchWindow;
        }
    }
}

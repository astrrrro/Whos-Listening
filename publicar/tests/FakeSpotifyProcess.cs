using System;

namespace WhosListening
{
    public class FakeSpotifyProcess : ISpotifyProcess
    {
        public bool Running;
        public bool CloseSucceeds = true;
        public int StartCalls;
        public int CloseCalls;
        public string LastStartedExe;

        public bool IsRunning() { return Running; }

        public bool CloseAndWait(TimeSpan timeout)
        {
            CloseCalls++;
            if (CloseSucceeds) Running = false;
            return CloseSucceeds;
        }

        public void Start(string exe)
        {
            StartCalls++;
            LastStartedExe = exe;
            Running = true;
        }

        public DateTime LastSelfLaunchUtc { get { return DateTime.MinValue; } }
    }
}

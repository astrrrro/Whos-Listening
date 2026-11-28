using System;
using System.Collections.Generic;

namespace WhosListening
{
    public class Account
    {
        public Account()
        {
            Auth = new Dictionary<string, string>();
        }

        public string Id { get; set; }
        public string Label { get; set; }
        public string Username { get; set; }
        public int Order { get; set; }
        public DateTime SavedAtUtc { get; set; }
        public Dictionary<string, string> Auth { get; set; }
    }
}

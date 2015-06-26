using System;

namespace SentinelDbUpdater.Trackers
{
    [Serializable]
    public class Contact
    {
        /// <summary>The submitter name</summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>The submitter email address</summary>
        public string Email
        {
            get;
            set;
        }

        /// <summary>The submitter organization</summary>
        public string Organization
        {
            get;
            set;
        }
    }
}

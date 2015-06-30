using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using Microsoft.IE.IEPortal.Data.Sentinel.DAL;
using Microsoft.IE.IEPortal.Data.Sentinel.DAL.DataModel;

namespace SentinelDbUpdater.Trackers
{
    /// <summary>
    /// Base class for tracker
    /// </summary>
    public class TrackerBase
    {

        public string TrackerName { get; set; }

        /// <summary>
        /// All trackers to implement function that retrieves the contribution infor between a date range
        /// </summary>
        /// <param name="since">start date of of time period</param>
        /// <param name="until">end date fo the time period</param>
        /// <returns>A list of contribution objects containing the spec action data</returns>
        public virtual List<Contribution> RetrieveDataFromSource(DateTime since, DateTime until)
        {
            return null;
        }

        /// <summary>
        /// Function runs the tracker
        /// </summary>
        /// <param name="since">start date of the time period</param>
        /// <param name="until">end date of time time period</param>
        public Tuple<int, int> RunTracker(DateTime since, DateTime until, bool isLocal)
        {
            var retrieveTime = new Stopwatch();
            var writeTime = new Stopwatch();

            var ds = SentinelDataService.GetInstance(isLocal);
            retrieveTime.Start();
            var contributionList = RetrieveDataFromSource(since, until);
            retrieveTime.Stop();
            
            // Format and display the TimeSpan value. 
            PrintLogger.WriteLine("Data retrieve completed. Runtime: " + elapsedTime(retrieveTime));
            PrintLogger.WriteLine("Writing to db...");
            
            writeTime.Start();
            var recordsAdded = ds.AddContributions(contributionList);
            writeTime.Stop();
            PrintLogger.WriteLine("Db write completed. Runtime: " + elapsedTime(writeTime));

            var recordsSkipped = contributionList.Count - recordsAdded;

            return new Tuple<int, int> (recordsAdded, recordsSkipped);
        }

        public string elapsedTime(Stopwatch sw)
        {
            TimeSpan ts = sw.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
            return elapsedTime;
        }

        
    }
}
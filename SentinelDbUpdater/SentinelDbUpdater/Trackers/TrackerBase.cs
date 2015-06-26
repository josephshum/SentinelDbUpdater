using System;
using System.Collections.Generic;
using Microsoft.IE.IEPortal.Data.Sentinel.DAL;
using Microsoft.IE.IEPortal.Data.Sentinel.DAL.DataModel;

namespace SentinelDbUpdater.Trackers
{
    /// <summary>
    /// Base class for tracker
    /// </summary>
    public class TrackerBase
    {
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
        public void RunTracker(DateTime since, DateTime until, bool isLocal)
        {

            var ds = SentinelDataService.GetInstance(isLocal);
            var contributionList = RetrieveDataFromSource(since, until);
            ds.AddContributions(contributionList);
        }
    }
}
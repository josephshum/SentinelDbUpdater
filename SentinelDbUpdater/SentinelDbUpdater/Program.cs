using System;
using System.Data.Entity.Migrations.Model;
using SentinelDbUpdater.Trackers;

namespace SentinelDbUpdater
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            PrintLogger.WriteLine("***********************************************************");
            PrintLogger.WriteLine("Sentinel Database Updater (" + DateTime.Now + ")");
            PrintLogger.WriteLine("");

            Console.WriteLine("Update Local (l) or Azure (a) ?");
            var isLocalDb_text = Console.ReadLine();
            bool isLocalDb = !isLocalDb_text.Equals("a");
            Console.WriteLine("How many months back?");
            var months = Console.ReadLine();

            var dbName = "Azure Db";
            if (isLocalDb) dbName = "Local Db";
                
            PrintLogger.WriteLine("Updating " + dbName + " going back " + months + " months.");

            UpdateData(new GithubTracker(), int.Parse(months), isLocalDb);

            UpdateData(new MailListTracker(), int.Parse(months), isLocalDb);

            PrintLogger.WriteLine("Completed...");
            Console.ReadLine();
        }

        public static void UpdateData(TrackerBase tracker, int numberOfMonths, bool isLocal)
        {
            //update
            var from = DateTime.Now.AddMonths(-numberOfMonths);
            var until = DateTime.Now;

            PrintLogger.WriteLine("Running " + tracker.TrackerName + " tracker from " + from + " to " + until + " [" + numberOfMonths + " months]");
            var stats = tracker.RunTracker(from, until, isLocal);
            PrintLogger.WriteLine("Added " + stats.Item1 + ", skipped " + stats.Item2 + " records from " + tracker.TrackerName);
            PrintLogger.WriteLine("");
        }

    }

    


}
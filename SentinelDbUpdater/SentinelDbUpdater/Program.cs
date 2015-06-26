using System;
using System.Data.Entity.Migrations.Model;
using SentinelDbUpdater.Trackers;

namespace SentinelDbUpdater
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Update Local (l) or Azure (a) ?");
            var isLocalDb_text = Console.ReadLine();
            var isLocalDb = true;

            if (isLocalDb_text.Equals("a"))
            {
                isLocalDb = false;
            }
            
            
            Console.WriteLine("How many months back?");
            var months = Console.ReadLine();
            
            UpdateGithubData(int.Parse(months), isLocalDb);

            UpdateMailListData(int.Parse(months), isLocalDb);


        }

        public static void UpdateGithubData(int numberOfMonths, bool isLocal)
        {
            //update
            var githubTracker = new GithubTracker();
            var from = DateTime.Now.AddMonths(-numberOfMonths);
            var until = DateTime.Now;

            Console.WriteLine("Running Github tracker from " + from + " to " + until);

            githubTracker.RunTracker(from, until, isLocal);
        }

        public static void UpdateMailListData(int numberOfMonths, bool isLocal)
        {
            //update
            var mailListTracker = new MailListTracker();
            var from = DateTime.Now.AddMonths(-numberOfMonths);
            var until = DateTime.Now;

            Console.WriteLine("Running MailList tracker from " + from + " to " + until);

            mailListTracker.RunTracker(from, until, isLocal);
        }


    }

    


}
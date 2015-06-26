using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.IE.IEPortal.Data.Sentinel.DAL.DataModel;
using Octokit;
using Author = Microsoft.IE.IEPortal.Data.Sentinel.DAL.DataModel.Author;
using Organization = Microsoft.IE.IEPortal.Data.Sentinel.DAL.DataModel.Organization;


namespace SentinelDbUpdater.Trackers
{

    /// <summary>
    /// Tracker retrieves commit data from Github
    /// </summary>
    public class GithubTracker: TrackerBase
    {

        private static readonly Regex SpecIdentifierRegex = new Regex(@"\[\s*(?:(?:css[0-9]{0,2}\s*-\s*(?<spec>[-a-z0-9@]+?)(?:-[0-9]+)?)|(?<spec>css[0-9]{1,2}(?:\.?[0-9]+))|(?<spec>[a-z*-]+))\s*\]", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        // hardcoded string to define what's being retrieve
        private const string Owner = "w3c";
        private const string Project = "csswg-drafts";
        private const string GithubUser = "ProjectSentinel";
        private const string GithubPassword = "HotelIndia1";

        /// <summary>
        /// Function gets data from Github
        /// </summary>
        /// <param name="since">Get data starting on this date</param>
        /// <param name="until">Get data up to this date</param>
        /// <returns></returns>
        public override List<Contribution> RetrieveDataFromSource(DateTime since, DateTime until)
        {
            var client = AuthenticateWithGitHub(GithubUser, GithubPassword);

            //DateTime.Now.AddYears(-1), DateTime.Now
            return GetAllCommitsInRepository(client, since, until);

        }

        /// <summary>
        /// Function uses given name and password and returns an authenticated Github client
        /// </summary>
        /// <param name="userName">Github username in string</param>
        /// <param name="userPassword">Github user password in string</param>
        /// <returns>Returns authenticated Github Client</returns>
        private static GitHubClient AuthenticateWithGitHub(string userName, string userPassword)
        {
            var basicAuth = new Credentials(userName, userPassword);
            var client = new GitHubClient(new ProductHeaderValue("tinyTestApp")) {Credentials = basicAuth};
            return client;
        }

        /// <summary>
        /// Function gets all data of all commits between "since" and "until"
        /// </summary>
        /// <param name="client">authenticated Github client</param>
        /// <param name="since">start date of the time period</param>
        /// <param name="until">end date of the time period</param>
        /// <returns>A list of Contribution objects containg Github commit data in the date range.</returns>
        private List<Contribution> GetAllCommitsInRepository(GitHubClient client, DateTime since, DateTime until)
        {
            //consoleAndWrite(sb, "Query commits between " + since.ToString() + " and " + until.ToString() + "...");

            var commitsRepository = client.Repository.Commits; //why commits here?

            var request = new CommitRequest()
            {
                //Path = path, // use path here to specify folders
                Since = since,
                Until = until
            };

            var commits = commitsRepository.GetAll(Owner, Project, request).Result;

            return (from commit in commits let c = commit.Commit where c != null where c.Message != null select CreateContribution(c, commit.Sha)).ToList();
        }

        /// <summary>
        /// Regex extracts the spec name from a given string.
        /// </summary>
        /// <param name="input">The glob of text to search</param>
        /// <returns>The spec string in lowercase.</returns>
        private static string RegexSpecName(string input)
        {
            var specName = "unknown";
            var m = SpecIdentifierRegex.Match(input.ToLower());

            if (m.Success)
            {
                specName = m.Groups["spec"].Value;
            }

            return specName;
        }

        /// <summary>
        /// Creates a contribution object given a string
        /// </summary>
        /// <param name="c">The commit object from Github</param>
        /// <param name="sha">The unique Sha from the Github commit object</param>
        /// <returns></returns>
        Contribution CreateContribution(Commit c, string sha)
        {

            var fact = new Contribution()
            {
                Sha = sha,
                Author = new Author()
                {
                    Name = c.Author.Name,
                    Email = c.Author.Email
                },
                Date = c.Author.Date.UtcDateTime,
                Email = c.Author.Email,
                Message = c.Message,

                Spec = new Spec()
                {
                    Name = RegexSpecName(c.Message)
                },

                Organization = new Organization()
                {
                    Name = TrackerData.GetNameAndOrganization(c.Author.Email)[1]
                },
                Tracker = new Tracker()
                {
                    Name = "Github"
                },
                Url = c.Url
            };

            return fact;

        }
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IE.IEPortal.Data.Sentinel.DAL.DataModel;


namespace Microsoft.IE.IEPortal.Data.Sentinel.DAL
{
    public class SentinelDataService
    {
        private static SentinelDataService _instance;
        private readonly SentinelDbContext _db;


        private const string LocalConnectionString = @"Data Source=(LocalDb)\v11.0; initial catalog=SentinelDb; Integrated Security=SSPI;";
        private const string AzureConnectionString = @"Server=tcp:hb1dd4b0zq.database.windows.net,1433;Database=ieportalsql-project-sentinel;User ID=hp6Fur@hb1dd4b0zq;Password=3r4NW~5fT3K2iYv;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;";


        public SentinelDataService(bool isLocal)
        {
            _db = isLocal ? new SentinelDbContext(LocalConnectionString) : new SentinelDbContext(AzureConnectionString);
            _db.Configuration.LazyLoadingEnabled = true;
        }

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static SentinelDataService GetInstance(bool isLocal)
        {
            return _instance ?? (_instance = new SentinelDataService(isLocal));
        }

        public int AddContributions(List<Contribution> contributions)
        {
            // makeing sure dups don't get added
            // retrieve all sha from db and save to hash table
            // also note that this assumes every sha from the db is different
            var recordsAdded = 0;
            var existingSha = (from c in _db.Contributions select c.Sha).ToDictionary(sha => sha, sha => false);
            // only add contribution if the sha is not found in the hash
            foreach (var c in contributions.Where(c => !existingSha.ContainsKey(c.Sha)))
            {
                existingSha.Add(c.Sha, false);
                AddContribution(c);
                recordsAdded++;
            }

            return recordsAdded;
        }

        public void AddContribution(Contribution contribution)
        {

            // TODO: Tables already have indices. Ask Richard how to make this more efficient so don't need "FirstOrDefault"
            // Retrieve existing foreign keys if they exist
            var author = _db.Authors.FirstOrDefault(a => a.Name.Equals(contribution.Author.Name));
            if (author != null) contribution.Author = author;

            var spec = _db.Specs.FirstOrDefault(a => a.Name.Equals(contribution.Spec.Name));
            if (spec != null) contribution.Spec = spec;

            var tracker = _db.Trackers.FirstOrDefault(a => a.Name.Equals(contribution.Tracker.Name));
            if (tracker != null) contribution.Tracker = tracker;

            var organization = _db.Organizations.FirstOrDefault(a => a.Name.Equals(contribution.Organization.Name));
            if (organization != null) contribution.Organization = organization;

            _db.ContributionsInternal.Add(contribution);

            _db.SaveChanges();
        }


        public void ClearAllTables()
        {
            // Use truncate here because there are thousands of records here
            _db.Database.ExecuteSqlCommand("TRUNCATE TABLE [Contribution]");

            // Unable to truncate the following due to foreign key contraints.
            // Nnumber of entries below is pretty low anyway, should not stress the db too much.
            _db.Database.ExecuteSqlCommand("DELETE FROM Tracker;");
            _db.Database.ExecuteSqlCommand("DELETE FROM Spec;");
            _db.Database.ExecuteSqlCommand("DELETE FROM Organization");
            _db.Database.ExecuteSqlCommand("DELETE FROM Author");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            if (_db != null)
            {
                _db.Dispose();
            }
        }
    }
}

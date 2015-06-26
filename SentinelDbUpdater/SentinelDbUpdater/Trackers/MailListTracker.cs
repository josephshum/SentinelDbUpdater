using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.IE.IEPortal.Data.Sentinel.DAL.DataModel;

namespace SentinelDbUpdater.Trackers
{
    /// <summary>A tracker for getting data from W3C mail list web pages</summary>
    public class MailListTracker: TrackerBase
    {
        /// <summary>The name of the tracker</summary>
        private const string Tracker = "MailList";

        /// <summary>Retrieves the full list of mails from a web page</summary>
        private static readonly Regex MailListIdentifier = new Regex(@"<div.*?class\s*=\s*(?<q>[""']?)messages-list\k<q>.*?>(?<messages>.*?)<div.*?class\s*=\s*(?<q2>[""']?)foot\k<q2>.*?>", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>Separates out the date and the mails for that date</summary>
        private static readonly Regex DateMaildentifier = new Regex(@"<li>.*?<dfn>(?<date>.+?)</dfn><ul>(?<mails>.+?)</ul>.*?</li>", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex RecordIdentifier = new Regex(@"href\s*=\s*(?<q>[""'])(?<url>[^""']+)\k<q>.*?>(?<subject>[^<]+).*?<em.*?>(?<author>[^<]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>Identifies each component of a particular mail</summary>
        private static readonly Regex SpecIdentifier = new Regex(@"\[\s*(?:(?:css[0-9]{0,2}\s*-\s*(?<spec>[-a-z0-9@]+?)(?:-[0-9]+)?)|(?<spec>css[0-9]{1,2}(?:\.?[0-9]+))|(?<spec>[a-z*-]+))\s*\]", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        /// <summary>Identifies the contents of an email message, the email and author name</summary>
        private static readonly Regex MailContents = new Regex(@"<dfn>From</dfn>:\s*(?<author>.*?)\s*&lt;[^>]*>(?<email>.*?)</a\s*>.*?<pre.*?id\s*=\s*(?<q>[""']?)body\k<q>.*?(?:</.*?>)(?<message>.*?)</pre\s*>", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>Gets the mail list data from the mail list data source</summary>
        /// <param name="startDate">Start date for mail list retrieval</param>
        /// <param name="endDate">End date for mail list retrieval</param>
        /// <returns>A list of mail items</returns>
        public override List<Contribution> RetrieveDataFromSource(DateTime startDate, DateTime endDate)
        {

            var pastDate = startDate;
            var monthSpan = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;

            var d = new List<Contribution>();
            for (var i = 0; i <= monthSpan; i++)
            {
                var month = pastDate.AddMonths(i);
                var format = string.Format("{0:yyyyMMM}", month);
                var u = new Uri("https://lists.w3.org/Archives/Public/www-style/" + format + "/");
                d.AddRange(ProcessUrl(u));
            }

            return d;

        }

        /// <summary>Processes and URL and gets the data for each mail item in a mail list</summary>
        /// <param name="url">The Mail list URL to scan</param>
        /// <returns>A list of mail items</returns>
        private IEnumerable<Contribution> ProcessUrl(Uri url)
        {
            var returnValue = new List<Contribution>();

            string responseData;
            var webRequest = WebRequest.CreateHttp(url);
            var httpResponse = (HttpWebResponse)webRequest.GetResponse();

            var stream = httpResponse.GetResponseStream();
            if (null == stream)
            {
                return returnValue;
            }
            using (var responseReader = new StreamReader(stream))
            {
                responseData = responseReader.ReadToEnd();
            }

            var mailListItem = MailListIdentifier.Match(responseData);

            var dateMailMatches = DateMaildentifier.Matches(mailListItem.Groups["messages"].Value);
            foreach (Match dateMails in dateMailMatches)
            {
                var itemsDateString = dateMails.Groups["date"].Value;
                DateTime itemsDate;
                if (!DateTime.TryParse(itemsDateString, out itemsDate))
                {
                    continue;
                }
                var mailItems = dateMails.Groups["mails"].Value;

                foreach (Match matchedRecord in RecordIdentifier.Matches(mailItems))
                {
                    var r = new Contribution
                    {
                        Date = itemsDate,
                        Tracker = new Tracker() {Name = Tracker}
                    };

                    var mailUri = new Uri(url, matchedRecord.Groups["url"].Value);

                    r.Url = mailUri.AbsoluteUri;

                    r.Author = new Author()
                    {
                        Name = matchedRecord.Groups["author"].Value
                    };

                    r.Message = HttpUtility.HtmlDecode(matchedRecord.Groups["subject"].Value);

                    string[] emailFullMessage;
                    if(SpecIdentifier.IsMatch(r.Message))
                    {
                        foreach (var spec in from Match specRecord in SpecIdentifier.Matches(r.Message) select specRecord.Groups["spec"].Value.IsNullOrEmptyOrWhiteSpace("unknown").ToLowerInvariant())
                        {
                            r.Sha = (r.Url + spec).GetHashSha1();

                            if (CheckContributionsForSha(r.Sha)) continue;
                            var s = r.DeepClone();
                            //if it is not in DB then go here
                            emailFullMessage = GetEmailAndFullMessage(mailUri);
                            s.Email = emailFullMessage[0];

                            SetAuthorEmailOrganization(s);

                            s.Spec = new Spec()
                            {
                                Name = spec
                            };

                            returnValue.Add(s);//submit record to DB
                        }
                    }
                    else
                    {
                        r.Sha = r.Url.GetHashSha1();
                        if (CheckContributionsForSha(r.Sha))
                        {
                            continue;
                        }
                        //if it is not in DB then go here
                        emailFullMessage = GetEmailAndFullMessage(mailUri);
                        r.Email = emailFullMessage[0];

                        SetAuthorEmailOrganization(r);

                        r.Spec = new Spec()
                        {
                            Name = matchedRecord.Groups["spec"].Value.IsNullOrEmptyOrWhiteSpace("unknown").ToLowerInvariant()
                        };

                        if(r.Spec.Name.Equals("unknown", StringComparison.InvariantCultureIgnoreCase))
                        {
                            r.Spec = new Spec()
                            {
                                Name = TrackerData.DetermineIntent(emailFullMessage[1])
                            };
                        }

                        //if spec is still unknown don't add it to DB
                        if(!r.Spec.Name.Equals("unknown", StringComparison.InvariantCultureIgnoreCase))
                        {
                            returnValue.Add(r);//submit record to DB
                        }
                    }
                }
            }

            return returnValue;
        }

        /// <summary>Sets the Author, Email and Organization for the Contribution</summary>
        /// <param name="r">The current Contribution record to update</param>
        private static void SetAuthorEmailOrganization(Contribution r)
        {
            string organization;
            if(!string.IsNullOrWhiteSpace(r.Email))
            {
                var nameAndOrganization = TrackerData.GetNameAndOrganization(r.Email);

                if(!string.IsNullOrWhiteSpace(nameAndOrganization[0]) && string.IsNullOrWhiteSpace(r.Author.Name))
                {
                    r.Author.Name = nameAndOrganization[0];
                }
                organization = nameAndOrganization[1];
            }
            else
            {
                var emailAndOrganization = TrackerData.GetEmailAndOrganization(r.Author.Name);

                r.Email = emailAndOrganization[0].ToLowerInvariant();
                organization = emailAndOrganization[1];
            }

            r.Author.Email = r.Email.IsNullOrEmptyOrWhiteSpace("unknown");
            r.Organization = new Organization()
            {
                Name = organization
            };
        }

        /// <summary>Get the email message and the email address</summary>
        /// <param name="url">The url of the email message</param>
        /// <returns>If the item is found then returns the email address and the full message, otherwise it returns "unknown", null</returns>
        private static string[] GetEmailAndFullMessage(Uri url)
        {
            var returnValue = new []{ null, (string) null};

            var webRequest = WebRequest.CreateHttp(url);
            var httpResponse = (HttpWebResponse)webRequest.GetResponse();

            if(httpResponse.StatusCode == HttpStatusCode.OK)
            {
                string responseData;
                var stream = httpResponse.GetResponseStream();
                if(null == stream)
                {
                    return returnValue;
                }
                using(var responseReader = new StreamReader(stream))
                {
                    responseData = responseReader.ReadToEnd();
                }

                var m = MailContents.Match(responseData);

                var message = m.Groups["message"].Value;
                var email = HttpUtility.HtmlDecode(m.Groups["email"].Value).IsNullOrEmptyOrWhiteSpace("unknown").ToLowerInvariant();

                returnValue = new[] { email, message };
            }
            else
            {
                throw new Exception("W3C server blocked access, DDOS attack detected");
            }

            return returnValue;
        }

        /// <summary>Check the DB Contributions table for SHA1</summary>
        /// <param name="sha1">The SHA1 for the URL for the email message</param>
        /// <returns>true if the SHA1 is found in the DB; otherwise false</returns>
        private static bool CheckContributionsForSha(string sha1)
        {
            return false;
        }
    }
}
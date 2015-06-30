using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Hosting;
using System.Xml;

namespace SentinelDbUpdater.Trackers
{
    public static class TrackerData
    {
        private static readonly Regex EmailDomainIdentifier = new Regex(@"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?<domain>(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+)[a-z0-9](?:[a-z0-9-]*[a-z0-9])?", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        private const string NameOrgXmlPath = @"Trackers\NameEmailOrganization.xml";

        //private string intentXmlPath = "~/Areas/Standards/Trackers/determineIntent.xml";

        /// <summary>Stores contact information retrieved from XML file</summary>
        private static readonly List<Contact> Contacts = new List<Contact>();

        /// <summary>Stores intent data and string for determining intent in emails and selecting spec</summary>
        //private static List<Contact> intent = new List<Contact>();

        /// <summary>Get the submitters name and organization</summary>
        /// <param name="email">email address for a submitter</param>
        /// <returns>If an email matches then returns the users name and organization, otherwise "unknown", "unknown"</returns>
        public static string[] GetNameAndOrganization(string email)
        {
            if(Contacts.Count == 0)
            {
                ProcessXmlFile(NameOrgXmlPath, null);
            }

            var person = Contacts.Find(i => i.Email == email);

            var org = GetPseudoOrg(person, email);

            var personName = string.Empty;
            if (null != person)
            {
                personName = person.Name;
            }

            return new[] { personName, org };
        }

        /// <summary>Get the submitters email and organization</summary>
        /// <param name="author">Full name of a submitter</param>
        /// <returns>If a name matches then returns the users email and organization, otherwise "unknown", "unknown"</returns>
        public static string[] GetEmailAndOrganization(string author)
        {
            if(Contacts.Count == 0)
            {
                ProcessXmlFile(NameOrgXmlPath, null);
            }

            var person = Contacts.Find(i => i.Name == author);

            var personEmail = string.Empty;
            if (null != person)
            {
                personEmail = person.Email;
            }

            var org = GetPseudoOrg(person, personEmail);

            return new[] { personEmail, org };
        }

        /// <summary>Detemines the Organization from the emailer's email address</summary>
        /// <param name="contact">The original Organization</param>
        /// <param name="email">Users email address</param>
        /// <returns>The Organization or the organization determined by email address</returns>
        private static string GetPseudoOrg(Contact contact, string email)
        {
            if((null != contact) && (!string.IsNullOrWhiteSpace(contact.Organization)))
            {
                return contact.Organization;
            }
            var returnValue = EmailDomainIdentifier.Match(email).Groups["domain"].Value.TrimEnd('.');

            if(returnValue.IndexOf('.') != -1)
            {
                return returnValue;
            }
            var ti = new CultureInfo("en-US", false).TextInfo;
            returnValue = ti.ToTitleCase(returnValue);

            if(returnValue.Length <= 3)
            {
                returnValue = returnValue.ToUpperInvariant();
            }

            //HACK HACK
            returnValue = returnValue.Equals("chromium", StringComparison.InvariantCultureIgnoreCase) ? "Google" : returnValue;

            return returnValue;
        }

        private static string ResolvePath(string xmlFile)
        {
            //var path = HostingEnvironment.MapPath(xmlFile);
            //if(string.IsNullOrWhiteSpace(path))
            //{
            //    // We're not hosted in IIS. Try another mechanism
            //    throw new NotImplementedException();
            //}

            //return path;
            return xmlFile;
        }

        /// <summary>Processes the XML file for usage</summary>
        /// <param name="xmlFile">The file path of the XML file</param>
        /// <param name="schemaFile">The schema file to use for validating the XML file</param>
        /// <returns>true if the xmlFile is processed correctly, otherwise false</returns>
        private static void ProcessXmlFile(string xmlFile, string schemaFile)
        {
            if(string.IsNullOrWhiteSpace(xmlFile))
            {
                return;
            }
            var settings = new XmlReaderSettings();
            if(!string.IsNullOrWhiteSpace(schemaFile))
            {
                settings.Schemas.Add(string.Empty, schemaFile);
            }
            settings.ValidationType = ValidationType.Schema;

            var xmlFilePath = ResolvePath(xmlFile);
            using(var reader = XmlReader.Create(xmlFilePath, settings))
            {
                var doc = new XmlDocument();
                doc.Load(reader);

                var root = doc.DocumentElement;
                if(null == root)
                {
                    return;
                }
                var xmlNodeList = root.SelectNodes("contact");
                if(null == xmlNodeList)
                {
                    return;
                }
                foreach(var emailer in from XmlNode xn in xmlNodeList
                                       select new Contact
                                       {
                                           Email = xn.GetAttribute("email", "unknown"),
                                           Name = xn.GetAttribute("name", "unknown"),
                                           Organization = xn.GetAttribute("org", "unknown")
                                       })
                {
                    Contacts.Add(emailer);
                }
            }
        }

        public static string DetermineIntent(string email)
        {
            //if(intent.Count == 0)
            //{
            //    processXmlFile(intentXmlPath, null);
            //}
            //analyze email message for intent to understand which spec/secton this message pertains to
            return "unknown";
        }
    }
}
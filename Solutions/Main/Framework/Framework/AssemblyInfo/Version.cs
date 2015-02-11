//-----------------------------------------------------------------------
// <copyright file="Version.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// This task is based on the AssemblyInfo task written by Neil Enns (http://code.msdn.microsoft.com/AssemblyInfoTaskvers). It is used here with permission.
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    internal class Version
    {
        private string versionString;

        public Version()
        {
            this.MajorVersion = "1";
            this.MinorVersion = "0";
            this.BuildNumber = "0";
            this.Revision = "0";
        }

        public Version(string version) : this(version, false)
        {
        }

        public Version(string version, bool isAssemblyVersion)
        {
            if (isAssemblyVersion)
            {
                this.ParseAssemblyVersion(version);
            }
            else
            {
                this.ParseVersion(version);
            }
        }

        public string VersionString
        {
            get { return this.versionString; }
            set { this.ParseVersion(value); }
        }

        public string MajorVersion { get; set; }

        public string MinorVersion { get; set; }

        public string BuildNumber { get; set; }

        public string Revision { get; set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}", this.MajorVersion, this.MinorVersion, this.BuildNumber, this.Revision);
        }

        private static string ValidateAssemblyVersionPart(string part)
        {
            return string.IsNullOrEmpty(part) || part == "*" ? "0" : part;
        }

        private void ParseAssemblyVersion(string version)
        {
            Regex versionPattern = new Regex(@"(?<majorVersion>(\d+))(\.(?<minorVersion>(\d+)))(\.(?<buildNumber>(\d+|\*)))?(\.(?<revision>(\d+|\*)))?", RegexOptions.Compiled);

            MatchCollection matches = versionPattern.Matches(version);
            if (matches.Count != 1)
            {
                throw new ArgumentException("The specified string \"" + version + "\" is not a valid AssemblyVersion number", "version");
            }

            this.MajorVersion = matches[0].Groups["majorVersion"].Value;
            this.MinorVersion = matches[0].Groups["minorVersion"].Value;
            this.BuildNumber = ValidateAssemblyVersionPart(matches[0].Groups["buildNumber"].Value);
            this.Revision = ValidateAssemblyVersionPart(matches[0].Groups["revision"].Value);
            this.versionString = version;
        }

        private void ParseVersion(string version)
        {
            Regex versionPattern = new Regex(@"(?<majorVersion>(\d+))(\.(?<minorVersion>(\d+)))(\.(?<buildNumber>(\d+)))(\.(?<revision>(\d+)))", RegexOptions.Compiled);

            MatchCollection matches = versionPattern.Matches(version);
            if (matches.Count != 1)
            {
                throw new ArgumentException("The specified string \"" + version + "\" is not a valid version number", "version");
            }

            this.MajorVersion = matches[0].Groups["majorVersion"].Value;
            this.MinorVersion = matches[0].Groups["minorVersion"].Value;
            this.BuildNumber = matches[0].Groups["buildNumber"].Value;
            this.Revision = matches[0].Groups["revision"].Value;
            this.versionString = version; // Very important that this is a little v, not big v, otherwise you get infinite recursion!
        }
    }
}
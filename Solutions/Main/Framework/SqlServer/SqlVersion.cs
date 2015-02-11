//-----------------------------------------------------------------------
// <copyright file="SqlVersion.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// This task is based on a submission by Stephen Nuchia.
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.SqlServer
{
    using System.Globalization;
    using System.Linq;
    using System.Transactions;
    using Microsoft.Build.Framework;

    /// <summary>
    /// The SqlVersion task provides the ability to manage multiple build versions in a simple database table.
    /// <para />
    /// <b>Valid TaskActions are:</b>
    /// <para><i>GetVersion</i> (<b>Required: </b>BuildName, DatabaseName <b>Optional: </b>Delimiter, FieldToIncrement, PaddingCount, PaddingDigit <b>Output: </b>Build, Major, Minor, Revision, Version)</para>
    /// <para><b>Remote Execution Support:</b> Yes</para>
    /// </summary>
    /// <remarks>
    /// <para/>
    /// The following TSql can be used to create the supported table structure:
    /// <para/>
    /// USE [YOURDATABASENAME]<para/>
    /// GO<para/>
    /// <para/>
    /// IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BuildNumbers]') AND type in (N'U'))<para/>
    /// DROP TABLE [dbo].[BuildNumbers];<para/>
    /// GO<para/>
    /// <para/>
    /// SET ANSI_NULLS ON;<para/>
    /// GO<para/>
    /// SET QUOTED_IDENTIFIER ON;<para/>
    /// GO<para/>
    /// SET ANSI_PADDING ON;<para/>
    /// GO<para/>
    /// <para/>
    /// CREATE TABLE [dbo].[BuildNumbers](<para/>
    ///     [SequenceName] [varchar](50) NOT NULL,<para/>
    ///     [Major] [int] NOT NULL,<para/>
    ///     [Minor] [int] NOT NULL,<para/>
    ///     [Build] [int] NOT NULL,<para/>
    ///     [Increment] [int] NOT NULL,<para/>
    ///  CONSTRAINT [PK_BuildNumbers_1] PRIMARY KEY CLUSTERED <para/>
    /// (<para/>
    ///     [SequenceName] ASC<para/>
    /// )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]<para/>
    /// ) ON [PRIMARY];<para/>
    /// GO<para/>
    /// <para/>
    /// SET ANSI_PADDING OFF;<para/>
    /// GO<para/>
    /// <para/>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>GetVersion</i> (<b>Required: </b> BuildName, DatabaseName <b>Optional:</b> FieldToIncrement, Delimiter, PaddingCount, PaddingDigit <b>Output: </b>Major, Minor, Build, Revision, Version)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </remarks>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Target Name="BuildNumberOverrideTarget">
    ///     <PropertyGroup>
    ///         <FieldToIncrement Condition=" '$(IncrementalBuild)'=='true' ">4</FieldToIncrement>
    ///         <FieldToIncrement Condition=" '$(IncrementalBuild)'!='true' ">3</FieldToIncrement>
    ///     </PropertyGroup>
    ///     <!-- Get the latest build number without incrementing it -->
    ///     <MSBuild.ExtensionPack.SqlServer.SqlVersion Taskaction="GetVersion" BuildName="V9 Production Build" DatabaseName="Mike">
    ///         <Output TaskParameter="Version" PropertyName="LatestVersion" />
    ///     </MSBuild.ExtensionPack.SqlServer.SqlVersion>
    ///     <Message Text="LatestVersion is: $(LatestVersion)"/>
    ///     <!-- Get the latest build number and increment as necessary -->
    ///     <MSBuild.ExtensionPack.SqlServer.SqlVersion Taskaction="GetVersion" BuildName="V9 Production Build" FieldToIncrement="$(FieldToIncrement)" DatabaseName="Mike">
    ///         <Output TaskParameter="Major" PropertyName="BuildMajor" />
    ///         <Output TaskParameter="Minor" PropertyName="BuildMinor" />
    ///         <Output TaskParameter="Build" PropertyName="BuildBuild" />
    ///         <Output TaskParameter="Revision" PropertyName="BuildRevision" />
    ///     </MSBuild.ExtensionPack.SqlServer.SqlVersion>
    ///     <!-- Override Team Build BuildNumber property -->
    ///     <PropertyGroup>
    ///         <BuildNumber>$(BuildMajor).$(BuildMinor).$(BuildBuild).$(BuildRevision)</BuildNumber>
    ///     </PropertyGroup>
    ///     <Message Text="BuildNumber is: $(BuildNumber)"/>
    ///     <!-- Export values so they can be seen by targets inside CoreCompile -->
    ///     <PropertyGroup>
    ///         <CustomPropertiesForBuild>$(CustomPropertiesForBuild);BuildMajor=$(BuildMajor);BuildMinor=$(BuildMinor);BuildBuild=$(BuildBuild);BuildRevision=$(BuildRevision)</CustomPropertiesForBuild>
    ///     </PropertyGroup>
    ///     <!-- Get the latest build number without incrementing it -->
    ///     <MSBuild.ExtensionPack.SqlServer.SqlVersion Taskaction="GetVersion" BuildName="V9 Production Build" DatabaseName="Mike">
    ///         <Output TaskParameter="Version" PropertyName="LatestVersion" />
    ///     </MSBuild.ExtensionPack.SqlServer.SqlVersion>
    ///     <Message Text="LatestVersion is: $(LatestVersion)"/>
    /// </Target>
    /// ]]></code>    
    /// </example>  
    public class SqlVersion : BaseTask
    {
        private int fieldToIncrement;
        private bool trustedConnection;
        private SqlVersionDataClass databaseLinq;
        private string delimiter = ".";

        /// <summary>
        /// Sets the Delimiter to use in the version number. Default is .
        /// </summary>
        public string Delimiter
        {
            get { return this.delimiter; }
            set { this.delimiter = value; }
        }

        /// <summary>
        /// Sets the number of padding digits to use, e.g. 4
        /// </summary>
        public int PaddingCount { get; set; }

        /// <summary>
        /// Sets the padding digit to use, e.g. 0
        /// </summary>
        public char PaddingDigit { get; set; }

        /// <summary>
        /// Gets the full four part Version
        /// </summary>
        [Output]
        public string Version { get; set; }

        /// <summary>
        /// The name of the build number sequence to query
        /// </summary>
        [Required]
        public string BuildName { get; set; }

        /// <summary>
        /// Number indicating which field is to be incremented.
        /// 0 = none (read out last number generated),
        /// 1-4 = Major, Minor, Build, Increment.
        /// </summary>
        public int FieldToIncrement
        {
            get { return this.fieldToIncrement; }
            set { this.fieldToIncrement = value; }
        }

        /// <summary>
        /// The name of the database whcih contains the BuildNumber table
        /// </summary>
        [Required]
        public string DatabaseName { get; set; }

        /// <summary>
        /// Major component of build number
        /// </summary>
        [Output]
        public int Major { get; set; }

        /// <summary>
        /// Minor component of build number
        /// </summary>
        [Output]
        public int Minor { get; set; }

        /// <summary>
        /// Build component of build number
        /// </summary>
        [Output]
        public string Build { get; set; }

        /// <summary>
        /// Revision component of build number
        /// </summary>
        [Output]
        public int Revision { get; set; }

        protected override void InternalExecute()
        {
            if (string.IsNullOrEmpty(this.UserName))
            {
                this.LogTaskMessage(MessageImportance.Low, "Using a Trusted Connection");
                this.trustedConnection = true;
            }

            string conStr = this.trustedConnection ? string.Format(CultureInfo.CurrentCulture, @"Data Source={0};Initial Catalog={1};Integrated Security=True", this.MachineName, this.DatabaseName) : string.Format(CultureInfo.CurrentCulture, @"Data Source={0};Initial Catalog={1};UID={2};PWD={3}", this.MachineName, this.DatabaseName, this.UserName, this.UserPassword);
            using (this.databaseLinq = new SqlVersionDataClass(conStr))
            {
                switch (this.TaskAction)
                {
                    case "GetVersion":
                        this.GetNextVersion();
                        break;
                    default:
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                        return;
                }
            }
        }

        private void GetNextVersion()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting version for: {0}", this.BuildName));

            using (TransactionScope ts = new TransactionScope())
            {
                var query = this.databaseLinq.BuildNumbers.Where(r => r.SequenceName == this.BuildName);
                
                var row = query.Single();
                switch (this.fieldToIncrement)
                {
                    case 1:
                        row.Major += 1;
                        row.Minor = row.Build = row.Increment = 0;
                        break;
                    case 2:
                        row.Minor += 1;
                        row.Build = row.Increment = 0;
                        break;
                    case 3:
                        row.Build += 1;
                        row.Increment = 0;
                        break;
                    case 4:
                        row.Increment += 1;
                        break;
                    case 0:
                        break;
                    default:
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid FieldToIncrement: {0}", this.FieldToIncrement));
                        return;
                }

                this.Major = row.Major;
                this.Minor = row.Minor;
                this.Build = row.Build.ToString(CultureInfo.CurrentCulture).PadLeft(this.PaddingCount, this.PaddingDigit);
                this.Revision = row.Increment;
                this.Version = string.Format(CultureInfo.CurrentCulture, "{0}{4}{1}{4}{2}{4}{3}", row.Major, row.Minor, row.Build.ToString(CultureInfo.CurrentCulture).PadLeft(this.PaddingCount, this.PaddingDigit), row.Increment, this.Delimiter);
                this.databaseLinq.SubmitChanges();
                ts.Complete();
            }
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="SqlLogger.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Loggers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <para>The SqlLogger can be used to log the output of builds to Microsoft SQLServer database</para>
    /// <para><b>Syntax: </b></para>
    /// <para>     /l:SqlLogger,MSBuild.ExtensionPack.Loggers.dll;BID=123;BN=YOURBuild;DS=MyServer;IC=YOURTable;SP=YOUrProcedure;CL;Verbosity=YOURVERBOSITY</para>
    /// <para><b>Parameters: </b></para>
    /// <para>BID (BUILDID/BUILDIDENTIFIER): An optional parameter that specifies the Build ID to associate with the build.</para>
    /// <para>BN (BUILDNAME): An optional parameter that specifies the Build Name to associate with the build.</para>
    /// <para>DS (DATASOURCE): An optional parameter that specifies the DataSource to use in the connectionstring. Defaults to "." (i.e. local).</para>
    /// <para>IC (INITIALCATALOG): An optional parameter that specifies the InitialCatalog to use in the connectionstring. Defaults to "MSBuildLogs".</para>
    /// <para>SP (STOREDPROCEDURE): An optional parameter that specifies the Stored Procedure to call. Defaults to "msbep_SqlLogger".</para>
    /// <para>CL (CLEARLOG): An optional parameter that clears the log for the specified BID before logging starts.</para>
    /// <para>Verbosity: An optional parameter that overrides the global verbosity setting for this logger only.</para>
    /// </summary>
    /// <example>
    /// <code lang="sql"><![CDATA[
    /// -- This script creates a sample database, table and procedure to be used by the SqlLogger
    /// -- Drop the database if it exists
    /// IF EXISTS (SELECT name FROM sys.databases WHERE name = N'MSBuildLogs')
    /// BEGIN
    ///     EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'MSBuildLogs'
    ///     USE [master]
    ///     ALTER DATABASE [MSBuildLogs] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
    ///     ALTER DATABASE [MSBuildLogs] SET SINGLE_USER 
    ///     DROP DATABASE [MSBuildLogs]
    /// END
    /// -- Create the database. Alter the paths as necessary to suite your environment
    /// CREATE DATABASE [MSBuildLogs] ON PRIMARY 
    /// ( NAME = N'MSBuildLogs', FILENAME = N'C:\a\MSBuildLogs.mdf' , SIZE = 3072KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
    /// LOG ON 
    /// ( NAME = N'MSBuildLogs_log', FILENAME = N'C:\a\MSBuildLogs_log.ldf' , SIZE = 1024KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
    /// GO
    /// SET ANSI_NULLS ON
    /// GO
    /// SET QUOTED_IDENTIFIER ON
    /// GO
    /// -- Create the table
    /// USE [MSBuildLogs]
    /// GO
    /// CREATE TABLE [MSBuildLogs].[dbo].[BuildLogs](
    ///     [id]        [int]            IDENTITY(1,1) NOT NULL,
    ///     [BuildId]    [int]            NULL,
    ///     [BuildName]    [nvarchar](100)    NULL,
    ///     [Event]        [nvarchar](50)    NOT NULL,
    ///     [Message]    [nvarchar](1500) NULL,
    ///     [EventTime]    [datetime]        NOT NULL
    /// ) ON [PRIMARY]
    /// GO
    /// -- Create a clustered index on the colums this table is likely to be searched on
    /// CREATE CLUSTERED INDEX [cidx_BuildId,EventTime] ON [MSBuildLogs].[dbo].[BuildLogs] 
    /// (
    /// [BuildId] ASC,
    /// [EventTime] ASC
    /// )WITH (STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    /// GO
    /// -- Create the stored procedure
    /// CREATE PROCEDURE [dbo].[msbep_SqlLogger]
    ///     @BuildId    int = NULL,
    ///     @BuildName    nvarchar(50) = NULL,
    ///     @Event        nvarchar(50),
    ///     @Message    nvarchar(1000) = NULL,
    ///     @ClearLog    bit = 0
    /// AS
    /// BEGIN
    /// DECLARE @EventTime DATETIME;
    /// SET @EventTime = GETDATE();
    /// IF ( @ClearLog = 1)
    /// BEGIN
    ///     DELETE FROM [dbo].BuildLogs
    ///     WHERE BuildId = @BuildId
    /// END
    /// INSERT INTO [dbo].BuildLogs (BuildId, BuildName, [Event], [Message], EventTime)
    /// VALUES    (@BuildId, @BuildName, @Event, @Message, @EventTime)
    /// END
    /// ]]></code>    
    /// </example> 
    public class SqlLogger : Logger
    {
        private static readonly char[] fileLoggerParameterDelimiters = new[] { ';' };
        private static readonly char[] fileLoggerParameterValueSplitCharacter = new[] { '=' };
        private int warnings;
        private int errors;
        private SqlConnection sqlConnection;
        private SqlCommand command;
        private DateTime startTime;
        private bool clearLog;
        private string datasource = ".";
        private string initialCatalog = "MSBuildLogs";
        private string storedProcedure = "msbep_SqlLogger";

        /// <summary>
        /// Initialize Override
        /// </summary>
        /// <param name="eventSource">IEventSource</param>
        public override void Initialize(IEventSource eventSource)
        {
            eventSource.BuildFinished += this.BuildFinished;
            eventSource.BuildStarted += this.BuildStarted;
            eventSource.ErrorRaised += this.ErrorRaised;
            eventSource.WarningRaised += this.WarningRaised;

            if (Verbosity != LoggerVerbosity.Quiet)
            {
                eventSource.MessageRaised += this.MessageRaised;
                eventSource.ProjectStarted += this.ProjectStarted;
                eventSource.ProjectFinished += this.ProjectFinished;
            }

            if (IsVerbosityAtLeast(LoggerVerbosity.Normal))
            {
                eventSource.TargetStarted += this.TargetStarted;
                eventSource.TargetFinished += this.TargetFinished;
            }

            if (IsVerbosityAtLeast(LoggerVerbosity.Detailed))
            {
                eventSource.TaskStarted += this.TaskStarted;
                eventSource.TaskFinished += this.TaskFinished;
            }

            this.command = new SqlCommand { CommandText = this.storedProcedure, CommandType = CommandType.StoredProcedure };
            this.ParseFileLoggerParameters();
            this.sqlConnection = new SqlConnection(string.Format(CultureInfo.InvariantCulture, "Data Source={0};Initial Catalog={1};Integrated Security=SSPI", this.datasource, this.initialCatalog));
            this.command.Connection = this.sqlConnection;

            SqlParameter param2 = new SqlParameter("@Event", SqlDbType.NVarChar, 50);
            this.command.Parameters.Add(param2);
            SqlParameter param3 = new SqlParameter("@Message", SqlDbType.NVarChar, 1000);
            this.command.Parameters.Add(param3);
            this.sqlConnection.Open();
        }

        /// <summary>
        /// Shutdown() is guaranteed to be called by MSBuild at the end of the build, after all 
        /// events have been raised.
        /// </summary>
        public override void Shutdown()
        {
            if (this.sqlConnection != null && this.sqlConnection.State != ConnectionState.Closed)
            {
                this.sqlConnection.Close();
            }
        }

        private void ParseFileLoggerParameters()
        {
            if (this.Parameters != null)
            {
                string[] strArray = this.Parameters.Split(fileLoggerParameterDelimiters);
                foreach (string[] strArray2 in from t in strArray where t.Length > 0 select t.Split(fileLoggerParameterValueSplitCharacter))
                {
                    this.ApplyFileLoggerParameter(strArray2[0], strArray2.Length > 1 ? strArray2[1] : null);
                }
            }
        }

        private void ApplyFileLoggerParameter(string parameterName, string parameterValue)
        {
            switch (parameterName.ToUpperInvariant())
            {
                case "BID":
                case "BUILDID":
                case "BUILDIDENTIFIER":
                    SqlParameter param4 = new SqlParameter("@BuildId", SqlDbType.Int) { Value = parameterValue };
                    this.command.Parameters.Add(param4);
                    break;
                case "BN":
                case "BUILDNAME":
                    SqlParameter param = new SqlParameter("@BuildName", SqlDbType.NVarChar, 100) { Value = parameterValue };
                    this.command.Parameters.Add(param);
                    break;
                case "DS":
                case "DATASOURCE":
                    this.datasource = parameterValue;
                    break;
                case "IC":
                case "INITIALCATALOG":
                    this.initialCatalog = parameterValue;
                    break;
                case "SP":
                case "STOREDPROCEDURE":
                    this.storedProcedure = parameterValue;
                    break;
                case "CL":
                case "CLEARLOG":
                    this.clearLog = true;
                    break;
                case "VERBOSITY":
                    this.Verbosity = (LoggerVerbosity)Enum.Parse(typeof(LoggerVerbosity), parameterValue);
                    break;
                case null:
                    return;
            }
        }

        private void BuildFinished(object sender, BuildFinishedEventArgs e)
        {
            this.WriteToSql("BuildFinished", e.Message);
            this.WriteToSql("TotalWarnings", this.warnings.ToString(CultureInfo.InvariantCulture));
            this.WriteToSql("TotalErrors", this.errors.ToString(CultureInfo.InvariantCulture));
            TimeSpan s = DateTime.UtcNow - this.startTime;
            this.WriteToSql("TimeElapsed", s.ToString());
        }

        private void BuildStarted(object sender, BuildStartedEventArgs e)
        {
            this.startTime = DateTime.Now;
            this.WriteToSql("BuildStarted", e.Message);
        }

        private void MessageRaised(object sender, BuildMessageEventArgs e)
        {
            if ((e.Importance == MessageImportance.High && IsVerbosityAtLeast(LoggerVerbosity.Minimal)) || (e.Importance == MessageImportance.Normal && IsVerbosityAtLeast(LoggerVerbosity.Normal)) || (e.Importance == MessageImportance.Low && IsVerbosityAtLeast(LoggerVerbosity.Detailed)))
            {
                this.WriteToSql("MessageRaised", e.Message);
            }
        }

        private void ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            this.WriteToSql("ProjectFinished", e.Message);
        }

        private void ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            this.WriteToSql("ProjectStarted", e.Message + "(" + e.ProjectFile + ")");
            if (IsVerbosityAtLeast(LoggerVerbosity.Diagnostic))
            {
                SortedDictionary<string, string> sortedProperties = new SortedDictionary<string, string>();
                foreach (DictionaryEntry k in e.Properties.Cast<DictionaryEntry>())
                {
                    sortedProperties.Add(k.Key.ToString(), k.Value.ToString());
                }

                foreach (var p in sortedProperties)
                {
                    this.WriteToSql("InitialProperty", p.Key + " = " + p.Value);
                }
            }
        }

        private void TargetFinished(object sender, TargetFinishedEventArgs e)
        {
            this.WriteToSql("TargetFinished", e.Message);
        }

        private void TargetStarted(object sender, TargetStartedEventArgs e)
        {
            this.WriteToSql("TargetStarted", e.Message);
        }

        private void TaskFinished(object sender, TaskFinishedEventArgs e)
        {
            this.WriteToSql("TaskFinished", e.Message);
        }

        private void TaskStarted(object sender, TaskStartedEventArgs e)
        {
            this.WriteToSql("TaskStarted", e.Message);
        }

        private void WarningRaised(object sender, BuildWarningEventArgs e)
        {
            string line = string.Format(CultureInfo.InvariantCulture, "{0}. Line: {1}, Column: {2}", e.Message, e.LineNumber, e.ColumnNumber);
            this.WriteToSql("WarningRaised", line);
            this.warnings++;           
        }

        private void ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            string line = string.Format(CultureInfo.InvariantCulture, "{0}. Line: {1}, Column: {2}: ", e.Message, e.LineNumber, e.ColumnNumber);
            this.WriteToSql("ErrorRaised", line);
            this.errors++;
        }

        private void WriteToSql(string eventName, string message)
        {
            if (this.clearLog)
            {
                SqlParameter param2 = new SqlParameter("@ClearLog", SqlDbType.Bit) { Value = true };
                this.command.Parameters.Add(param2);
            }

            if (!string.IsNullOrEmpty(message))
            {
                this.command.Parameters["@Message"].Value = message;
            }
            
            this.command.Parameters["@Event"].Value = eventName;
            this.command.ExecuteNonQuery();

            if (this.clearLog)
            {
                this.command.Parameters["@ClearLog"].Value = null;
                this.clearLog = false;
            }
        }
    }
}
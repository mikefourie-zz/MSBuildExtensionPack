//-----------------------------------------------------------------------
// <copyright file="SqlExecute.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.SqlServer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using MSBuild.ExtensionPack.SqlServer.Extended;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Execute</i> (<b>Required: </b> ConnectionString, Sql or Files <b>Optional:</b> CommandTimeout, Parameters, Retry, UseTransaction, IgnoreScriptErrors <b>Output: </b> FailedScripts)</para>
    /// <para><i>ExecuteRawReader</i> (<b>Required: </b> ConnectionString, Sql <b>Optional:</b> CommandTimeout, Parameters, Retry, UseTransaction, IgnoreScriptErrors <b>Output: </b> RawReaderResult, FailedScripts)</para>
    /// <para><i>ExecuteReader</i> (<b>Required: </b> ConnectionString, Sql <b>Optional:</b> CommandTimeout, Parameters, Retry, UseTransaction, IgnoreScriptErrors <b>Output: </b> ReaderResult, FailedScripts)</para>
    /// <para><i>ExecuteScalar</i> (<b>Required: </b> ConnectionString, Sql <b>Optional:</b> CommandTimeout, Parameters, Retry, UseTransaction, IgnoreScriptErrors <b>Output: </b> ScalarResult, FailedScripts)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <ItemGroup>
    ///         <Files Include="C:\a\Proc1.sql"/>
    ///         <Files Include="C:\a\Proc2.sql"/>
    ///         <Files Include="C:\a\Proc3.sql"/>
    ///         <File2s Include="C:\a\SQLQuery1.sql"/>
    ///         <File2s Include="C:\a\SQLQuery2.sql"/>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Execute SQL and return a scalar -->
    ///         <MSBuild.ExtensionPack.SqlServer.SqlExecute TaskAction="ExecuteScalar" Sql="Select GETDATE()" ConnectionString="Data Source=desktop\Sql2008;Initial Catalog=;Integrated Security=True">
    ///             <Output PropertyName="ScResult" TaskParameter="ScalarResult"/>
    ///         </MSBuild.ExtensionPack.SqlServer.SqlExecute>
    ///         <Message Text="$(ScResult)"/>
    ///         <!-- Execute SQL and return the result in raw text form -->
    ///         <MSBuild.ExtensionPack.SqlServer.SqlExecute TaskAction="ExecuteRawReader" Sql="Select * from sys.tables" ConnectionString="Data Source=desktop\Sql2008;Initial Catalog=;Integrated Security=True">
    ///             <Output PropertyName="RawResult" TaskParameter="RawReaderResult"/>
    ///         </MSBuild.ExtensionPack.SqlServer.SqlExecute>
    ///         <Message Text="$(RawResult)"/>
    ///         <!-- Execute SQL and return the result in an Item. Each column is available as metadata -->
    ///         <MSBuild.ExtensionPack.SqlServer.SqlExecute TaskAction="ExecuteReader" Sql="Select * from sys.tables" ConnectionString="Data Source=desktop\Sql2008;Initial Catalog=;Integrated Security=True">
    ///             <Output ItemName="RResult" TaskParameter="ReaderResult"/>
    ///         </MSBuild.ExtensionPack.SqlServer.SqlExecute>
    ///         <Message Text="%(RResult.Identity) - %(RResult.object_id)"/>
    ///         <!-- Execute some sql files -->
    ///         <MSBuild.ExtensionPack.SqlServer.SqlExecute TaskAction="Execute" Retry="true" UseTransaction="true" Files="@(Files)" ConnectionString="Data Source=desktop\Sql2008;Initial Catalog=;Integrated Security=True"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    [HelpUrl("http://www.msbuildextensionpack.com/help/4.0.4.0/html/0d864b98-649a-5454-76ea-bd3069fde8bd.htm")]
    public class SqlExecute : BaseTask
    {
        private static readonly Regex splitter = new Regex(@"^\s*GO\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private const string ExecuteTaskAction = "Execute";
        private const string ExecuteScalarTaskAction = "ExecuteScalar";
        private const string ExecuteReaderTaskAction = "ExecuteReader";
        private const string ExecuteRawReaderTaskAction = "ExecuteRawReader";
        private int commandTimeout = 30;
        private DateTime timer;

        internal delegate void ScriptExecutionEventHandler(object sender, ExecuteEventArgs e);

        internal event ScriptExecutionEventHandler ScriptFileExecuted;

        /// <summary>
        /// Sets the connection string to use for executing the Sql or Files
        /// </summary>
        [DropdownValue(ExecuteTaskAction)]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Sets the timeout in seconds. Default is 30
        /// </summary>
        public int CommandTimeout
        {
            get { return this.commandTimeout; }
            set { this.commandTimeout = value; }
        }

        /// <summary>
        /// Sets the files to execute
        /// </summary>
        [TaskAction(ExecuteTaskAction, false)]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// Sets the Sql to execute
        /// </summary>
        [TaskAction(ExecuteTaskAction, false)]
        public string Sql { get; set; }

        /// <summary>
        /// Sets the parameters to substitute at execution time
        /// </summary>
        [TaskAction(ExecuteTaskAction, false)]
        public ITaskItem[] Parameters { get; set; }

        /// <summary>
        /// Specifies whether files should be re-executed if they initially fail
        /// </summary>
        [TaskAction(ExecuteTaskAction, false)]
        public bool Retry { get; set; }

        /// <summary>
        /// Set to true to run the sql within a transaction
        /// </summary>
        [TaskAction(ExecuteTaskAction, false)]
        public bool UseTransaction { get; set; }

        /// <summary>
        /// Ignore any script errors, i.e. continue executing any remaining scripts when an error is encountered.  Failed
        /// scripts will be returned in the FailedScripts output item.
        /// </summary>
        [TaskAction(ExecuteTaskAction, false)]
        public bool IgnoreScriptErrors { get; set; }

        /// <summary>
        /// Gets the scalar result
        /// </summary>
        [Output]
        public string ScalarResult { get; set; }

        /// <summary>
        /// Gets the raw output from the reader
        /// </summary>
        [Output]
        public string RawReaderResult { get; set; }

        /// <summary>
        /// Gets the output from a reader in an Item with metadata matching the names of columns. The first column returned will be used as the identity.
        /// </summary>
        [Output]
        public ITaskItem[] ReaderResult { get; set; }

        /// <summary>
        /// A list of failed scripts.  Each will have metadata item ErrorMessage set to the error encountered.
        /// </summary>
        [Output]
        public ITaskItem[] FailedScripts { get; set; }

        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case ExecuteTaskAction:
                case ExecuteScalarTaskAction:
                case ExecuteReaderTaskAction:
                case ExecuteRawReaderTaskAction:
                    this.ExecuteSql();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private static string LoadScript(string fileName)
        {
            string retValue;
            using (StreamReader textFileReader = new StreamReader(fileName, System.Text.Encoding.Default, true))
            {
                retValue = new SqlScriptLoader(textFileReader).ReadToEnd();
            }

            return retValue;
        }

        private string SubstituteParameters(string sqlCommandText)
        {
            if (this.Parameters == null)
            {
                return sqlCommandText;
            }

            return this.Parameters.Aggregate(sqlCommandText, (current, parameter) => current.Replace(parameter.GetMetadata("name"), parameter.GetMetadata("value")));
        }

        private void ExecuteSql()
        {
            this.ScriptFileExecuted += this.ScriptExecuted;
            try
            {
                this.timer = DateTime.Now;
                if (!string.IsNullOrEmpty(this.Sql))
                {
                    this.ExecuteText();
                }
                else
                {
                    this.ExecuteFiles();
                }
            }
            finally
            {
                this.ScriptFileExecuted -= this.ScriptExecuted;
            }
        }

        private void ExecuteFiles()
        {
            string sqlCommandText;
            bool retry = true;
            int previousFailures = this.Files.Length;
            ApplicationException lastException = null;
            using (SqlConnection sqlConnection = this.CreateConnection(this.ConnectionString))
            {
                sqlConnection.Open();
                while (retry)
                {
                    int errorNo = 0;
                    ITaskItem[] failures = new ITaskItem[this.Files.Length];
                    var failedScripts = new List<ITaskItem>();
                    foreach (ITaskItem fileInfo in this.Files)
                    {
                        this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.CurrentCulture, "Execute: {0}", fileInfo.ItemSpec));

                        try
                        {
                            this.LogTaskMessage(MessageImportance.Low, "Loading {0}.", new[] { fileInfo.ItemSpec });
                            sqlCommandText = this.SubstituteParameters(LoadScript(fileInfo.ItemSpec)) + Environment.NewLine;
                            string[] batches = splitter.Split(sqlCommandText);
                            this.LogTaskMessage(MessageImportance.Low, "Split {0} into {1} batches.", new object[] { fileInfo.ItemSpec, batches.Length });
                            SqlTransaction sqlTransaction = null;
                            SqlCommand command = sqlConnection.CreateCommand();
                            if (this.UseTransaction)
                            {
                                sqlTransaction = sqlConnection.BeginTransaction();
                            }

                            try
                            {
                                int batchNum = 1;
                                foreach (string batchText in batches)
                                {
                                    sqlCommandText = batchText.Trim();
                                    if (sqlCommandText.Length > 0)
                                    {
                                        command.CommandText = sqlCommandText;
                                        command.CommandTimeout = this.CommandTimeout;
                                        command.Connection = sqlConnection;
                                        command.Transaction = sqlTransaction;
                                        this.LogTaskMessage(MessageImportance.Low, "Executing Batch {0}", new object[] { batchNum++ });
                                        this.LogTaskMessage(MessageImportance.Low, sqlCommandText);
                                        command.ExecuteNonQuery();
                                    }
                                }

                                if (sqlTransaction != null)
                                {
                                    sqlTransaction.Commit();
                                }
                            }
                            catch
                            {
                                if (sqlTransaction != null)
                                {
                                    sqlTransaction.Rollback();
                                }

                                throw;
                            }

                            this.OnScriptFileExecuted(new ExecuteEventArgs(new FileInfo(fileInfo.ItemSpec)));
                        }
                        catch (SqlException ex)
                        {
                            fileInfo.SetMetadata("ErrorMessage", ex.Message);
                            failedScripts.Add(fileInfo);
                            lastException = new ApplicationException(string.Format(CultureInfo.CurrentUICulture, "{0}. {1}", fileInfo.ItemSpec, ex.Message), ex);
                            if (!this.Retry && !this.IgnoreScriptErrors)
                            {
                                throw lastException;
                            }

                            failures[errorNo] = fileInfo;
                            errorNo++;
                            this.OnScriptFileExecuted(new ExecuteEventArgs(new FileInfo(fileInfo.ItemSpec), ex));
                        }
                    }

                    if (!this.Retry)
                    {
                        retry = false;
                    }
                    else
                    {
                        if (errorNo > 0)
                        {
                            this.Files = new ITaskItem[errorNo];
                            for (int i = 0; i < errorNo; i++)
                            {
                                this.Files[i] = failures[i];
                            }

                            if (this.Files.Length >= previousFailures && !this.IgnoreScriptErrors)
                            {
                                throw lastException;
                            }

                            previousFailures = this.Files.Length;
                        }
                        else
                        {
                            retry = false;
                        }
                    }

                    this.FailedScripts = failedScripts.ToArray();
                }
            }
        }

        private void ExecuteText()
        {
            using (SqlConnection sqlConnection = this.CreateConnection(this.ConnectionString))
            using (SqlCommand command = new SqlCommand(this.SubstituteParameters(this.Sql), sqlConnection))
            {
                command.CommandTimeout = this.CommandTimeout;
                this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.CurrentCulture, "Execute: {0}", command.CommandText));
                sqlConnection.Open();
                SqlTransaction sqlTransaction = null;
                try
                {
                    if (this.UseTransaction)
                    {
                        sqlTransaction = sqlConnection.BeginTransaction();
                        command.Transaction = sqlTransaction;
                    }

                    switch (this.TaskAction)
                    {
                        case ExecuteTaskAction:
                            command.ExecuteNonQuery();
                            break;
                        case ExecuteScalarTaskAction:
                            var result = command.ExecuteScalar();
                            this.ScalarResult = result.ToString();
                            break;
                        case ExecuteReaderTaskAction:
                            ArrayList rows = new ArrayList();
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader != null)
                                {
                                    while (reader.Read())
                                    {
                                        ITaskItem rowItem = new TaskItem(reader[0].ToString());
                                        for (int i = 0; i < reader.FieldCount; i++)
                                        {
                                            rowItem.SetMetadata(reader.GetName(i), reader[i].ToString());
                                        }

                                        rows.Add(rowItem);
                                    }
                                }
                            }

                            this.ReaderResult = new ITaskItem[rows.Count];
                            for (int i = 0; i < rows.Count; i++)
                            {
                                this.ReaderResult[i] = (ITaskItem)rows[i];
                            }

                            break;
                        case ExecuteRawReaderTaskAction:
                            using (SqlDataReader rawreader = command.ExecuteReader())
                            {
                                this.RawReaderResult = string.Empty;
                                if (rawreader != null)
                                {
                                    while (rawreader.Read())
                                    {
                                        string resultRow = string.Empty;
                                        for (int i = 0; i < rawreader.FieldCount; i++)
                                        {
                                            resultRow += rawreader[i] + " ";
                                        }

                                        this.RawReaderResult += resultRow + Environment.NewLine;
                                    }
                                }
                            }

                            break;
                    }

                    if (sqlTransaction != null)
                    {
                        sqlTransaction.Commit();
                    }

                    TimeSpan s = DateTime.Now - this.timer;
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Execution Time: {0} seconds", s.TotalSeconds));
                    this.timer = DateTime.Now;
                }
                catch
                {
                    if (sqlTransaction != null)
                    {
                        sqlTransaction.Rollback();
                    }

                    throw;
                }
            }
        }

        private SqlConnection CreateConnection(string connectionString)
        {
            SqlConnection returnedConnection = null;
            SqlConnection connection = null;
            try
            {
                connection = new SqlConnection(connectionString);
                connection.InfoMessage += this.TraceMessageEventHandler;
                returnedConnection = connection;
                connection = null;
            }
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                }
            }

            return returnedConnection;
        }

        private void TraceMessageEventHandler(object sender, SqlInfoMessageEventArgs e)
        {
            if (this.ScriptFileExecuted != null)
            {
                ExecuteEventArgs args = new ExecuteEventArgs(e.Errors);
                this.ScriptFileExecuted(null, args);
            }
        }

        private void OnScriptFileExecuted(ExecuteEventArgs scriptFileExecuted)
        {
            if (scriptFileExecuted != null && this.ScriptFileExecuted != null)
            {
                this.ScriptFileExecuted(null, scriptFileExecuted);
            }
        }

        private void ScriptExecuted(object sender, ExecuteEventArgs scriptInfo)
        {
            if (scriptInfo.ScriptFileInfo != null)
            {
                if (scriptInfo.Succeeded)
                {
                    TimeSpan s = DateTime.Now - this.timer;
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Successfully executed: {0} ({1} seconds)", scriptInfo.ScriptFileInfo.Name, s.TotalSeconds));
                    this.timer = DateTime.Now;
                }
                else
                {
                    TimeSpan s = DateTime.Now - this.timer;
                    this.LogTaskWarning(string.Format(CultureInfo.CurrentCulture, "Failed to executed: {0}. {1} ({2} seconds)", scriptInfo.ScriptFileInfo.Name, scriptInfo.ExecutionException.Message, s.TotalSeconds));
                    this.timer = DateTime.Now;
                }
            }
            else
            {
                if (scriptInfo.SqlInfo != null)
                {
                    foreach (SqlError infoMessage in scriptInfo.SqlInfo)
                    {
                        this.LogTaskMessage("    - " + infoMessage.Message);
                    }
                }
            }
        }
    }
}
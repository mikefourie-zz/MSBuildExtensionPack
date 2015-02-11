//-----------------------------------------------------------------------
// <copyright file="SqlCmd.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.SqlServer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using MSBuild.ExtensionPack.SqlServer.Extended;

    /// <summary>
    /// Wraps the SQL Server command line executable SqlCmd.exe.
    /// <para />
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Execute</i> (<b>Required: </b>CommandLineQuery or InputFiles  <b>Optional: </b>Database, DedicatedAdminConnection, DisableVariableSubstitution, EchoInput, EnableQuotedIdentifiers, Headers, LoginTimeout, LogOn, NewPassword, OutputFile, Password, QueryTimeout, RedirectStandardError, Server, SeverityLevel, SqlCmdPath, UnicodeOutput, UseClientRegionalSettings, Variables, Workstation)</para>
    /// <para><b>Remote Execution Support:</b> Yes</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    ///  <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <ItemGroup>
    ///         <InputFile Include="C:\File1.sql"/>
    ///         <InputFile Include="C:\File2.sql"/>
    ///         <InputFile Include="C:\File3.sql"/>
    ///     </ItemGroup>
    ///     <ItemGroup>
    ///         <Variable Include="DbName">
    ///             <Value>master</Value>
    ///         </Variable>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Simple CommandLineQuery -->
    ///         <MSBuild.ExtensionPack.SqlServer.SqlCmd TaskAction="Execute" CommandLineQuery="SELECT @@VERSION;" />
    ///         <!-- Simple CommandLineQuery setting the Server and Database and outputing to a file -->
    ///         <MSBuild.ExtensionPack.SqlServer.SqlCmd TaskAction="Execute" Server="(local)" Database="@(DbName)" CommandLineQuery="SELECT @@VERSION;" OutputFile="C:\Output.txt"/>
    ///         <!-- Simple CommandLineQuery setting the Server and Database and running external files -->
    ///         <MSBuild.ExtensionPack.SqlServer.SqlCmd TaskAction="Execute" Server="(local)" Database="@(DbName)" InputFiles="@(InputFile)" />
    ///         <!-- Simple CommandLineQuery setting the Server and Database, running external files and using variable substition -->
    ///         <MSBuild.ExtensionPack.SqlServer.SqlCmd TaskAction="Execute" Server="(local)" Database="@(DbName)" InputFiles="@(InputFile)" Variables="@(Variable)" />
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    public class SqlCmd : BaseTask
    {
        private const string ExecuteTaskAction = "Execute";
        private const string ExecutionMessage = "Executing '{0}' with '{1}'";
        private const string InputFileMessage = "Adding input file '{0}'";
        private const string InvalidSqlCmdPathError = "Unable to resolve path to sqlcmd.exe. Assuming it is in the PATH environment variable.";
        private const string InvalidTaskActionError = "Invalid TaskAction passed: {0}";
        private const string LoginTimeoutRangeError = "The LoginTimeout value specified '{0}' does not fall in the allowed range of 0 to 65534. Using the default value of eight (8) seconds.";
        private const string QueryMessage = "Adding query '{0}'";
        private const string QueryTimeoutRangeError = "The QueryTimeout value specified '{0}' does not fall in the allowed range of 1 to 65535.";

        // 8191 * 4 =  32,764; 8,191 is the documented maximum number of characters in a command-line string(see http://support.microsoft.com/kb/830473).
        private const int CommandLineMaxLength = 32764;
        private int loginTimeout = 8;
        private int queryTimeout;
        private string server = ".";

        /// <summary>
        /// Initializes a new instance of the SqlCmd class
        /// </summary>
        public SqlCmd()
        {
            this.DisableVariableSubstitution = false;
            this.EchoInput = false;
            this.RedirectStandardError = false;
            this.UseClientRegionalSettings = false;
        }

        /// <summary>
        /// Gets or sets the path to the sqlcmd.exe.
        /// </summary>
        public string SqlCmdPath { get; set; }

        #region Login Related Options

        /// <summary>
        /// <para>Gets or sets the user login id. If neither the <see cref="LogOn"/> or <see cref="Password"/> option is specified,
        /// <see cref="SqlCmd"/> tries to connect by using Microsoft Windows Authentication mode. Authentication is
        /// based on the Windows account of the user who is running <see cref="SqlCmd"/>.</para>
        /// <para><b>Note:</b> The <i>OSQLUSER</i> environment variable is available for backwards compatibility. The <i>
        /// SQLCMDUSER</i> environment variable takes precedence over the <i>OSQLUSER</i> environment variable. This 
        /// means that <see cref="SqlCmd"/> and <b>osql</b> can be used next to each other without interference.</para>
        /// </summary>
        public string LogOn { get; set; }

        /// <summary>
        /// <para>Gets or sets the user specified password. Passwords are case-sensitive. If the <see cref="LogOn"/> option
        /// is used and the <see cref="Password"/> option is not used, and the <i>SQLCMDPASSWORD</i> environment variable
        /// has not been set, <see cref="SqlCmd"/> uses the default password (NULL).</para>
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Changes the password for a user.
        /// </summary>
        public string NewPassword { get; set; }

        /// <summary>
        /// <para>Gets or sets the name of the SQL Server to which to connect. It sets the <see cref="SqlCmd"/> scripting variable 
        /// <i>SQLCMDSERVER</i>.</para>
        /// <para>Specify <see cref="Server"/> to connect to the default instance of SQL Server on that server computer. Specify 
        /// <see cref="Server"/> to connect to a named instance of SQL Server on that server computer. If no server computer is 
        /// specified, <see cref="SqlCmd"/> connects to the default instance of SQL Server on the local computer. This option is 
        /// required when you execute sqlcmd from a remote computer on the network.</para>
        /// <para>If you do not specify a <see cref="Server" /> when you start <see cref="SqlCmd" />, SQL Server checks for and 
        /// uses the <i>SQLCMDSERVER</i> environment variable.</para>
        /// <para><b>Note: </b>The <i>OSQLSERVER</i> environment variable has been kept for backward compatibility. The 
        /// <i>SQLCMDSERVER</i> environment variable takes precedence over the <i>OSQLSERVER</i> environment variable.</para>
        /// </summary>
        public string Server
        {
            get { return this.server; }
            set { this.server = value; }
        }

        /// <summary>
        /// Gets or sets the workstation name. This option sets the <see cref="SqlCmd"/> scripting variable <i>SQLCMDWORKSTATION</i>. 
        /// The workstation name is listed in the <b>hostname</b> column of the <b>sys.processes</b> catalog view and can be returned 
        /// using the stored procedure <b>sp_who</b>. If this option is not specified, the default is the current computer name. This name 
        /// can be used to identify different sqlcmd sessions.
        /// </summary>
        public string Workstation { get; set; }

        /// <summary>
        /// Gets or sets the name of the database to connect to. Issues a <code>USE</code> <i>db_name</i> statement when you start 
        /// <see cref="SqlCmd"/>. This option sets the <see cref="SqlCmd"/> scripting variable <i>SQLCMDDBNAME</i>. This specifies 
        /// the initial database. The default is your login's default-database property. If the database does not exist, an error message 
        /// is generated and <see cref="SqlCmd"/> exits.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds before the <see cref="SqlCmd"/> login to the OLE DB provider times out when
        /// you try to connect to a server. The default login time-out for <see cref="SqlCmd"/> is eight (8) seconds. The login time-
        /// out value must be a number between 0 and 65534. If the value supplied is not numeric or does not fall into that range,
        /// the <see cref="SqlCmd"/> generates an error message. A value of 0 specifies the time-out to be indefinite.
        /// </summary>
        public int LoginTimeout
        {
            get
            {
                return this.loginTimeout;
            }

            set
            {
                if (value >= 0 && value <= 65534)
                {
                    this.loginTimeout = value;
                }
                else
                {
                    this.LogTaskWarning(string.Format(CultureInfo.InvariantCulture, LoginTimeoutRangeError, value));
                }
            }
        }

        /// <summary>
        /// Gets or sets a flag that indicates if the connection to SQL Server should use a Dedicated Administrator Connection (DAC).
        /// This kind of connection is used to troubleshoot a server. This will only work with server computers that support DAC. If 
        /// DAC is not available, <see cref="SqlCmd"/> generates an error message and then exits. For more information about DAC, see 
        /// <a href="http://msdn.microsoft.com/en-us/library/ms189595.aspx">Using a Dedicated Administrator Connection</a>.
        /// </summary>
        public bool DedicatedAdminConnection { get; set; }

        #endregion

        #region Input/Output Options

        /// <summary>
        /// <para>Gets or sets the path to a file that contains a batch of SQL statements. Multiple files may be specified that will be read 
        /// and processed in order. Do not use any spaces between the file names. <see cref="SqlCmd"/> will first check to see 
        /// whether all files exist. If one or more files do not exist, <see cref="SqlCmd"/> will exit. The <see cref="InputFiles"/> and
        /// <see cref="CommandLineQuery"/> options are mutually exclusive.</para>
        /// Please note that if you provide a large number of files, you may exceed the maximum length of a command line (http://support.microsoft.com/kb/830473). It's recommended you make use of smaller batches if you encounter this issue.       
        /// </summary>
        public ITaskItem[] InputFiles { get; set; }

        /// <summary>
        /// <para>Gets or sets the file that receives output from <see cref="SqlCmd"/>.</para>
        /// <para>If the <see cref="UnicodeOutput"/> option is specified, the <i>output file</i> is stored in Unicode format.
        /// If the file name is not valid, an error message is generated, and <see cref="SqlCmd"/> exits. <see cref="SqlCmd"/> does 
        /// not support concurrent writing of multiple <see cref="SqlCmd"/> processes to the same file. The file output will be 
        /// corrupted or incorrect. This file will be created if it does not exist. A file of the same name from a prior <see cref="SqlCmd"/> session 
        /// will be overwritten. The file specified here is not the stdout file. If a stdout file is specified this file will not be used.</para>
        /// </summary>
        public string OutputFile { get; set; }

        /// <summary>
        /// Gets or sets a flag that indicates if the <see cref="OutputFile"/> is stored in Unicode format, regardless of the 
        /// format of the <see cref="InputFiles"/>.
        /// </summary>
        public bool UnicodeOutput { get; set; }

        /// <summary>
        /// Gets or sets a flag that indicates whether or not to redirect the error message output to the screen 
        /// (<b>stderr</b>).If you do not specify a parameter or if you specify <b>0</b>, only error messages that 
        /// have a severity level of 11 or higher are redirected. If you specify <b>1</b>, all error message output including 
        /// PRINT is redirected. Has no effect if you use <see cref="OutputFile"/>. By default, messages are sent to <b>stdout</b>.
        /// </summary>
        public bool RedirectStandardError { get; set; }

        /// <summary>
        /// Gets or sets a flag that indicates if the SQL Server OLE DB provider uses the client regional settings when it converts
        /// currency, and date and time data to character data. The default is server regional settings.
        /// </summary>
        public bool UseClientRegionalSettings { get; set; }

        #endregion

        #region Query Execution Options

        /// <summary>
        /// Gets or sets one or more command line queries to execute when <see cref="SqlCmd"/> starts, but does not exit
        /// sqlcmd when the query has finished running.
        /// </summary>
        public ITaskItem[] CommandLineQuery { get; set; }

        /// <summary>
        /// Gets or sets a flag that indicates if the input scripts are written to the standard output device (<b>stdout</b>).
        /// </summary>
        public bool EchoInput { get; set; }

        /// <summary>
        /// Gets or sets a flag that sets the <code>SET QUOTED_IDENTIFIER</code> connection option to <code>ON</code>. By 
        /// default, it is set to <code>OFF</code>. For more information, see 
        /// <a href="http://msdn.microsoft.com/en-us/library/ms174393.aspx">SET QUOTED_IDENTIFIER (Transact-SQL).</a>
        /// </summary>
        public bool EnableQuotedIdentifiers { get; set; }

        /// <summary>
        /// Controls the severity level that is used to set the ERRORLEVEL variable. If the ERRORLEVEL reported is >= SeverityLevel then the task will log an error. 
        /// </summary>
        public int SeverityLevel { get; set; }

        /// <summary>
        /// <para>Gets or sets the number of seconds before a command (or SQL statement) times out. This option sets the <see cref="SqlCmd"/>
        /// scripting variable <i>SQLCMDSTATTIMEOUT</i>. If a <i>time_out</i> value is not specified, the command does not time out. The 
        /// query <i>time_out</i> must be a number between 1 and 65535. If the value supplied is not numeric or does not fall into that range,
        /// <see cref="SqlCmd"/> generates an error message.</para>
        /// <para><b>Note:</b> The actual time out value may vary from the specified <i>time_out</i> value by several seconds.</para>
        /// </summary>
        public int QueryTimeout
        {
            get
            {
                return this.queryTimeout;
            }

            set
            {
                if (value >= 1 && value <= 65535)
                {
                    this.queryTimeout = value;
                }
                else
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, QueryTimeoutRangeError, value));
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="SqlCmd"/> scripting variable that can be used in a <see cref="SqlCmd"/> script. You can specify multiple 
        /// <see cref="Variables"/> and values. If there are errors in any of the values specified, <see cref="SqlCmd"/> generates an error 
        /// message and then exits.
        /// </summary>
        public ITaskItem[] Variables { get; set; }

        /// <summary>
        /// Causes <see cref="SqlCmd"/> to ignore scripting variables. This is useful when a script contains many INSERT statements that 
        /// may contain strings that have the same format as regular variables, such as $(variable_name).
        /// </summary>
        public bool DisableVariableSubstitution { get; set; }

        #endregion

        #region Formatting Options

        /// <summary>
        /// Specifies the number of rows to print between the column headings. The default is to print headings one time for each set of 
        /// query results. This option sets the sqlcmd scripting variable <i>SQLCMDHEADERS</i>. Use -1 to specify that headers must not be 
        /// printed. Any value that is not valid causes <see cref="SqlCmd"/> to generate an error message and then exit.
        /// </summary>
        public int Headers { get; set; }

        #endregion

        protected override void InternalExecute()
        {
            if (this.InputFiles == null && this.CommandLineQuery == null)
            {
                this.Log.LogError("InputFiles or CommandLineQuery is required");
                return;
            }

            switch (this.TaskAction)
            {
                case ExecuteTaskAction:
                    this.SqlExecute();
                    break;
                default:
                    this.Log.LogError(InvalidTaskActionError, this.TaskAction);
                    return;
            }
        }

        private string BuildArguments()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // Login Related Options

            // Login Id
            if (!string.IsNullOrEmpty(this.LogOn))
            {
                sb.Append(" -U ");
                sb.Append(this.LogOn);

                // Only pass in the password if a user id has been specified
                // Password
                if (!string.IsNullOrEmpty(this.Password))
                {
                    sb.Append(" -P ");
                    sb.Append(this.Password);
                }
            }
            else
            {
                // default to assume Trusted
                sb.Append(" -E ");
            }

            // New Password and exit
            if (!string.IsNullOrEmpty(this.NewPassword))
            {
                sb.Append(" -Z ");
                sb.Append(this.NewPassword);
            }

            // Server
            if (!string.IsNullOrEmpty(this.Server))
            {
                sb.Append(" -S ");
                sb.Append(this.server);
            }

            // Workstation
            if (!string.IsNullOrEmpty(this.Workstation))
            {
                sb.Append(" -H ");
                sb.Append(this.Workstation);
            }

            if (!string.IsNullOrEmpty(this.Database))
            {
                sb.Append(" -d ");
                sb.Append(this.Database);
            }

            // Login Timeout
            sb.Append(" -l ");
            sb.Append(this.LoginTimeout);

            if (this.DedicatedAdminConnection)
            {
                sb.Append(" -A ");
            }

            // Output file
            if (!string.IsNullOrEmpty(this.OutputFile))
            {
                sb.Append(" -o ");
                sb.Append("\"");
                sb.Append(this.OutputFile);
                sb.Append("\"");
            }

            // Code Page

            // Unicode
            if (this.UnicodeOutput)
            {
                sb.Append(" -u ");
            }

            // SeverityLevel
            if (this.SeverityLevel > 0)
            {
                sb.Append(" -V " + this.SeverityLevel);
            }

            // Redirect Standard Error
            if (this.RedirectStandardError)
            {
                sb.Append(" -r 1 ");
            }

            // Client Regional settings
            if (this.UseClientRegionalSettings)
            {
                sb.Append(" -R ");
            }

            // Query Execution Options

            // Command line query
            if (this.CommandLineQuery != null)
            {
                foreach (ITaskItem query in this.CommandLineQuery)
                {
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentUICulture, QueryMessage, query.ItemSpec));
                    sb.Append(" -Q ");
                    sb.Append("\"");
                    sb.Append(query.ItemSpec);
                    sb.Append("\"");
                }
            }

            // Echo Input
            if (this.EchoInput)
            {
                sb.Append(" -e ");
            }

            // Enabled Quoted Identifiers
            if (this.EnableQuotedIdentifiers)
            {
                sb.Append(" -I ");
            }

            // Query timeout
            if (this.QueryTimeout > 0)
            {
                sb.Append(" -t ");
                sb.Append(this.QueryTimeout);
            }

            // Variables
            if (this.Variables != null)
            {
                foreach (ITaskItem variable in this.Variables)
                {
                    sb.Append(" -v ");
                    sb.Append(variable.ItemSpec);
                    sb.Append("=\"");
                    sb.Append(variable.GetMetadata("Value"));
                    sb.Append("\"");
                }
            }

            // DisableVariableSubstitution
            if (this.DisableVariableSubstitution)
            {
                sb.Append(" -x ");
            }

            return sb.ToString();
        }

        private void ExecuteCommand(string arguments)
        {
            var sqlCmdWrapper = new SqlCmdWrapper(this.SqlCmdPath, arguments, string.Empty);

            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentUICulture, ExecutionMessage, sqlCmdWrapper.Executable, arguments));

            // Get the return value
            int returnValue = sqlCmdWrapper.Execute();

            // Write out the output
            if (!string.IsNullOrEmpty(sqlCmdWrapper.StandardOutput))
            {
                this.LogTaskMessage(MessageImportance.Normal, sqlCmdWrapper.StandardOutput);
            }

            // Write out any errors
            this.SwitchReturnValue(returnValue, sqlCmdWrapper.StandardError.Trim());

            string[] stdOutLines = sqlCmdWrapper.StandardOutput == null ? new string[0] : sqlCmdWrapper.StandardOutput.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var regex = new Regex(@"^(Msg|HResult) (?<ErrorCode>(0x[0-9A-F]+)|\d+), Level (?<Level>\d+), State (?<StateID>\d+)(, Server [^,]+(, (?<ObjectName>[^,]+))?, Line (?<LineNumber>\d+))?$");
            bool foundError = false;
            string errorCode = string.Empty;
            string objectName = string.Empty;
            int lineNum = 0;
            int severityLevel = 0;
            bool loggingErrorsBySeverityLevel = this.SeverityLevel > 0;
            foreach (string line in stdOutLines)
            {
                if (foundError)
                {
                    if (loggingErrorsBySeverityLevel && severityLevel >= this.SeverityLevel)
                    {
                        this.Log.LogError(string.Empty, errorCode, string.Empty, objectName, lineNum, 0, 0, 0, line);
                    }
                    else
                    {
                        this.Log.LogWarning(string.Empty, errorCode, string.Empty, objectName, lineNum, 0, 0, 0, line);
                    }

                    foundError = false;
                    continue;
                }

                var match = regex.Match(line);
                if (match.Success)
                {
                    var groups = match.Groups;
                    int.TryParse(groups["LineNumber"].ToString(), out lineNum);
                    int.TryParse(groups["Level"].ToString(), out severityLevel);
                    errorCode = groups["ErrorCode"].ToString();
                    objectName = groups["ObjectName"].ToString();
                    if (loggingErrorsBySeverityLevel && severityLevel >= this.SeverityLevel)
                    {
                        this.Log.LogError(string.Empty, errorCode, string.Empty, objectName, lineNum, 0, 0, 0, line);
                    }
                    else
                    {
                        this.Log.LogWarning(string.Empty, errorCode, string.Empty, objectName, lineNum, 0, 0, 0, line);
                    }

                    foundError = true;
                }
            }

            if (loggingErrorsBySeverityLevel && returnValue >= this.SeverityLevel)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "SeverityLevel: {0} has been met or exceeded: {1}", this.SeverityLevel, returnValue));
            }
        }

        private void SqlExecute()
        {
            // Resolve the path to the sqlcmd.exe tool
            if (!System.IO.File.Exists(this.SqlCmdPath))
            {
                this.LogTaskMessage(MessageImportance.Low, InvalidSqlCmdPathError);
                this.SqlCmdPath = "sqlcmd.exe";
            }

            string baseArguments = this.BuildArguments();
            if (this.InputFiles != null && this.InputFiles.Length > 0)
            {
                int lengthRemaining = CommandLineMaxLength - this.SqlCmdPath.Length - baseArguments.Length - 2;
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "There are {0} characters available in the argument list for the input files.", lengthRemaining));
                using (InputFileArgumentCollection ifac = new InputFileArgumentCollection(this, this.InputFiles, lengthRemaining))
                {
                    foreach (var inputFileArgs in ifac)
                    {
                        this.ExecuteCommand(baseArguments + " " + inputFileArgs);
                    }
                }
            }
            else
            {
                this.ExecuteCommand(baseArguments);
            }
        }

        private void SwitchReturnValue(int returnValue, string error)
        {
            switch (returnValue)
            {
                case 1:
                    this.LogTaskWarning("Exit Code 1. Failure: " + error);
                    break;
            }
        }

        private sealed class InputFileArgumentCollection : IEnumerator<string>, IEnumerable<string>
        {
            private readonly BaseTask task;
            private readonly int maxArgLength = CommandLineMaxLength;
            private readonly ITaskItem[] inputFiles;
            private int currentIndex;
            private StringBuilder currentArgList;

            public InputFileArgumentCollection(BaseTask baseTask, ITaskItem[] inputFileList, int maxArgLength)
            {
                this.task = baseTask;
                this.inputFiles = inputFileList;
                this.maxArgLength = maxArgLength;
                this.Reset();
            }

            public string Current
            {
                get { return this.currentArgList.ToString(); }
            }

            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            public bool MoveNext()
            {
                if (this.inputFiles == null || this.currentIndex >= this.inputFiles.Length)
                {
                    return false;
                }

                this.currentArgList = new StringBuilder();
                for (; this.currentIndex < this.inputFiles.Length; ++this.currentIndex)
                {
                    ITaskItem file = this.inputFiles[this.currentIndex];
                    string fullPath = file.GetMetadata("FullPath");
                    if (this.currentArgList.Length + 6 + fullPath.Length > this.maxArgLength)
                    {
                        break;
                    }

                    this.task.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, InputFileMessage, file.GetMetadata("FullPath")));
                    this.currentArgList.Append(" -i ");
                    this.currentArgList.Append("\"");
                    this.currentArgList.Append(file.GetMetadata("FullPath"));
                    this.currentArgList.Append("\"");
                }

                return true;
            }

            public void Reset()
            {
                this.currentIndex = 0;
                this.currentArgList = new StringBuilder();
            }

            public IEnumerator<string> GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="Svn.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// The code was written by Jozsef Fejes (http://joco.name).
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Subversion
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Serialization;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Win32;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Version</i> (<b>Required: </b>Item <b>Output: </b>Info <b>Optional:</b> UserName, UserPassword)</para>
    /// <para><i>Info</i> (<b>Required: </b>Item <b>Output: </b>Info <b>Optional:</b> UserName, UserPassword)</para>
    /// <para><i>GetProperty</i> (<b>Required: </b>Item, PropertyName <b>Output: </b>PropertyValue <b>Optional:</b> UserName, UserPassword)</para>
    /// <para><i>SetProperty</i> (<b>Required: </b>Item, PropertyName, PropertyValue <b>Optional:</b> UserName, UserPassword)</para>
    /// <para><i>Checkout</i> (<b>Required: </b>Items, Destination <b>Optional:</b> UserName, UserPassword)</para>
    /// <para><i>Update</i> (<b>Required: </b>Items <b>Optional:</b> UserName, UserPassword)</para>
    /// <para><i>Add</i> (<b>Required: </b>Items <b>Optional:</b> UserName, UserPassword)</para>
    /// <para><i>Copy</i> (<b>Required: </b>Items, Destination <b>Optional:</b> UserName, UserPassword, CommitMessage)</para>
    /// <para><i>Delete</i> (<b>Required: </b>Items <b>Optional:</b> UserName, UserPassword)</para>
    /// <para><i>Move</i> (<b>Required: </b>Items, Destination <b>Optional:</b> UserName, UserPassword)</para>
    /// <para><i>Commit</i> (<b>Required: </b>Items <b>Optional:</b> UserName, UserPassword, CommitMessage)</para>
    /// <para><i>Export</i> (<b>Required: </b>Item, Destination <b>Optional:</b> UserName, UserPassword, Force)</para>
    /// </summary>
    /// <remarks>
    /// <para>The task needs a command-line SVN client (svn.exe and svnversion.exe) to be available. The following are supported
    /// and automatically detected:</para>
    /// <list type="bullet">
    ///     <item>any SVN client in the PATH environment variable</item>
    ///     <item>Cygwin 1.7, with the subversion package installed</item>
    ///     <item>TortoiseSVN 1.7, with the command line component installed</item>
    ///     <item>CollabNet Subversion Client 1.7</item>
    ///     <item>Slik SVN 1.7, with the Subversion client component installed</item>
    /// </list>
    /// <para>If you publish a project that uses this task, remember to notify your users that they need one of the mentioned
    /// clients in order to build your code. The PATH detection allows everyone to set a preference, the other detections are
    /// there so that it will just work for most users.</para>
    /// <para>The Version action calls svnversion.exe, all other actions call subcommands of svn.exe. Some parameters also accept
    /// URL's, not just local paths. Please refer to SVN's documentation for more information.</para>
    /// </remarks>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <!-- Version -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="Version" Item="c:\path">
    ///             <Output TaskParameter="Info" ItemName="VInfo"/>
    ///         </MSBuild.ExtensionPack.Subversion.Svn>
    ///         <Message Text="MinRevision: %(VInfo.MinRevision), MaxRevision: %(VInfo.MaxRevision), IsMixed: %(VInfo.IsMixed), IsModified: %(VInfo.IsModified)"/>
    ///         <Message Text="IsSwitched: %(VInfo.IsSwitched), IsPartial: %(VInfo.IsPartial), IsClean: %(VInfo.IsClean)"/>
    ///         <!-- Info -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="Info" Item="c:\path">
    ///             <Output TaskParameter="Info" ItemName="IInfo"/>
    ///         </MSBuild.ExtensionPack.Subversion.Svn>
    ///         <Message Text="EntryKind: %(IInfo.EntryKind), EntryRevision: %(IInfo.EntryRevision), EntryURL: %(IInfo.EntryURL)"/>
    ///         <Message Text="RepositoryRoot: %(IInfo.RepositoryRoot), RepositoryUUID: %(IInfo.RepositoryUUID)"/>
    ///         <Message Text="WorkingCopySchedule: %(IInfo.WorkingCopySchedule), WorkingCopyDepth: %(IInfo.WorkingCopyDepth)"/>
    ///         <Message Text="CommitAuthor: %(IInfo.CommitAuthor), CommitRevision: %(IInfo.CommitRevision), CommitDate: %(IInfo.CommitDate)"/>
    ///         <!-- GetProperty -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="GetProperty" Item="c:\path" PropertyName="svn:externals">
    ///             <Output TaskParameter="PropertyValue" PropertyName="GProp"/>
    ///         </MSBuild.ExtensionPack.Subversion.Svn>
    ///         <Message Text="PropertyValue: $(GProp)"/>
    ///         <!-- SetProperty -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="SetProperty" Item="c:\path" PropertyName="test" PropertyValue="hello"/>
    ///         <!-- Checkout -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="Checkout" Items="http://repository/url" Destination="c:\path"/>
    ///         <!-- Update -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="Update" Items="c:\path1;c:\path2"/>
    ///         <!-- Add -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="Add" Items="c:\path\newfile"/>
    ///         <!-- Copy -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="Copy" Items="c:\path\file1;c:\path\file2" Destination="c:\path\directory"/>
    ///         <!-- Delete -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="Delete" Items="c:\path\something"/>
    ///         <!-- Move -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="Move" Items="c:\path\file1;c:\path\file2" Destination="c:\path\directory"/>
    ///         <!-- Commit with default commit message -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="Commit" Items="c:\path\something"/>
    ///         <!-- Commit with commit message -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="Commit" Items="c:\path\something" CommitMessage="MsBuild committed from something directory" />
    ///         <!-- Export -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="Export" Item="c:\path\workingcopy" Destination="c:\path\exported"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class Svn : BaseTask
    {
        protected const string SvnExecutableName = "svn.exe";
        protected const string SvnVersionExecutableName = "svnversion.exe";
        private const string VersionTaskAction = "Version";
        private const string InfoTaskAction = "Info";
        private const string GetPropertyTaskAction = "GetProperty";
        private const string SetPropertyTaskAction = "SetProperty";
        private const string CheckoutTaskAction = "Checkout";
        private const string UpdateTaskAction = "Update";
        private const string AddTaskAction = "Add";
        private const string CopyTaskAction = "Copy";
        private const string DeleteTaskAction = "Delete";
        private const string MoveTaskAction = "Move";
        private const string CommitTaskAction = "Commit";
        private const string ExportTaskAction = "Export";
        private const string DefaultCommitMessage = "MsBuild";

        private static readonly string SvnPath = FindSvnPath();

        [Required]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// The item that the Version, Info, GetProperty, SetProperty and Export actions use.
        /// </summary>
        public ITaskItem Item { get; set; }

        /// <summary>
        /// The items that the Checkout, Update, Add, Copy, Delete, Move and Commit actions use.
        /// </summary>
        public ITaskItem[] Items { get; set; }

        /// <summary>
        /// The destination that the Checkout, Copy, Move and Export actions use.
        /// </summary>
        public ITaskItem Destination { get; set; }

        /// <summary>
        /// Filled up by the Version and Info actions.
        /// </summary>
        [Output]
        public ITaskItem Info { get; set; }

        /// <summary>
        /// The name of the property that the GetProperty and SetProperty actions use.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Set whether to use Force on Export. Default is false.
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// The value of the property that the GetProperty action returns and the SetProperty action sets.
        /// </summary>
        [Output]
        public string PropertyValue { get; set; }

        /// <summary>
        /// The commit message that the Commit action sends with the commit to the repository
        /// </summary>
        public string CommitMessage { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            if (SvnPath == null)
            {
                Log.LogError("A supported SVN client installation was not found");
                return;
            }

            switch (this.TaskAction)
            {
                case VersionTaskAction:
                    this.ExecVersion();
                    break;

                case InfoTaskAction:
                    this.ExecInfo();
                    break;

                case GetPropertyTaskAction:
                    this.ExecGetProperty();
                    break;

                case SetPropertyTaskAction:
                    this.ExecSetProperty();
                    break;

                case CheckoutTaskAction:
                    this.ExecCheckout();
                    break;

                case UpdateTaskAction:
                    this.ExecUpdate();
                    break;

                case AddTaskAction:
                    this.ExecAdd();
                    break;

                case CopyTaskAction:
                    this.ExecCopy();
                    break;

                case DeleteTaskAction:
                    this.ExecDelete();
                    break;

                case MoveTaskAction:
                    this.ExecMove();
                    break;

                case CommitTaskAction:
                    this.ExecCommit();
                    break;

                case ExportTaskAction:
                    this.ExecExport();
                    break;

                default:
                    this.Log.LogError("Invalid TaskAction passed: {0}", this.TaskAction);
                    return;
            }
        }

        /// <summary>
        /// Checks if a path is a valid SVN path where svn.exe and svnversion.exe can be found.
        /// </summary>
        /// <param name="dir">the path to check</param>
        /// <returns><paramref name="dir"/> if it is valid, null otherwise</returns>
        private static bool IsSvnPath(string dir)
        {
            return Path.IsPathRooted(dir) // for a consistent behavior
                   && File.Exists(Path.Combine(dir, SvnExecutableName))
                   && File.Exists(Path.Combine(dir, SvnVersionExecutableName));
        }

        /// <summary>
        /// Runs the specified try-delegate in the 32 and the 64 bit registry. The registry native to the process is checked first.
        /// </summary>
        /// <param name="inner">the try-delegate</param>
        /// <returns>whatever the try-delegate returns</returns>
        private static string TryRegistry3264(Func<RegistryKey, string> inner)
        {
            string ret;

            // native first
            using (var key = Utilities.SoftwareRegistryNative)
            {
                if (key != null && (ret = inner(key)) != null)
                {
                    return ret;
                }
            }

            // non-native second
            using (var key = Utilities.SoftwareRegistryNonnative)
            {
                if (key != null && (ret = inner(key)) != null)
                {
                    return ret;
                }
            }

            return null;
        }

        /// <summary>
        /// Tries to find an SVN installation in the PATH environment variable.
        /// </summary>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string TryEnvironmentPath()
        {
            var paths = Environment.GetEnvironmentVariable("PATH");
            if (paths != null)
            {
                return paths.Split(Path.PathSeparator).FirstOrDefault(IsSvnPath);
            }

            return null;
        }

        /// <summary>
        /// Tries to find an SVN installation in all Cygwin installations.
        /// </summary>
        /// <param name="software">the HKLM\SOFTWARE registry key</param>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string TryCygwin(RegistryKey software)
        {
            // Cygwin installations are registered at HKLM\SOFTWARE\Cygwin\Installations (32 bit only), and they are NT object
            // manager paths. SVN is installed under /usr/bin.
            using (var key = software.OpenSubKey(@"Cygwin\Installations"))
            {
                if (key == null)
                {
                    return null;
                }

                foreach (var value in key.GetValueNames().Select(name => key.GetValue(name) as string))
                {
                    if (value == null)
                    {
                        continue;
                    }

                    if (value.StartsWith(@"\??\UNC\", StringComparison.OrdinalIgnoreCase))
                    {
                        // NT object manager UNC path
                        var dir = Path.Combine(Path.Combine(@"\\" + value.Substring(8), "usr"), "bin");
                        if (IsSvnPath(dir))
                        {
                            return dir;
                        }
                    }
                    else if (value.StartsWith(@"\??\", StringComparison.OrdinalIgnoreCase))
                    {
                        // NT object manager local drive path
                        var dir = Path.Combine(Path.Combine(value.Substring(4), "usr"), "bin");
                        if (IsSvnPath(dir))
                        {
                            return dir;
                        }
                    }
                    else
                    {
                        // maybe a regular path
                        var dir = Path.Combine(Path.Combine(value, "usr"), "bin");
                        if (IsSvnPath(dir))
                        {
                            return dir;
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Tries to find a TortoiseSVN installation in the given registry view.
        /// </summary>
        /// <param name="software">the HKLM\SOFTWARE registry key</param>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string TryTortoiseSvn(RegistryKey software)
        {
            // HKLM\SOFTWARE\TortoiseSVN!Directory points to the base installation dir, binaries are under \bin
            using (var key = software.OpenSubKey(@"TortoiseSVN"))
            {
                if (key != null)
                {
                    var dir = key.GetValue("Directory") as string;
                    if (dir != null)
                    {
                        dir = Path.Combine(dir, "bin");
                        if (IsSvnPath(dir))
                        {
                            return dir;
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Tries to find a CollabNet Subversion Client installation in the given registry view.
        /// </summary>
        /// <param name="software">the HKLM\SOFTWARE registry key</param>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string TryCollabNet(RegistryKey software)
        {
            // HKLM\SOFTWARE\CollabNet\Subversion!Client Version contains the version number
            // HKLM\SOFTWARE\CollabNet\Subversion\[version]\Client!Install Location contains the actual directory
            using (var key = software.OpenSubKey(@"CollabNet\Subversion"))
            {
                if (key != null)
                {
                    var version = key.GetValue("Client Version") as string;
                    if (version != null)
                    {
                        using (var subkey = key.OpenSubKey(version + @"\Client"))
                        {
                            var dir = subkey.GetValue(@"Install Location") as string;
                            if (IsSvnPath(dir))
                            {
                                return dir;
                            }
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Tries to find a SlikSvn installation in the given registry view.
        /// </summary>
        /// <param name="software">the HKLM\SOFTWARE registry key</param>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string TrySlikSvn(RegistryKey software)
        {
            // HKLM\SOFTWARE\SlikSvn\Install!Location points to the binaries directory
            using (var key = software.OpenSubKey(@"SlikSvn\Install"))
            {
                if (key != null)
                {
                    var dir = key.GetValue("Location") as string;
                    if (dir != null)
                    {
                        if (IsSvnPath(dir))
                        {
                            return dir;
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Tries to find an SVN installation from all possible places.
        /// </summary>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string FindSvnPath()
        {
            string ret;

            // PATH environment variable
            if ((ret = TryEnvironmentPath()) != null)
            {
                return ret;
            }

            // Cygwin
            if ((ret = TryRegistry3264(TryCygwin)) != null)
            {
                return ret;
            }

            // TortoiseSVN
            if ((ret = TryRegistry3264(TryTortoiseSvn)) != null)
            {
                return ret;
            }

            // CollabNet
            if ((ret = TryRegistry3264(TryCollabNet)) != null)
            {
                return ret;
            }

            // SlikSvn
            if ((ret = TryRegistry3264(TrySlikSvn)) != null)
            {
                return ret;
            }

            // didn't find it, will report it as an error from where it's used
            return null;
        }

        private void ExecVersion()
        {
            // required params
            if (this.Item == null)
            {
                Log.LogError("The Item parameter is required");
                return;
            }

            // execute the tool
            var cmd = this.CreateCommandLineBuilder();
            cmd.AppendSwitch("-q");
            cmd.AppendFileNameIfNotNull(this.Item);
            var output = Utilities.ExecuteWithLogging(Log, Path.Combine(SvnPath, SvnVersionExecutableName), cmd.ToString(), false);

            if (!this.Log.HasLoggedErrors)
            {
                // decode the response
                var m = Regex.Match(output, @"^\s?(?<min>[0-9]+)(:(?<max>[0-9]+))?(?<sw>[MSP]*)\s?$");
                if (!m.Success)
                {
                    // not versioned or really an error, we don't care
                    Log.LogError("Version: Invalid output from SVN tool: no regex  match. Svn tool output was: {0}", output);
                    return;
                }

                var mixed = !string.IsNullOrEmpty(m.Groups["max"].Value);
                var sw = m.Groups["sw"].Value;

                // fill up the output
                this.Info = new TaskItem(this.Item);
                this.Info.SetMetadata("MinRevision", m.Groups["min"].Value);
                this.Info.SetMetadata("MaxRevision", m.Groups[mixed ? "max" : "min"].Value);
                this.Info.SetMetadata("IsMixed", mixed.ToString(CultureInfo.InvariantCulture));
                this.Info.SetMetadata("IsModified", sw.Contains("M").ToString(CultureInfo.InvariantCulture));
                this.Info.SetMetadata("IsSwitched", sw.Contains("S").ToString(CultureInfo.InvariantCulture));
                this.Info.SetMetadata("IsPartial", sw.Contains("P").ToString(CultureInfo.InvariantCulture));
                this.Info.SetMetadata("IsClean", (!mixed && sw.Length == 0).ToString(CultureInfo.InvariantCulture));
            }
        }

        private void ExecInfo()
        {
            // required params
            if (this.Item == null)
            {
                Log.LogError("The Item parameter is required");
                return;
            }

            // execute the tool
            var cmd = this.CreateCommandLineBuilder();
            cmd.AppendSwitch("info");
            cmd.AppendSwitch("--non-interactive");
            cmd.AppendSwitch("--xml");
            cmd.AppendFileNameIfNotNull(this.Item);
            var output = Utilities.ExecuteWithLogging(Log, Path.Combine(SvnPath, SvnExecutableName), cmd.ToString(), false);

            if (!this.Log.HasLoggedErrors)
            {
                // deserialize the response
                var xs = new XmlSerializer(typeof(Schema.infoType));
                Schema.infoType info;
                try
                {
                    using (var sr = new StringReader(output))
                    {
                        info = (Schema.infoType)xs.Deserialize(sr);
                    }
                }
                catch (InvalidOperationException invalidOperationException)
                {
                    // not versioned or really an error, we don't care
                    Log.LogError("Info: Invalid output from SVN tool: InvalidOperationException: {0}", invalidOperationException.Message);
                    return;
                }

                // check the response
                if (info.entry == null || info.entry.Length != 1)
                {
                    // this really shouldn't happen
                    Log.LogError("Info: Invalid output from SVN tool: Svn info type entry was null or had a length different from 1");
                    return;
                }

                var entry = info.entry[0];

                // fill up the output
                this.Info = new TaskItem(this.Item);
                this.Info.SetMetadata("EntryKind", entry.kind);
                this.Info.SetMetadata("EntryRevision", entry.revision.ToString(CultureInfo.InvariantCulture));
                this.Info.SetMetadata("EntryURL", entry.url);

                if (entry.repository != null)
                {
                    this.Info.SetMetadata("RepositoryRoot", entry.repository.root);
                    this.Info.SetMetadata("RepositoryUUID", entry.repository.uuid);
                }

                if (entry.wcinfo != null)
                {
                    this.Info.SetMetadata("WorkingCopySchedule", entry.wcinfo.schedule);
                    this.Info.SetMetadata("WorkingCopyDepth", entry.wcinfo.depth);
                }

                if (entry.commit != null)
                {
                    if (entry.commit.author != null)
                    {
                        this.Info.SetMetadata("CommitAuthor", entry.commit.author);
                    }

                    this.Info.SetMetadata("CommitRevision", entry.commit.revision.ToString(CultureInfo.InvariantCulture));
                    this.Info.SetMetadata("CommitDate", entry.commit.date.ToString("o", CultureInfo.InvariantCulture));
                }
            }
        }

        private void ExecGetProperty()
        {
            // required params
            if (this.Item == null || string.IsNullOrEmpty(this.PropertyName))
            {
                Log.LogError("The Item and PropertyName parameters are required");
                return;
            }

            // execute the tool
            var cmd = this.CreateCommandLineBuilder();
            cmd.AppendSwitch("propget");
            cmd.AppendSwitch("--non-interactive");
            cmd.AppendSwitch("--xml");
            cmd.AppendFixedParameter(this.PropertyName);
            cmd.AppendFileNameIfNotNull(this.Item);
            var output = Utilities.ExecuteWithLogging(Log, Path.Combine(SvnPath, SvnExecutableName), cmd.ToString(), false);

            if (!Log.HasLoggedErrors)
            {
                // deserialize the response
                var xs = new XmlSerializer(typeof(Schema.propertiesType));
                Schema.propertiesType props;
                try
                {
                    using (var sr = new StringReader(output))
                    {
                        props = (Schema.propertiesType)xs.Deserialize(sr);
                    }
                }
                catch (InvalidOperationException ioe)
                {
                    Log.LogError(string.Format(CultureInfo.InvariantCulture, "InvalidOperationException: Invalid output from SVN tool - {0}", ioe));
                    return;
                }

                // check the response
                if (props.target == null || (props.target.Length == 1 && props.target[0].property == null))
                {
                    // the property is not set, we handle it with a warning
                    Log.LogWarning("The SVN property doesn't exist");
                    return;
                }

                if (props.target.Length != 1)
                {
                    // this really shouldn't happen
                    Log.LogError("Invalid output from SVN tool. Error Condition = props.target.Length != 1");
                    return;
                }

                if (props.target[0].property.Length != 1)
                {
                    // this really shouldn't happen
                    Log.LogError("Invalid output from SVN tool. Error Condition = props.target[0].property.Length != 1");
                    return;
                }

                if (props.target[0].property[0].name != this.PropertyName)
                {
                    // this really shouldn't happen
                    Log.LogError(string.Format(CultureInfo.InvariantCulture, "Invalid output from SVN tool. Error Condition = props.target[0].property[0].name != this.PropertyName. Where this.PropertyName = {0}", this.PropertyName));
                    return;
                }

                // fill up the output
                this.PropertyValue = props.target[0].property[0].Value;
            }
        }

        private void ExecSetProperty()
        {
            // required params
            if (this.Item == null || string.IsNullOrEmpty(this.PropertyName))
            {
                Log.LogError("The Item and PropertyName parameters are required");
                return;
            }

            // execute the tool
            var cmd = this.CreateCommandLineBuilder();
            cmd.AppendSwitch("propset");
            cmd.AppendSwitch("--non-interactive");
            cmd.AppendFixedParameter(this.PropertyName);
            cmd.AppendFixedParameter(this.PropertyValue);
            cmd.AppendFileNameIfNotNull(this.Item);
            Utilities.ExecuteWithLogging(this.Log, Path.Combine(SvnPath, SvnExecutableName), cmd.ToString(), true);
        }

        private void ExecCheckout()
        {
            // required params
            if (this.Items == null || this.Items.Length == 0 || this.Destination == null)
            {
                Log.LogError("The Items and Destination parameters are required");
                return;
            }

            // execute the tool
            var cmd = this.CreateCommandLineBuilder();
            cmd.AppendSwitch("checkout");
            cmd.AppendSwitch("--non-interactive");
            cmd.AppendFileNamesIfNotNull(this.Items, " ");
            cmd.AppendFileNameIfNotNull(this.Destination);
            Utilities.ExecuteWithLogging(this.Log, Path.Combine(SvnPath, SvnExecutableName), cmd.ToString(), true);
        }

        private void ExecUpdate()
        {
            // required params
            if (this.Items == null || this.Items.Length == 0)
            {
                Log.LogError("The Items parameter is required");
                return;
            }

            // execute the tool
            var cmd = this.CreateCommandLineBuilder();
            cmd.AppendSwitch("update");
            cmd.AppendSwitch("--non-interactive");
            cmd.AppendFileNamesIfNotNull(this.Items, " ");
            Utilities.ExecuteWithLogging(this.Log, Path.Combine(SvnPath, SvnExecutableName), cmd.ToString(), true);
        }

        private void ExecAdd()
        {
            // required params
            if (this.Items == null || this.Items.Length == 0)
            {
                Log.LogError("The Items parameter is required");
                return;
            }

            // execute the tool
            var cmd = this.CreateCommandLineBuilder();
            cmd.AppendSwitch("add");
            cmd.AppendSwitch("--non-interactive");
            cmd.AppendFileNamesIfNotNull(this.Items, " ");
            Utilities.ExecuteWithLogging(this.Log, Path.Combine(SvnPath, SvnExecutableName), cmd.ToString(), true);
        }

        private void ExecCopy()
        {
            // required params
            if (this.Items == null || this.Items.Length == 0 || this.Destination == null)
            {
                Log.LogError("The Items and Destination parameters are required");
                return;
            }

            if (string.IsNullOrWhiteSpace(this.CommitMessage))
            {
                this.CommitMessage = DefaultCommitMessage;
            }

            if (this.CommitMessage.Contains("\""))
            {
                Log.LogError("There appears to be quotes in the commit message. This is not supported yet.");
                return;
            }

            // execute the tool
            var cmd = this.CreateCommandLineBuilder();
            cmd.AppendSwitch("copy");
            cmd.AppendSwitch("--non-interactive");
            cmd.AppendSwitch("-m");
            cmd.AppendFixedParameter("\"" + this.CommitMessage + "\"");
            cmd.AppendFileNamesIfNotNull(this.Items, " ");
            cmd.AppendFileNameIfNotNull(this.Destination);
            Utilities.ExecuteWithLogging(this.Log, Path.Combine(SvnPath, SvnExecutableName), cmd.ToString(), true);
        }

        private void ExecDelete()
        {
            // required params
            if (this.Items == null || this.Items.Length == 0)
            {
                Log.LogError("The Items parameter is required");
                return;
            }

            // execute the tool
            var cmd = this.CreateCommandLineBuilder();
            cmd.AppendSwitch("delete");
            cmd.AppendSwitch("--non-interactive");
            cmd.AppendFileNamesIfNotNull(this.Items, " ");
            Utilities.ExecuteWithLogging(this.Log, Path.Combine(SvnPath, SvnExecutableName), cmd.ToString(), true);
        }

        private void ExecMove()
        {
            // required params
            if (this.Items == null || this.Items.Length == 0 || this.Destination == null)
            {
                Log.LogError("The Items and Destination parameters are required");
                return;
            }

            // execute the tool
            var cmd = this.CreateCommandLineBuilder();
            cmd.AppendSwitch("move");
            cmd.AppendSwitch("--non-interactive");
            cmd.AppendFileNamesIfNotNull(this.Items, " ");
            cmd.AppendFileNameIfNotNull(this.Destination);
            Utilities.ExecuteWithLogging(this.Log, Path.Combine(SvnPath, SvnExecutableName), cmd.ToString(), true);
        }

        private void ExecCommit()
        {
            // required params
            if (this.Items == null || this.Items.Length == 0)
            {
                Log.LogError("The Items parameter is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(this.CommitMessage))
            {
                this.CommitMessage = DefaultCommitMessage;
            }

            if (this.CommitMessage.Contains("\""))
            {
                Log.LogError("There appears to be quotes in the commit message. This is not supported yet.");
                return;
            }

            // execute the tool
            var cmd = this.CreateCommandLineBuilder();
            cmd.AppendSwitch("commit");
            cmd.AppendSwitch("--non-interactive");
            cmd.AppendSwitch("-m");
            cmd.AppendFixedParameter("\"" + this.CommitMessage + "\"");
            cmd.AppendFileNamesIfNotNull(this.Items, " ");
            Utilities.ExecuteWithLogging(this.Log, Path.Combine(SvnPath, SvnExecutableName), cmd.ToString(), true);
        }

        private void ExecExport()
        {
            // required params
            if (this.Item == null || this.Destination == null)
            {
                Log.LogError("The Item and Destination parameters are required");
                return;
            }

            // execute the tool
            var cmd = this.CreateCommandLineBuilder();
            cmd.AppendSwitch("export");
            cmd.AppendSwitch("--non-interactive");
            if (this.Force)
            {
                cmd.AppendSwitch("--force");
            }

            cmd.AppendFileNameIfNotNull(this.Item);
            cmd.AppendFileNameIfNotNull(this.Destination);
            Utilities.ExecuteWithLogging(this.Log, Path.Combine(SvnPath, SvnExecutableName), cmd.ToString(), true);
        }

        private Utilities.CommandLineBuilder2 CreateCommandLineBuilder()
        {
            var cmd = new Utilities.CommandLineBuilder2();
            if (this.UserName != null)
            {
                cmd.AppendSwitch("--username " + this.UserName);
            }

            if (this.UserPassword != null)
            {
                cmd.AppendSwitch("--password " + this.UserPassword);
            }

            return cmd;
        }
    }
}

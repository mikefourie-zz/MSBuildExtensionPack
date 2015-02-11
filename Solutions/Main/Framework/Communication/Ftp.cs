//-----------------------------------------------------------------------
// <copyright file="Ftp.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Communication
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Extended;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>UploadFiles</i> (<b>Required:</b> Host, FileNames <b>Optional:</b> UserName, UserPassword, WorkingDirectory, RemoteDirectoryName, Port)</para>
    /// <para><i>DownloadFiles</i> (<b>Required:</b> Host <b>Optional:</b> FileNames, UserName, UserPassword, WorkingDirectory, RemoteDirectoryName, Port)</para>    
    /// <para><i>DeleteFiles</i> (<b>Required:</b> Host, FileNames <b>Optional:</b> UserName, UserPassword, WorkingDirectory, RemoteDirectoryName, Port)</para>    
    /// <para><i>DeleteDirectory</i> (<b>Required:</b> Host<b>Optional:</b> UserName, UserPassword, WorkingDirectory, RemoteDirectoryName, Port)</para>    
    /// <para><i>CreateDirectory</i> (<b>Required:</b> Host<b>Optional:</b> UserName, UserPassword, WorkingDirectory, RemoteDirectoryName, Port)</para>    
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///   <PropertyGroup>
    ///     <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///     <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     <ftpHost>localhost</ftpHost>
    ///   </PropertyGroup>
    ///   <Import Project="$(TPath)"/>
    ///   <Target Name="Default">
    ///     <ItemGroup>
    ///       <!-- Specify FilesToUpload -->
    ///       <FilesToUpload Include="C:\demo.txt" />
    ///       <FilesToUpload Include="C:\demo2.txt" />
    ///     </ItemGroup>
    ///     <MSBuild.ExtensionPack.Communication.Ftp TaskAction="UploadFiles" Host="$(ftpHost)" FileNames="@(FilesToUpload)"/>
    ///     <ItemGroup>
    ///       <!-- Specify the files to Download-->
    ///       <FilesToDownload Include="demo2.txt" />
    ///       <FilesToDownload Include="demo.txt" />
    ///     </ItemGroup>
    ///     <MSBuild.ExtensionPack.Communication.Ftp TaskAction="DownloadFiles" Host="$(ftpHost)" FileNames="@(FilesToDownload)" WorkingDirectory="C:\FtpWorkingFolder"/>
    ///     <MSBuild.ExtensionPack.Communication.Ftp TaskAction="CreateDirectory" Host="$(ftpHost)" RemoteDirectoryName="NewFolder1"/>
    ///     <MSBuild.ExtensionPack.Communication.Ftp TaskAction="CreateDirectory" Host="$(ftpHost)" RemoteDirectoryName="NewFolder2"/>
    ///     <MSBuild.ExtensionPack.Communication.Ftp TaskAction="DeleteDirectory" Host="$(ftpHost)" RemoteDirectoryName="NewFolder1"/>
    ///     <MSBuild.ExtensionPack.Communication.Ftp TaskAction="DeleteFiles" Host="$(ftpHost)" FileNames="@(FilesToDownload)" />
    ///   </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class Ftp : BaseTask
    {
        private const string UploadFilesTaskAction = "UploadFiles";
        private const string DownloadFilesTaskAction = "DownloadFiles";
        private const string DeleteFilesTaskAction = "DeleteFiles";
        private const string DeleteDirectoryTaskAction = "DeleteDirectory";
        private const string CreateDirectoryTaskAction = "CreateDirectory";

        /// <summary>
        /// Sets the Host of the FTP Site.
        /// </summary>
        [Required]
        public string Host { get; set; }

        /// <summary>
        /// Sets the Remote Path to connect to the FTP Site
        /// </summary>
        public string RemoteDirectoryName { get; set; }

        /// <summary>
        /// Sets the working directory on the local machine
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// The port used to connect to the ftp server.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Sets if the upload action will overwrite existing files
        /// </summary>
        public string Overwrite { get; set; }

        /// <summary>
        /// The list of files that needs to be transfered over FTP
        /// </summary>
        public ITaskItem[] FileNames { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (string.IsNullOrEmpty(this.Host))
            {
                this.Log.LogError("The required host attribute has not been set for FTP.");
                return;
            }

            switch (this.TaskAction)
            {
                case CreateDirectoryTaskAction:
                    this.CreateDirectory();
                    break;
                case DeleteDirectoryTaskAction:
                    this.DeleteDirectory();
                    break;
                case DeleteFilesTaskAction:
                    this.DeleteFiles();
                    break;
                case DownloadFilesTaskAction:
                    this.DownloadFiles();
                    break;
                case UploadFilesTaskAction:
                    this.UploadFiles();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid Task Action passed: {0}", this.TaskAction));
                    return;
            }
        }

        /// <summary>
        /// Creates a new Ftp directory on the ftp server.
        /// </summary>
        private void CreateDirectory()
        {
            if (string.IsNullOrEmpty(this.RemoteDirectoryName))
            {
                this.Log.LogError("The required RemoteDirectoryName attribute has not been set for FTP.");
                return;
            }

            using (FtpConnection ftpConnection = this.CreateFtpConnection())
            {
                ftpConnection.LogOn();
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating Directory: {0}", this.RemoteDirectoryName));
                try
                {
                    ftpConnection.CreateDirectory(this.RemoteDirectoryName);
                }
                catch (FtpException ex)
                {
                    if (ex.Message.Contains("550"))
                    {
                        return;
                    }

                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "There was an error creating ftp directory: {0}. The Error Details are \"{1}\" and error code is {2} ", this.RemoteDirectoryName, ex.Message, ex.ErrorCode));
                }
            }
        }

        /// <summary>
        /// Deletes an Ftp directory on the ftp server.
        /// </summary>
        private void DeleteDirectory()
        {
            if (string.IsNullOrEmpty(this.RemoteDirectoryName))
            {
                this.Log.LogError("The required RemoteDirectoryName attribute has not been set for FTP.");
                return;
            }

            using (FtpConnection ftpConnection = this.CreateFtpConnection())
            {
                ftpConnection.LogOn();
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting Directory: {0}", this.RemoteDirectoryName));
                try
                {
                    ftpConnection.DeleteDirectory(this.RemoteDirectoryName);
                }
                catch (FtpException ex)
                {
                    if (ex.Message.Contains("550"))
                    {
                        return;
                    }

                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "There was an error deleting ftp directory: {0}. The Error Details are \"{1}\" and error code is {2} ", this.RemoteDirectoryName, ex.Message, ex.ErrorCode));
                }
            }
        }

        /// <summary>
        /// Delete given files from the FTP Directory
        /// </summary>
        private void DeleteFiles()
        {
            if (this.FileNames == null)
            {
                this.Log.LogError("The required FileNames attribute has not been set for FTP.");
                return;
            }

            using (FtpConnection ftpConnection = this.CreateFtpConnection())
            {
                ftpConnection.LogOn();
                this.LogTaskMessage("Deleting Files");
                if (!string.IsNullOrEmpty(this.RemoteDirectoryName))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting Current Directory: {0}", this.RemoteDirectoryName));
                    ftpConnection.SetCurrentDirectory(this.RemoteDirectoryName);
                }

                foreach (string fileName in this.FileNames.Select(item => item.ItemSpec))
                {
                    try
                    {
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting: {0}", fileName));
                        ftpConnection.DeleteFile(fileName);
                    }
                    catch (FtpException ex)
                    {
                        if (ex.Message.Contains("550"))
                        {
                            continue;
                        }

                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "There was an error in deleting file: {0}. The Error Details are \"{1}\" and error code is {2} ", fileName, ex.Message, ex.ErrorCode));
                    }
                }
            }
        }

        /// <summary>
        /// Upload Files 
        /// </summary>
        private void UploadFiles()
        {
            if (this.FileNames == null)
            {
                this.Log.LogError("The required fileNames attribute has not been set for FTP.");
                return;
            }

            using (FtpConnection ftpConnection = this.CreateFtpConnection())
            {
                this.LogTaskMessage("Uploading Files");
                if (!string.IsNullOrEmpty(this.WorkingDirectory))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting Local Directory: {0}", this.WorkingDirectory));
                    FtpConnection.SetLocalDirectory(this.WorkingDirectory);
                }

                ftpConnection.LogOn();

                if (!string.IsNullOrEmpty(this.RemoteDirectoryName))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting Current Directory: {0}", this.RemoteDirectoryName));
                    ftpConnection.SetCurrentDirectory(this.RemoteDirectoryName);
                }

                var overwrite = true;
                var files = new List<FtpFileInfo>();
                if (!string.IsNullOrEmpty(this.Overwrite))
                {
                    if (!bool.TryParse(this.Overwrite, out overwrite))
                    {
                        overwrite = true;
                    }
                }

                if (!overwrite)
                {
                    files.AddRange(ftpConnection.GetFiles());
                }

                foreach (string fileName in this.FileNames.Select(item => item.ItemSpec))
                {
                    try
                    {
                        if (File.Exists(fileName))
                        {
                            if (!overwrite && files.FirstOrDefault(fi => fi.Name == fileName) != null)
                            {
                                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Skipped: {0}", fileName));
                                continue;
                            }

                            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Uploading: {0}", fileName));
                            ftpConnection.PutFile(fileName);
                        }
                    }
                    catch (FtpException ex)
                    {
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "There was an error uploading file: {0}. The Error Details are \"{1}\" and error code is {2} ", fileName, ex.Message, ex.ErrorCode));
                    }
                }
            }
        }

        /// <summary>
        /// Download Files 
        /// </summary>
        private void DownloadFiles()
        {
            using (FtpConnection ftpConnection = this.CreateFtpConnection())
            {
                if (!string.IsNullOrEmpty(this.WorkingDirectory))
                {
                    if (!Directory.Exists(this.WorkingDirectory))
                    {
                        Directory.CreateDirectory(this.WorkingDirectory);
                    }

                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting Local Directory: {0}", this.WorkingDirectory));

                    FtpConnection.SetLocalDirectory(this.WorkingDirectory);
                }

                ftpConnection.LogOn();

                if (!string.IsNullOrEmpty(this.RemoteDirectoryName))
                {
                    ftpConnection.SetCurrentDirectory(this.RemoteDirectoryName);
                }

                this.LogTaskMessage("Downloading Files");
                if (this.FileNames == null)
                {
                    FtpFileInfo[] filesToDownload = ftpConnection.GetFiles();
                    foreach (FtpFileInfo fileToDownload in filesToDownload)
                    {
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Downloading: {0}", fileToDownload));
                        ftpConnection.GetFile(fileToDownload.Name, false);
                    }
                }
                else
                {
                    foreach (string fileName in this.FileNames.Select(item => item.ItemSpec.Trim()))
                    {
                        try
                        {
                            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Downloading: {0}", fileName));
                            ftpConnection.GetFile(fileName, false);
                        }
                        catch (FtpException ex)
                        {
                            this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "There was an error downloading file: {0}. The Error Details are \"{1}\" and error code is {2} ", fileName, ex.Message, ex.ErrorCode));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates an FTP Connection object 
        /// </summary>
        /// <returns>An initialised FTP Connection</returns>
        private FtpConnection CreateFtpConnection()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Connecting to FTP Host: {0}", this.Host));

            if (!string.IsNullOrEmpty(this.UserName))
            {
                return this.Port != 0 ? new FtpConnection(this.Host, this.Port, this.UserName, this.UserPassword) : new FtpConnection(this.Host, this.UserName, this.UserPassword);
            }

            return this.Port != 0 ? new FtpConnection(this.Host, this.Port) : new FtpConnection(this.Host);
        }
    }
}
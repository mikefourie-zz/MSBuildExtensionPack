//-----------------------------------------------------------------------
// <copyright file="Ftp.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Communication
{
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Framework;
    using MSBuild.ExtensionPack.Communication.Extended;

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
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.8.0/html/e38221c2-c686-2a47-489c-ea2ef10d915b.htm")]
    public class Ftp : BaseTask
    {
        private const string UploadFilesTaskAction = "UploadFiles";
        private const string DownloadFilesTaskAction = "DownloadFiles";
        private const string DeleteFilesTaskAction = "DeleteFiles";
        private const string DeleteDirectoryTaskAction = "DeleteDirectory";
        private const string CreateDirectoryTaskAction = "CreateDirectory";

        [DropdownValue(CreateDirectoryTaskAction)]
        [DropdownValue(DeleteDirectoryTaskAction)]
        [DropdownValue(DeleteFilesTaskAction)]
        [DropdownValue(DownloadFilesTaskAction)]
        [DropdownValue(UploadFilesTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }
        
        /// <summary>
        /// Sets the Host of the FTP Site.
        /// </summary>
        [Required]
        [TaskAction(CreateDirectoryTaskAction, true)]
        [TaskAction(DeleteDirectoryTaskAction, true)]
        [TaskAction(DeleteFilesTaskAction, true)]
        [TaskAction(DownloadFilesTaskAction, true)]
        [TaskAction(UploadFilesTaskAction, true)]
        public string Host { get; set; }
        
        /// <summary>
        /// Sets the Remote Path to connect to the FTP Site
        /// </summary>        
        [TaskAction(CreateDirectoryTaskAction, true)]
        [TaskAction(DeleteDirectoryTaskAction, true)]
        [TaskAction(DeleteFilesTaskAction, false)]
        [TaskAction(DownloadFilesTaskAction, false)]
        [TaskAction(UploadFilesTaskAction, false)]
        public string RemoteDirectoryName { get; set; }

        /// <summary>
        /// Sets the working directory on the local machine
        /// </summary>        
        [TaskAction(CreateDirectoryTaskAction, false)]
        [TaskAction(DeleteDirectoryTaskAction, false)]
        [TaskAction(DeleteFilesTaskAction, false)]
        [TaskAction(DownloadFilesTaskAction, false)]
        [TaskAction(UploadFilesTaskAction, false)]
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// The port used to connect to the ftp server.
        /// </summary>        
        [TaskAction(CreateDirectoryTaskAction, false)]
        [TaskAction(DeleteDirectoryTaskAction, false)]
        [TaskAction(DeleteFilesTaskAction, false)]
        [TaskAction(DownloadFilesTaskAction, false)]
        [TaskAction(UploadFilesTaskAction, false)]
        public int Port { get; set; }

        /// <summary>
        /// The list of files that needs to be transfered over FTP
        /// </summary>        
        [TaskAction(CreateDirectoryTaskAction, false)]
        [TaskAction(DeleteDirectoryTaskAction, false)]
        [TaskAction(DeleteFilesTaskAction, true)]
        [TaskAction(DownloadFilesTaskAction, false)]
        [TaskAction(UploadFilesTaskAction, true)]
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
            FtpConnection ftpConnection = this.CreateFtpConnection();            
            try
            {   
                ftpConnection.LogOn();
                if (string.IsNullOrEmpty(this.RemoteDirectoryName))
                {
                    this.Log.LogError("The required Remote Directory Name attribute has not been set for FTP.");                 
                }
                else
                {
                    try
                    {
                        if (ftpConnection.DirectoryExists(this.RemoteDirectoryName))
                        {
                            this.Log.LogError("The FTP Directory already exists on the ftp server.");
                        }
                        else
                        {
                            ftpConnection.CreateDirectory(this.RemoteDirectoryName);
                        }
                    }
                    catch (FtpException ex)
                    {
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "There was an error creating ftp directory: {0}. The Error Details are \"{1}\" and error code is {2} ", this.RemoteDirectoryName, ex.Message, ex.ErrorCode));
                    }                
                }
            }
            finally
            {
                ftpConnection.Close();
            }
        }

        /// <summary>
        /// Deletes an Ftp directory on the ftp server.
        /// </summary>
        private void DeleteDirectory()
        {
            FtpConnection ftpConnection = this.CreateFtpConnection();
            try
            {
                ftpConnection.LogOn();
                if (string.IsNullOrEmpty(this.RemoteDirectoryName))
                {
                    this.Log.LogError("The required Remote Directory Name attribute has not been set for FTP.");
                }
                else
                {
                    try
                    {
                        if (!ftpConnection.DirectoryExists(this.RemoteDirectoryName))
                        {                            
                            this.Log.LogError("The FTP Directory does not exist.");
                        }
                        else
                        {
                            ftpConnection.DeleteDirectory(this.RemoteDirectoryName);                         
                        }
                    }
                    catch (FtpException ex)
                    {
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "There was an error creating ftp directory: {0}. The Error Details are \"{1}\" and error code is {2} ", this.RemoteDirectoryName, ex.Message, ex.ErrorCode));
                    }
                }
            }
            finally
            {
                ftpConnection.Close();
            }
        }

        /// <summary>
        /// Delete given files from the FTP Directory
        /// </summary>
        private void DeleteFiles()
        {
            FtpConnection ftpConnection = this.CreateFtpConnection();
            try
            {
                ftpConnection.LogOn();
                if (this.FileNames == null)
                {
                    this.Log.LogError("The required Files Names attribute has not been set for FTP.");
                }
                else
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(this.RemoteDirectoryName))
                        {
                            ftpConnection.SetCurrentDirectory(this.RemoteDirectoryName);
                        }

                        foreach (string fileName in this.FileNames.Select(item => item.ItemSpec))
                        {
                            try
                            {
                                if (ftpConnection.FileExists(fileName))
                                {
                                    ftpConnection.DeleteFile(fileName);                                    
                                }
                            }
                            catch (FtpException ex)
                            {   
                                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "There was an error in deleting file: {0}. The Error Details are \"{1}\" and error code is {2} ", fileName, ex.Message, ex.ErrorCode));
                            }
                        }
                    }
                    catch (FtpException ex)
                    {
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "There was an error creating ftp directory: {0}. The Error Details are \"{1}\" and error code is {2} ", this.RemoteDirectoryName, ex.Message, ex.ErrorCode));
                    }
                }
            }
            finally
            {
                ftpConnection.Close();
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

            FtpConnection ftpConnection = this.CreateFtpConnection();            
            try
            {   
                if (!string.IsNullOrEmpty(this.WorkingDirectory))
                {
                    FtpConnection.SetLocalDirectory(this.WorkingDirectory);        
                }

                ftpConnection.LogOn();

                if (!string.IsNullOrEmpty(this.RemoteDirectoryName))
                {
                    ftpConnection.SetCurrentDirectory(this.RemoteDirectoryName);
                }

                foreach (string fileName in this.FileNames.Select(item => item.ItemSpec))
                {
                    try
                    {
                        if (File.Exists(fileName))
                        {
                            ftpConnection.PutFile(fileName);
                        }
                    }
                    catch (FtpException ex)
                    {
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "There was an error uploading file: {0}. The Error Details are \"{1}\" and error code is {2} ", fileName, ex.Message, ex.ErrorCode));
                    }
                }
            }
            finally
            {   
                ftpConnection.Close();
            }
        }

        /// <summary>
        /// Download Files 
        /// </summary>
        private void DownloadFiles()
        {
            FtpConnection ftpConnection = this.CreateFtpConnection();
            try
            {
                if (!string.IsNullOrEmpty(this.WorkingDirectory))
                {
                    FtpConnection.SetLocalDirectory(this.WorkingDirectory);
                }

                ftpConnection.LogOn();

                if (!string.IsNullOrEmpty(this.RemoteDirectoryName))
                {
                    ftpConnection.SetCurrentDirectory(this.RemoteDirectoryName);
                }

                if (this.FileNames == null)
                {
                    FtpFileInfo[] filesToDownload = ftpConnection.GetFiles();
                    foreach (FtpFileInfo fileToDownload in filesToDownload)
                    {
                        ftpConnection.GetFile(fileToDownload.Name, false);
                    }
                }
                else
                {
                    foreach (string fileName in this.FileNames.Select(item => item.ItemSpec.Trim()))
                    {
                        try
                        {
                            ftpConnection.GetFile(fileName, false);                            
                        }
                        catch (FtpException ex)
                        {
                            this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "There was an error downloading file: {0}. The Error Details are \"{1}\" and error code is {2} ", fileName, ex.Message, ex.ErrorCode));
                        }
                    }
                }
            }
            finally
            {
                ftpConnection.Close();
            }
        }

        /// <summary>
        /// Creates an FTP Connection object 
        /// </summary>
        /// <returns>An initialised FTP Connection</returns>
        private FtpConnection CreateFtpConnection()
        {
            if (!string.IsNullOrEmpty(this.UserName))
            {
                return this.Port != 0 ? new FtpConnection(this.Host, this.Port, this.UserName, this.UserPassword) : new FtpConnection(this.Host, this.UserName, this.UserPassword);                
            }

            return this.Port != 0 ? new FtpConnection(this.Host, this.Port) : new FtpConnection(this.Host);
        }
    }
}
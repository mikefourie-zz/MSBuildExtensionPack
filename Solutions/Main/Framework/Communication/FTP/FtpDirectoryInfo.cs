//-----------------------------------------------------------------------
// <copyright file="FtpDirectoryInfo.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Communication.Extended
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;

    /// <summary>
    /// The <c>FtpDirectoryInfo</c> class encapsulates a remote FTP directory.
    /// </summary>
    [Serializable]    
    public class FtpDirectoryInfo : FileSystemInfo
    {
        private DateTime? creationTime;
        private DateTime? lastAccessTime;
        private DateTime? lastWriteTime;
        
        public FtpDirectoryInfo(FtpConnection ftp, string path)
        {
            this.FtpConnection = ftp;
            this.FullPath = path;
        }
        
        protected FtpDirectoryInfo(SerializationInfo info, StreamingContext context) : base(info, context)
        {         
        }

        public FtpConnection FtpConnection { get; internal set; }

        public new DateTime? LastAccessTime
        {
            get { return this.lastAccessTime.HasValue ? (DateTime?)this.lastAccessTime.Value : null; }
            internal set { this.lastAccessTime = value; }
        }

        public new DateTime? CreationTime
        {
            get { return this.creationTime.HasValue ? (DateTime?)this.creationTime.Value : null; }
            internal set { this.creationTime = value; }
        }

        public new DateTime? LastWriteTime
        {
            get { return this.lastWriteTime.HasValue ? (DateTime?)this.lastWriteTime.Value : null; }
            internal set { this.lastWriteTime = value; }
        }

        public new DateTime? LastAccessTimeUtc
        {
            get { return this.lastAccessTime.HasValue ? (DateTime?)this.lastAccessTime.Value.ToUniversalTime() : null; }
        }

        public new DateTime? CreationTimeUtc
        {
            get { return this.creationTime.HasValue ? (DateTime?)this.creationTime.Value.ToUniversalTime() : null; }
        }

        public new DateTime? LastWriteTimeUtc
        {
            get { return this.lastWriteTime.HasValue ? (DateTime?)this.lastWriteTime.Value.ToUniversalTime() : null; }
        }

        public new FileAttributes Attributes { get; set; }

        public override bool Exists
        {
            get { return this.FtpConnection.DirectoryExists(this.FullName); }
        }

        public override string Name
        {
            get { return Path.GetFileName(this.FullPath); }
        }

        public override void Delete()
        {
            this.FtpConnection.DeleteDirectory(this.Name);
        }

        public FtpDirectoryInfo[] GetDirectories()
        {
            return this.FtpConnection.GetDirectories(this.FullPath);
        }

        public FtpDirectoryInfo[] GetDirectories(string path)
        {
            path = Path.Combine(this.FullPath, path);
            return this.FtpConnection.GetDirectories(path);
        }

        public FtpFileInfo[] GetFiles()
        {
            return this.GetFiles(this.FtpConnection.GetCurrentDirectory());
        }

        public FtpFileInfo[] GetFiles(string mask)
        {
            return this.FtpConnection.GetFiles(mask);
        }

        /// <summary>
        /// No specific impelementation is needed of the GetObjectData to serialize this object
        /// because all attributes are redefined.
        /// </summary>
        /// <param name="info">The SerializationInfo to populate with data. </param>
        /// <param name="context">The destination for this serialization. </param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {   
            base.GetObjectData(info, context);
        }
    }
}
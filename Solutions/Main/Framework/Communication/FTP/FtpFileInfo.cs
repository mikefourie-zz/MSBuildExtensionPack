//-----------------------------------------------------------------------
// <copyright file="FtpFileInfo.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Communication.Extended
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;

    /// <summary>
    /// The <c>FtpFileInfo</c> class encapsulates a remote FTP directory.
    /// </summary>
    [Serializable]
    public sealed class FtpFileInfo : FileSystemInfo
    {
        private readonly string fileName; 
        private readonly FtpConnection ftpConnection;

        private DateTime? lastAccessTime;
        private DateTime? lastWriteTime;
        private DateTime? creationTime;

        public FtpFileInfo(FtpConnection ftp, string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            this.OriginalPath = filePath;
            this.FullPath = filePath;
            this.ftpConnection = ftp;
            this.fileName = Path.GetFileName(filePath);
        }

        private FtpFileInfo(SerializationInfo info, StreamingContext context) : base(info, context)
        {         
        }

        public FtpConnection FtpConnection
        {
            get { return this.ftpConnection; }
        }

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

        public new FileAttributes Attributes { get; internal set; }

        public override string Name
        {
            get { return this.fileName; }
        }

        public override bool Exists
        {
            get { return this.FtpConnection.FileExists(this.FullName); }
        }

        public override void Delete()
        {
            this.FtpConnection.DeleteDirectory(this.FullName);
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
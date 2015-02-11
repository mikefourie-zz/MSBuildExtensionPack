//-----------------------------------------------------------------------
// <copyright file="FtpException.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Communication.Extended
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The <c>FtpException</c> class encapsulates an FTP exception.
    /// </summary>
    [Serializable]
    public class FtpException : Exception
    {
        private readonly int ftpError;

        public FtpException()
        {
            this.ftpError = 0;            
        }

        public FtpException(string message) : this(-1, message)
        {
        }
        
        public FtpException(int error, string message) : base(message)
        {
            this.ftpError = error;
        }

        public FtpException(string message, Exception innerException) : base(message, innerException)
        {         
        }

        protected FtpException(SerializationInfo info, StreamingContext context) : base(info, context)
        {         
        }

        public int ErrorCode
        {
            get { return this.ftpError; }
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
//-----------------------------------------------------------------------
// <copyright file="ExecuteEventArgs.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.SqlServer.Extended
{
    using System;
    using System.Data.SqlClient;
    using System.IO;

    internal class ExecuteEventArgs : EventArgs
    {
        private readonly FileInfo scriptFileInfo;
        private readonly bool succeeded;
        private readonly Exception executionException;
        private readonly SqlErrorCollection sqlInfo;

        public ExecuteEventArgs(FileInfo scriptFileInfo)
        {
            this.scriptFileInfo = scriptFileInfo;
            this.succeeded = true;
        }

        public ExecuteEventArgs(SqlErrorCollection sqlInfo)
        {
            this.succeeded = true;
            this.sqlInfo = sqlInfo;
        }

        public ExecuteEventArgs(FileInfo scriptFileInfo,  Exception reasonForFailure)
        {
            this.scriptFileInfo = scriptFileInfo;
            this.executionException = reasonForFailure;
        }

        public SqlErrorCollection SqlInfo
        {
            get { return this.sqlInfo; }
        }

        public System.IO.FileInfo ScriptFileInfo
        {
            get
            {
                return this.scriptFileInfo;
            }
        }

        public bool Succeeded
        {
            get
            {
                return this.succeeded;
            }
        }

        public Exception ExecutionException
        {
            get
            {
                return this.executionException;
            }
        }
    }
}
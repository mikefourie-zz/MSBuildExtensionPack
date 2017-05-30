//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="ExecuteEventArgs.cs">(c) 2017 Mike Fourie and Contributors (http://www.MSBuildExtensionPack.com) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
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

        public SqlErrorCollection SqlInfo => this.sqlInfo;

        public System.IO.FileInfo ScriptFileInfo => this.scriptFileInfo;

        public bool Succeeded => this.succeeded;

        public Exception ExecutionException => this.executionException;
    }
}
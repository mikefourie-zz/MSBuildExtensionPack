//-----------------------------------------------------------------------
// <copyright file="IHostsFileWriter.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System.IO;

    public interface IHostsFileWriter
    {
        void Write(string path, IHostsFile hostsFile);
    }

    internal sealed class HostsFileWriter : IHostsFileWriter
    {
        public void Write(string path, IHostsFile hostsFile)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                hostsFile.Save(sw);
            }
        }
    }
}

//-----------------------------------------------------------------------
// <copyright file="IHostsFileReader.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System.IO;

    public interface IHostsFileReader
    {
        IHostsFile Read(string path, bool truncate);
    }

    internal sealed class HostsFileReader : IHostsFileReader
    {
        public IHostsFile Read(string path, bool truncate)
        {
            if (File.Exists(path))
            {
                return new HostsFileEntries(File.ReadAllLines(path), truncate);
            }

            return new HostsFileEntries(new string[0]);
        }
    }
}

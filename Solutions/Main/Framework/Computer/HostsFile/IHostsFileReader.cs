//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="IHostsFileReader.cs">(c) 2017 Mike Fourie and Contributors (https://github.com/mikefourie/MSBuildExtensionPack) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
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

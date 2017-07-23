//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="IHostsFileWriter.cs">(c) 2017 Mike Fourie and Contributors (https://github.com/mikefourie/MSBuildExtensionPack) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSBuild.ExtensionPack.Git
{
    /// <summary>
    /// Facade for NGit
    /// </summary>
    public interface IGitFacade
    {
        void Clone(string repositoryToClone, string targetDirectory);
        void CheckoutBranch(string localRepository, string branch);
        string GetLatestSHA(string localRepository);
    }
}

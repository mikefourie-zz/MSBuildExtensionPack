//-----------------------------------------------------------------------
// <copyright file="IGitFacade.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
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

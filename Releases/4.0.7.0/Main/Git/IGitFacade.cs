//-----------------------------------------------------------------------
// <copyright file="IGitFacade.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Git
{
    /// <summary>
    /// IGitFacade
    /// </summary>
    public interface IGitFacade
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="repositoryToClone">repositoryToClone</param>
        /// <param name="targetDirectory">targetDirectory</param>
        void Clone(string repositoryToClone, string targetDirectory);

        /// <summary>
        /// CheckoutBranch
        /// </summary>
        /// <param name="localRepository">localRepository</param>
        /// <param name="branch">branch</param>
        void CheckoutBranch(string localRepository, string branch);

        /// <summary>
        /// GetLatestSHA
        /// </summary>
        /// <param name="localRepository">localRepository</param>
        /// <returns>string</returns>
        string GetLatestSHA(string localRepository);
    }
}

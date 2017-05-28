//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="IGitFacade.cs">(c) 2017 Mike Fourie and Contributors (http://www.MSBuildExtensionPack.com) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
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

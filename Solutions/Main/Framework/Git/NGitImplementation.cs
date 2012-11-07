using NGit;
using NGit.Api;
using Sharpen;

namespace MSBuild.ExtensionPack.Git
{
    class NGitImplementation : IGitFacade
    {
        /// <summary>
        /// Clone a Git repository.
        /// </summary>
        /// <param name="repositoryToClone">The repository to clone.</param>
        /// <param name="targetDirectory">The target directory where you want to place the clone.</param>
        public void Clone(string repositoryToClone, string targetDirectory)
        {            
            NGit.Api.Git.
                CloneRepository().
                SetURI(repositoryToClone).SetDirectory(new FilePath(targetDirectory)).
                SetBare(false).
                SetCloneAllBranches(true).
                Call();
        }

        /// <summary>
        /// Checkout a branch or SHA.
        /// </summary>
        /// <param name="localRepository">The local Git repository.</param>
        /// <param name="branch">The branch or SHA you want to check out.</param>
        public void CheckoutBranch(string localRepository, string branch)
        {
            NGit.Api.Git.Open(localRepository).Checkout().SetName(branch).Call();
        }

        /// <summary>
        /// Gets the latest SHA from a local Git repository.
        /// </summary>
        /// <param name="localRepository">The local Git repository.</param>
        /// <returns>The SHA of the latest commit</returns>
        public string GetLatestSha(string localRepository)
        {
            ObjectId latestCommit = NGit.Api.Git.
                                        Open(localRepository).
                                        Log().
                                        GetRepository().
                                        Resolve(Constants.HEAD);

            return latestCommit.Name;
        }
    }
}

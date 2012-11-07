using System;
using System.Globalization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace MSBuild.ExtensionPack.Git
{
    public class Clone : Task
    {
        private readonly IGitFacade _gitFacade;
        private string _sha;

        /// <summary>
        /// Initializes a new instance of the <see cref="Clone"/> class.
        /// </summary>
        /// <param name="facade">IGitFacade interface</param>
        /// <remarks>Added to be able to unit test this class using a mock</remarks>
        public Clone(IGitFacade facade)
        {
            _gitFacade = facade;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Clone"/> class.
        /// </summary>
        /// <remarks>When no parameter is used in the constructor, instantiate the default</remarks>
        public Clone()
        {
            _gitFacade = new NGitImplementation();
        }

        /// <summary>
        /// Gets or sets the repository to clone.
        /// </summary>
        /// <value>
        /// The repository to clone.
        /// </value>
        [Required]
        public string RepositoryToClone { get; set; }

        /// <summary>
        /// Gets or sets the target directory.
        /// </summary>
        /// <value>
        /// The target directory.
        /// </value>
        [Required]
        public string TargetDirectory { get; set; }

        /// <summary>
        /// Gets the SHA of the latest commit in the given repository.
        /// </summary>
        [Output]
        public string Sha
        {
            get { return _sha; }
        }

        /// <summary>
        /// Gets or sets the branch to switch to.
        /// </summary>
        /// <value>
        /// The branch to switch to.
        /// </value>
        public string BranchToSwitchTo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// true if the task successfully executed; otherwise, false.
        /// </returns>
        public override bool Execute()
        {
            try
            {
                _gitFacade.Clone(RepositoryToClone, TargetDirectory);
                Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Cloning {0} to {1}", RepositoryToClone, TargetDirectory));

                if (!string.IsNullOrEmpty(BranchToSwitchTo) && BranchToSwitchTo.ToLower(CultureInfo.InvariantCulture) != "master")
                {
                    _gitFacade.CheckoutBranch(TargetDirectory, BranchToSwitchTo);
                    Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Checking out branch/SHA '{0}'", BranchToSwitchTo));
                }

                _sha = _gitFacade.GetLatestSha(TargetDirectory);
                Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Latest commit is '{0}'", _sha));
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }

            return true;
        }

    }
}
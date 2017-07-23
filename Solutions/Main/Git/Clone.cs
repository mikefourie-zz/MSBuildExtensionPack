//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="Clone.cs">(c) 2017 Mike Fourie and Contributors (https://github.com/mikefourie/MSBuildExtensionPack) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Git
{
    using System;
    using System.Globalization;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Clone
    /// </summary>
    public class Clone : Task
    {
        private readonly IGitFacade gitFacade;

        /// <summary>
        /// Initializes a new instance of the <see cref="Clone"/> class.
        /// </summary>
        /// <param name="facade">IGitFacade interface</param>
        /// <remarks>Added to be able to unit test this class using a mock</remarks>
        public Clone(IGitFacade facade)
        {
            this.gitFacade = facade;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Clone"/> class.
        /// </summary>
        /// <remarks>When no parameter is used in the constructor, instantiate the default</remarks>
        public Clone()
        {
            this.gitFacade = new NGitImplementation();
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
        public string SHA { get; private set; }

        /// <summary>
        /// Gets or sets the branch to switch to.
        /// </summary>
        /// <value>
        /// The branch to switch to.
        /// </value>
        public string BranchToSwitchTo { get; set; }

        /// <summary>
        /// Execute the task
        /// </summary>
        /// <returns>bool</returns>
        public override bool Execute()
        {
            try
            {
                this.gitFacade.Clone(this.RepositoryToClone, this.TargetDirectory);
                this.Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Cloning {0} to {1}", this.RepositoryToClone, this.TargetDirectory));

                if (!string.IsNullOrEmpty(this.BranchToSwitchTo) && this.BranchToSwitchTo.ToUpperInvariant() != "MASTER")
                {
                    this.gitFacade.CheckoutBranch(this.TargetDirectory, this.BranchToSwitchTo);
                    this.Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Checking out branch/SHA '{0}'", this.BranchToSwitchTo));
                }

                this.SHA = this.gitFacade.GetLatestSHA(this.TargetDirectory);
                this.Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Latest commit is '{0}'", this.SHA));
            }
            catch (Exception ex)
            {
                this.Log.LogErrorFromException(ex);
                return false;
            }

            return true;
        }
    }
}

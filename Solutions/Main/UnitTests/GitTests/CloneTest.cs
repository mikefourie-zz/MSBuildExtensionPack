//-----------------------------------------------------------------------
// <copyright file="CloneTest.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.GitTests
{
    using MSBuild.ExtensionPack.Git;
    using Microsoft.Build.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class CloneTest
    {
        /// <summary>
        /// Happy path. Execute should return true when no errors occur.
        /// </summary>
        [Test]
        public void ShouldReturnTrueWhenCalled()
        {
            // Arrange
            var msbuildCloneTask = new Clone(new Mock<IGitFacade>().Object)
            {
                // Mock away IBuildEngine cause we are not interested in the logging functionality.
                BuildEngine = new Mock<IBuildEngine>().Object,
                RepositoryToClone = string.Empty,
                TargetDirectory = string.Empty
            };

            // Act
            bool result = msbuildCloneTask.Execute();

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Tests if Clone and GetLatestSHA are called.
        /// </summary>
        [Test]
        public void CloneRepository()
        {
            // Arrange
            var mock = new Mock<IGitFacade>();
            var msbuildCloneTask = new Clone(mock.Object)
            {
                // Mock away IBuildEngine cause we are not interested in the logging functionality.
                BuildEngine = new Mock<IBuildEngine>().Object,
                RepositoryToClone = string.Empty,
                TargetDirectory = string.Empty
            };

            // Act
            Assert.IsTrue(msbuildCloneTask.Execute());

            // Assert
            mock.Verify(git => git.Clone(msbuildCloneTask.RepositoryToClone, msbuildCloneTask.TargetDirectory), Times.Once());
            mock.Verify(git => git.GetLatestSha(msbuildCloneTask.TargetDirectory), Times.Once());
        }

        /// <summary>
        /// Tests if Clone, CheckoutBranch and GetLatestSHA is called.
        /// </summary>
        [Test]
        public void CloneRepositoryCheckoutBranch()
        {
            // Arrange
            var mock = new Mock<IGitFacade>();
            var msbuildCloneTask = new Clone(mock.Object)
            {
                // Mock away IBuildEngine cause we are not interested in the logging functionality.
                BuildEngine = new Mock<IBuildEngine>().Object,
                RepositoryToClone = string.Empty,
                TargetDirectory = string.Empty,
                BranchToSwitchTo = "somebranchtoswitchto"
            };

            // Act
            msbuildCloneTask.Execute();

            // Assert
            mock.Verify(git => git.Clone(msbuildCloneTask.RepositoryToClone, msbuildCloneTask.TargetDirectory), Times.Once());
            mock.Verify(git => git.CheckoutBranch(msbuildCloneTask.TargetDirectory, msbuildCloneTask.BranchToSwitchTo), Times.Once());
            mock.Verify(git => git.GetLatestSha(msbuildCloneTask.TargetDirectory), Times.Once());
        }

        /// <summary>
        /// Execute should return false when an exception occurs in the task.
        /// </summary>
        [Test]
        public void ShouldReturnFalseWhenExceptionOccurs()
        {
            // Arrange
            var gitMock = new Mock<IGitFacade>();
            var msbuildCloneTask = new Clone(gitMock.Object)
            {
                BuildEngine = new Mock<IBuildEngine>().Object,
                RepositoryToClone = string.Empty,
                TargetDirectory = string.Empty
            };

            gitMock.Setup(git => git.Clone(msbuildCloneTask.RepositoryToClone, msbuildCloneTask.TargetDirectory)).Throws<InvalidOperationException>();

            // Act 
            bool result = msbuildCloneTask.Execute();

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// MSBuild should log an error when an exception occurs in the task.
        /// </summary>
        [Test]
        public void ShouldLogErrorWhenExceptionErrors()
        {
            // Arrange
            var gitMock = new Mock<IGitFacade>();
            var buildengineMock = new Mock<IBuildEngine>();
            var msbuildCloneTask = new Clone(gitMock.Object)
            {
                BuildEngine = buildengineMock.Object,
                RepositoryToClone = string.Empty,
                TargetDirectory = string.Empty
            };
            gitMock.Setup(git => git.Clone(msbuildCloneTask.RepositoryToClone, msbuildCloneTask.TargetDirectory)).Throws<InvalidOperationException>();

            // Act 
            msbuildCloneTask.Execute();

            // Assert
            buildengineMock.Verify(msbuild => msbuild.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()));
        }
    }
}

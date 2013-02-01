//-----------------------------------------------------------------------
// <copyright file="PathTest.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework.Tests
{
    using Microsoft.QualityTools.Testing.Fakes;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PathTest
    {
        [TestMethod]
        public void Path_GetExtension()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.Filepath = @"C:\myfile.myex";
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "GetExtension";

            // act
            target.Execute();

            // assert
            Assert.IsTrue(target.Value == ".myex");
        }

        [TestMethod]
        public void Path_GetFileName()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.Filepath = @"C:\myfile.myex";
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "GetFileName";

            // act
            target.Execute();

            // assert
            Assert.IsTrue(target.Value == "myfile.myex");
        }

        [TestMethod]
        public void Path_GetFileNameWithoutExtension()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.Filepath = @"C:\myfile.myex";
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "GetFileNameWithoutExtension";

            // act
            target.Execute();

            // assert
            Assert.IsTrue(target.Value == "myfile");
        }

        [TestMethod]
        public void Path_GetFullPath()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.Filepath = @"C:\myfile.myex";
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "GetFullPath";

            // act
            target.Execute();

            // assert
            Assert.IsTrue(target.Value == @"C:\myfile.myex");
        }

        [TestMethod]
        public void Path_ChangeExtension()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.Filepath = @"C:\myfile.myex";
            target.Extension = "log";
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "ChangeExtension";

            // act
            target.Execute();

            // assert
            Assert.IsTrue(target.Value == @"C:\myfile.log");
        }

        [TestMethod]
        public void Path_GetRandomFileName()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "GetRandomFileName";
            using (ShimsContext.Create())
            {
                System.IO.Fakes.ShimPath.GetRandomFileName = () => "abc.sds";

                // act
                target.Execute();

                // assert
                Assert.AreEqual(target.Value, "abc.sds");
            }
        }

        [TestMethod]
        public void Path_Combine()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.Filepath = @"C:\myfile";
            target.Filepath2 = @"log.txt";
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "Combine";

            // act
            target.Execute();

            // assert
            Assert.IsTrue(target.Value == @"C:\myfile\log.txt");
        }

        [TestMethod]
        public void Path_GetDirectoryName()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.Filepath = @"C:\mydir\myfile.txt";
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "GetDirectoryName";

            // act
            target.Execute();

            // assert
            Assert.IsTrue(target.Value == @"C:\mydir");
        }

        [TestMethod]
        public void Path_CantExecuteRemote()
        {
            // arrange
            Path target = new Path();
            target.Filepath = @"C:\myfile.myex";
            target.MachineName = "Another";
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "GetExtension";

            // act
            target.Execute();

            // assert
            Assert.IsTrue(target.Value == null);
        }
    }
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="PathTest.cs">(c) 2017 Mike Fourie and Contributors (http://www.MSBuildExtensionPack.com) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
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
        public void Path_GetPathRoot()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.Filepath = @"C:\mypath\mypath2\myfile.myex";
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "GetPathRoot";

            // act
            target.Execute();

            // assert
            Assert.IsTrue(target.Value == @"C:\");
        }

        [TestMethod]
        public void Path_HasExtensionTrue()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.Filepath = @"C:\mypath\mypath2\myfile.myex";
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "HasExtension";

            // act
            target.Execute();

            // assert
            Assert.AreEqual(target.Value, "True");
        }

        [TestMethod]
        public void Path_HasExtensionFalse()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.Filepath = @"C:\mypath\mypath2\myfile";
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "HasExtension";

            // act
            target.Execute();

            // assert
            Assert.AreEqual(target.Value, "False");
        }

        [TestMethod]
        public void Path_IsPathRootedFalse()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.Filepath = @"..\myfile.txt";
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "IsPathRooted";

            // act
            target.Execute();

            // assert
            Assert.AreEqual(target.Value, "False");
        }

        [TestMethod]
        public void Path_IsPathRootedTrue()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.Filepath = @"c:\myfile.txt";
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "IsPathRooted";

            // act
            target.Execute();

            // assert
            Assert.AreEqual(target.Value, "True");
        }

        [TestMethod]
        public void Path_InvalidTaskAction()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "NotValid";

            // act
            bool result = target.Execute();

            // assert
            Assert.AreEqual(result, false);
        }

        [TestMethod]
        public void Path_GetTempPath()
        {
            // arrange
            MSBuild.ExtensionPack.Framework.Path target = new MSBuild.ExtensionPack.Framework.Path();
            target.BuildEngine = new MockBuildEngine();
            target.TaskAction = "GetTempPath";

            // act
            target.Execute();

            // assert
            Assert.IsTrue(target.Value == System.IO.Path.GetTempPath());
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

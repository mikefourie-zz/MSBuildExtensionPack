//-----------------------------------------------------------------------
// <copyright file="TextStringTest.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FrameworkTests
{
    using System;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MSBuild.ExtensionPack.Framework;

    /// <summary>
    /// Unit Tests for TestString Task
    /// </summary>
    [TestClass]
    public class TextStringTest
    {
        #region Test Setup and Teardown
        #endregion

        #region Test Methods
        [TestMethod]
        public void TextStringSplitNoString1Test()
        {
            TextString target = new TextString();
            target.String1 = null;
            target.String2 = " ";
            target.TaskAction = "Split";
            target.BuildEngine = new MockBuildEngine();

            bool result = target.Execute();
            Assert.IsFalse(result);

            target.String1 = String.Empty;
            result = target.Execute();
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TextStringSplitNoString2Test()
        {
            TextString target = new TextString();
            target.String1 = "The  quick  brown  fox  jumped  over  the  lazy  dog.";
            target.String2 = null;
            target.TaskAction = "Split";
            target.BuildEngine = new MockBuildEngine();

            bool result = target.Execute();
            Assert.IsFalse(result);

            target.String2 = String.Empty;
            result = target.Execute();
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TextStringSplitWithoutSelectedIndexTest()
        {
            var input = "The  quick  brown  fox  jumped  over  the  lazy  dog.";
            var separator = " ";

            string[] expected = input.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);

            TextString target = new TextString();
            target.String1 = input;
            target.String2 = separator;
            target.TaskAction = "Split";
            target.StartIndex = -1;
            target.BuildEngine = new MockBuildEngine();

            bool result = target.Execute();
            Assert.IsTrue(result);
            Assert.IsNotNull(target.Strings);
            Assert.AreEqual(expected.Length, target.Strings.Length);
            Assert.AreEqual(0, expected.Except(target.Strings.Select(x => x.ItemSpec)).Count());
            Assert.IsNull(target.NewString);
        }

        [TestMethod]
        public void TextStringSplitWithSelectedIndexTest()
        {
            var input = "The  quick  brown  fox  jumped  over  the  lazy  dog.";
            var separator = " ";

            string[] expected = input.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);

            TextString target = new TextString();
            target.String1 = input;
            target.String2 = separator;
            target.TaskAction = "Split";
            target.StartIndex = 2;
            target.BuildEngine = new MockBuildEngine();

            bool result = target.Execute();
            Assert.IsTrue(result);
            Assert.IsNotNull(target.Strings);
            Assert.AreEqual(expected.Length, target.Strings.Length);
            Assert.AreEqual(0, expected.Except(target.Strings.Select(x => x.ItemSpec)).Count());
            Assert.AreEqual(expected[target.StartIndex], target.NewString);
        }
        #endregion

        #region Test Support Methods and Properties
        #endregion
    }
}

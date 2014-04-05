//-----------------------------------------------------------------------
// <copyright file="AssemblyInfoTests.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework.Tests
{
    using System;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    [TestClass]
    public class AssemblyInfoTests
    {
        [TestMethod]
        public void Can_update_attribute()
        {
            string tempFile = System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "AssemblyInfo_normal_spacing.Temp.cs");
            try
            {
                System.IO.File.Copy(System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "AssemblyInfo_normal_spacing.cs"), tempFile, overwrite: true);
                var assemblyInfoTask = new AssemblyInfo
                {
                    BuildEngine = new MockBuildEngine(),
                    AssemblyCompany = "Foo Bar Ltd.",
                    AssemblyInfoFiles = new ITaskItem[] { new TaskItem(tempFile), }
                };

                Assert.IsTrue(assemblyInfoTask.Execute());
            }
            finally
            {
                System.IO.File.Delete(tempFile);                
            }
        }

        [TestMethod]
        public void Can_update_attribute_when_spaces_appear_after_assembly_keyword()
        {
            string tempFile = System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "AssemblyInfo_mixed_spacing.Temp.cs");
            try
            {
                System.IO.File.Copy(System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "AssemblyInfo_mixed_spacing.cs"), tempFile, overwrite: true);
                var assemblyInfoTask = new AssemblyInfo
                {
                    BuildEngine = new MockBuildEngine(),
                    AssemblyCompany = "Foo Bar Ltd.",
                    AssemblyInfoFiles = new ITaskItem[] { new TaskItem(tempFile), }
                };

                Assert.IsTrue(assemblyInfoTask.Execute());
            }
            finally
            {
                System.IO.File.Delete(tempFile);                
            }
        }    

        [TestMethod]
        public void Can_update_attribute_when_single_quotes_appear_in_attribute_constructor()
        {
            string tempFile = System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "AssemblyInfo_mixed_spacing.Temp.cs");
            try
            {
                System.IO.File.Copy(System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "AssemblyInfo_mixed_spacing.cs"), tempFile, overwrite: true);
                var assemblyInfoTask = new AssemblyInfo
                {
                    BuildEngine = new MockBuildEngine(),
                    AssemblyDescription = "Foo Bar Description.",
                    AssemblyInfoFiles = new ITaskItem[] { new TaskItem(tempFile), }
                };

                Assert.IsTrue(assemblyInfoTask.Execute());
            }
            finally
            {
                System.IO.File.Delete(tempFile);                
            }
        }
    }
}
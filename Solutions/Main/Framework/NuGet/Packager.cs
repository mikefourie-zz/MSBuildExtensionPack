//-----------------------------------------------------------------------
// <copyright file="Packager.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.NuGet
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using Microsoft.Build.Framework;
    
    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Pack</i> (<b>Required: </b> Id, Version, Authors, Description, LibFiles, <b>Optional:</b> LicenseUrl, ProjectUrl, Title, ContentFiles, ToolsFiles, Owners, ReleaseNotes, CopyrightsText, IconUrl, RequireLicenseAgreement, Tags, Dependencies, References,  FrameworkAssemblies)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///    <ItemGroup>
    ///      <LibraryFiles Include="C:\Work\Community\MSBuildExtensionPack\Samples\Files\DotNet40\*.*">
    ///        <Framework>net40</Framework>
    ///      </LibraryFiles>
    ///      <LibraryFiles Include="C:\Work\Community\MSBuildExtensionPack\Samples\Files\DotNet35\*.*">
    ///        <Framework>net35</Framework>
    ///      </LibraryFiles>
    ///      <LibraryFiles Include="C:\Work\Community\MSBuildExtensionPack\Samples\Files\*.*" />
    ///    </ItemGroup>
    ///    <ItemGroup>
    ///      <Dependencies Include="log4net">
    ///        <Framework>net40</Framework>
    ///        <Version>1.2.10</Version>
    ///      </Dependencies>
    ///    </ItemGroup>
    ///    <ItemGroup>
    ///      <FrameworkAssemblies Include="System.Data.Entity">
    ///        <Framework>net40</Framework>
    ///      </FrameworkAssemblies>
    ///      <FrameworkAssemblies Include="System.ComponentModel.DataAnnotations">
    ///        <Framework>net40</Framework>
    ///      </FrameworkAssemblies>
    ///    </ItemGroup>
    ///    <Target Name="Default">
    ///     <Packager TaskAction="Pack" Id="MSBuildExtensionPack" Description="MSBuildExtensionPack Sample NuGet Package" ProjectUrl="http:///www.nuget.org" LicenseUrl="http:///www.nuget.org" LibraryFiles="@(LibraryFiles)" OutputFile="MSBE.nupkg" Authors="Hamid Shahid" Owners="Hamid Shahid" Dependencies="@(Dependencies)" Version="1.0.0.0"/>
    ///    </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    public class Packager : BaseTask
    {
        private const string PackTaskAction = "pack";

        /// <summary>
        /// Initializes a new instance of the Packager class.
        /// </summary>
        public Packager()
        {
            this.RequiresExplicitLicensing = false;
        }

        /// <summary>
        /// Gets or sets the Id of the NuGet package.
        /// is the package name that is shown when packages are listed using the Package Manager Console. 
        /// These are also used when installing a package using the Install-Package command within the Package Manager Console. 
        /// Package IDs may not contain any spaces or characters that are invalid in an URL.
        /// </summary>
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the version of the package. The version of the package, in a format like 1.2.3.
        /// </summary>
        [Required]        
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the Title text of the NuGet package. The human-friendly title of the package displayed in the Manage NuGet Packages dialog.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the RequireLicense property which determines whether the NuGet package requires explicit license permissions or not.
        /// Default is false.
        /// </summary>
        public bool RequiresExplicitLicensing { get; set; }

        /// <summary>
        /// Gets or sets a comma-separated list of authors of the package code.
        /// </summary>
        [Required]
        public string Authors { get; set; }

        /// <summary>
        /// Gets or sets a comma-separated list of the package creators.
        /// </summary>
        public string Owners { get; set; }

        /// <summary>
        /// Gets or sets a link to the license that the package is under.
        /// </summary>
        public string LicenseUrl { get; set; }

        /// <summary>
        /// Gets or sets a URL for the image to use as the icon for the package in the Manage NuGet Packages dialog box.
        /// </summary>
        public string IconUrl { get; set; }

        /// <summary>
        /// Gets or sets a URL for the home page of the package.
        /// </summary>
        public string ProjectUrl { get; set; }

        /// <summary>
        /// Gets or sets the long description of the package.
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the Release Notes of the NuGet package.
        /// This field only shows up when the package is an update to a previously installed package. 
        /// It is displayed where the Description would normally be displayed.
        /// </summary>
        public string ReleaseNotes { get; set; }

        /// <summary>
        /// Gets or sets the Copyright text of the NuGet package.
        /// </summary>
        public string CopyrightText { get; set; }

        /// <summary>
        /// Gets or sets the tags for the NuGet package. It should be a space-delimited list of tags.
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        /// Gets or sets the summary, a short description of the package.
        /// If specified, this shows up in the middle pane of the Add Package Dialog. 
        /// If not specified, a truncated version of the description is used instead.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the locale ID for the package, such as en-us.
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the directory containing the command line tool, NuGet.exe.
        /// If none is specified, will default to Resources directory of the currently executing assembly.
        /// </summary>
        public string NuGetExeDir { get; set; }

        /// <summary>
        /// Gets or sets the NuGet Output file
        /// </summary>
        [Required]
        public string OutputFile { get; set; }

        /// <summary>
        /// Gets or sets the Dependencies of the NuGet package. 
        /// An Example of ItemGroup passed to this property
        /// <ItemGroup>
        ///     <Dependencies Include="log4net">
        ///         <Framework>net40</Framework>
        ///         <Version>1.2.10</Version>
        ///     </Dependencies>
        /// </ItemGroup>     
        /// </summary>
        public ITaskItem[] Dependencies { get; set; }

        /// <summary>
        /// Gets or sets the References files or the files which are added as reference by default. The collection should only contain the names of the files and not the path.        
        /// </summary>
        /// <example>
        /// <code lang="xml"><![CDATA[
        /// <references>
        ///     <reference file="xunit.dll" />
        ///     <reference file="xunit.extensions.dll" />
        /// </references>
        /// ]]></code>    
        /// </example>
        public ITaskItem[] References { get; set; }

        /// <summary>
        /// Gets or sets the Files to be included in the lib folder of the package. Assemblies (.dll files) in the lib folder are added as assembly references when the package is installed.
        /// </summary>
        /// <example>
        /// <code lang="xml"><![CDATA[
        /// <ItemGroup>
        ///     <LibraryFiles Include="..\Community\MSBuildExtensionPack\Samples\Files\DotNet40\*.*">
        ///         <Framework>net40</Framework>
        ///     </LibraryFiles>
        ///     <LibraryFiles Include="..\Community\MSBuildExtensionPack\Samples\Files\DotNet35\*.*">
        ///         <Framework>net35</Framework>
        ///     </LibraryFiles>
        ///     <LibraryFiles Include="..\Community\MSBuildExtensionPack\Samples\Files\*.*" />
        /// </ItemGroup>
        /// ]]></code>    
        /// </example>
        public ITaskItem[] LibraryFiles { get; set; }

        /// <summary>
        /// Gets or sets the Files to be included in the contents folder of the package. Files in the content folder are copied to the root of your application when the package is installed.
        /// </summary>
        /// <example>
        /// <code lang="xml"><![CDATA[
        /// <ItemGroup>
        ///     <Content Include="..\Community\MSBuildExtensionPack\Samples\Files\readme.txt">
        ///         <Framework>net40</Framework>
        ///     </Content>
        ///     <Content Include="..\Community\MSBuildExtensionPack\Samples\Files\readme35.txt">
        ///         <Framework>net35</Framework>
        ///     </Content>
        ///     <Content Include="..\Community\MSBuildExtensionPack\Samples\Files\*.txt" />
        /// </ItemGroup>
        /// ]]></code>    
        /// </example>
        public ITaskItem[] ContentFiles { get; set; }

        /// <summary>
        /// Gets or sets the Files to be included in the tools folder of the package. The tools folder of a package is for powershell scripts and programs accessible from the Package Manager Console.
        /// </summary>
        /// <example>
        /// <code lang="xml"><![CDATA[
        /// <ItemGroup>
        ///     <Tools Include="..\Community\MSBuildExtensionPack\Samples\Files\init.ps1">
        ///         <Framework>net40</Framework>
        ///     </Tools>
        ///     <Tools Include="..\Community\MSBuildExtensionPack\Samples\Files\init35.ps1">
        ///         <Framework>net35</Framework>
        ///     </Tools>
        ///     <Tools Include="..\Community\MSBuildExtensionPack\Samples\Files\*.ps1" />
        /// </ItemGroup>
        /// ]]></code>    
        /// </example>
        public ITaskItem[] ToolsFiles { get; set; }

        /// <summary>
        /// Gets or sets the Framework Assemblies that the deployed package is dependant upon.
        /// </summary>
        /// <example>
        /// <ItemGroup>
        ///     <FrameworkAssemblies Include="System.Data.Entity">
        ///         <Framework>net45</Framework>
        ///     </FrameworkAssemblies>
        ///     <FrameworkAssemblies Include="System.ComponentModel.DataAnnotations">
        ///         <Framework>net40</Framework>
        ///     </FrameworkAssemblies>
        /// </ItemGroup>
        /// </example>
        public ITaskItem[] FrameworkAssemblies { get; set; }

        /// <summary>   
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction.ToLower(CultureInfo.CurrentCulture))
            {
                case PackTaskAction:
                    this.Pack();
                    break;

                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid Task Action passed: {0}", this.TaskAction));
                    return;
            }
        }

        /// <summary>
        /// Validates whether the given version number is the correct format or not
        /// </summary>
        /// <param name="version">The version number</param>
        /// <returns>True if the version number is valid. False otherwise</returns>
        private static bool IsValidVersionNumber(string version)
        {
            Regex regex = new Regex(@"\d+(?:\.\d+)+");
            return regex.Match(version).Success;
        }

        /// <summary>
        /// Generates the dependency Xml element for NuGet specification
        /// </summary>
        /// <param name="defaultNamespace">The xml namespace</param>
        /// <param name="taskItem">The Task item read from the project file</param>
        /// <returns>Dependency XElement</returns>
        private static XElement GenerateDependencyElement(XNamespace defaultNamespace, ITaskItem taskItem)
        {
            var dependency = new XElement(defaultNamespace + "dependency");
            dependency.Add(new XAttribute("id", taskItem.ItemSpec));
            var version = taskItem.GetMetadata("version");
            if (!string.IsNullOrWhiteSpace(version))
            {
                if (!IsValidVersionNumber(version))
                {
                    throw new Exception(string.Format(CultureInfo.CurrentCulture, "Invalid version {0} specified for dependency {1}", version, taskItem.ItemSpec));
                }

                dependency.Add(new XAttribute("version", version));
            }

            return dependency;
        }

        /// <summary>
        /// Generates the Framework Xml element for NuGet specification
        /// </summary>
        /// <param name="defaultNamespace">The xml namespace</param>
        /// <param name="taskItem">The Task item read from the project file</param>
        /// <returns>Framework XElement</returns>
        private static XElement GenerateFrameworkAssemblyXElement(XNamespace defaultNamespace, ITaskItem taskItem)
        {
            var frameworkAssembly = new XElement(defaultNamespace + "frameworkAssembly "); 
            frameworkAssembly.Add(new XAttribute("assemblyName", taskItem.ItemSpec));
            if (!string.IsNullOrWhiteSpace(taskItem.GetMetadata("version")))
            {
                frameworkAssembly.Add(new XAttribute("version", taskItem.GetMetadata("version")));
            }

            return frameworkAssembly;
        }

        /// <summary>
        /// The method populates the lib folder in the nuget package.
        /// </summary>
        /// <param name="folderName">The name of the folder to be created.</param>
        /// <param name="packageDirectoryPath">The directory path where the nuget specification message is created.</param>
        /// <param name="items">The item group to read the files.</param>
        private static void PopulateFolder(string folderName, string packageDirectoryPath, IEnumerable<ITaskItem> items)
        {
            if (items == null)
            {
                return;
            }

            var libDirectory = Directory.CreateDirectory(Path.Combine(packageDirectoryPath, folderName));
            foreach (var item in items)
            {
                var framework = item.GetMetadata("framework");
                if (!string.IsNullOrWhiteSpace(framework))
                {
                    framework = Path.Combine(libDirectory.FullName, framework);
                    if (!Directory.Exists(framework))
                    {
                        Directory.CreateDirectory(framework);
                    }

                    File.Copy(item.ItemSpec, Path.Combine(framework, Path.GetFileName(item.ItemSpec)));
                }
                else
                {
                    File.Copy(item.ItemSpec, Path.Combine(libDirectory.FullName, Path.GetFileName(item.ItemSpec)));
                }
            }
        }

        /// <summary>
        /// Generate NuGet specification, arrange files in folder structures and prepares the package.
        /// </summary>
        private void Pack()
        {
            if (!IsValidVersionNumber(this.Version))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid version number {0}. Examples of valid version numbers are 1.0, 1.2.3. 2.3.1909.7", this.Version));
                return;
            }

            string nugetDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(nugetDirectory);
            try
            {
                var nugetspecification = this.GenerateSpecification(nugetDirectory);
                PopulateFolder("lib", nugetDirectory, this.LibraryFiles);
                PopulateFolder("content", nugetDirectory, this.ContentFiles);
                PopulateFolder("tools", nugetDirectory, this.ToolsFiles);
                this.PreparePackage(nugetspecification);
            }
            finally
            {
                Directory.Delete(nugetDirectory, true);
            }
        }

        /// <summary>
        /// Generates the NuGet Package
        /// </summary>
        /// <param name="nugetSpecificationFile">The xml file containing NuGet package specification</param>
        private void PreparePackage(string nugetSpecificationFile)
        {
            string executionDirectory = Path.GetDirectoryName(nugetSpecificationFile);

            // Default to Resources directory so behavior is consistent with previous versions (when NuGetExeDir is not specified).
            this.NuGetExeDir = this.NuGetExeDir != null ? this.NuGetExeDir : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Resources\");
            string nugetFilePath = Path.Combine(this.NuGetExeDir, "NuGet.exe");

            var processStartInfo = new ProcessStartInfo()
            {
                Arguments = "pack " + nugetSpecificationFile,
                WorkingDirectory = executionDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                FileName = nugetFilePath
            };

            using (var process = Process.Start(processStartInfo))
            {
                process.WaitForExit(20000);
                if (process.ExitCode != 0)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Nuget Package could not be created. Exit Code: {0}.", process.ExitCode));
                }
                else
                {
                    var files = Directory.GetFiles(executionDirectory, this.Id + "." + this.Version + "*.nupkg");
                    if (files.Any())
                    {
                        File.Copy(files[0], this.OutputFile, true);
                        this.LogTaskMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "NuGet Package {0} created successfully.", this.OutputFile));
                    }
                }
            }
        }

        /// <summary>
        /// The method generates a valid Nuget specification file in the given directory
        /// </summary>
        /// <param name="directoryPath">The directory path where the nuget specification message is created.</param>                
        /// <returns>The complete path of the nuget specification file.</returns>
        private string GenerateSpecification(string directoryPath)
        {
            var specFileName = Path.Combine(directoryPath, this.Id + ".nuspec");
            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Generating NuGet specification file {0}", specFileName));
            
            XNamespace defaultNamespace = "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd";
            var specXml = new XElement(defaultNamespace + "package");
            specXml.Add(
                new XElement(
                    defaultNamespace + "metadata",
                    new XElement(defaultNamespace + "id", this.Id),
                    new XElement(defaultNamespace + "version", this.Version),
                    new XElement(defaultNamespace + "title", this.Title),
                    new XElement(defaultNamespace + "authors", this.Authors),
                    new XElement(defaultNamespace + "owners", this.Owners),
                    new XElement(defaultNamespace + "description", this.Description),
                    new XElement(defaultNamespace + "releaseNotes", this.ReleaseNotes),
                    new XElement(defaultNamespace + "summary", this.Summary),
                    new XElement(defaultNamespace + "language", this.Language),
                    new XElement(defaultNamespace + "projectUrl", this.ProjectUrl),
                    new XElement(defaultNamespace + "iconUrl", this.IconUrl),
                    new XElement(defaultNamespace + "licenseUrl", this.LicenseUrl),
                    new XElement(defaultNamespace + "copyright", this.CopyrightText),
                    new XElement(defaultNamespace + "requireLicenseAcceptance", this.RequiresExplicitLicensing.ToString().ToLower(CultureInfo.CurrentCulture)),
                    new XElement(defaultNamespace + "tags", this.Tags)));

            if (this.Dependencies != null)
            { 
                specXml.Element(defaultNamespace + "metadata").Add(new XElement(
                            defaultNamespace + "dependencies",
                            from dependency in this.Dependencies.Select(e => GenerateDependencyElement(defaultNamespace, e)) select dependency));
            }

            if (this.References != null)
            {
                specXml.Element(defaultNamespace + "metadata").Add(new XElement(
                                            defaultNamespace + "references",
                                            from reference in this.References.Select(e => { var reference = new XElement(defaultNamespace + "reference"); reference.Add(new XAttribute("file", e.ItemSpec)); return reference; }) select reference));
            }

            if (this.FrameworkAssemblies != null)
            {
                specXml.Element(defaultNamespace + "metadata").Add(new XElement(
                                            defaultNamespace + "frameworkAssemblies",
                                            from frameworkAssemblies in this.FrameworkAssemblies.Select(e => GenerateFrameworkAssemblyXElement(defaultNamespace, e)) select frameworkAssemblies));
            }

            specXml.Save(specFileName);
            return specFileName;
        }
    }
}

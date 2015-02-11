//-----------------------------------------------------------------------
// <copyright file="ILMerge.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task wraps ILMerge.
    /// <para/> This has been tested using ILMerge v2.10.0526
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <ItemGroup>
    ///             <Input Include="C:\b\MSBuild.ExtensionPack.dll"/>
    ///             <Input Include="C:\b\Ionic.Zip.dll"/>
    ///         </ItemGroup>
    ///         <MSBuild.ExtensionPack.Framework.ILMerge InputAssemblies="@(Input)" OutputFile="C:\a\MyNewAssembly.dll"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class ILMerge : ToolTask
    {
        private int fileAlignment = 512;
        private bool publicKeyTokens = true;

        /// <summary>
        /// AllowDuplicateResources
        /// </summary>
        public bool AllowDuplicateResources { get; set; }

        /// <summary>
        /// Allows the user to either allow all public types to be renamed when they are duplicates, or to specify it for arbitrary type names
        /// <para/>Command line option: [/allowDup[:typeName]]*
        /// <para/>Default: no duplicates of public types allowed.
        /// </summary>
        public ITaskItem[] AllowDuplicateTypes { get; set; }

        /// <summary>
        /// If set, any assembly-level attributes names that have the same type are copied over into the target directory as long as the definition of the attribute type specifies that “AllowMultiple” is true.
        /// <para/>Command line option: /allowMultiple
        /// <para/>Default: false
        /// </summary>
        public bool AllowMultipleAssemblyLevelAttributes { get; set; }

        /// <summary>
        /// When this is set before calling Merge, then if an assembly's PeKind flag (this is the value of the field listed as .corflags in the Manifest) is zero it will be treated as if it was ILonly.
        /// <para/>This can be used to allow C++ assemblies to be merged; it does not appear that the C++ compiler writes the value as ILonly.
        /// <para/>However, if such an assembly has any non-IL features, then they will probably not be copied over into the target assembly correctly.
        /// <para/>So please use this option with caution.
        /// <para/>Command line option: /zeroPeKind
        /// <para/>Default: false
        /// </summary>
        public bool AllowZeroPeKind { get; set; }

        /// <summary>
        /// If this is set before calling Merge, then it specifies the path and filename to an atttribute assembly, an assembly that will be used to get all of the assembly-level attributes such as Culture, Version, etc.
        /// <para/>It will also be used to get the Win32 Resources from. It is mutually exclusive with the CopyAttributes property (Section 2.7).
        /// <para/>When it is not specified, then the Win32 Resources from the primary assembly are copied over into the target assembly.
        /// <para/>If it is not a full path, then the current directory is used.
        /// <para/>Command line option: /attr:filename
        /// <para/>Default: null
        /// </summary>
        public ITaskItem AttributeFile { get; set; }

        /// <summary>
        /// When this is set before calling Merge, then the "transitive closure" of the input assemblies is computed and added to the list of input assemblies.
        /// <para/>An assembly is considered part of the transitive closure if it is referenced, either directly or indirectly, from one of the originally
        /// <para/> specified input assemblies and it has an external reference to one of the input assemblies, or one of the assemblies that has such a reference.
        /// <para/>Command line option: /closed
        /// <para/>Default: false
        /// </summary>
        public bool Closed { get; set; }

        /// <summary>
        /// When this is set before calling Merge, then the assembly level attributes of each input assembly are copied over into the target assembly.
        /// <para/>Any duplicate attribute overwrites a previously copied attribute. If you want to allow duplicates (for those attributes whose type specifies “AllowMultiple” in their definition), then you can also set the AllowMultipleAssemblyLevelAttributes.
        /// <para/>The input assemblies are processed in the order they are specified. This option is mutually exclusive with specifying an attribute assembly, i.e., the property AttributeFile.
        /// <para/>When an attribute assembly is specified, then no assembly-level attributes are copied over from the input assemblies
        /// <para/>Command line option: /copyattrs
        /// <para/>Default: false
        /// </summary>
        public bool CopyAttributes { get; set; }

        /// <summary>
        /// When this is set to true, ILMerge creates a .pdb file for the output assembly and merges into it any .pdb files found for input assemblies.
        /// <para/>If you do not want a .pdb file created for the output assembly, either set this property to false or else specify the /ndebug option at the command line.
        /// <para/>Command line option: /ndebug
        /// <para/>Default: true
        /// </summary>
        public bool DebugInfo { get; set; }

        /// <summary>
        /// When this is set before calling Merge, then the target assembly will be delay signed. This can be set only in conjunction with the /keyfile option (Section 2.13).
        /// <para/>Command line option: /delaysign
        /// <para/>Default: null
        /// </summary>
        public bool DelaySign { get; set; }

        /// <summary>
        /// This property is used only in conjunction with the Internalize property (Section 2.12). When this is set before calling Merge, it indicates 
        /// <para/>the path and filename that will be used to identify types that are not to have their visibility modified.
        /// <para/>If Internalize is true, but ExcludeFile is "", then all types in any assembly other than the primary assembly are made non-public.
        /// <para/>Setting this property implicitly sets Internalize to true. The contents of the file should be one regular expression per line.
        /// <para/>The syntax is that defined in the .NET namespace System.Text.RegularExpressions for regular expressions.
        /// <para/>The regular expressions are matched against each type's full name, e.g., "System.Collections.IList".
        /// <para/>If the match fails, it is tried again with the assembly name (surrounded by square brackets) prepended to the type name.
        /// <para/>Thus, the pattern “\[A\].*” excludes all types in assembly A from being made non-public. (The backslashes are required because the string is treated as a regular expression.)
        /// <para/>The pattern “N.T” will match all types named T in the namespace named N no matter what assembly they are defined in.
        /// <para/> It is important to note that the regular expressions are not anchored to the beginning of the string; if this is desired, use the appropriate regular expression operator characters to do so.
        /// <para/>Command line option: /internalize[:excludeFile]
        /// <para/>Default: null
        /// </summary>
        public ITaskItem ExcludeFile { get; set; }

        /// <summary>
        /// Sets the input assemblies to merge.
        /// </summary>
        [Required]
        public ITaskItem[] InputAssemblies { get; set; }

        /// <summary>
        /// Set to true to delete the InputAssemblies after the merged file has been created. Default is false;
        /// </summary>
        public bool DeleteInputAssemblies { get; set; }

        /// <summary>
        /// This controls whether types in assemblies other than the primary assembly have their visibility modified. When it is true, then all non-exempt types that are visible outside of their assembly 
        /// <para/>have their visibility modified so that they are not visible from outside of the merged assembly. A type is exempt if its full name matches a line from the ExcludeFile (Section 2.10) using the .NET regular expression engine.
        /// <para/>Command line option: /internalize[:excludeFile]
        /// <para/>Default: false
        /// </summary>
        public bool Internalize { get; set; }

        /// <summary>
        /// This controls the file alignment used for the target assembly. The setter sets the value to the largest power of two that is no larger than the supplied argument, and is at least 512. 
        /// <para/>Command line option: /align:n
        /// <para/>Default: 512
        /// </summary>
        public int FileAlignment
        {
            get { return this.fileAlignment; }
            set { this.fileAlignment = value; }
        }

        /// <summary>
        /// When this is set before calling Merge, it specifies the path and filename to a .snk file. The target assembly will be signed with its contents and will 
        /// <para/>then have a strong name. It can be used with the DelaySign property (Section 2.9) to have the target assembly delay signed. 
        /// <para/>This can be done even if the primary assembly was fully signed.
        /// <para/>Command line option: /keyfile:filename
        /// <para/>Default: null
        /// </summary>
        public ITaskItem KeyFile { get; set; }

        /// <summary>
        /// When this is set before calling Merge, then log messages are written. It is used in conjunction with the LogFile property.
        /// <para/>If Log is true, but LogFile is null, then log messages are written to Console.Out. To specify this behavior on the command line, the option "/log" can be given without a log file.
        /// <para/>Command line option: /log[:logfile]
        /// <para/>Default: false
        /// </summary>
        public bool LogMessages { get; set; }

        /// <summary>
        /// When this is set before calling Merge, it indicates the path and filename that log messages are written to. If LogMessages is true, but LogFile is null, then log messages are written to Console.Out.
        /// <para/>Command line option: /log[:logfile]
        /// <para/>Default: null
        /// </summary>
        public ITaskItem LogFile { get; set; }

        /// <summary>
        /// This must be set before calling Merge. It specifies the path and filename that the target assembly will be written to.
        /// <para/>Command line option: /out:filename
        /// <para/>Default: null
        /// </summary>
        [Required]
        public ITaskItem OutputFile { get; set; }

        /// <summary>
        /// This must be set before calling Merge. It indicates whether external assembly references in the manifest of the target assembly will use full public keys (false) or public key tokens (true).
        /// <para/>Command line option: /out:filename
        /// <para/>Default: true
        /// </summary>
        public bool PublicKeyTokens
        {
            get { return this.publicKeyTokens; }
            set { this.publicKeyTokens = value; }
        }

        /// <summary>
        /// This method sets the .NET Framework for the target assembly to be the one specified by platform. Valid strings for the first argument are "v1", "v1.1", "v2", and "v4". 
        /// <para/>The "v" is case insensitive and is also optional. This way ILMerge can be used to "cross-compile", i.e., it can run in one version of the framework and generate 
        /// <para/>the target assembly so it will run under a different assembly. The second argument is the directory in which mscorlib.dll is to be found.
        /// <para/>Command line option: /targetplatform:version,platformdirectory
        /// <para/>Default: null
        /// </summary>
        public string TargetPlatformVersion { get; set; }

        /// <summary>
        /// This method sets the .NET Framework for the target assembly to be the one specified by platform. Valid strings for the first argument are "v1", "v1.1", "v2", and "v4". 
        /// <para/>The "v" is case insensitive and is also optional. This way ILMerge can be used to "cross-compile", i.e., it can run in one version of the framework and generate 
        /// <para/>the target assembly so it will run under a different assembly. The second argument is the directory in which mscorlib.dll is to be found.
        /// <para/>Command line option: /targetplatform:version,platformdirectory
        /// <para/>Default: null
        /// </summary>
        public ITaskItem TargetPlatformDirectory { get; set; }

        /// <summary>
        /// Once merging is complete, this property is true if and only if the primary assembly had a strong name, but the target assembly does not. 
        /// <para/>This can occur when an .snk file is not specified, or if something goes wrong trying to read its contents.
        /// </summary>
        [Output]
        public bool StrongNameLost { get; set; }

        /// <summary>
        /// This controls whether the target assembly is created as a library, a console application or as a Windows application. When it is not specified, then the target 
        /// <para/>assembly will be the same kind as that of the primary assembly. (In that case, the file extensions found on the specified target assembly and the primary 
        /// <para/>assembly must match.) When it is specified, then the file extension of the target assembly must match the specification. The possible values are ILMerge.Kind.{Dll, Exe, WinExe}
        /// <para/>Command line option: /target:(library|exe|winexe)
        /// <para/>Default: ILMerge.Kind.SameAsPrimaryAssembly
        /// </summary>
        public string TargetKind { get; set; }

        /// <summary>
        /// When this is true, then types with the same name are all merged into a single type in the target assembly. The single type is the union of all of the individual 
        /// <para/>types in the input assemblies: it contains all of the members from each of the corresponding types in the input assemblies. It cannot be specified at the same time as /allowDup.
        /// <para/>Command line option: /union
        /// <para/>Default: false
        /// </summary>
        public bool UnionMerge { get; set; }

        /// <summary>
        /// When this has a non-null value, then the target assembly will be given its value as the version number of the assembly. When specified on the command line, the 
        /// <para/>version is read in as a string and should look like "6.2.1.3" (but without the quote marks). The version must be a valid assembly version as defined by the attribute AssemblyVersion in the System.Reflection namespace.
        /// <para/>Command line option: /ver:version
        /// <para/>Default: null
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// This property controls whether XML documentation files are merged to produce an XML documentation file for the target assembly.
        /// <para/>Command line option: /xmldocs
        /// <para/>Default: false
        /// </summary>
        public bool XmlDocs { get; set; }

        /// <summary>
        /// Sets the directories to be used to search for input assemblies. Each item should contain a directory name.
        /// <para/>Command line option: /lib:directory
        /// </summary>
        public ITaskItem[] SearchDirectories { get; set; }

        protected override string ToolName
        {
            get { return "ILMerge.exe"; }
        }

        protected override string GenerateFullPathToTool()
        {
            return string.IsNullOrEmpty(this.ToolPath) ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft\ILMerge\" + this.ToolName) : System.IO.Path.Combine(this.ToolPath, this.ToolName);
        }

        protected override string GenerateCommandLineCommands()
        {
            CommandLineBuilder builder = new CommandLineBuilder();

            if (this.AllowDuplicateResources)
            {
                builder.AppendSwitch(@"/allowDuplicateResources");
            }

            if (this.AllowDuplicateTypes != null)
            {
                foreach (ITaskItem allowDuplicateType in this.AllowDuplicateTypes)
                {
                    builder.AppendSwitch(@"/allowDup:" + allowDuplicateType.ItemSpec);
                }
            }

            if (this.AllowZeroPeKind)
            {
                builder.AppendSwitch(@"/zeroPeKind");
            }

            if (this.AttributeFile != null)
            {
                builder.AppendSwitch(string.Format(CultureInfo.CurrentCulture, "/attr:\"{0}\"", this.AttributeFile.ItemSpec));
            }

            if (this.Closed)
            {
                builder.AppendSwitch(@"/closed");
            }

            if (this.CopyAttributes)
            {
                builder.AppendSwitch(@"/copyattrs");
            }

            if (!this.DebugInfo)
            {
                builder.AppendSwitch(@"/ndebug");
            }

            if (this.DelaySign)
            {
                builder.AppendSwitch(@"/delaysign");
            }

            if (this.ExcludeFile != null)
            {
                builder.AppendSwitch(string.Format(CultureInfo.CurrentCulture, "/internalize:\"{0}\"", this.ExcludeFile.ItemSpec));
            }
            else if (this.Internalize)
            {
                builder.AppendSwitch(@"/internalize");
            }

            if (this.KeyFile != null)
            {
                builder.AppendSwitch(string.Format(CultureInfo.CurrentCulture, "/keyfile:\"{0}\"", this.KeyFile.ItemSpec));
            }

            if (this.LogFile != null)
            {
                builder.AppendSwitch(string.Format(CultureInfo.CurrentCulture, "/log:\"{0}\"", this.LogFile.ItemSpec));
            }

            if (this.PublicKeyTokens)
            {
                builder.AppendSwitch(@"/useFullPublicKeyForReferences");
            }

            if (this.SearchDirectories != null)
            {
                foreach (ITaskItem searchDirectory in this.SearchDirectories)
                {
                    builder.AppendSwitch(@"/lib:" + searchDirectory.ItemSpec);
                }
            }

            if (this.TargetPlatformVersion != null)
            {
                builder.AppendSwitch(@"/targetplatform:" + this.TargetPlatformVersion + @"," + this.TargetPlatformDirectory.ItemSpec);
            }

            if (this.TargetKind != null)
            {
                builder.AppendSwitch(@"/target:" + this.TargetKind);
            }

            if (this.UnionMerge)
            {
                builder.AppendSwitch(@"/union");
            }

            if (this.Version != null)
            {
                builder.AppendSwitch(@"/ver:" + this.Version);
            }

            if (this.XmlDocs)
            {
                builder.AppendSwitch(@"/xmldocs");
            }

            builder.AppendSwitch(@"/align:" + this.FileAlignment);
            builder.AppendSwitch(string.Format(CultureInfo.CurrentCulture, "/out:\"{0}\"", this.OutputFile.ItemSpec));
            builder.AppendFileNamesIfNotNull(this.InputAssemblies, @" ");

            return builder.ToString();
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            Log.LogMessage("Running " + pathToTool + " " + commandLineCommands);
            int retVal = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

            if (this.DeleteInputAssemblies)
            {
                foreach (ITaskItem file in this.InputAssemblies.Where(file => File.Exists(file.ItemSpec)))
                {
                    File.Delete(file.ItemSpec);
                }
            }

            return retVal;
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            this.Log.LogMessage(MessageImportance.Normal, singleLine);
        }
    }
}
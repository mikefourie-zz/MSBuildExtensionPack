//-----------------------------------------------------------------------
// <copyright file="Guid.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System.Globalization;
    using System.Security.Cryptography;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Create</i> (<b>Output: </b> GuidString, FormattedGuidString)</para>
    /// <para><i>CreateCrypto</i> (<b>Output: </b> GuidString, FormattedGuidString)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <!-- Create a new Guid and get the formatted and unformatted values -->
    ///         <MSBuild.ExtensionPack.Framework.Guid TaskAction="Create">
    ///             <Output TaskParameter="FormattedGuidString" PropertyName="FormattedGuidString1" />
    ///             <Output TaskParameter="GuidString" PropertyName="GuidStringItem" />
    ///         </MSBuild.ExtensionPack.Framework.Guid>
    ///         <Message Text="GuidStringItem: $(GuidStringItem)"/>
    ///         <Message Text="FormattedGuidString: $(FormattedGuidString1)"/>
    ///         <!-- Create a new cryptographically strong Guid and get the formatted and unformatted values -->
    ///         <MSBuild.ExtensionPack.Framework.Guid TaskAction="CreateCrypto">
    ///             <Output TaskParameter="FormattedGuidString" PropertyName="FormattedGuidString1" />
    ///             <Output TaskParameter="GuidString" PropertyName="GuidStringItem" />
    ///         </MSBuild.ExtensionPack.Framework.Guid>
    ///         <Message Text="GuidStringItem Crypto: $(GuidStringItem)"/>
    ///         <Message Text="FormattedGuidString Crypto: $(FormattedGuidString1)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
	[HelpUrl("http://www.msbuildextensionpack.com/help/3.5.1.0/html/ceb07d01-751c-0c5a-09b3-fc712ace6f13.htm")]
    public class Guid : BaseTask
    {
        private const string CreateTaskAction = "Create";
        private const string CreateCryptoTaskAction = "CreateCrypto";
        
        private System.Guid internalGuid;

        [DropdownValue(CreateTaskAction)]
        [DropdownValue(CreateCryptoTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// 32 digits separated by hyphens: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
        /// </summary>
        [Output]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(CreateCryptoTaskAction, false)]
        public string[] FormattedGuidString
        {
            get { return new[] { this.internalGuid.ToString("D", CultureInfo.CurrentCulture) }; }
        }

        /// <summary>
        /// 32 digits: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
        /// </summary>
        [Output]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(CreateCryptoTaskAction, false)]
        public string[] GuidString
        {
            get { return new[] { this.internalGuid.ToString("N", CultureInfo.CurrentCulture) }; }
        }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            switch (this.TaskAction)
            {
                case "Create":
                    this.Get();
                    break;
                case "CreateCrypto":
                    this.GetCrypto();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        /// <summary>
        /// Gets this instance.
        /// </summary>
        private void Get()
        {
            this.LogTaskMessage("Getting random GUID");
            this.internalGuid = System.Guid.NewGuid();
        }

        /// <summary>
        /// Gets the crypto.
        /// </summary>
        private void GetCrypto()
        {
            this.LogTaskMessage("Getting Cryptographically Secure GUID");
            byte[] data = new byte[16];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(data);
            this.internalGuid = new System.Guid(data);
        }
    }
}
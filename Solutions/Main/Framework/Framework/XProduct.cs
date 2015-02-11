//-----------------------------------------------------------------------
// <copyright file="XProduct.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// The code was contributed by Matthias Koch
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task creates a cross product of up to 10 ItemGroups
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <ItemGroup>
    ///         <AllConfigurations Include="Release">
    ///             <Name>Release</Name>
    ///             <Framework>net-3.5</Framework>
    ///             <OutputDirectory>net-3.5\bin\release\</OutputDirectory>
    ///         </AllConfigurations>
    ///         <AllConfigurations Include="Debug">
    ///             <Name>Debug</Name>
    ///             <Framework>net-3.5</Framework>
    ///             <OutputDirectory>net-3.5\bin\debug\</OutputDirectory>
    ///         </AllConfigurations>
    ///         <AllPlatforms Include="x86">
    ///             <Use32Bit>True</Use32Bit>
    ///         </AllPlatforms>
    ///         <AllPlatforms Include="x64">
    ///             <Use32Bit>False</Use32Bit>
    ///         </AllPlatforms>
    ///         <AllDatabaseSystems Include="SqlServerLocal"  Condition="'true' == 'true'">
    ///             <DataSource>localhost\.</DataSource>
    ///             <DatabaseDirectory>C:\Databases\.</DatabaseDirectory>
    ///         </AllDatabaseSystems>
    ///         <AllDatabaseSystems Include="SqlServer2005">
    ///             <DataSource>localhost\MSSQL2005</DataSource>
    ///             <DatabaseDirectory>C:\Databases\MsSql2005</DatabaseDirectory>
    ///         </AllDatabaseSystems>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <MSBuild.ExtensionPack.Framework.XProduct IdentityFormat="{0}-{1}-{2}" Group1="@(AllConfigurations)" Group2="@(AllPlatforms)" Group3="@(AllDatabaseSystems)" >
    ///             <Output ItemName="NewList" TaskParameter="Result" />
    ///             <Output PropertyName="CountX" TaskParameter="Count" />
    ///         </MSBuild.ExtensionPack.Framework.XProduct>
    ///         <Message Text="Got $(CountX) configurations" />
    ///         <Message Text="%(NewList.Identity)
    ///                     %(NewList.Name)
    ///                     %(NewList.Framework)
    ///                     %(NewList.OutputDirectory)
    ///                     %(NewList.Use32Bit)
    ///                     %(NewList.DataSource)
    ///                     %(NewList.DataBaseDirectory)" />
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    public class XProduct : Task
    {
        /// <summary>
        /// The cross-product result output.
        /// </summary>
        [Output]
        public ITaskItem[] Result { get; set; }

        /// <summary>
        /// The number of items produced by the cross-product
        /// </summary>
        [Output]
        public int Count { get; set; }

        /// <summary>
        /// Specifies the format to use for the new ItemGroup names
        /// </summary>
        public string IdentityFormat { get; set; }

        /// <summary>
        /// Copies original Identity metadata to result item as well - suffixed by the group number, i.e. you can use <c>%(ResultList.Identity1)</c>.
        /// </summary>
        public bool AddOriginalIdentityUsingGroupNumberSuffix { get; set; }

        /// <summary>
        /// ItemGroup1
        /// </summary>
        public ITaskItem[] Group1 { get; set; }

        /// <summary>
        /// ItemGroup2
        /// </summary>
        public ITaskItem[] Group2 { get; set; }

        /// <summary>
        /// ItemGroup3
        /// </summary>
        public ITaskItem[] Group3 { get; set; }

        /// <summary>
        /// ItemGroup4
        /// </summary>
        public ITaskItem[] Group4 { get; set; }

        /// <summary>
        /// ItemGroup5
        /// </summary>
        public ITaskItem[] Group5 { get; set; }

        /// <summary>
        /// ItemGroup6
        /// </summary>
        public ITaskItem[] Group6 { get; set; }

        /// <summary>
        /// ItemGroup7
        /// </summary>
        public ITaskItem[] Group7 { get; set; }

        /// <summary>
        /// ItemGroup8
        /// </summary>
        public ITaskItem[] Group8 { get; set; }

        /// <summary>
        /// ItemGroup9
        /// </summary>
        public ITaskItem[] Group9 { get; set; }

        /// <summary>
        /// ItemGroup10
        /// </summary>
        public ITaskItem[] Group10 { get; set; }

        public override bool Execute()
        {
            var groups = this.CreateDataArrays().ToList();

            this.Result = new ITaskItem[] { new TaskItem() };
            for (var i = 0; i < groups.Count; ++i)
            {
                this.Result = DoXProduct(this.Result, groups[i], i + 1, this.AddOriginalIdentityUsingGroupNumberSuffix).ToArray();
            }

            this.Count = this.Result.Length;

            for (var i = 0; i < this.Result.Length; i++)
            {
                DoIdentity(this.Result[i], this.IdentityFormat ?? "{0}", i);
            }

            return !this.Log.HasLoggedErrors;
        }

        private static IEnumerable<ITaskItem> DoXProduct(IEnumerable<ITaskItem> group1, ITaskItem[] group2, int group2Number, bool addOriginalIdentityUsingGroupNumberSuffix)
        {
            foreach (var item1 in group1)
            {
                foreach (var item2 in group2)
                {
                    var newItem = new TaskItem(item1.ItemSpec + ";" + item2.ItemSpec);
                    item1.CopyMetadataTo(newItem);
                    item2.CopyMetadataTo(newItem);
                    if (addOriginalIdentityUsingGroupNumberSuffix)
                    {
                        newItem.SetMetadata("Identity" + group2Number, item2.ItemSpec);
                    }

                    yield return newItem;
                }
            }
        }

        private static void DoIdentity(ITaskItem item, string identityFormat, int number)
        {
            var replacements = item.ItemSpec.Split(';').Select((t, i) => new { Old = "{" + i + "}", New = t }).ToDictionary(x => x.Old, x => x.New);
            replacements["{0}"] = number.ToString(CultureInfo.InvariantCulture);

            var regex = new Regex(string.Join("|", replacements.Keys.Select(Regex.Escape)));
            item.ItemSpec = regex.Replace(identityFormat, m => replacements[m.Value]);
        }

        private IEnumerable<ITaskItem[]> CreateDataArrays()
        {
            var allProperties = typeof(XProduct).GetProperties();
            var dataProperties = Enumerable.Range(1, 10).Select(i => allProperties.SingleOrDefault(p => p.Name == "Group" + i)).TakeWhile(x => x != null);
            var datas = dataProperties.Select(p => p.GetValue(this, null)).Cast<ITaskItem[]>().TakeWhile(x => x != null).ToArray();
            return datas;
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="MsBuildHelper.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Escape</i> (<b>Required: </b> InputString <b>Output: </b> OutputString)</para>
    /// <para><i>FilterItems</i> (<b>Required: </b> InputItems1, RegexPattern <b>Optional:</b> Metadata <b>Output: </b> OutputItems)</para>
    /// <para><i>FilterItemsOnMetadata</i> (<b>Required: </b> InputItems1, InputItems2, Metadata <b>Optional: </b>Separator <b>Output: </b> OutputItems)</para>
    /// <para><i>GetCommonItems</i> (<b>Required: </b> InputItems1, InputItems2 <b>Output: </b> OutputItems, ItemCount)</para>
    /// <para><i>GetCurrentDirectory</i> (<b>Output: </b> CurrentDirectory)</para>
    /// <para><i>GetDistinctItems</i> (<b>Required: </b> InputItems1, InputItems2 <b>Output: </b> OutputItems, ItemCount)</para>
    /// <para><i>GetItem</i> (<b>Required: </b> InputItems1, Position<b>Output: </b> OutputItems)</para>
    /// <para><i>GetItemCount</i> (<b>Required: </b> InputItems1 <b>Output: </b> ItemCount)</para>
    /// <para><i>GetLastItem</i> (<b>Required: </b> InputItems1<b>Output: </b> OutputItems)</para>
    /// <para><i>ItemColToString</i> (<b>Required: </b> InputItems1 <b>Optional: </b>Separator <b>Output: </b>OutputString)</para>
    /// <para><i>RemoveDuplicateFiles</i> (<b>Required: </b> InputItems1 <b>Output: </b> OutputItems, ItemCount)</para>
    /// <para><i>Sort</i> (<b>Required: </b> InputItems1<b>Output: </b> OutputItems)</para>
    /// <para><i>StringToItemCol</i> (<b>Required: </b> ItemString, Separator <b>Output: </b> OutputItems, ItemCount)</para>
    /// <para><i>UpdateMetadata</i> (<b>Required: </b> InputItems1, InputItems2 <b>Output: </b> OutputItems)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default;UpdateMetadata;FilterItemsOnMetadata" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <ItemGroup>
    ///             <!-- Define some collections to use in the samples-->
    ///             <Col1 Include="hello"/>
    ///             <Col1 Include="how"/>
    ///             <Col1 Include="are"/>
    ///             <Col2 Include="you"/>
    ///             <Col3 Include="hello"/>
    ///             <Col3 Include="bye"/>
    ///             <DuplicateFiles Include="C:\Demo\**\*"/>
    ///             <XXX Include="AA"/>
    ///             <XXX Include="AAB"/>
    ///             <XXX Include="ABA"/>
    ///             <XXX Include="AABA"/>
    ///             <XXX Include="BBAA"/>
    ///             <YYY Include="AA">
    ///                 <Filter>CC</Filter>
    ///             </YYY>
    ///             <YYY Include="AA">
    ///                 <Filter>CHJC</Filter>
    ///             </YYY>
    ///             <YYY Include="BB">
    ///                 <Filter>CCDG</Filter>
    ///             </YYY>
    ///             <YYY Include="CC">
    ///                 <Filter>CDC</Filter>
    ///             </YYY>
    ///             <YYY Include="DD">
    ///                 <Filter>CCEE</Filter>
    ///             </YYY>
    ///         </ItemGroup>
    ///         <!-- Filter Items based on Name -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="FilterItems" InputItems1="@(XXX)" RegexPattern="^AA">
    ///             <Output TaskParameter="OutputItems" ItemName="filtered"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="filtered Items: %(filtered.Identity)"/>
    ///         <!-- Filter Items based on MetaData -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="FilterItems" InputItems1="@(YYY)" Metadata="Filter" RegexPattern="^CC">
    ///             <Output TaskParameter="OutputItems" ItemName="filteredbymeta"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="filteredbymeta Items: %(filteredbymeta.Identity)"/>
    ///         <!-- Convert an Item Collection into a string -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="ItemColToString" InputItems1="@(Col1)" Separator=" - ">
    ///             <Output TaskParameter="OutString" PropertyName="out"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="OutString: $(out)"/>
    ///         <!-- Escape a string with special MSBuild characters -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="Escape" InString="hello how;are *you">
    ///             <Output TaskParameter="OutString" PropertyName="out"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="OutString: $(out)"/>
    ///         <!-- Sort an ItemGroup alphabetically -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="Sort" InputItems1="@(Col1)">
    ///             <Output TaskParameter="OutputItems" ItemName="sorted"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="Sorted Items: %(sorted.Identity)"/>
    ///         <!-- Get a single item by position -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="GetItem" InputItems1="@(Col1)" Position="2">
    ///             <Output TaskParameter="OutputItems" ItemName="AnItem"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="Item: %(AnItem.Identity)"/>
    ///         <!-- Get the last item -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="GetLastItem" InputItems1="@(Col1)">
    ///             <Output TaskParameter="OutputItems" ItemName="LastItem"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="Last Item: %(LastItem.Identity)"/>
    ///         <!-- Get common items. Note that this can be accomplished without using a custom task. -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="GetCommonItems" InputItems1="@(Col1)" InputItems2="@(Col3)">
    ///             <Output TaskParameter="OutputItems" ItemName="comm"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="Common Items: %(comm.Identity)"/>
    ///         <!-- Get distinct items. Note that this can be accomplished without using a custom task. -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="GetDistinctItems" InputItems1="@(Col1)" InputItems2="@(Col3)">
    ///             <Output TaskParameter="OutputItems" ItemName="distinct"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="Distinct Items: %(distinct.Identity)"/>
    ///         <!-- Remove duplicate files. This can accomplish a large performance gain in some copy operations -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="RemoveDuplicateFiles" InputItems1="@(DuplicateFiles)">
    ///             <Output TaskParameter="OutputItems" ItemName="NewCol1"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="Full File List contains: %(DuplicateFiles.Identity)"/>
    ///         <Message Text="Removed Duplicates Contains: %(NewCol1.Identity)"/>
    ///         <!-- Get the number of items in a collection -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="GetItemCount" InputItems1="@(NewCol1)">
    ///             <Output TaskParameter="ItemCount" PropertyName="MyCount"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="$(MyCount)"/>
    ///         <!-- Convert a seperated list to an ItemGroup -->
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="StringToItemCol" ItemString="how,how,are,you" Separator=",">
    ///             <Output TaskParameter="OutputItems" ItemName="NewCol11"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///         <Message Text="String Item Collection contains: %(NewCol11.Identity)"/>
    ///     </Target>
    ///     <Target Name="UpdateMetadata">
    ///         <!-- This sample uses the UpdateMetadata TaskAction to update existing meatadata using that from another item -->
    ///         <ItemGroup>
    ///             <SolutionToBuild Include="$(BuildProjectFolderPath)\ChangeThisOne.sln">
    ///                 <Meta1>OriginalValue</Meta1>
    ///             </SolutionToBuild>
    ///             <SolutionToBuild Include="$(BuildProjectFolderPath)\ChangeThisToo.sln">
    ///                 <Meta1>OriginalValue</Meta1>
    ///                 <Meta2>Mike</Meta2>
    ///             </SolutionToBuild>
    ///         </ItemGroup>
    ///         <Message Text="Before = %(SolutionToBuild.Identity) %(SolutionToBuild.Meta1) %(SolutionToBuild.Meta2)" />
    ///         <ItemGroup>
    ///             <ItemsToChange Include="@(SolutionToBuild)">
    ///                 <Meta1>ChangedValue</Meta1>
    ///                 <Meta2>Dave</Meta2>
    ///             </ItemsToChange>
    ///         </ItemGroup>
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="UpdateMetadata" InputItems1="@(SolutionToBuild)" InputItems2="@(ItemsToChange)">
    ///             <Output TaskParameter="OutputItems" ItemName="SolutionToBuildTemp" />
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper >
    ///         <ItemGroup>
    ///             <SolutionToBuild Remove="@(SolutionToBuild)"/>
    ///             <SolutionToBuild Include="@(SolutionToBuildTemp)"/>
    ///         </ItemGroup>
    ///         <Message Text="After  = %(SolutionToBuild.Identity) %(SolutionToBuild.Meta1) %(SolutionToBuild.Meta2)"/>
    ///     </Target>
    ///     <ItemGroup>
    ///         <MyItems Include="$(AssembliesPath)\Assembly1.dll">
    ///             <Roles>Role1</Roles>
    ///             <GAC>true</GAC>
    ///         </MyItems>
    ///         <MyItems Include="$(AssembliesPath)\Assembly2.dll">
    ///             <Roles>Role2</Roles>
    ///             <GAC>true</GAC>
    ///         </MyItems>
    ///         <MyItems Include="$(AssembliesPath)\Assembly2.dll">
    ///             <Roles>Role2</Roles>
    ///             <GAC>false</GAC>
    ///         </MyItems>
    ///         <Roles Include="Role2;Role1"/>
    ///     </ItemGroup>
    ///     <Target Name="FilterItemsOnMetadata" DependsOnTargets="GetWorkingSets">
    ///         <Message Text="1 = %(MyItemsWorkingSet.Identity) - %(MyItemsWorkingSet.GAC)"/>
    ///     </Target>
    ///     <Target Name="GetWorkingSets">
    ///         <MSBuild.ExtensionPack.Framework.MsBuildHelper TaskAction="FilterItemsOnMetadata" InputItems1="@(MyItems)" InputItems2="@(Roles)" Separator=";" MetaData="Roles">
    ///             <Output TaskParameter="OutputItems" ItemName="MyItemsWorkingSet"/>
    ///         </MSBuild.ExtensionPack.Framework.MsBuildHelper>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class MSBuildHelper : BaseTask
    {
        private List<ITaskItem> inputItems1;
        private List<ITaskItem> inputItems2;
        private List<ITaskItem> outputItems;

        /// <summary>
        /// Gets the current directory
        /// </summary>
        [Output]
        public string CurrentDirectory { get; set; }

        /// <summary>
        /// Sets the position of the Item to get
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Sets the string to convert to a Task Item
        /// </summary>
        public string ItemString { get; set; }

        /// <summary>
        /// Sets the separator to use for splitting the ItemString when calling StringToItemCol. Also used in FilterItemsOnMetadata
        /// </summary>
        public string Separator { get; set; }

        /// <summary>
        /// Sets the input string
        /// </summary>
        public string InString { get; set; }

        /// <summary>
        /// Sets the Metadata
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Gets the output string
        /// </summary>
        [Output]
        public string OutString { get; set; }

        /// <summary>
        /// Sets InputItems1.
        /// </summary>
        public ITaskItem[] InputItems1
        {
            get { return this.inputItems1.ToArray(); }
            set { this.inputItems1 = new List<ITaskItem>(value); }
        }

        /// <summary>
        /// Sets InputItems2.
        /// </summary>
        public ITaskItem[] InputItems2
        {
            get { return this.inputItems2.ToArray(); }
            set { this.inputItems2 = new List<ITaskItem>(value); }
        }

        /// <summary>
        /// Gets the OutputItems.
        /// </summary>
        [Output]
        public ITaskItem[] OutputItems
        {
            get { return this.outputItems == null ? null : this.outputItems.ToArray(); }
            set { this.outputItems = new List<ITaskItem>(value); }
        }

        /// <summary>
        /// Gets the ItemCount.
        /// </summary>
        [Output]
        public int ItemCount { get; set; }

        /// <summary>
        /// Sets the regex pattern.
        /// </summary>
        public string RegexPattern { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            switch (this.TaskAction)
            {
                case "FilterItemsOnMetadata":
                    this.FilterItemsOnMetadata();
                    break;
                case "Escape":
                    this.Escape();
                    break;
                case "FilterItems":
                    this.FilterItems();
                    break;
                case "RemoveDuplicateFiles":
                    this.RemoveDuplicateFiles();
                    break;
                case "GetItemCount":
                    this.GetItemCount();
                    break;
                case "GetItem":
                    this.GetItem();
                    break;
                case "GetLastItem":
                    this.GetLastItem();
                    break;
                case "GetCommonItems":
                    this.GetCommonItems();
                    break;
                case "GetDistinctItems":
                    this.GetDistinctItems();
                    break;
                case "GetCurrentDirectory":
                    this.GetCurrentDirectory();
                    break;
                case "Sort":
                    this.Sort();
                    break;
                case "StringToItemCol":
                    this.StringToItemCol();
                    break;
                case "ItemColToString":
                    this.ItemColToString();
                    break;
                case "UpdateMetadata":
                    this.UpdateMetadata();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void FilterItemsOnMetadata()
        {
            this.LogTaskMessage("Filtering Items on metadata");
            if (this.InputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            if (this.inputItems2 == null)
            {
                Log.LogError("InputItems2 is required");
                return;
            }

            if (string.IsNullOrEmpty(this.Separator))
            {
                Log.LogError("Separator is required");
                return;
            }

            if (string.IsNullOrEmpty(this.Metadata))
            {
                Log.LogError("Metadata is required");
                return;
            }

            this.outputItems = new List<ITaskItem>();
            foreach (ITaskItem item in this.InputItems1)
            {
                string[] filters1 = item.GetMetadata(this.Metadata).Split(new[] { this.Separator }, StringSplitOptions.RemoveEmptyEntries);
                List<string> filters1List = new List<string>(filters1.Length);
                filters1List.AddRange(filters1);
                if (this.InputItems2.Any(item2 => filters1List.Contains(item2.ItemSpec)))
                {
                    this.outputItems.Add(item);
                }
            }
        }

        private void FilterItems()
        {
            this.LogTaskMessage("Filtering Items");
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            if (string.IsNullOrEmpty(this.RegexPattern))
            {
                Log.LogError("RegexPattern is required");
                return;
            }

            // Load the regex to use
            Regex parseRegex = new Regex(this.RegexPattern, RegexOptions.Compiled);
            this.outputItems = new List<ITaskItem>();

            foreach (ITaskItem item in this.InputItems1)
            {
                Match m = string.IsNullOrEmpty(this.Metadata) ? parseRegex.Match(item.ItemSpec) : parseRegex.Match(item.GetMetadata(this.Metadata));

                if (m.Success)
                {
                    this.outputItems.Add(item);
                }
            }

            this.ItemCount = this.outputItems.Count;
        }

        private void UpdateMetadata()
        {
            // We need to filter out reserved metadata as it can't be updated
            List<string> reservedMetadata = new List<string> { "FullPath", "RootDir", "Filename", "Extension", "RelativeDir", "Directory", "RecursiveDir", "Identity", "ModifiedTime", "CreatedTime", "AccessedTime" };

            // Validate input
            if ((this.InputItems1 == null) || (this.InputItems2 == null))
            {
                this.Log.LogError("InputItems1 and InputItems2 are mandatory", null);
                return;
            }

            this.LogTaskMessage("Updating Metadata");
            int sourceIndex = 0;
            foreach (ITaskItem sourceItem in this.InputItems1)
            {
                // Fill the new list with the source one
                this.OutputItems = this.InputItems1;
                foreach (ITaskItem itemToModify in this.InputItems2)
                {
                    // See if this is a match.  If it is then change the metadata in the new list
                    if (sourceItem.ToString() == itemToModify.ToString())
                    {
                        foreach (var s in sourceItem.MetadataNames)
                        {
                            if (reservedMetadata.Contains(s.ToString()))
                            {
                                break;
                            }

                            if (itemToModify.MetadataNames.Cast<object>().Any(x => s == x))
                            {
                                this.LogTaskMessage(string.Format(CultureInfo.InstalledUICulture, "Updating {0}.{1} to {2}", this.OutputItems[sourceIndex], s, itemToModify.GetMetadata(s.ToString())));
                                this.OutputItems[sourceIndex].SetMetadata(s.ToString(), itemToModify.GetMetadata(s.ToString()));
                            }
                        }
                    }
                }

                sourceIndex += 1;
            }
        }

        private void Escape()
        {
            if (string.IsNullOrEmpty(this.InString))
            {
                Log.LogError("InString is required");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Escaping string: {0}", this.InString));
            this.OutString = Microsoft.Build.BuildEngine.Utilities.Escape(this.InString);
        }

        private void Sort()
        {
            this.LogTaskMessage("Sorting Items");
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            this.outputItems = new List<ITaskItem>();
            ArrayList sortedItems = new ArrayList(this.InputItems1.Length);
            
            foreach (ITaskItem item in this.InputItems1)
            {
                sortedItems.Add(item.ItemSpec);
            }

            sortedItems.Sort();
            foreach (string s in sortedItems)
            {
                foreach (ITaskItem item in this.InputItems1)
                {
                    if (item.ItemSpec == s)
                    {
                        this.outputItems.Add(item);
                        break;
                    }
                }
            }
        }

        private void GetItem()
        {
            this.LogTaskMessage("Getting Item");
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            if (this.Position > this.InputItems1.Length - 1)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Position: {0} is outside the size of the item collection: {1}", this.Position, this.InputItems1.Length));
            }

            this.outputItems = new List<ITaskItem> { this.inputItems1[this.Position] };
        }

        private void GetLastItem()
        {
            this.LogTaskMessage("Getting Last Item");
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            this.outputItems = new List<ITaskItem> { this.inputItems1[this.inputItems1.Count - 1] };
        }

        private void GetDistinctItems()
        {
            this.LogTaskMessage("Getting Distinct Items");
            this.outputItems = new List<ITaskItem>();
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            if (this.inputItems2 == null)
            {
                Log.LogError("InputItems2 is required");
                return;
            }

            foreach (ITaskItem item in this.InputItems1)
            {
                bool found = false;

                // we only match on itemspec.
                foreach (ITaskItem item2 in this.inputItems2)
                {
                    if (item.ItemSpec == item2.ItemSpec)
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    this.outputItems.Add(item);
                }
            }

            foreach (ITaskItem item in this.InputItems2)
            {
                bool found = false;

                // we only match on itemspec.
                foreach (ITaskItem item2 in this.InputItems1)
                {
                    if (item.ItemSpec == item2.ItemSpec)
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    this.outputItems.Add(item);
                }
            }

            this.ItemCount = this.outputItems.Count;
        }

        private void GetCommonItems()
        {
            this.LogTaskMessage("Getting Common Items");
            this.outputItems = new List<ITaskItem>();
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            if (this.inputItems2 == null)
            {
                Log.LogError("InputItems2 is required");
                return;
            }

            foreach (ITaskItem item in this.inputItems1)
            {
                bool found = false;

                // we only match on itemspec.
                foreach (ITaskItem item2 in this.inputItems2)
                {
                    if (item.ItemSpec == item2.ItemSpec)
                    {
                        found = true;
                    }
                }

                if (found)
                {
                    this.outputItems.Add(item);
                }
            }

            this.ItemCount = this.outputItems.Count;
        }

        private void StringToItemCol()
        {
            this.LogTaskMessage("Converting String to Item Collection");

            if (string.IsNullOrEmpty(this.ItemString))
            {
                Log.LogError("ItemString is required");
                return;
            }

            if (string.IsNullOrEmpty(this.Separator))
            {
                Log.LogError("Separator is required");
                return;
            }

            this.outputItems = new List<ITaskItem>();
            string[] s = this.ItemString.Split(new[] { this.Separator }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string newItem in s)
            {
                this.outputItems.Add(new TaskItem(newItem));
            }

            this.ItemCount = this.outputItems.Count;
        }

        private void ItemColToString()
        {
            this.LogTaskMessage("Converting Item Collection to String");
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }
            
            if (string.IsNullOrEmpty(this.Separator))
            {
                this.Separator = string.Empty;
            }

            StringBuilder stringToReturn = new StringBuilder();
            foreach (ITaskItem t in this.inputItems1)
            {
                stringToReturn.AppendFormat("{0}{1}", t.ItemSpec, this.Separator);
            }

            this.OutString = stringToReturn.ToString().Substring(0, stringToReturn.Length - this.Separator.Length);
        }

        private void RemoveDuplicateFiles()
        {
            this.LogTaskMessage("Removing Duplicates");
            if (this.inputItems1 == null)
            {
                Log.LogError("InputItems1 is required");
                return;
            }

            this.outputItems = new List<ITaskItem>();
            ArrayList names = new ArrayList();

            foreach (ITaskItem item in this.InputItems1)
            {
                FileInfo f = new FileInfo(item.ItemSpec);
                if (!names.Contains(f.Name))
                {
                    names.Add(f.Name);
                    this.outputItems.Add(item);
                }
            }

            this.ItemCount = this.outputItems.Count;
        }

        private void GetCurrentDirectory()
        {
            this.LogTaskMessage("Getting Current Directory");
            System.IO.FileInfo projFile = new System.IO.FileInfo(BuildEngine.ProjectFileOfTaskNode);

            if (projFile.Directory != null)
            {
                this.CurrentDirectory = projFile.Directory.FullName;
            }
        }

        private void GetItemCount()
        {
            this.LogTaskMessage("Getting Item Count");
            this.ItemCount = this.inputItems1 == null ? 0 : this.inputItems1.Count;
        }
    }
}
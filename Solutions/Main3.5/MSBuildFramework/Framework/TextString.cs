//-----------------------------------------------------------------------
// <copyright file="TextString.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Globalization;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Compare</i> (<b>Required: </b> String1, String2, Comparison <b> Optional: </b> IgnoreCase <b>Output: </b>Result)</para>
    /// <para><i>EndsWith</i> (<b>Required: </b> String1, String2<b> Optional: </b> IgnoreCase <b>Output: </b>Result)</para>
    /// <para><i>GetLength</i> (<b>Required: </b> OldString<b> Output: </b> NewString)</para>
    /// <para><i>Insert</i> (<b>Required: </b> OldString, String1, StartIndex<b> Output: </b> NewString)</para>
    /// <para><i>PadLeft</i> (<b>Required: </b> OldString, String1 (1 char) <b> Optional: </b>Count <b>Output: </b> NewString)</para>
    /// <para><i>PadRight</i> (<b>Required: </b> OldString, String1 (1 char) <b> Optional: </b>Count <b>Output: </b> NewString)</para>
    /// <para><i>Remove</i> (<b>Required: </b> OldString, StartIndex <b> Optional: </b>Count <b> Output: </b> NewString)</para>
    /// <para><i>Replace</i> (<b>Required: </b> OldString, OldValue, NewValue <b> Output: </b> NewString)</para>
    /// <para><i>StartsWith</i> (<b>Required: </b> String1, String2<b> Optional: </b> IgnoreCase <b>Output: </b>Result)</para>
    /// <para><i>ToLower</i> (<b>Required: </b> OldString<b> Output: </b> NewString)</para>
    /// <para><i>ToUpper</i> (<b>Required: </b> OldString<b> Output: </b> NewString)</para>
    /// <para><i>Trim</i> (<b>Required: </b> OldString<b> Output: </b> NewString)</para>
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
    ///         <!-- Uppercase a string -->
    ///         <MSBuild.ExtensionPack.Framework.TextString TaskAction="ToUpper" OldString="helLo">
    ///             <Output PropertyName="out" TaskParameter="NewString"/>
    ///         </MSBuild.ExtensionPack.Framework.TextString>
    ///         <Message Text="The Result: $(out)"/>
    ///         <!-- Lowercase a string -->
    ///         <MSBuild.ExtensionPack.Framework.TextString TaskAction="ToLower" OldString="HellO">
    ///             <Output PropertyName="out" TaskParameter="NewString"/>
    ///         </MSBuild.ExtensionPack.Framework.TextString>
    ///         <Message Text="The Result: $(out)"/>
    ///         <!-- PadLeft a string -->
    ///         <MSBuild.ExtensionPack.Framework.TextString TaskAction="PadLeft" OldString="Hello" String1="A" Count="10">
    ///             <Output PropertyName="out" TaskParameter="NewString"/>
    ///         </MSBuild.ExtensionPack.Framework.TextString>
    ///         <Message Text="The Result: $(out)"/>
    ///         <!-- PadRight a string -->
    ///         <MSBuild.ExtensionPack.Framework.TextString TaskAction="PadRight" OldString="Hello" String1="A" Count="10">
    ///             <Output PropertyName="out" TaskParameter="NewString"/>
    ///         </MSBuild.ExtensionPack.Framework.TextString>
    ///         <Message Text="The Result: $(out)"/>
    ///         <!-- Check whether a string starts with another string -->
    ///         <MSBuild.ExtensionPack.Framework.TextString TaskAction="StartsWith" String1="Hello" String2="He">
    ///             <Output PropertyName="TheResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Framework.TextString>
    ///         <Message Text="The Result: $(TheResult)"/>
    ///         <!-- Check whether a string ends with another string -->
    ///         <MSBuild.ExtensionPack.Framework.TextString TaskAction="EndsWith" String1="Hello" String2="Lo" IgnoreCase="false">
    ///             <Output PropertyName="TheResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Framework.TextString>
    ///         <Message Text="The Result: $(TheResult)"/>
    ///         <!-- Compare two strings to see whether they are equal -->
    ///         <MSBuild.ExtensionPack.Framework.TextString TaskAction="Compare" String1="Hello" String2="Hello" Comparison="equals">
    ///             <Output PropertyName="TheResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Framework.TextString>
    ///         <Message Text="The Result: $(TheResult)"/>
    ///         <!-- Compare two strings to see whether they are equal -->
    ///         <MSBuild.ExtensionPack.Framework.TextString TaskAction="Compare" String1="Hello" String2="Hallo" Comparison="equals">
    ///             <Output PropertyName="TheResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Framework.TextString>
    ///         <Message Text="The Result: $(TheResult)"/>
    ///         <!-- See whether one string is greater than another -->
    ///         <MSBuild.ExtensionPack.Framework.TextString TaskAction="Compare" String1="Hello" String2="Hallo" Comparison="greaterthan">
    ///             <Output PropertyName="TheResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Framework.TextString>
    ///         <Message Text="The Result: $(TheResult)"/>
    ///         <!-- See whether one string is less than another -->
    ///         <MSBuild.ExtensionPack.Framework.TextString TaskAction="Compare" String1="Hello" String2="Hallo" Comparison="lessthan">
    ///             <Output PropertyName="TheResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Framework.TextString>
    ///         <Message Text="The Result: $(TheResult)"/>
    ///         <!-- See whether a string contains another string -->
    ///         <MSBuild.ExtensionPack.Framework.TextString TaskAction="Compare" String1="Hello" String2="llo" Comparison="contains">
    ///             <Output PropertyName="TheResult" TaskParameter="Result"/>
    ///         </MSBuild.ExtensionPack.Framework.TextString>
    ///         <Message Text="The Result: $(TheResult)"/>
    ///         <!-- Replace the contents off a string -->
    ///         <MSBuild.ExtensionPack.Framework.TextString TaskAction="Replace" OldString="Hello" OldValue="llo" NewValue="XYZ">
    ///             <Output PropertyName="out" TaskParameter="NewString"/>
    ///         </MSBuild.ExtensionPack.Framework.TextString>
    ///         <Message Text="The Result: $(out)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class TextString : BaseTask
    {
        private bool ignoreCase = true;
        private StringComparison strcom = StringComparison.OrdinalIgnoreCase;

        /// <summary>
        /// Sets the start index for Remove
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// Sets the count
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets a value indicating whether the result is true or false.
        /// </summary>
        [Output]
        public bool Result { get; set; }

        /// <summary>
        /// Sets the string1.
        /// </summary>
        public string String1 { get; set; }

        /// <summary>
        /// Sets the string2.
        /// </summary>
        public string String2 { get; set; }

        /// <summary>
        /// Sets the Comparison. Supports 'GreaterThan', 'LessThan', 'GreaterThanOrEquals', 'LessThanOrEquals', 'Contains', 'StartsWith', 'EndsWith'
        /// </summary>
        public string Comparison { get; set; }

        /// <summary>
        /// Sets a value indicating whether [ignore case]. Default is true.
        /// </summary>
        public bool IgnoreCase
        {
            get { return this.ignoreCase; }
            set { this.ignoreCase = value; }
        }

        /// <summary>
        /// Sets the old string.
        /// </summary>
        public string OldString { get; set; }

        /// <summary>
        /// Gets the new string.
        /// </summary>
        [Output]
        public string NewString { get; set; }

        /// <summary>
        /// Sets the old value.
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>
        /// Sets the new value.
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// This is the main execute method that all tasks should implement
        /// </summary>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            if (!this.IgnoreCase)
            {
                this.strcom = StringComparison.Ordinal;
            }

            switch (this.TaskAction)
            {
                case "Compare":
                    this.Compare();
                    break;
                case "EndsWith":
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking whether: {0} ends with: {1}", this.String1, this.String2));
                    this.Result = this.String1.EndsWith(this.String2, this.strcom);
                    break;
                case "StartsWith":
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking whether: {0} starts with: {1}", this.String1, this.String2));
                    this.Result = this.String1.StartsWith(this.String2, this.strcom);
                    break;
                case "Replace":
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Replacing String: {0}", this.OldString));
                    this.NewString = this.OldString.Replace(this.OldValue, this.NewValue);
                    break;
                case "Trim":
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Trimming String: {0}", this.OldString));
                    this.NewString = this.OldString.Trim();
                    break;
                case "ToLower":
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Lower casing: {0}", this.OldString));
                    this.NewString = this.OldString.ToLower(CultureInfo.CurrentUICulture);
                    break;
                case "ToUpper":
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Upper casing: {0}", this.OldString));
                    this.NewString = this.OldString.ToUpper(CultureInfo.CurrentUICulture);
                    break;
                case "Remove":
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing String: {0}", this.OldString));
                    this.NewString = this.Count > 0 ? this.OldString.Remove(this.StartIndex, this.Count) : this.OldString.Remove(this.StartIndex);
                    break;
                case "GetLength":
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Get Length of: {0}", this.OldString));
                    this.NewString = this.OldString.Length.ToString(CultureInfo.CurrentCulture);
                    break;
                case "Insert":
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Inserting: {0} into: {1}", this.String1, this.OldString));
                    this.NewString = this.OldString.Insert(this.StartIndex, this.String1);
                    break;
                case "PadLeft":
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Padding: {0} left with: {1}", this.OldString, this.String1[0]));
                    this.NewString = this.OldString.PadLeft(this.Count, this.String1[0]);
                    break;
                case "PadRight":
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Padding: {0} right with: {1}", this.OldString, this.String1[0]));
                    this.NewString = this.OldString.PadRight(this.Count, this.String1[0]);
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        /// <summary>
        /// Compares this instance.
        /// </summary>
        private void Compare()
        {
            if (string.IsNullOrEmpty(this.String1))
            {
                this.Log.LogError("String1 is required");
                return;
            }

            if (string.IsNullOrEmpty(this.String2))
            {
                this.Log.LogError("String2 is required");
                return;
            }

            if (string.IsNullOrEmpty(this.Comparison))
            {
                this.Log.LogError("Comparison is required");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Comparing String: {0} [{1}] {2}", this.String1, this.Comparison.ToUpperInvariant(), this.String2));
            switch (this.Comparison.ToUpperInvariant())
            {
                case "GREATERTHAN":
                    this.Result = string.Compare(this.String1, this.String2, strcom) > 0;
                    break;
                case "LESSTHAN":
                    this.Result = string.Compare(this.String1, this.String2, strcom) < 0;
                    break;
                case "GREATERTHANOREQUALS":
                    this.Result = string.Compare(this.String1, this.String2, strcom) >= 0;
                    break;
                case "LESSTHANOREQUALS":
                    this.Result = string.Compare(this.String1, this.String2, strcom) <= 0;
                    break;
                case "EQUALS":
                    this.Result = string.Compare(this.String1, this.String2, strcom) == 0;
                    break;
                case "CONTAINS":
                    this.Result = this.String1.IndexOf(this.String2, strcom) >= 0;
                    break;
                case "STARTSWITH":
                    this.Result = this.String1.StartsWith(this.String2, strcom);
                    break;
                case "ENDSWITH":
                    this.Result = this.String1.EndsWith(this.String2, strcom);
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid Comparison passed: {0}. Valid Comparisons are GREATERTHAN, LESSTHAN, GREATERTHANOREQUALS, LESSTHANOREQUALS, EQUALS, CONTAINS, STARTSWITH, ENDSWITH", this.Comparison.ToUpperInvariant()));
                    return;
            }
        }
    }
}
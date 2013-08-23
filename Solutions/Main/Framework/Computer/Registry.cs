//-----------------------------------------------------------------------
// <copyright file="Registry.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System;
    using System.Globalization;
    using System.Text;
    using Microsoft.Build.Framework;
    using Microsoft.Win32;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CheckEmpty</i> (<b>Required: </b> RegistryHive, Key <b>Optional:</b> RegistryView <b>Output: </b>Empty)</para>
    /// <para><i>CheckValueExists</i> (<b>Required: </b> RegistryHive, Key, Value <b>Optional:</b> RegistryView <b>Output: </b>Empty (true iff the value does not exist))</para>
    /// <para><i>CreateKey</i> (<b>Required: </b> RegistryHive, Key <b>Optional:</b> RegistryView)</para>
    /// <para><i>DeleteKey</i> (<b>Required: </b> RegistryHive, Key <b>Optional:</b> RegistryView)</para>
    /// <para><i>DeleteKeyTree</i> (<b>Required: </b> RegistryHive, Key <b>Optional:</b> RegistryView )</para>
    /// <para><i>DeleteValue</i> (<b>Required: </b> RegistryHive, Key, Value <b>Optional:</b> RegistryView<b>Output: </b>Empty (true iff the Delete was redundant))</para>
    /// <para><i>Get</i> (<b>Required: </b> RegistryHive, Key, Value  <b>Optional:</b> RegistryView <b>Output: </b>Data)</para>
    /// <para><i>Set</i> (<b>Required: </b> RegistryHive, Key, Value <b>Optional:</b> DataType, RegistryView)</para>
    /// <para><b>Remote Execution Support:</b> Yes</para>
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
    ///         <!-- Create a key -->
    ///         <MSBuild.ExtensionPack.Computer.Registry TaskAction="CreateKey" RegistryHive="LocalMachine" Key="SOFTWARE\ANewTemp"/>
    ///         <!-- Check if a key is empty -->
    ///         <MSBuild.ExtensionPack.Computer.Registry TaskAction="CheckEmpty" RegistryHive="LocalMachine" Key="SOFTWARE\ANewTemp">
    ///             <Output PropertyName="REmpty" TaskParameter="Empty"/>
    ///         </MSBuild.ExtensionPack.Computer.Registry>
    ///         <Message Text="SOFTWARE\ANewTemp is empty: $(REmpty)"/>
    ///         <!-- Set a value -->
    ///         <MSBuild.ExtensionPack.Computer.Registry TaskAction="Set" RegistryHive="LocalMachine" Key="SOFTWARE\ANewTemp" Value="MySetting" Data="21"/>
    ///         <!-- Check if the value exists -->
    ///         <MSBuild.ExtensionPack.Computer.Registry TaskAction="CheckValueExists" RegistryHive="LocalMachine" Key="SOFTWARE\ANewTemp" Value="MySetting">
    ///             <Output PropertyName="RExists" TaskParameter="Exists"/>
    ///         </MSBuild.ExtensionPack.Computer.Registry>
    ///         <Message Text="SOFTWARE\ANewTemp\@MySetting exists: $(RExists)"/>
    ///         <!-- Get the value out -->
    ///         <MSBuild.ExtensionPack.Computer.Registry TaskAction="Get" RegistryHive="LocalMachine" Key="SOFTWARE\ANewTemp" Value="MySetting">
    ///             <Output PropertyName="RData" TaskParameter="Data"/>
    ///         </MSBuild.ExtensionPack.Computer.Registry>
    ///         <Message Text="Registry Value: $(RData)"/>
    ///         <!-- Check if a key is empty again -->
    ///         <MSBuild.ExtensionPack.Computer.Registry TaskAction="CheckEmpty" RegistryHive="LocalMachine" Key="SOFTWARE\ANewTemp">
    ///             <Output PropertyName="REmpty" TaskParameter="Empty"/>
    ///         </MSBuild.ExtensionPack.Computer.Registry>
    ///         <Message Text="SOFTWARE\ANewTemp is empty: $(REmpty)"/>
    ///         <!-- Set some Binary Data -->
    ///         <MSBuild.ExtensionPack.Computer.Registry TaskAction="Set" RegistryHive="LocalMachine" Key="SOFTWARE\ANewTemp" DataType="Binary" Value="binval" Data="10, 43, 44, 45, 14, 255" />
    ///         <!--Get some Binary Data--> 
    ///         <MSBuild.ExtensionPack.Computer.Registry TaskAction="Get" RegistryHive="LocalMachine" Key="SOFTWARE\ANewTemp" Value="binval">
    ///             <Output PropertyName="RData" TaskParameter="Data"/>
    ///         </MSBuild.ExtensionPack.Computer.Registry>
    ///         <Message Text="Registry Value: $(RData)"/>
    ///         <!-- Delete a value -->
    ///         <MSBuild.ExtensionPack.Computer.Registry TaskAction="DeleteValue" RegistryHive="LocalMachine" Key="SOFTWARE\ANewTemp" Value="MySetting" />
    ///         <!-- Delete a key -->
    ///         <MSBuild.ExtensionPack.Computer.Registry TaskAction="DeleteKey" RegistryHive="LocalMachine" Key="SOFTWARE\ANewTemp"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Registry : BaseTask
    {
        private const string CheckEmptyTaskAction = "CheckEmpty";
        private const string CheckValueExistsTaskAction = "CheckValueExists";
        private const string CreateKeyTaskAction = "CreateKey";
        private const string DeleteKeyTaskAction = "DeleteKey";
        private const string DeleteKeyTreeTaskAction = "DeleteKeyTree";
        private const string DeleteValueTaskAction = "DeleteValue";
        private const string GetTaskAction = "Get";
        private const string SetTaskAction = "Set";
        private RegistryKey registryKey;
        private RegistryHive hive;
        private RegistryView view = Microsoft.Win32.RegistryView.Default;

        /// <summary>
        /// Sets the type of the data. RegistryValueKind Enumeration. Support for Binary, DWord, MultiString, QWord, ExpandString
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Gets the data.
        /// </summary>
        [Output]
        public string Data { get; set; }

        /// <summary>
        /// Sets the value. If Value is not provided, an attempt will be made to read the Default Value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Sets the Registry Hive. Supports ClassesRoot, CurrentUser, LocalMachine, Users, PerformanceData, CurrentConfig, DynData
        /// </summary>
        [Required]
        public string RegistryHive
        {
            get
            {
                return this.hive.ToString();
            }

            set
            {
                this.hive = (RegistryHive)Enum.Parse(typeof(RegistryHive), value);
            }
        }

        /// <summary>
        /// Sets the Registry View. Supports Registry32, Registry64 and Default. Defaults to Default
        /// </summary>
        public string RegistryView
        {
            get
            {
                return this.view.ToString();
            }

            set
            {
                this.view = (RegistryView)Enum.Parse(typeof(RegistryView), value);
            }
        }

        /// <summary>
        /// Sets the key.
        /// </summary>
        [Required]
        public string Key { get; set; }

        /// <summary>
        /// Indicates whether the Registry Key is empty or not
        /// </summary>
        [Output]
        public bool Empty { get; set; }

        /// <summary>
        /// Indicates whether the Registry value exists
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            try
            {
                this.registryKey = RegistryKey.OpenRemoteBaseKey(this.hive, this.MachineName, this.view);
            }
            catch (System.ArgumentException)
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The Registry Hive provided is not valid: {0}", this.RegistryHive));
                return;
            }

            switch (this.TaskAction)
            {
                case CreateKeyTaskAction:
                    this.CreateKey();
                    break;
                case DeleteKeyTaskAction:
                    this.DeleteKey();
                    break;
                case DeleteKeyTreeTaskAction:
                    this.DeleteKeyTree();
                    break;
                case GetTaskAction:
                    this.Get();
                    break;
                case SetTaskAction:
                    this.Set();
                    break;
                case CheckEmptyTaskAction:
                    this.CheckEmpty();
                    break;
                case DeleteValueTaskAction:
                    this.DeleteValue();
                    break;
                case CheckValueExistsTaskAction:
                    this.CheckValueExists();
                    break;
                default:
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private static string GetRegistryKeyValue(RegistryKey subkey, string value)
        {
            var v = subkey.GetValue(value);
            if (v == null)
            {
                return null;
            }

            RegistryValueKind valueKind = subkey.GetValueKind(value);
            if (valueKind == RegistryValueKind.Binary && v is byte[])
            {
                byte[] valueBytes = (byte[])v;
                StringBuilder bytes = new StringBuilder(valueBytes.Length * 2);
                foreach (byte b in valueBytes)
                {
                    bytes.Append(b.ToString(CultureInfo.InvariantCulture));
                    bytes.Append(',');
                }

                return bytes.ToString(0, bytes.Length - 1);
            }

            if (valueKind == RegistryValueKind.MultiString && v is string[])
            {
                var itemList = new StringBuilder();
                foreach (string item in (string[])v)
                {
                    itemList.Append(item);
                    itemList.Append(',');
                }

                return itemList.ToString(0, itemList.Length - 1);
            }

            return v.ToString();
        }

        /// <summary>
        /// Checks if a Registry Key contains values or subkeys.
        /// </summary>
        private void CheckEmpty()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking if Registry Key: {0} is empty in Hive: {1}, View: {2} on: {3}", this.Key, this.RegistryHive, this.RegistryView, this.MachineName));
            RegistryKey subKey = this.registryKey.OpenSubKey(this.Key, true);
            if (subKey != null)
            {
                if (subKey.SubKeyCount <= 0)
                {
                    this.Empty = subKey.ValueCount <= 0;
                }
                else
                {
                    this.Empty = false;
                }
            }
            else
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "Registry Key: {0} not found in Hive: {1}, View: {2} on: {3}", this.Key, this.RegistryHive, this.RegistryView, this.MachineName));
            }
        }

        private void Set()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting Registry Value: {0} for Key: {1} in Hive: {2}, View: {3} on: {4}", this.Value, this.Key, this.RegistryHive, this.RegistryView, this.MachineName));
            bool changed = false;
            RegistryKey subKey = this.registryKey.OpenSubKey(this.Key, true);
            if (subKey != null)
            {
                string oldData = GetRegistryKeyValue(subKey, this.Value);
                if (oldData == null || oldData != this.Data)
                {
                    if (string.IsNullOrEmpty(this.DataType))
                    {
                        subKey.SetValue(this.Value, this.Data);
                    }
                    else
                    {
                        // assumption that ',' is separator for binary and multistring value types.
                        char[] separator = { ',' };
                        object registryValue;

                        RegistryValueKind valueKind = (Microsoft.Win32.RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), this.DataType, true);
                        switch (valueKind)
                        {
                            case RegistryValueKind.Binary:
                                string[] parts = this.Data.Split(separator);
                                byte[] val = new byte[parts.Length];
                                for (int i = 0; i < parts.Length; i++)
                                {
                                    val[i] = byte.Parse(parts[i], CultureInfo.CurrentCulture);
                                }

                                registryValue = val;
                                break;
                            case RegistryValueKind.DWord:
                                registryValue = uint.Parse(this.Data, CultureInfo.CurrentCulture);
                                break;
                            case RegistryValueKind.MultiString:
                                string[] parts1 = this.Data.Split(separator);
                                registryValue = parts1;
                                break;
                            case RegistryValueKind.QWord:
                                registryValue = ulong.Parse(this.Data, CultureInfo.CurrentCulture);
                                break;
                            default:
                                registryValue = this.Data;
                                break;
                        }

                        subKey.SetValue(this.Value, registryValue, valueKind);
                    }

                    changed = true;
                }

                subKey.Close();
            }
            else
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "Registry Key: {0} not found in Hive: {1}, View: {2} on: {3}", this.Key, this.RegistryHive, this.RegistryView, this.MachineName));
            }

            if (changed)
            {
                // Broadcast config change
                if (0 == NativeMethods.SendMessageTimeout(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SETTINGCHANGE, 0, "Environment", NativeMethods.SMTO_ABORTIFHUNG, NativeMethods.SENDMESSAGE_TIMEOUT, 0))
                {
                    this.LogTaskWarning("NativeMethods.SendMessageTimeout returned 0");
                }
            }

            this.registryKey.Close();
        }

        private void Get()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting Registry value: {0} from Key: {1} in Hive: {2}, View: {3} on: {4}", this.Value, this.Key, this.RegistryHive, this.RegistryView, this.MachineName));
            RegistryKey subKey = this.registryKey.OpenSubKey(this.Key, false);
            if (subKey == null)
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The Registry Key provided is not valid: {0}", this.Key));
                return;
            }

            if (subKey.GetValue(this.Value) == null)
            {
                this.LogTaskMessage(string.IsNullOrEmpty(this.Value) ? string.Format(CultureInfo.CurrentCulture, "A Default value was not found for the Registry Key: {0}", this.Key) : string.Format(CultureInfo.CurrentCulture, "The Registry value provided is not valid: {0}", this.Value));
                return;
            }

            this.Data = GetRegistryKeyValue(subKey, this.Value);
            subKey.Close();
            this.registryKey.Close();
        }

        private void DeleteKeyTree()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting Key Tree: {0} in Hive: {1}, View: {2} on: {3}", this.Key, this.RegistryHive, this.RegistryView, this.MachineName));
            using (RegistryKey r = RegistryKey.OpenRemoteBaseKey(this.hive, this.MachineName, this.view))
            {
                r.DeleteSubKeyTree(this.Key);
            }
        }

        private void DeleteKey()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting Registry Key: {0} in Hive: {1}, View: {2} on: {3}", this.Key, this.RegistryHive, this.RegistryView, this.MachineName));
            using (RegistryKey r = RegistryKey.OpenRemoteBaseKey(this.hive, this.MachineName, this.view))
            {
                r.DeleteSubKey(this.Key, false);
            }
        }

        private void CreateKey()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating Registry Key: {0} in Hive: {1}, View: {2} on: {3}", this.Key, this.RegistryHive, this.RegistryView, this.MachineName));
            using (RegistryKey r = RegistryKey.OpenRemoteBaseKey(this.hive, this.MachineName, this.view))
            using (RegistryKey r2 = r.CreateSubKey(this.Key))
            {
            }
        }

        private void DeleteValue()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting Registry value: {0} from Key: {1} in Hive: {2} on: {3}", this.Value, this.Key, this.RegistryHive, this.MachineName));
            RegistryKey subKey = this.registryKey.OpenSubKey(this.Key, true);
            if (subKey != null)
            {
                var val = subKey.GetValue(this.Value);
                if (val != null)
                {
                    subKey.DeleteValue(this.Value);
                }
            }
        }

        private void CheckValueExists()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking if Registry Value: {0} for Key {1} exists in Hive: {2} on: {3}", this.Value, this.Key, this.RegistryHive, this.MachineName));
            RegistryKey subKey = this.registryKey.OpenSubKey(this.Key, false);
            this.Exists = !((subKey == null) || (subKey.GetValue(this.Value) == null));
        }
    }
}
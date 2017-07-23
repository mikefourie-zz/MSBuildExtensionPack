//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigMangerTests.cs">(c) 2017 Mike Fourie and Contributors (https://github.com/mikefourie/MSBuildExtensionPack) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
//// TODO: on a rainy day.......
//////using System;
//////using System.Configuration;
//////using System.Web.Configuration;
//////using System.Xml;
//////using Microsoft.Build.Utilities;
//////using Microsoft.VisualStudio.TestTools.UnitTesting;
//////using MSBuild.ExtensionPack.Computer;

//////namespace MSBuild.ExtensionPack.FrameworkTests.Computer
//////{
//////    [TestClass]
//////    public sealed class DotNetFrameworkTest
//////    {
//////        private DotNetFramework _task;
//////        private bool _result;
//////        private Configuration _config;

//////        [TestInitialize]
//////        public void SetUp()
//////        {
//////            _task = new DotNetFramework
//////                        {
//////                            Log = new TaskLoggingHelper(new MockBuildEngine(), "Network")
//////                        };
//////            ReloadConfig();
//////        }

//////        private void ReloadConfig()
//////        {
//////            switch( _task.ConfigurationFile )
//////            {
//////                case( "MachineConfig" ):
//////                    _config = WebConfigurationManager.OpenMachineConfiguration();
//////                    break;
//////                case( "FrameworkWebConfig" ):
//////                    _config = WebConfigurationManager.OpenWebConfiguration(null);
//////                    break;
//////                default:
//////                    throw new ApplicationException("Unrecognized value for the ConfigurationFile task parameter.");
//////            }
//////        }

//////        #region Set Machine Config App Setting
//////        [TestMethod]
//////        public void ShouldAddAppSettingToMachineConfig()
//////        {
//////            const string appSettingName = "mytestsetting";
//////            GivenAppSettingDoesNotExist(appSettingName);

//////            WhenSettingAppSetting(appSettingName, "mytestvalue");

//////            ThenTaskSucceeded();
//////            ThenAppSettingIs(appSettingName, "mytestvalue");
//////        }

//////        [TestMethod]
//////        public void ShouldUpdateMachineConfigAppSetting()
//////        {
//////            var settingName = "existingsetting";
//////            var settingValue = "existingvalue";
//////            GivenExistingAppSetting(settingName, settingValue);

//////            WhenSettingAppSetting(settingName, "mynewvalue");

//////            ThenTaskSucceeded();
//////            ThenAppSettingIs(settingName, "mynewvalue");
//////        }

//////        [TestMethod]
//////        public void ShouldNotDuplicateMachineConfigAppSettingWhenNewValueIsTheSame()
//////        {
//////            const string settingName = "existingsetting";
//////            const string settingValue = "existingvalue";
//////            GivenExistingAppSetting(settingName, settingValue);

//////            // when updating app setting
//////            WhenSettingAppSetting(settingName, settingValue);

//////            ThenTaskSucceeded();
//////            ThenAppSettingIs(settingName, _task.SettingValue);
//////        }

//////        [TestMethod]
//////        public void ShouldRequireSettingNameWhenSettingAppSetting()
//////        {
//////            WhenSettingAppSetting(null, "");

//////            ThenTaskFailed();
//////        }

//////        [TestMethod]
//////        public void ShouldRemoveAppSetting()
//////        {
//////            const string name = "appsettingtoremove";
//////            const string value = "it doesn't matter";

//////            GivenExistingAppSetting(name, value);

//////            WhenRemovingAppSetting(name);

//////            ThenTaskSucceeded();
//////            ThenAppSettingDoesNotExist(name);
//////        }

//////        #endregion

//////        #region Set Connection String
//////        [TestMethod]
//////        public void ShouldAddNewConnectionStringToMachineConfig()
//////        {
//////            const string name = "newnewconnectionstring";
//////            const string value = "my connection string";

//////            GivenNoConnectionString(name);

//////            WhenSettingConnectionString(name, value);

//////            ThenTaskSucceeded();
//////            ThenConnectionStringIs(name, value);
//////        }

//////        [TestMethod]
//////        public void ShouldUpdateExistingConnectionStringInMachineConfig()
//////        {
//////            const string name = "existingconnectionstring";
//////            const string firstValue = "my first value";
//////            const string updatedValue = "my updated value";

//////            GivenExistingConnectionString(name, firstValue);

//////            WhenSettingConnectionString(name, updatedValue);

//////            ThenTaskSucceeded();
//////            ThenConnectionStringIs(name, updatedValue);
//////        }

//////        private void GivenExistingConnectionString(string name, string value)
//////        {
//////            GivenNoConnectionString(name);
//////            _config.ConnectionStrings.ConnectionStrings.Add(new ConnectionStringSettings(name, value));
//////            _config.Save(ConfigurationSaveMode.Minimal);
//////        }

//////        [TestMethod]
//////        public void ShouldRequireSettingNameWhenSettingConnectionString()
//////        {
//////            WhenSettingConnectionString(null, "");

//////            ThenTaskFailed();
//////        }

//////        [TestMethod]
//////        public void ShouldRemoveConnectionString()
//////        {
//////            const string name = "connectionstringtoremove";
//////            const string value = "it doesn't matter";

//////            GivenExistingConnectionString(name, value);

//////            WhenRemovingConnectionString(name);

//////            ThenTaskSucceeded();
//////            ThenConnectionStringDoesNotExist(name);
//////        }

//////        #endregion

//////        [TestMethod]
//////        public void ShouldDefaultToMachineConfig()
//////        {
//////            Assert.AreEqual(DotNetConfigurationFile.MachineConfig.ToString(), _task.ConfigurationFile);
//////        }

//////        [TestMethod]
//////        public void ShouldDefaultToMinimalSaveMode()
//////        {
//////            Assert.AreEqual(ConfigurationSaveMode.Minimal.ToString(), _task.SaveMode);
//////        }

//////        [TestMethod]
//////        public void ShouldFailIfInvalidTaskActionGive()
//////        {
//////            _task.TaskAction = "djkfjsdlk";

//////            var result = _task.Execute();
//////            Assert.IsFalse(result);
//////        }

//////        [TestMethod]
//////        public void ShouldSupportUpdatingFrameworkWebConfig()
//////        {
//////            GivenUpdatingFrameworkWebConfig();
//////            GivenAppSettingDoesNotExist("blah");

//////            WhenSettingAppSetting("blah", "blah");

//////            ThenTaskSucceeded();
//////            ThenAppSettingIs("blah", "blah");
//////        }

//////        [TestMethod]
//////        public void ShouldSupportSettingSaveMode()
//////        {
//////            const string settingName = "savemodesetting";
//////            const string settingValue = "modified";

//////            GivenExistingAppSetting(settingName, settingValue);
//////            GivenUpdatingFrameworkWebConfig();
//////            GivenAppSettingDoesNotExist(settingName);
//////            GivenSaveMode(ConfigurationSaveMode.Modified);

//////            WhenSettingAppSetting(settingName, settingValue);

//////            ThenTaskSucceeded();
//////            ThenAppSettingIs(settingName, settingValue);

//////            // we need to check the actual web.config file, since the appsettings collection is hierarcical and includes
//////            // settings from web.config.
//////            var xmlFile = new XmlDocument();
//////            xmlFile.Load(_config.FilePath);

//////            var nodes = xmlFile.SelectNodes("//appSettings/add[@key='" + settingName + "' and @value='" + settingValue + "']");
//////            Assert.IsNotNull(nodes);
//////            Assert.AreEqual(1, nodes.Count);
//////        }

//////        private void GivenSaveMode(ConfigurationSaveMode saveMode)
//////        {
//////            _task.SaveMode = saveMode.ToString();
//////        }

//////        private void GivenUpdatingFrameworkWebConfig()
//////        {
//////            _task.ConfigurationFile = DotNetConfigurationFile.FrameworkWebConfig.ToString();
//////            ReloadConfig();
//////        }

//////        private void GivenExistingAppSetting(string settingName, string settingValue)
//////        {
//////            GivenAppSettingDoesNotExist(settingName);
//////            _config.AppSettings.Settings.Add(settingName, settingValue);
//////            SaveConfig();
//////        }

//////        private void GivenAppSettingDoesNotExist(string appSettingName)
//////        {
//////            if (_config.AppSettings.Settings[appSettingName] != null)
//////            {
//////                _config.AppSettings.Settings.Remove(appSettingName);
//////                SaveConfig();
//////            }
//////        }

//////        private void GivenNoConnectionString(string name)
//////        {
//////            if (_config.Sections["connectionStrings"] != null &&
//////                _config.ConnectionStrings.ConnectionStrings[name] != null)
//////            {
//////                _config.ConnectionStrings.ConnectionStrings.Remove(name);
//////                SaveConfig();
//////            }
//////        }

//////        private void SaveConfig()
//////        {
//////            _config.Save(ConfigurationSaveMode.Minimal);
//////        }

//////        private void ThenConnectionStringDoesNotExist(string name)
//////        {
//////            var connectionString = _config.ConnectionStrings.ConnectionStrings[name];
//////            Assert.IsNull(connectionString);
//////        }

//////        private void ThenConnectionStringIs(string name, string value)
//////        {
//////            var connectionString = _config.ConnectionStrings.ConnectionStrings[name];
//////            Assert.IsNotNull(connectionString);
//////            Assert.AreEqual(value, connectionString.ConnectionString);
//////        }

//////        private void ThenTaskFailed()
//////        {
//////            Assert.IsFalse(_result);
//////            Assert.IsTrue(_task.Log.HasLoggedErrors);
//////        }

//////        private void ThenTaskSucceeded()
//////        {
//////            Assert.IsTrue(_result);
//////        }

//////        private void ThenAppSettingDoesNotExist(string name)
//////        {
//////            var connectionString = _config.AppSettings.Settings[name];
//////            Assert.IsNull(connectionString);
//////        }

//////        private void ThenAppSettingIs(string appSettingName, string appSettingValue)
//////        {
//////            var setting = _config.AppSettings.Settings[appSettingName];
//////            Assert.IsNotNull(setting);
//////            Assert.AreEqual(appSettingValue, setting.Value);
//////        }

//////        private void WhenExecutingTask()
//////        {
//////            _result = _task.Execute();
//////            ReloadConfig();
//////        }

//////        private void WhenRemovingAppSetting(string name)
//////        {
//////            _task.SettingName = name;
//////            _task.TaskAction = DotNetFramework.RemoveAppSettingTaskAction;
//////            WhenExecutingTask();
//////        }

//////        private void WhenRemovingConnectionString(string name)
//////        {
//////            _task.SettingName = name;
//////            _task.TaskAction = DotNetFramework.RemoveConnectionStringTaskAction;
//////            WhenExecutingTask();
//////        }

//////        private void WhenSettingConnectionString(string name, string value)
//////        {
//////            _task.TaskAction = DotNetFramework.SetConnectionStringTaskAction;
//////            _task.SettingName = name;
//////            _task.SettingValue = value;
//////            WhenExecutingTask();
//////        }

//////        private void WhenSettingAppSetting(string settingName, string appSettingValue)
//////        {
//////            _task.TaskAction = DotNetFramework.SetAppSettingTaskAction;
//////            _task.SettingName = settingName;
//////            _task.SettingValue = appSettingValue;
//////            WhenExecutingTask();
//////        }
//////    }
//////}

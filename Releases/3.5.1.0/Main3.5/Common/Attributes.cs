//-----------------------------------------------------------------------
// <copyright file="Attributes.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack
{
	using System;

	/// <summary>
	/// Provides attribute for integration with MSBuild Sidekick v2. Specifies task action for which task parameter is relevant
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class TaskActionAttribute : Attribute
	{		
		/// <summary>
		/// Sets the task action
		/// </summary>
		public string TaskAction
		{
			get;
			set;
		}

		/// <summary>
		/// Indicates if task parameter is required for specified task action
		/// </summary>
		public bool Required
		{
			get;
			set;
		}

		/// <summary>
		/// Specifies task action for which task parameter is relevant
		/// </summary>
		/// <param name="taskAction">Task action for which task parameter is relevant</param>
		/// <param name="required">Indicates if task parameter is required for specified task action</param>
		public TaskActionAttribute(string taskAction, bool required)
		{			
			TaskAction = taskAction;
			Required = required;
		}
	}

	/// <summary>
	/// Provides attribute for integration with MSBuild Sidekick v2. Specifies help url for the task
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class HelpUrlAttribute : Attribute
	{

		/// <summary>
		/// Sets the help url
		/// </summary>
		public string Url
		{
			get;
			set;
		}

		/// <summary>
		/// Specifies help url for the task
		/// </summary>
		/// <param name="url">Help url</param>
		public HelpUrlAttribute(string url)
		{
			Url = url;
		}
	}

	/// <summary>
	/// Provides attribute for integration with MSBuild Sidekick v2. Specify entry for the parameter dropdown list
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class DropdownValueAttribute : Attribute
	{
		/// <summary>
		/// Sets the dropdown entry value
		/// </summary>
		public string Value
		{
			get;
			set;
		}

		/// <summary>
		/// Specify entry for the parameter dropdown list
		/// </summary>
		/// <param name="value">Entry value</param>
		public DropdownValueAttribute(string value)
		{
			Value = value;
		}
	}

}
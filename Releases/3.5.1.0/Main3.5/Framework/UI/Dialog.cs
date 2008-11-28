//-----------------------------------------------------------------------
// <copyright file="Dialog.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.UI
{
    using System.Globalization;
    using Microsoft.Build.Framework;
    using MSBuild.ExtensionPack.UI.Extended;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Show</i> (<b>Required: </b>Text <b>Optional: </b>Title, Height, Width, Button1Text, Button2Text, Button3Text, MessageColour, MessageBold <b>Output: </b>ButtonClickedText)</para>
    /// <para><i>Prompt</i> (<b>Required: </b>Text <b>Optional: </b>Title, Height, Width, Button1Text, Button2Text, Button3Text, MessageColour, MessageBold, MaskText <b>Output: </b>ButtonClickedText, UserText)</para>
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
    ///         <!-- A simple message -->
    ///         <MSBuild.ExtensionPack.UI.Dialog TaskAction="Show" Text="Hello MSBuild">
    ///             <Output TaskParameter="ButtonClickedText" PropertyName="Clicked"/>
    ///         </MSBuild.ExtensionPack.UI.Dialog>
    ///         <Message Text="User Clicked: $(Clicked)"/>
    ///         <!-- A longer message with a few more attributes set -->
    ///         <MSBuild.ExtensionPack.UI.Dialog TaskAction="Show" Title="A Longer Message" MessageBold="True" Button2Text="Cancel" MessageColour="Green" Height="300" Width="600" Text="Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Cras vitae velit. Pellentesque malesuada diam eget sem. Praesent vestibulum. Donec egestas, quam at viverra volutpat, eros nulla gravida nisi, sed bibendum metus mauris ut diam. Aliquam interdum lacus nec quam. Etiam porta, elit sed pretium vestibulum, nisi dui condimentum enim, ut rhoncus ipsum leo nec est. Nullam congue velit id ligula. Sed imperdiet bibendum pede. In hac habitasse platea dictumst. Praesent eleifend, elit quis convallis aliquam, mi arcu feugiat sem, at blandit mauris nisi eget mauris.">
    ///             <Output TaskParameter="ButtonClickedText" PropertyName="Clicked"/>
    ///         </MSBuild.ExtensionPack.UI.Dialog>
    ///         <Message Text="User Clicked: $(Clicked)"/>
    ///         <!-- A simple prompt for input -->
    ///         <MSBuild.ExtensionPack.UI.Dialog TaskAction="Prompt" Title="Information Required" Button2Text="Cancel" Text="Please enter your Name below">
    ///             <Output TaskParameter="ButtonClickedText" PropertyName="Clicked"/>
    ///             <Output TaskParameter="UserText" PropertyName="Typed"/>
    ///         </MSBuild.ExtensionPack.UI.Dialog>
    ///         <Message Text="User Clicked: $(Clicked)"/>
    ///         <Message Text="User Typed: $(Typed)"/>
    ///         <!-- A prompt for password input -->
    ///         <MSBuild.ExtensionPack.UI.Dialog TaskAction="Prompt" Title="Sensitive Information Required" Button2Text="Cancel" Text="Please enter your Password below" MessageColour="Red" MaskText="true">
    ///             <Output TaskParameter="ButtonClickedText" PropertyName="Clicked"/>
    ///             <Output TaskParameter="UserText" PropertyName="Typed"/>
    ///         </MSBuild.ExtensionPack.UI.Dialog>
    ///         <Message Text="User Clicked: $(Clicked)"/>
    ///         <Message Text="User Typed: $(Typed)"/>
    ///     </Target >
    /// </Project>
    /// ]]></code>
    /// </example>
    public class Dialog : BaseTask
    {
        private const string cShowTaskAction = "Show";
        private const string cPromptTaskAction = "Prompt";

        private string title = "Message";
        private int height = 180;
        private int width = 400;
        private string button1Text = "Ok";

        [DropdownValue(cShowTaskAction)]
        [DropdownValue(cPromptTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }

        }

        /// <summary>
        /// Sets the height of the form. Default is 400
        /// </summary>
        [TaskAction(cShowTaskAction, false)]
        [TaskAction(cPromptTaskAction, false)]
        public int Height
        {
            get { return this.height; }
            set { this.height = value; }
        }

        /// <summary>
        /// Sets the width of the form. Default is 180
        /// </summary>
        [TaskAction(cShowTaskAction, false)]
        [TaskAction(cPromptTaskAction, false)]
        public int Width
        {
            get { return this.width; }
            set { this.width = value; }
        }

        /// <summary>
        /// Sets the text for Button1. Default is 'Ok'
        /// </summary>
        [TaskAction(cShowTaskAction, false)]
        [TaskAction(cPromptTaskAction, false)]
        public string Button1Text
        {
            get { return this.button1Text; }
            set { this.button1Text = value; }
        }

        /// <summary>
        /// Sets the text for Button2. If no text is set the button will not be displayed
        /// </summary>
        [TaskAction(cShowTaskAction, false)]
        [TaskAction(cPromptTaskAction, false)]
        public string Button2Text { get; set; }

        /// <summary>
        /// Set the text for Button3. If no text is set the button will not be displayed
        /// </summary>
        [TaskAction(cShowTaskAction, false)]
        [TaskAction(cPromptTaskAction, false)]
        public string Button3Text { get; set; }

        /// <summary>
        /// Sets the text for the message that is displayed
        /// </summary>
        [Required]
        [TaskAction(cShowTaskAction, true)]
        [TaskAction(cPromptTaskAction, true)]
        public string Text { get; set; }

        /// <summary>
        /// Sets the Title of the Dialog. Default is 'Message'
        /// </summary>
        [TaskAction(cShowTaskAction, false)]
        [TaskAction(cPromptTaskAction, false)]
        public string Title
        {
            get { return this.title; }
            set { this.title = value; }
        }

        /// <summary>
        /// Sets the message text colour. Default is ControlText (usually black).
        /// </summary>
        [TaskAction(cShowTaskAction, false)]
        [TaskAction(cPromptTaskAction, false)]
        public string MessageColour { get; set; }

        /// <summary>
        /// Sets whether the message text is bold. Default is false.
        /// </summary>
        [TaskAction(cShowTaskAction, false)]
        [TaskAction(cPromptTaskAction, false)]
        public bool MessageBold { get; set; }

        /// <summary>
        /// Set to true to use the default password character to mask the user input
        /// </summary>
        [TaskAction(cPromptTaskAction, false)]
        public bool MaskText { get; set; }

        /// <summary>
        /// Gets the text of the button that the user clicked
        /// </summary>
        [Output]
        [TaskAction(cShowTaskAction, false)]
        [TaskAction(cPromptTaskAction, false)]
        public string ButtonClickedText { get; set; }

        /// <summary>
        /// Gets the text that the user typed into the Prompt
        /// </summary>
        [Output]
        [TaskAction(cPromptTaskAction, false)]
        public string UserText { get; set; }

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
                case "Show":
                    this.Show();
                    break;
                case "Prompt":
                    this.Prompt();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Show()
        {
            using (MessageForm form = new MessageForm(this.Text, this.MessageColour, this.MessageBold, this.Button1Text, this.Button2Text, this.Button3Text))
            {
                form.Width = this.Width;
                form.Height = this.Height;
                form.Text = this.Title;
                form.ShowDialog();
                this.ButtonClickedText = form.ButtonClickedText;
            }
        }

        private void Prompt()
        {
            using (PromptForm form = new PromptForm(this.Text, this.MessageColour, this.MessageBold, this.Button1Text, this.Button2Text, this.Button3Text, this.MaskText))
            {
                form.Width = this.Width;
                form.Height = this.Height;
                form.Text = this.Title;
                form.ShowDialog();
                this.ButtonClickedText = form.ButtonClickedText;
                this.UserText = form.UserText;
            }
        }
    }
}
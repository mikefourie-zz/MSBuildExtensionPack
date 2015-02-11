//-----------------------------------------------------------------------
// <copyright file="Dialog.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.UI
{
    using System.Globalization;
    using Microsoft.Build.Framework;
    using MSBuild.ExtensionPack.UI.Extended;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Confirm</i> (<b>Required: </b>Text <b>Optional: </b>Title, Height, Width, ConfirmText, ErrorText, ErrorTitle, Button1Text, Button2Text, MaskText <b>Output: </b>ButtonClickedText, UserText)</para>
    /// <para><i>Show</i> (<b>Required: </b>Text <b>Optional: </b>Title, Height, Width, Button1Text, Button2Text, Button3Text, MessageColour, MessageBold <b>Output: </b>ButtonClickedText)</para>
    /// <para><i>Prompt</i> (<b>Required: </b>Text <b>Optional: </b>Title, Height, Width, Button1Text, Button2Text, Button3Text, MessageColour, MessageBold, MaskText <b>Output: </b>ButtonClickedText, UserText)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
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
    ///         <!-- Confirm a Password -->
    ///         <MSBuild.ExtensionPack.UI.Dialog TaskAction="Confirm" Title="Confirmation Required" Button2Text="Cancel" Text="Enter Password" ConfirmText="Confirm Password" MaskText="true">
    ///             <Output TaskParameter="ButtonClickedText" PropertyName="Clicked"/>
    ///             <Output TaskParameter="UserText" PropertyName="Typed"/>
    ///         </MSBuild.ExtensionPack.UI.Dialog>
    ///         <Message Text="User Clicked: $(Clicked)"/>
    ///         <Message Text="User Typed: $(Typed)"/>
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
        private const string ShowTaskAction = "Show";
        private const string PromptTaskAction = "Prompt";
        private const string ConfirmTaskAction = "Confirm";

        private string title = "Message";
        private int height = 180;
        private int width = 400;
        private string button1Text = "OK";
        private string errorTitle = "Error";
        private string errorText = "The supplied values do not match";
        private string confirmText = "Confirm";

        /// <summary>
        /// Sets the height of the form. Default is 180
        /// </summary>
        public int Height
        {
            get { return this.height; }
            set { this.height = value; }
        }

        /// <summary>
        /// Sets the width of the form. Default is 400
        /// </summary>
        public int Width
        {
            get { return this.width; }
            set { this.width = value; }
        }

        /// <summary>
        /// Sets the text for Button1. Default is 'Ok'
        /// </summary>
        public string Button1Text
        {
            get { return this.button1Text; }
            set { this.button1Text = value; }
        }

        /// <summary>
        /// Sets the text for Button2. If no text is set the button will not be displayed
        /// </summary>
        public string Button2Text { get; set; }

        /// <summary>
        /// Set the text for Button3. If no text is set the button will not be displayed
        /// </summary>
        public string Button3Text { get; set; }

        /// <summary>
        /// Sets the text for the message that is displayed
        /// </summary>
        [Required]
        public string Text { get; set; }

        /// <summary>
        /// Sets the title for the error messagebox if Confirm fails. Default is 'Error'
        /// </summary>
        public string ErrorTitle
        {
            get { return this.errorTitle; }
            set { this.errorTitle = value; }
        }

        /// <summary>
        /// Sets the text for the error messagebox if Confirm fails. Default is 'The supplied values do not match'
        /// </summary>
        public string ErrorText
        {
            get { return this.errorText; }
            set { this.errorText = value; }
        }

        /// <summary>
        /// Sets the confirmation text for the message that is displayed. Default is 'Confirm' 
        /// </summary>
        public string ConfirmText
        {
            get { return this.confirmText; }
            set { this.confirmText = value; }
        }
        
        /// <summary>
        /// Sets the Title of the Dialog. Default is 'Message' for Show and Prompt, 'Confirm' for Confirm TaskAction
        /// </summary>
        public string Title
        {
            get { return this.title; }
            set { this.title = value; }
        }

        /// <summary>
        /// Sets the message text colour. Default is ControlText (usually black).
        /// </summary>
        public string MessageColour { get; set; }

        /// <summary>
        /// Sets whether the message text is bold. Default is false.
        /// </summary>
        public bool MessageBold { get; set; }

        /// <summary>
        /// Set to true to use the default password character to mask the user input
        /// </summary>
        public bool MaskText { get; set; }

        /// <summary>
        /// Gets the text of the button that the user clicked
        /// </summary>
        [Output]
        public string ButtonClickedText { get; set; }

        /// <summary>
        /// Gets the text that the user typed into the Prompt
        /// </summary>
        [Output]
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
                case ShowTaskAction:
                    this.Show();
                    break;
                case PromptTaskAction:
                    this.Prompt();
                    break;
                case ConfirmTaskAction:
                    this.Confirm();
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

        private void Confirm()
        {
            using (ConfirmForm form = new ConfirmForm(this.Text, this.ConfirmText, this.ErrorTitle, this.ErrorText, this.Button1Text, this.Button2Text, this.MaskText))
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
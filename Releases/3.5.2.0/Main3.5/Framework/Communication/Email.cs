//-----------------------------------------------------------------------
// <copyright file="Email.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Communication
{
    using System.Globalization;
    using System.Net.Mail;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Send</i> (<b>Required: </b> SmtpServer, MailFrom, MailTo, Subject  <b>Optional: </b> Priority, Body, Format, Attachments)</para>
    /// <para><b>Remote Execution Support:</b> No</para>
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
    ///         <ItemGroup>
    ///             <!-- Specify some attachments -->         
    ///             <Attachment Include="C:\demo.txt"/>
    ///             <Attachment Include="C:\demo2.txt"/>
    ///             <!-- Specify some recipients -->
    ///             <Recipient Include="nospam@freet2odev.com"/>
    ///             <Recipient Include="nospam2@freet2odev.com"/>
    ///         </ItemGroup>
    ///         <MSBuild.ExtensionPack.Communication.Email TaskAction="Send" Subject="Test Email" SmtpServer="yoursmtpserver" MailFrom="nospam@freet2odev.com" MailTo="@(Recipient)" Body="body text" Attachments="@(Attachment)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.1.0/html/2439bba8-d062-a4b9-3ca6-2e348d031ec1.htm")]
    public class Email : BaseTask
    {
        private const string SendTaskAction = "Send";
        
        private string format = "HTML";
        private string priority = "Normal";

        [DropdownValue(SendTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// The SMTP server to use to send the email.
        /// </summary>
        [Required]
        [TaskAction(SendTaskAction, true)]
        public string SmtpServer { get; set; }

        /// <summary>
        /// The email address to send the email from.
        /// </summary>
        [Required]
        [TaskAction(SendTaskAction, true)]
        public string MailFrom { get; set; }

        /// <summary>
        /// Sets the Item Colleciton of email address to send the email to.
        /// </summary>
        [Required]
        [TaskAction(SendTaskAction, true)]
        public ITaskItem[] MailTo { get; set; }

        /// <summary>
        /// The subject of the email.
        /// </summary>
        [Required]
        [TaskAction(SendTaskAction, true)]
        public string Subject { get; set; }

        /// <summary>
        /// The priority of the email. Default is Normal
        /// </summary>
        [TaskAction(SendTaskAction, false)]
        public string Priority
        {
            get { return this.priority; }
            set { this.priority = value; }
        }

        /// <summary>
        /// The body of the email.
        /// </summary>
        [TaskAction(SendTaskAction, false)]
        public string Body { get; set; }

        /// <summary>
        /// Sets the format of the email. Default is HTML
        /// </summary>
        [TaskAction(SendTaskAction, false)]
        public string Format
        {
            get { return this.format; }
            set { this.format = value; }
        }

        /// <summary>
        /// An Item Collection of full paths of files to attach to the email.
        /// </summary>
        [TaskAction(SendTaskAction, false)]
        public ITaskItem[] Attachments { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case "Send":
                    this.Send();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Send()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Sending email: {0}", this.Subject));
            MailMessage msg = new MailMessage { From = new MailAddress(this.MailFrom) };

            foreach (ITaskItem recipient in this.MailTo)
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Adding recipient: {0}", recipient.ItemSpec));
                msg.To.Add(new MailAddress(recipient.ItemSpec));
            }

            if (this.Attachments != null)
            {
                foreach (ITaskItem file in this.Attachments)
                {
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Adding attachment: {0}", file.ItemSpec));
                    Attachment attachment = new Attachment(file.ItemSpec);
                    msg.Attachments.Add(attachment);
                }
            }

            msg.Subject = this.Subject ?? string.Empty;
            msg.Body = this.Body ?? string.Empty;
            if (this.format.ToUpperInvariant() == "HTML")
            {
                msg.IsBodyHtml = true;
            }

            SmtpClient client = new SmtpClient(this.SmtpServer) { UseDefaultCredentials = true };
            client.Send(msg);
        }
    }
}
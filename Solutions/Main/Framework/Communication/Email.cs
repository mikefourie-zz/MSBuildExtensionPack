//-----------------------------------------------------------------------
// <copyright file="Email.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Communication
{
    using System;
    using System.Globalization;
    using System.Net.Mail;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Send</i> (<b>Required: </b> SmtpServer, MailFrom, MailTo, Subject  <b>Optional: </b> Priority, Body, Format, Attachments, UseDefaultCredentials, UserName, UserPassword, Port, EnableSsl)</para>
    /// <para><b>Remote Execution Support:</b> No</para>
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
    public class Email : BaseTask
    {
        private bool useDefaultCredentials = true;
        private string format = "HTML";
        private System.Net.Mail.MailPriority priority = System.Net.Mail.MailPriority.Normal;

        /// <summary>
        /// The SMTP server to use to send the email.
        /// </summary>
        [Required]
        public string SmtpServer { get; set; }

        /// <summary>
        /// Sets the port to use. Ignored if not specified.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Sets whether to EnableSsl
        /// </summary>
        public bool EnableSsl { get; set; }

        /// <summary>
        /// The email address to send the email from.
        /// </summary>
        [Required]
        public string MailFrom { get; set; }

        /// <summary>
        /// Sets the Item Colleciton of email address to send the email to.
        /// </summary>
        [Required]
        public ITaskItem[] MailTo { get; set; }

        /// <summary>
        /// The subject of the email.
        /// </summary>
        [Required]
        public string Subject { get; set; }

        /// <summary>
        /// The priority of the email. Default is Normal (also available High and Low).
        /// </summary>
        public string Priority
        {
            get { return this.priority.ToString(); }
            set { this.priority = (System.Net.Mail.MailPriority)Enum.Parse(typeof(System.Net.Mail.MailPriority), value); }
        }

        /// <summary>
        /// The body of the email.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Sets the format of the email. Default is HTML
        /// </summary>
        public string Format
        {
            get { return this.format; }
            set { this.format = value; }
        }

        /// <summary>
        /// Gets or sets a Boolean value that controls whether the DefaultCredentials are sent with requests. DefaultCredentials represents the system credentials for the current security context in which the application is running. Default is true.
        /// <para>If UserName and UserPassword is supplied, this is set to false. If UserName and UserPassword are not supplied and this is set to false then mail is sent to the server anonymously.</para>
        /// <para><b>If you provide credentials for basic authentication, they are sent to the server in clear text. This can present a security issue because your credentials can be seen, and then used by others.</b></para>
        /// </summary>
        public bool UseDefaultCredentials
        {
            get { return this.useDefaultCredentials; }
            set { this.useDefaultCredentials = value; }
        }

        /// <summary>
        /// An Item Collection of full paths of files to attach to the email.
        /// </summary>
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
            using (MailMessage msg = new MailMessage())
            {
                msg.From = new MailAddress(this.MailFrom);
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
                msg.Priority = this.priority;
                msg.Body = this.Body ?? string.Empty;
                if (this.format.ToUpperInvariant() == "HTML")
                {
                    msg.IsBodyHtml = true;
                }

                using (SmtpClient client = new SmtpClient(this.SmtpServer))
                {
                    if (this.Port > 0)
                    {
                        client.Port = this.Port;
                    }

                    client.EnableSsl = this.EnableSsl;
                    client.UseDefaultCredentials = this.UseDefaultCredentials;
                    if (!string.IsNullOrEmpty(this.UserName))
                    {
                        client.Credentials = new System.Net.NetworkCredential(this.UserName, this.UserPassword);
                    }

                    client.Send(msg);
                }
            }
        }
    }
}
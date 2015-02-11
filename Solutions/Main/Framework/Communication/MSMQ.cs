//-----------------------------------------------------------------------
// <copyright file="MSMQ.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Communication
{
    using System;
    using System.Globalization;
    using System.Messaging;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Create</i> (<b>Required: </b>  Path <b>Optional: Label, Transactional, Authenticated, MaximumQueueSize, MaximumJournalSize, UseJournalQueue, Force, Privacy</b> )</para>
    /// <para><i>CheckExists</i> (<b>Required: </b>  Path <b>Output: Exists</b> )</para>
    /// <para><i>Delete</i> (<b>Required: </b>  Path <b>Optional: </b> )</para>
    /// <para><i>Send</i> (<b>Required: </b>  Path <b>Optional: Message, Label</b> )</para>
    /// <para><i>SetPermissions</i> (<b>Required: </b>  Path <b>Optional: Allow, Deny, Revoke, Set</b> )</para>
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
    ///             <Allow Include="TFS">
    ///                 <Permissions>DeleteMessage,ReceiveMessage</Permissions>
    ///             </Allow>
    ///             <Deny Include="TFS">
    ///                 <Permissions>GetQueueProperties</Permissions>
    ///             </Deny>
    ///         </ItemGroup>
    ///         <!-- Create queue -->
    ///         <MSBuild.ExtensionPack.Communication.MSMQ TaskAction="Create" Path=".\private$\3" Label="Test Queue" Force="true"/>
    ///         <!-- Check if the queue exists -->
    ///         <MSBuild.ExtensionPack.Communication.MSMQ TaskAction="CheckExists" Path=".\private$\3">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Communication.MSMQ>
    ///         <Message Text="Exists: $(DoesExist)"/>
    ///         <!-- Delete the queue -->
    ///         <MSBuild.ExtensionPack.Communication.MSMQ TaskAction="Delete" Path=".\private$\3"/>
    ///         <!-- Check if the queue exists -->
    ///         <MSBuild.ExtensionPack.Communication.MSMQ TaskAction="CheckExists" Path=".\private$\3">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Communication.MSMQ>
    ///         <Message Text="Exists: $(DoesExist)"/>
    ///         <!-- Delete the queue again to see that no error is thrown -->
    ///         <MSBuild.ExtensionPack.Communication.MSMQ TaskAction="Delete" Path=".\private$\3"/>
    ///         <!-- Create queue -->
    ///         <MSBuild.ExtensionPack.Communication.MSMQ TaskAction="Create" Path=".\private$\3" Label="Test Queue" Force="true" Transactional="false" Authenticated="" MaximumQueueSize="220"/>
    ///         <!-- Send Message -->
    ///         <MSBuild.ExtensionPack.Communication.MSMQ TaskAction="Send" Path=".\private$\3" Message="Mike" Label="Hi2"/>
    ///         <!-- Send Message -->
    ///         <MSBuild.ExtensionPack.Communication.MSMQ TaskAction="Send" Path=".\private$\3" Message="" Label=""/>
    ///         <!-- Set permissions -->
    ///         <MSBuild.ExtensionPack.Communication.MSMQ TaskAction="SetPermissions" Path=".\private$\3" Allow="@(Allow)" Deny="@(Deny)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class MSMQ : BaseTask
    {
        private const string CreateTaskAction = "Create";
        private const string DeleteTaskAction = "Delete";
        private const string CheckExistsTaskAction = "CheckExists";
        private const string SendTaskAction = "Send";
        private const string SetPermissionsTaskAction = "SetPermissions";
        private System.Messaging.EncryptionRequired privacy = System.Messaging.EncryptionRequired.Optional;

        /// <summary>
        /// Sets the path of the queue. Required.
        /// </summary>
        [Required]
        public string Path { get; set; }

        /// <summary>
        /// Sets the Label of the queue 
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Sets the Message to send to the queue
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// An access-allowed entry that causes the new rights to be added to any existing rights the trustee has. Permission metadata supports: DeleteMessage, PeekMessage, WriteMessage, DeleteJournalMessage, SetQueueProperties, GetQueueProperties, DeleteQueue, GetQueuePermissions, ChangeQueuePermissions, TakeQueueOwnership, ReceiveMessage, ReceiveJournalMessage, GenericRead, GenericWrite, FullControl
        /// </summary>
        public ITaskItem[] Allow { get; set; }

        /// <summary>
        /// An access-denied entry that denies the specified rights in addition to any currently denied rights of the trustee. Permission metadata supports: DeleteMessage, PeekMessage, WriteMessage, DeleteJournalMessage, SetQueueProperties, GetQueueProperties, DeleteQueue, GetQueuePermissions, ChangeQueuePermissions, TakeQueueOwnership, ReceiveMessage, ReceiveJournalMessage, GenericRead, GenericWrite, FullControl
        /// </summary>
        public ITaskItem[] Deny { get; set; }
        
        /// <summary>
        /// An entry that removes all existing allowed or denied rights for the specified trustee. Permission metadata supports: DeleteMessage, PeekMessage, WriteMessage, DeleteJournalMessage, SetQueueProperties, GetQueueProperties, DeleteQueue, GetQueuePermissions, ChangeQueuePermissions, TakeQueueOwnership, ReceiveMessage, ReceiveJournalMessage, GenericRead, GenericWrite, FullControl
        /// </summary>
        public ITaskItem[] Revoke { get; set; }

        /// <summary>
        /// An access-allowed entry that is similar to Allow, except that the new entry allows only the specified rights. Using it discards any existing rights, including all existing access-denied entries for the trustee. Permission metadata supports: DeleteMessage, PeekMessage, WriteMessage, DeleteJournalMessage, SetQueueProperties, GetQueueProperties, DeleteQueue, GetQueuePermissions, ChangeQueuePermissions, TakeQueueOwnership, ReceiveMessage, ReceiveJournalMessage, GenericRead, GenericWrite, FullControl
        /// </summary>
        public ITaskItem[] Set { get; set; }

        /// <summary>
        /// Set true to create a transactional queue; false to create a non-transactional queue. Default is false;
        /// </summary>
        public bool Transactional { get; set; }

        /// <summary>
        /// Set to try to create an Authenticated queueu. Default is false
        /// </summary>
        public bool Authenticated { get; set; }

        /// <summary>
        /// Set to true to recreate a queue if it already exists
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Sets the maximum queue size in kb.
        /// </summary>
        public int MaximumQueueSize { get; set; }

        /// <summary>
        /// Sets the maximum journal size in kb.
        /// </summary>
        public int MaximumJournalSize { get; set; }

        /// <summary>
        /// Set to true to use the journal queue
        /// </summary>
        public bool UseJournalQueue { get; set; }

        /// <summary>
        /// You can specify whether the queue accepts private (encrypted) messages, non-private (non-encrypted) messages, or both. Supports Optional (default), None, Both.
        /// </summary>
        public string Privacy
        {
            get { return this.privacy.ToString(); }
            set { this.privacy = (System.Messaging.EncryptionRequired)Enum.Parse(typeof(System.Messaging.EncryptionRequired), value); }
        }

        /// <summary>
        /// Gets whether the queue exists
        /// </summary>
        [Output]
        public bool Exists { get; set; }
        
        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case CreateTaskAction:
                    this.Create();
                    break;
                case CheckExistsTaskAction:
                    this.CheckExists();
                    break;
                case DeleteTaskAction:
                    this.Delete();
                    break;
                case SendTaskAction:
                    this.Send();
                    break;
                case SetPermissionsTaskAction:
                    this.SetPermissions();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void SetPermissions()
        {
            if (System.Messaging.MessageQueue.Exists(this.Path))
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting permissions on queue: {0}", this.Path));
                using (System.Messaging.MessageQueue queue = new System.Messaging.MessageQueue(this.Path))
                {
                    if (this.Allow != null)
                    {
                        foreach (ITaskItem i in this.Allow)
                        {
                            MessageQueueAccessRights permission = (MessageQueueAccessRights)Enum.Parse(typeof(MessageQueueAccessRights), i.GetMetadata("Permissions"), true);
                            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Allow permission for user {0} - {1}", i.ItemSpec, i.GetMetadata("Permissions")));
                            queue.SetPermissions(i.ItemSpec, permission, AccessControlEntryType.Allow);
                        }
                    }

                    if (this.Deny != null)
                    {
                        foreach (ITaskItem i in this.Deny)
                        {
                            MessageQueueAccessRights permission = (MessageQueueAccessRights)Enum.Parse(typeof(MessageQueueAccessRights), i.GetMetadata("Permissions"), true);
                            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Deny permission for user {0} - {1}", i.ItemSpec, i.GetMetadata("Permissions")));
                            queue.SetPermissions(i.ItemSpec, permission, AccessControlEntryType.Deny);
                        }
                    }

                    if (this.Set != null)
                    {
                        foreach (ITaskItem i in this.Set)
                        {
                            MessageQueueAccessRights permission = (MessageQueueAccessRights)Enum.Parse(typeof(MessageQueueAccessRights), i.GetMetadata("Permissions"), true);
                            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Set permission for user {0} - {1}", i.ItemSpec, i.GetMetadata("Permissions")));
                            queue.SetPermissions(i.ItemSpec, permission, AccessControlEntryType.Set);
                        }
                    }

                    if (this.Revoke != null)
                    {
                        foreach (ITaskItem i in this.Revoke)
                        {
                            MessageQueueAccessRights permission = (MessageQueueAccessRights)Enum.Parse(typeof(MessageQueueAccessRights), i.GetMetadata("Permissions"), true);
                            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Revoke permission for user {0} - {1}", i.ItemSpec, i.GetMetadata("Permissions")));
                            queue.SetPermissions(i.ItemSpec, permission, AccessControlEntryType.Revoke);
                        }
                    }
                }
            }
            else
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Queue not found: {0}", this.Path));
            }
        }

        private void Create()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating queue: {0}", this.Path));
            if (System.Messaging.MessageQueue.Exists(this.Path))
            {
                if (this.Force)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting existing queue: {0}", this.Path));
                    System.Messaging.MessageQueue.Delete(this.Path);
                }
                else
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Queue already exists. Use Force=\"true\" to delete the existing queue: {0}", this.Path));
                    return;
                }
            }

            using (System.Messaging.MessageQueue q = System.Messaging.MessageQueue.Create(this.Path, this.Transactional))
            {
                if (!string.IsNullOrEmpty(this.Label))
                {
                    q.Label = this.Label;
                }

                if (this.Authenticated)
                {
                    q.Authenticate = true;
                }

                if (this.MaximumQueueSize > 0)
                {
                    q.MaximumQueueSize = this.MaximumQueueSize;
                }

                if (this.UseJournalQueue)
                {
                    q.UseJournalQueue = true;

                    if (this.MaximumJournalSize > 0)
                    {
                        q.MaximumJournalSize = this.MaximumJournalSize;
                    }
                }

                q.EncryptionRequired = this.privacy;
            }
        }

        private void CheckExists()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking whether queue exists: {0}", this.Path));
            this.Exists = System.Messaging.MessageQueue.Exists(this.Path);
        }

        private void Send()
        {
            if (System.Messaging.MessageQueue.Exists(this.Path))
            {
                if (string.IsNullOrEmpty(this.Label))
                {
                    this.Label = string.Empty;
                }

                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Sending message to queue: [{0}] - {1}", this.Path, this.Message));

                // Connect to a queue on the local computer.
                using (System.Messaging.MessageQueue queue = new System.Messaging.MessageQueue(this.Path))
                {
                    // Send a message to the queue.
                    if (this.Transactional)
                    {
                        // Create a transaction.
                        using (MessageQueueTransaction myTransaction = new MessageQueueTransaction())
                        {
                            // Begin the transaction.
                            myTransaction.Begin();

                            // Send the message.
                            queue.Send(this.Message, this.Label, myTransaction);

                            // Commit the transaction.
                            myTransaction.Commit();
                        }
                    }
                    else
                    {
                        queue.Send(this.Message, this.Label);
                    }
                }
            }
            else
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Queue not found: {0}", this.Path));
            }
        }

        private void Delete()
        {
            if (System.Messaging.MessageQueue.Exists(this.Path))
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting queue: {0}", this.Path));
                System.Messaging.MessageQueue.Delete(this.Path);
            }
        }
    }
}
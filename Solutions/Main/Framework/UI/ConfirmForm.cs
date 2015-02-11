//-----------------------------------------------------------------------
// <copyright file="ConfirmForm.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.UI.Extended
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// PromptForm
    /// </summary>
    public partial class ConfirmForm : Form
    {
        private readonly string errorTitle;
        private readonly string errorText;
        private string buttonClickedText = "None";
        private string userText = string.Empty;

        public ConfirmForm()
        {
            this.InitializeComponent();
        }

        public ConfirmForm(string messageText, string message2Text, string errorTitle, string errorText, string button1Text, string button2Text, bool maskText)
        {
            this.InitializeComponent();
            this.labelText.Text = messageText;
            this.label2Text.Text = message2Text;
            this.errorTitle = errorTitle;
            this.errorText = errorText;

            if (maskText)
            {
                this.textBoxFirst.UseSystemPasswordChar = true;
                this.textBoxSecond.UseSystemPasswordChar = true;
            }

            if (!string.IsNullOrEmpty(button1Text))
            {
                this.button1.Visible = true;
                this.button1.Text = button1Text;
            }

            if (!string.IsNullOrEmpty(button2Text))
            {
                this.button2.Visible = true;
                this.button2.Text = button2Text;
            }
        }

        public string ButtonClickedText
        {
            get { return this.buttonClickedText; }
            set { this.buttonClickedText = value; }
        }

        public string UserText
        {
            get { return this.userText; }
            set { this.userText = value; }
        }

        private void Button1_Click(object sender, System.EventArgs e)
        {
            if (string.Compare(this.textBoxFirst.Text, this.textBoxSecond.Text, StringComparison.Ordinal) != 0)
            {
                MessageBox.Show(this.errorText, this.errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.ButtonClickedText = this.button1.Text;
            this.UserText = this.textBoxFirst.Text;
            this.Close();
        }

        private void Button2_Click(object sender, System.EventArgs e)
        {
            this.ButtonClickedText = this.button2.Text;
            this.UserText = this.textBoxFirst.Text;
            this.Close();
        }
    }
}
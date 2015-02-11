//-----------------------------------------------------------------------
// <copyright file="MessageForm.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.UI.Extended
{
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// MessageForm
    /// </summary>
    public partial class MessageForm : Form
    {
        private string buttonClickedText = "None";

        public MessageForm()
        {
            this.InitializeComponent();
        }

        public MessageForm(string messageText, string messageColour, bool messageBold, string button1Text, string button2Text, string button3Text)
        {
            this.InitializeComponent();
            this.labelText.Text = messageText;
            
            if (messageBold)
            {
                this.labelText.Font = new Font(this.labelText.Font, FontStyle.Bold);
            }
            
            if (!string.IsNullOrEmpty(messageColour))
            {
                this.labelText.ForeColor = Color.FromName(messageColour);
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

            if (!string.IsNullOrEmpty(button3Text))
            {
                this.button3.Visible = true;
                this.button3.Text = button3Text;
            }
        }

        public string ButtonClickedText
        {
            get { return this.buttonClickedText; }
            set { this.buttonClickedText = value; }
        }

        private void Button1_Click(object sender, System.EventArgs e)
        {
            this.ButtonClickedText = this.button1.Text;
            this.Close();
        }

        private void Button2_Click(object sender, System.EventArgs e)
        {
            this.ButtonClickedText = this.button2.Text;
            this.Close();
        }

        private void Button3_Click(object sender, System.EventArgs e)
        {
            this.ButtonClickedText = this.button3.Text;
            this.Close();
        }
    }
}

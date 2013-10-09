namespace MSBuild.ExtensionPack.Computer.Extended
{
    partial class GetPasswordForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GetPasswordForm));
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOk = new System.Windows.Forms.Button();
            this.textBoxPassword = new System.Windows.Forms.TextBox();
            this.labelPassword = new System.Windows.Forms.Label();
            this.checkBoxMask = new System.Windows.Forms.CheckBox();
            this.pictureBoxOpenLock = new System.Windows.Forms.PictureBox();
            this.pictureBoxLock = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxOpenLock)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLock)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonCancel.Location = new System.Drawing.Point(210, 60);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 2;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // buttonOk
            // 
            this.buttonOk.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonOk.Location = new System.Drawing.Point(291, 60);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.TabIndex = 1;
            this.buttonOk.Text = "&OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.ButtonOk_Click);
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.BackColor = System.Drawing.Color.White;
            this.textBoxPassword.Location = new System.Drawing.Point(79, 25);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.Size = new System.Drawing.Size(287, 20);
            this.textBoxPassword.TabIndex = 0;
            this.textBoxPassword.UseSystemPasswordChar = true;
            this.textBoxPassword.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TextBoxPassword_KeyUp);
            // 
            // labelPassword
            // 
            this.labelPassword.AutoSize = true;
            this.labelPassword.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPassword.Location = new System.Drawing.Point(76, 9);
            this.labelPassword.Name = "labelPassword";
            this.labelPassword.Size = new System.Drawing.Size(61, 13);
            this.labelPassword.TabIndex = 5;
            this.labelPassword.Text = "Password";
            // 
            // checkBoxMask
            // 
            this.checkBoxMask.AutoSize = true;
            this.checkBoxMask.Checked = true;
            this.checkBoxMask.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxMask.Location = new System.Drawing.Point(314, 9);
            this.checkBoxMask.Name = "checkBoxMask";
            this.checkBoxMask.Size = new System.Drawing.Size(52, 17);
            this.checkBoxMask.TabIndex = 3;
            this.checkBoxMask.Text = "Mask";
            this.checkBoxMask.UseVisualStyleBackColor = true;
            this.checkBoxMask.CheckedChanged += new System.EventHandler(this.CheckBoxMask_CheckedChanged);
            // 
            // pictureBoxOpenLock
            // 
            this.pictureBoxOpenLock.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxOpenLock.Image")));
            this.pictureBoxOpenLock.Location = new System.Drawing.Point(12, 9);
            this.pictureBoxOpenLock.Name = "pictureBoxOpenLock";
            this.pictureBoxOpenLock.Size = new System.Drawing.Size(48, 48);
            this.pictureBoxOpenLock.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBoxOpenLock.TabIndex = 6;
            this.pictureBoxOpenLock.TabStop = false;
            this.pictureBoxOpenLock.Visible = false;
            // 
            // pictureBoxLock
            // 
            this.pictureBoxLock.ErrorImage = ((System.Drawing.Image)(resources.GetObject("pictureBoxLock.ErrorImage")));
            this.pictureBoxLock.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxLock.Image")));
            this.pictureBoxLock.Location = new System.Drawing.Point(12, 9);
            this.pictureBoxLock.Name = "pictureBoxLock";
            this.pictureBoxLock.Size = new System.Drawing.Size(48, 48);
            this.pictureBoxLock.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBoxLock.TabIndex = 7;
            this.pictureBoxLock.TabStop = false;
            // 
            // GetPasswordForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(378, 95);
            this.Controls.Add(this.pictureBoxLock);
            this.Controls.Add(this.pictureBoxOpenLock);
            this.Controls.Add(this.textBoxPassword);
            this.Controls.Add(this.checkBoxMask);
            this.Controls.Add(this.labelPassword);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.buttonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GetPasswordForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GetPasswordForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxOpenLock)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLock)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.Label labelPassword;
        private System.Windows.Forms.CheckBox checkBoxMask;
        private System.Windows.Forms.PictureBox pictureBoxOpenLock;
        private System.Windows.Forms.PictureBox pictureBoxLock;
    }
}
namespace CrossworldsModManager
{
    partial class AboutForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.labelAppName = new System.Windows.Forms.Label();
            this.labelInfo = new System.Windows.Forms.Label();
            this.buttonOK = new System.Windows.Forms.Button();
            this.labelIconCredit = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelAppName
            // 
            this.labelAppName.AutoSize = true;
            this.labelAppName.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAppName.ForeColor = System.Drawing.Color.White;
            this.labelAppName.Location = new System.Drawing.Point(12, 9);
            this.labelAppName.Name = "labelAppName";
            this.labelAppName.Size = new System.Drawing.Size(327, 20);
            this.labelAppName.TabIndex = 0;
            this.labelAppName.Text = "Sonic Racing: Crossworlds Mod Manager";
            // 
            // labelInfo
            // 
            this.labelInfo.ForeColor = System.Drawing.Color.Gainsboro;
            this.labelInfo.Location = new System.Drawing.Point(13, 40);
            this.labelInfo.Name = "labelInfo";
            this.labelInfo.Size = PlatformUtils.IsLinux ? new System.Drawing.Size(359, 145) : new System.Drawing.Size(359, 115);
            this.labelInfo.TabIndex = 1;
            if (PlatformUtils.IsLinux)
            {
                this.labelInfo.Text = "Coded by RED1 in C# using .NET and Windows Forms.\r\nPorted to Linux by AntiApple4life\r\n\r\nAcknowledgements:\r\nLocResUti" +
                                      "lity by anubi47 (github.com/anubi47/LocResUtility)\r\nrepak by trumank (https://github.com/trumank/repak)\r\nSharpCompress" +
                                      " (https://github.com/adamhathcock/sharpcompress)\r\n\r\nSpecial Thanks:\r\nWindows Forms Linux port by DanielVanNoord\r\nhttps://github.com/DanielVanNoord/System.Windows.Forms\r\n\r\nThis application is not affiliated with Sega or Epic Games.";
            }
            else
            {
                this.labelInfo.Text = "Coded by RED1 in C# using .NET and Windows Forms.\r\n\r\nAcknowledgements:\r\nLocResUti" +
                                      "lity by anubi47 (github.com/anubi47/LocResUtility)\r\nrepak by trumank (https://github.com/trumank/repak)\r\nSharpCompress" +
                                      " (https://github.com/adamhathcock/sharpcompress)\r\n\r\nThis application is not affiliated with Sega or Epic Games.";
            }
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(70)))));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.FlatAppearance.BorderSize = 0;
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonOK.ForeColor = System.Drawing.Color.White;
            this.buttonOK.Location = PlatformUtils.IsLinux ? new System.Drawing.Point(297, 205) : new System.Drawing.Point(297, 165);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 2;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = false;
            // 
            // labelIconCredit
            // 
            this.labelIconCredit.AutoSize = true;
            this.labelIconCredit.ForeColor = System.Drawing.Color.Gainsboro;
            this.labelIconCredit.Location = PlatformUtils.IsLinux ? new System.Drawing.Point(13, 205) : new System.Drawing.Point(13, 165);
            this.labelIconCredit.Name = "labelIconCredit";
            this.labelIconCredit.Size = new System.Drawing.Size(75, 13);
            this.labelIconCredit.TabIndex = 3;
            this.labelIconCredit.Text = "Icon by Derpy";
            // 
            // AboutForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.ClientSize = PlatformUtils.IsLinux ? new System.Drawing.Size(384, 240) : new System.Drawing.Size(384, 200);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.labelIconCredit);
            this.Controls.Add(this.labelInfo);
            this.Controls.Add(this.labelAppName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelAppName;
        private System.Windows.Forms.Label labelInfo;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Label labelIconCredit;
    }
}

namespace CrossworldsModManager
{
    partial class SettingsForm
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
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.tabOptions = new System.Windows.Forms.TabPage();
            this.tabAdvanced = new System.Windows.Forms.TabPage();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbPlatformType = new System.Windows.Forms.ComboBox();
            this.txtGameDir = new System.Windows.Forms.TextBox();
            this.btnBrowseGameDir = new System.Windows.Forms.Button();
            this.btnBrowseModsDir = new System.Windows.Forms.Button();
            this.txtModsDir = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lblTheme = new System.Windows.Forms.Label();
            this.cmbTheme = new System.Windows.Forms.ComboBox();
            this.btnCustomizeTheme = new System.Windows.Forms.Button();
            this.chkSortEnabled = new System.Windows.Forms.CheckBox();
            this.chkAutoClean = new System.Windows.Forms.CheckBox();
            this.chkCheckForGames = new System.Windows.Forms.CheckBox();
            this.chkAutoCloseLog = new System.Windows.Forms.CheckBox();
            this.chkDeveloperMode = new System.Windows.Forms.CheckBox();
            this.chkDoNotBackup = new System.Windows.Forms.CheckBox();
            this.chkDoNotConfirmEnableDisable = new System.Windows.Forms.CheckBox();
            this.chkDoNotWarnUnsavedChanges = new System.Windows.Forms.CheckBox();
            this.lblAesKey = new System.Windows.Forms.Label();
            this.txtAesKey = new System.Windows.Forms.TextBox();
            this.tabControl.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.tabOptions.SuspendLayout();
            this.tabAdvanced.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabGeneral);
            this.tabControl.Controls.Add(this.tabOptions);
            this.tabControl.Controls.Add(this.tabAdvanced);
            this.tabControl.Location = new System.Drawing.Point(6, 6);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(566, 224);
            this.tabControl.TabIndex = 0;
            // 
            // tabGeneral
            // 
            this.tabGeneral.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.tabGeneral.Controls.Add(this.label1);
            this.tabGeneral.Controls.Add(this.cmbPlatformType);
            this.tabGeneral.Controls.Add(this.txtGameDir);
            this.tabGeneral.Controls.Add(this.btnBrowseGameDir);
            this.tabGeneral.Controls.Add(this.btnBrowseModsDir);
            this.tabGeneral.Controls.Add(this.txtModsDir);
            this.tabGeneral.Controls.Add(this.label2);
            this.tabGeneral.Controls.Add(this.lblTheme);
            this.tabGeneral.Controls.Add(this.cmbTheme);
            this.tabGeneral.Controls.Add(this.btnCustomizeTheme);
            this.tabGeneral.Location = new System.Drawing.Point(4, 22);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tabGeneral.Size = new System.Drawing.Size(558, 198);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            // 
            // tabOptions
            // 
            this.tabOptions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.tabOptions.Controls.Add(this.chkSortEnabled);
            this.tabOptions.Controls.Add(this.chkAutoClean);
            this.tabOptions.Controls.Add(this.chkCheckForGames);
            this.tabOptions.Controls.Add(this.chkAutoCloseLog);
            this.tabOptions.Controls.Add(this.chkDeveloperMode);
            this.tabOptions.Controls.Add(this.chkDoNotBackup);
            this.tabOptions.Controls.Add(this.chkDoNotConfirmEnableDisable);
            this.tabOptions.Controls.Add(this.chkDoNotWarnUnsavedChanges);
            this.tabOptions.Location = new System.Drawing.Point(4, 22);
            this.tabOptions.Name = "tabOptions";
            this.tabOptions.Padding = new System.Windows.Forms.Padding(3);
            this.tabOptions.Size = new System.Drawing.Size(558, 198);
            this.tabOptions.TabIndex = 1;
            this.tabOptions.Text = "Options";
            // 
            // tabAdvanced
            // 
            this.tabAdvanced.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.tabAdvanced.Controls.Add(this.lblAesKey);
            this.tabAdvanced.Controls.Add(this.txtAesKey);
            this.tabAdvanced.Location = new System.Drawing.Point(4, 22);
            this.tabAdvanced.Name = "tabAdvanced";
            this.tabAdvanced.Padding = new System.Windows.Forms.Padding(3);
            this.tabAdvanced.Size = new System.Drawing.Size(558, 198);
            this.tabAdvanced.TabIndex = 2;
            this.tabAdvanced.Text = "Advanced";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.Gainsboro;
            this.label1.Location = new System.Drawing.Point(9, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Game Directory:";
            // 
            // cmbPlatformType
            // 
            this.cmbPlatformType.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.cmbPlatformType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPlatformType.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbPlatformType.ForeColor = System.Drawing.Color.White;
            this.cmbPlatformType.FormattingEnabled = true;
            this.cmbPlatformType.Items.AddRange(new object[] { "Steam", "Custom" });
            this.cmbPlatformType.Location = new System.Drawing.Point(93, 9);
            this.cmbPlatformType.Name = "cmbPlatformType";
            this.cmbPlatformType.Size = new System.Drawing.Size(70, 21);
            this.cmbPlatformType.TabIndex = 1;
            // 
            // txtGameDir
            // 
            this.txtGameDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtGameDir.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.txtGameDir.ForeColor = System.Drawing.Color.White;
            this.txtGameDir.Location = new System.Drawing.Point(167, 9);
            this.txtGameDir.Name = "txtGameDir";
            this.txtGameDir.Size = new System.Drawing.Size(308, 20);
            this.txtGameDir.TabIndex = 2;
            // 
            // btnBrowseGameDir
            // 
            this.btnBrowseGameDir.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseGameDir.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(70)))));
            this.btnBrowseGameDir.ForeColor = System.Drawing.Color.White;
            this.btnBrowseGameDir.Location = new System.Drawing.Point(481, 7);
            this.btnBrowseGameDir.Name = "btnBrowseGameDir";
            this.btnBrowseGameDir.Size = new System.Drawing.Size(68, 23);
            this.btnBrowseGameDir.TabIndex = 3;
            this.btnBrowseGameDir.Text = "Browse...";
            this.btnBrowseGameDir.UseVisualStyleBackColor = false;
            this.btnBrowseGameDir.Click += new System.EventHandler(this.btnBrowseGameDir_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.Color.Gainsboro;
            this.label2.Location = new System.Drawing.Point(9, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Mods Directory:";
            // 
            // txtModsDir
            // 
            this.txtModsDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtModsDir.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.txtModsDir.ForeColor = System.Drawing.Color.White;
            this.txtModsDir.Location = new System.Drawing.Point(93, 38);
            this.txtModsDir.Name = "txtModsDir";
            this.txtModsDir.Size = new System.Drawing.Size(382, 20);
            this.txtModsDir.TabIndex = 5;
            // 
            // btnBrowseModsDir
            // 
            this.btnBrowseModsDir.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseModsDir.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(70)))));
            this.btnBrowseModsDir.ForeColor = System.Drawing.Color.White;
            this.btnBrowseModsDir.Location = new System.Drawing.Point(481, 36);
            this.btnBrowseModsDir.Name = "btnBrowseModsDir";
            this.btnBrowseModsDir.Size = new System.Drawing.Size(68, 23);
            this.btnBrowseModsDir.TabIndex = 6;
            this.btnBrowseModsDir.Text = "Browse...";
            this.btnBrowseModsDir.UseVisualStyleBackColor = false;
            this.btnBrowseModsDir.Click += new System.EventHandler(this.btnBrowseModsDir_Click);
            // 
            // lblTheme
            // 
            this.lblTheme.AutoSize = true;
            this.lblTheme.ForeColor = System.Drawing.Color.Gainsboro;
            this.lblTheme.Location = new System.Drawing.Point(9, 71);
            this.lblTheme.Name = "lblTheme";
            this.lblTheme.Size = new System.Drawing.Size(43, 13);
            this.lblTheme.TabIndex = 7;
            this.lblTheme.Text = "Theme:";
            // 
            // cmbTheme
            // 
            this.cmbTheme.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.cmbTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTheme.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbTheme.ForeColor = System.Drawing.Color.White;
            this.cmbTheme.FormattingEnabled = true;
            this.cmbTheme.Location = new System.Drawing.Point(93, 68);
            this.cmbTheme.Name = "cmbTheme";
            this.cmbTheme.Size = new System.Drawing.Size(200, 21);
            this.cmbTheme.TabIndex = 8;
            this.cmbTheme.SelectedIndexChanged += new System.EventHandler(this.cmbTheme_SelectedIndexChanged);
            // 
            // btnCustomizeTheme
            // 
            this.btnCustomizeTheme.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(70)))));
            this.btnCustomizeTheme.ForeColor = System.Drawing.Color.White;
            this.btnCustomizeTheme.Location = new System.Drawing.Point(299, 66);
            this.btnCustomizeTheme.Name = "btnCustomizeTheme";
            this.btnCustomizeTheme.Size = new System.Drawing.Size(75, 23);
            this.btnCustomizeTheme.TabIndex = 9;
            this.btnCustomizeTheme.Text = "Edit...";
            this.btnCustomizeTheme.UseVisualStyleBackColor = false;
            this.btnCustomizeTheme.Visible = false;
            this.btnCustomizeTheme.Click += new System.EventHandler(this.btnCustomizeTheme_Click);
            // 
            // chkSortEnabled
            // 
            this.chkSortEnabled.AutoSize = true;
            this.chkSortEnabled.ForeColor = System.Drawing.Color.Gainsboro;
            this.chkSortEnabled.Location = new System.Drawing.Point(12, 12);
            this.chkSortEnabled.Name = "chkSortEnabled";
            this.chkSortEnabled.Size = new System.Drawing.Size(215, 17);
            this.chkSortEnabled.TabIndex = 0;
            this.chkSortEnabled.Text = "Sort enabled mods to the top of the list";
            this.chkSortEnabled.UseVisualStyleBackColor = true;
            // 
            // chkAutoClean
            // 
            this.chkAutoClean.AutoSize = true;
            this.chkAutoClean.ForeColor = System.Drawing.Color.Gainsboro;
            this.chkAutoClean.Location = new System.Drawing.Point(12, 35);
            this.chkAutoClean.Name = "chkAutoClean";
            this.chkAutoClean.Size = new System.Drawing.Size(212, 17);
            this.chkAutoClean.TabIndex = 1;
            this.chkAutoClean.Text = "Automatically clean up temporary files";
            this.chkAutoClean.UseVisualStyleBackColor = true;
            // 
            // chkCheckForGames
            // 
            this.chkCheckForGames.AutoSize = true;
            this.chkCheckForGames.ForeColor = System.Drawing.Color.Gainsboro;
            this.chkCheckForGames.Location = new System.Drawing.Point(12, 58);
            this.chkCheckForGames.Name = "chkCheckForGames";
            this.chkCheckForGames.Size = new System.Drawing.Size(226, 17);
            this.chkCheckForGames.TabIndex = 2;
            this.chkCheckForGames.Text = "Check for game installations on startup";
            this.chkCheckForGames.UseVisualStyleBackColor = true;
            // 
            // chkAutoCloseLog
            // 
            this.chkAutoCloseLog.AutoSize = true;
            this.chkAutoCloseLog.ForeColor = System.Drawing.Color.Gainsboro;
            this.chkAutoCloseLog.Location = new System.Drawing.Point(12, 81);
            this.chkAutoCloseLog.Name = "chkAutoCloseLog";
            this.chkAutoCloseLog.Size = new System.Drawing.Size(221, 17);
            this.chkAutoCloseLog.TabIndex = 3;
            this.chkAutoCloseLog.Text = "Auto-close debug log on successful save";
            this.chkAutoCloseLog.UseVisualStyleBackColor = true;
            // 
            // chkDeveloperMode
            // 
            this.chkDeveloperMode.AutoSize = true;
            this.chkDeveloperMode.ForeColor = System.Drawing.Color.Gainsboro;
            this.chkDeveloperMode.Location = new System.Drawing.Point(12, 104);
            this.chkDeveloperMode.Name = "chkDeveloperMode";
            this.chkDeveloperMode.Size = new System.Drawing.Size(138, 17);
            this.chkDeveloperMode.TabIndex = 4;
            this.chkDeveloperMode.Text = "Enable Developer Mode";
            this.chkDeveloperMode.UseVisualStyleBackColor = true;
            // 
            // chkDoNotBackup
            // 
            this.chkDoNotBackup.AutoSize = true;
            this.chkDoNotBackup.ForeColor = System.Drawing.Color.Gainsboro;
            this.chkDoNotBackup.Location = new System.Drawing.Point(12, 127);
            this.chkDoNotBackup.Name = "chkDoNotBackup";
            this.chkDoNotBackup.Size = new System.Drawing.Size(220, 17);
            this.chkDoNotBackup.TabIndex = 5;
            this.chkDoNotBackup.Text = "Do not backup mods automatically";
            this.chkDoNotBackup.UseVisualStyleBackColor = true;
            // 
            // chkDoNotConfirmEnableDisable
            // 
            this.chkDoNotConfirmEnableDisable.AutoSize = true;
            this.chkDoNotConfirmEnableDisable.ForeColor = System.Drawing.Color.Gainsboro;
            this.chkDoNotConfirmEnableDisable.Location = new System.Drawing.Point(12, 150);
            this.chkDoNotConfirmEnableDisable.Name = "chkDoNotConfirmEnableDisable";
            this.chkDoNotConfirmEnableDisable.Size = new System.Drawing.Size(255, 17);
            this.chkDoNotConfirmEnableDisable.TabIndex = 6;
            this.chkDoNotConfirmEnableDisable.Text = "Do not ask for confirmation when enabling/disabling all";
            this.chkDoNotConfirmEnableDisable.UseVisualStyleBackColor = true;
            // 
            // chkDoNotWarnUnsavedChanges
            // 
            this.chkDoNotWarnUnsavedChanges.AutoSize = true;
            this.chkDoNotWarnUnsavedChanges.ForeColor = System.Drawing.Color.Gainsboro;
            this.chkDoNotWarnUnsavedChanges.Location = new System.Drawing.Point(12, 173);
            this.chkDoNotWarnUnsavedChanges.Name = "chkDoNotWarnUnsavedChanges";
            this.chkDoNotWarnUnsavedChanges.Size = new System.Drawing.Size(255, 17);
            this.chkDoNotWarnUnsavedChanges.TabIndex = 7;
            this.chkDoNotWarnUnsavedChanges.Text = "Do not warn about unsaved changes when playing";
            this.chkDoNotWarnUnsavedChanges.UseVisualStyleBackColor = true;
            // 
            // lblAesKey
            // 
            this.lblAesKey.AutoSize = true;
            this.lblAesKey.ForeColor = System.Drawing.Color.Gainsboro;
            this.lblAesKey.Location = new System.Drawing.Point(9, 15);
            this.lblAesKey.Name = "lblAesKey";
            this.lblAesKey.Size = new System.Drawing.Size(54, 13);
            this.lblAesKey.TabIndex = 0;
            this.lblAesKey.Text = "AES Key:";
            // 
            // txtAesKey
            // 
            this.txtAesKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAesKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.txtAesKey.ForeColor = System.Drawing.Color.White;
            this.txtAesKey.Location = new System.Drawing.Point(69, 12);
            this.txtAesKey.Name = "txtAesKey";
            this.txtAesKey.Size = new System.Drawing.Size(480, 20);
            this.txtAesKey.TabIndex = 1;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(410, 236);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(70)))));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.Location = new System.Drawing.Point(491, 236);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(580, 270);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.ForeColor = System.Drawing.Color.White;
            this.tabControl.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.tabGeneral.PerformLayout();
            this.tabOptions.ResumeLayout(false);
            this.tabOptions.PerformLayout();
            this.tabAdvanced.ResumeLayout(false);
            this.tabAdvanced.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.TabPage tabOptions;
        private System.Windows.Forms.TabPage tabAdvanced;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtGameDir;
        private System.Windows.Forms.Button btnBrowseGameDir;
        private System.Windows.Forms.Button btnBrowseModsDir;
        private System.Windows.Forms.TextBox txtModsDir;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox chkSortEnabled;
        private System.Windows.Forms.CheckBox chkAutoClean;
        private System.Windows.Forms.CheckBox chkCheckForGames;
        private System.Windows.Forms.CheckBox chkAutoCloseLog;
        private System.Windows.Forms.CheckBox chkDeveloperMode;
        private System.Windows.Forms.CheckBox chkDoNotBackup;
        private System.Windows.Forms.CheckBox chkDoNotConfirmEnableDisable;
        private System.Windows.Forms.CheckBox chkDoNotWarnUnsavedChanges;
        private System.Windows.Forms.Label lblTheme;
        private System.Windows.Forms.ComboBox cmbTheme;
        private System.Windows.Forms.Button btnCustomizeTheme;
        private System.Windows.Forms.ComboBox cmbPlatformType;
        private System.Windows.Forms.Label lblAesKey;
        private System.Windows.Forms.TextBox txtAesKey;
    }
}

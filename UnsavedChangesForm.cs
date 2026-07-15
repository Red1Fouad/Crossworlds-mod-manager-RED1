using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public class UnsavedChangesForm : Form
    {

        public UnsavedChangesForm()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.Text = "Unsaved Changes";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(450, 160);
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowIcon = false;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            var lblMessage = new Label()
            {
                Text = "You have unsaved changes to your enabled mods.\nDo you want to save them before playing?",
                Location = new Point(20, 20),
                Size = new Size(410, 50),
                ForeColor = Color.White,
                Font = new Font(SystemFonts.MessageBoxFont?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, 10)
            };

            var btnSaveAndPlay = new Button()
            {
                Text = "Save and Play",
                DialogResult = DialogResult.Yes,
                Location = new Point(130, 115),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnSaveAndPlay.FlatAppearance.BorderSize = 0;

            var btnPlayAnyways = new Button()
            {
                Text = "Play Anyways",
                DialogResult = DialogResult.No,
                Location = new Point(240, 115),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnPlayAnyways.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button()
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(350, 115),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.Add(lblMessage);
            this.Controls.Add(btnSaveAndPlay);
            this.Controls.Add(btnPlayAnyways);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnSaveAndPlay;
            this.CancelButton = btnCancel;
        }
    }
#pragma warning restore CA1416
}
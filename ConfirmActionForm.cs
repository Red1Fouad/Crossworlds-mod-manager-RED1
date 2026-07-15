using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public class ConfirmActionForm : Form
    {
        private Label lblMessage;
        private CheckBox chkDontShow;
        private Button btnYes;
        private Button btnNo;

        public bool DontShowAgain => chkDontShow.Checked;

        public ConfirmActionForm(string message, string checkboxText)
        {
            this.Text = "Confirm";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(420, 150);
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            lblMessage = new Label()
            {
                Text = message,
                Location = new Point(12, 12),
                Size = new Size(380, 44),
                ForeColor = Color.Gainsboro
            };

            chkDontShow = new CheckBox()
            {
                Text = checkboxText,
                Location = new Point(12, 64),
                AutoSize = true,
                ForeColor = Color.Gainsboro
            };

            btnYes = new Button()
            {
                Text = "Yes",
                DialogResult = DialogResult.Yes,
                Location = new Point(220, 96),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnYes.FlatAppearance.BorderSize = 0;

            btnNo = new Button()
            {
                Text = "No",
                DialogResult = DialogResult.No,
                Location = new Point(312, 96),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnNo.FlatAppearance.BorderSize = 0;

            this.Controls.Add(lblMessage);
            this.Controls.Add(chkDontShow);
            this.Controls.Add(btnYes);
            this.Controls.Add(btnNo);
            this.AcceptButton = btnYes;
            this.CancelButton = btnNo;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            
            ThemeManager.ApplyTheme(this);
        }
    }
}

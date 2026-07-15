using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public partial class ExFatWarningForm : Form
    {
        public bool DoNotShowAgain { get; private set; }
        private CheckBox _chkDoNotShow = null!;

        public ExFatWarningForm()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.Text = "File System Warning";
            this.ClientSize = new Size(450, 210);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;

            var lblMsg = new Label();
            lblMsg.Text = "The game is installed on an exFAT drive.\n\n" +
                          "Symbolic links are not supported on exFAT, so mods must be COPIED instead.\n\n" +
                          "This will take up more disk space (bloat) and take longer to install.\n" +
                          "Do you want to proceed?";
            lblMsg.Font = new Font(SystemFonts.MessageBoxFont?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, 10);
            lblMsg.TextAlign = ContentAlignment.MiddleLeft;
            lblMsg.Dock = DockStyle.Top;
            lblMsg.Height = 130;
            lblMsg.Padding = new Padding(10);

            _chkDoNotShow = new CheckBox();
            _chkDoNotShow.Text = "Do not show this warning again";
            _chkDoNotShow.AutoSize = true;
            _chkDoNotShow.Location = new Point(15, 140);

            var btnOk = new Button();
            btnOk.Text = "Proceed";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(250, 160);
            btnOk.Size = new Size(80, 30);
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.BackColor = Color.FromArgb(0, 122, 204);
            btnOk.ForeColor = Color.White;
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.UseVisualStyleBackColor = false;
            btnOk.Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;

            var btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(340, 160);
            btnCancel.Size = new Size(80, 30);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.BackColor = Color.FromArgb(63, 63, 70);
            btnCancel.ForeColor = Color.White;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;

            this.Controls.Add(btnCancel);
            this.Controls.Add(btnOk);
            this.Controls.Add(_chkDoNotShow);
            this.Controls.Add(lblMsg);
            
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            DoNotShowAgain = _chkDoNotShow.Checked;
            base.OnFormClosing(e);
        }
    }
#pragma warning restore CA1416
}

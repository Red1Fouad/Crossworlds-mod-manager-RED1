using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public partial class ProgressForm : Form
    {
        private Label lblStatus = null!;
        private ProgressBar progressBar = null!;
        private Button btnOk = null!;
        private Button btnCancel = null!;
        public System.Threading.CancellationTokenSource? TokenSource { get; private set; }

        public ProgressForm(string title)
        {
            InitializeComponent();
            this.Text = title;
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            this.lblStatus = new Label();
            this.progressBar = new ProgressBar();
            this.btnOk = new Button();
            this.btnCancel = new Button();
            this.SuspendLayout();

            // Form settings
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            this.ClientSize = new Size(384, 120);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ControlBox = false;

            // lblStatus
            this.lblStatus.Location = new Point(12, 20);
            this.lblStatus.Size = new Size(360, 23);
            this.lblStatus.Text = "Initializing...";
            this.lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            // progressBar
            this.progressBar.Location = new Point(12, 50);
            this.progressBar.Size = new Size(360, 23);
            this.progressBar.Style = ProgressBarStyle.Continuous;

            // btnOk
            this.btnOk.DialogResult = DialogResult.OK;
            this.btnOk.Location = new Point(150, 85);
            this.btnOk.Size = new Size(80, 30);
            this.btnOk.Text = "OK";
            this.btnOk.Visible = false; // Only show when done
            this.btnOk.FlatStyle = FlatStyle.Flat;
            this.btnOk.BackColor = Color.FromArgb(0, 122, 204);
            this.btnOk.ForeColor = Color.White;
            this.btnOk.FlatAppearance.BorderSize = 0;
            this.btnOk.UseVisualStyleBackColor = false;
            this.btnOk.Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;

            // btnCancel
            this.btnCancel.Location = new Point(50, 85);
            this.btnCancel.Size = new Size(80, 30);
            this.btnCancel.Text = "Cancel";
            this.btnCancel.FlatStyle = FlatStyle.Flat;
            this.btnCancel.BackColor = Color.FromArgb(160, 160, 160);
            this.btnCancel.ForeColor = Color.White;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
            this.btnCancel.Click += (s, e) =>
            {
                try
                {
                    this.TokenSource?.Cancel();
                    this.btnCancel.Enabled = false;
                    UpdateStatus("Cancelling...");
                }
                catch { }
            };

            // Add controls
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblStatus);
            this.ResumeLayout(false);
        }

        public void UpdateProgress(int percentage)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => UpdateProgress(percentage)));
                return;
            }
            progressBar.Value = Math.Clamp(percentage, 0, 100);
        }

        public void UpdateStatus(string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => UpdateStatus(status)));
                return;
            }
            lblStatus.Text = status;
        }

        public void ShowCompletion(string finalMessage)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => ShowCompletion(finalMessage)));
                return;
            }
            lblStatus.Text = finalMessage;
            progressBar.Visible = false;
            btnOk.Visible = true;
            btnCancel.Visible = false;
        }

        public IProgress<string> GetLoggerProgress()
        {
            return new Progress<string>(UpdateStatus);
        }

        protected override void OnShown(EventArgs e)
        {
            // Initialize cancellation token before raising Shown so callers can observe it.
            TokenSource = new System.Threading.CancellationTokenSource();
            base.OnShown(e);
        }
    }
#pragma warning restore CA1416
}
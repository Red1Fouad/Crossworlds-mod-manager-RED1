using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public class LogForm : Form
    {
        private readonly RichTextBox rtbLog;
        private readonly Button btnClose;
        private readonly Button btnClear;

        public LogForm()
        {
            Text = "Debug Log";
            Size = new Size(700, 400);
            StartPosition = FormStartPosition.CenterParent;

            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Consolas", 10),
                HideSelection = false
            };

            btnClose = new Button
            {
                Text = "Close",
                Dock = DockStyle.Right,
                Width = 80,
                Enabled = false,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White
            };
            btnClose.Click += (s, e) => Close();
            btnClose.FlatAppearance.BorderSize = 0;

            btnClear = new Button
            {
                Text = "Clear",
                Dock = DockStyle.Right,
                Width = 80,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White
            };
            btnClear.Click += (s, e) => rtbLog.Clear();

            var panel = new Panel { Dock = DockStyle.Bottom, Height = 36 };
            panel.Controls.Add(btnClose);
            panel.Controls.Add(btnClear);

            Controls.Add(rtbLog);
            Controls.Add(panel);
            
            ThemeManager.ApplyTheme(this);
        }

        public void AppendLog(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), text);
                return;
            }

            if (string.IsNullOrEmpty(text)) return;

            // Add timestamp for clarity
            var line = $"[{DateTime.Now:HH:mm:ss}] {text}\n";
            rtbLog.AppendText(line);
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.ScrollToCaret();
        }

        public void MarkDone()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(MarkDone));
                return;
            }
            btnClose.Enabled = true;
        }

        public bool ContainsText(string text)
        {
            return rtbLog.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Gets an IProgress<string> instance that logs messages to this form.
        /// </summary>
        /// <returns>An IProgress<string> for logging.</returns>
        public IProgress<string> GetLoggerProgress()
        {
            return new Progress<string>(AppendLog);
        }
    }
}

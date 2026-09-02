using System.Windows.Forms;
using System.Drawing;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption, string defaultValue = "")
        {
            using (Form prompt = new Form())
            {
                prompt.Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
                prompt.Width = 450;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = caption;
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;
                prompt.BackColor = Color.FromArgb(45, 45, 48);
                prompt.ForeColor = Color.White;

                Label textLabel = new Label() { Left = 20, Top = 20, Text = text, AutoSize = true, MaximumSize = new Size(400, 0) };
                TextBox textBox = new TextBox() { 
                    Left = 20, 
                    Top = 50, 
                    Width = 390, 
                    Text = defaultValue,
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };
                
                // Adjust input position based on label height
                if (textLabel.Bottom > 40) textBox.Top = textLabel.Bottom + 10;

                FlowLayoutPanel buttonPanel = new FlowLayoutPanel()
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(10),
                    BackColor = Color.FromArgb(45, 45, 48)
                };

                Button confirmation = new Button() { 
                    Text = "OK", DialogResult = DialogResult.OK,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    Size = new Size(80, 30),
                    UseVisualStyleBackColor = false,
                    Margin = new Padding(5, 0, 0, 0)
                };
                confirmation.FlatAppearance.BorderSize = 0;

                Button cancel = new Button() { 
                    Text = "Cancel", DialogResult = DialogResult.Cancel,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(63, 63, 70),
                    ForeColor = Color.White,
                    Size = new Size(80, 30),
                    UseVisualStyleBackColor = false,
                    Margin = new Padding(5, 0, 0, 0)
                };
                cancel.FlatAppearance.BorderSize = 0;

                buttonPanel.Controls.Add(cancel);
                buttonPanel.Controls.Add(confirmation);

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(buttonPanel);
                
                // Calculate height based on content
                int contentHeight = textBox.Bottom + buttonPanel.Height + 40;
                prompt.ClientSize = new Size(450, contentHeight);

                prompt.AcceptButton = confirmation;
                prompt.CancelButton = cancel;

                ThemeManager.ApplyTheme(prompt);

                return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
            }
        }
    }
#pragma warning restore CA1416
}
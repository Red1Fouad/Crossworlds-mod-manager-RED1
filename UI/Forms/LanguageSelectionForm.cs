using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public class LanguageSelectionForm : Form
    {
        public string SelectedLanguage { get; private set; } = "";
        private ComboBox? cmbLanguages;
        private Button? btnOk;
        private Button? btnCancel;

        public LanguageSelectionForm(List<string> languages)
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            cmbLanguages!.DataSource = languages;
            if (languages.Contains("en", StringComparer.OrdinalIgnoreCase))
            {
                cmbLanguages!.SelectedItem = languages.First(l => l.Equals("en", StringComparison.OrdinalIgnoreCase));
            }
            else if (languages.Any())
            {
                cmbLanguages!.SelectedIndex = 0;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Select Language";
            this.Size = new Size(350, 180);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            var lblPrompt = new Label
            {
                Text = "Select the language to use as a base for text editing:",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F)
            };

            cmbLanguages = new ComboBox
            {
                Location = new Point(20, 50),
                Width = 290,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F)
            };

            btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(150, 90), Size = new Size(80, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => { SelectedLanguage = cmbLanguages.SelectedItem?.ToString() ?? ""; };

            btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(240, 90), Size = new Size(70, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(63, 63, 70), ForeColor = Color.White };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.Add(lblPrompt);
            this.Controls.Add(cmbLanguages);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
    }
#pragma warning restore CA1416
}
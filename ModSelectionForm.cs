using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public class ModSelectionForm : Form
    {
        public List<string> SelectedMods { get; private set; } = new List<string>();
        private CheckedListBox chkMods = null!;
        private TextBox txtSearch = null!;
        private Button btnOk = null!;
        private Button btnCancel = null!;

        private readonly List<string> _allMods;
        private readonly HashSet<string> _checkedMods;
        private bool _isUpdatingList = false;

        public ModSelectionForm(List<string> availableMods, List<string> currentGroupMods)
        {
            _allMods = availableMods.OrderBy(m => m).ToList();
            _checkedMods = new HashSet<string>(currentGroupMods, StringComparer.OrdinalIgnoreCase);

            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            UpdateList();
        }

        private void InitializeComponent()
        {
            this.Text = "Select Mods";
            this.Size = new Size(400, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                Padding = new Padding(5),
                BackColor = Color.FromArgb(45, 45, 48)
            };

            var lblSearch = new Label
            {
                Text = "Search:",
                Dock = DockStyle.Left,
                AutoSize = true,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 4, 5, 0)
            };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F)
            };
            txtSearch.TextChanged += (s, e) => UpdateList();

            pnlTop.Controls.Add(txtSearch);
            pnlTop.Controls.Add(lblSearch);
            txtSearch.BringToFront();

            chkMods = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                CheckOnClick = true
            };
            chkMods.ItemCheck += ChkMods_ItemCheck;

            var pnlBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5)
            };

            btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Size = new Size(80, 30),
                Margin = new Padding(5)
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => {
                SelectedMods = _checkedMods.ToList();
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White,
                Size = new Size(80, 30),
                Margin = new Padding(5)
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            pnlBottom.Controls.Add(btnOk);
            pnlBottom.Controls.Add(btnCancel);

            // Add controls in specific order for Docking to work as expected
            this.Controls.Add(chkMods);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlBottom);
            
            // Ensure correct docking order (Top/Bottom first in Z-order)
            chkMods.BringToFront();
            pnlBottom.SendToBack();
            pnlTop.SendToBack();

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void UpdateList()
        {
            _isUpdatingList = true;
            chkMods.BeginUpdate();
            chkMods.Items.Clear();

            string filter = txtSearch.Text.Trim();
            var items = string.IsNullOrEmpty(filter) 
                ? _allMods 
                : _allMods.Where(m => m.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            foreach (var mod in items)
            {
                bool isChecked = _checkedMods.Contains(mod);
                chkMods.Items.Add(mod, isChecked);
            }

            chkMods.EndUpdate();
            _isUpdatingList = false;
        }

        private void ChkMods_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (_isUpdatingList) return;

            string modName = chkMods.Items[e.Index].ToString() ?? "";
            if (e.NewValue == CheckState.Checked)
            {
                _checkedMods.Add(modName);
            }
            else
            {
                _checkedMods.Remove(modName);
            }
        }
    }
#pragma warning restore CA1416
}
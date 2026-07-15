using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public class GroupManagerForm : Form
    {
        private ListBox lstGroups = null!;
        private ListBox lstGroupMods = null!;
        private Button btnDeleteGroup = null!;
        private Button btnRenameGroup = null!;
        private Button btnEditMods = null!;
        private Button btnClose = null!;
        
        private readonly List<string> _allAvailableMods;

        public GroupManagerForm(List<string> allAvailableMods)
        {
            _allAvailableMods = allAvailableMods;
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            RefreshGroupList();
        }

        private void InitializeComponent()
        {
            this.Text = "Manage Mod Groups";
            this.Size = new Size(700, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 250,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Left Panel (Groups)
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var lblGroups = new Label { Text = "Groups", Dock = DockStyle.Top, Height = 20, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), ForeColor = Color.White };
            lstGroups = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            lstGroups.SelectedIndexChanged += (s, e) => RefreshModList();

            var pnlLeftButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 35, FlowDirection = FlowDirection.LeftToRight };
            btnRenameGroup = CreateButton("Rename", (s, e) => RenameGroup());
            btnDeleteGroup = CreateButton("Delete", (s, e) => DeleteGroup());
            pnlLeftButtons.Controls.Add(btnRenameGroup);
            pnlLeftButtons.Controls.Add(btnDeleteGroup);

            pnlLeft.Controls.Add(lstGroups);
            pnlLeft.Controls.Add(pnlLeftButtons);
            pnlLeft.Controls.Add(lblGroups);

            // Right Panel (Mods)
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var lblMods = new Label { Text = "Mods in Group", Dock = DockStyle.Top, Height = 20, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), ForeColor = Color.White };
            lstGroupMods = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            
            var pnlRightButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 35, FlowDirection = FlowDirection.LeftToRight };
            btnEditMods = CreateButton("Edit Mods...", (s, e) => EditGroupMods(), 100);
            pnlRightButtons.Controls.Add(btnEditMods);

            pnlRight.Controls.Add(lstGroupMods);
            pnlRight.Controls.Add(pnlRightButtons);
            pnlRight.Controls.Add(lblMods);

            splitContainer.Panel1.Controls.Add(pnlLeft);
            splitContainer.Panel2.Controls.Add(pnlRight);

            // Bottom Panel (Close)
            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            btnClose = CreateButton("Close", (s, e) => Close());
            btnClose.Location = new Point(this.ClientSize.Width - 90, 5);
            btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            pnlBottom.Controls.Add(btnClose);

            this.Controls.Add(splitContainer);
            this.Controls.Add(pnlBottom);
        }

        private Button CreateButton(string text, EventHandler onClick, int width = 80)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 5, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private void RefreshGroupList()
        {
            var selected = lstGroups.SelectedItem;
            lstGroups.Items.Clear();
            foreach (var key in SettingsManager.Settings.ModGroups.Keys.OrderBy(k => k))
            {
                lstGroups.Items.Add(key);
            }
            if (selected != null && lstGroups.Items.Contains(selected))
                lstGroups.SelectedItem = selected;
            else if (lstGroups.Items.Count > 0)
                lstGroups.SelectedIndex = 0;
            else
                RefreshModList(); // Clear mod list if no groups
        }

        private void RefreshModList()
        {
            lstGroupMods.Items.Clear();
            if (lstGroups.SelectedItem is string groupName && SettingsManager.Settings.ModGroups.TryGetValue(groupName, out var mods))
            {
                foreach (var mod in mods)
                {
                    lstGroupMods.Items.Add(mod);
                }
            }
        }

        private void DeleteGroup()
        {
            if (lstGroups.SelectedItem is string groupName)
            {
                if (CustomMessageBox.Show($"Are you sure you want to delete group '{groupName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    SettingsManager.Settings.ModGroups.Remove(groupName);
                    SettingsManager.Save();
                    RefreshGroupList();
                }
            }
        }

        private void RenameGroup()
        {
            if (lstGroups.SelectedItem is string groupName)
            {
                string newName = Prompt.ShowDialog("Enter new name:", "Rename Group", groupName);
                if (!string.IsNullOrWhiteSpace(newName) && newName != groupName)
                {
                    if (SettingsManager.Settings.ModGroups.ContainsKey(newName))
                    {
                        CustomMessageBox.Show("A group with that name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var mods = SettingsManager.Settings.ModGroups[groupName];
                    SettingsManager.Settings.ModGroups.Remove(groupName);
                    SettingsManager.Settings.ModGroups[newName] = mods;
                    SettingsManager.Save();
                    RefreshGroupList();
                    lstGroups.SelectedItem = newName;
                }
            }
        }

        private void EditGroupMods()
        {
            if (lstGroups.SelectedItem is string groupName && SettingsManager.Settings.ModGroups.TryGetValue(groupName, out var currentMods))
            {
                using (var form = new ModSelectionForm(_allAvailableMods, currentMods))
                {
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        SettingsManager.Settings.ModGroups[groupName] = form.SelectedMods;
                        SettingsManager.Save();
                        RefreshModList();
                    }
                }
            }
        }
    }
#pragma warning restore CA1416
}
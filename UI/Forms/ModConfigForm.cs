using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public partial class ModConfigForm : Form
    {
        private readonly ModInfo _modInfo;
        private readonly ModProfile _activeProfile;
        // The key is the GroupName, the value is a list of controls (RadioButtons or CheckBoxes).
        private readonly Dictionary<string, List<Control>> _groupControls = new();
        // Cache panels for each group to preserve state when switching views
        private readonly Dictionary<string, Panel> _groupPanels = new();
        private Panel? _selectedGroupPanel;
        private FlowLayoutPanel _groupsFlowPanel = null!;
        private Panel _optionsPanel = null!;
        private SplitContainer _splitContainer = null!;
        
        public ModConfigForm(ModInfo modInfo)
        {
            InitializeComponent();
            _modInfo = modInfo;
            this.Text = $"Configure '{_modInfo.Name}'";

            // This is a bit of a shortcut. A better way would be to pass the profile in.
            // But for now, this will work.
            var activeProfileName = SettingsManager.Settings.ActiveProfileName ?? "";
            if (!SettingsManager.Settings.Profiles.TryGetValue(activeProfileName, out _activeProfile!))
                _activeProfile = new ModProfile(); // Fallback to an empty profile if something is wrong.

            this.Shown += ModConfigForm_Shown;
        }
        
        private void ModConfigForm_Load(object? sender, EventArgs e)
        {
            this.Controls.Clear();
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            this.ClientSize = new Size(960, 540); // 16:9 aspect ratio
            this.MinimumSize = new Size(800, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Main SplitContainer
            _splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                // SplitterDistance will be set in the Shown event
                BackColor = Color.FromArgb(37, 37, 38),
                SplitterWidth = 2
            };

            // Left Panel - Groups Flow Panel
            _groupsFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };
            // Adjust the width of child controls to fill the panel
            _groupsFlowPanel.ControlAdded += (s, e) => {
                if (e.Control is not null)
                {
                    e.Control.Width = _groupsFlowPanel.ClientSize.Width - e.Control.Margin.Horizontal;
                }
            };
            _groupsFlowPanel.SizeChanged += (s, e) => { foreach (Control c in _groupsFlowPanel.Controls) { c.Width = _groupsFlowPanel.ClientSize.Width - c.Margin.Horizontal; } };
            
            // Bottom panel for action buttons
            var pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(10, 0, 10, 0),
            };

            var btnSave = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new Size(95, 32),
                Location = new Point(pnlButtons.ClientSize.Width - 105, 9),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                UseVisualStyleBackColor = false,
                Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += btnSave_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new Size(95, 32),
                Location = new Point(pnlButtons.ClientSize.Width - 210, 9),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White,
                UseVisualStyleBackColor = false,
                Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            pnlButtons.Controls.Add(btnSave);
            pnlButtons.Controls.Add(btnCancel);

            // Right Panel - Options
            _optionsPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(37, 37, 38), Padding = new Padding(20), AutoScroll = true };

            // Add controls to containers
            _splitContainer.Panel1.Controls.Add(_groupsFlowPanel);
            _splitContainer.Panel2.Controls.Add(_optionsPanel);

            this.Controls.Add(_splitContainer);
            this.Controls.Add(pnlButtons);

            ThemeManager.ApplyTheme(this);

            foreach (var group in _modInfo.ConfigurationGroups)
            {
                var groupPanel = CreateGroupSelectionPanel(group);
                _groupsFlowPanel.Controls.Add(groupPanel);
            }

            if (_groupsFlowPanel.Controls.Count > 0)
            {
                // Simulate a click on the first item to select it
                GroupSelectionPanel_Click(_groupsFlowPanel.Controls[0], EventArgs.Empty);
            }
        }

        private void ModConfigForm_Shown(object? sender, EventArgs e)
        {
            // Set the splitter distance here to ensure it's not overridden by layout events.
            _splitContainer.SplitterDistance = 250;
        }

        private Panel CreateGroupSelectionPanel(ModConfigurationGroup group)
        {
            var panel = new Panel
            {
                Height = 40,
                Margin = new Padding(0, 0, 0, 4),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = ThemeManager.CurrentTheme.ButtonBackColor,
                Tag = group.GroupName,
                Cursor = Cursors.Hand
            };

            var label = new Label
            {
                Text = group.GroupName,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeManager.CurrentTheme.ButtonForeColor,
                Font = new Font(this.Font.FontFamily, 10f),
                Tag = group.GroupName, // Also tag the label for click event
                Cursor = Cursors.Hand
            };

            panel.Controls.Add(label);

            // Attach events to both panel and label for reliable hover/click
            panel.MouseEnter += (s, e) => { if (panel != _selectedGroupPanel) panel.BackColor = ControlPaint.Light(ThemeManager.CurrentTheme.ButtonBackColor); };
            panel.MouseLeave += (s, e) => { if (panel != _selectedGroupPanel) panel.BackColor = ThemeManager.CurrentTheme.ButtonBackColor; };
            panel.Click += GroupSelectionPanel_Click;
            label.Click += GroupSelectionPanel_Click;

            return panel;
        }

        private void GroupSelectionPanel_Click(object? sender, EventArgs e)
        {
            if (sender is not Control { Tag: string groupName } clickedControl) return;

            var group = _modInfo.ConfigurationGroups.FirstOrDefault(g => g.GroupName == groupName);
            if (group == null) return;

            // Find the panel that was clicked, even if the label inside was the source
            var parentPanel = clickedControl as Panel ?? clickedControl.Parent as Panel;

            // Don't do anything if the same panel is clicked again
            if (parentPanel == _selectedGroupPanel) return;

            // Update selection visual state
            if (_selectedGroupPanel != null)
            {
                _selectedGroupPanel.BackColor = ThemeManager.CurrentTheme.ButtonBackColor; // Deselect old
            }

            if (parentPanel != null)
            {
                parentPanel.BackColor = ThemeManager.CurrentTheme.AccentColor; // Select new
                _selectedGroupPanel = parentPanel;
            }

            _optionsPanel.Controls.Clear();

            // Check if we've already created a panel for this group
            if (_groupPanels.TryGetValue(groupName, out var existingPanel))
            {
                _optionsPanel.Controls.Add(existingPanel);
                return;
            }

            // If not, create a new one and cache it
            var newOptionsHostPanel = CreateOptionsHostPanel(group);

            if (group.Type == ModConfigType.SelectOne)
            {
                BuildSelectOneUI(newOptionsHostPanel, group);
            }
            else if (group.Type == ModConfigType.SelectMultiple)
            {
                BuildSelectMultipleUI(newOptionsHostPanel, group);
            }

            _groupPanels[groupName] = newOptionsHostPanel; // Cache the new panel
            _optionsPanel.Controls.Add(newOptionsHostPanel);
        }

        private void BuildSelectOneUI(Panel parentPanel, ModConfigurationGroup group)
        {
            var configKey = $"{_modInfo.Name}:{group.GroupName}";
            _activeProfile.ModConfigurations.TryGetValue(configKey, out var savedOption);
            
            var controls = new List<Control>();
            bool selectionMade = false;

            foreach (string optionName in group.Options)
            {
                var radioButton = new RadioButton
                {
                    Text = optionName,
                    Tag = optionName,
                    Location = new Point(0, 50 + (controls.Count * 30)),
                    AutoSize = true,
                    ForeColor = ThemeManager.CurrentTheme.ForeColor,
                    BackColor = Color.Transparent,
                    Font = new Font(this.Font.FontFamily, 10f)
                };

                // Check this button if it's the saved option, or if it's the first option and nothing is saved yet.
                if (!selectionMade && (savedOption == optionName || (string.IsNullOrEmpty(savedOption) && controls.Count == 0)))
                {
                    radioButton.Checked = true;
                    selectionMade = true;
                }

                parentPanel.Controls.Add(radioButton);
                controls.Add(radioButton);
            }
            _groupControls[group.GroupName] = controls;
        }

        private void BuildSelectMultipleUI(Panel parentPanel, ModConfigurationGroup group)
        {
            var configKey = $"{_modInfo.Name}:{group.GroupName}";
            _activeProfile.ModConfigurations.TryGetValue(configKey, out var savedOptionsString);
            var savedOptions = savedOptionsString?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();

            var controls = new List<Control>();
            foreach (string optionName in group.Options)
            {
                var checkBox = new CheckBox
                {
                    Text = optionName,
                    Tag = optionName,
                    Location = new Point(0, 50 + (controls.Count * 30)),
                    AutoSize = true,
                    ForeColor = ThemeManager.CurrentTheme.ForeColor,
                    BackColor = Color.Transparent,
                    Font = new Font(this.Font.FontFamily, 10f)
                };

                if (savedOptions.Contains(optionName))
                {
                    checkBox.Checked = true;
                }

                parentPanel.Controls.Add(checkBox);
                controls.Add(checkBox);
            }

            _groupControls[group.GroupName] = controls;
        }

        private Panel CreateOptionsHostPanel(ModConfigurationGroup group)
        {
            var panel = new Panel
            {
                AutoSize = true,
                Width = _optionsPanel.ClientSize.Width - 40, // Set width to fill parent, accounting for padding
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent, 
                Padding = new Padding(0)
            };

            var lblTitle = new Label
            {
                Text = group.GroupName,
                Font = new Font(this.Font, FontStyle.Bold),
                ForeColor = ThemeManager.CurrentTheme.ForeColor,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblDescription = new Label
            {
                Text = group.Description,
                ForeColor = ThemeManager.CurrentTheme.ForeColor,
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft
            };

            panel.Controls.Add(lblTitle);
            panel.Controls.Add(lblDescription);
            return panel;
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            foreach (var group in _modInfo.ConfigurationGroups)
            {
                var configKey = $"{_modInfo.Name}:{group.GroupName}";
                
                // Only process groups that have had their controls created
                if (!_groupControls.TryGetValue(group.GroupName, out var controls)) continue;

                if (group.Type == ModConfigType.SelectOne)
                {
                    // Find the checked radio button in the group
                    var checkedRadioButton = controls.OfType<RadioButton>().FirstOrDefault(rb => rb.Checked);
                    if (checkedRadioButton != null)
                    {
                        _activeProfile.ModConfigurations[configKey] = checkedRadioButton.Text;
                    }
                }
                else if (group.Type == ModConfigType.SelectMultiple)
                {
                    var enabledOptions = new List<string>();
                    foreach (CheckBox cb in controls.OfType<CheckBox>())
                    {
                        if (cb.Checked)
                        {
                            enabledOptions.Add(cb.Text);
                        }
                    }
                    _activeProfile.ModConfigurations[configKey] = string.Join(",", enabledOptions);
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
#pragma warning restore CA1416
}
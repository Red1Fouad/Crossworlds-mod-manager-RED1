using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NativeFileDialogs.Net;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    // Data Models
    public class ModConfig
    {
        public Dictionary<string, string> MainSection { get; set; } = new Dictionary<string, string>();
        public List<ConfigGroup> Groups { get; set; } = new List<ConfigGroup>();
    }

    public class ConfigGroup
    {
        public string Name { get; set; } = "NewGroup";
        public string Type { get; set; } = "SelectOne";
        public string Description { get; set; } = "";
        public List<ConfigOption> Options { get; set; } = new List<ConfigOption>();

        public override string ToString() => Name;
    }

    public class ConfigOption
    {
        public string Name { get; set; } = "NewOption";
        public List<string> Files { get; set; } = new List<string>();

        public override string ToString() => Name;
    }

    public class MainInfoPlaceholder { public override string ToString() => "[Mod Info]"; }

    // The Editor Form
    public class ModConfigEditor : Form
    {
        private string _modPath;
        private string _configPath;
        private ModConfig _config = new ModConfig();
        private string? _pendingThumbnailPath;

        // UI Controls
        private ListBox _lstGroups = null!;
        private TextBox _txtGroupName = null!;
        private ComboBox _cmbGroupType = null!;
        private TextBox _txtGroupDesc = null!;
        
        private ListBox _lstOptions = null!;
        private ListBox _lstFiles = null!;
        private Panel _pnlRight = null!;
        
        // Main Info Controls
        private Panel _pnlMainInfo = null!;
        private TextBox _txtModName = null!;
        private TextBox _txtModAuthor = null!;
        private TextBox _txtModVersion = null!;
        private TextBox _txtModDescription = null!;
        private PictureBox _picThumbnail = null!;

        public ModConfigEditor(string modPath)
        {
            _modPath = modPath;
            _configPath = Path.Combine(modPath, "mod.ini");
            
            InitializeComponent();
            ThemeManager.ApplyTheme(this);

            // Make Save button brighter
            var btnSave = this.Controls.Find("btnSave", true).FirstOrDefault() as Button;
            if (btnSave != null)
            {
                btnSave.BackColor = Color.FromArgb(30, 144, 255); // DodgerBlue
            }
            LoadConfig();
        }

        private void InitializeComponent()
        {
            this.Text = "Mod Config Maker";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            var splitContainer = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1, BackColor = Color.FromArgb(45, 45, 48) };
            this.Controls.Add(splitContainer);
            splitContainer.SplitterDistance = 250;

            // --- Left Panel (Groups List) ---
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            splitContainer.Panel1.Controls.Add(pnlLeft);

            var lblGroups = new Label { Text = "Configuration Groups", Dock = DockStyle.Top, Height = 25, Font = new Font(this.Font, FontStyle.Bold), ForeColor = Color.White };
            _lstGroups = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            _lstGroups.SelectedIndexChanged += (s, e) => UpdateRightPanel();
            
            var pnlGroupButtons = new Panel { Dock = DockStyle.Bottom, Height = 100, Padding = new Padding(0, 5, 0, 0) };
            
            // Group Buttons Layout
            var btnAddGroup = CreateButton("Add Group", (s, e) => AddGroup());
            var btnRemoveGroup = CreateButton("Remove", (s, e) => RemoveGroup());
            var btnUpGroup = CreateButton("▲", (s, e) => MoveGroup(-1), 30);
            var btnDownGroup = CreateButton("▼", (s, e) => MoveGroup(1), 30);
            var btnSave = CreateButton("Save && Close", (s, e) => SaveConfig());
            btnSave.Name = "btnSave";
            btnSave.BackColor = Color.FromArgb(0, 122, 204);

            var btnClean = CreateButton("Clean .disabled", (s, e) => CleanDisabledFiles());
            btnClean.Dock = DockStyle.Bottom;

            var flowGroupActions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 35, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0) };
            flowGroupActions.Controls.Add(btnAddGroup);
            flowGroupActions.Controls.Add(btnRemoveGroup);
            flowGroupActions.Controls.Add(btnUpGroup);
            flowGroupActions.Controls.Add(btnDownGroup);

            pnlGroupButtons.Controls.Add(btnSave); // Dock Bottom
            btnSave.Dock = DockStyle.Bottom;
            pnlGroupButtons.Controls.Add(btnClean); // Dock Bottom (above Save)
            pnlGroupButtons.Controls.Add(flowGroupActions); // Dock Top

            pnlLeft.Controls.Add(_lstGroups);
            pnlLeft.Controls.Add(pnlGroupButtons);
            pnlLeft.Controls.Add(lblGroups);

            // --- Right Panel (Main Info) ---
            _pnlMainInfo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), Visible = false };
            splitContainer.Panel2.Controls.Add(_pnlMainInfo);

            var grpMain = new GroupBox { Text = "Mod Information", Dock = DockStyle.Top, Height = 320, ForeColor = Color.White };
            var layoutMain = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(5) };
            layoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _txtModName = new TextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            _txtModAuthor = new TextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            _txtModVersion = new TextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            _txtModDescription = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            
            _picThumbnail = new PictureBox { Width = 160, Height = 90, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 10, 0) };
            var btnChangeThumb = CreateButton("Change...", (s, e) => ChangeThumbnail(), 80);
            var pnlThumb = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            pnlThumb.Controls.Add(_picThumbnail);
            pnlThumb.Controls.Add(btnChangeThumb);

            layoutMain.Controls.Add(new Label { Text = "Name:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.Gainsboro }, 0, 0);
            layoutMain.Controls.Add(_txtModName, 1, 0);
            layoutMain.Controls.Add(new Label { Text = "Author:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.Gainsboro }, 0, 1);
            layoutMain.Controls.Add(_txtModAuthor, 1, 1);
            layoutMain.Controls.Add(new Label { Text = "Version:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.Gainsboro }, 0, 2);
            layoutMain.Controls.Add(_txtModVersion, 1, 2);
            layoutMain.Controls.Add(new Label { Text = "Description:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.Gainsboro }, 0, 3);
            layoutMain.Controls.Add(_txtModDescription, 1, 3);
            layoutMain.Controls.Add(new Label { Text = "Thumbnail:", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, ForeColor = Color.Gainsboro, Margin = new Padding(0, 5, 0, 0) }, 0, 4);
            layoutMain.Controls.Add(pnlThumb, 1, 4);

            grpMain.Controls.Add(layoutMain);
            _pnlMainInfo.Controls.Add(grpMain);

            // --- Right Panel (Group Details) ---
            _pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), Visible = false };
            splitContainer.Panel2.Controls.Add(_pnlRight);
            // Ensure MainInfo is on top if visible, though visibility toggling handles it.

            // 1. Group Properties (Top)
            var grpProps = new GroupBox { Text = "Group Properties", Dock = DockStyle.Top, Height = 130, ForeColor = Color.White };
            var layoutProps = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(5) };
            layoutProps.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layoutProps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblName = new Label { Text = "Name:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.Gainsboro };
            _txtGroupName = new TextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            _txtGroupName.TextChanged += (s, e) => {
                if (_lstGroups.SelectedItem is ConfigGroup g)
                {
                    g.Name = _txtGroupName.Text;
                    // Refresh list display safely
                    int idx = _lstGroups.SelectedIndex;
                    if (idx >= 0 && idx < _lstGroups.Items.Count)
                    {
                        _lstGroups.Items[idx] = g;
                        _lstGroups.Refresh();
                    }
                }
            };

            var lblType = new Label { Text = "Type:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.Gainsboro };
            _cmbGroupType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _cmbGroupType.Items.AddRange(new object[] { "SelectOne", "SelectMultiple" });
            _cmbGroupType.SelectedIndexChanged += (s, e) => { if (_lstGroups.SelectedItem is ConfigGroup g) g.Type = _cmbGroupType.SelectedItem?.ToString() ?? "SelectOne"; };

            var lblDesc = new Label { Text = "Description:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.Gainsboro };
            _txtGroupDesc = new TextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            _txtGroupDesc.TextChanged += (s, e) => { if (_lstGroups.SelectedItem is ConfigGroup g) g.Description = _txtGroupDesc.Text; };

            layoutProps.Controls.Add(lblName, 0, 0); layoutProps.Controls.Add(_txtGroupName, 1, 0);
            layoutProps.Controls.Add(lblType, 0, 1); layoutProps.Controls.Add(_cmbGroupType, 1, 1);
            layoutProps.Controls.Add(lblDesc, 0, 2); layoutProps.Controls.Add(_txtGroupDesc, 1, 2);
            grpProps.Controls.Add(layoutProps);

            // 2. Splitter for Options and Files
            var splitRight = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 200, BackColor = Color.FromArgb(45, 45, 48) };
            
            // 3. Options List (Middle)
            var pnlOptions = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
            var lblOptHeader = new Label { Text = "Options", Dock = DockStyle.Top, Height = 20, Font = new Font(this.Font, FontStyle.Bold), ForeColor = Color.White };
            _lstOptions = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            _lstOptions.SelectedIndexChanged += (s, e) => UpdateFilesList();

            var pnlOptButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 35, FlowDirection = FlowDirection.LeftToRight };
            pnlOptButtons.Controls.Add(CreateButton("Add", (s, e) => AddOption(), 60));
            pnlOptButtons.Controls.Add(CreateButton("Rename", (s, e) => RenameOption(), 70));
            pnlOptButtons.Controls.Add(CreateButton("Remove", (s, e) => RemoveOption(), 70));
            pnlOptButtons.Controls.Add(CreateButton("▲", (s, e) => MoveOption(-1), 30));
            pnlOptButtons.Controls.Add(CreateButton("▼", (s, e) => MoveOption(1), 30));

            pnlOptions.Controls.Add(_lstOptions);
            pnlOptions.Controls.Add(pnlOptButtons);
            pnlOptions.Controls.Add(lblOptHeader);
            splitRight.Panel1.Controls.Add(pnlOptions);

            // 4. Files List (Bottom)
            var pnlFiles = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0) };
            var lblFileHeader = new Label { Text = "Files (Drag && Drop Supported)", Dock = DockStyle.Top, Height = 20, Font = new Font(this.Font, FontStyle.Bold), ForeColor = Color.White };
            _lstFiles = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, AllowDrop = true, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            _lstFiles.DragEnter += _lstFiles_DragEnter;
            _lstFiles.DragDrop += _lstFiles_DragDrop;
            _lstFiles.KeyDown += _lstFiles_KeyDown;
            
            var pnlFileButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 35, FlowDirection = FlowDirection.LeftToRight };
            pnlFileButtons.Controls.Add(CreateButton("Add Files...", BtnAddFile_Click, 90));
            pnlFileButtons.Controls.Add(CreateButton("Remove", (s, e) => RemoveFile(), 70));

            pnlFiles.Controls.Add(_lstFiles);
            pnlFiles.Controls.Add(pnlFileButtons);
            pnlFiles.Controls.Add(lblFileHeader);
            splitRight.Panel2.Controls.Add(pnlFiles);

            // Add to Right Panel (Order matters for Docking: Fill first, then Top)
            _pnlRight.Controls.Add(splitRight);
            _pnlRight.Controls.Add(grpProps);
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
                Margin = new Padding(0, 0, 5, 0),
                Font = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private void LoadConfig()
        {
            _config = new ModConfig();
            if (File.Exists(_configPath))
            {
                try
                {
                    var lines = File.ReadAllLines(_configPath);
                    string currentSection = "";
                    ConfigGroup? currentGroup = null;
                    var tempFiles = new Dictionary<string, string>();

                    foreach (var line in lines)
                    {
                        var trim = line.Trim();
                        if (string.IsNullOrWhiteSpace(trim) || trim.StartsWith(";") || trim.StartsWith("#")) continue;

                        if (trim.StartsWith("[") && trim.EndsWith("]"))
                        {
                            currentSection = trim.Substring(1, trim.Length - 2);
                            if (currentSection.StartsWith("Config:", StringComparison.OrdinalIgnoreCase))
                            {
                                var groupName = currentSection.Substring(7);
                                currentGroup = new ConfigGroup { Name = groupName };
                                _config.Groups.Add(currentGroup);
                            }
                            else
                            {
                                currentGroup = null;
                            }
                            continue;
                        }

                        var parts = trim.Split(new[] { '=' }, 2);
                        if (parts.Length != 2) continue;
                        var key = parts[0].Trim();
                        var val = parts[1].Trim();
                        val = val.Trim('"');

                        if (currentSection.Equals("Main", StringComparison.OrdinalIgnoreCase))
                        {
                            _config.MainSection[key] = val;
                        }
                        else if (currentSection.Equals("Files", StringComparison.OrdinalIgnoreCase))
                        {
                            tempFiles[key] = val;
                        }
                        else if (currentGroup != null)
                        {
                            if (key.Equals("Type", StringComparison.OrdinalIgnoreCase)) currentGroup.Type = val;
                            else if (key.Equals("Description", StringComparison.OrdinalIgnoreCase)) currentGroup.Description = val;
                            else if (key.Equals("Options", StringComparison.OrdinalIgnoreCase))
                            {
                                // Options=Vanilla,Grand Prix Final Laps
                                var opts = val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var o in opts)
                                {
                                    currentGroup.Options.Add(new ConfigOption { Name = o.Trim() });
                                }
                            }
                        }
                    }

                    // Map files to options
                    foreach (var kvp in tempFiles)
                    {
                        var file = kvp.Key;
                        var pointer = kvp.Value; // Group.Option
                        
                        // Find the group that matches the start of the pointer
                        var group = _config.Groups.FirstOrDefault(g => pointer.StartsWith(g.Name + ".", StringComparison.OrdinalIgnoreCase));
                        if (group != null)
                        {
                            var optionName = pointer.Substring(group.Name.Length + 1);
                            var opt = group.Options.FirstOrDefault(o => o.Name.Equals(optionName, StringComparison.OrdinalIgnoreCase));
                            opt?.Files.Add(file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Error loading config: {ex.Message}");
                }
            }
            else
            {
                // Generate template defaults
                _config.MainSection["Name"] = new DirectoryInfo(_modPath).Name;
                _config.MainSection["Version"] = "1.0";
                _config.MainSection["Author"] = "";
                _config.MainSection["Description"] = "";
            }

            // Populate Main Info fields
            _txtModName.Text = _config.MainSection.ContainsKey("Name") ? _config.MainSection["Name"] : new DirectoryInfo(_modPath).Name;
            _txtModAuthor.Text = _config.MainSection.ContainsKey("Author") ? _config.MainSection["Author"] : "";
            _txtModVersion.Text = _config.MainSection.ContainsKey("Version") ? _config.MainSection["Version"] : "1.0";
            _txtModDescription.Text = _config.MainSection.ContainsKey("Description") ? _config.MainSection["Description"] : "";

            RefreshGroupList();
            LoadThumbnail();
        }

        private void RefreshGroupList()
        {
            _lstGroups.Items.Clear();
            foreach (var grp in _config.Groups)
            {
                _lstGroups.Items.Add(grp);
            }
            _lstGroups.Items.Insert(0, new MainInfoPlaceholder());
            if (_lstGroups.Items.Count > 0) _lstGroups.SelectedIndex = 0;
        }

        private void UpdateRightPanel()
        {
            if (_lstGroups.SelectedItem is ConfigGroup grp)
            {
                _pnlMainInfo.Visible = false;
                _pnlRight.Visible = true;
                _txtGroupName.Text = grp.Name;
                _cmbGroupType.SelectedItem = grp.Type;
                _txtGroupDesc.Text = grp.Description;

                _lstOptions.Items.Clear();
                foreach (var opt in grp.Options) _lstOptions.Items.Add(opt);
                
                if (_lstOptions.Items.Count > 0) _lstOptions.SelectedIndex = 0;
                else _lstFiles.Items.Clear();
            }
            else if (_lstGroups.SelectedItem is MainInfoPlaceholder)
            {
                _pnlRight.Visible = false;
                _pnlMainInfo.Visible = true;
            }
            else
            {
                _pnlRight.Visible = false;
                _pnlMainInfo.Visible = false;
            }
        }

        private void UpdateFilesList()
        {
            _lstFiles.Items.Clear();
            if (_lstOptions.SelectedItem is ConfigOption opt)
            {
                foreach (var f in opt.Files) _lstFiles.Items.Add(f);
            }
        }

        private void AddGroup()
        {
            string name = Prompt.ShowDialog("Enter Group Name:", "New Group", "NewGroup");
            if (string.IsNullOrWhiteSpace(name)) return;
            
            var newGrp = new ConfigGroup { Name = name };
            _config.Groups.Add(newGrp);
            _lstGroups.Items.Add(newGrp);
            _lstGroups.SelectedItem = newGrp;
        }

        private void RemoveGroup()
        {
            if (_lstGroups.SelectedItem is ConfigGroup grp)
            {
                if (CustomMessageBox.Show($"Delete group '{grp.Name}'?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _config.Groups.Remove(grp);
                    _lstGroups.Items.Remove(grp);
                    UpdateRightPanel();
                }
            }
        }

        private void MoveGroup(int direction)
        {
            if (_lstGroups.SelectedItem is not ConfigGroup grp) return;
            int idx = _config.Groups.IndexOf(grp);
            int newIdx = idx + direction;
            if (newIdx < 0 || newIdx >= _config.Groups.Count) return;

            _config.Groups.RemoveAt(idx);
            _config.Groups.Insert(newIdx, grp);
            
            RefreshGroupList();
            _lstGroups.SelectedItem = grp;
        }

        private void AddOption()
        {
            if (_lstGroups.SelectedItem is ConfigGroup grp)
            {
                string name = Prompt.ShowDialog("Enter Option Name:", "New Option", "NewOption");
                if (string.IsNullOrWhiteSpace(name)) return;

                var newOpt = new ConfigOption { Name = name };
                grp.Options.Add(newOpt);
                _lstOptions.Items.Add(newOpt);
                _lstOptions.SelectedItem = newOpt;
            }
        }

        private void RenameOption()
        {
            if (_lstOptions.SelectedItem is ConfigOption opt)
            {
                string name = Prompt.ShowDialog("Enter new name:", "Rename Option", opt.Name);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    opt.Name = name;
                    // Refresh list item text safely
                    int idx = _lstOptions.SelectedIndex;
                    if (idx >= 0 && idx < _lstOptions.Items.Count)
                    {
                        _lstOptions.Items[idx] = opt;
                        _lstOptions.Refresh();
                    }
                }
            }
        }

        private void RemoveOption()
        {
            if (_lstGroups.SelectedItem is ConfigGroup grp && _lstOptions.SelectedItem is ConfigOption opt)
            {
                if (CustomMessageBox.Show($"Delete option '{opt.Name}'?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    grp.Options.Remove(opt);
                    _lstOptions.Items.Remove(opt);
                    UpdateFilesList();
                }
            }
        }

        private void MoveOption(int direction)
        {
            if (_lstGroups.SelectedItem is not ConfigGroup grp || _lstOptions.SelectedItem is not ConfigOption opt) return;
            int idx = grp.Options.IndexOf(opt);
            int newIdx = idx + direction;
            if (newIdx < 0 || newIdx >= grp.Options.Count) return;

            grp.Options.RemoveAt(idx);
            grp.Options.Insert(newIdx, opt);

            _lstOptions.Items.Clear();
            foreach (var o in grp.Options) _lstOptions.Items.Add(o);
            _lstOptions.SelectedItem = opt;
        }

        private void BtnAddFile_Click(object? sender, EventArgs e)
        {
            if (!(_lstOptions.SelectedItem is ConfigOption opt)) return;
            
            Dictionary<string, string> filters = new Dictionary<string, string>
            {
                {"Mod Files", "pak,utoc,ucas,json"}
            };

            string? startingPath = Directory.Exists(_modPath) ? _modPath : null;
            
            // TODO: add titlebar text when NativeFileDialogs supports it
            NfdStatus result = Nfd.OpenDialogMultiple(out string[]? filePath, filters, startingPath);

            if (result == NfdStatus.Ok)
            {
                if (filePath is not null) AddFilesToOption(opt, filePath);
            }
        }

        private void AddFilesToOption(ConfigOption opt, string[] filePaths)
        {
            foreach (var file in filePaths)
            {
                // We want the path relative to the mod folder if possible
                string relativePath = file;
                if (file.StartsWith(_modPath, StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = Path.GetRelativePath(_modPath, file);
                }
                else
                {
                    // If it's outside, we might just use the filename, but warn?
                    // For now, just use filename as fallback or relative if inside.
                    // Actually, mod.ini expects files inside the mod folder.
                    // If the user selects a file outside, we should probably copy it or just use the name and assume they will move it.
                    // Let's just use the filename if it's not in the path, assuming it's in the root of the mod.
                    relativePath = Path.GetFileName(file);
                }

                if (!opt.Files.Contains(relativePath))
                {
                    opt.Files.Add(relativePath);
                    _lstFiles.Items.Add(relativePath);
                }
            }
        }

        private void RemoveFile()
        {
            if (_lstOptions.SelectedItem is ConfigOption opt)
            {
                var selectedFiles = _lstFiles.SelectedItems.Cast<string>().ToList();
                foreach (var file in selectedFiles)
                {
                    opt.Files.Remove(file);
                    _lstFiles.Items.Remove(file);
                }
            }
        }

        private void _lstFiles_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete) RemoveFile();
        }

        private void _lstFiles_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void _lstFiles_DragDrop(object? sender, DragEventArgs e)
        {
            if (!(_lstOptions.SelectedItem is ConfigOption opt)) return;
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
            {
                AddFilesToOption(opt, files);
            }
        }

        private void CleanDisabledFiles()
        {
            if (CustomMessageBox.Show("This will rename all '*.disabled' files in the mod folder back to their original names.\n\nUse this before publishing your mod to ensure all files are active.\n\nProceed?", "Clean Disabled Files", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                int count = 0;
                var files = Directory.GetFiles(_modPath, "*.disabled", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    string newName = file.Substring(0, file.Length - ".disabled".Length);
                    if (File.Exists(newName)) File.Delete(newName);
                    File.Move(file, newName);
                    count++;
                }
                CustomMessageBox.Show($"Cleaned {count} file(s).", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error cleaning files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadThumbnail()
        {
            string[] extensions = { ".jpg", ".png", ".jpeg", ".bmp", ".gif" };
            foreach (var ext in extensions)
            {
                var p = Path.Combine(_modPath, "Thumb" + ext);
                if (File.Exists(p))
                {
                    try
                    {
                        using (var stream = new FileStream(p, FileMode.Open, FileAccess.Read))
                        {
                            _picThumbnail.Image = Image.FromStream(stream);
                        }
                    }
                    catch { }
                    break;
                }
            }
        }

        private void ChangeThumbnail()
        {
            Dictionary<string, string> filters = new Dictionary<string, string>
            {
                {"Image Files", "jpg,png,jpeg,bmp,gif"}
            };
            
            // TODO: add titlebar text when NativeFileDialogs supports it
            NfdStatus result = Nfd.OpenDialog(out string? filePath, filters);

            if (result == NfdStatus.Ok)
            {
                if (filePath is null) return;
                
                _pendingThumbnailPath = filePath;
                try
                {
                    using (var stream = new FileStream(_pendingThumbnailPath, FileMode.Open, FileAccess.Read))
                    {
                        _picThumbnail.Image = Image.FromStream(stream);
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Error loading image: {ex.Message}");
                }
            }
        }

        private void SaveConfig()
        {
            var sb = new StringBuilder();
            
            // [Main]
            _config.MainSection["Name"] = _txtModName.Text;
            _config.MainSection["Author"] = _txtModAuthor.Text;
            _config.MainSection["Version"] = _txtModVersion.Text;
            _config.MainSection["Description"] = _txtModDescription.Text;

            sb.AppendLine("[Main]");
            foreach (var kvp in _config.MainSection)
            {
                sb.AppendLine($"{kvp.Key}={kvp.Value}");
            }
            sb.AppendLine();

            // [Config:Group]
            foreach (var grp in _config.Groups)
            {
                sb.AppendLine($"[Config:{grp.Name}]");
                sb.AppendLine($"Type={grp.Type}");
                sb.AppendLine($"Description={grp.Description}");
                sb.AppendLine($"Options={string.Join(",", grp.Options.Select(o => o.Name))}");
                sb.AppendLine();
            }

            // [Files]
            sb.AppendLine("[Files]");
            foreach (var grp in _config.Groups)
            {
                foreach (var opt in grp.Options)
                {
                    foreach (var file in opt.Files)
                    {
                        sb.AppendLine($"{file}={grp.Name}.{opt.Name}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(_pendingThumbnailPath) && File.Exists(_pendingThumbnailPath))
            {
                try
                {
                    string[] extensions = { ".jpg", ".png", ".jpeg", ".bmp", ".gif" };
                    foreach (var ext in extensions)
                    {
                        var p = Path.Combine(_modPath, "Thumb" + ext);
                        if (File.Exists(p)) File.Delete(p);
                    }
                    
                    string dest = Path.Combine(_modPath, "Thumb" + Path.GetExtension(_pendingThumbnailPath));
                    File.Copy(_pendingThumbnailPath, dest, true);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Error saving thumbnail: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            File.WriteAllText(_configPath, sb.ToString());
            CustomMessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
#pragma warning restore CA1416
}
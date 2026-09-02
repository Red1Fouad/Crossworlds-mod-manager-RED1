using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using NativeFileDialogs.Net;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public class TextCreatorForm : Form
    {
        private readonly string _targetFileName;
        private readonly string _sourceJsonPath;
        private readonly string _languageCode;
        private List<GameLocStringEntry> _sourceData = new();
        private List<ModLocEntry> _modData = new();

        private DataGridView? dgvSource;
        private DataGridView? dgvMod;
        private TextBox? txtSearch;
        private ComboBox? cmbTargetLang;
        private CheckBox? chkCaseSensitive;
        private TextBox? txtFindInMod;
        private TextBox? txtReplaceWith;
        private CheckBox? chkReplaceCaseSensitive;
        private Button? btnAdd;
        private Button? btnRemove;
        private Button? btnSave;
        private Button? btnReplace;
        private Label? lblStatus;

        public TextCreatorForm(string targetFileName, string sourceJsonPath, string languageCode)
        {
            _targetFileName = targetFileName;
            _sourceJsonPath = sourceJsonPath;
            _languageCode = languageCode;
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            LoadGameDataAsync();
        }

        private void InitializeComponent()
        {
            this.Text = $"Text Change Tool - {_targetFileName}";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 90,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Left Panel (Source)
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var lblSource = new Label { Text = "Game Text (Source)", Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 40 };
            var lblSearch = new Label { Text = "Search:", Location = new Point(0, 8), AutoSize = true };
            txtSearch = new TextBox { Location = new Point(60, 5), Width = 250, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            chkCaseSensitive = new CheckBox 
            { 
                Text = "Case Sensitive", 
                Location = new Point(320, 5), 
                AutoSize = true,
                ForeColor = Color.White
            };
            chkCaseSensitive.CheckedChanged += (s, e) => UpdateSourceList(txtSearch.Text);

            pnlSearch.Controls.Add(lblSearch);
            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Controls.Add(chkCaseSensitive);

            // New Language Selection Panel
            var pnlAddOptions = new Panel { Dock = DockStyle.Bottom, Height = 35, Padding = new Padding(0, 5, 0, 0) };
            var lblTargetLang = new Label { Text = "Target Lang:", AutoSize = true, Location = new Point(0, 8), ForeColor = Color.White };
            cmbTargetLang = new ComboBox { Location = new Point(80, 5), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cmbTargetLang.Items.AddRange(new object[] { "en", "fr", "it", "de", "es", "es-US", "ru", "ja", "ko", "zh-Hans", "zh-Hant", "pt", "pl", "th" });
            if (cmbTargetLang.Items.Contains(_languageCode)) cmbTargetLang.SelectedItem = _languageCode;
            else if (cmbTargetLang.Items.Count > 0) cmbTargetLang.SelectedIndex = 0;
            else cmbTargetLang.Text = _languageCode;

            pnlAddOptions.Controls.Add(lblTargetLang);
            pnlAddOptions.Controls.Add(cmbTargetLang);

            dgvSource = CreateDataGridView();
            dgvSource.Dock = DockStyle.Fill;
            dgvSource.Columns.Add("Namespace", "Namespace");
            dgvSource.Columns.Add("Key", "Key");
            dgvSource.Columns.Add("Value", "Current Text");
            dgvSource.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvSource.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSource.DoubleClick += (s, e) => AddSelectedToMod();

            btnAdd = new Button { Text = "Add to Mod >>", Dock = DockStyle.Bottom, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) => AddSelectedToMod();

            pnlLeft.Controls.Add(dgvSource);
            pnlLeft.Controls.Add(pnlAddOptions);
            pnlLeft.Controls.Add(btnAdd);
            pnlLeft.Controls.Add(pnlSearch);
            pnlLeft.Controls.Add(lblSource);

            // Right Panel (Mod)
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var lblMod = new Label { Text = "Mod Text (Changes)", Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };

            // New Find/Replace Panel
            var pnlReplace = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 5, 0, 5) };
            var lblFind = new Label { Text = "Find:", AutoSize = true, Margin = new Padding(3, 8, 3, 0) };
            txtFindInMod = new TextBox { Width = 120, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            var lblReplace = new Label { Text = "Replace:", AutoSize = true, Margin = new Padding(3, 8, 3, 0) };
            txtReplaceWith = new TextBox { Width = 120, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            chkReplaceCaseSensitive = new CheckBox { Text = "Case Sensitive", AutoSize = true, ForeColor = Color.White, Margin = new Padding(3, 5, 3, 0) };
            btnReplace = new Button { Text = "Replace All", Width = 90, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(63, 63, 70), ForeColor = Color.White };
            btnReplace.FlatAppearance.BorderSize = 0;
            btnReplace.Click += BtnReplace_Click;

            pnlReplace.Controls.Add(lblFind);
            pnlReplace.Controls.Add(txtFindInMod);
            pnlReplace.Controls.Add(lblReplace);
            pnlReplace.Controls.Add(txtReplaceWith);
            pnlReplace.Controls.Add(chkReplaceCaseSensitive);
            pnlReplace.Controls.Add(btnReplace);

            dgvMod = CreateDataGridView();
            dgvMod.Dock = DockStyle.Fill;
            dgvMod.Columns.Add("Language", "Lang");
            dgvMod.Columns.Add("Namespace", "Namespace");
            dgvMod.Columns.Add("Key", "Key");
            dgvMod.Columns.Add("Value", "New Text (Double Click)");
            dgvMod.Columns[0].Width = 60;
            dgvMod.Columns[1].ReadOnly = true;
            dgvMod.Columns[2].ReadOnly = true;
            dgvMod.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvMod.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMod.CellValueChanged += DgvMod_CellValueChanged;

            var pnlButtons = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            btnRemove = new Button { Text = "Remove Selected", Width = 120, Height = 30, Location = new Point(0, 5), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(192, 57, 43), ForeColor = Color.White };
            btnRemove.FlatAppearance.BorderSize = 0;
            btnRemove.Click += (s, e) => RemoveSelectedFromMod();

            btnSave = new Button { Text = "Save JSON", Width = 120, Height = 30, Location = new Point(130, 5), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => SaveJson();

            pnlButtons.Controls.Add(btnRemove);
            pnlButtons.Controls.Add(btnSave);

            pnlRight.Controls.Add(dgvMod);
            pnlRight.Controls.Add(pnlButtons);
            pnlRight.Controls.Add(pnlReplace);
            pnlRight.Controls.Add(lblMod);

            splitContainer.Panel1.Controls.Add(pnlLeft);
            splitContainer.Panel2.Controls.Add(pnlRight);

            lblStatus = new Label { Dock = DockStyle.Bottom, Height = 25, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.FromArgb(30, 30, 30) };

            this.Controls.Add(splitContainer);
            this.Controls.Add(lblStatus);
        }

        private DataGridView CreateDataGridView()
        {
            var dgv = new DataGridView();
            dgv.BackgroundColor = Color.FromArgb(30, 30, 30);
            dgv.ForeColor = Color.White;
            dgv.GridColor = Color.FromArgb(60, 60, 60);
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BorderStyle = BorderStyle.None;
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            dgv.DefaultCellStyle.ForeColor = Color.White;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 122, 204);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            return dgv;
        }

        private async void LoadGameDataAsync()
        {
            lblStatus!.Text = "Loading Game.json...";
            try
            {
                await Task.Run(() =>
                {
                    var jsonString = File.ReadAllText(_sourceJsonPath);
                    var root = JsonSerializer.Deserialize<GameLocRoot>(jsonString);
                    
                    _sourceData.Clear();
                    if (root?.Items != null)
                    {
                        foreach (var item in root.Items)
                        {
                            if (item.Namespaces != null)
                            {
                                foreach (var ns in item.Namespaces)
                                {
                                    if (ns.Strings != null)
                                    {
                                        foreach (var str in ns.Strings)
                                        {
                                            _sourceData.Add(new GameLocStringEntry
                                            {
                                                Namespace = ns.Name ?? "",
                                                Key = str.Key ?? "",
                                                Value = str.Value ?? ""
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                });

                UpdateSourceList("");
                lblStatus.Text = $"Loaded {_sourceData.Count} text entries.";

                if (File.Exists(_targetFileName))
                {
                    LoadExistingMod(_targetFileName);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to load Game.json: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error loading data.";
            }
        }

        private void LoadExistingMod(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                var loadedData = JsonSerializer.Deserialize<List<ModLocEntry>>(json);
                if (loadedData != null)
                {
                    _modData = loadedData;
                    dgvMod!.Rows.Clear();
                    foreach (var item in _modData)
                    {
                        int idx = dgvMod.Rows.Add(item.Language, item.Namespace, item.Key, item.Value);
                        dgvMod.Rows[idx].Tag = item;
                    }
                    lblStatus!.Text = $"Loaded existing mod with {_modData.Count} entries.";
                    CustomMessageBox.Show($"Loaded {_modData.Count} entries from existing file.", "Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to load existing mod file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateSourceList(string filter)
        {
            dgvSource!.Rows.Clear();
            StringComparison comparison = chkCaseSensitive!.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            var items = string.IsNullOrWhiteSpace(filter) 
                ? _sourceData 
                : _sourceData.Where(x => x.Key.Contains(filter, comparison) || 
                                         x.Value.Contains(filter, comparison) ||
                                         x.Namespace.Contains(filter, comparison)).ToList();

            // Limit display to avoid UI lag if too many items
            int limit = 2000;
            foreach (var item in items.Take(limit))
            {
                dgvSource.Rows.Add(item.Namespace, item.Key, item.Value);
            }
            
            if (items.Count > limit)
            {
                lblStatus!.Text = $"Showing first {limit} results of {items.Count}. Please refine search.";
            }
        }

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            UpdateSourceList(txtSearch!.Text);
        }

        private void AddSelectedToMod()
        {
            string targetLang = cmbTargetLang?.SelectedItem?.ToString() ?? _languageCode;

            foreach (DataGridViewRow row in dgvSource!.SelectedRows)
            {
                string ns = row.Cells[0].Value?.ToString() ?? "";
                string key = row.Cells[1].Value?.ToString() ?? "";
                string val = row.Cells[2].Value?.ToString() ?? "";

                // Check if already exists for this language
                if (_modData.Any(x => x.Namespace == ns && x.Key == key && x.Language == targetLang)) continue;

                var entry = new ModLocEntry { Language = targetLang, Namespace = ns, Key = key, Value = val };
                _modData.Add(entry);
                int idx = dgvMod!.Rows.Add(targetLang, ns, key, val);
                dgvMod.Rows[idx].Tag = entry;
            }
        }

        private void RemoveSelectedFromMod()
        {
            var rowsToRemove = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in dgvMod!.SelectedRows)
            {
                rowsToRemove.Add(row);
            }

            foreach (var row in rowsToRemove)
            {
                if (row.Tag is ModLocEntry entry)
                {
                    _modData.Remove(entry);
                }
                dgvMod.Rows.Remove(row);
            }
        }

        private void DgvMod_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = dgvMod!.Rows[e.RowIndex];
                if (row.Tag is ModLocEntry entry)
                {
                    if (e.ColumnIndex == 0) // Language column
                    {
                        entry.Language = row.Cells[0].Value?.ToString() ?? "";
                    }
                    else if (e.ColumnIndex == 3) // Value column
                    {
                        entry.Value = row.Cells[3].Value?.ToString() ?? "";
                    }
                }
            }
        }

        private void SaveJson()
        {
            if (_modData.Count == 0)
            {
                CustomMessageBox.Show("No changes to save.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Filter out items that haven't actually changed from the source
            var changesToSave = new List<ModLocEntry>();
            foreach (var modItem in _modData)
            {
                // If language differs from source, always save (it's a translation or new entry)
                if (modItem.Language != _languageCode)
                {
                    changesToSave.Add(modItem);
                    continue;
                }

                var originalItem = _sourceData.FirstOrDefault(s => s.Namespace == modItem.Namespace && s.Key == modItem.Key);
                
                // If it's a new key (not in source) OR the value is different, save it.
                if (originalItem == null || !string.Equals(originalItem.Value, modItem.Value, StringComparison.Ordinal))
                {
                    changesToSave.Add(modItem);
                }
            }

            if (changesToSave.Count == 0)
            {
                CustomMessageBox.Show("No text changes detected compared to the original game text.\n\nAdd items from the left and modify their text on the right to create a mod.", "No Changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Dictionary<string, string> filters = new Dictionary<string, string>
            {
                {"JSON Files", "json"}
            };
            // TODO: add titlebar text when NativeFileDialogs supports it
            NfdStatus result = Nfd.SaveDialog(out string? fileName, filters, Path.GetFileName(_targetFileName));

            if (result == NfdStatus.Ok)
            {
                if (fileName is null) return;
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(changesToSave, options);
                    File.WriteAllText(fileName, json);
                    CustomMessageBox.Show($"Saved {changesToSave.Count} entries to:\n{fileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnReplace_Click(object? sender, EventArgs e)
        {
            string findText = txtFindInMod!.Text;
            string replaceText = txtReplaceWith!.Text;

            if (string.IsNullOrEmpty(findText))
            {
                CustomMessageBox.Show("Please enter text to find.", "Find and Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var comparison = chkReplaceCaseSensitive!.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int replacements = 0;

            // Update the underlying data model
            foreach (var entry in _modData)
            {
                if (entry.Value.Contains(findText, comparison))
                {
                    entry.Value = entry.Value.Replace(findText, replaceText, comparison);
                    replacements++;
                }
            }

            if (replacements > 0)
            {
                // Refresh the DataGridView from the updated data model
                dgvMod!.Rows.Clear();
                foreach (var item in _modData)
                {
                    int idx = dgvMod.Rows.Add(item.Language, item.Namespace, item.Key, item.Value);
                    dgvMod.Rows[idx].Tag = item;
                }
                CustomMessageBox.Show($"{replacements} occurrence(s) replaced.", "Replace Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                CustomMessageBox.Show("Text not found in the mod changes list.", "Find and Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Data Classes for Game.json
        private class GameLocRoot
        {
            public List<GameLocItem>? Items { get; set; }
        }
        private class GameLocItem
        {
            public List<GameLocNamespace>? Namespaces { get; set; }
        }
        private class GameLocNamespace
        {
            public string? Name { get; set; }
            public List<GameLocString>? Strings { get; set; }
        }
        private class GameLocString
        {
            public string? Key { get; set; }
            public string? Value { get; set; }
        }

        // Internal class for flat list
        private class GameLocStringEntry
        {
            public string Namespace { get; set; } = "";
            public string Key { get; set; } = "";
            public string Value { get; set; } = "";
        }

        // Data Class for Mod JSON
        private class ModLocEntry
        {
            public string Language { get; set; } = "";
            public string Namespace { get; set; } = "";
            public string Key { get; set; } = "";
            public string Value { get; set; } = "";
        }
    }
#pragma warning restore CA1416
}
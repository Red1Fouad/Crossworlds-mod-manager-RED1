using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing;
using System.IO.Compression;
using System.IO.Pipes;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using NativeFileDialogs.AutoGen;
using NativeFileDialogs.Net;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public partial class MainForm : Form
    {
        // This is a common executable name pattern for Unreal Engine games.
        // We check for the process name without the .exe extension.
        private const string GameProcessName = "SonicRacingCrossWorldsSteam";
        private Dictionary<string, (string Path, string? AppName)> _gameInstallations = new();
        private List<ListViewItem> _allModItems = new List<ListViewItem>();
        private string? _selectedPlatform;
        private readonly string? _oneClickUrl;
        private ToolStripMenuItem? renameToolStripMenuItem;
        private ToolStripMenuItem? changeColorToolStripMenuItem;
        private ToolStripMenuItem? configMakerItem;
        private ToolStripMenuItem? normalizeRootItem;
        private int _dropInsertionIndex = -1;
        private bool _showDropLine;

        public MainForm(string? oneClickUrl, string appVersion)
        {
            InitializeComponent();
            _oneClickUrl = oneClickUrl;

            if (PlatformUtils.IsWindows)
                this.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            else
            {
                string iconPath = Path.Combine(PlatformUtils.GetToolsDir(), "icon.png");
                if (File.Exists(iconPath))
                {
                    using (var bmp = new Bitmap(iconPath))
                        this.Icon = Icon.FromHandle(bmp.GetHicon());
                }
            }

            // Use the version in the title bar
            this.Text = $"Blue Star Manager v{appVersion} - A Sonic Racing: CrossWorlds Mod Manager";

            // Apply the custom dark theme renderer for menus and tool strips
            // ToolStripManager.Renderer is now set by ThemeManager.SetTheme()

            // Add Text Change Tool to Tools menu
            var textChangeToolItem = new ToolStripMenuItem("Text Change Tool");
            textChangeToolItem.ForeColor = Color.White;
            textChangeToolItem.BackColor = Color.FromArgb(45, 45, 48);
            textChangeToolItem.Click += TextChangeToolItem_Click;
            toolsToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            toolsToolStripMenuItem.DropDownItems.Add(textChangeToolItem);

            LoadSettingsAndSetup();

            ThemeManager.SetTheme(SettingsManager.Settings.SelectedTheme);
            ThemeManager.ApplyTheme(this);

            // Enable OwnerDraw to customize section appearance
            modListView.OwnerDraw = true;
            modListView.DrawSubItem += modListView_DrawSubItem;
            modListView.DrawColumnHeader += modListView_DrawColumnHeader;
            modListView.ItemCheck += modListView_ItemCheck;

            // Initialize details pane with default state (icon)
            UpdateModDetails(null);

            // Enable drag-and-drop for reordering
            modListView.AllowDrop = true;
            modListView.ItemDrag += modListView_ItemDrag;
            modListView.DragEnter += modListView_DragEnter;
            modListView.DragOver += modListView_DragOver;
            modListView.DragLeave += modListView_DragLeave;
            modListView.DragDrop += modListView_DragDrop;
            modListView.Paint += modListView_Paint;

            // Add Mod Config Maker to context menu
            configMakerItem = new ToolStripMenuItem("Mod Config Maker");
            configMakerItem.ForeColor = Color.White;
            configMakerItem.BackColor = Color.FromArgb(45, 45, 48);
            configMakerItem.Click += ConfigMakerItem_Click;
            
            normalizeRootItem = new ToolStripMenuItem("Normalize Mod Root");
            normalizeRootItem.ForeColor = Color.White;
            normalizeRootItem.BackColor = Color.FromArgb(45, 45, 48);
            normalizeRootItem.Click += NormalizeRootItem_Click;
            
            if (modContextMenuStrip != null)
            {
                renameToolStripMenuItem = new ToolStripMenuItem("Rename");
                renameToolStripMenuItem.ForeColor = Color.White;
                renameToolStripMenuItem.BackColor = Color.FromArgb(45, 45, 48);
                renameToolStripMenuItem.Click += RenameSection_Click;
                modContextMenuStrip.Items.Insert(0, renameToolStripMenuItem);

                changeColorToolStripMenuItem = new ToolStripMenuItem("Change Color");
                changeColorToolStripMenuItem.ForeColor = Color.White;
                changeColorToolStripMenuItem.BackColor = Color.FromArgb(45, 45, 48);
                changeColorToolStripMenuItem.Click += ChangeSectionColor_Click;
                modContextMenuStrip.Items.Insert(1, changeColorToolStripMenuItem);

                var addSectionItem = new ToolStripMenuItem("Add Section");
                addSectionItem.ForeColor = Color.White;
                addSectionItem.BackColor = Color.FromArgb(45, 45, 48);
                addSectionItem.Click += AddSection_Click;

                modContextMenuStrip.Items.Add(new ToolStripSeparator());
                modContextMenuStrip.Items.Add(configMakerItem);
                modContextMenuStrip.Items.Add(normalizeRootItem);
                modContextMenuStrip.Items.Add(addSectionItem);
            }

            // Make top-level menus change color when their dropdown is opened/closed
            try
            {
                fileToolStripMenuItem.DropDownOpened += ParentMenu_DropDownOpened;
                fileToolStripMenuItem.DropDownClosed += ParentMenu_DropDownClosed;
                toolsToolStripMenuItem.DropDownOpened += ParentMenu_DropDownOpened;
                toolsToolStripMenuItem.DropDownClosed += ParentMenu_DropDownClosed;
                profilesToolStripMenuItem.DropDownOpened += ParentMenu_DropDownOpened;
                profilesToolStripMenuItem.DropDownClosed += ParentMenu_DropDownClosed;
                groupsToolStripMenuItem.DropDownOpened += ParentMenu_DropDownOpened;
                groupsToolStripMenuItem.DropDownClosed += ParentMenu_DropDownClosed;
            }
            catch
            {
                // Ignore if designer names differ or items not present
            }

            StartPipeServer();
        }

        private void RenameSection_Click(object? sender, EventArgs e)
        {
            if (modListView.SelectedItems.Count != 1) return;
            var item = modListView.SelectedItems[0];
            if (item.Tag is ModSection section)
            {
                string newName = Prompt.ShowDialog("Enter new section name:", "Rename Section", section.Name);
                if (!string.IsNullOrWhiteSpace(newName) && newName != section.Name)
                {
                    section.Name = newName;
                    item.Text = newName;
                    modListView.Invalidate(item.Bounds);
                    SaveModListState();
                }
            }
        }

        private void ChangeSectionColor_Click(object? sender, EventArgs e)
        {
            if (modListView.SelectedItems.Count != 1) return;
            var item = modListView.SelectedItems[0];
            if (item.Tag is ModSection section)
            {
                using (var cd = new ColorDialog())
                {
                    cd.Color = section.TextColor ?? ThemeManager.CurrentTheme.AccentColor;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        section.TextColor = cd.Color;
                        modListView.Invalidate(item.Bounds);
                        SaveModListState();
                    }
                }
            }
        }

        private void AddSection_Click(object? sender, EventArgs e)
        {
            string name = Prompt.ShowDialog("Enter section name:", "Add Section");
            if (string.IsNullOrWhiteSpace(name)) return;

            var item = CreateSectionListViewItem(name);

            int insertIndex = _allModItems.Count;
            if (modListView.SelectedItems.Count > 0)
            {
                var selectedItem = modListView.SelectedItems[0];
                insertIndex = _allModItems.IndexOf(selectedItem);
                if (insertIndex == -1) insertIndex = _allModItems.Count;
            }

            _allModItems.Insert(insertIndex, item);
            ApplyFilter();
            SaveModListState();
            UpdateStatus($"Added section '{name}'.");
        }

        private void TextChangeToolItem_Click(object? sender, EventArgs e)
        {
            using (var nameForm = new TextCreatorFileNameForm())
            {
                if (nameForm.ShowDialog(this) != DialogResult.OK) return;

                string fileName = nameForm.FileName;
                if (string.IsNullOrWhiteSpace(fileName)) return;
                if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    fileName += ".json";

                string workDir = PlatformUtils.GetWorkDir();

                string gameJsonPath = Path.Combine(workDir, "Game.json");
                string selectedLanguage = "en"; // Default language
                bool proceedToEditor = false;

                if (File.Exists(gameJsonPath))
                {
                    //var result = CustomMessageBox.Show(
                    //    "An existing Game.json was found in the Tools folder.\n\n" +
                    //    "Would you like to create a new one from the game's files (Yes), or use the existing one (No)?",
                    //    "Game.json Found",
                    //    MessageBoxButtons.YesNoCancel,
                    //    MessageBoxIcon.Question);

                    //if (result == DialogResult.Cancel) return;
                    //if (result == DialogResult.No)
                    //{
                    //    // Assume existing is English. User can regenerate if they need another language.
                    //    proceedToEditor = true;
                    //}
                }

                if (!proceedToEditor) // This block runs if Game.json doesn't exist OR user chose to create a new one.
                {
                    string? localizationPath = null;

                    // 1. Try Game Directory
                    if (!string.IsNullOrEmpty(_selectedPlatform) && _gameInstallations.TryGetValue(_selectedPlatform, out var gameInfo))
                    {
                        var path = Path.Combine(gameInfo.Path, "UNION", "Content", "Localization", "Game");
                        if (Directory.Exists(path)) localizationPath = path;
                    }

                    // 2. Try Tools Directory
                    if (string.IsNullOrEmpty(localizationPath))
                    {
                        var toolsLocPath = Path.Combine(workDir, "Locres", "UNION", "Content", "Localization", "Game");
                        if (Directory.Exists(toolsLocPath)) localizationPath = toolsLocPath;
                    }

                    if (string.IsNullOrEmpty(localizationPath))
                    {
                        CustomMessageBox.Show("Could not find the game's localization folder.\n\nPlease ensure a game installation is selected or the 'Locres' folder exists in Tools.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var languages = Directory.GetDirectories(localizationPath).Select(Path.GetFileName).Where(f => f != null).Cast<string>().ToList();
                    if (languages.Count == 0)
                    {
                        CustomMessageBox.Show("No languages found in the game's localization folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    using (var langForm = new LanguageSelectionForm(languages!))
                    {
                        if (langForm.ShowDialog(this) != DialogResult.OK || string.IsNullOrEmpty(langForm.SelectedLanguage)) return;
                        selectedLanguage = langForm.SelectedLanguage;
                    }

                    var locresPath = Path.Combine(localizationPath, selectedLanguage, "Game.locres");
                    if (!File.Exists(locresPath))
                    {
                        CustomMessageBox.Show($"Game.locres for language '{selectedLanguage}' not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    using (var progressForm = new ProgressForm("Creating Game.json..."))
                    {
                        bool success = false;
                        progressForm.Shown += async (s, e) => {
                            success = await LocresConverter.ConvertLocresToJsonFile(locresPath, gameJsonPath, progressForm.GetLoggerProgress());
                            progressForm.ShowCompletion(success ? "Game.json created successfully!" : "Failed to create Game.json.");
                        };
                        progressForm.ShowDialog(this);
                        if (!success) return;
                    }
                    proceedToEditor = true;
                }

                if (proceedToEditor)
                {
                    using (var editor = new TextCreatorForm(fileName, gameJsonPath, selectedLanguage))
                    {
                        editor.ShowDialog(this);
                    }
                    RefreshModList();
                }
            }
        }

        private void NormalizeRootItem_Click(object? sender, EventArgs e)
        {
            var selectedItems = modListView.SelectedItems.Cast<ListViewItem>().ToList();
            if (selectedItems.Count == 0)
            {
                CustomMessageBox.Show("Please select one or more mods to normalize.", "Normalize Mod Root", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int success = 0;
            foreach (ListViewItem item in selectedItems)
            {
                if (item.Tag is ModInfo modInfo)
                {
                    try
                    {
                        Program.CheckAndSetModRoot(modInfo.DirectoryPath);
                        success++;
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show($"Failed to normalize '{modInfo.Name}':\n{ex.Message}", "Normalize Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            RefreshModList();
            CustomMessageBox.Show($"Normalization complete for {success} mod(s). Check the debug log or mod_ops.log for details.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadSettingsAndSetup()
        {
            SettingsManager.Load();

            if (SettingsManager.SettingsWereReset)
            {
                CustomMessageBox.Show(
                    "Your settings file was corrupted and has been reset to defaults.\n\nA backup of the corrupted file was saved alongside settings.json.",
                    "Settings Reset", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            MigrateToProfiles();

            if (string.IsNullOrWhiteSpace(SettingsManager.Settings.ModsDirectory))
            {
                PromptForModsDirectory();
            }

            if (SettingsManager.Settings.CheckForGamesOnStartup)
            {
                DetectGameInstallations();
            }

            UpdateProfilesMenu();
            UpdateGroupsMenu();
            // Before doing anything else, create a backup of the mods directory to ModsTemp.
            try
            {
                if (!SettingsManager.Settings.DoNotBackupModsAutomatically && !string.IsNullOrWhiteSpace(SettingsManager.Settings.ModsDirectory) && Directory.Exists(SettingsManager.Settings.ModsDirectory))
                {
                    ModBackupManager.BackupMods(SettingsManager.Settings.ModsDirectory);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create startup mods backup: {ex.Message}");
            }

            // Load the list of mods when the application starts.
            RefreshModList();
        }
        
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // This needs to be called after the main form is shown to position correctly.
            UpdateDeveloperTabVisibility();

            if (!string.IsNullOrEmpty(_oneClickUrl))
            {
                HandleOneClickInstallAsync(_oneClickUrl);
            }

            // Check for updates after the form is visible.
            Program.CheckForUpdates();

            // Check for mod updates after the form is visible.
            _ = CheckForModUpdatesAsync();

            // Show the MegaMan promo popup
            ShowPromoPopup();
        }

        private void StartPipeServer()
        {
            Task.Run(async () =>
            {
                while (!IsDisposed)
                {
                    try
                    {
                        using (var server = new NamedPipeServerStream("CrossworldsModManagerPipe", PipeDirection.In))
                        {
                            await server.WaitForConnectionAsync();
                            using (var reader = new StreamReader(server))
                            {
                                var url = await reader.ReadToEndAsync();
                                if (!string.IsNullOrEmpty(url))
                                {
                                    this.BeginInvoke((Action)(() => HandleOneClickInstallAsync(url)));
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors (e.g. disposal during shutdown)
                    }
                }
            });
        }

        private void DetectGameInstallations()
        {
            _gameInstallations = GameRegistry.FindGameInstallations();
        
            // If we found a game and the settings path is empty, auto-configure it.
            if (_gameInstallations.Any() && string.IsNullOrWhiteSpace(SettingsManager.Settings.GameDirectory))
            {
                var firstInstall = _gameInstallations.First();
                SettingsManager.Settings.GameDirectory = firstInstall.Value.Path;
                SettingsManager.Save();
                UpdateStatus($"Automatically set game directory to: {firstInstall.Value.Path}");
            }
        
            // Register custom (non-Steam) installation if configured
            if (!string.IsNullOrEmpty(SettingsManager.Settings.CustomUnionDirectory))
            {
                var customGameRoot = Path.GetDirectoryName(SettingsManager.Settings.CustomUnionDirectory.TrimEnd('\\', '/'));
                if (!string.IsNullOrEmpty(customGameRoot) && Directory.Exists(customGameRoot))
                {
                    _gameInstallations["Custom"] = (customGameRoot, null);
                }
            }
        
            launchPlatformDropDown.DropDownItems.Clear();
        
            // Always provide options for Steam, using detected paths or falling back to the settings path.
            var platformsToShow = new List<string> { "Steam" };
            foreach (var platformName in platformsToShow)
            {
                var item = new ToolStripMenuItem(platformName);
                item.Tag = platformName;
                item.Click += PlatformMenuItem_Click;
                item.ForeColor = Color.White;
                item.BackColor = Color.FromArgb(45, 45, 48);
                launchPlatformDropDown.DropDownItems.Add(item);
            }
        
            // Add Custom platform option if configured
            if (_gameInstallations.ContainsKey("Custom"))
            {
                var customPlatformItem = new ToolStripMenuItem("Custom");
                customPlatformItem.Tag = "Custom";
                customPlatformItem.Click += PlatformMenuItem_Click;
                customPlatformItem.ForeColor = Color.White;
                customPlatformItem.BackColor = Color.FromArgb(45, 45, 48);
                launchPlatformDropDown.DropDownItems.Add(customPlatformItem);
            }
        
            // Always add "Executable" option.
            var customItem = new ToolStripMenuItem("Executable");
            customItem.Tag = "Executable";
            customItem.Click += PlatformMenuItem_Click;
            customItem.ForeColor = Color.White;
            customItem.BackColor = Color.FromArgb(45, 45, 48);
            launchPlatformDropDown.DropDownItems.Add(customItem);
        
            // Select the preferred platform if set, otherwise fallback to detection or first item.
            ToolStripMenuItem? itemToSelect = null;

            if (!string.IsNullOrEmpty(SettingsManager.Settings.PreferredLaunchPlatform))
            {
                itemToSelect = launchPlatformDropDown.DropDownItems.Cast<ToolStripMenuItem>()
                    .FirstOrDefault(i => (string?)i.Tag == SettingsManager.Settings.PreferredLaunchPlatform);
            }

            if (itemToSelect == null && _gameInstallations.Any())
            {
                itemToSelect = launchPlatformDropDown.DropDownItems.Cast<ToolStripMenuItem>().FirstOrDefault(i => i.Text == _gameInstallations.Keys.First());
            }

            if (itemToSelect == null && launchPlatformDropDown.DropDownItems.Count > 0)
            {
                itemToSelect = launchPlatformDropDown.DropDownItems[0] as ToolStripMenuItem;
            }

            if (itemToSelect != null)
                PlatformMenuItem_Click(itemToSelect, EventArgs.Empty);
        }

        private void PlatformMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not string platform) return;

            if (platform == "Executable")
            {
                bool pathSet = !string.IsNullOrEmpty(SettingsManager.Settings.GameDirectory) && 
                               !string.IsNullOrEmpty(SettingsManager.Settings.GameExecutableName) &&
                               File.Exists(Path.Combine(SettingsManager.Settings.GameDirectory, SettingsManager.Settings.GameExecutableName));
                
                if (!pathSet)
                {
                    Dictionary<string, string> filters = new Dictionary<string, string>
                    {
                        {"Game Executable", "SonicRacingCrossWorlds.exe"}
                    };
                    // TODO: add titlebar text when NativeFileDialogs supports it
                    NfdStatus result = Nfd.OpenDialog(out string? fileName, filters);

                    if (result == NfdStatus.Ok)
                    {
                        if (fileName == null) return;
                        SettingsManager.Settings.GameDirectory = Path.GetDirectoryName(fileName);
                        SettingsManager.Settings.GameExecutableName = Path.GetFileName(fileName);
                        SettingsManager.Save();
                    }
                    else return;
                }
            }
            else if (platform == "Custom")
            {
                if (!string.IsNullOrEmpty(SettingsManager.Settings.CustomUnionDirectory))
                {
                    var gameRoot = Path.GetDirectoryName(SettingsManager.Settings.CustomUnionDirectory.TrimEnd('\\', '/'));
                    if (!string.IsNullOrEmpty(gameRoot))
                    {
                        SettingsManager.Settings.GameDirectory = gameRoot;
                        SettingsManager.Settings.GameExecutableName = null;
                        SettingsManager.Save();
                    }
                }
            }

            _selectedPlatform = platform; // This is safe now
            launchPlatformDropDown.Text = _selectedPlatform; // This is safe now
            
            if (SettingsManager.Settings.PreferredLaunchPlatform != platform)
            {
                SettingsManager.Settings.PreferredLaunchPlatform = platform;
                SettingsManager.Save();
            }

            UpdateStatus($"Selected platform: {_selectedPlatform}");
        }
        
        /// <summary>
        /// Applies mod configuration by enabling/disabling files based on selected options.
        /// </summary>
        /// <param name="modInfo">The mod to configure.</param>
        /// <param name="selectedOptionIdentifiers">A list of "GroupName.OptionName" identifiers for the enabled options.</param>
        private void ApplyModConfiguration(ModInfo modInfo, List<string> selectedOptionIdentifiers)
        {
            if (modInfo.FileGroupMappings.Count == 0) return;

            // A special identifier "enable" is used for simple, non-configurable mods that are checked.
            // This block handles enabling all files for such mods.
            if (selectedOptionIdentifiers.Contains("enable"))
            {
                if (!Directory.Exists(modInfo.DirectoryPath)) return;

                try
                {
                    foreach (var file in Directory.EnumerateFiles(modInfo.DirectoryPath, "*.*", SearchOption.AllDirectories))
                    {
                        if (file.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
                        {
                            try { File.Move(file, file.Replace(".disabled", "")); } catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"Error enabling files for '{modInfo.Name}': {ex.Message}");
                }
                return;
            }

            // Iterate through all possible files defined in the [Files] section.
            foreach (var fileMapping in modInfo.FileGroupMappings)
            {
                try
                {
                var fileBase = fileMapping.Key;
                // Fix for Linux: Normalize path separators to match the current OS
                if (Path.DirectorySeparatorChar == '/')
                {
                    fileBase = fileBase.Replace('\\', '/');
                }

                if (!modInfo.FileGroupMappings.TryGetValue(fileMapping.Key, out var group) || group == null) continue;

                // Correctly separate the directory and the base filename from the mod.ini entry.
                string combinedPath = Path.Combine(modInfo.DirectoryPath, fileBase);
                string? directory = Path.GetDirectoryName(combinedPath);
                string baseName = Path.GetFileName(combinedPath);

                if (string.IsNullOrEmpty(directory)) continue;
                if (!Directory.Exists(directory)) continue;

                // Find all related files (.pak, .utoc, .ucas, etc.)
                var filesToProcess = Directory.GetFiles(Path.GetFullPath(directory), baseName + ".*");

                foreach (var filePath in filesToProcess)
                {
                    // Ignore text-based config files
                    string ext = Path.GetExtension(filePath).ToLowerInvariant();
                    if (ext == ".ini" || ext == ".txt" || ext == ".md") continue;

                    bool shouldBeEnabled = selectedOptionIdentifiers.Contains(group);
                    string enabledPath = filePath.Replace(".disabled", "");
                    string disabledPath = enabledPath + ".disabled";

                    try
                    {
                        if (shouldBeEnabled && File.Exists(disabledPath))
                        {
                            File.Move(disabledPath, enabledPath);
                        }
                        else if (!shouldBeEnabled && File.Exists(enabledPath))
                        {
                            File.Move(enabledPath, disabledPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Failed to apply option for '{baseName}': {ex.Message}");
                    }
                }
                }
                catch (Exception ex)
                {
                    AppendLog($"Error processing file mapping '{fileMapping.Key}': {ex.Message}");
                }
            }
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            await SaveAndInstallModsAsync();
        }

        private async Task SaveAndInstallModsAsync()
        {
            // Disable the Play button during save and show status
            btnPlay.Enabled = false;
            btnPlay.ForeColor = Color.Gray;
            btnPlay.Text = "Saving...";

            // Before applying configurations, check for newly enabled mods that need a default config.
            bool defaultsSet = false;
            // Use a copy of the items to iterate over, as the collection can be modified.
            foreach (ListViewItem item in _allModItems)
            {
                if (item.Checked && item.Tag is ModInfo modInfo && modInfo.ConfigurationGroups.Any())
                {   
                    var activeProfile = GetActiveProfile();
                    if (activeProfile == null) continue;

                    // If a configurable mod is enabled but has no saved configuration, set the default.
                    foreach (var group in modInfo.ConfigurationGroups)
                    {
                        var configKey = $"{modInfo.Name}:{group.GroupName}";
                        if (!activeProfile.ModConfigurations.ContainsKey(configKey) && group.Options.Any())
                        {
                            // Set the default option for this group
                            activeProfile.ModConfigurations[configKey] = group.Options.First();
                            defaultsSet = true;
                        }
                    }
                }
            }

            if (defaultsSet) SettingsManager.Save(); // Save the new default settings.

            // Now that defaults are set, save the final state of enabled mods and their order.
            SaveModListState();

            // Show progress in the status bar
            progressBar.Visible = true;
            progressBar.Value = 0;

            IProgress<string> progress = new Progress<string>(s =>
            {
                UpdateStatus(s);
                AppendLog(s);
            });
            IProgress<int> progressBarProgress = new Progress<int>(p => progressBar.Value = p);

            try
            {
                // Then, apply the current configurations for all mods.
                foreach (ListViewItem item in _allModItems)
                {
                    try
                    {
                        if (item.Tag is ModInfo modInfo && modInfo.ConfigurationGroups.Any())
                        {
                            // Get the saved selection for this mod.
                            var activeProfile = GetActiveProfile();
                            if (activeProfile == null) continue;

                            var selectedOptionIdentifiers = new List<string>();
                            foreach (var group in modInfo.ConfigurationGroups)
                            {
                                var configKey = $"{modInfo.Name}:{group.GroupName}";
                                if (activeProfile.ModConfigurations.TryGetValue(configKey, out var selectedValue))
                                {
                                    // For SelectMultiple, the value is comma-separated. Split it.
                                    var options = selectedValue.Split(',').Select(s => s.Trim());
                                    foreach (var option in options)
                                    {
                                        selectedOptionIdentifiers.Add($"{group.GroupName}.{option}");
                                    }
                                }
                            }
                            ApplyModConfiguration(modInfo, selectedOptionIdentifiers);
                        }
                        else if (item.Tag is ModInfo modInfoWithoutConfig) // Handle non-configurable mods
                        {
                            ApplyModConfiguration(modInfoWithoutConfig, item.Checked ? new List<string> { "enable" } : new List<string>());
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Error processing mod '{item.Text}': {ex.Message}");
                    }
                }

                progressBarProgress.Report(10);
                // Check if any enabled mods have JSON files.
                var enabledModsWithJson = _allModItems
                    .Where(i => i.Checked && i.Tag is ModInfo modInfo && // Only look at checked mods
                               // Enumerate all .json files, but crucially, ignore any that are currently disabled.
                               // This ensures we only merge JSON files that correspond to the user's selected configuration.
                               Directory.EnumerateFiles(modInfo.DirectoryPath, "*.json", SearchOption.AllDirectories)
                                   .Any(jsonPath => !jsonPath.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
/*
                                Directory.EnumerateFiles(modInfo.DirectoryPath, "*.json", SearchOption.AllDirectories)
                                    .Any(jsonPath => {
                                        if (Path.GetFileName(jsonPath).Equals("info.json", StringComparison.OrdinalIgnoreCase))
                                        {
                                            // Check if it's a metadata file to be ignored.
                                            var content = File.ReadAllText(jsonPath);
                                            return !(content.Contains("\"name\"") && content.Contains("\"version\"") &&
                                                     content.Contains("\"author\"") && content.Contains("\"mod_type\""));
                                        }
                                        return true; // It's not info.json, so include it.
                                    })
*/
                    )
                    .ToList();

                if (enabledModsWithJson.Count == 0)
                {
                    progressBarProgress.Report(20);
                    progress.Report("No enabled mods with .json files found. Cleaning up old merged pak...");
                    if (!string.IsNullOrEmpty(_selectedPlatform) && _gameInstallations.TryGetValue(_selectedPlatform, out var gameInfo))
                    {
                        var targetModsDir = Path.Combine(gameInfo.Path, "UNION", "Content", "Paks", "~mods");
                        var oldLocresPakPath = Path.Combine(targetModsDir, "LocresMod.pak");
                        var zzzLocresPakPath = Path.Combine(targetModsDir, "ZZZ_LocresMod.pak");
                        var bangLocresPakPath = Path.Combine(targetModsDir, "!LocresMod.pak");

                        if (File.Exists(oldLocresPakPath))
                        {
                            try
                            {
                                File.Delete(oldLocresPakPath);
                                progress.Report($"Deleted old merged pak: {oldLocresPakPath}");
                            }
                            catch (Exception ex)
                            {
                                progress.Report($"Failed to delete old merged pak: {ex.Message}");
                            }
                        }

                        if (File.Exists(zzzLocresPakPath))
                        {
                            try
                            {
                                File.Delete(zzzLocresPakPath);
                                progress.Report($"Deleted old merged pak: {zzzLocresPakPath}");
                            }
                            catch (Exception ex)
                            {
                                progress.Report($"Failed to delete old merged pak: {ex.Message}");
                            }
                        }

                        if (File.Exists(bangLocresPakPath))
                        {
                            try
                            {
                                File.Delete(bangLocresPakPath);
                                progress.Report($"Deleted merged pak: {bangLocresPakPath}");
                            }
                            catch (Exception ex)
                            {
                                progress.Report($"Failed to delete merged pak: {ex.Message}");
                            }
                        }
                        else if (!File.Exists(oldLocresPakPath) && !File.Exists(zzzLocresPakPath) && !File.Exists(bangLocresPakPath))
                        {
                            progress.Report("No old merged pak found to delete.");
                        }
                    }
                    progressBarProgress.Report(50);
                    await InstallModsAsync(); // Still install other mods
                    progressBarProgress.Report(100);
                    progress.Report("\n✓ Save and install complete!");
                }
                else
                {
                    // Run the full merge/pack process since there are JSON mods.
                    progressBarProgress.Report(20);

                    // Extract base localization files from game archives
                    var extractGamePath = !string.IsNullOrEmpty(_selectedPlatform) && _gameInstallations.TryGetValue(_selectedPlatform, out var extractGameInfo)
                        ? extractGameInfo.Path
                        : SettingsManager.Settings.GameDirectory;
                    if (!string.IsNullOrEmpty(extractGamePath))
                    {
                        var locresOutput = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "Locres");
                        await Cue4ParseProvider.ExtractLocalizationAsync(extractGamePath, locresOutput, progress);
                    }

                    var installTask = InstallModsAsync();
                    // Pass the list of mods that we've already determined have active JSON files.
                    // This prevents the converter from having to re-scan all mods.
                    var modsToProcessForJson = enabledModsWithJson.Select(i => i.Tag as ModInfo).Where(m => m != null);

                    var jsonTask = LocresConverter.ProcessModJsonFilesAsync(modsToProcessForJson!, progress);

                    await Task.WhenAll(installTask, jsonTask);
                    progressBarProgress.Report(50);

                    // After merging JSON, automatically pack the results back to .locres
                    progress.Report("\nStarting to pack merged .locres files...");
                    var gamePathForPack = !string.IsNullOrEmpty(_selectedPlatform) && _gameInstallations.TryGetValue(_selectedPlatform, out var _gameInfoForPack)
                        ? _gameInfoForPack.Path
                        : string.Empty;
                    await LocresConverter.PackMergedLocresAsync(gamePathForPack, progress);
                    progressBarProgress.Report(80);

                    // After the pack operation completes, install the final produced pak into the game's ~mods folder.
                    try
                    {
                        var sourcePak = PlatformUtils.GetLocresModPakPath();
                        if (!File.Exists(sourcePak))
                        {
                            var found = Directory.GetFiles(PlatformUtils.GetWorkDir(), "LocresMod.pak", SearchOption.AllDirectories).FirstOrDefault();
                            if (!string.IsNullOrEmpty(found)) sourcePak = found;
                        }

                        if (File.Exists(sourcePak))
                        {
                            if (!string.IsNullOrEmpty(_selectedPlatform) && _gameInstallations.TryGetValue(_selectedPlatform, out var gameInfo))
                            {
                                var targetModsDir = Path.Combine(gameInfo.Path, "UNION", "Content", "Paks", "~mods");
                                Directory.CreateDirectory(targetModsDir);

                                var oldPak = Path.Combine(targetModsDir, "LocresMod.pak");
                                if (File.Exists(oldPak)) File.Delete(oldPak);

                                var zzzPak = Path.Combine(targetModsDir, "ZZZ_LocresMod.pak");
                                if (File.Exists(zzzPak)) File.Delete(zzzPak);

                                var bangPak = Path.Combine(targetModsDir, "!LocresMod.pak");
                                if (File.Exists(bangPak)) File.Delete(bangPak);

                                var bangLink = Path.Combine(targetModsDir, "!LocresMod");
                                if (Directory.Exists(bangLink)) Directory.Delete(bangLink);
                                if (File.Exists(bangLink)) File.Delete(bangLink);

                                // Create a container folder in Tools to hold the pak, so we can link the folder
                                var packedDir = Path.Combine(PlatformUtils.GetWorkDir(), "LocresMod_Packed");
                                if (!Directory.Exists(packedDir)) Directory.CreateDirectory(packedDir);

                                var oldPackedPak = Path.Combine(packedDir, "LocresMod.pak");
                                if (File.Exists(oldPackedPak)) File.Delete(oldPackedPak);

                                var packedPakPath = Path.Combine(packedDir, "!LocresMod_P.pak");
                                if (File.Exists(packedPakPath)) File.Delete(packedPakPath);

                                File.Move(sourcePak, packedPakPath);

                                var destLink = Path.Combine(targetModsDir, "zzzLocresMod");
                                try
                                {
                                    if (Directory.Exists(destLink)) Directory.Delete(destLink);
                                    if (File.Exists(destLink)) File.Delete(destLink);

                                    // Use CreateSymbolicLinkAsync which creates directory links (Junctions on Windows)
                                    bool linked = await CreateSymbolicLinkAsync(destLink, packedDir);

                                    if (linked)
                                    {
                                        progress.Report($"Linked merged pak folder to ~mods: {Path.GetFileName(destLink)}");
                                    }
                                    else
                                    {
                                        progress.Report("Failed to link merged pak folder to ~mods.");
                                    }
                                }
                                catch (Exception moveEx)
                                {
                                    progress.Report($"Failed to link merged pak folder: {moveEx.Message}");
                                }
                            }
                            else
                            {
                                progress.Report("Merged pak exists, but game installation not found to install it.");
                            }
                        }
                        else
                        {
                            progress.Report("No merged pak found in Tools to install.");
                        }
                    }
                    catch (Exception ex)
                    {
                        progress.Report($"Failed to install merged pak: {ex.Message}");
                    }

                    progressBarProgress.Report(100);
                    progress.Report("\n✓ All tasks completed successfully!");
                }
            }
            catch (Exception ex)
            {
                progress.Report($"\nAn error occurred during the save process: {ex.Message}");
                CustomMessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Visible = false;

                if (SettingsManager.Settings.AutoCleanTemporaryFiles)
                {
                    // Cleanup the LocresMod folder from the Tools directory as it's no longer needed.
                    var locresModTempPath = Path.Combine(PlatformUtils.GetToolsDir(), "LocresMod");
                    if (Directory.Exists(locresModTempPath))
                    {
                        try { Directory.Delete(locresModTempPath, true); progress.Report($"Cleaned up temporary folder: {locresModTempPath}"); }
                        catch (Exception ex) { progress.Report($"Could not clean up temporary folder: {ex.Message}"); }
                    }

                    // Cleanup the merged Game_*.json files from the Tools directory.
                    var mergedJsonDir = PlatformUtils.GetMergedJsonDir();
                    if (Directory.Exists(mergedJsonDir))
                    {
                        try
                        {
                            var mergedJsonFiles = Directory.GetFiles(mergedJsonDir, "Game_*.json");
                            foreach (var file in mergedJsonFiles)
                            {
                                File.Delete(file);
                                progress.Report($"Cleaned up merged JSON: {Path.GetFileName(file)}");
                            }
                        }
                        catch (Exception ex) { progress.Report($"Could not clean up merged JSON files: {ex.Message}"); }
                    }
                }

                if (SettingsManager.Settings.AutoCloseLogOnSuccess && !rtbLog.Text.Contains("Failed") && !rtbLog.Text.Contains("Error"))
                {
                    splitContainerRoot.Panel2Collapsed = true;
                    btnToggleDebugLog.Text = "Log";
                }

                progress.Report("Saving Complete");

                // Re-enable the Play button
                btnPlay.Enabled = true;
                btnPlay.ForeColor = Color.White;
                btnPlay.Text = "▶ Play";
            }
        }

        private async void btnPlay_Click(object sender, EventArgs e)
        {
            if (!SettingsManager.Settings.DoNotWarnUnsavedChanges && HasUnsavedChanges())
            {
                using (var form = new UnsavedChangesForm())
                {
                    var result = form.ShowDialog(this);

                    if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                    else if (result == DialogResult.Yes) // Save and Play
                    {
                        await SaveAndInstallModsAsync();
                    }
                }
            }

            if (string.IsNullOrEmpty(_selectedPlatform) || (_selectedPlatform != "Executable" && !_gameInstallations.ContainsKey(_selectedPlatform)))
            {
                CustomMessageBox.Show("Could not find game installation to launch.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Check if the game process is already running.
            if (Process.GetProcessesByName(GameProcessName).Any())
            {
                CustomMessageBox.Show("The game is already running.", "Game Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateStatus("Launch aborted: Game is already running.");
                return;
            }

            try
            {
                UpdateStatus($"Launching {_selectedPlatform} version...");
                string launchUrl = "";
                if (_selectedPlatform == "Steam")
                {
                    launchUrl = $"steam://run/{GameRegistry.SteamAppId}";
                }
                else if (_selectedPlatform == "Executable")
                {
                    // For custom paths, we find and launch the executable directly.
                    var exeName = SettingsManager.Settings.GameExecutableName ?? "SonicRacingCrossWorlds.exe";
                    var exePath = Path.Combine(SettingsManager.Settings.GameDirectory ?? "", exeName);
                    
                    if (File.Exists(exePath))
                    {
                        Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
                    }
                    else
                    {
                        CustomMessageBox.Show($"Executable not found at:\n{exePath}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return; // Return early as we don't use launchUrl for this case
                }
                else if (_selectedPlatform == "Custom")
                {
                    string customUnionDir = SettingsManager.Settings.CustomUnionDirectory ?? "";
                    var win64Dir = Path.Combine(customUnionDir, "Binaries", "Win64");
                    if (!Directory.Exists(win64Dir))
                    {
                        CustomMessageBox.Show($"Binaries directory not found at:\n{win64Dir}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    var exeFiles = Directory.GetFiles(win64Dir, "*SonicRacing*.exe", SearchOption.TopDirectoryOnly);
                    if (exeFiles.Length == 0)
                    {
                        CustomMessageBox.Show($"No SonicRacing executable found in:\n{win64Dir}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    Process.Start(new ProcessStartInfo(exeFiles[0]) { UseShellExecute = true });
                    return;
                }

                Process.Start(new ProcessStartInfo(launchUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to launch the game: {ex.Message}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Launch failed.");
            }
        }

        private void RefreshModList()
        {
            _allModItems.Clear();
            modListView.Items.Clear();

            if (string.IsNullOrWhiteSpace(SettingsManager.Settings.ModsDirectory) || !Directory.Exists(SettingsManager.Settings.ModsDirectory))
            {
                UpdateStatus("Mods directory not set or found. Please configure it in Settings.");
                return;
            }
            
            var modsDir = SettingsManager.Settings.ModsDirectory;
            var modDirectories = Directory.GetDirectories(modsDir);

            // Removed automatic mod.ini generation. Mod authors should provide mod.ini files manually.

            var activeProfile = GetActiveProfile();
            if (activeProfile == null) return;

            // Use saved settings
            var enabledMods = new HashSet<string>(activeProfile.EnabledMods, StringComparer.OrdinalIgnoreCase);
            var modLoadOrder = activeProfile.ModLoadOrder;
            var foundMods = new Dictionary<string, ModInfo>();
            
            foreach (var dir in modDirectories) // First, find all available mods
            {
                // Skip temp/trash folders or hidden folders to prevent processing backups/failed moves
                if (Path.GetFileName(dir).StartsWith("_") || (new DirectoryInfo(dir).Attributes & FileAttributes.Hidden) != 0) continue;

                // Non-destructive nested mod.ini detection:
                // If a mod.ini exists in a subdirectory, treat that subdirectory as the mod root
                // for purposes of reading mod metadata, without moving or deleting any files.
                string modRoot = dir;
                try
                {
                    var iniFiles = Directory.GetFiles(dir, "mod.ini", SearchOption.AllDirectories);
                    if (iniFiles.Length > 0)
                    {
                        string fullModPath = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string? foundIni = null;
                        foreach (var f in iniFiles)
                        {
                            var fDir = Path.GetDirectoryName(f);
                            if (string.IsNullOrEmpty(fDir)) continue;
                            var fullDir = Path.GetFullPath(fDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            // prefer an ini that is not the original root
                            if (!fullDir.Equals(fullModPath, StringComparison.OrdinalIgnoreCase))
                            {
                                foundIni = f;
                                break;
                            }
                        }

                        if (foundIni != null)
                        {
                            var candidate = Path.GetDirectoryName(foundIni)!;
                            // Safety: ensure candidate is inside the mod folder
                            if (Path.GetFullPath(candidate).StartsWith(fullModPath, StringComparison.OrdinalIgnoreCase))
                            {
                                modRoot = candidate;
                                Debug.WriteLine($"Detected nested mod.ini for '{dir}', using '{modRoot}' as mod root (non-destructive).");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error searching for nested mod.ini under '{dir}': {ex.Message}");
                }

                var modFolderName = Path.GetFileName(modRoot);
                if (string.IsNullOrEmpty(modFolderName)) continue;

                var iniPath = Path.Combine(modRoot, "mod.ini");

                // Check for legacy config (desc.ini) if mod.ini doesn't exist
                if (!File.Exists(iniPath))
                {
                    ConvertLegacyConfig(dir);
                }

                ModInfo modInfo;

                if (File.Exists(iniPath))
                {
                    var iniSections = IniParser.Parse(iniPath);
                    if (!iniSections.TryGetValue("Main", out var mainSection))
                    {
                        continue;
                    }

                    modInfo = new ModInfo
                    {
                        Name = mainSection.GetValueOrDefault("Name", modFolderName),
                        Author = mainSection.GetValueOrDefault("Author", "Unknown"),
                        Version = mainSection.ContainsKey("Version") ? mainSection["Version"] : "-1",
                        Description = mainSection.GetValueOrDefault("Description", "No description provided."),
                        DirectoryPath = modRoot,
                        IsLogicMod = mainSection.GetValueOrDefault("Type", "").Equals("LogicMod", StringComparison.OrdinalIgnoreCase)
                    };

                    // Populate GBVersion from [GameBanana] section if available, otherwise fallback to main Version
                    if (iniSections.TryGetValue("GameBanana", out var gbSec))
                    {
                        if (gbSec.TryGetValue("ItemId", out var itemIdValue))
                        {
                            modInfo.GBItemId = itemIdValue;
                        }
                        if (gbSec.TryGetValue("ItemType", out var itemTypeValue))
                        {
                            modInfo.GBItemType = itemTypeValue;
                        }

                        if (gbSec.TryGetValue("GBVersion", out var gbVersionValue))
                        {
                            modInfo.GBVersion = gbVersionValue;
                        }
                    }

                    // New logic for multiple named [Config:GroupName] sections
                    var configSections = iniSections.Where(s => s.Key.StartsWith("Config:", StringComparison.OrdinalIgnoreCase)).ToList();
                    foreach (var configKvp in configSections)
                    {
                        modInfo.ConfigurationGroups.Add(new ModConfigurationGroup(configKvp.Key, configKvp.Value));
                    }

                    var filesKvp = iniSections.FirstOrDefault(s => s.Key.Equals("Files", StringComparison.OrdinalIgnoreCase));
                    if (filesKvp.Value != null)
                    {
                        modInfo.FileGroupMappings = filesKvp.Value;
                    }
                }
                else
                {
                    // This is an "ini-less" mod. Treat the folder as a basic mod.
                    modInfo = new ModInfo
                    {
                        Name = modFolderName,
                        DirectoryPath = modRoot
                        // Author, Version, etc., will use default values.
                    };
                }

                foundMods[modFolderName] = modInfo;
            }

            // Now, add mods to the list view in the correct, saved order.
            foreach (var modName in modLoadOrder)
            {
                if (modName.StartsWith("SECTION:"))
                {
                    var sectionName = modName.Substring(8);
                    Color? color = null;
                    if (sectionName.Contains('|'))
                    {
                        var parts = sectionName.Split('|');
                        sectionName = parts[0];
                        if (parts.Length > 1 && int.TryParse(parts[1], out int argb))
                        {
                            color = Color.FromArgb(argb);
                        }
                    }
                    _allModItems.Add(CreateSectionListViewItem(sectionName, color));
                }
                else if (foundMods.TryGetValue(modName, out var modInfo)) // This should be case-insensitive in the future if needed
                {
                    _allModItems.Add(CreateModListViewItem(modInfo, enabledMods));
                    foundMods.Remove(modName); // Remove from dictionary so we don't add it again.
                }
            }

            // Finally, add any new, unsorted mods to the end of the list.
            foreach (var modInfo in foundMods.Values)
            {
                _allModItems.Add(CreateModListViewItem(modInfo, enabledMods));
            }

            ApplyFilter();
            UpdateStatus($"{_allModItems.Count} mod(s) found.");
        }

        /// <summary>
        /// Scans for legacy 'desc.ini' files and generates a mod.ini if found.
        /// </summary>
        private bool ConvertLegacyConfig(string modDir)
        {
            var descFiles = Directory.GetFiles(modDir, "desc.ini", SearchOption.AllDirectories);
            if (descFiles.Length == 0) return false;

            var iniPath = Path.Combine(modDir, "mod.ini");
            var iniData = File.Exists(iniPath)
                ? IniParser.Parse(iniPath)
                : new Dictionary<string, Dictionary<string, string>>();

            // [Main]
            if (!iniData.ContainsKey("Main"))
            {
                iniData["Main"] = new Dictionary<string, string>();
            }
            var mainSection = iniData["Main"];
            if (!mainSection.ContainsKey("Name")) mainSection["Name"] = Path.GetFileName(modDir);
            if (!mainSection.ContainsKey("Author")) mainSection["Author"] = "Unknown";
            if (!mainSection.ContainsKey("Version")) mainSection["Version"] = "1.0";
            if (!mainSection.ContainsKey("Description")) mainSection["Description"] = "Auto-generated from legacy configuration.";

            // GroupName -> List of Options
            var groups = new Dictionary<string, List<string>>();
            // FilePath -> GroupName.OptionName
            var fileMappings = new Dictionary<string, string>();

            foreach (var descFile in descFiles)
            {
                var optionDir = Path.GetDirectoryName(descFile);
                if (optionDir == null) continue;

                // Determine Group Name based on folder structure
                string relativePath = Path.GetRelativePath(modDir, optionDir);
                if (relativePath == "." || string.IsNullOrEmpty(relativePath)) continue;

                // Default group name
                string groupName = "Configuration";

                // Check if the option is nested (Group/Option)
                // If relativePath has directory separators, the parent folder is the group.
                // Example: "Course 01 - Ocean View\0 Ocean View TSR version" -> Group: "Course 01 - Ocean View"
                string groupRelPath = Path.GetDirectoryName(relativePath) ?? "";
                if (!string.IsNullOrEmpty(groupRelPath) && groupRelPath != ".")
                {
                    // Use the parent folder path as the group name, replacing separators with spaces or keeping them safe
                    groupName = groupRelPath.Replace(Path.DirectorySeparatorChar, ' ').Replace(Path.AltDirectorySeparatorChar, ' ');
                }

                // Sanitize group name for INI section safety
                groupName = groupName.Replace("[", "(").Replace("]", ")");

                var descData = IniParser.Parse(descFile);
                string optionName = "";
                if (descData.TryGetValue("Description", out var descSection))
                {
                    if (descSection.TryGetValue("Name", out var nameVal)) optionName = nameVal;
                }

                if (string.IsNullOrWhiteSpace(optionName))
                {
                    optionName = Path.GetFileName(optionDir) ?? "Unknown";
                }

                // Sanitize option name (remove commas as they break the CSV list)
                optionName = optionName.Replace(",", "");

                if (!groups.ContainsKey(groupName))
                {
                    groups[groupName] = new List<string>();
                }

                // Ensure unique option names
                string uniqueOptionName = optionName;
                if (groups[groupName].Contains(uniqueOptionName))
                {
                    int i = 2;
                    while (groups[groupName].Contains($"{uniqueOptionName} {i}")) i++;
                    uniqueOptionName = $"{uniqueOptionName} {i}";
                }
                groups[groupName].Add(uniqueOptionName);

                // Map files
                var files = Directory.GetFiles(optionDir);
                foreach (var file in files)
                {
                    if (Path.GetFileName(file).Equals("desc.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    
                    string fileRelPath = Path.GetRelativePath(modDir, file);
                    fileMappings[fileRelPath] = $"{groupName}.{uniqueOptionName}";
                }
            }

            // Add new Config sections if they don't exist
            foreach (var group in groups)
            {
                var configSectionName = $"Config:{group.Key}";
                if (!iniData.ContainsKey(configSectionName))
                {
                    iniData[configSectionName] = new Dictionary<string, string>
                    {
                        ["Type"] = "SelectOne",
                        ["Description"] = $"Select {group.Key}:",
                        ["Options"] = string.Join(", ", group.Value)
                    };
                }
            }

            // Add new File mappings if they don't exist
            if (fileMappings.Any())
            {
                if (!iniData.ContainsKey("Files"))
                {
                    iniData["Files"] = new Dictionary<string, string>();
                }

                foreach (var mapping in fileMappings)
                {
                    // Don't overwrite existing file mappings
                    if (!iniData["Files"].ContainsKey(mapping.Key))
                    {
                        iniData["Files"][mapping.Key] = mapping.Value;
                    }
                }
            }

            // Write mod.ini
            IniParser.Write(iniPath, iniData);
            return true;
        }

        private async Task CheckForModUpdatesAsync()
        {
            UpdateStatus("Checking for mod updates...");
            AppendLog("--- Starting GameBanana Mod Update Check ---");
            IProgress<string> logger = new Progress<string>(s =>
            {
                AppendLog(s);
            });

            var updateTasks = new List<Task>();

            foreach (ListViewItem item in _allModItems)
            {
                if (item.Tag is not ModInfo modInfo) continue;

                var iniPath = Path.Combine(modInfo.DirectoryPath, "mod.ini");
                if (!File.Exists(iniPath))
                {
                    logger.Report($"[{modInfo.Name}] Skipping: mod.ini not found.");
                    continue;
                }

                var iniData = IniParser.Parse(iniPath);
                if (!iniData.TryGetValue("GameBanana", out var gbSection) ||
                    !gbSection.TryGetValue("ItemId", out var itemIdStr) ||
                    !gbSection.TryGetValue("ItemType", out var itemType))
                {
                    logger.Report($"[{modInfo.Name}] Skipping: [GameBanana] section with ItemId/ItemType not found in mod.ini.");
                    continue;
                }

                if (!int.TryParse(itemIdStr, out var itemId)) continue;

                // Add a task to fetch and compare the version for this mod
                updateTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var localGbVersion = modInfo.GBVersion ?? "0";
                        var iniSections = IniParser.Parse(iniPath);

                        var localRawVersionStr = modInfo.Version == "-1" ? "0" : modInfo.Version;
                        var localRawVersion = localRawVersionStr.Split('.').Select(part => int.TryParse(part, out var num) ? num : 0).ToArray() ?? [0];

                        logger.Report($"[{modInfo.Name}] Found GameBanana info. Item: {itemType}/{itemId}. Local GBVersion: '{localGbVersion}'; Local Raw Version: '{string.Join(".", localRawVersion)}'.");
                        string? latestVersionStr = await GameBananaApiService.GetLatestModUpdateCountAsync(itemType, itemId) ?? "0";
                        string? latestRawVersionStr = await GameBananaApiService.GetLatestModVersionAsync(itemType, itemId) ?? "0";

                        int[]? rawVersionParts = latestRawVersionStr?.Split('.').Select(part => int.TryParse(part, out var num) ? num : 0).ToArray();

                        if (string.IsNullOrEmpty(latestVersionStr) && string.IsNullOrEmpty(latestRawVersionStr))
                        {
                            logger.Report($"[{modInfo.Name}] -> Could not fetch remote version from API.");
                            return;
                        }
                        else if (!string.IsNullOrEmpty(latestVersionStr) && !string.IsNullOrEmpty(latestRawVersionStr))
                            logger.Report($"[{modInfo.Name}] -> Fetched remote version: '{latestVersionStr}' and raw version: '{latestRawVersionStr}'.");
                        else if (!string.IsNullOrEmpty(latestVersionStr))
                            logger.Report($"[{modInfo.Name}] -> Fetched remote version: '{latestVersionStr}'.");
                        else if (!string.IsNullOrEmpty(latestRawVersionStr))
                            logger.Report($"[{modInfo.Name}] -> Fetched remote raw version: '{latestRawVersionStr}'.");

                        // Compare integers: prefer GBVersion for local value, fallback to main Version if GBVersion isn't numeric

                        if (int.TryParse(latestVersionStr, out int latestVersion) || rawVersionParts?.Length > 0)
                        {
                            if (!int.TryParse(localGbVersion, out int localGbInt) && !(localGbVersion == "0" && rawVersionParts != null && rawVersionParts.Length > 0 && rawVersionParts.Any(part => part > 0))){
                                if (latestVersion > localGbInt)
                                {
                                    logger.Report($"[{modInfo.Name}] -> UPDATE AVAILABLE! (Remote: {latestVersion} > Local(GB): {localGbInt})");
                                    // We need to update the UI on the UI thread.
                                    this.Invoke((Action)(() => {
                                        item.ForeColor = Color.LimeGreen;
                                        var updateSubItem = item.SubItems[3];
                                        if (!updateSubItem.Text.Contains("Update"))
                                        {
                                            updateSubItem.Text = "🔄 Update";
                                        }
                                    }));
                                }
                                else
                                {
                                    logger.Report($"[{modInfo.Name}] -> Mod is up to date. (Remote: {latestVersion} <= Local(GB): {localGbInt})");
                                }
                            } else if (rawVersionParts != null && rawVersionParts.Length > 0) // try to instead use rawVersion if GBVersion isn't a valid integer or is 0
                            {
                                bool IsRemoteVersionNewer = false;
                                for (int i = 0; i < Math.Max(rawVersionParts.Length, localRawVersion.Length); i++)
                                {
                                    if (i >= rawVersionParts.Length) // Remote version has fewer parts, treat missing parts as 0
                                        break;
                                    if (i >= localRawVersion.Length) // Local version has fewer parts, treat missing parts as 0
                                    {
                                        IsRemoteVersionNewer = true; // Remote has more parts, consider it newer if all previous parts are equal
                                        break;
                                    }
                                    if (rawVersionParts[i] > localRawVersion[i])
                                    {
                                        IsRemoteVersionNewer = true;
                                        break;
                                    }
                                }

                                if (IsRemoteVersionNewer)
                                {
                                    logger.Report($"[{modInfo.Name}] -> UPDATE AVAILABLE! (Remote: {latestVersionStr} > Local(Raw): {string.Join(".", [.. localRawVersion.Select(x => x.ToString())])})");
                                    // We need to update the UI on the UI thread.
                                    this.Invoke((Action)(() => {
                                        item.ForeColor = Color.LimeGreen;
                                        var updateSubItem = item.SubItems[3];
                                        if (!updateSubItem.Text.Contains("Update"))
                                        {
                                            updateSubItem.Text = "🔄 Update";
                                        }
                                    }));
                                }
                                else
                                {
                                    logger.Report($"[{modInfo.Name}] -> Mod is up to date. (Remote: {string.Join(".", [.. rawVersionParts.Select(x => x.ToString())])} <= Local(GB): {string.Join(".", [.. localRawVersion.Select(x => x.ToString())])})");
                                }
                            }
                        }
                    }
                    catch (Exception ex) { logger.Report($"[{modInfo.Name}] ERROR checking update: {ex.Message}"); }
                }));
            }

            await Task.WhenAll(updateTasks);
            UpdateStatus("Mod update check complete.");
            AppendLog("--- Mod Update Check Complete ---");
        }

        private ListViewItem CreateModListViewItem(ModInfo modInfo, HashSet<string> enabledMods)
        {
            var item = new ListViewItem(new[] 
                    {
                        modInfo.Name,
                        modInfo.Author,
                        // Add text to the "Actions" column only if the mod is configurable.
                        modInfo.ConfigurationGroups.Any() ? "⚙️ Configure" : "",
                        "" // Placeholder for the new Update column
            }) 
            {
                Tag = modInfo
            };

            string modFolderName = Path.GetFileName(modInfo.DirectoryPath) ?? "";
            if (!string.IsNullOrEmpty(modFolderName))
            {
                item.Checked = enabledMods.Contains(modFolderName);
            }

            // Set the initial color based on the checked state
            item.ForeColor = item.Checked ? ThemeManager.CurrentTheme.ForeColor : Color.Gray;

            return item;
        }

        private ListViewItem CreateSectionListViewItem(string name, Color? color = null)
        {
            var item = new ListViewItem(name);
            item.Tag = new ModSection { Name = name, TextColor = color };
            item.ForeColor = color ?? ThemeManager.CurrentTheme.AccentColor;
            // Add empty subitems to align with columns
            item.SubItems.Add(""); // Author
            item.SubItems.Add(""); // Actions
            item.SubItems.Add(""); // Update
            return item;
        }

        private void ApplyFilter()
        {
            modListView.BeginUpdate();
            modListView.Items.Clear();

            var searchText = txtSearch.Text;
            var filteredItems = string.IsNullOrWhiteSpace(searchText)
                ? _allModItems // If search is empty, show all mods
                : _allModItems.Where(item => item.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);

            modListView.Items.AddRange(filteredItems.ToArray());
            modListView.EndUpdate();
        }
        
        private void SaveModListState()
        {
            var activeProfile = GetActiveProfile();
            if (activeProfile == null) return;

            var enabledMods = new List<string>();
            var modLoadOrder = new List<string>();
            foreach (ListViewItem item in _allModItems)
            {
                if (item.Tag is ModInfo modInfo)
                {
                    var modFolderName = Path.GetFileName(modInfo.DirectoryPath);
                    if (!string.IsNullOrEmpty(modFolderName))
                    {
                        modLoadOrder.Add(modFolderName);
                        if (item.Checked)
                        {
                            enabledMods.Add(modFolderName);
                        }
                    }
                }
                else if (item.Tag is ModSection section)
                {
                    if (section.TextColor.HasValue)
                        modLoadOrder.Add($"SECTION:{section.Name}|{section.TextColor.Value.ToArgb()}");
                    else
                        modLoadOrder.Add($"SECTION:{section.Name}");
                }
            }
            activeProfile.EnabledMods = enabledMods;
            activeProfile.ModLoadOrder = modLoadOrder;
            SettingsManager.Save();
        }

        private bool HasUnsavedChanges()
        {
            var activeProfile = GetActiveProfile();
            if (activeProfile == null) return false;

            var currentEnabledMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var currentLoadOrder = new List<string>();

            foreach (ListViewItem item in _allModItems)
            {
                if (item.Tag is ModInfo modInfo)
                {
                    var dirName = Path.GetFileName(modInfo.DirectoryPath);
                    currentLoadOrder.Add(dirName);
                    if (item.Checked)
                    {
                        currentEnabledMods.Add(dirName);
                    }
                }
            }

            var savedEnabledMods = new HashSet<string>(activeProfile.EnabledMods ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            
            if (!currentEnabledMods.SetEquals(savedEnabledMods)) return true;

            // Check load order
            var savedLoadOrder = activeProfile.ModLoadOrder ?? new List<string>();
            
            // Filter savedLoadOrder to only include mods currently present in the list (to handle external deletions)
            var savedLoadOrderFiltered = savedLoadOrder.Where(m => currentLoadOrder.Contains(m, StringComparer.OrdinalIgnoreCase)).ToList();
            
            // If counts differ (e.g. new mods added), it's a change
            if (currentLoadOrder.Count != savedLoadOrderFiltered.Count) return true;

            for (int i = 0; i < currentLoadOrder.Count; i++)
            {
                if (!string.Equals(currentLoadOrder[i], savedLoadOrderFiltered[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private async Task<bool> InstallModsAsync()
        {
            string? gamePath = null;
            if (!string.IsNullOrEmpty(_selectedPlatform) && _gameInstallations.TryGetValue(_selectedPlatform, out var detectedGameInfo))
            {
                gamePath = detectedGameInfo.Path;
            }
            else gamePath = SettingsManager.Settings.GameDirectory;

            if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
            {
                UpdateStatus("Cannot install mods: Game path not found.");
                return false;
            }
            var targetModsDir = Path.Combine(gamePath, "UNION", "Content", "Paks", "~mods");
            var targetLogicModsDir = Path.Combine(gamePath, "UNION", "Content", "Paks", "LogicMods");
            var targetUe4ssModsDir = Path.Combine(gamePath, "UNION", "Binaries", "Win64", "ue4ss", "Mods");

            // Check if any enabled mods require UE4SS
            bool requiresUe4ss = false;
            foreach (ListViewItem item in _allModItems)
            {
                if (item.Checked && item.Tag is ModInfo modInfo && (IsUe4ssScriptMod(modInfo.DirectoryPath) || modInfo.IsLogicMod))
                {
                    requiresUe4ss = true;
                    break;
                }
            }

            if (requiresUe4ss)
            {
                await EnsureUe4ssInstalledAsync(gamePath);
            }

            // Check for exFAT file system
            bool useCopy = false;
            try
            {
                if (!Directory.Exists(targetModsDir)) Directory.CreateDirectory(targetModsDir);
                var driveInfo = new DriveInfo(Path.GetPathRoot(targetModsDir) ?? targetModsDir);
                
                if (string.Equals(driveInfo.DriveFormat, "exFAT", StringComparison.OrdinalIgnoreCase))
                {
                    if (!SettingsManager.Settings.SuppressExFatWarning)
                    {
                        using (var warning = new ExFatWarningForm())
                        {
                            if (warning.ShowDialog(this) != DialogResult.OK)
                            {
                                UpdateStatus("Installation cancelled.");
                                return false;
                            }
                            if (warning.DoNotShowAgain)
                            {
                                SettingsManager.Settings.SuppressExFatWarning = true;
                                SettingsManager.Save();
                            }
                        }
                    }
                    useCopy = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking drive format: {ex.Message}");
            }

            try
            {
                // 1. Ensure the target directory exists.
                Directory.CreateDirectory(targetModsDir);

                // 1b. Clean out old LogicMods links/folders
                if (Directory.Exists(targetLogicModsDir))
                {
                    foreach (var dir in Directory.GetDirectories(targetLogicModsDir))
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        if ((dirInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                        {
                            Directory.Delete(dir);
                        }
                        else if (Regex.IsMatch(dirInfo.Name, @"^\d{3}-"))
                        {
                            if (File.Exists(Path.Combine(dir, ".bsm_managed")))
                            {
                                Directory.Delete(dir, true);
                            }
                        }
                    }
                }

                // 2. Clear out old symbolic links AND copied folders created by this manager.
                foreach (var dir in Directory.GetDirectories(targetModsDir))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    // Check for symlink
                    if ((dirInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    {
                        Directory.Delete(dir);
                    }
                    // Check for copied folder pattern (000-ModName)
                    else if (Regex.IsMatch(dirInfo.Name, @"^\d{3}-"))
                    {
                        if (File.Exists(Path.Combine(dir, ".bsm_managed")))
                        {
                            Directory.Delete(dir, true);
                        }
                    }
                }

                // 2b. Cleanup UE4SS mods (remove any mod managed by us from the target dir)
                if (Directory.Exists(targetUe4ssModsDir))
                {
                    foreach (ListViewItem item in _allModItems)
                    {
                        if (item.Tag is ModInfo modInfo && IsUe4ssScriptMod(modInfo.DirectoryPath))
                        {
                            var modFolderName = Path.GetFileName(modInfo.DirectoryPath);
                            var destPath = Path.Combine(targetUe4ssModsDir, modFolderName);
                            
                            // If the mod is disabled (unchecked), just remove enabled.txt to disable it.
                            // We leave the folder structure intact to avoid unnecessary IO.
                            if (!item.Checked && Directory.Exists(destPath))
                            {
                                var enabledTxtPath = Path.Combine(destPath, "enabled.txt");
                                if (File.Exists(enabledTxtPath)) File.Delete(enabledTxtPath);
                            }
                        }
                    }
                }

                // 3. Create new links for checked mods.
                var installTasks = new List<Task<bool>>();
                // Iterate through all items in the list view to preserve the visual load order.
                for (int i = 0; i < _allModItems.Count; i++)
                {
                    var item = _allModItems[i];
                    // Only create links for mods that are actually checked.
                    if (item.Checked && item.Tag is ModInfo modInfo)
                    {
                        var modFolderName = Path.GetFileName(modInfo.DirectoryPath);
                        if (!string.IsNullOrEmpty(modFolderName))
                        {
                            if (IsUe4ssScriptMod(modInfo.DirectoryPath))
                            {
                                // Install UE4SS Script Mod (Always Copy)
                                if (!Directory.Exists(targetUe4ssModsDir)) Directory.CreateDirectory(targetUe4ssModsDir);
                                var destPath = Path.Combine(targetUe4ssModsDir, modFolderName);
                                installTasks.Add(InstallUe4ssModAsync(destPath, modInfo.DirectoryPath));
                            }
                            else if (modInfo.IsLogicMod)
                            {
                                // Install Logic Mod
                                if (!Directory.Exists(targetLogicModsDir)) Directory.CreateDirectory(targetLogicModsDir);
                                var linkName = Path.Combine(targetLogicModsDir, $"{i:D3}-{modFolderName}");

                                if (useCopy)
                                {
                                    installTasks.Add(CopyDirectoryAsync(linkName, modInfo.DirectoryPath, true));
                                }
                                else
                                {
                                    installTasks.Add(CreateSymbolicLinkAsync(linkName, modInfo.DirectoryPath));
                                }
                            }
                            else
                            {
                                // Standard Mod Installation
                                // Assign a prefix to enforce load order.
                                var linkName = Path.Combine(targetModsDir, $"{i:D3}-{modFolderName}");
                                
                                if (useCopy)
                                {
                                    installTasks.Add(CopyDirectoryAsync(linkName, modInfo.DirectoryPath, true));
                                }
                                else
                                {
                                    installTasks.Add(CreateSymbolicLinkAsync(linkName, modInfo.DirectoryPath));
                                }
                            }
                        }
                    }
                }
                
                // 4. Handle Developer Mode files (if enabled)
                if (SettingsManager.Settings.DeveloperModeEnabled && !string.IsNullOrEmpty(SettingsManager.Settings.DeveloperExportPath))
                {
                    var devExportSourcePath = SettingsManager.Settings.DeveloperExportPath;
                    var enabledFileBases = GetDevEnabledFileBaseNames();

                    if (enabledFileBases.Any())
                    {
                        if (string.IsNullOrEmpty(SettingsManager.Settings.ModsDirectory))
                        {
                            UpdateStatus("Developer mode files not installed: Mods directory is not set.");
                            return false;
                        }

                        // Create a temporary directory inside the mods folder to hold hardlinks
                        var devModDir = Path.Combine(SettingsManager.Settings.ModsDirectory, "_DevExport");
                        if (Directory.Exists(devModDir)) Directory.Delete(devModDir, true);
                        Directory.CreateDirectory(devModDir);

                        foreach (var baseName in enabledFileBases)
                        {
                            var sourceFiles = Directory.GetFiles(devExportSourcePath, $"{baseName}.*");
                            foreach (var sourceFile in sourceFiles)
                            {
                                // Append _P to the filename for the game to recognize it as a pak file.
                                var fileName = Path.GetFileNameWithoutExtension(sourceFile);
                                var extension = Path.GetExtension(sourceFile);
                                var destFile = Path.Combine(devModDir, $"{fileName}_P{extension}");
                                await CreateHardLinkAsync(destFile, sourceFile);
                            }
                        }

                        // Now, create a symbolic link from ~mods to our _DevExport folder
                        var devLinkName = Path.Combine(targetModsDir, "000-DevExport"); // "000" prefix for highest priority
                        if (useCopy)
                        {
                            await CopyDirectoryAsync(devLinkName, devModDir, true);
                        }
                        else
                        {
                            await CreateSymbolicLinkAsync(devLinkName, devModDir);
                        }
                        UpdateStatus("Installed developer export files.");
                    }
                }

                // Wait for all link creation tasks to complete.
                bool[] results = await Task.WhenAll(installTasks);
                int installedCount = results.Count(wasSuccessful => wasSuccessful);

                UpdateStatus($"Successfully installed {installedCount} of {installTasks.Count} enabled mod(s).");

                // Note: do NOT add the LocresMod pak here — it must be added after UnrealPak has
                // finished producing the final `LocresMod.pak`. That step is handled after
                // `PackMergedLocresAsync` completes so we don't install a clean/old pak.
                return true;
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"An error occurred during mod installation: {ex.Message}", "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Mod installation failed.");
                return false;
            }
        }

        private async Task EnsureUe4ssInstalledAsync(string gamePath)
        {
            var win64Dir = Path.Combine(gamePath, "UNION", "Binaries", "Win64");
            var ue4ssDir = Path.Combine(win64Dir, "ue4ss");

            if (Directory.Exists(ue4ssDir)) return;

            var result = CustomMessageBox.Show(
                "One or more enabled mods require UE4SS (Unreal Engine 4 Scripting System) to function.\n\n" +
                "It is not currently installed. Would you like to download and install it automatically now?",
                "UE4SS Required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                UpdateStatus("UE4SS installation skipped. Some mods may not work.");
                return;
            }

            UpdateStatus("UE4SS not found. Downloading and installing...");
            string downloadUrl = "https://gamebanana.com/dl/1534195";
            string tempFile = Path.Combine(Path.GetTempPath(), "ue4ss_install.7z");

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    var response = await client.GetAsync(downloadUrl);
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(tempFile, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                UpdateStatus("Extracting UE4SS...");
                Directory.CreateDirectory(win64Dir);

                await Task.Run(() =>
                {
                    using (var archive = ArchiveFactory.OpenArchive(tempFile))
                    {
                        archive.WriteToDirectory(win64Dir, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to install UE4SS: {ex.Message}");
                CustomMessageBox.Show($"Failed to automatically install UE4SS. Please install it manually.\nError: {ex.Message}", "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        private static void SetConfirmationMode(Form form, GameBananaFile? fileToInstall)
        {
            if (form is ModDetailsFormLinux linuxForm)
                linuxForm.SetConfirmationMode(fileToInstall);
            else if (form is ModDetailsForm winForm)
                winForm.SetConfirmationMode(fileToInstall);
        }

        private static string GetModFormName(Form form)
        {
            if (form is ModDetailsFormLinux linuxForm)
                return linuxForm.ModName;
            return ((ModDetailsForm)form).ModName;
        }

        private static Task LaunchModFormProgress(Form form, GameBananaFile fileToInstall)
        {
            if (form is ModDetailsFormLinux linuxForm)
                return linuxForm.LaunchProgressForm(fileToInstall);
            return ((ModDetailsForm)form).LaunchProgressForm(fileToInstall);
        }

        private async Task<bool> CreateSymbolicLinkAsync(string linkPath, string targetPath)
        {
            if (PlatformUtils.IsWindows)
            {
                // Use Junctions on Windows to avoid Admin requirement for symbolic links
                try
                {
                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        process.Start();
                        await process.WaitForExitAsync();
                        return process.ExitCode == 0;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Junction failed: {ex.Message}");
                    return false;
                }
            }
            else
            {
                try
                {
                    await Task.Run(() => Directory.CreateSymbolicLink(linkPath, targetPath));
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Symlink failed: {ex.Message}");
                    return false;
                }
            }
        }

        private async Task<bool> CreateFileSymbolicLinkAsync(string linkPath, string targetPath)
        {
            if (PlatformUtils.IsWindows)
            {
                try
                {
                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c mklink \"{linkPath}\" \"{targetPath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        process.Start();
                        await process.WaitForExitAsync();
                        return process.ExitCode == 0;
                    }
                }
                catch { return false; }
            }
            else
            {
                try { File.CreateSymbolicLink(linkPath, targetPath); return true; }
                catch { return false; }
            }
        }

        private async Task<bool> CreateHardLinkAsync(string linkPath, string targetPath)
        {
            // Delete the old link if it exists, to ensure we can create a new one.
            if (File.Exists(linkPath))
            {
                File.Delete(linkPath);
            }

            string command;
            string arguments;

            if (PlatformUtils.IsWindows)
            {
                command = "cmd.exe";
                arguments = $"/c mklink /H \"{linkPath}\" \"{targetPath}\"";
            }
            else // Linux/macOS
            {
                command = "ln";
                arguments = $"-s \"{targetPath}\" \"{linkPath}\"";
            }

            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
        }
        
        private bool IsUe4ssScriptMod(string modPath)
        {
            try
            {
                var scriptsDir = Path.Combine(modPath, "Scripts");
                return Directory.Exists(scriptsDir) && Directory.EnumerateFiles(scriptsDir, "*.lua").Any();
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> InstallUe4ssModAsync(string destPath, string sourcePath)
        {
            bool success = await CopyDirectoryAsync(destPath, sourcePath, false);
            if (success)
            {
                try
                {
                    await File.WriteAllTextAsync(Path.Combine(destPath, "enabled.txt"), "");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to create enabled.txt: {ex.Message}");
                }
            }
            return success;
        }

        private async Task<bool> CopyDirectoryAsync(string destDir, string sourceDir, bool markAsManaged = false)
        {
            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(destDir)) Directory.Delete(destDir, true);
                    Directory.CreateDirectory(destDir);
                    CopyDirRecursive(sourceDir, destDir);
                    if (markAsManaged)
                    {
                        File.WriteAllText(Path.Combine(destDir, ".bsm_managed"), "");
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Copy failed: {ex.Message}");
                return false;
            }
        }

        private void CopyDirRecursive(string sourceDir, string destDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            foreach (FileInfo file in dir.GetFiles())
                file.CopyTo(Path.Combine(destDir, file.Name), true);

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                string newDestDir = Path.Combine(destDir, subDir.Name);
                Directory.CreateDirectory(newDestDir);
                CopyDirRecursive(subDir.FullName, newDestDir);
            }
        }

        private void PromptForModsDirectory()
        {
            CustomMessageBox.Show("Welcome! Please select a folder to store your mods.", "First-Time Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            while (true)
            {
                // TODO: add titlebar text when NativeFileDialogs supports it
                NfdStatus result = Nfd.PickFolder(out string? dir);
                if (result == NfdStatus.Ok)
                {
                    if (dir == null) continue;
                    var dirName = new DirectoryInfo(dir).Name;
                    if (dirName.Equals("~mods", StringComparison.OrdinalIgnoreCase))
                    {
                        CustomMessageBox.Show("You cannot select the game's '~mods' folder as your mod storage directory.\n\nThis folder is used by the manager to install mods. Please select a different folder to store your source mods.", "Invalid Directory", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    SettingsManager.Settings.ModsDirectory = dir;
                    SettingsManager.Save();
                    break;
                }
                UpdateStatus("Warning: Mods directory not selected.");
                break;
            }
        }

        private void btnAddMod_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SettingsManager.Settings.ModsDirectory) || !Directory.Exists(SettingsManager.Settings.ModsDirectory))
            {
                CustomMessageBox.Show("The mods directory is not configured. Please set it in Settings before adding mods.", "Mods Directory Not Set", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            Dictionary<string, string> filters = new Dictionary<string, string>
            {
                {"Mod Archives", "zip,7z,rar,tar,tar.gz,tar.xz,tar.zst,tar.bz2,tar.lz"}
            };

            // TODO: add titlebar text when NativeFileDialogs supports it
            NfdStatus pickerResult = Nfd.OpenDialogMultiple(out string[]? filePaths, filters);
        
            if (pickerResult == NfdStatus.Ok)
            {
                int successCount = 0;
                var modsDirectory = SettingsManager.Settings.ModsDirectory;

                // Backup mods before adding any new mods (unless user disabled automatic backups)
                try
                {
                    if (!SettingsManager.Settings.DoNotBackupModsAutomatically)
                        ModBackupManager.BackupMods(modsDirectory);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to create backup before adding mods: {ex.Message}");
                }

                filePaths ??= [];
                foreach (var file in filePaths)
                {
                    try
                    {
                        string modName = Path.GetFileNameWithoutExtension(file);
                        string targetDir = Path.Combine(modsDirectory, modName); 

                        if (Directory.Exists(targetDir))
                        {
                            var result = CustomMessageBox.Show($"A mod named '{modName}' already exists. Do you want to overwrite it?", "Mod Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (result == DialogResult.No) continue;
                            Directory.Delete(targetDir, true);
                        }

                        Directory.CreateDirectory(targetDir);

                        using (var archive = ArchiveFactory.OpenArchive(file))
                        {
                            archive.WriteToDirectory(targetDir, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                        }
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show($"Failed to install mod from '{Path.GetFileName(file)}':\n{ex.Message}", "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                UpdateStatus($"{successCount} of {filePaths.Length} mod(s) installed.");
                RefreshModList();
            }
        }

        private void btnRemoveMod_Click(object sender, EventArgs e)
        {
            var selectedItems = modListView.SelectedItems.Cast<ListViewItem>().ToList();
            if (selectedItems.Count == 0)
            {
                CustomMessageBox.Show("Please select a mod to remove.", "Remove Mod", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (CustomMessageBox.Show($"Are you sure you want to permanently delete the selected {selectedItems.Count} mod(s)? This cannot be undone.", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                foreach (ListViewItem item in selectedItems)
                {
                    if (item.Tag is ModInfo modInfo)
                    {
                        try
                        {
                            if (Directory.Exists(modInfo.DirectoryPath))
                            {
                                Directory.Delete(modInfo.DirectoryPath, true);
                            }
                            _allModItems.Remove(item);
                        }
                        catch (Exception ex)
                        {
                            CustomMessageBox.Show($"Failed to delete mod '{modInfo.Name}':\n{ex.Message}", "Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else if (item.Tag is ModSection)
                    {
                        _allModItems.Remove(item);
                    }
                }

                ApplyFilter(); // Refresh the list view
                SaveModListState(); // Save the new load order without the deleted mods
                UpdateStatus($"{selectedItems.Count} mod(s) deleted.");
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshModList();
            _ = CheckForModUpdatesAsync();
        }

        private void MoveSelectedItem(int direction)
        {
            if (modListView.SelectedItems.Count != 1) return;

            var selectedItem = modListView.SelectedItems[0];
            int index = selectedItem.Index;
            int newIndex = index + direction;

            if (newIndex >= 0 && newIndex < modListView.Items.Count)
            {
                modListView.Items.RemoveAt(index);
                modListView.Items.Insert(newIndex, selectedItem);
                modListView.Items[newIndex].Selected = true;
                modListView.Focus();

                _allModItems.Clear();
                _allModItems.AddRange(modListView.Items.Cast<ListViewItem>());
            }
        }

        private void modListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            var modInfo = modListView.SelectedItems.Count > 0 ? modListView.SelectedItems[0].Tag as ModInfo : null;
            UpdateModDetails(modInfo);
        }

        private void modListView_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            // Prevent checking/unchecking section items
            if (modListView.Items[e.Index].Tag is ModSection)
            {
                e.NewValue = e.CurrentValue;
            }
        }

        private void modListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            // To prevent this from running for every item during a refresh, we check if the listview has focus.
            if (modListView.Focused)
                e.Item.ForeColor = e.Item.Checked ? Color.White : Color.Gray;
        }

        private void SortModsView()
        {
            // Update colors for all items
            foreach (ListViewItem item in _allModItems)
                item.ForeColor = item.Checked ? Color.White : Color.Gray;

            if (SettingsManager.Settings.SortEnabledModsToTop)
            {
                var enabledItems = _allModItems.Where(i => i.Checked).ToList();
                var disabledItems = _allModItems.Where(i => !i.Checked).ToList();

                _allModItems.Clear();
                _allModItems.AddRange(enabledItems);
                _allModItems.AddRange(disabledItems);
                
                ApplyFilter(); // Refresh the list view with new order
            }
        }

        private void modListView_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            if (e.Item?.Tag is ModSection section)
            {
                // For sections, we draw a custom separator style across the entire row
                if (e.ColumnIndex == 0)
                {
                    // Calculate the full bounds of the row (spanning all columns)
                    var rowBounds = e.Item.Bounds;
                    
                    // Fill background
                    using (var brush = new SolidBrush(ThemeManager.CurrentTheme.ControlBackColor))
                    {
                        e.Graphics.FillRectangle(brush, rowBounds);
                    }

                    // Draw the text centered
                    var text = section.Name;
                    var textColor = section.TextColor ?? ThemeManager.CurrentTheme.AccentColor;
                    using (var font = new Font(e.Item.Font, FontStyle.Bold))
                    {
                        // Use TextRenderer for better measurement and drawing (fixes clipping and centering)
                        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
                        var textSize = TextRenderer.MeasureText(e.Graphics, text, font, new Size(rowBounds.Width, rowBounds.Height), flags);
                        
                        var textRect = new Rectangle(
                            rowBounds.X + (rowBounds.Width - textSize.Width) / 2,
                            rowBounds.Y + (rowBounds.Height - textSize.Height) / 2,
                            textSize.Width,
                            textSize.Height);

                        TextRenderer.DrawText(e.Graphics, text, font, textRect, textColor, flags);

                        // Draw lines on left and right
                        using (var pen = new Pen(ThemeManager.CurrentTheme.BorderColor))
                        {
                            int midY = rowBounds.Y + rowBounds.Height / 2;
                            // Left line
                            e.Graphics.DrawLine(pen, rowBounds.X + 10, midY, textRect.X - 10, midY);
                            // Right line
                            e.Graphics.DrawLine(pen, textRect.Right + 10, midY, rowBounds.Right - 10, midY);
                        }
                    }
                }
                // We handled everything in column 0, so do nothing for other columns
            }
            else
            {
                // For normal items, let the system draw it (including checkboxes)
                e.DrawDefault = true;
            }
        }

        private void modListView_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void CreateNewGroupFromSelection(object? sender, EventArgs e)
        {
            var selectedItems = modListView.SelectedItems.Cast<ListViewItem>().ToList();
            if (selectedItems.Count == 0) return;

            string name = Prompt.ShowDialog("Enter a name for the new mod group:", "Create Group");
            if (string.IsNullOrWhiteSpace(name)) return;

            if (SettingsManager.Settings.ModGroups.ContainsKey(name))
            {
                if (CustomMessageBox.Show($"Group '{name}' already exists. Overwrite?", "Overwrite Group", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }

            var groupMods = new List<string>();
            foreach (var item in selectedItems)
            {
                if (item.Tag is ModInfo modInfo)
                {
                    groupMods.Add(Path.GetFileName(modInfo.DirectoryPath));
                }
            }

            SettingsManager.Settings.ModGroups[name] = groupMods;
            SettingsManager.Save();
            UpdateGroupsMenu();
            UpdateStatus($"Group '{name}' created with {groupMods.Count} mods.");
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm())
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // Reload everything to apply new settings
                    LoadSettingsAndSetup();
                    ThemeManager.SetTheme(SettingsManager.Settings.SelectedTheme);
                    ThemeManager.ApplyTheme(this);
                    UpdateDeveloperTabVisibility();
                }
            }
        }

        private void UpdateDeveloperTabVisibility()
        {
            if (SettingsManager.Settings.DeveloperModeEnabled)
            {
                if (!tabControlMain.TabPages.Contains(tabDeveloper))
                {
                    tabControlMain.TabPages.Add(tabDeveloper);
                }
                LoadDeveloperSettings();
            }
            else
            {
                if (tabControlMain.TabPages.Contains(tabDeveloper))
                {
                    tabControlMain.TabPages.Remove(tabDeveloper);
                }
            }
        }

        private void ParentMenu_DropDownOpened(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem itm)
            {
                itm.ForeColor = Color.Black;
            }
        }

        private void ParentMenu_DropDownClosed(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem itm)
            {
                itm.ForeColor = Color.White;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var aboutForm = new AboutForm())
            {
                aboutForm.ShowDialog(this);
            }
        }

        private void UpdateStatus(string message)
        {
            toolStripStatusLabel1.Text = message;
        }

        private async void btnEnableAll_Click(object? sender, EventArgs e)
        {
            await ApplyEnableDisableAll(true);
        }

        private async void btnDisableAll_Click(object? sender, EventArgs e)
        {
            await ApplyEnableDisableAll(false);
        }

        private async Task ApplyEnableDisableAll(bool enable)
        {
            if (!SettingsManager.Settings.DoNotConfirmEnableDisable)
            {
                using var dlg = new ConfirmActionForm(enable ? "Enable all mods?" : "Disable all mods?", "Do not show this again");
                var res = dlg.ShowDialog(this);
                if (res != DialogResult.Yes)
                {
                    return;
                }
                if (dlg.DontShowAgain)
                {
                    SettingsManager.Settings.DoNotConfirmEnableDisable = true;
                    SettingsManager.Save();
                }
            }

            foreach (ListViewItem item in modListView.Items)
            {
                if (item.Tag is ModInfo) // Only check/uncheck actual mods, ignore sections
                {
                    item.Checked = enable;
                }
            }
            SaveModListState();
            UpdateStatus(enable ? "Enabling all mods and applying changes..." : "Disabling all mods and applying changes...");
            await InstallModsAsync();
            UpdateStatus(enable ? "All mods enabled." : "All mods disabled.");
        }

        private void enableAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = ApplyEnableDisableAll(true);
        }

        private void disableAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = ApplyEnableDisableAll(false);
        }

        private void organizeAlphabeticallyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var items = modListView.Items.Cast<ListViewItem>().OrderBy(i => i.Text, StringComparer.InvariantCultureIgnoreCase).ToArray();
            modListView.BeginUpdate();
            modListView.Items.Clear();
            modListView.Items.AddRange(items);
            modListView.EndUpdate();
            _allModItems.Clear();
            _allModItems.AddRange(items);
            SaveModListState();
            UpdateStatus("Organized mods alphabetically.");
        }

        private void moveToTopContextMenuItem_Click(object sender, EventArgs e)
        {
            var selected = modListView.SelectedItems.Cast<ListViewItem>().ToList();
            if (selected.Count == 0) return;
            // Insert selected items at top preserving their order
            foreach (var item in selected)
            {
                modListView.Items.Remove(item);
            }
            int insertIndex = 0;
            foreach (var item in selected)
            {
                modListView.Items.Insert(insertIndex++, item);
            }
            _allModItems.Clear();
            _allModItems.AddRange(modListView.Items.Cast<ListViewItem>());
            SaveModListState();
            UpdateStatus($"Moved {selected.Count} mod(s) to top.");
        }

        private void moveToBottomContextMenuItem_Click(object sender, EventArgs e)
        {
            var selected = modListView.SelectedItems.Cast<ListViewItem>().ToList();
            if (selected.Count == 0) return;
            foreach (var item in selected)
            {
                modListView.Items.Remove(item);
                modListView.Items.Add(item);
            }
            _allModItems.Clear();
            _allModItems.AddRange(modListView.Items.Cast<ListViewItem>());
            SaveModListState();
            UpdateStatus($"Moved {selected.Count} mod(s) to bottom.");
        }

        private void modListView_MouseClick(object sender, MouseEventArgs e)
        {
            var hitTestInfo = modListView.HitTest(e.X, e.Y);
            var item = hitTestInfo.Item;
            var subItem = hitTestInfo.SubItem;

            if (item == null || subItem == null) return;

            if (item.Tag is not ModInfo modInfo) return;

            int columnIndex = item.SubItems.IndexOf(subItem);

            // Check for a click in the "Update" column (index 3).
            if (columnIndex == 3 && subItem.Text.Contains("Update"))
            {
                HandleModUpdateClick(modInfo);
            }
            // Check for a click in the "Actions" column (index 2).
            else if (columnIndex == 2)
            {
                if (modInfo.ConfigurationGroups.Any() && subItem.Text.Contains("Configure"))
                {
                    ShowModConfigForm(modInfo);
                }
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private async void HandleModUpdateClick(ModInfo modInfo)
        {
            var result = CustomMessageBox.Show($"An update is available for '{modInfo.Name}'.\n\nWould you like to view the update details and install it now?", "Update Mod", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.No) return;

            try
            {
                // We need the ItemId and ItemType to fetch the full mod object.
                var iniPath = Path.Combine(modInfo.DirectoryPath, "mod.ini");
                if (!File.Exists(iniPath))
                {
                    CustomMessageBox.Show("Could not find mod.ini file to get update information.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var iniData = IniParser.Parse(iniPath);
                if (!iniData.TryGetValue("GameBanana", out var gbSection) ||
                    !gbSection.TryGetValue("ItemId", out var itemIdStr) ||
                    !int.TryParse(itemIdStr, out var itemId) ||
                    !gbSection.TryGetValue("ItemType", out var itemType))
                {
                    CustomMessageBox.Show("Could not find valid GameBanana information in the mod's mod.ini file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UpdateStatus($"Fetching update info for '{modInfo.Name}'...");
                var gameBananaMod = await GameBananaApiService.GetModFromProfilePageAsync(itemType, itemId);

                if (gameBananaMod == null)
                {
                    throw new Exception("Failed to retrieve mod details from GameBanana API.");
                }

                // Open the ModDetailsForm, which will handle the download/install process.
                // This is the same behavior as clicking "Download" in the mod browser.
                using (var modDetailsForm = PlatformUtils.IsLinux
                    ? (Form)new ModDetailsFormLinux(gameBananaMod, new Progress<string>(s => AppendLog(s)), RefreshModList)
                    : new ModDetailsForm(gameBananaMod, new Progress<string>(s => AppendLog(s)), RefreshModList))
                {
                    modDetailsForm.ShowDialog(this);
                }
                
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to start update process:\n\n{ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnToggleDebugLog_Click(object sender, EventArgs e)
        {
            if (splitContainerRoot.Panel2Collapsed)
            {
                splitContainerRoot.Panel2Collapsed = false;
                btnToggleDebugLog.Text = "Hide Log";
            }
            else
            {
                splitContainerRoot.Panel2Collapsed = true;
                btnToggleDebugLog.Text = "Log";
            }
        }

        private void ShowPromoPopup()
        {
            try
            {
                string flagPath = PlatformUtils.GetPromoFlagPath("megaman_promo.flag");
                if (File.Exists(flagPath)) return;

                // Ensure the directory exists before trying to show/write the flag
                var flagDir = Path.GetDirectoryName(flagPath);
                if (flagDir != null && !Directory.Exists(flagDir)) Directory.CreateDirectory(flagDir);

                using (var promoForm = new CrossworldsModManager.MegaManPromoForm())
                {
                    promoForm.ShowDialog(this);
                    if (promoForm.DoNotShowAgain)
                    {
                        File.WriteAllText(flagPath, "seen");
                    }
                }
            }
            catch { /* Ignore errors in promo */ }
        }

        #region Context Menu

        private void modContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Allow opening even if no selection (to add section at the end)
            bool hasSelection = modListView.SelectedItems.Count > 0;

            bool singleSelection = modListView.SelectedItems.Count == 1;
            var selectedItem = singleSelection ? modListView.SelectedItems[0] : null;
            var modInfo = selectedItem?.Tag as ModInfo;
            var section = selectedItem?.Tag as ModSection;

            bool isMod = modInfo != null;
            bool isSection = section != null;

            // Configure option
            configureToolStripMenuItem.Visible = isMod;
            configureToolStripMenuItem.Enabled = singleSelection && (modInfo?.ConfigurationGroups.Any() ?? false);

            // Single item only actions
            openFolderToolStripMenuItem.Visible = isMod;
            openFolderToolStripMenuItem.Enabled = singleSelection && isMod;
            
            toggleEnabledToolStripMenuItem.Visible = isMod;
            toggleEnabledToolStripMenuItem.Enabled = singleSelection && isMod;

            if (renameToolStripMenuItem != null)
            {
                renameToolStripMenuItem.Visible = isSection;
                renameToolStripMenuItem.Enabled = singleSelection && isSection;
            }
            if (changeColorToolStripMenuItem != null)
            {
                changeColorToolStripMenuItem.Visible = isSection;
                changeColorToolStripMenuItem.Enabled = singleSelection && isSection;
            }
            if (configMakerItem != null)
            {
                configMakerItem.Visible = isMod;
            }
            if (normalizeRootItem != null)
            {
                // Only show if there is a selection and it contains at least one mod (not just sections)
                normalizeRootItem.Visible = hasSelection && modListView.SelectedItems.Cast<ListViewItem>().Any(i => i.Tag is ModInfo);
            }

            // Move Up/Down options
            moveUpToolStripMenuItem1.Enabled = singleSelection && selectedItem != null && selectedItem.Index > 0;
            moveDownToolStripMenuItem1.Enabled = singleSelection && selectedItem != null && selectedItem.Index < modListView.Items.Count - 1;

            // Delete option
            deleteToolStripMenuItem.Enabled = hasSelection;
            
            // Move to Top/Bottom
            moveToTopToolStripMenuItem.Enabled = hasSelection;
            moveToBottomToolStripMenuItem.Enabled = hasSelection;

            // Populate Add to Group menu
            bool canAddToGroup = hasSelection && !modListView.SelectedItems.Cast<ListViewItem>().Any(i => i.Tag is ModSection);
            addToGroupToolStripMenuItem.Visible = canAddToGroup;

            addToGroupToolStripMenuItem.DropDownItems.Clear();

            var newGroupItem = new ToolStripMenuItem("New Group...");
            newGroupItem.Click += CreateNewGroupFromSelection;
            addToGroupToolStripMenuItem.DropDownItems.Add(newGroupItem);
            
            addToGroupToolStripMenuItem.Enabled = canAddToGroup;

            if (SettingsManager.Settings.ModGroups.Count > 0)
            {
                addToGroupToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
                foreach (var groupName in SettingsManager.Settings.ModGroups.Keys.OrderBy(g => g))
                {
                    var item = new ToolStripMenuItem(groupName);
                    item.Click += (s, args) => AddSelectionToGroup(groupName);
                    addToGroupToolStripMenuItem.DropDownItems.Add(item);
                }
            }
        }

        private void ShowModConfigForm(ModInfo modInfo)
        {
            using (var configForm = new ModConfigForm(modInfo))
            {
                var activeProfile = GetActiveProfile();
                if (activeProfile == null) return;

                if (configForm.ShowDialog(this) == DialogResult.OK)
                {
                    // The new config form will save configurations directly to the active profile.
                    // We just need to save the settings file.
                    SettingsManager.Save();
                    UpdateStatus($"Configuration saved for '{modInfo.Name}'. Click Save to apply changes.");
                }
            }
        }

        private void configureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (modListView.SelectedItems.Count != 1) return;
            var item = modListView.SelectedItems[0];

            if (item.Tag is ModInfo modInfo && modInfo.ConfigurationGroups.Any())
            {
                ShowModConfigForm(modInfo);
            }
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (modListView.SelectedItems.Count != 1) return;
            var item = modListView.SelectedItems[0];

            if (item.Tag is ModInfo modInfo)
            {
                PlatformUtils.OpenFolderInExplorer(modInfo.DirectoryPath);
                
            }
        }

        private void toggleEnabledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (modListView.SelectedItems.Count != 1) return;
            var item = modListView.SelectedItems[0];
            item.Checked = !item.Checked;
        }

        private void moveUpContextMenuItem_Click(object sender, EventArgs e)
        {
            MoveSelectedItem(-1);
        }

        private void moveDownContextMenuItem_Click(object sender, EventArgs e)
        {
            MoveSelectedItem(1);
        }

        private void ConfigMakerItem_Click(object? sender, EventArgs e)
        {
            if (modListView.SelectedItems.Count != 1) return;
            var item = modListView.SelectedItems[0];

            if (item.Tag is ModInfo modInfo)
            {
                string modRoot = modInfo.DirectoryPath;
                try
                {
                    // Re-run non-destructive nested detection to ensure we open the true mod root
                    var iniFiles = Directory.GetFiles(modInfo.DirectoryPath, "mod.ini", SearchOption.AllDirectories);
                    if (iniFiles.Length > 0)
                    {
                        string fullModPath = Path.GetFullPath(modInfo.DirectoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string? foundIni = null;
                        foreach (var f in iniFiles)
                        {
                            var fDir = Path.GetDirectoryName(f);
                            if (string.IsNullOrEmpty(fDir)) continue;
                            var fullDir = Path.GetFullPath(fDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            if (!fullDir.Equals(fullModPath, StringComparison.OrdinalIgnoreCase))
                            {
                                foundIni = f;
                                break;
                            }
                        }

                        if (foundIni != null)
                        {
                            var candidate = Path.GetDirectoryName(foundIni)!;
                            if (Path.GetFullPath(candidate).StartsWith(fullModPath, StringComparison.OrdinalIgnoreCase))
                            {
                                modRoot = candidate;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ConfigMaker: nested detection failed: {ex.Message}");
                }

                using (var editor = new ModConfigEditor(modRoot))
                {
                    editor.ShowDialog(this);
                }
            }
        }

        #endregion

        #region Profile Management

        private void MigrateToProfiles()
        {
            // If old settings exist and no profiles exist, migrate them.
            if (SettingsManager.Settings.Profiles.Count == 0 && SettingsManager.Settings.EnabledMods != null)
            {
                const string defaultProfileName = "Default";
                var defaultProfile = new ModProfile
                {
                    EnabledMods = SettingsManager.Settings.EnabledMods,
                    ModConfigurations = SettingsManager.Settings.ModConfigurations ?? new Dictionary<string, string>(),
                    ModLoadOrder = SettingsManager.Settings.ModLoadOrder ?? new List<string>()
                };

                SettingsManager.Settings.Profiles[defaultProfileName] = defaultProfile;
                SettingsManager.Settings.ActiveProfileName = defaultProfileName;

                // Clear old properties to finalize migration
                SettingsManager.Settings.EnabledMods = null;
                SettingsManager.Settings.ModConfigurations = null;
                SettingsManager.Settings.ModLoadOrder = null;

                SettingsManager.Save();
            }
            else if (SettingsManager.Settings.Profiles.Count == 0)
            {
                // First run, create a default profile.
                const string defaultProfileName = "Default";
                SettingsManager.Settings.Profiles[defaultProfileName] = new ModProfile();
                SettingsManager.Settings.ActiveProfileName = defaultProfileName;
                SettingsManager.Save();
            }
        }

        private ModProfile? GetActiveProfile()
        {
            if (string.IsNullOrEmpty(SettingsManager.Settings.ActiveProfileName) ||
                !SettingsManager.Settings.Profiles.TryGetValue(SettingsManager.Settings.ActiveProfileName, out var profile))
            {
                // Fallback if active profile is missing
                if (SettingsManager.Settings.Profiles.Any())
                {
                    SettingsManager.Settings.ActiveProfileName = SettingsManager.Settings.Profiles.Keys.First();
                    return SettingsManager.Settings.Profiles.Values.First();
                }
                return null;
            }
            return profile;
        }

        private void UpdateProfilesMenu()
        {
            profilesToolStripMenuItem.DropDownItems.Clear();

            foreach (var profileName in SettingsManager.Settings.Profiles.Keys.OrderBy(p => p))
            {
                var item = new ToolStripMenuItem(profileName)
                {
                    Tag = profileName,
                    Checked = profileName == SettingsManager.Settings.ActiveProfileName,
                    CheckOnClick = true,
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(45, 45, 48)
                };
                item.Click += ProfileMenuItem_Click;
                profilesToolStripMenuItem.DropDownItems.Add(item);
            }

            profilesToolStripMenuItem.DropDownItems.Add(toolStripSeparator3);
            profilesToolStripMenuItem.DropDownItems.Add(newProfileToolStripMenuItem);
            profilesToolStripMenuItem.DropDownItems.Add(renameProfileToolStripMenuItem);
            profilesToolStripMenuItem.DropDownItems.Add(deleteProfileToolStripMenuItem);
        }

        private void ProfileMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem clickedItem || clickedItem.Tag is not string profileName) return;

            // Uncheck all other items
            foreach (var item in profilesToolStripMenuItem.DropDownItems)
            {
                if (item is ToolStripMenuItem menuItem && menuItem != clickedItem)
                {
                    menuItem.Checked = false;
                }
            }
            clickedItem.Checked = true;

            SettingsManager.Settings.ActiveProfileName = profileName;
            SettingsManager.Save();
            RefreshModList();
            UpdateStatus($"Switched to profile: {profileName}");
        }

        private void newProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string newProfileName = Prompt.ShowDialog("Enter a name for the new profile:", "New Profile");
            if (string.IsNullOrWhiteSpace(newProfileName) || SettingsManager.Settings.Profiles.ContainsKey(newProfileName))
            {
                if (!string.IsNullOrWhiteSpace(newProfileName))
                    CustomMessageBox.Show("A profile with that name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SettingsManager.Settings.Profiles[newProfileName] = new ModProfile();
            SettingsManager.Settings.ActiveProfileName = newProfileName;
            SettingsManager.Save();
            UpdateProfilesMenu();
            RefreshModList();
        }

        private void renameProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentName = SettingsManager.Settings.ActiveProfileName;
            if (string.IsNullOrEmpty(currentName)) return;

            string newName = Prompt.ShowDialog("Enter a new name for the profile:", "Rename Profile", currentName);
            if (string.IsNullOrWhiteSpace(newName) || newName == currentName || SettingsManager.Settings.Profiles.ContainsKey(newName))
            {
                if (!string.IsNullOrWhiteSpace(newName) && newName != currentName)
                    CustomMessageBox.Show("A profile with that name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var profile = SettingsManager.Settings.Profiles[currentName];
            SettingsManager.Settings.Profiles.Remove(currentName);
            SettingsManager.Settings.Profiles[newName] = profile;
            SettingsManager.Settings.ActiveProfileName = newName;
            SettingsManager.Save();
            UpdateProfilesMenu();
        }

        private void deleteProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentName = SettingsManager.Settings.ActiveProfileName;
            if (string.IsNullOrEmpty(currentName) || SettingsManager.Settings.Profiles.Count <= 1)
            {
                CustomMessageBox.Show("You cannot delete the last profile.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (CustomMessageBox.Show($"Are you sure you want to delete the '{currentName}' profile?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                SettingsManager.Settings.Profiles.Remove(currentName);
                SettingsManager.Settings.ActiveProfileName = SettingsManager.Settings.Profiles.Keys.First();
                SettingsManager.Save();
                UpdateProfilesMenu();
                RefreshModList();
            }
        }

        #endregion

        #region Group Management

        private void UpdateGroupsMenu()
        {
            // Keep the first 3 items (Save, Manage, Separator) and remove the rest
            while (groupsToolStripMenuItem.DropDownItems.Count > 3)
            {
                groupsToolStripMenuItem.DropDownItems.RemoveAt(3);
            }

            foreach (var groupName in SettingsManager.Settings.ModGroups.Keys.OrderBy(g => g))
            {
                var groupItem = new ToolStripMenuItem(groupName)
                {
                    Tag = groupName,
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(45, 45, 48)
                };

                var loadExclusive = new ToolStripMenuItem("Load (Exclusive)")
                {
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(45, 45, 48)
                };
                loadExclusive.Click += (s, e) => ApplyModGroup(groupName, GroupApplyMode.Exclusive);

                var enableAdditive = new ToolStripMenuItem("Enable (Additive)")
                {
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(45, 45, 48)
                };
                enableAdditive.Click += (s, e) => ApplyModGroup(groupName, GroupApplyMode.Additive);

                var disableGroup = new ToolStripMenuItem("Disable Group Mods")
                {
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(45, 45, 48)
                };
                disableGroup.Click += (s, e) => ApplyModGroup(groupName, GroupApplyMode.Subtract);

                groupItem.DropDownItems.Add(loadExclusive);
                groupItem.DropDownItems.Add(enableAdditive);
                groupItem.DropDownItems.Add(disableGroup);

                groupsToolStripMenuItem.DropDownItems.Add(groupItem);
            }
        }

        private enum GroupApplyMode { Exclusive, Additive, Subtract }

        private void ApplyModGroup(string groupName, GroupApplyMode mode)
        {
            if (!SettingsManager.Settings.ModGroups.TryGetValue(groupName, out var modsInGroup)) return;

            var modsSet = new HashSet<string>(modsInGroup, StringComparer.OrdinalIgnoreCase);
            int changes = 0;

            foreach (ListViewItem item in modListView.Items)
            {
                if (item.Tag is ModInfo modInfo)
                {
                    string modFolderName = Path.GetFileName(modInfo.DirectoryPath);
                    bool isInGroup = modsSet.Contains(modFolderName);

                    if (mode == GroupApplyMode.Exclusive)
                    {
                        if (item.Checked != isInGroup)
                        {
                            item.Checked = isInGroup;
                            changes++;
                        }
                    }
                    else if (mode == GroupApplyMode.Additive)
                    {
                        if (isInGroup && !item.Checked)
                        {
                            item.Checked = true;
                            changes++;
                        }
                    }
                    else if (mode == GroupApplyMode.Subtract)
                    {
                        if (isInGroup && item.Checked)
                        {
                            item.Checked = false;
                            changes++;
                        }
                    }
                }
            }

            if (changes > 0)
            {
                SaveModListState();
                UpdateStatus($"Applied group '{groupName}' ({mode}). {changes} changes made.");
            }
            else
            {
                UpdateStatus($"Applied group '{groupName}'. No changes were necessary.");
            }
        }

        private void saveEnabledAsGroupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string name = Prompt.ShowDialog("Enter a name for the new mod group:", "Save Group");
            if (string.IsNullOrWhiteSpace(name)) return;

            if (SettingsManager.Settings.ModGroups.ContainsKey(name))
            {
                if (CustomMessageBox.Show($"Group '{name}' already exists. Overwrite?", "Overwrite Group", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }

            var enabledMods = new List<string>();
            foreach (ListViewItem item in modListView.Items)
            {
                if (item.Checked && item.Tag is ModInfo modInfo)
                {
                    enabledMods.Add(Path.GetFileName(modInfo.DirectoryPath));
                }
            }

            SettingsManager.Settings.ModGroups[name] = enabledMods;
            SettingsManager.Save();
            UpdateGroupsMenu();
            UpdateStatus($"Group '{name}' saved with {enabledMods.Count} mods.");
        }

        private void AddSelectionToGroup(string groupName)
        {
            var selectedItems = modListView.SelectedItems.Cast<ListViewItem>().ToList();
            if (selectedItems.Count == 0) return;

            if (!SettingsManager.Settings.ModGroups.TryGetValue(groupName, out var groupMods))
            {
                groupMods = new List<string>();
                SettingsManager.Settings.ModGroups[groupName] = groupMods;
            }

            int addedCount = 0;
            foreach (var item in selectedItems)
            {
                if (item.Tag is ModInfo modInfo)
                {
                    string modFolderName = Path.GetFileName(modInfo.DirectoryPath);
                    if (!groupMods.Any(m => m.Equals(modFolderName, StringComparison.OrdinalIgnoreCase)))
                    {
                        groupMods.Add(modFolderName);
                        addedCount++;
                    }
                }
            }

            if (addedCount > 0)
            {
                SettingsManager.Save();
                UpdateGroupsMenu();
                UpdateStatus($"Added {addedCount} mod(s) to group '{groupName}'.");
            }
            else
            {
                UpdateStatus($"Selected mods are already in group '{groupName}'.");
            }
        }

        private void manageGroupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var allMods = _allModItems
                .Select(item => item.Tag as ModInfo)
                .Where(info => info != null)
                .Select(info => Path.GetFileName(info!.DirectoryPath))
                .ToList();

            using (var form = new GroupManagerForm(allMods))
            {
                form.ShowDialog(this);
                UpdateGroupsMenu(); // Refresh menu after management
            }
        }

        private void btnBrowseMods_Click(object? sender, EventArgs e)
        {
            // Create a logger that reports to our main debug log window
            IProgress<string> browserLogger = new Progress<string>(s =>
            {
                AppendLog($"[GB Browser] {s}");
            });

            using (var browserForm = new GameBananaBrowserForm(browserLogger, RefreshModList))
            {
                browserForm.ShowDialog(this);
            }
        }

        private void btnBackupMods_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SettingsManager.Settings.ModsDirectory) || !Directory.Exists(SettingsManager.Settings.ModsDirectory))
            {
                CustomMessageBox.Show("Mods directory is not set or does not exist.", "Backup Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ModBackupManager.BackupMods(SettingsManager.Settings.ModsDirectory);
        }

        private void btnRestoreMods_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SettingsManager.Settings.ModsDirectory))
            {
                CustomMessageBox.Show("Mods directory is not set.", "Restore Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (CustomMessageBox.Show("Are you sure you want to restore mods from backup? This will overwrite existing files.", "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ModBackupManager.RestoreModsFromBackup(SettingsManager.Settings.ModsDirectory);
                RefreshModList();
            }
        }

        private async void HandleOneClickInstallAsync(string url)
        {
            try
            {
                // Format: bluestar:https://gamebanana.com/mmdl/DOWNLOAD_ID,TYPE,MOD_ID,ARCHIVE_TYPE
                var parts = url.Split(',');
                if (parts.Length < 3) throw new ArgumentException("Invalid 1-Click URL format (missing commas).");

                var urlPart = parts[0]; // e.g., bluestar:https://gamebanana.com/mmdl/1577381
                var downloadIdString = urlPart.Split('/').LastOrDefault();
                if (!int.TryParse(downloadIdString, out int downloadId))
                {
                    throw new ArgumentException("Could not parse Download ID from URL.");
                }
                
                if (!int.TryParse(parts[2], out int modId))
                {
                    throw new ArgumentException("Could not parse Mod ID from URL.");
                }

                var modType = parts[1];

                UpdateStatus($"1-Click Install: Fetching info for Mod ID {modId}, File ID {downloadId}...");

                // Fetch the full mod details from the profile page API before showing the dialog.
                var fullModInfo = await GameBananaApiService.GetModFromProfilePageAsync(modType, modId);

                if (fullModInfo == null)
                {
                    throw new Exception($"Could not retrieve mod information for ID {modId}. The mod may have been removed.");
                }

                // Now, get the list of files and find the one matching our download ID.
                var downloadPage = await GameBananaApiService.GetModDownloadPageAsync(fullModInfo);
                var fileToInstall = downloadPage?.Files?.FirstOrDefault(f => f.FileId == downloadId);

                // Create a logger that reports to our main debug log window
                IProgress<string> browserLogger = new Progress<string>(s =>
                {
                    AppendLog($"[1-Click] {s}");
                });

                if (fileToInstall == null)
                {
                    CustomMessageBox.Show($"The specified file (ID: {downloadId}) could not be found.\n\nThis might happen if the mod was updated. Please select the correct file from the list.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    using (var detailsForm = PlatformUtils.IsLinux
                        ? (Form)new ModDetailsFormLinux(fullModInfo, browserLogger, RefreshModList)
                        : new ModDetailsForm(fullModInfo, browserLogger, RefreshModList))
                    {
                        detailsForm.ShowDialog(this);
                    }
                    return;
                }

                // Update the status with the correct file name.
                UpdateStatus($"Found file: {fileToInstall.FileName}");

                using (var detailsForm = PlatformUtils.IsLinux
                    ? (Form)new ModDetailsFormLinux(fullModInfo, browserLogger, RefreshModList)
                    : new ModDetailsForm(fullModInfo, browserLogger, RefreshModList))
                {
                    SetConfirmationMode(detailsForm, fileToInstall);

                    try
                    {
                        if (!SettingsManager.Settings.DoNotBackupModsAutomatically && !string.IsNullOrWhiteSpace(SettingsManager.Settings.ModsDirectory) && Directory.Exists(SettingsManager.Settings.ModsDirectory))
                            ModBackupManager.BackupMods(SettingsManager.Settings.ModsDirectory);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to create backup before 1-Click install: {ex.Message}");
                    }

                    var result = detailsForm.ShowDialog(this);

                    if (result == DialogResult.OK)
                    {
                        var modName = GetModFormName(detailsForm);
                        await LaunchModFormProgress(detailsForm, fileToInstall);
                        UpdateStatus($"1-Click Install for '{modName}' successful!");
                    }
                    else
                    {
                        UpdateStatus("1-Click Install cancelled by user.");
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to handle 1-Click Install link:\n\n{ex.Message}", "1-Click Install Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("1-Click Install failed.");
            }
        }
        #endregion

        #region Drag and Drop Reordering

        private void modListView_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            var draggedItem = e.Item as ListViewItem;
            if (draggedItem == null) return;
            modListView.DoDragDrop(GetModDragSelection(draggedItem), DragDropEffects.Move);
        }

        private List<ListViewItem> GetModDragSelection(ListViewItem clickedItem)
        {
            var selected = modListView.SelectedItems.Cast<ListViewItem>().ToList();
            if (selected.Contains(clickedItem))
                return selected;
            return new List<ListViewItem> { clickedItem };
        }

        private void modListView_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = (e.Data != null && e.Data.GetDataPresent(typeof(List<ListViewItem>)))
                ? DragDropEffects.Move : DragDropEffects.None;
        }

        private void modListView_DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data == null || !e.Data.GetDataPresent(typeof(List<ListViewItem>)))
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            e.Effect = DragDropEffects.Move;

            Point pt = modListView.PointToClient(new Point(e.X, e.Y));
            int newIndex = CalculateDropInsertionIndex(pt);
            if (_dropInsertionIndex != newIndex || !_showDropLine)
            {
                _dropInsertionIndex = newIndex;
                _showDropLine = true;
                modListView.Invalidate();
            }
        }

        private void modListView_DragLeave(object? sender, EventArgs e)
        {
            if (_showDropLine)
            {
                _showDropLine = false;
                _dropInsertionIndex = -1;
                modListView.Invalidate();
            }
        }

        private int CalculateDropInsertionIndex(Point clientPt)
        {
            var items = modListView.Items;
            if (items.Count == 0) return 0;

            ListViewItem? hoverItem = modListView.GetItemAt(clientPt.X, clientPt.Y);
            if (hoverItem != null)
            {
                int midY = hoverItem.Bounds.Top + hoverItem.Bounds.Height / 2;
                return clientPt.Y < midY ? hoverItem.Index : hoverItem.Index + 1;
            }

            // Below the last item or on empty area
            var lastItem = items[^1];
            return clientPt.Y > lastItem.Bounds.Bottom ? items.Count : 0;
        }

        private void modListView_DragDrop(object? sender, DragEventArgs e)
        {
            _showDropLine = false;
            _dropInsertionIndex = -1;
            modListView.Invalidate();

            var dragged = e.Data?.GetData(typeof(List<ListViewItem>)) as List<ListViewItem>;
            if (dragged == null || dragged.Count == 0) return;

            Point dropPoint = modListView.PointToClient(new Point(e.X, e.Y));
            ListViewItem? targetItem = modListView.GetItemAt(dropPoint.X, dropPoint.Y);
            int targetIndex = CalculateDropInsertionIndex(dropPoint);

            // If the target itself is being dragged, bail out
            if (targetItem != null && dragged.Contains(targetItem))
                return;

            // Build list of (index, item) pairs sorted descending by index
            var targeted = dragged
                .Select(item => (index: item.Index, item))
                .OrderByDescending(p => p.index)
                .ToList();

            modListView.BeginUpdate();

            // Remove all dragged items (highest index first to preserve indices)
            foreach (var (_, item) in targeted)
                modListView.Items.Remove(item);

            // Adjust target index for removals that happened before it
            int insertAt = targetIndex;
            foreach (var (idx, _) in targeted.Where(p => p.index < targetIndex))
                insertAt--;

            // Re-insert in original order
            foreach (var item in dragged)
                modListView.Items.Insert(insertAt++, item);

            // Re-select
            foreach (var item in dragged)
                item.Selected = true;

            modListView.EndUpdate();
            modListView.Focus();

            SyncModListAfterReorder();
        }

        private void SyncModListAfterReorder()
        {
            _allModItems.Clear();
            _allModItems.AddRange(modListView.Items.Cast<ListViewItem>());
            SaveModListState();
        }

        private void modListView_Paint(object? sender, PaintEventArgs e)
        {
            if (!_showDropLine) return;

            int lineY = GetDropLineY();
            if (lineY < 0) return;

            using (var pen = new Pen(ThemeManager.CurrentTheme.AccentColor, 2))
            {
                e.Graphics.DrawLine(pen, 0, lineY, modListView.ClientSize.Width, lineY);
            }
        }

        private int GetDropLineY()
        {
            var items = modListView.Items;
            if (items.Count == 0) return _dropInsertionIndex == 0 ? 0 : -1;

            if (_dropInsertionIndex < items.Count)
                return items[_dropInsertionIndex].Bounds.Top;

            if (_dropInsertionIndex == items.Count)
                return items[^1].Bounds.Bottom;

            return -1;
        }

        #endregion

        #region Archive Extraction

        private async void UpdateModDetails(ModInfo? mod)
        {
            string? thumbPath = null;

            if (mod == null)
            {
                lblModName.Text = "Select a mod";
                lblModAuthor.Text = "";
                lblModVersion.Text = "";
                txtModDescription.Text = "";
            }
            else
            {
                lblModName.Text = mod.Name;
                lblModAuthor.Text = "By: " + mod.Author;
                lblModVersion.Text = "Version: " + (mod.Version == "-1" ? "1.0" : mod.Version);
                txtModDescription.Text = mod.Description;

                // Load Thumbnail
                string[] extensions = { ".jpg", ".png", ".jpeg", ".bmp", ".gif" };
                foreach (var ext in extensions)
                {
                    var p = Path.Combine(mod.DirectoryPath, "Thumb" + ext);
                    if (File.Exists(p))
                    {
                        thumbPath = p;
                        break;
                    }
                }

                // If no local thumbnail, try to download from GameBanana if info exists
                if (thumbPath == null && !string.IsNullOrEmpty(mod.GBItemId) && !string.IsNullOrEmpty(mod.GBItemType) && int.TryParse(mod.GBItemId, out int itemId))
                {
                    if (mod.GBItemType.Equals("Sound", StringComparison.OrdinalIgnoreCase))
                    {
                        var soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "Sound.jpg");
                        if (File.Exists(soundPath)) thumbPath = soundPath;
                    }
                    else
                    {
                        try
                        {
                            AppendLog($"Thumbnail for '{mod.Name}' not found locally. Attempting to download from GameBanana...");
                            var gbMod = await GameBananaApiService.GetModFromProfilePageAsync(mod.GBItemType, itemId);
                            if (gbMod != null && !string.IsNullOrEmpty(gbMod.ThumbnailUrl))
                            {
                                using (var client = new HttpClient())
                                {
                                    var thumbBytes = await client.GetByteArrayAsync(gbMod.ThumbnailUrl);
                                    string ext = Path.GetExtension(gbMod.ThumbnailUrl);
                                    if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                                    string newThumbPath = Path.Combine(mod.DirectoryPath, "Thumb" + ext);
                                    await File.WriteAllBytesAsync(newThumbPath, thumbBytes);
                                    thumbPath = newThumbPath; // Use the newly downloaded thumbnail
                                    AppendLog($"Successfully downloaded thumbnail for '{mod.Name}'.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            AppendLog($"Failed to download thumbnail for '{mod.Name}': {ex.Message}");
                        }
                    }
                }
            }

            if (thumbPath == null)
            {
                var fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "icon.png");
                if (File.Exists(fallbackPath)) thumbPath = fallbackPath;
            }

            if (thumbPath != null)
            {
                try
                {
                    using (var stream = new FileStream(thumbPath, FileMode.Open, FileAccess.Read))
                    {
                        var img = Image.FromStream(stream);
                        picModImage.Image = ProcessThumbnail(img);
                    }
                }
                catch { picModImage.Image = null; }
            }
            else
            {
                picModImage.Image = null;
            }
        }

        private Image ProcessThumbnail(Image img)
        {
            // Force 16:9 aspect ratio (Letterbox)
            int targetW = 640;
            int targetH = 360;
            
            var bmp = new Bitmap(targetW, targetH);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                
                float scale = Math.Min((float)targetW / img.Width, (float)targetH / img.Height);
                int scaleW = (int)(img.Width * scale);
                int scaleH = (int)(img.Height * scale);
                
                int x = (targetW - scaleW) / 2;
                int y = (targetH - scaleH) / 2;
                
                g.DrawImage(img, x, y, scaleW, scaleH);
            }
            return bmp;
        }

        private void AppendLog(string text)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.BeginInvoke(new Action<string>(AppendLog), text);
                return;
            }
            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\n");
            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.ScrollToCaret();
        }
        #endregion

        #region Developer Tools Logic

        private void LoadDeveloperSettings()
        {
            var path = SettingsManager.Settings.DeveloperExportPath;
            lblDevPath.Text = string.IsNullOrEmpty(path) ? "No export path selected." : path;
            lblDevPath.ForeColor = string.IsNullOrEmpty(path) ? Color.Gray : Color.White;
            ScanAndListDevFiles();
        }

        private void btnDevSelectPath_Click(object sender, EventArgs e)
        {
            // TODO: add titlebar text when NativeFileDialogs supports it
            NfdStatus result = Nfd.PickFolder(out string? folderPath);
            if (result ==  NfdStatus.Ok)
            {
                if (folderPath == null) return;
                SettingsManager.Settings.DeveloperExportPath = folderPath;
                SettingsManager.Save();
                LoadDeveloperSettings();
            }
        }

        private void btnDevRefresh_Click(object sender, EventArgs e)
        {
            ScanAndListDevFiles();
        }

        private void btnOpenModsFolder_Click(object? sender, EventArgs e)
        {
            string? gamePath = null;
            if (!string.IsNullOrEmpty(_selectedPlatform) && _gameInstallations.TryGetValue(_selectedPlatform, out var detectedGameInfo))
            {
                gamePath = detectedGameInfo.Path;
            }
            else
            {
                gamePath = SettingsManager.Settings.GameDirectory;
            }

            if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
            {
                CustomMessageBox.Show("Game directory is not set or not found. Cannot open ~mods folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var targetModsDir = Path.Combine(gamePath, "UNION", "Content", "Paks", "~mods");

            try
            {
                Directory.CreateDirectory(targetModsDir);

                PlatformUtils.OpenFolderInExplorer(targetModsDir);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to open folder:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ScanAndListDevFiles()
        {
            // Temporarily unsubscribe from the event to prevent it from firing during programmatic updates.
            chkListDevFiles.ItemCheck -= chkListDevFiles_ItemCheck;

            chkListDevFiles.Items.Clear();
            var path = SettingsManager.Settings.DeveloperExportPath;

            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                // Re-subscribe before exiting
                chkListDevFiles.ItemCheck += chkListDevFiles_ItemCheck;
                return;
            }

            var unifiedFiles = Directory.GetFiles(path)
                .Select(f => Path.GetFileNameWithoutExtension(f)!)
                .Where(f => !string.IsNullOrEmpty(f))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            var enabledFiles = new HashSet<string>(SettingsManager.Settings.DeveloperEnabledFiles, StringComparer.OrdinalIgnoreCase);

            foreach (var fileBaseName in unifiedFiles.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                bool isEnabled = enabledFiles.Contains(fileBaseName);
                chkListDevFiles.Items.Add(fileBaseName, isEnabled);
            }

            // Re-subscribe to the event now that the list is populated.
            chkListDevFiles.ItemCheck += chkListDevFiles_ItemCheck;
        }

        private void chkListDevFiles_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            // This event fires *before* the state is updated, so we schedule the save to happen right after.
            this.BeginInvoke((Action)(() => {
                SettingsManager.Settings.DeveloperEnabledFiles = GetDevEnabledFileBaseNames();
                SettingsManager.Save();
            }));
        }

        public List<string> GetDevEnabledFileBaseNames()
        {
            return chkListDevFiles.CheckedItems.Cast<string>().ToList();
        }

        #endregion
    }
#pragma warning restore CA1416
}
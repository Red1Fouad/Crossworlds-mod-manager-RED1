using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using System.IO.Compression;
using SharpCompress.Archives;
using System.Windows.Forms;
using System.Net.Http;
using System.Threading.Tasks;


namespace CrossworldsModManager
{
    // Suppress CA1416 as System.Drawing is supported on Linux via libgdiplus for this application
#pragma warning disable CA1416
    public partial class ModDetailsForm : Form
    {
        private readonly GameBananaMod _mod;
        private readonly IProgress<string>? _logger;
        private readonly Action? _onModsChanged;
        private bool _isConfirmationMode = false;

        private PictureBox picModImage = null!;
        private Label lblModName = null!;
        private Label lblAuthor = null!;
        private LinkLabel lnkProfileUrl = null!; 
        private Label lblLikeCount = null!;
        private WebBrowser webDescription = null!; 
        private Button btnDownload = null!;
        private ListBox lstFiles = null!;
        private ProgressBar prgDownload = null!;
        private Label lblProgress = null!;
        private TableLayoutPanel mainLayout = null!;

        public string ModName => _mod.Name;

        public ModDetailsForm(GameBananaMod mod, IProgress<string>? logger = null, Action? onModsChanged = null)
        {
            _mod = mod;
            _logger = logger;
            _onModsChanged = onModsChanged;
            InitializeComponent();
            // Set the form's icon from the executable's embedded icon.
            if (PlatformUtils.IsWindows)
            {
                this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "icon.png");
                if (File.Exists(iconPath))
                {
                    using (var bmp = new Bitmap(iconPath)) { this.Icon = Icon.FromHandle(bmp.GetHicon()); }
                }
            }

            ThemeManager.ApplyTheme(this);
            this.Load += async (s, e) => await PopulateDataAsync();
        }

        private void InitializeComponent()
        {
            // Initialize controls
            this.picModImage = new PictureBox();
            this.lblModName = new Label();
            this.lblAuthor = new Label();
            this.lnkProfileUrl = new LinkLabel();
            this.lblLikeCount = new Label();
            this.webDescription = new WebBrowser(); 
            this.btnDownload = new Button();
            this.lstFiles = new ListBox();
            this.prgDownload = new ProgressBar();
            this.lblProgress = new Label();

            // Main layout panel
            this.mainLayout = new TableLayoutPanel();
            mainLayout.ColumnCount = 2;
            mainLayout.RowCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F)); // Details on left
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F)); // Download on right
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.BackColor = Color.FromArgb(45, 45, 48);

            // Create a dedicated panel for the WebBrowser control
            Panel pnlWebContainer = new Panel();
            pnlWebContainer.Dock = DockStyle.Fill;
            pnlWebContainer.BorderStyle = BorderStyle.None;

            // Left panel for mod details
            Panel pnlDetails = new Panel();
            pnlDetails.Dock = DockStyle.Fill;
            pnlDetails.Padding = new Padding(10);
            pnlDetails.AutoScroll = true;
            pnlDetails.BackColor = Color.FromArgb(37, 37, 38);
            pnlDetails.Controls.Add(pnlWebContainer); // Add web container to details panel

            // Right panel for download options
            Panel pnlDownloadOptions = new Panel();
            pnlDownloadOptions.Dock = DockStyle.Fill;
            pnlDownloadOptions.Padding = new Padding(10);
            pnlDownloadOptions.BackColor = Color.FromArgb(45, 45, 48);

            // picModImage
            this.picModImage.Dock = DockStyle.Top;
            this.picModImage.SizeMode = PictureBoxSizeMode.Zoom;
            this.picModImage.Height = 200; // Fixed height for image
            this.picModImage.BackColor = Color.Black;

            // lblModName
            this.lblModName.Dock = DockStyle.Top;
            this.lblModName.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblModName.ForeColor = Color.White;
            this.lblModName.Padding = new Padding(0, 10, 0, 5);
            this.lblModName.AutoSize = false; // Allow text to wrap
            this.lblModName.Height = 60; // Give it some height for wrapping

            // lblAuthor
            this.lblAuthor.Dock = DockStyle.Top;
            this.lblAuthor.Font = new Font("Segoe UI", 10F);
            this.lblAuthor.ForeColor = Color.LightGray;
            this.lblAuthor.Padding = new Padding(0, 0, 0, 5);
            this.lblAuthor.AutoSize = true;

            // lnkProfileUrl
            this.lnkProfileUrl.Dock = DockStyle.Top;
            this.lnkProfileUrl.Font = new Font("Segoe UI", 9F);
            this.lnkProfileUrl.ForeColor = Color.FromArgb(0, 122, 204);
            this.lnkProfileUrl.LinkColor = Color.FromArgb(0, 122, 204);
            this.lnkProfileUrl.Padding = new Padding(0, 0, 0, 10);
            this.lnkProfileUrl.AutoSize = true;
            this.lnkProfileUrl.LinkClicked += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_mod.ProfileUrl) { UseShellExecute = true });

            // lblLikeCount
            this.lblLikeCount.Dock = DockStyle.Top;
            this.lblLikeCount.Font = new Font("Segoe UI", 9F);
            this.lblLikeCount.ForeColor = Color.LightGray;
            this.lblLikeCount.Padding = new Padding(0, 0, 0, 10);
            this.lblLikeCount.AutoSize = true;

            // webDescription
            this.webDescription.Dock = DockStyle.Fill;
            this.webDescription.MinimumSize = new System.Drawing.Size(20, 20);
            this.webDescription.IsWebBrowserContextMenuEnabled = false;
            this.webDescription.WebBrowserShortcutsEnabled = false;
            this.webDescription.ScriptErrorsSuppressed = true;
            this.webDescription.Navigating += (s, e) =>
            {
                if (s is WebBrowser wb)
                {
                    e.Cancel = true;
                    var url = e.Url?.ToString();
                    if (!string.IsNullOrEmpty(url) && url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                        }
                        catch { }
                    }
                }
            };

            // lstFiles
            this.lstFiles.Dock = DockStyle.Fill;
            this.lstFiles.BackColor = Color.FromArgb(37, 37, 38);
            this.lstFiles.ForeColor = Color.White;
            this.lstFiles.BorderStyle = BorderStyle.FixedSingle;
            this.lstFiles.IntegralHeight = false;

            // lblProgress
            this.lblProgress.Dock = DockStyle.Bottom;
            this.lblProgress.Height = 20;
            this.lblProgress.TextAlign = ContentAlignment.MiddleCenter;
            this.lblProgress.Visible = false;
            this.lblProgress.ForeColor = Color.White;

            // prgDownload
            this.prgDownload.Dock = DockStyle.Bottom;
            this.prgDownload.Height = 10;
            this.prgDownload.Visible = false;
            this.prgDownload.Style = ProgressBarStyle.Continuous;


            // btnDownload
            this.btnDownload.Dock = DockStyle.Bottom;
            this.btnDownload.Text = "Download Selected File";
            this.btnDownload.Height = 40;
            this.btnDownload.FlatStyle = FlatStyle.Flat;
            this.btnDownload.BackColor = Color.FromArgb(0, 122, 204);
            this.btnDownload.FlatAppearance.BorderSize = 0;
            this.btnDownload.ForeColor = Color.White;
            this.btnDownload.Click += btnDownload_Click;
            
            // Add controls to panels
            pnlWebContainer.Controls.Add(this.webDescription); // Add browser to its container
            pnlDetails.Controls.Add(this.lnkProfileUrl);
            pnlDetails.Controls.Add(this.lblAuthor);
            pnlDetails.Controls.Add(this.lblLikeCount);
            pnlDetails.Controls.Add(this.lblModName);
            pnlDetails.Controls.Add(this.picModImage);

            pnlDownloadOptions.Controls.Add(this.lstFiles);
            pnlDownloadOptions.Controls.Add(this.prgDownload);
            pnlDownloadOptions.Controls.Add(this.lblProgress);
            pnlDownloadOptions.Controls.Add(this.btnDownload);

            // Add panels to main layout
            mainLayout.Controls.Add(pnlDetails, 0, 0);
            mainLayout.Controls.Add(pnlDownloadOptions, 1, 0);

            // Form settings
            this.Controls.Add(mainLayout);
            this.Text = "Mod Details: " + _mod.Name;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            this.ClientSize = new Size(800, 600);
            this.MinimumSize = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
        }

        public void SetConfirmationMode(GameBananaFile? fileToInstall = null)
        {
            _isConfirmationMode = true;
            this.Text = "Confirm Mod Installation";
            btnDownload.Text = "Install";
            btnDownload.DialogResult = DialogResult.OK; // Will be handled by click event

            // Undock the original button so we can place it in a new panel
            btnDownload.Dock = DockStyle.None;
            btnDownload.Anchor = AnchorStyles.Right;
            btnDownload.Width = 100;

            // Add a cancel button
            var btnCancel = new Button
            {
                Name = "btnCancel",
                Text = "Cancel",
                Anchor = AnchorStyles.Right,
                Width = 100,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            // Use a FlowLayoutPanel to keep buttons next to each other at the bottom right
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 50,
                Padding = new Padding(0, 5, 0, 5)
            };

            // Capture the original parent before changing it.
            var originalParent = btnDownload.Parent;
            Panel? parentPanel = null;

            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnDownload);

            // Add the new button panel to the parent of the original download button
            if (originalParent is Panel pp)
            {
                parentPanel = pp;
                parentPanel.Controls.Add(buttonPanel);
                // Bring the button panel to the front to ensure it's at the very bottom
                buttonPanel.BringToFront();
            }

            // For 1-Click, we already know the file, so we don't need to show the list.
            // Instead, show the file name being installed.
            if (fileToInstall != null)
            {
                // Replace the listbox with a label showing the file being installed
                lstFiles.Parent?.Controls.Remove(lstFiles);
                lstFiles.Dispose();

                var lblFileName = new Label
                {
                    Text = $"Installing: {fileToInstall.FileName}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                    BackColor = Color.FromArgb(45, 45, 48),
                    Padding = new Padding(10)
                };
                if (parentPanel != null)
                {
                    // Insert before the button panel so it takes the remaining space
                    parentPanel.Controls.Add(lblFileName);
                    lblFileName.BringToFront();
                }
            }
            else
            {
                // For 1-Click, we already know the file, so we don't need to show the list.
                // Remove the listbox from its parent so it no longer takes up space.
                // The button panel will now be the only thing at the bottom of the right panel.
                lstFiles.Parent?.Controls.Remove(lstFiles);
                lstFiles.Dispose();
            }
        }

        private void ShowProgressView()
        {
            // Hide the main details and file list panels
            mainLayout.ColumnStyles[0].Width = 0;
            mainLayout.ColumnStyles[1].Width = 100;
            mainLayout.Controls[0].Visible = false; // pnlDetails

            // In the right panel, hide everything except the progress indicators
            lstFiles.Visible = false;
            btnDownload.Visible = false;
            // The FlowLayoutPanel with the buttons is a child of btnDownload's original parent
            if (btnDownload.Parent is FlowLayoutPanel buttonPanel)
            {
                buttonPanel.Visible = false;
            }
        }

        private void ShowCompletionView()
        {
            // This is called after a successful 1-Click install.
            // It reconfigures the progress view to show a final "OK" button.

            prgDownload.Visible = false;
            lblProgress.Text = "Installation Complete!";
            lblProgress.Font = new Font(lblProgress.Font, FontStyle.Bold);

            // Create a final OK button to close the form.
            var btnOk = new Button
            {
                Name = "btnOk",
                Text = "OK",
                DialogResult = DialogResult.OK,
                Height = 40,
                Dock = DockStyle.Bottom,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White
            };
            btnOk.FlatAppearance.BorderSize = 0;

            if (prgDownload.Parent != null)
            {
                prgDownload.Parent.Controls.Add(btnOk);
            }
        }
        private async Task PopulateDataAsync()
        {
            if (this.IsDisposed || webDescription.IsDisposed) return;

            lblModName.Text = _mod.Name;
            lblAuthor.Text = $"by {_mod.Author}";
            lnkProfileUrl.Text = _mod.ProfileUrl;
            lblLikeCount.Text = $"Likes: {_mod.LikeCount:N0}";
            webDescription.DocumentText = "<body style='background-color:#252526; color:white; font-family:sans-serif;'>Loading description...</body>";

            if (!string.IsNullOrEmpty(_mod.ThumbnailUrl))
            {
                picModImage.LoadAsync(_mod.ThumbnailUrl);
            }

            // Asynchronously load description and file list
            try
            {
                _logger?.Report($"Fetching details and files for mod '{_mod.Name}' (ID: {_mod.Id})...");

                // Run API calls in parallel for faster loading
                var detailsTask = GameBananaApiService.GetModDetailsAsync(_mod);
                var downloadPageTask = GameBananaApiService.GetModDownloadPageAsync(_mod);
                await Task.WhenAll(detailsTask, downloadPageTask);

                var modProfile = await detailsTask;
                var downloadPage = await downloadPageTask;

                if (this.IsDisposed) return;

                if (webDescription.IsDisposed) return;

                // Populate description
                if (modProfile?.Description != null && !string.IsNullOrEmpty(modProfile.Description))
                {
                    _mod.Name = modProfile.Name; // Update name from profile
                    lblLikeCount.Text = $"Likes: {modProfile.LikeCount:N0}"; // Update with more accurate count from profile if available
                    _logger?.Report($"Successfully fetched details. Description length: {modProfile.Description.Length} characters.");
                    // Inject some basic CSS to make the HTML content match the dark theme.
                    string styledHtml = "<style>body { background-color: #252526; color: white; font-family: sans-serif; } a { color: #569CD6; }</style>" + modProfile.Description;
                    webDescription.DocumentText = styledHtml;
                }
                else
                {
                    _logger?.Report("Received a valid response, but it contained no description.");
                    webDescription.DocumentText = "<body style='background-color:#252526; color:white; font-family:sans-serif;'>No description available for this mod.</body>";
                }

                if (this.IsDisposed || lstFiles.IsDisposed) return;

                // Populate file list
                if (downloadPage?.Files != null && downloadPage.Files.Any())
                {
                    lstFiles.Items.AddRange(downloadPage.Files.ToArray());
                    lstFiles.SelectedIndex = 0; // Select the first file by default
                    _logger?.Report($"Found {downloadPage.Files.Count} file(s).");
                }
                else
                {
                    _logger?.Report("No downloadable files found for this mod.");
                    btnDownload.Enabled = false;
                    btnDownload.Text = "No Files Found";
                }
            }
            catch (Exception ex)
            {
                if (this.IsDisposed) return;
                _logger?.Report($"ERROR fetching mod details: {ex.Message}");
                webDescription.DocumentText = $"<body style='background-color:#252526; color:white; font-family:sans-serif;'>Failed to load description. See debug log for details.</body>";
                btnDownload.Enabled = false;
                btnDownload.Text = "Error Loading Files";
            }
        }

        private async void btnDownload_Click(object? sender, EventArgs e)
        {
            if (_isConfirmationMode) return;

            if (lstFiles.SelectedItem is not GameBananaFile selectedFile)
            {
                CustomMessageBox.Show("Please select a file to download.", "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // For regular downloads from the browser, we launch the progress form directly.
            await LaunchProgressForm(selectedFile);
            this.DialogResult = DialogResult.OK; // Mark as successful to close the details form.
            this.Close();
        }

        public GameBananaFile? GetSelectedFile()
        {
            return lstFiles.SelectedItem as GameBananaFile;
        }

        private async Task RunFullInstallProcessAsync(GameBananaFile selectedFile, ProgressForm progressForm)
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrossworldsModManager", "Downloads");
            Directory.CreateDirectory(appDataPath);
            var downloadedFilePath = Path.Combine(appDataPath, selectedFile.FileName);

            try
            {
                // Backup mods before performing download/install (unless user disabled automatic backups)
                try
                {
                    var modsDirectory = SettingsManager.Settings.ModsDirectory;
                    if (!SettingsManager.Settings.DoNotBackupModsAutomatically && !string.IsNullOrWhiteSpace(modsDirectory) && Directory.Exists(modsDirectory))
                    {
                        ModBackupManager.BackupMods(modsDirectory);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Report($"WARNING: Failed to create backup before install: {ex.Message}");
                }

                await DownloadFileAsync(selectedFile, downloadedFilePath, progressForm);
                string installedPath = await ExtractAndInstallAsync(downloadedFilePath, selectedFile.FileName, progressForm);

                // Check if a thumbnail already exists
                bool thumbnailExists = false;
                if (!string.IsNullOrEmpty(installedPath))
                {
                    string[] extensions = { ".jpg", ".png", ".jpeg", ".bmp", ".gif" };
                    foreach (var ext in extensions)
                    {
                        if (File.Exists(Path.Combine(installedPath, "Thumb" + ext)))
                        {
                            thumbnailExists = true;
                            break;
                        }
                    }
                }

                // Download thumbnail if available and one doesn't already exist
                if (!thumbnailExists && !string.IsNullOrEmpty(_mod.ThumbnailUrl) && !string.IsNullOrEmpty(installedPath))
                {
                    try
                    {
                        progressForm.UpdateStatus("Downloading thumbnail...");
                        using (var client = new HttpClient())
                        {
                            var thumbBytes = await client.GetByteArrayAsync(_mod.ThumbnailUrl);
                            string ext = Path.GetExtension(_mod.ThumbnailUrl);
                            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                            string thumbPath = Path.Combine(installedPath, "Thumb" + ext);
                            await File.WriteAllBytesAsync(thumbPath, thumbBytes);
                        }
                    }
                    catch (Exception ex) { _logger?.Report($"Warning: Failed to download thumbnail: {ex.Message}"); }
                }

                _onModsChanged?.Invoke(); // Trigger a refresh on the main form
                progressForm.ShowCompletion("Installation Complete!");
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to download or install mod: {ex.Message}";
                _logger?.Report($"ERROR: {errorMsg}");
                progressForm.ShowCompletion($"Error: {ex.Message}");
            }
            finally
            {
                // Clean up the downloaded file
                if (File.Exists(downloadedFilePath))
                {
                    try { File.Delete(downloadedFilePath); }
                    catch (Exception ex) { _logger?.Report($"Could not clean up temporary file '{downloadedFilePath}': {ex.Message}"); }
                }
            }
        }

        public Task LaunchProgressForm(GameBananaFile fileToInstall)
        {
            using (var progressForm = new ProgressForm($"Installing '{_mod.Name}'..."))
            {
                // Run the installation process asynchronously while the form is shown modally.
                progressForm.Shown += async (s, e) => await RunFullInstallProcessAsync(fileToInstall, progressForm);
                progressForm.ShowDialog(this);
            }
            return Task.CompletedTask;
        }

        private async Task DownloadFileAsync(GameBananaFile selectedFile, string destinationPath, ProgressForm progressForm)
        {
            _logger?.Report($"Starting download: {selectedFile.DownloadUrl}");
            progressForm.UpdateStatus("Downloading...");

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(selectedFile.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var totalBytesRead = 0L;

                using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        var buffer = new byte[81920];
                        int bytesRead;
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fs.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            if (totalBytes != -1)
                            {
                                var progressPercentage = (int)((totalBytesRead * 100) / totalBytes);
                                progressForm.UpdateProgress(progressPercentage);
                                progressForm.UpdateStatus($"Downloading... {progressPercentage}%");
                            }
                        }
                    }
                }
            }
            _logger?.Report($"Download complete: {destinationPath}");
        }

        private async Task<string> ExtractAndInstallAsync(string archivePath, string fileName, ProgressForm progressForm)
        {
            var modsDirectory = SettingsManager.Settings.ModsDirectory;
            if (string.IsNullOrEmpty(modsDirectory)) throw new InvalidOperationException("Mods directory is not set.");

            progressForm.UpdateStatus("Extracting...");
            progressForm.UpdateProgress(100); // Keep bar full during extraction

            // Use the mod's GameBanana name for the folder, sanitizing it for file system compatibility.
            string modName = SanitizeFolderName(_mod.Name);

            // CRITICAL SAFETY CHECK: Ensure the sanitized mod name is not empty or just whitespace.
            // If it is, we must abort to prevent deleting the root mods directory.
            if (string.IsNullOrWhiteSpace(modName))
            {
                throw new InvalidOperationException("Mod name is invalid or empty, cannot create a folder for it. Aborting installation to prevent data loss.");
            }

            // Append the zip file name (without extension) to distinguish different files of the same mod,
            // preventing overwrites when a mod offers multiple variants.
            string fileVariant = SanitizeFolderName(Path.GetFileNameWithoutExtension(fileName));
            if (!string.IsNullOrWhiteSpace(fileVariant))
            {
                modName = $"{modName} - {fileVariant}";
            }

            string targetDir = Path.Combine(modsDirectory, modName);

            // Preserve any existing mod.ini (if present) before deleting old files.
            string? preservedIniContent = null;
            string? preservedIniRelativePath = null;

            if (Directory.Exists(targetDir))
            {
                try
                {
                    var existingIni = Directory.GetFiles(targetDir, "mod.ini", SearchOption.AllDirectories).FirstOrDefault();
                    if (existingIni != null)
                    {
                        preservedIniContent = File.ReadAllText(existingIni);
                        preservedIniRelativePath = Path.GetRelativePath(targetDir, existingIni);
                    }

                    // Delete old contents of the folder but keep the root directory itself.
                    DeleteDirectoryContents(targetDir);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to clear existing mod folder '{targetDir}': {ex.Message}");
                }
            }

            Directory.CreateDirectory(targetDir);

            _logger?.Report($"Extracting {Path.GetFileName(archivePath)} using SharpCompress...");
            await Task.Run(() =>
            {
                using var archive = ArchiveFactory.OpenArchive(archivePath);
                archive.WriteToDirectory(targetDir, new SharpCompress.Common.ExtractionOptions { ExtractFullPath = true, Overwrite = true });
            });

            // After extraction, check whether the extracted content already contains a mod.ini (possibly nested).
            var extractedIni = Directory.GetFiles(targetDir, "mod.ini", SearchOption.AllDirectories).FirstOrDefault();

            if (extractedIni == null && preservedIniContent != null && preservedIniRelativePath != null)
            {
                // Restore preserved mod.ini into the same relative location inside the extracted folder.
                try
                {
                    var restorePath = Path.Combine(targetDir, preservedIniRelativePath);
                    var restoreDir = Path.GetDirectoryName(restorePath);
                    if (!string.IsNullOrEmpty(restoreDir) && !Directory.Exists(restoreDir)) Directory.CreateDirectory(restoreDir);
                    File.WriteAllText(restorePath, preservedIniContent);
                    extractedIni = restorePath;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to restore preserved mod.ini for '{targetDir}': {ex.Message}");
                }
            }

            // Determine the directory to update/create mod.ini in: prefer the directory containing an existing mod.ini, otherwise use targetDir
            string iniTargetDir = targetDir;
            if (!string.IsNullOrEmpty(extractedIni))
            {
                var d = Path.GetDirectoryName(extractedIni);
                if (!string.IsNullOrEmpty(d)) iniTargetDir = d;
            }

            // After extraction and possible restore, create or update the mod.ini file with GameBanana info.
            await CreateOrUpdateModIniAsync(iniTargetDir);
            
            return iniTargetDir;
        }

        private void DeleteDirectoryContents(string dir)
        {
            var directory = new DirectoryInfo(dir);
            foreach (var file in directory.GetFiles())
            {
                try { file.IsReadOnly = false; file.Delete(); } catch { }
            }
            foreach (var sub in directory.GetDirectories())
            {
                try { sub.Delete(true); } catch { }
            }
        }

        private async Task CreateOrUpdateModIniAsync(string modDirectory)
        {
            var iniPath = Path.Combine(modDirectory, "mod.ini");
            Dictionary<string, Dictionary<string, string>> iniData;

            if (File.Exists(iniPath))
            {
                iniData = IniParser.Parse(iniPath);
            }
            else
            {
                // mod.ini doesn't exist. Check for legacy config before creating a new one.
                if (!ConvertLegacyConfig(modDirectory, out iniData))
                {
                    // Not a legacy mod, create a blank ini structure.
                    iniData = new Dictionary<string, Dictionary<string, string>>();
                }
            }

            // Ensure [Main] section exists
            if (!iniData.ContainsKey("Main"))
            {
                iniData["Main"] = new Dictionary<string, string>();
            }

            // Ensure [GameBanana] section exists
            if (!iniData.ContainsKey("GameBanana"))
            {
                iniData["GameBanana"] = new Dictionary<string, string>();
            }

            // Add GameBanana metadata
            iniData["GameBanana"]["ItemId"] = _mod.Id.ToString();
            iniData["GameBanana"]["ItemType"] = _mod.ModelName;

            // Fetch the latest GameBanana version counter and store it separately as GBVersion
            try
            {
                var latestVersion = await GameBananaApiService.GetLatestModUpdateCountAsync(_mod.ModelName, _mod.Id);
                if (!string.IsNullOrWhiteSpace(latestVersion))
                {
                    iniData["GameBanana"]["GBVersion"] = latestVersion;
                }
                else if (!iniData["GameBanana"].ContainsKey("GBVersion"))
                {
                    iniData["GameBanana"]["GBVersion"] = "0";
                }
            }
            catch
            {
                if (!iniData["GameBanana"].ContainsKey("GBVersion"))
                {
                    iniData["GameBanana"]["GBVersion"] = "0";
                }
            };

            // Set raw version in x.y.z...n format
            try
            {
                var latestRawVersion = await GameBananaApiService.GetLatestModVersionAsync(_mod.ModelName, _mod.Id);
                if (!string.IsNullOrWhiteSpace(latestRawVersion))
                {
                    iniData["Main"]["Version"] = latestRawVersion;
                }
                else if (!iniData["Main"].ContainsKey("Version"))
                {
                    iniData["Main"]["Version"] = "0.0";
                }
            }
            catch
            {
                if (!iniData["Main"].ContainsKey("Version"))
                {
                    iniData["Main"]["Version"] = "0.0";
                }
            };

            // Set the author in [Main] to the GameBanana mod author for installs
            // originating from the browser or 1-Click flow.
            if (!string.IsNullOrWhiteSpace(_mod.Author))
            {
                iniData["Main"]["Author"] = _mod.Author;
            }

            // Write the updated data back to the file.
            IniParser.Write(iniPath, iniData);
        }

        private bool ConvertLegacyConfig(string modDir, out Dictionary<string, Dictionary<string, string>> iniData)
        {
            iniData = new Dictionary<string, Dictionary<string, string>>();
            var descFiles = Directory.GetFiles(modDir, "desc.ini", SearchOption.AllDirectories);
            if (descFiles.Length == 0) return false;

            // [Main]
            iniData["Main"] = new Dictionary<string, string>
            {
                ["Name"] = Path.GetFileName(modDir),
                ["Author"] = "Unknown",
                ["Version"] = "1.0",
                ["Description"] = "Auto-generated from legacy configuration."
            };

            // GroupName -> List of Options
            var groups = new Dictionary<string, List<string>>();
            // FilePath -> GroupName.OptionName
            var fileMappings = new Dictionary<string, string>();

            foreach (var descFile in descFiles)
            {
                var optionDir = Path.GetDirectoryName(descFile);
                if (optionDir == null) continue;

                string relativePath = Path.GetRelativePath(modDir, optionDir);
                if (relativePath == "." || string.IsNullOrEmpty(relativePath)) continue;

                string groupName = "Configuration";
                string groupRelPath = Path.GetDirectoryName(relativePath) ?? "";
                if (!string.IsNullOrEmpty(groupRelPath) && groupRelPath != ".")
                {
                    groupName = groupRelPath.Replace(Path.DirectorySeparatorChar, ' ').Replace(Path.AltDirectorySeparatorChar, ' ');
                }
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
                optionName = optionName.Replace(",", "");

                if (!groups.ContainsKey(groupName))
                {
                    groups[groupName] = new List<string>();
                }

                string uniqueOptionName = optionName;
                if (groups[groupName].Contains(uniqueOptionName))
                {
                    int i = 2;
                    while (groups[groupName].Contains($"{uniqueOptionName} {i}")) i++;
                    uniqueOptionName = $"{uniqueOptionName} {i}";
                }
                groups[groupName].Add(uniqueOptionName);

                var files = Directory.GetFiles(optionDir);
                foreach (var file in files)
                {
                    if (Path.GetFileName(file).Equals("desc.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    
                    string fileRelPath = Path.GetRelativePath(modDir, file);
                    fileMappings[fileRelPath] = $"{groupName}.{uniqueOptionName}";
                }
            }

            // Create Config sections
            foreach (var group in groups)
            {
                iniData[$"Config:{group.Key}"] = new Dictionary<string, string>
                {
                    ["Type"] = "SelectOne",
                    ["Description"] = $"Select {group.Key}:",
                    ["Options"] = string.Join(", ", group.Value)
                };
            }

            // [Files]
            if (fileMappings.Any())
            {
                iniData["Files"] = fileMappings;
            }
            
            return true;
        }
        
        private static string SanitizeFolderName(string name)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalidChars)
            {
                name = name.Replace(c, '_');
            }
            // Trim any leading/trailing spaces or dots that might cause issues
            return name.Trim(' ', '.');
        }
    }
#pragma warning restore CA1416
}
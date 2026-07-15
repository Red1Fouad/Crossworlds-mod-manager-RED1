using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CrossworldsModManager
{
    static class Program
    {
        // Current application version.
        public const string AppVersion = "1.1.5";

        // Unique GUID for the application to identify the mutex and messages.
        private const string AppGuid = "c1a2b3d4-e5f6-7890-1234-567890abcdef"; // Please generate a new GUID for your app
        private const string ProtocolName = "bluestar";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [STAThread]
        static void Main(string[] args)
        {
            string? oneClickUrl = args.Length > 0 && args[0].StartsWith($"{ProtocolName}:", StringComparison.OrdinalIgnoreCase)
                ? args[0]
                : null;

            bool isFirstInstance = true;
            var currentProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(currentProcess.ProcessName);

            foreach (var p in processes)
            {
                if (p.Id != currentProcess.Id)
                {
                    isFirstInstance = false;
                    break;
                }
            }

            if (isFirstInstance)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if (PlatformUtils.IsWindows)
                {
                    RegisterProtocol();
                }
                else if (PlatformUtils.IsAppImage &&
                         Environment.GetEnvironmentVariable("BLUESTAR_DISABLE_PROTOCOL_REGISTER") == null &&
                         Environment.GetEnvironmentVariable("DESKTOPINTEGRATION") != "1")
                {
                    HandleAppImageIntegration();
                }

                try
                {
                    Application.Run(new MainForm(oneClickUrl, AppVersion));
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"A fatal error occurred:\n{ex.Message}\n\n{ex.StackTrace}",
                        "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (oneClickUrl != null)
            {
                SendUrlToExistingInstance(oneClickUrl, currentProcess);
            }
        }

        private static void HandleAppImageIntegration()
        {
            string applicationsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "applications");
            string desktopFileName = "com.bluestar.manager.desktop";
            string desktopFilePath = Path.Combine(applicationsPath, desktopFileName);
            string curAppImagePath = Environment.GetEnvironmentVariable("APPIMAGE")!;

            if (File.Exists(desktopFilePath))
            {
                var lines = File.ReadAllLines(desktopFilePath);
                bool pathChanged = lines.Any(l => l.StartsWith("Exec=") && !l.Contains(curAppImagePath));

                if (pathChanged)
                {
                    var result = CustomMessageBox.Show(
                        "The AppImage's location seems to have been moved from where it was last executed.\nWould you like to update the .desktop file to point to the current location of the AppImage?",
                        "Linux Integration",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                        UpdateLinuxDesktopFile(curAppImagePath);
                }
            }
            else
            {
                RegisterProtocolLinux();
            }
        }

        private static void SendUrlToExistingInstance(string url, Process currentProcess)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", "CrossworldsModManagerPipe", PipeDirection.Out))
                {
                    client.Connect(1000);
                    using (var writer = new StreamWriter(client))
                    {
                        writer.Write(url);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to send URL to existing instance: {ex.Message}");
            }

            if (PlatformUtils.IsWindows)
            {
                var otherProcess = Process.GetProcessesByName(currentProcess.ProcessName)
                    .FirstOrDefault(p => p.Id != currentProcess.Id);
                if (otherProcess != null)
                    SetForegroundWindow(otherProcess.MainWindowHandle);
            }
        }

        private static void RegisterProtocol()
        {
            if (!PlatformUtils.IsWindows) { RegisterProtocolLinux(); return; }

            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey($"Software\\Classes\\{ProtocolName}"))
                {
                    if (key == null) return;

                    string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    if (string.IsNullOrEmpty(exePath))
                    {
                        Debug.WriteLine("Could not determine executable path for protocol registration.");
                        return;
                    }

                    key.SetValue("", $"URL:{ProtocolName} Protocol");
                    key.SetValue("URL Protocol", "");

                    using (var commandKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        commandKey?.SetValue("", $"\"{exePath}\" \"%1\"");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to register URL protocol: {ex.Message}");
            }
        }

        private static void RegisterProtocolLinux()
        {
            if (Environment.GetEnvironmentVariable("BLUESTAR_DISABLE_PROTOCOL_REGISTER") != null ||
                Environment.GetEnvironmentVariable("DESKTOPINTEGRATION") == "1")
                return;

            try
            {
                string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                string? exeDirPath = Path.GetDirectoryName(exePath);
                if (string.IsNullOrEmpty(exePath) || string.IsNullOrEmpty(exeDirPath)) return;

                string desktopFileName = "com.bluestar.manager.desktop";
                string applicationsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "applications");
                string iconsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "icons");

                if (!Directory.Exists(applicationsPath))
                    Directory.CreateDirectory(applicationsPath);

                string desktopFilePath = Path.Combine(applicationsPath, desktopFileName);

                if (!File.Exists(desktopFilePath))
                {
                    var result = CustomMessageBox.Show(
                        "Would you like to register the 'bluestar:' URL protocol?\n\n" +
                        "This allows 1-Click downloads from GameBanana to work.\n" +
                        "This will create a .desktop file in ~/.local/share/applications.",
                        "Linux Integration",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result != DialogResult.Yes) return;
                }

                string content;
                if (PlatformUtils.IsAppImage)
                {
                    if (!Directory.Exists(iconsPath))
                        Directory.CreateDirectory(iconsPath);
                    try
                    {
                        File.Copy(Path.Combine(exeDirPath, "Tools", "icon-square.png"), Path.Combine(iconsPath, "bluestar.png"));
                    }
                    catch (IOException) { }

                    content = $"""
                        [Desktop Entry]
                        Type=Application
                        Name=Bluestar Manager
                        Categories=Game;Utility
                        Icon={Path.Combine(iconsPath, "bluestar.png")}
                        Exec="{Environment.GetEnvironmentVariable("APPIMAGE")}" %u
                        Comment=Powerful mod manager for Sonic Racing: CrossWorlds
                        StartupNotify=false
                        MimeType=x-scheme-handler/{ProtocolName};
                        """;
                }
                else
                {
                    content = $"""
                        [Desktop Entry]
                        Type=Application
                        Name=Bluestar Manager
                        Categories=Game;Utility
                        Icon={exeDirPath}/Tools/icon.png
                        Exec="{exePath}" %u
                        Comment=Powerful mod manager for Sonic Racing: CrossWorlds
                        StartupNotify=false
                        MimeType=x-scheme-handler/{ProtocolName};
                        """;
                }

                File.WriteAllText(desktopFilePath, content);

                RunProcess("xdg-mime", $"default {desktopFileName} x-scheme-handler/{ProtocolName}");
                RunProcess("update-desktop-database", applicationsPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to register URL protocol on Linux: {ex.Message}");
            }
        }

        private static void RunProcess(string fileName, string arguments)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit();
            }
            catch { }
        }

        /// <summary>
        /// Checks GitHub for a new release and prompts the user to update if one is found.
        /// </summary>
        public static async void CheckForUpdates()
        {
            try
            {
                string owner = "Red1Fouad";
                string repo = "Crossworlds-mod-manager-RED1";
                string latestVersionTag = "";
                string downloadUrl = string.Empty;
                string fileName = string.Empty;

                using (var client = new HttpClient())
                {
                    // GitHub API requires a User-Agent header.
                    client.DefaultRequestHeaders.Add("User-Agent", "CrossworldsModManager-Update-Check");

                    // Get the latest release information
                    var response = await client.GetStringAsync($"https://api.github.com/repos/{owner}/{repo}/releases/latest");
                    
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        if (doc.RootElement.TryGetProperty("tag_name", out var tagElement))
                        {
                            latestVersionTag = tagElement.GetString() ?? "";
                        }

                        if (doc.RootElement.TryGetProperty("assets", out var assetsElement))
                        {
                            foreach (var asset in assetsElement.EnumerateArray())
                            {
                                string name = asset.GetProperty("name").GetString() ?? "";
                                string url = asset.GetProperty("browser_download_url").GetString() ?? "";

                                var targetExt = PlatformUtils.IsWindows ? ".zip" : ".AppImage";
                    if (name.EndsWith(targetExt, StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = url;
                        fileName = name;
                        break;
                    }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(latestVersionTag) || string.IsNullOrEmpty(downloadUrl))
                {
                    return;
                }

                // Normalize version strings (e.g., "v1.2.3" -> "1.2.3")
                var latestVersion = new Version(latestVersionTag.TrimStart('v'));
                var currentVersion = new Version(AppVersion);

                if (latestVersion > currentVersion)
                {
                    if (PlatformUtils.IsWindows)
                        await HandleWindowsUpdate(latestVersionTag, downloadUrl);
                    else
                        await HandleLinuxUpdate(latestVersionTag, downloadUrl, fileName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking for updates: {ex.Message}");
                // Silently fail, as this is not a critical function.
            }
        }

        private static async Task HandleWindowsUpdate(string latestVersionTag, string downloadUrl)
        {
            var result = CustomMessageBox.Show(
                $"A new version ({latestVersionTag}) is available!\nWould you like to update now?",
                "Update Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result != DialogResult.Yes) return;

            string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.exe");
            if (!File.Exists(updaterPath))
            {
                CustomMessageBox.Show($"Updater executable not found at:\n{updaterPath}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var currentProcess = Process.GetCurrentProcess();
            string? appPath = currentProcess.MainModule?.FileName;

            if (string.IsNullOrEmpty(appPath))
            {
                CustomMessageBox.Show("Could not determine the application path. Update cannot proceed.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string arguments = $"--pid {currentProcess.Id} --appPath \"{appPath}\" --downloadUrl \"{downloadUrl}\"";
            Process.Start(updaterPath, arguments);
            Application.Exit();
        }

        private static async Task HandleLinuxUpdate(string latestVersionTag, string downloadUrl, string fileName)
        {
            if (Environment.GetEnvironmentVariable("BLUESTAR_DISABLE_SELF_UPDATE") != null)
            {
                var result = CustomMessageBox.Show(
                    $"A new version ({latestVersionTag}) is available!\nWould you like to open the Github page?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                    Process.Start("xdg-open", $"https://github.com/Red1Fouad/Crossworlds-mod-manager-RED1/releases/latest");
                return;
            }

            var downloadResult = CustomMessageBox.Show(
                $"A new version ({latestVersionTag}) is available!\nWould you like to download it now?",
                "Update Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            if (downloadResult != DialogResult.Yes) return;

            await DownloadLinuxUpdate(downloadUrl, fileName);
        }

        private static async Task DownloadLinuxUpdate(string downloadUrl, string fileName)
        {
            try
            {
                string? currentAppImage = Environment.GetEnvironmentVariable("APPIMAGE");
                string downloadDir = !string.IsNullOrEmpty(currentAppImage)
                    ? Path.GetDirectoryName(currentAppImage) ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

                string destinationPath = Path.Combine(downloadDir, fileName);

                using (var client = new HttpClient())
                {
                    var data = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(destinationPath, data);
                }

                try { Process.Start("chmod", $"+x \"{destinationPath}\"")?.WaitForExit(); }
                catch { }

                UpdateLinuxDesktopFile(destinationPath);

                CustomMessageBox.Show($"Update downloaded to:\n{destinationPath}\n\nThe application shortcut has been updated.\nPlease restart the application.", "Update Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to update: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void UpdateLinuxDesktopFile(string newAppImagePath)
        {
            try
            {
                string applicationsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "applications");
                string desktopFilePath = Path.Combine(applicationsPath, "com.bluestar.manager.desktop");

                if (!File.Exists(desktopFilePath)) return;

                var lines = File.ReadAllLines(desktopFilePath);
                bool changed = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("Exec=") && !lines[i].Contains(newAppImagePath))
                    {
                        lines[i] = $"Exec=\"{newAppImagePath}\" %u";
                        changed = true;
                    }
                }
                if (changed)
                {
                    File.WriteAllLines(desktopFilePath, lines);
                    RunProcess("update-desktop-database", applicationsPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update desktop file: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks for a mod.ini in all subdirectories of the given mod path.
        /// If found, it makes that directory the root of the mod.
        /// </summary>
        /// <param name="modPath">The current root directory of the mod.</param>
        public static void CheckAndSetModRoot(string modPath)
        {
            try
            {
                if (!Directory.Exists(modPath)) return;

                // Search for mod.ini in subdirectories
                var iniFiles = Directory.GetFiles(modPath, "mod.ini", SearchOption.AllDirectories);

                if (iniFiles.Length > 0)
                {
                    string? foundIniPath = null;
                    string fullModPath = Path.GetFullPath(modPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    // Find the first mod.ini that is NOT in the root directory
                    foreach (var file in iniFiles)
                    {
                        var dir = Path.GetDirectoryName(file);
                        if (dir == null) continue;
                        var fullDir = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                        if (!fullDir.Equals(fullModPath, StringComparison.OrdinalIgnoreCase))
                        {
                            foundIniPath = file;
                            break;
                        }
                    }

                    if (foundIniPath == null) return;

                    string? newRoot = Path.GetDirectoryName(foundIniPath);

                    // If the found path is somehow the same as modPath, return
                    if (string.IsNullOrEmpty(newRoot) || newRoot.Equals(modPath, StringComparison.OrdinalIgnoreCase)) return;

                    Debug.WriteLine($"Found mod.ini in subdirectory: {newRoot}. Promoting to root.");
                    
                    // Safety check: Ensure newRoot is actually inside modPath
                    if (!Path.GetFullPath(newRoot).StartsWith(fullModPath, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Safety check failed: {newRoot} is not inside {modPath}");
                        return;
                    }

                    // Move the new root to a temporary location outside the mods directory
                    // to avoid performing destructive operations inside the user's mods folder.
                    string tempRoot = Path.Combine(Path.GetTempPath(), "CrossworldsModManager", Guid.NewGuid().ToString());
                    try
                    {
                        Directory.CreateDirectory(tempRoot);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to create temp root '{tempRoot}': {ex.Message}");
                        return;
                    }

                    string tempPath = Path.Combine(tempRoot, Path.GetFileName(newRoot) ?? ("mod_temp_" + Guid.NewGuid().ToString()));

                    // Try to move the new root into the temp area; if that fails (cross-volume),
                    // fall back to copying then deleting the source.
                    try
                    {
                        SafeMoveDirectory(newRoot, tempPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to move or copy new root to temp: {ex.Message}");
                        Log($"Failed to move or copy new root '{newRoot}' to temp '{tempPath}': {ex.Message}");
                        return; // Abort
                    }

                    // Instead of moving the original folder to a sibling trash inside the mods folder,
                    // move it to the temp root outside the mods directory. This prevents accidental
                    // deletion of unrelated folders inside the mods directory.
                    string trashPath = Path.Combine(tempRoot, Path.GetFileName(modPath) + "_trash_" + Guid.NewGuid().ToString());
                    bool trashCreated = false;

                    try
                    {
                        SafeMoveDirectory(modPath, trashPath);
                        trashCreated = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to move original folder to trash: {ex.Message}. Reverting.");
                        Log($"Failed to move or copy original '{modPath}' to trash '{trashPath}': {ex.Message}");
                        try { SafeMoveDirectory(tempPath, newRoot); } catch { }
                        return;
                    }

                    // Rename the temp folder to the original mod path name
                    try
                    {
                        SafeMoveDirectory(tempPath, modPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to rename temp folder to mod path: {ex.Message}. Reverting.");
                        Log($"Failed to move or copy temp '{tempPath}' to mod path '{modPath}': {ex.Message}");
                        // Restore from trash
                        if (trashCreated && Directory.Exists(trashPath))
                        {
                            try { SafeMoveDirectory(trashPath, modPath); SafeMoveDirectory(tempPath, newRoot); } catch { }
                        }
                        return;
                    }

                    // Success! Do NOT automatically delete the trash folder. Retain it in the system temp
                    // for manual inspection/cleanup to avoid accidental mass deletion.
                    Debug.WriteLine($"Mod root normalized. Original folder moved to: {trashPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error normalizing mod directory: {ex.Message}");
            }
        }

        // Log file path for operations that may affect user data.
        private static readonly string OperationLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mod_ops.log");

        private static void Log(string message)
        {
            try
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(OperationLogPath, line);
                Debug.WriteLine(message);
            }
            catch
            {
                // Best effort only; don't let logging interfere with operations.
            }
        }

        // Attempts to move a directory. If a simple move fails (e.g., across volumes),
        // falls back to a recursive copy followed by deletion of the source.
        private static void SafeMoveDirectory(string sourceDir, string destDir)
        {
            if (string.Equals(Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(destDir).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                return; // same path

            try
            {
                // Try a standard move first (fast, preserves metadata when possible).
                Directory.Move(sourceDir, destDir);
                Log($"Moved directory '{sourceDir}' -> '{destDir}'");
                return;
            }
            catch (IOException)
            {
                // Fall through to copy fallback.
            }
            catch (UnauthorizedAccessException)
            {
                // Fall through to copy fallback in case of permission oddities.
            }

            // Copy fallback
            CopyDirectory(sourceDir, destDir);

            // Verify destination exists, then remove source.
            if (Directory.Exists(destDir))
            {
                try
                {
                    Directory.Delete(sourceDir, true);
                    Log($"Copied directory '{sourceDir}' -> '{destDir}' and deleted source.");
                }
                catch (Exception ex)
                {
                    Log($"Copied directory '{sourceDir}' -> '{destDir}', but failed to delete source: {ex.Message}");
                    throw;
                }
            }
            else
            {
                throw new IOException($"Destination directory '{destDir}' does not exist after copy fallback.");
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            var sourceInfo = new DirectoryInfo(sourceDir);
            if (!sourceInfo.Exists) throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

            Directory.CreateDirectory(destDir);

            // Copy files
            foreach (var file in sourceInfo.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                var destFile = Path.Combine(destDir, file.Name);
                file.CopyTo(destFile, true);
            }

            // Recursively copy subdirectories
            foreach (var dir in sourceInfo.GetDirectories())
            {
                var destSubDir = Path.Combine(destDir, dir.Name);
                CopyDirectory(dir.FullName, destSubDir);
            }
        }
    }
}

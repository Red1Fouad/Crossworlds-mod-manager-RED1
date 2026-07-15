using System.Collections.Generic;
using System.Drawing;

namespace CrossworldsModManager
{
    public class ModProfile
    {
        public List<string> EnabledMods { get; set; } = new();
        public Dictionary<string, string> ModConfigurations { get; set; } = new();
        public List<string> ModLoadOrder { get; set; } = new();
    }

    public class AppSettings
    {
        public string? GameDirectory { get; set; }
        public string? GameExecutableName { get; set; }
        public string? ModsDirectory { get; set; }
        public string? PreferredLaunchPlatform { get; set; }

        public Dictionary<string, ModProfile> Profiles { get; set; } = new();
        public string? ActiveProfileName { get; set; }
        public Dictionary<string, List<string>> ModGroups { get; set; } = new();
        public string SelectedTheme { get; set; } = "Default";
        public SerializableTheme CustomTheme { get; set; } = new SerializableTheme();

        public bool SortEnabledModsToTop { get; set; } = true;
        public bool AutoCleanTemporaryFiles { get; set; } = true;
        public bool CheckForGamesOnStartup { get; set; } = true;
        public bool AutoCloseLogOnSuccess { get; set; } = false;

        // If true, the app will NOT create automatic backups before operations.
        // Default is false (automatic backups enabled).
        public bool DoNotBackupModsAutomatically { get; set; } = true;

        // If true, the app will NOT show confirmation when enabling/disabling all mods.
        // Default is false (show confirmation).
        public bool DoNotConfirmEnableDisable { get; set; } = false;

        // If true, the app will NOT warn about unsaved changes when playing.
        // Default is false (warn).
        public bool DoNotWarnUnsavedChanges { get; set; } = false;

        // Deprecated properties for migration
        public List<string>? EnabledMods { get; set; }
        public Dictionary<string, string>? ModConfigurations { get; set; }
        public List<string>? ModLoadOrder { get; set; }

        public bool DeveloperModeEnabled { get; set; } = false;

        // Developer Mode Settings
        public string? DeveloperExportPath { get; set; }
        public List<string> DeveloperEnabledFiles { get; set; } = new();
        public bool SuppressExFatWarning { get; set; } = false;
        public string? LastFileBrowserPath { get; set; }
        public List<string> FavoritePaths { get; set; } = new();

        public string? UsmapVersion { get; set; }
        public string? CustomUnionDirectory { get; set; }

        public string GameAesKey { get; set; } = "0x1B5E5FF1646B6509B0EF29FF3890A95AFB91BF131DD335146FC237D667D5E798";
    }

    public class SerializableTheme
    {
        public int BackColor { get; set; } = Color.FromArgb(45, 45, 48).ToArgb();
        public int ForeColor { get; set; } = Color.White.ToArgb();
        public int ControlBackColor { get; set; } = Color.FromArgb(30, 30, 30).ToArgb();
        public int ControlForeColor { get; set; } = Color.White.ToArgb();
        public int ButtonBackColor { get; set; } = Color.FromArgb(63, 63, 70).ToArgb();
        public int ButtonForeColor { get; set; } = Color.White.ToArgb();
        public int AccentColor { get; set; } = Color.FromArgb(0, 122, 204).ToArgb();
        public int? PlayButtonColor { get; set; }
        public int BorderColor { get; set; } = Color.FromArgb(80, 80, 80).ToArgb();
        public int MenuBackColor { get; set; } = Color.FromArgb(60, 60, 60).ToArgb();
        public int MenuForeColor { get; set; } = Color.White.ToArgb();
    }
}
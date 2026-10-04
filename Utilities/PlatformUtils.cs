using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CrossworldsModManager
{
    public static class PlatformUtils
    {
        public static bool IsAppImage => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                                         Environment.GetEnvironmentVariable("APPIMAGE") != null;

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static string AppDataDir
        {
            get
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "bluestar");
                if (IsAppImage && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static string AppDataDataDir
        {
            get
            {
                var dir = Path.Combine(AppDataDir, "data");
                if (IsAppImage && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static string GetBaseDir()
        {
            return IsAppImage ? AppDataDir : AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string GetDataDir()
        {
            return IsAppImage ? AppDataDataDir : AppDomain.CurrentDomain.BaseDirectory;
        }

        // Files that ship inside the app (icon.png, Megaman image, Sound.jpg, LocResUtilityCli,
        // repak, Oodle dlls). These must stay anchored to the base directory so they are found
        // inside the AppImage mount.
        public static string GetToolsDir()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
        }

        // Writable counterpart to GetToolsDir() for anything the app generates (extracted
        // .locres, downloaded helper tools, user-supplied base files). An AppImage base directory
        // is a read-only mount, so generated data is redirected to the app data directory there.
        public static string GetToolsWorkDir()
        {
            return IsAppImage ? AppDataDataDir : GetToolsDir();
        }

        public static string GetWorkDir()
        {
            return IsAppImage ? AppDataDataDir : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
        }

        public static string GetModBackupDir()
        {
            return IsAppImage
                ? Path.Combine(AppDataDir, "backup")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModsTemp");
        }

        public static string GetPromoFlagPath(string flagName)
        {
            return IsAppImage
                ? Path.Combine(AppDataDir, flagName)
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, flagName);
        }

        public static string GetLocresModOutputRoot()
        {
            return IsAppImage
                ? Path.Combine(AppDataDataDir, "LocresMod", "UNION", "Content", "Localization", "Game")
                : Path.Combine(GetToolsDir(), "LocresMod", "UNION", "Content", "Localization", "Game");
        }

        public static string GetLocresModPakPath()
        {
            return IsAppImage
                ? Path.Combine(AppDataDataDir, "LocresMod.pak")
                : Path.Combine(GetToolsDir(), "LocresMod.pak");
        }

        public static string GetMergedJsonDir()
        {
            return IsAppImage ? AppDataDataDir : GetToolsDir();
        }

        public static string GetSettingsFilePath()
        {
            return IsAppImage
                ? Path.Combine(AppDataDir, "settings.json")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        }

        public static string GetOpsLogPath()
        {
            return IsAppImage
                ? Path.Combine(AppDataDir, "mod_ops.log")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mod_ops.log");
        }

        public static void OpenFolderInExplorer(string path)
        {
            if (IsLinux)
                Process.Start("xdg-open", $"\"{path}\"");
            else
                Process.Start("explorer.exe", path);
        }
    }
}

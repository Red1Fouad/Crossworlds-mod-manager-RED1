using System;
using System.IO;
using System.Text.Json;

namespace CrossworldsModManager
{
    public static class SettingsManager
    {
        private static readonly string SettingsFilePath = PlatformUtils.GetSettingsFilePath();
        public static AppSettings Settings { get; private set; } = new AppSettings();
        public static bool SettingsWereReset { get; private set; }

        public static void Load()
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                try
                {
                    Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch (JsonException)
                {
                    var backupPath = SettingsFilePath + ".corrupted." + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bak";
                    try { File.Copy(SettingsFilePath, backupPath, true); } catch { }
                    Settings = new AppSettings();
                    SettingsWereReset = true;
                }
            }
        }

        public static void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(Settings, options);
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsFilePath, json);
        }
    }
}

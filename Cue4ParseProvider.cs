using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

namespace CrossworldsModManager
{
    public static class Cue4ParseProvider
    {
        private static DefaultFileProvider? _provider;
        private static readonly object _lock = new();

        public static string GameAesKey
        {
            get => SettingsManager.Settings?.GameAesKey ?? "0x1B5E5FF1646B6509B0EF29FF3890A95AFB91BF131DD335146FC237D667D5E798";
            set => SettingsManager.Settings.GameAesKey = value;
        }

        public static EGame GameEngine { get; set; } = EGame.GAME_UE5_4;

        public static DefaultFileProvider? Provider => _provider;

        public static bool IsInitialized => _provider != null;

        public static event Action<string>? OnLog;

        private static void Log(string message)
        {
            Debug.WriteLine($"[CUE4Parse] {message}");
            OnLog?.Invoke(message);
        }

        private static readonly string[] ExcludedSubDirs = ["~mods", "LogicMods"];

        public static void Initialize(string gameDirectory)
        {
            var paksDir = Path.Combine(gameDirectory, "UNION", "Content", "Paks");
            if (!Directory.Exists(paksDir))
                throw new DirectoryNotFoundException($"Game Paks directory not found: {paksDir}");

            lock (_lock)
            {
                Log("Initializing Oodle decompression...");
                try
                {
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var oodleDll = Path.Combine(baseDir, "oodle-data-shared.dll");
                    var oodleOld = Path.Combine(baseDir, "oo2core_9_win64.dll");

                    if (File.Exists(oodleOld))
                        oodleDll = oodleOld;
                    else if (!File.Exists(oodleDll))
                    {
                        Log("Oodle DLL not found locally, downloading...");
                        try
                        {
                            var urls = new[]
                            {
                                "https://github.com/WorkingRobot/OodleUE/releases/download/2026-06-04-1357/msvc-x64-release.zip",
                                "https://github.com/WorkingRobot/OodleUE/releases/download/2026-06-04-1357/clang-cl-x64-release.zip"
                            };
                            var entryPaths = new[] { "bin/oodle-data-shared.dll", "oodle-data-shared.dll" };
                            using var client = new HttpClient();
                            client.DefaultRequestHeaders.UserAgent.ParseAdd("BluestarManager/1.0");
                            client.Timeout = TimeSpan.FromSeconds(15);
                            bool downloaded = false;
                            foreach (var url in urls)
                            {
                                try
                                {
                                    var response = client.GetAsync(url).GetAwaiter().GetResult();
                                    response.EnsureSuccessStatusCode();
                                    using var zipStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                                    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                                    foreach (var ep in entryPaths)
                                    {
                                        var entry = archive.GetEntry(ep);
                                        if (entry != null)
                                        {
                                            entry.ExtractToFile(oodleDll, overwrite: true);
                                            Log("Oodle DLL downloaded successfully");
                                            downloaded = true;
                                            break;
                                        }
                                    }
                                    if (downloaded) break;
                                }
                                catch { /* try next URL */ }
                            }
                            if (!downloaded)
                                Log("Oodle DLL entry not found in any download");
                        }
                        catch (Exception dlEx)
                        {
                            Log($"Oodle download failed: {dlEx.Message}");
                        }
                    }

                    if (File.Exists(oodleDll))
                    {
                        Log($"Loading Oodle DLL from {oodleDll}");
                        OodleHelper.Initialize(oodleDll);
                        Log("Oodle decompression initialized");
                    }
                    else
                    {
                        Log("Oodle DLL not available");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Oodle initialization: {ex.Message}");
                }

                _provider?.Dispose();
                _provider = new DefaultFileProvider(
                    paksDir,
                    SearchOption.AllDirectories,
                    new VersionContainer(GameEngine),
                    StringComparer.OrdinalIgnoreCase
                );

                // Manually register game archives, excluding ~mods and LogicMods
                var archiveFiles = Directory.EnumerateFiles(paksDir, "*.*", SearchOption.AllDirectories)
                    .Where(f => ShouldInclude(f, paksDir))
                    .ToList();

                Log($"Scanning {paksDir} for archives (excluding ~mods and LogicMods)...");
                var registered = 0;
                foreach (var file in archiveFiles)
                {
                    var ext = Path.GetExtension(file).TrimStart('.').ToUpperInvariant();
                    if (ext is "PAK" or "UTOC" or "UCAS")
                    {
                        _provider.RegisterVfs(new FileInfo(file));
                        registered++;
                    }
                }

                Log($"Registered {registered} VFS archives");

                var mounted = _provider.Mount();
                Log($"Mounted {mounted} unencrypted archives");

                SubmitAesKey();
            }
        }

        private static bool ShouldInclude(string filePath, string paksDir)
        {
            var relative = Path.GetRelativePath(paksDir, filePath);
            var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return parts.Length <= 1 || !ExcludedSubDirs.Contains(parts[0], StringComparer.OrdinalIgnoreCase);
        }

        public static void SubmitAesKey()
        {
            if (_provider == null) return;

            var key = new FAesKey(GameAesKey);
            Log($"Submitting AES key: {key.KeyString}");

            if (_provider.RequiredKeys.Count > 0)
            {
                foreach (var guid in _provider.RequiredKeys)
                {
                    var submitted = _provider.SubmitKey(guid, key);
                    Log($"Submitted key for GUID {guid}, mounted {submitted} archives");
                }
            }
            else
            {
                var submitted = _provider.SubmitKey(new FGuid(), key);
                Log($"No required keys found, submitted to default GUID, mounted {submitted} archives");
            }

            // Try mounting any remaining unloaded VFS with the key
            if (_provider.UnloadedVfs.Count > 0)
            {
                var remaining = _provider.Mount();
                Log($"Mounted {remaining} remaining archives after key submission");
            }

            var totalFiles = _provider.Files.Count;
            var mountedReaders = _provider.MountedVfs.Count;
            Log($"CUE4Parse ready: {totalFiles} files available across {mountedReaders} mounted archives");
        }

        public static async Task<bool> TryInitializeAsync(string gameDirectory)
        {
            try
            {
                await Task.Run(() => Initialize(gameDirectory));
                Log("CUE4Parse initialization completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log($"CUE4Parse initialization failed: {ex.Message}");
                return false;
            }
        }

        public static async Task ExtractLocalizationAsync(string gameDirectory, string outputRoot, IProgress<string>? progress = null)
        {
            if (!IsInitialized)
            {
                Log("Initializing CUE4Parse for localization extraction...");
                await Task.Run(() => Initialize(gameDirectory));
            }

            if (_provider == null)
            {
                progress?.Report("CUE4Parse provider not available.");
                return;
            }

            var localizationPrefix = "UNION/Content/Localization/Game/";
            var matchingFiles = _provider.Files.Keys
                .Where(k => k.StartsWith(localizationPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingFiles.Count == 0)
            {
                progress?.Report("No localization files found in game archives.");
                return;
            }

            progress?.Report($"Extracting {matchingFiles.Count} localization files from game...");

            int count = 0;
            foreach (var gamePath in matchingFiles)
            {
                if (!_provider.Files.TryGetValue(gamePath, out var gameFile)) continue;

                var outputPath = Path.Combine(outputRoot, gamePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                try
                {
                    var data = await gameFile.ReadAsync();
                    await File.WriteAllBytesAsync(outputPath, data);
                    count++;
                }
                catch (Exception ex)
                {
                    Log($"Failed to extract {gamePath}: {ex.Message}");
                }
            }

            progress?.Report($"Extracted {count} localization files to {outputRoot}");
        }

        public static void Dispose()
        {
            lock (_lock)
            {
                _provider?.Dispose();
                _provider = null;
                Log("CUE4Parse provider disposed");
            }
        }
    }
}

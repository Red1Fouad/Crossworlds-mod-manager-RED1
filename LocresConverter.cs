using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;

namespace CrossworldsModManager
{
    public static class LocresConverter
    {
        private const string ToolUrl = "https://github.com/anubi47/LocResUtility/releases/download/v2.1.0/LocResUtilityCli-v2.1.0-win-x64.7z";
        #if SELFCONTAINED
            private const string LinuxToolUrl = "https://github.com/AntiApple4life/LocResUtility/releases/download/v2.1.0/LocResUtilityCli-v2.1.0-linux-self-contained-x64.7z";
        #else
            private const string LinuxToolUrl = "https://github.com/AntiApple4life/LocResUtility/releases/download/v2.1.0/LocResUtilityCli-v2.1.0-linux-x64.7z";
        #endif
        private static readonly string ToolsDir = PlatformUtils.GetToolsDir();
        private static readonly string ToolExePath = Path.Combine(ToolsDir, "LocResUtilityCli", PlatformUtils.IsLinux ? "LocResUtilityCli" : "LocResUtilityCli.exe");

        public static async Task ConvertToJsonAsync(string locresPath)
        {
            var jsonPath = Path.ChangeExtension(locresPath, ".json");
            try
            {
                string? exePath = await EnsureToolExistsAsync();
                if (exePath == null) return;

                // Use the 'export' command: export <outputPath> <targetPath>
                await RunProcessAsync(exePath, $"export \"{jsonPath}\" \"{locresPath}\" -y", null);
                CustomMessageBox.Show($"Successfully converted to:\n{jsonPath}",
                    "Conversion Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to convert .locres to .json.\n\nError: {ex.Message}",
                    "Conversion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static async Task<bool> ConvertLocresToJsonFile(string locresPath, string outputPath, IProgress<string> progress)
        {
            try
            {
                string? exePath = await EnsureToolExistsAsync();
                if (exePath == null) return false;

                await RunProcessAsync(exePath, $"export \"{outputPath}\" \"{locresPath}\" -y", progress);

                if (File.Exists(outputPath))
                {
                    progress.Report($"Successfully converted '{Path.GetFileName(locresPath)}' to '{Path.GetFileName(outputPath)}'.");
                    return true;
                }
                else
                {
                    progress.Report($"Conversion failed: Output file not created at '{outputPath}'.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                progress.Report($"ERROR during conversion: {ex.Message}");
                return false;
            }
        }

        public static async Task ConvertToLocresAsync(string jsonPath)
        {
            var locresPath = Path.ChangeExtension(jsonPath, ".locres");
            try
            {
                string? exePath = await EnsureToolExistsAsync();
                if (exePath == null) return;

                var baseLocresPath = Path.Combine(ToolsDir, "Game.locres");
                if (!File.Exists(baseLocresPath))
                {
                    CustomMessageBox.Show($"Base file 'Game.locres' not found in the Tools folder.\nPlease place a clean copy of the game's .locres file there to use as a base for importing.",
                        "Base File Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Use the 'import' command: import <outputPath> <targetPath> <sourcePath>
                // outputPath = new .locres, targetPath = base Game.locres, sourcePath = our .json
                await RunProcessAsync(exePath, $"import \"{locresPath}\" \"{baseLocresPath}\" \"{jsonPath}\" -y", null);
                CustomMessageBox.Show($"Successfully created new .locres at:\n{locresPath}",
                    "Conversion Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to convert .json to .locres.\n\nError: {ex.Message}",
                    "Conversion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static async Task ProcessModJsonFilesAsync(IEnumerable<ModInfo> enabledMods, IProgress<string>? progress = null)
        {
            if (PlatformUtils.IsAppImage)
                Directory.CreateDirectory(PlatformUtils.AppDataDataDir);
            // Language -> Namespace -> Key -> Value
            var jsonModifications = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

            // 1. Collect all JSON modifications from enabled mods, respecting load order (reverse iteration)
            foreach (var modInfo in enabledMods.Reverse())
            {
                var modJsonFiles = Directory.EnumerateFiles(modInfo.DirectoryPath, "*.json", SearchOption.AllDirectories);
                foreach (var jsonFile in modJsonFiles)
                {
                    try
                    {
                        var jsonContent = await File.ReadAllTextAsync(jsonFile);
                        var token = JToken.Parse(jsonContent);

                        if (token is JArray modArray)
                        {
                            ExtractModifications(modArray, jsonModifications);
                        }
                        else if (token is JObject modObj)
                        {
                            // Support two object formats:
                            // 1) { "en": { "strings": { ... } } }
                            // 2) { "Language": "en", "Namespace": "...", "Key": "...", "Value": "..." }
                            var handled = false;
                            // Format 1: language -> { strings: { Namespace: [ { Key, Value }, ... ] } }
                            foreach (var prop in modObj.Properties())
                            {
                                if (prop.Value is JObject valObj && valObj["strings"] is JObject)
                                {
                                    ExtractModificationsFromObject(modObj, jsonModifications);
                                    handled = true;
                                    break;
                                }
                            }
                            if (!handled)
                            {
                                // Treat it as a single change object and wrap into array
                                var arr = new JArray();
                                arr.Add(modObj);
                                ExtractModifications(arr, jsonModifications);
                            }
                        }

                        progress?.Report($"Loaded JSON: {jsonFile}");
                    }
                    catch (Exception ex)
                    {
                        var msg = $"Could not parse or process JSON file '{jsonFile}': {ex.Message}";
                        if (progress != null) progress.Report(msg);
                        else Debug.WriteLine(msg);
                    }
                }
            }

            if (jsonModifications.Count == 0)
            {
                progress?.Report("No text mod (.json) files found to merge.");
                return;
            }

            // 2. Ensure the tool exists
            string? exePath = await EnsureToolExistsAsync();
            if (exePath == null) return;

            // 3. Iterate through each language folder and process the locres file
            var languagesRoot = Path.Combine(ToolsDir, "Locres", "UNION", "Content", "Localization", "Game");
            if (!Directory.Exists(languagesRoot))
            {
                var msg = $"Base locres directory not found at: {languagesRoot}";
                if (progress != null) progress.Report(msg);
                CustomMessageBox.Show(msg, "Directory Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. Iterate through each language that has modifications.
            var languagesToProcess = jsonModifications.Keys;
            if (languagesToProcess.Count == 0)
            {
                progress?.Report("No valid language-specific modifications found in JSON files.");
                return;
            }

            var tasks = languagesToProcess.Select(async langCode =>
            {
                var baseLocresPath = Path.Combine(languagesRoot, langCode, "Game.locres");

                if (!File.Exists(baseLocresPath))
                {
                    progress?.Report($"Skipping '{langCode}': Game.locres not found.");
                    return;
                }

                progress?.Report($"Processing language: {langCode}");

                var tempBaseJsonPath = Path.Combine(Path.GetTempPath(), $"base_{langCode}.json");
                await RunProcessAsync(exePath, $"export \"{tempBaseJsonPath}\" \"{baseLocresPath}\" -y", progress);
                if (!File.Exists(tempBaseJsonPath))
                {
                    progress?.Report($"Failed to convert Game.locres for '{langCode}'.");
                    return;
                }

                var baseJson = JObject.Parse(await File.ReadAllTextAsync(tempBaseJsonPath));

                // Try to locate namespaces in several common JSON layouts produced by different exports.
                // 1) Mapping style: baseJson[lang]["strings"] is an object where properties are namespace names -> JArray
                JObject? namespacesObj = null;
                if (baseJson[langCode] is JObject langRoot && langRoot["strings"] is JObject sObj)
                {
                    namespacesObj = (JObject)sObj;
                }
                else if (baseJson["strings"] is JObject sObj2)
                {
                    namespacesObj = (JObject)sObj2;
                }

                // 2) Array style: baseJson["Items"][*]["Namespaces"] -> JArray where each namespace has { "Name": ..., "Strings": [...] }
                JArray? namespacesArray = null;
                if (baseJson["Items"] is JArray itemsArray)
                {
                    foreach (var item in itemsArray.Children<JObject>())
                    {
                        // Some exports include Culture on the item; if present prefer the one matching langCode
                        if (item["Culture"] != null)
                        {
                            if (string.Equals(item["Culture"]?.ToString(), langCode, StringComparison.OrdinalIgnoreCase) && item["Namespaces"] is JArray nsArr)
                            {
                                namespacesArray = (JArray)nsArr;
                                break;
                            }
                        }
                        // Fallback: first item that has Namespaces
                        if (namespacesArray == null && item["Namespaces"] is JArray anyNs)
                        {
                            namespacesArray = (JArray)anyNs;
                        }
                    }
                }

                if (namespacesObj == null && namespacesArray == null)
                {
                    progress?.Report($"Could not find namespaces structure for language '{langCode}' in exported JSON.");
                    if (File.Exists(tempBaseJsonPath)) File.Delete(tempBaseJsonPath);
                    return;
                }

                // 4. Apply modifications to the base JSON for the current language
                foreach (var nsEntry in jsonModifications[langCode])
                {
                    var ns = nsEntry.Key;

                    if (namespacesObj != null)
                    {
                        if (namespacesObj[ns] is JArray stringEntries)
                        {
                            foreach (KeyValuePair<string, string> keyEntry in nsEntry.Value)
                            {
                                var key = keyEntry.Key;
                                var value = keyEntry.Value;
                                var entryToUpdate = stringEntries.FirstOrDefault(t => t["Key"]?.ToString() == key);
                                if (entryToUpdate != null) entryToUpdate["Value"] = value;
                            }
                        }
                    }
                    else if (namespacesArray != null)
                    {
                        var namespaceObject = namespacesArray.FirstOrDefault(item => item["Name"]?.ToString().Equals(ns, StringComparison.OrdinalIgnoreCase) ?? false) as JObject;
                        if (namespaceObject != null && namespaceObject["Strings"] is JArray stringEntries)
                        {
                            foreach (KeyValuePair<string, string> keyEntry in nsEntry.Value)
                            {
                                var key = keyEntry.Key;
                                var value = keyEntry.Value;
                                var entryToUpdate = stringEntries.FirstOrDefault(t => t["Key"]?.ToString() == key);
                                if (entryToUpdate != null) entryToUpdate["Value"] = value;
                            }
                        }
                    }
                }

                // 5. Save the final merged JSON for this language to the Tools folder
                var outputJsonPath = Path.Combine(PlatformUtils.GetMergedJsonDir(), $"Game_{langCode}.json");
                await File.WriteAllTextAsync(outputJsonPath, baseJson.ToString(Newtonsoft.Json.Formatting.Indented));
                progress?.Report($"Saved merged JSON for {langCode}: {outputJsonPath}");

                if (File.Exists(tempBaseJsonPath)) File.Delete(tempBaseJsonPath);
            });

            await Task.WhenAll(tasks);

            progress?.Report("Successfully merged JSON modifications for all found languages.");
        }

        public static async Task PackMergedLocresAsync(string gamePath, IProgress<string>? progress = null)
        {
            if (PlatformUtils.IsAppImage)
                Directory.CreateDirectory(PlatformUtils.AppDataDataDir);
            try
            {
                string? exePath = await EnsureToolExistsAsync();
                if (exePath == null) return;

                var mergedJsonFiles = Directory.GetFiles(PlatformUtils.GetMergedJsonDir(), "Game_*.json");
                if (mergedJsonFiles.Length == 0)
                {
                    progress?.Report("No merged 'Game_*.json' files found in the Tools folder to pack.");
                    return;
                }

                var languagesRoot = Path.Combine(ToolsDir, "Locres", "UNION", "Content", "Localization", "Game");
                
                var outputRoot = PlatformUtils.GetLocresModOutputRoot();

                var tasks = mergedJsonFiles.Select(async jsonPath =>
                {
                    var fileName = Path.GetFileName(jsonPath); // e.g., Game_en.json
                    var langCode = fileName.Substring("Game_".Length, fileName.Length - "Game_".Length - ".json".Length);

                    var baseLocresPath = Path.Combine(languagesRoot, langCode, "Game.locres");
                    if (!File.Exists(baseLocresPath))
                    {
                        progress?.Report($"Skipping '{langCode}': Original Game.locres not found.");
                        return false;
                    }
                    var outputLangDir = Path.Combine(outputRoot, langCode);
                    Directory.CreateDirectory(outputLangDir);
                    var outputLocresPath = Path.Combine(outputLangDir, "Game.locres");

                    progress?.Report($"Packing '{langCode}'...");
                    // Use the 'import' command: import <outputPath> <targetPath> <sourcePath>
                    await RunProcessAsync(exePath, $"import \"{outputLocresPath}\" \"{baseLocresPath}\" \"{jsonPath}\" -y", progress);
                    return true;
                });

                var results = await Task.WhenAll(tasks);
                int successCount = results.Count(r => r);

                if (successCount > 0)
                {
                    progress?.Report($"\nPacking {successCount} language(s) complete. Now creating final pak file...");

                    // Final step: Run UnrealPak.bat with the LocresMod folder path.
                    var repakDir = Path.Combine(ToolsDir, "repak");
                    var repakBinPath = Path.Combine(repakDir, PlatformUtils.IsLinux ? "repak" : "repak.exe");
                    if (File.Exists(repakBinPath))
                    {
                        var locresModPath = PlatformUtils.IsAppImage ? Path.Combine(PlatformUtils.AppDataDataDir, "LocresMod") : Path.Combine(ToolsDir, "LocresMod");
                        var outputPakPath = PlatformUtils.GetLocresModPakPath();
                        
                        var cmdArgs = $"pack \"{locresModPath}\"";
                        await RunProcessAsync(repakBinPath, cmdArgs, progress, repakDir);

                        progress?.Report($"Final pak file created at: {outputPakPath}");
                    }
                    else
                    {
                        progress?.Report($"Warning: {(PlatformUtils.IsLinux ? "repak" : "repak.exe")} not found at {repakDir}. Skipping final pak creation.");
                    }
                }

                progress?.Report($"Successfully packed {successCount} of {mergedJsonFiles.Length} language(s).");
            }
            catch (Exception ex)
            {
                progress?.Report($"An error occurred while packing .locres files: {ex.Message}");
            }
        }

        private static async Task<string?> EnsureToolExistsAsync()
        {
            if (File.Exists(ToolExePath))
            {
                return ToolExePath;
            }

            #if SELFCONTAINED
            var choice = CustomMessageBox.Show("LocResUtility is not found. Would you like to download it now? (approx. 25 MB download, 86 MB extracted)",
                "Download Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            #else
            var choice = CustomMessageBox.Show("LocResUtility is not found. Would you like to download it now? (approx. 3 MB download, 15 MB extracted)",
                "Download Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            #endif

            if (choice == DialogResult.No) return null;

            try
            {
                Directory.CreateDirectory(ToolsDir);
                var archivePath = Path.Combine(ToolsDir, "LocResUtility.7z");

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(PlatformUtils.IsLinux ? LinuxToolUrl : ToolUrl);
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(archivePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                using (var archive = SevenZipArchive.OpenArchive(archivePath))
                {
                    archive.WriteToDirectory(ToolsDir, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                }

                File.Delete(archivePath);
                if (File.Exists(ToolExePath))
                {
                    if (PlatformUtils.IsLinux)
                        File.SetUnixFileMode(ToolExePath, UnixFileMode.UserExecute);
                    CustomMessageBox.Show("LocResUtility downloaded and extracted successfully.", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return ToolExePath;
                }
                throw new FileNotFoundException(PlatformUtils.IsLinux ? "Failed to find LocResUtility binary after extraction." : "Failed to find LocResUtility.exe after extraction.");
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to download or extract LocResUtility.\n\nError: {ex.Message}", "Download Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private static async Task RunProcessAsync(string fileName, string arguments, IProgress<string>? progress = null, string? workingDirectory = null)
        {
            var header = $"\n> Running command: {Path.GetFileName(fileName)} {arguments}";
            if (progress != null) { progress.Report(header); }
            else { Debug.WriteLine(header); }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo(fileName, arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(fileName) ?? AppDomain.CurrentDomain.BaseDirectory
                }
            };

            process.Start();

            // Asynchronously read the output and error streams to avoid deadlocks
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(output))
            {
                var outMsg = $"[Output]:\n{output.Trim()}";
                if (progress != null) { progress.Report(outMsg); }
                else { Debug.WriteLine(outMsg); }
            }
            if (!string.IsNullOrWhiteSpace(error))
            {
                var errMsg = $"[Error]:\n{error.Trim()}";
                if (progress != null) { progress.Report(errMsg); }
                else { Debug.WriteLine(errMsg); }
            }
        }

        private static void ExtractModifications(JArray modArray, Dictionary<string, Dictionary<string, Dictionary<string, string>>> modifications)
        {
            foreach (var item in modArray.Children<JObject>())
            {
                var language = item["Language"]?.ToString();
                var ns = item["Namespace"]?.ToString();
                var key = item["Key"]?.ToString();
                var value = item["Value"]?.ToString();

                if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(ns) || string.IsNullOrEmpty(key) || value == null) continue;

                if (!modifications.ContainsKey(language))
                {
                    modifications[language] = new Dictionary<string, Dictionary<string, string>>();
                }
                if (!modifications[language].ContainsKey(ns))
                {
                    modifications[language][ns] = new Dictionary<string, string>();
                }
                modifications[language][ns][key] = value;
            }
        }

        private static void ExtractModificationsFromObject(JObject modJson, Dictionary<string, Dictionary<string, Dictionary<string, string>>> modifications)
        {
            foreach (var langProperty in modJson.Properties())
            {
                var language = langProperty.Name;
                if (langProperty.Value? ["strings"] is not JObject namespaces) continue;

                if (!modifications.ContainsKey(language))
                {
                    modifications[language] = new Dictionary<string, Dictionary<string, string>>();
                }

                foreach (var nsProperty in namespaces.Properties())
                {
                    var ns = nsProperty.Name;
                    if (nsProperty.Value is not JArray stringEntries) continue;

                    if (!modifications[language].ContainsKey(ns))
                    {
                        modifications[language][ns] = new Dictionary<string, string>();
                    }

                    foreach (var entry in stringEntries.Children<JObject>())
                    {
                        var key = entry["Key"]?.ToString();
                        var value = entry["Value"]?.ToString();
                        if (key != null && value != null) modifications[language][ns][key] = value;
                    }
                }
            }
        }
    }
}
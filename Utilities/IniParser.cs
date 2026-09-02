using System.Collections.Generic;
using System.IO;

namespace CrossworldsModManager
{
    public static class IniParser
    {
        public static Dictionary<string, Dictionary<string, string>> Parse(string filePath)
        {
            var data = new Dictionary<string, Dictionary<string, string>>();
            if (!File.Exists(filePath)) return data;

            string currentSection = "";

            foreach (var line in File.ReadAllLines(filePath))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                    continue;

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    if (!data.ContainsKey(currentSection))
                    {
                        data[currentSection] = new Dictionary<string, string>();
                    }
                }
                else if (!string.IsNullOrEmpty(currentSection) && trimmedLine.Contains("="))
                {
                    var parts = trimmedLine.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim().Trim('"'); // Trim quotes
                        data[currentSection][key] = value;
                    }
                }
            }
            return data;
        }

        public static void Write(string filePath, Dictionary<string, Dictionary<string, string>> data)
        {
            var lines = new List<string>();
            foreach (var section in data)
            {
                lines.Add($"[{section.Key}]");
                foreach (var kvp in section.Value)
                {
                    // Quote values that contain spaces or special characters, but not simple ones.
                    var value = kvp.Value;
                    if (value.Contains(" ") || value.Contains(";") || value.Contains("="))
                    {
                        value = $"\"{value}\"";
                    }
                    lines.Add($"{kvp.Key} = {value}");
                }
                lines.Add(""); // Add a blank line between sections
            }

            // Ensure the directory exists before writing
            var directory = Path.GetDirectoryName(filePath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
                File.WriteAllLines(filePath, lines);
            }
        }
    }
}
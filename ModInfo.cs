using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace CrossworldsModManager
{
    public class ModInfo
    {
        public string Name { get; set; } = "Unknown Mod";
        public string Author { get; set; } = "Unknown Author";
        public string Version { get; set; } = "1.0";
        // GameBanana version counter (number of updates) used for update checks.
        public string GBVersion { get; set; } = "0";
        public string? GBItemId { get; set; }
        public string? GBItemType { get; set; }
        public string Description { get; set; } = "No description provided.";
        public string DirectoryPath { get; set; } = "";
        public bool IsLogicMod { get; set; } = false;

        // New properties for multi-group configuration
        public List<ModConfigurationGroup> ConfigurationGroups { get; set; } = new();
        public Dictionary<string, string> FileGroupMappings { get; set; } = new();
    }

    public class ModConfigurationGroup
    {
        public string GroupName { get; set; }
        public ModConfigType Type { get; set; }
        public string Description { get; set; }
        public List<string> Options { get; set; }

        public ModConfigurationGroup(string configSectionKey, Dictionary<string, string> configSection)
        {
            // Extract GroupName from "Config:GroupName"
            GroupName = configSectionKey.Split(':').Last().Trim();

            string typeStr = configSection.GetValueOrDefault("Type", "SelectOne");
            if (System.Enum.TryParse<ModConfigType>(typeStr, true, out var configType))
            {
                Type = configType;
            }
            else
            {
                Type = ModConfigType.SelectOne; // Default to SelectOne if parsing fails
            }
            Description = configSection.GetValueOrDefault("Description", $"Select an option for {GroupName}:");
            Options = configSection.GetValueOrDefault("Options", "").Split(',').Select(o => o.Trim()).ToList();
        }
    }

    public class ModSection
    {
        public string Name { get; set; } = "Section";
        public Color? TextColor { get; set; }
    }
}
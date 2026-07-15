<img width="175" height="216" alt="icon" src="https://github.com/user-attachments/assets/57e9ec12-f8e1-4cc3-9cfb-507b96da6315" />

# ***THIS MOD MANAGER DOES NOT WORK ON exFAT DRIVES! MAKE SURE THE GAME IS ON NTFS DRIVE BEFORE USING THIS MOD MANAGER!!!!***

# Blue Star Manager: The CrossWorlds Mod Manager

**Blue Star Manager** is a powerful mod manager for *Sonic Racing: CrossWorlds* on PC. It provides an easy-to-use interface for players to install and manage their mods, and offers robust features for mod creators to build configurable and advanced modifications.

## Features

- **Easy Mod Management**: Simple interface to enable and disable mods.
- **Mod Configuration**: In-app UI for configuring mods that have options (e.g., character skins, color variants, gameplay tweaks).
- **GameBanana Integration**: Browse and install mods directly from GameBanana without leaving the manager.
- **Advanced Mod Support**:
    - **File Patching**: Automatically handles `.pak`, `.ucas`, and `.utoc` files.
    - **Text Merging**: Allows mods to modify in-game text via a simple `mod.json` file.
- **Automatic Updates**: Keeps your mods and the manager itself up-to-date.

---

## For Players: How to Use

1. **Download**: Grab the latest release of Blue Star Manager.
2. **Setup**: Place the manager anywhere and run it. It will automatically detect your *Sonic Racing: CrossWorlds* installation (Steam version required). If it can't find it, you can set the game path manually in the settings.
3. **Install Mods**:
    - Use the "GameBanana" tab to browse and install mods with one click.
    - Or, manually download a mod and drag-and-drop its `.zip` or folder into the manager.
4. **Enable/Disable**: Click the checkbox next to a mod to enable or disable it.
5. **Configure Mods**: If a mod has a "Configure" button, you can click it to open a window with different options for that mod.
6. **Play**: Click "Launch Game" to play with your selected mods!
---

## For Mod Creators: Creating Compatible Mods

To make your mods compatible with Blue Star Manager, you need to include a `mod.ini` file in the root of your mod's folder. This file contains metadata and defines configuration options.

### 1. Basic `mod.ini` (Metadata)

The `[Main]` section provides information about your mod that is displayed in the manager.

**Example:**
```ini
; This is a comment
[Main]
Name = My Awesome Mod
Author = Your Name
Description = This mod replaces the main character's car with a cool new model.
```

-   **Name**: The display name of your mod.
-   **Author**: Your name or alias.
-   **Version**: The version number of your mod.
-   **Description**: A short description of what your mod does.
-   **Type**: (Optional) Set this to `LogicMod` if your mod is a UE4SS Logic Mod.

**Thumbnail**: (Optional) You can add a Thumbnail image of your mod by adding your Thumb.png or Thumb.jpg next to the `mod.ini`.

> **Note:** If you don't provide a `mod.ini`, the manager will use the mod's folder name as its name and won't show any other details.

### 2. Creating Configuration Options

## Automatically:
You can configure your mod by right clicking it in the list, and selecting "Mod Config Maker".

## Manually:
You can add configuration options that will appear in a "Configure" window for your mod using `[Config:GroupName]` sections. `GroupName` is a unique name for your option group (e.g., "Color", "Character").

-   **Type**: `SelectOne` (radio buttons) or `SelectMultiple` (checkboxes).
-   **Description**: Text displayed above the options.
-   **Options**: A comma-separated list of choices.

**Example: `SelectOne` (Radio Buttons)**
```ini
[Config:Color]
Type = SelectOne
Description = Choose the color for the new car model:
Options = Red, Blue, Green, Black
```

**Example: `SelectMultiple` (Checkboxes)**
```ini
[Config:Extra Effects]
Type = SelectMultiple
Description = Select additional visual effects:
Options = Neon Underglow, Sparkling Exhaust, Custom Horn
```

### 3. Linking Configuration to Files

The `[Files]` section maps your configuration options to the files they control. The format is `FilePath = GroupName.OptionName`.

-   `FilePath` is the relative path to your mod file.
-   `GroupName.OptionName` is the unique identifier for the option.
> **Important:** The manager automatically handles all related UE4\5 pak files (`.pak`, `.ucas`, `.utoc`, etc.). You only need to specify the base file path once. This also works for `.json` files used for text merging.

**Example:**
```ini
[Config:Character]
Type=SelectOne
Description=Choose your character style:
Options=Vanilla,Classic,Modern

[Files]
; Pak, Utoc and Ucas files for different character models. No Need to seperately add files, just add their shared filename.
Models/Classic = Character.Classic
Models/Modern = Character.Modern

; JSON files for different character names
Text/ClassicName.json = Character.Classic
Text/ModernName.json = Character.Modern
```

### 4. Text Merging (JSON Files)

You can directly edit in-game text by creating `.json` files. This allows you to add or overwrite text entries in the game's localization tables.

Each `.json` file contains an array of objects, where each object represents a single text change:
- **Language**: The language code (e.g., `"en"`).
- **Namespace**: The localization namespace (e.g., `"DB_CharacterName"`).
- **Key**: The unique identifier for the text string.
- **Value**: The new text you want to display.

#### Two Ways to Use Text Merging:

1.  **Simple (Non-Configurable)**: For simple text changes that are always active when the mod is enabled, create a `.json` file (e.g., `mod.json`) in the root of your mod folder (next to `mod.ini`).

2.  **Advanced (Configurable)**: To offer different text options (like character names that match a model swap), create multiple `.json` files and link them to options in the `[Files]` section of your `mod.ini`, as shown in the example above.

**Example `ClassicName.json`:**
```json
[
    {
        "Language": "en",
        "Namespace": "DB_CharacterName",
        "Key": "chara12001",
        "Value": "Classic Character"
    }
]
```

**Example `ModernName.json`:**
```json
[
    {
        "Language": "en",
        "Namespace": "DB_CharacterName",
        "Key": "chara12001",
        "Value": "Modern Character 1"
    },
    {
        "Language": "en",
        "Namespace": "DB_CharacterName",
        "Key": "chara12002",
        "Value": "Modern Character 2"
    }
]
```

---

## Credits

-   **RED1**: Lead Developer
-   **AntiApple4life**: Linux Port
-   **repak**: Used for game asset packaging.
-   **LocResUtilityCLI**: Used for `.locres` file manipulation.
-   **SharpCompress**: Used for archive extraction.
-   **DanielVanNoord/System.Windows.Forms**: Used for the Linux port's frontend.

This tool is not affiliated with SEGA or Sumo Digital.

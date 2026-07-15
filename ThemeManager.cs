// c:\games\Projects\Crossworlds mod manager RED1\ThemeManager.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CrossworldsModManager
{
    public class Theme
    {
        public string Name { get; set; } = "Default";
        public Color BackColor { get; set; } = Color.FromArgb(45, 45, 48);
        public Color ForeColor { get; set; } = Color.White;
        public Color ControlBackColor { get; set; } = Color.FromArgb(30, 30, 30);
        public Color ControlForeColor { get; set; } = Color.White;
        public Color ButtonBackColor { get; set; } = Color.FromArgb(63, 63, 70);
        public Color ButtonForeColor { get; set; } = Color.White;
        public Color AccentColor { get; set; } = Color.FromArgb(0, 122, 204);
        public Color? PlayButtonColor { get; set; }
        public Color BorderColor { get; set; } = Color.FromArgb(80, 80, 80);
        public Color MenuBackColor { get; set; } = Color.FromArgb(60, 60, 60);
        public Color MenuForeColor { get; set; } = Color.White;
    }

    public static class ThemeManager
    {
        public static Theme CurrentTheme { get; private set; } = new Theme();

        private static readonly Dictionary<string, Theme> Themes = new Dictionary<string, Theme>
        {
            ["Default"] = new Theme
            {
                PlayButtonColor = Color.FromArgb(45, 137, 45)
            },
            ["Sonic Blue"] = new Theme
            {
                Name = "Sonic Blue",
                BackColor = Color.FromArgb(0, 50, 100),
                ForeColor = Color.White,
                ControlBackColor = Color.FromArgb(0, 30, 70),
                ControlForeColor = Color.White,
                ButtonBackColor = Color.FromArgb(0, 80, 160),
                ButtonForeColor = Color.White,
                AccentColor = Color.FromArgb(0, 190, 220), // Cyan
                BorderColor = Color.FromArgb(0, 100, 200),
                MenuBackColor = Color.FromArgb(0, 60, 120),
                MenuForeColor = Color.White
            },
            ["Purple"] = new Theme
            {
                Name = "Purple",
                BackColor = Color.FromArgb(35, 30, 40),
                ControlBackColor = Color.FromArgb(25, 20, 30),
                ButtonBackColor = Color.FromArgb(50, 40, 60),
                MenuBackColor = Color.FromArgb(45, 35, 55),
                AccentColor = Color.FromArgb(138, 43, 226)
            },
            ["Pink"] = new Theme
            {
                Name = "Pink",
                BackColor = Color.FromArgb(40, 30, 35),
                ControlBackColor = Color.FromArgb(30, 20, 25),
                ButtonBackColor = Color.FromArgb(60, 40, 50),
                MenuBackColor = Color.FromArgb(55, 35, 45),
                AccentColor = Color.FromArgb(255, 20, 147)
            },
            ["Green"] = new Theme
            {
                Name = "Green",
                BackColor = Color.FromArgb(30, 40, 30),
                ControlBackColor = Color.FromArgb(20, 30, 20),
                ButtonBackColor = Color.FromArgb(40, 60, 40),
                MenuBackColor = Color.FromArgb(35, 55, 35),
                AccentColor = Color.FromArgb(34, 139, 34)
            },
            ["Red"] = new Theme
            {
                Name = "Red",
                BackColor = Color.FromArgb(40, 30, 30),
                ControlBackColor = Color.FromArgb(30, 20, 20),
                ButtonBackColor = Color.FromArgb(60, 40, 40),
                MenuBackColor = Color.FromArgb(55, 35, 35),
                AccentColor = Color.FromArgb(178, 34, 34)
            },
            ["Dark Yellow"] = new Theme
            {
                Name = "Dark Yellow",
                BackColor = Color.FromArgb(40, 38, 30),
                ControlBackColor = Color.FromArgb(30, 28, 20),
                ButtonBackColor = Color.FromArgb(60, 55, 40),
                MenuBackColor = Color.FromArgb(55, 50, 35),
                AccentColor = Color.FromArgb(184, 134, 11)
            },
            ["Orange"] = new Theme
            {
                Name = "Orange",
                BackColor = Color.FromArgb(45, 40, 35),
                ControlBackColor = Color.FromArgb(35, 30, 25),
                ButtonBackColor = Color.FromArgb(65, 55, 45),
                MenuBackColor = Color.FromArgb(60, 50, 40),
                AccentColor = Color.FromArgb(255, 140, 0)
            },
            ["Blue"] = new Theme
            {
                Name = "Blue",
                BackColor = Color.FromArgb(30, 35, 45),
                ControlBackColor = Color.FromArgb(20, 25, 35),
                ButtonBackColor = Color.FromArgb(40, 50, 70),
                MenuBackColor = Color.FromArgb(35, 45, 60),
                AccentColor = Color.FromArgb(30, 144, 255)
            },
            ["Teal"] = new Theme
            {
                Name = "Teal",
                BackColor = Color.FromArgb(30, 40, 40),
                ControlBackColor = Color.FromArgb(20, 30, 30),
                ButtonBackColor = Color.FromArgb(40, 60, 60),
                MenuBackColor = Color.FromArgb(35, 55, 55),
                AccentColor = Color.FromArgb(0, 180, 180)
            },
            ["Grey and Black"] = new Theme
            {
                Name = "Grey and Black",
                BackColor = Color.Black,
                ControlBackColor = Color.FromArgb(20, 20, 20),
                ButtonBackColor = Color.FromArgb(40, 40, 40),
                AccentColor = Color.FromArgb(100, 100, 100),
                MenuBackColor = Color.FromArgb(30, 30, 30),
                BorderColor = Color.FromArgb(60, 60, 60)
            },
            ["Custom"] = new Theme { Name = "Custom" }
        };

        public static List<string> GetAvailableThemes() => new List<string>(Themes.Keys);

        public static void SetTheme(string themeName)
        {
            if (Themes.TryGetValue(themeName, out var theme))
            {
                if (themeName == "Custom")
                {
                    ReloadCustomTheme(SettingsManager.Settings.CustomTheme);
                }
                CurrentTheme = theme;
            }
            else
            {
                CurrentTheme = Themes["Default"];
            }
            
            // Update renderer
            ToolStripManager.Renderer = new DynamicThemeMenuRenderer(new DynamicThemeColorTable());
        }

        public static void ReloadCustomTheme(SerializableTheme settings)
        {
            if (!Themes.ContainsKey("Custom")) return;
            var t = Themes["Custom"];
            t.BackColor = Color.FromArgb(settings.BackColor);
            t.ForeColor = Color.FromArgb(settings.ForeColor);
            t.ControlBackColor = Color.FromArgb(settings.ControlBackColor);
            t.ControlForeColor = Color.FromArgb(settings.ControlForeColor);
            t.ButtonBackColor = Color.FromArgb(settings.ButtonBackColor);
            t.ButtonForeColor = Color.FromArgb(settings.ButtonForeColor);
            t.AccentColor = Color.FromArgb(settings.AccentColor);
            t.PlayButtonColor = settings.PlayButtonColor.HasValue ? Color.FromArgb(settings.PlayButtonColor.Value) : null;
            t.BorderColor = Color.FromArgb(settings.BorderColor);
            t.MenuBackColor = Color.FromArgb(settings.MenuBackColor);
            t.MenuForeColor = Color.FromArgb(settings.MenuForeColor);
        }

        public static void ApplyTheme(Form form)
        {
            form.BackColor = CurrentTheme.BackColor;
            form.ForeColor = CurrentTheme.ForeColor;

            ApplyThemeToControls(form.Controls);
        }

        public static void ApplyTheme(Control control)
        {
            UpdateControl(control);
            if (control.HasChildren)
            {
                ApplyThemeToControls(control.Controls);
            }
        }

        private static void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                UpdateControl(c);
                if (c.HasChildren)
                {
                    ApplyThemeToControls(c.Controls);
                }
            }
        }

        private static void UpdateControl(Control c)
        {
            if (c.Tag is string tag && tag == "ColorSwatch") return;

            if (c is ModCardControl)
            {
                c.BackColor = CurrentTheme.ButtonBackColor;
                c.ForeColor = CurrentTheme.ButtonForeColor;
            }
            else if (c is Button btn)
            {
                if (btn.FlatStyle == FlatStyle.Flat)
                {
                    // Heuristic for action buttons
                    if (btn.Name == "btnPlay")
                    {
                         btn.BackColor = CurrentTheme.PlayButtonColor ?? CurrentTheme.AccentColor;
                         btn.ForeColor = Color.White; 
                    }
                    else if (btn.Name == "btnSave" || btn.Name == "btnOk" || btn.Name == "btnYes" || btn.Name == "btnDownload")
                    {
                         btn.BackColor = CurrentTheme.AccentColor;
                         btn.ForeColor = Color.White; 
                    }
                    else
                    {
                        btn.BackColor = CurrentTheme.ButtonBackColor;
                        btn.ForeColor = CurrentTheme.ButtonForeColor;
                    }
                }
            }
            else if (c is TextBox || c is ListBox || c is ComboBox || c is RichTextBox || c is CheckedListBox || c is ListView || c is TreeView || c is DataGridView)
            {
                c.BackColor = CurrentTheme.ControlBackColor;
                c.ForeColor = CurrentTheme.ControlForeColor;
            }
            else if (c is Panel || c is GroupBox || c is TabPage || c is SplitContainer || c is FlowLayoutPanel || c is TableLayoutPanel || c is UserControl)
            {
                c.BackColor = CurrentTheme.BackColor;
                c.ForeColor = CurrentTheme.ForeColor;
            }
            else if (c is Label || c is CheckBox || c is RadioButton || c is LinkLabel)
            {
                c.ForeColor = CurrentTheme.ForeColor;
            }
            else if (c is MenuStrip menu)
            {
                menu.BackColor = CurrentTheme.MenuBackColor;
                menu.ForeColor = CurrentTheme.MenuForeColor;
            }
            else if (c is StatusStrip status)
            {
                status.BackColor = CurrentTheme.AccentColor;
                status.ForeColor = Color.White;
            }
        }
    }
}

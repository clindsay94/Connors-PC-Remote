namespace CPCRemote.UI.Helpers
{
    using System.Runtime.Versioning;
    using System.Text.Json;
    using System.Threading;
    using Microsoft.UI.Xaml;
    using Windows.UI;
    using System.IO; // Ensure this is included

    /// <summary>
    /// Helper class for managing application themes
    /// </summary>
    [SupportedOSPlatform("windows10.0.17763.0")]
    internal static class ThemeHelper
    {
        /// <summary>
        /// Available theme modes
        /// </summary>
        public enum ThemeMode
        {
            System = 0,
            Light = 1,
            Dark = 2,
        }

        private static ThemeMode _current = ThemeMode.System;

        // Add this lock object for thread safety
        private static readonly object _lock = new();

        /// <summary>
        /// Event raised when theme changes
        /// </summary>
        public static event EventHandler<ThemeMode>? ThemeChanged;

        /// <summary>
        /// Path to store theme settings
        /// </summary>
        private static string SettingsFilePath
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CPCRemote"
                );
                try
                {
                    if (!Directory.Exists(folder))
                    {
                        _ = Directory.CreateDirectory(folder);
                    }
                }
                catch
                {
                    // Ignore directory creation failures
                }
                return Path.Combine(folder, "theme.json");
            }
        }

        /// <summary>
        /// Gets the current theme mode (thread-safe)
        /// </summary>
        public static ThemeMode Current
        {
            get
            {
                lock (_lock)
                {
                    return _current;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _current = value;
                }
            }
        }

        /// <summary>
        /// Sets the theme, persists it, and notifies listeners
        /// </summary>
        /// <param name="theme">The theme mode to set</param>
        public static void SetTheme(ThemeMode theme)
        {
            bool changed;
            lock (_lock)
            {
                changed = _current != theme;
                _current = theme;
            }

            if (changed)
            {
                try
                {
                    Save();
                }
                catch
                {
                    // Ignore save errors
                }
                ThemeChanged?.Invoke(null, theme);
            }
        }

        /// <summary>
        /// Loads theme settings from storage
        /// </summary>
        [SupportedOSPlatform("windows10.0.17763.0")]
        public static void Load()
        {
            try
            {
                string path = SettingsFilePath;
                if (!File.Exists(path))
                    return;

                string json = File.ReadAllText(path);
                ThemeSettings? settings = JsonSerializer.Deserialize<ThemeSettings?>(json);
                if (settings is not null)
                {
                    SetTheme(settings.Mode);
                }
            }
            catch
            {
                // Ignore errors; leave current theme as-is
            }
        }

        /// <summary>
        /// Cached JSON serializer options
        /// </summary>
        private static readonly JsonSerializerOptions _cachedJsonOptions = new()
        {
            WriteIndented = true,
        };

        /// <summary>
        /// Saves current theme to disk
        /// </summary>
        public static void Save()
        {
            try
            {
                string path = SettingsFilePath;
                ThemeSettings settings = new() { Mode = Current };
                string json = JsonSerializer.Serialize(settings, _cachedJsonOptions);
                File.WriteAllText(path, json);
            }
            catch
            {
                // Swallow exceptions to avoid crashing callers
            }
        }

        /// <summary>
        /// Gets light theme color resources
        /// </summary>
        /// <returns>Light theme resource dictionary</returns>
        public static ResourceDictionary GetLightColors()
        {
            return new ResourceDictionary
            {
                { "PrimaryColor", Color.FromArgb(255, 103, 80, 164) },
                { "OnPrimaryColor", Color.FromArgb(255, 255, 255, 255) },
                { "BackgroundColor", Color.FromArgb(255, 250, 250, 250) },
                { "OnBackgroundColor", Color.FromArgb(255, 0, 0, 0) },
                { "SurfaceColor", Color.FromArgb(255, 255, 255, 255) },
                { "OnSurfaceColor", Color.FromArgb(255, 0, 0, 0) },
                { "ErrorColor", Color.FromArgb(255, 176, 0, 32) },
            };
        }

        /// <summary>
        /// Gets dark theme color resources
        /// </summary>
        /// <returns>Dark theme resource dictionary</returns>
        public static ResourceDictionary GetDarkColors()
        {
            return new ResourceDictionary
            {
                { "PrimaryColor", Color.FromArgb(255, 147, 125, 214) },
                { "OnPrimaryColor", Color.FromArgb(255, 255, 255, 255) },
                { "BackgroundColor", Color.FromArgb(255, 32, 32, 32) },
                { "OnBackgroundColor", Color.FromArgb(255, 230, 230, 230) },
                { "SurfaceColor", Color.FromArgb(255, 18, 18, 18) },
                { "OnSurfaceColor", Color.FromArgb(255, 230, 230, 230) },
                { "ErrorColor", Color.FromArgb(255, 255, 82, 82) },
            };
        }

        /// <summary>
        /// Applies theme to the application
        /// </summary>
        /// <param name="theme">Theme name (Light, Dark, or System)</param>
        public static void ApplyTheme()
        {
            // Note: In WinUI 3, theme application is typically handled at the app level
            // This method is kept for compatibility but may not be fully functional
            // without proper main window reference
        }

        /// <summary>
        /// Applies accent color to the application
        /// </summary>
        /// <param name="color">Accent color to apply</param>
        public static void ApplyAccentColor(Color color)
        {
            try
            {
                if (Application.Current?.Resources?.ThemeDictionaries == null)
                    return;

                if (Application.Current.Resources.ThemeDictionaries.TryGetValue("Light", out object? lightObj) 
                    && lightObj is ResourceDictionary lightTheme)
                {
                    lightTheme.MergedDictionaries.Clear();
                    lightTheme.MergedDictionaries.Add(GetLightColors());
                    lightTheme["AccentColor"] = color;
                }

                if (Application.Current.Resources.ThemeDictionaries.TryGetValue("Dark", out object? darkObj) 
                    && darkObj is ResourceDictionary darkTheme)
                {
                    darkTheme.MergedDictionaries.Clear();
                    darkTheme.MergedDictionaries.Add(GetDarkColors());
                    darkTheme["AccentColor"] = color;
                }
            }
            catch
            {
                // Ignore accent color application errors
            }
        }
    }

    /// <summary>
    /// Settings class for theme persistence
    /// </summary>
    [SupportedOSPlatform("windows10.0.17763.0")]
    internal class ThemeSettings
    {
        /// <summary>
        /// Gets or sets the theme mode
        /// </summary>
        public ThemeHelper.ThemeMode Mode { get; set; } = ThemeHelper.ThemeMode.System;
    }
}

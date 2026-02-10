namespace CPCRemote.UI.Services;

using System;
using System.Collections.Generic;
using Windows.UI;

/// <summary>
/// Centralized service for managing sensor category colors.
/// Loads user-customized colors from settings and provides defaults.
/// Fires <see cref="ColorsChanged"/> when any color is updated so
/// dashboard widgets can refresh.
/// </summary>
public sealed class CategoryColorService
{
    private readonly SettingsService _settings;
    private readonly Dictionary<string, Color> _colors = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Raised when any category color changes.</summary>
    public event EventHandler? ColorsChanged;

    /// <summary>All known categories.</summary>
    public static readonly string[] AllCategories =
        ["CPU", "GPU", "Memory", "Motherboard", "Storage", "Cooling", "Network", "Other"];

    /// <summary>Default category colors (matches original hardcoded values).</summary>
    public static readonly Dictionary<string, Color> Defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CPU"]         = Color.FromArgb(255, 255, 107, 107),
        ["GPU"]         = Color.FromArgb(255,   0, 230, 118),
        ["Memory"]      = Color.FromArgb(255,  92, 107, 192),
        ["Motherboard"] = Color.FromArgb(255, 255, 193,   7),
        ["Storage"]     = Color.FromArgb(255, 121, 134, 203),
        ["Cooling"]     = Color.FromArgb(255,  77, 208, 225),
        ["Network"]     = Color.FromArgb(255, 224,  64, 251),
        ["Other"]       = Color.FromArgb(255, 144, 164, 174),
    };

    public CategoryColorService(SettingsService settings)
    {
        _settings = settings;
        LoadColors();
    }

    /// <summary>Gets the color for a given category.</summary>
    public Color GetColor(string category)
    {
        if (_colors.TryGetValue(category, out var c)) return c;
        if (Defaults.TryGetValue(category, out var d)) return d;
        return Defaults["Other"];
    }

    /// <summary>Sets a category color and persists it.</summary>
    public void SetColor(string category, Color color)
    {
        _colors[category] = color;
        _settings.Set($"CategoryColor_{category}", ColorToHex(color));
        ColorsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Resets all category colors to defaults.</summary>
    public void ResetAll()
    {
        foreach (var cat in AllCategories)
        {
            _colors[cat] = Defaults[cat];
            _settings.Set($"CategoryColor_{cat}", string.Empty);
        }
        ColorsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadColors()
    {
        foreach (var cat in AllCategories)
        {
            var hex = _settings.Get($"CategoryColor_{cat}", string.Empty);
            if (!string.IsNullOrEmpty(hex) && TryParseHex(hex, out var color))
            {
                _colors[cat] = color;
            }
            else
            {
                _colors[cat] = Defaults[cat];
            }
        }
    }

    private static string ColorToHex(Color c)
        => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    private static bool TryParseHex(string hex, out Color color)
    {
        color = default;
        if (string.IsNullOrEmpty(hex)) return false;

        hex = hex.TrimStart('#');

        try
        {
            byte a = 255, r, g, b;
            if (hex.Length == 8)
            {
                a = Convert.ToByte(hex[..2], 16);
                r = Convert.ToByte(hex[2..4], 16);
                g = Convert.ToByte(hex[4..6], 16);
                b = Convert.ToByte(hex[6..8], 16);
            }
            else if (hex.Length == 6)
            {
                r = Convert.ToByte(hex[..2], 16);
                g = Convert.ToByte(hex[2..4], 16);
                b = Convert.ToByte(hex[4..6], 16);
            }
            else return false;

            color = Color.FromArgb(a, r, g, b);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

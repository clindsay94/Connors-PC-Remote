using CPCRemote.UI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace CPCRemote.UI.Services
{
    public class SettingsService
    {
        private const string ServiceConfigFileName = "service-settings.json";
        private const string AppSettingsFileName = "app-settings.json";
        private readonly ApplicationDataContainer? _localSettings;
        private readonly bool _isPackaged;
        private readonly string _localAppDataPath;

        public SettingsService()
        {
            _isPackaged = IsPackaged();
            if (_isPackaged)
            {
                try
                {
                    _localSettings = ApplicationData.Current.LocalSettings;
                    _localAppDataPath = ApplicationData.Current.LocalFolder.Path;
                }
                catch
                {
                    // Fallback if ApplicationData.Current fails despite IsPackaged() returning true
                    _isPackaged = false;
                    _localSettings = null;
                    _localAppDataPath = GetUnpackagedAppDataPath();
                    Directory.CreateDirectory(_localAppDataPath);
                }
            }
            else
            {
                _localAppDataPath = GetUnpackagedAppDataPath();
                Directory.CreateDirectory(_localAppDataPath);
            }
        }

        private string GetUnpackagedAppDataPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CPCRemote");
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);

        private bool IsPackaged()
        {
            try
            {
                int length = 0;
                // APPMODEL_ERROR_NO_PACKAGE = 15700
                return GetCurrentPackageFullName(ref length, null) != 15700;
            }
            catch
            {
                return false;
            }
        }

        public void Set<T>(string key, T value)
        {
            if (_isPackaged && _localSettings != null)
            {
                _localSettings.Values[key] = value;
            }
            else
            {
                var settings = LoadUnpackagedSettings();
                if (value == null)
                {
                    settings.Remove(key);
                }
                else
                {
                    settings[key] = JsonSerializer.SerializeToElement(value);
                }
                SaveUnpackagedSettings(settings);
            }
        }

        public T Get<T>(string key, T defaultValue)
        {
            if (_isPackaged && _localSettings != null)
            {
                if (_localSettings.Values.TryGetValue(key, out object? value) && value != null)
                {
                    return (T)value;
                }
            }
            else
            {
                var settings = LoadUnpackagedSettings();
                if (settings.TryGetValue(key, out var value))
                {
                    try
                    {
                        return value.Deserialize<T>() ?? defaultValue;
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
            }
            return defaultValue;
        }

        public async Task<ServiceConfiguration?> LoadServiceConfigurationAsync()
        {
            if (_isPackaged)
            {
                var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(ServiceConfigFileName);
                if (item is not StorageFile file)
                {
                    return null;
                }

                var json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize<ServiceConfiguration>(json);
            }
            else
            {
                var path = Path.Combine(_localAppDataPath, ServiceConfigFileName);
                if (!File.Exists(path))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<ServiceConfiguration>(json);
            }
        }

        public async Task SaveServiceConfigurationAsync(ServiceConfiguration config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

            if (_isPackaged)
            {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(ServiceConfigFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
            }
            else
            {
                var path = Path.Combine(_localAppDataPath, ServiceConfigFileName);
                await File.WriteAllTextAsync(path, json);
            }
        }

        private Dictionary<string, JsonElement> LoadUnpackagedSettings()
        {
            var path = Path.Combine(_localAppDataPath, AppSettingsFileName);
            if (!File.Exists(path))
            {
                return new Dictionary<string, JsonElement>();
            }

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new Dictionary<string, JsonElement>();
            }
            catch
            {
                return new Dictionary<string, JsonElement>();
            }
        }

        private void SaveUnpackagedSettings(Dictionary<string, JsonElement> settings)
        {
            var path = Path.Combine(_localAppDataPath, AppSettingsFileName);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}

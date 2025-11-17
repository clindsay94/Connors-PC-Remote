using CPCRemote.UI.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace CPCRemote.UI.Services
{
    public class SettingsService
    {
        private const string ServiceConfigFileName = "service-settings.json";
        private readonly ApplicationDataContainer _localSettings;

        public SettingsService()
        {
            _localSettings = ApplicationData.Current.LocalSettings;
        }

        public void Set<T>(string key, T value)
        {
            _localSettings.Values[key] = value;
        }

        public T Get<T>(string key, T defaultValue)
        {
            if (_localSettings.Values.TryGetValue(key, out object? value) && value != null)
            {
                return (T)value;
            }
            return defaultValue;
        }

        public async Task<ServiceConfiguration?> LoadServiceConfigurationAsync()
        {
            var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(ServiceConfigFileName) as StorageFile;
            if (file == null)
            {
                return null;
            }

            var json = await FileIO.ReadTextAsync(file);
            return JsonSerializer.Deserialize<ServiceConfiguration>(json);
        }

        public async Task SaveServiceConfigurationAsync(ServiceConfiguration config)
        {
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(ServiceConfigFileName, CreationCollisionOption.ReplaceExisting);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await FileIO.WriteTextAsync(file, json);
        }
    }
}

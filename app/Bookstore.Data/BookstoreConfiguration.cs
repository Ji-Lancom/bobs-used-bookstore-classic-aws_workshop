using System;
using System.Collections.Generic;

namespace BobsBookstoreClassic.Data
{
    public sealed class BookstoreConfiguration
    {
        private static readonly Lazy<BookstoreConfiguration> Lazy = new Lazy<BookstoreConfiguration>(() => new BookstoreConfiguration());

        private static BookstoreConfiguration Instance => Lazy.Value;

        private readonly Dictionary<string, string> _appSettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _connectionStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private BookstoreConfiguration() { }

        public static void Initialize(IReadOnlyDictionary<string, string?> appSettings, IReadOnlyDictionary<string, string?> connectionStrings)
        {
            foreach (var kvp in appSettings)
            {
                if (kvp.Value != null)
                    Instance._appSettings[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in connectionStrings)
            {
                if (kvp.Value != null)
                    Instance._connectionStrings[kvp.Key] = kvp.Value;
            }
        }

        public static void AddSetting(string key, string value)
        {
            Instance._appSettings[key] = value;
        }

        public static bool TryGetSetting(string key, out string? value)
        {
            return Instance._appSettings.TryGetValue(key, out value);
        }

        public static string GetSetting(string key)
        {
            if (Instance._appSettings.TryGetValue(key, out var value))
                return value;
            return string.Empty;
        }

        public static T GetSetting<T>(string key)
        {
            var value = GetSetting(key);
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static void AddConnectionString(string key, string value)
        {
            Instance._connectionStrings[key] = value;
        }

        public static string GetConnectionString(string key)
        {
            if (Instance._connectionStrings.TryGetValue(key, out var value))
                return value;
            return string.Empty;
        }
    }
}

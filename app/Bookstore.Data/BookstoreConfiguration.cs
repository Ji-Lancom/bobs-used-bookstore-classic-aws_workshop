using System;
using System.Collections.Generic;
#if NET5_0_OR_GREATER
using Microsoft.Extensions.Configuration;
#endif

namespace BobsBookstoreClassic.Data
{
    public sealed class BookstoreConfiguration
    {
        private static readonly Lazy<BookstoreConfiguration> Lazy = new Lazy<BookstoreConfiguration>(() => new BookstoreConfiguration());

        private static BookstoreConfiguration Instance => Lazy.Value;

        private readonly Dictionary<string, string> _appSettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _connectionStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private BookstoreConfiguration() { }

        /// <summary>
        /// Initializes BookstoreConfiguration from an IConfiguration instance (ASP.NET Core).
        /// Call this at application startup before any other configuration access.
        /// IConfiguration keys use ':' as separator; these are normalized to '/' to match legacy usage.
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            // Load all key/value pairs, normalizing ':' to '/' for legacy key format compatibility
            // e.g. "Services:Authentication" in IConfiguration -> "Services/Authentication"
            foreach (var item in configuration.AsEnumerable())
            {
                if (item.Value != null)
                {
                    var normalizedKey = item.Key.Replace(":", "/");
                    Instance._appSettings[normalizedKey] = item.Value;
                }
            }

            // Load connection strings from the standard ConnectionStrings section
            foreach (var cs in configuration.GetSection("ConnectionStrings").GetChildren())
            {
                if (cs.Value != null)
                    Instance._connectionStrings[cs.Key] = cs.Value;
            }
        }

        public static void AddSetting(string key, string value)
        {
            Instance._appSettings[key] = value;
        }

        public static string GetSetting(string key)
        {
            // Also check environment variables at call time (supports runtime override)
            var envValue = Environment.GetEnvironmentVariable(key);
            if (envValue != null) return envValue;

            Instance._appSettings.TryGetValue(key, out var result);
            return result ?? string.Empty;
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
            Instance._connectionStrings.TryGetValue(key, out var result);
            return result ?? string.Empty;
        }
    }
}

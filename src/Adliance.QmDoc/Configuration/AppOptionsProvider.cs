using System;
using System.IO;
using System.Text.Json;

namespace Adliance.QmDoc.Configuration
{
    public static class AppOptionsProvider
    {
        public static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".qmdoc");
        private static string AppOptionsFilePath => Path.Combine(DataDirectory, "options.json");

        private static void EnsureDataDirectoryExists()
        {
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }
        }

        public static AppOptions LoadOptions()
        {
            if (File.Exists(AppOptionsFilePath))
            {
                return JsonSerializer.Deserialize<AppOptions>(File.ReadAllText(AppOptionsFilePath));
            }

            return new AppOptions();
        }

        public static void StoreOptions(AppOptions options)
        {
            EnsureDataDirectoryExists();
            File.WriteAllText(AppOptionsFilePath, JsonSerializer.Serialize(options, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
    }
}
using System;
using System.IO;
using System.Text.Json;

namespace Adliance.QmDoc.Options;

public static class OptionsProvider
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

    public static Options LoadOptions()
    {
        if (File.Exists(AppOptionsFilePath))
        {
            return JsonSerializer.Deserialize<Options>(File.ReadAllText(AppOptionsFilePath)) ?? new Options();
        }

        return new Options();
    }

    public static void StoreOptions(Options options)
    {
        EnsureDataDirectoryExists();
        File.WriteAllText(AppOptionsFilePath, JsonSerializer.Serialize(options, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}
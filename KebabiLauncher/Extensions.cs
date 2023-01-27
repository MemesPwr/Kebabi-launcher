using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.Win32;

namespace KebabiLauncher;

internal static class Extensions
{
    public static void SaveToJson<TValue>(this TValue value, string path, bool prettyPrint = false)
    {
        var jsonRaw = JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = prettyPrint });
        File.WriteAllText(path, jsonRaw);
    }

    [SupportedOSPlatform("windows")]
    public static RegistryKey? OpenKeyByPath(this RegistryKey baseKey, params string[] keys)
        => AppUtility.OpenKeyByPath(baseKey, keys: keys);
}
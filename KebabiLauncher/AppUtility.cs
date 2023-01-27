using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Text.Json;
using KebabiLauncher.Models;
using Microsoft.Win32;

namespace KebabiLauncher;

internal static class AppUtility
{
    public static bool ConsoleConfirm(string question, bool yesByDefault = true)
    {
        Console.Write($"{question} [{(yesByDefault ? "Y/n" : "y/N")}]: ");
        while (true)
        {
            var input = Console.ReadKey(true);
            if (input.Key is ConsoleKey.Y)
            {
                Console.WriteLine("Y");
                return true;
            }

            if (input.Key is ConsoleKey.N)
            {
                Console.WriteLine("N");
                return false;
            }

            if (input.Key is ConsoleKey.Enter)
            {
                Console.WriteLine(yesByDefault ? "Y" : "N");
                return yesByDefault;
            }
        }
    }

    public static bool ReadJsonFile<T>(string path, [NotNullWhen(true)] out T? value)
    {
        var jsonRaw = File.ReadAllText(path);
        value = JsonSerializer.Deserialize<T>(jsonRaw);
        return value is not null;
    }

    [SupportedOSPlatform("windows")]
    public static RegistryKey? OpenKeyByPath(RegistryKey baseKey, params string[] keys)
    {
        RegistryKey? currentKey = baseKey;
        foreach (var keyName in keys)
        {
            currentKey = currentKey.OpenSubKey(keyName, true);
            if (currentKey is null)
            {
                Console.WriteLine($"Failed to open key \"{keyName}\"");
                break;
            }
        }

        return currentKey;
    }

    public static void FixRegistryAccess(RegistryKey key)
    {
        var currentUserStr = Environment.UserDomainName + "\\" + Environment.UserName;
        
        Console.WriteLine($"Going to fix access on key \"{key.Name}\" for user \"{currentUserStr}\"...");

        var rs = key.GetAccessControl(AccessControlSections.All);
        rs.AddAccessRule(new RegistryAccessRule(currentUserStr, RegistryRights.WriteKey | RegistryRights.ReadKey | RegistryRights.Delete | RegistryRights.FullControl, AccessControlType.Allow));
        key.SetAccessControl(rs);
    }
}
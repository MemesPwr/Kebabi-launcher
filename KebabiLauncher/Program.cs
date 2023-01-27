using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using KebabiLauncher;
using KebabiLauncher.Models;
using Microsoft.Win32;

internal class Program
{
    private const string DefaultAccountName = "default";

    private static readonly string WorkingDirectory = Environment.CurrentDirectory;
    private static readonly string AccountsDirectory = Path.Combine(WorkingDirectory, "accounts");
    
    private static readonly string KebabiDlcPath = Path.Combine(WorkingDirectory, "AkebiLauncher.exe");
    private static readonly string AppConfigFilePath = Path.Combine(WorkingDirectory, $"{nameof(KebabiLauncher)}.config.json");

    private static readonly string TextRegGayOrgName = Encoding.UTF8.GetString(new byte[] { 0x6D, 0x69, 0x48, 0x6F, 0x59, 0x6F }); // fck u ho%%%yo
    private static readonly string TextRegCertainGameName = Encoding.UTF8.GetString(new byte[] { 0x47, 0x65, 0x6E, 0x73, 0x68, 0x69, 0x6E, 0x20, 0x49, 0x6D, 0x70, 0x61, 0x63, 0x74 });
    private static readonly string TextCertainAnimeGameProcName = Encoding.UTF8.GetString(new byte[] { 0x47, 0x65, 0x6E, 0x73, 0x68, 0x69, 0x6E, 0x49, 0x6D, 0x70, 0x61, 0x63, 0x74 });

    public static void Main(string[] args)
    {
        try
        {
            MainUnsafe(args);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    private static void MainUnsafe(string[] args)
    {
        Console.WriteLine($"Args: [{string.Join(' ', args)}]");

        if (!File.Exists(KebabiDlcPath))
        {
            Console.WriteLine($"Failed to find kebabi prime launcher in \"{WorkingDirectory}\"");
            return;
        }
        
        if (!LoadAppConfig(out var appConfig))
            return;

        if (!Directory.Exists(AccountsDirectory))
            Directory.CreateDirectory(AccountsDirectory);

        var accountName = args.FirstOrDefault() ?? DefaultAccountName;
        if (!LoadProfileData(appConfig, accountName, out var profileConfigPath, out var isDefaultProfile, out var isNewProfile, out var profile))
            return;

        var isNewDefault = isDefaultProfile && isNewProfile;

        var certainAnimeGameReg = Registry.CurrentUser.OpenKeyByPath("Software", TextRegGayOrgName, TextRegCertainGameName);
        if (certainAnimeGameReg is null)
            return;

        var skipRegDataInit = false;
        if (!profile.Data.Any() && !isNewProfile)
        {
            Console.WriteLine("It looks that something went wrong at shutting down last time.");
            var useLastData = AppUtility.ConsoleConfirm("Should we use last profile data?");
            if (useLastData)
            {
                skipRegDataInit = true;
            }
            else if (AppUtility.ConsoleConfirm("But it will wipe login information for last used account. Are you sure?", false))
            {
                Console.WriteLine("Going to wipe account data...");
            }
        }

        var values = certainAnimeGameReg.GetValueNames();
        if (!skipRegDataInit)
        {
            foreach (var valueName in values)
            {
                if (isNewDefault)
                    profile.Data[valueName] = new RegValue(certainAnimeGameReg.GetValue(valueName), certainAnimeGameReg.GetValueKind(valueName));
                else
                    certainAnimeGameReg.DeleteValue(valueName);
            }

            if (isNewDefault)
            {
                SaveProfileData(appConfig, profile, profileConfigPath);
            }
            else
            {
                foreach (var (valueName, value) in profile.Data)
                {
                    var data = GetRegValue(appConfig, valueName, value);
                    if (data is null)
                        continue;

                    certainAnimeGameReg.SetValue(valueName, data, value.Kind);
                }
            }
        }

        Console.WriteLine("Going to launch kebabi prime...");
        var kebabiProcess = Process.Start(KebabiDlcPath, appConfig.KebabiArgs);
        kebabiProcess.WaitForExit();
        Console.WriteLine("GLHF");

        var procWasFound = false;
        var wtfShown = false;
        Console.WriteLine("Waiting for certain anime game process...");
        for (var i = 0; i < appConfig.AnimeGameWaitingTime; i++)
        {
            var processes = Process.GetProcessesByName(TextCertainAnimeGameProcName);
            switch (processes.Length)
            {
                case < 1: continue;
                case > 1:
                    if (!wtfShown)
                    {
                        wtfShown = true;
                        Console.WriteLine("HOW TF YOU GET LAUNCHED THIS SHIT MORE THAN ONE TIME?! DM me 'how to' in a Discord: !root#7334");
                        Console.WriteLine("Well... waiting till one of this will be closed...");
                    }

                    i--;
                    continue;
            }

            procWasFound = true;
            var animeGameProc = processes.First();

            Console.WriteLine($"Found! PID: {animeGameProc.Id}. Waiting for it exit. Enjoy the game. I'll do some shit behind the scenes after you done.");
            animeGameProc.WaitForExit();
        }

        if (!procWasFound)
        {
            Console.WriteLine("Failed to find certain anime game process.");
            return;
        }

        Console.WriteLine("Saving account data...");
        values = certainAnimeGameReg.GetValueNames();
        foreach (var valueName in values)
        {
            profile.Data[valueName] = new RegValue(certainAnimeGameReg.GetValue(valueName), certainAnimeGameReg.GetValueKind(valueName));
        }
        SaveProfileData(appConfig, profile, profileConfigPath);

        if (appConfig.DoCheatConfigBackup)
            DoCheatConfigsBackup();

        appConfig.SaveToJson(AppConfigFilePath, true);
        Console.WriteLine("Bye-bye...");
    }

    private static bool LoadAppConfig([NotNullWhen(true)] out AppConfig? appConfig)
    {
        appConfig = default;

        if (!File.Exists(AppConfigFilePath))
            new AppConfig(new ResolutionOverride()).SaveToJson(AppConfigFilePath, true);

        if (AppUtility.ReadJsonFile(AppConfigFilePath, out appConfig))
            return true;

        Console.WriteLine($"Failed to read app config \"{AppConfigFilePath}\"!");
        return false;
    }

    private static bool LoadProfileData(AppConfig appConfig, string accountName, out string profileConfigPath, out bool isDefault, out bool isNewProfile, [NotNullWhen(true)] out UserData? profileData)
    {
        isDefault = accountName.Equals(DefaultAccountName, StringComparison.InvariantCulture);
        isNewProfile = false;
        profileData = default;
        
        profileConfigPath = Path.Combine(AccountsDirectory, $"{accountName}.json");
        if (!Path.Exists(profileConfigPath))
        {
            if (!isDefault && !AppUtility.ConsoleConfirm($"Account \"{accountName}\" does not exists. Do you wanna add new?"))
            {
                Console.WriteLine("Wel... Bye..");
                return false;
            }

            isNewProfile = true;
            Console.WriteLine("Creating empty account data...");
            new UserData().SaveToJson(profileConfigPath, appConfig.PrettyPrintConfigs);
        }

        Console.WriteLine($"Loading account \"{accountName}\" data...");
        if (AppUtility.ReadJsonFile(profileConfigPath, out profileData))
            return true;

        Console.WriteLine("Failed to read account data!");
        return false;
    }

    private static void SaveProfileData(AppConfig appConfig, UserData profileConfig, string profileConfigPath)
        => profileConfig.SaveToJson(profileConfigPath, appConfig.PrettyPrintConfigs);

    private static object? GetRegValue(AppConfig appConfig, string valueName, RegValue value)
    {
        if (appConfig.ResolutionOverride.Enabled && valueName.StartsWith("Screenmanager "))
        {
            if (valueName.Contains(" Is Fullscreen mode_"))
                return appConfig.ResolutionOverride.FullScreen ? 1 : 0;

            if (valueName.Contains(" Resolution Height_"))
                return appConfig.ResolutionOverride.Height;

            if (valueName.Contains(" Resolution Width_"))
                return appConfig.ResolutionOverride.Width;
        }

        if (value.Data is not JsonElement jData)
            return default;

        switch (value.Kind)
        {
            case RegistryValueKind.MultiString:
                if (jData.ValueKind is JsonValueKind.Array)
                    return jData.EnumerateArray()
                        .Where(x => x.ValueKind is JsonValueKind.String)
                        .Select(x => x.GetString() ?? string.Empty)
                        .ToArray();

                return default;
            case RegistryValueKind.ExpandString:
            case RegistryValueKind.String:
                return jData.ValueKind is JsonValueKind.String ? jData.GetString() : default(object?);
            case RegistryValueKind.Binary:
            {
                if (jData.ValueKind is not JsonValueKind.String)
                    return default;

                var base64 = jData.GetString();
                if (string.IsNullOrEmpty(base64))
                    return default;

                //Console.WriteLine($"\"{valueName}\" is binary type... But it should be a base64 string...");
                var bytesData = Convert.FromBase64String(base64);
                //Console.WriteLine($"\"{valueName}\":\n{new string('=', 10)}\n{Encoding.Default.GetString(bytesData)}\n{new string('=', 10)}");

                return bytesData;
            }
            case RegistryValueKind.DWord:
                return jData.ValueKind is JsonValueKind.Number ? jData.GetInt32() : default(object?);
            case RegistryValueKind.QWord:
                return jData.ValueKind is JsonValueKind.Number ? jData.GetInt64() : default(object?);
            case RegistryValueKind.None:
            case RegistryValueKind.Unknown:
            default: return default;
        }
    }

    private static void DoCheatConfigsBackup()
    {
        Console.WriteLine("Backing up cheat config...");

        var currentDlcCfgJson = Path.Combine(WorkingDirectory, "cfg.json");
        var currentDlcCfgIni = Path.Combine(WorkingDirectory, "cfg.ini");
        var backupDirName = $"{DateTime.Now:dd-MM-yyyy HH-mm}";
        var backupsDir = Path.Combine(WorkingDirectory, "KebabiCfgBackups", backupDirName);
        if (Directory.Exists(backupsDir))
        {
            Console.WriteLine($"Wait a minute... \"{backupsDir}\" is exists...");
            backupDirName += $"_{DateTime.Now.Ticks}";
            backupsDir = Path.Combine(WorkingDirectory, backupDirName);

            if (Directory.Exists(backupsDir))
            {
                Console.WriteLine($"I dunno what's happening but \"{backupsDir}\" exists too... Cannot do safe backup cancelling...");
                return;
            }
        }

        var bkpDlcCfgJson = Path.Combine(backupsDir, "cfg.json");
        var bkpDlcCfgIni = Path.Combine(backupsDir, "cfg.ini");

        Console.WriteLine($"Baking up into: {backupsDir}");
        var dlcCfgJsonExists = File.Exists(currentDlcCfgJson);
        var dlcCfgIniExists = File.Exists(currentDlcCfgIni);

        if (!dlcCfgJsonExists && !dlcCfgIniExists)
        {
            Console.WriteLine("Could not find any config to backup...");
            return;
        }

        Directory.CreateDirectory(backupsDir);
        if (dlcCfgJsonExists)
            File.Copy(currentDlcCfgJson, bkpDlcCfgJson);
        if (dlcCfgIniExists)
            File.Copy(currentDlcCfgIni, bkpDlcCfgIni);
    }
}
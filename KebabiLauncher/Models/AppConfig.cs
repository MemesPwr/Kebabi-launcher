namespace KebabiLauncher.Models;

public record AppConfig(
    ResolutionOverride ResolutionOverride,
    string KebabiArgs = "",
    bool PrettyPrintConfigs = false,
    int AnimeGameWaitingTime = 60,
    bool DoCheatConfigBackup = true);
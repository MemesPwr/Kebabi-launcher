namespace KebabiLauncher.Models;

public record ResolutionOverride(
    bool Enabled = false,
    bool FullScreen = true,
    int Height = 0,
    int Width = 0);
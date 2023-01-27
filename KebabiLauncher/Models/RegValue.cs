using System.Text.Json;
using Microsoft.Win32;

namespace KebabiLauncher.Models;

public record RegValue(object? Data, RegistryValueKind Kind);
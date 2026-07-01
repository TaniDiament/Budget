using System.IO;
using System.Text.Json;
using Budget.Models;

namespace Budget.Services;

public sealed class WindowPlacementStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _stateFolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Budget");

    private readonly string _stateFilePath;

    public WindowPlacementStore()
    {
        _stateFilePath = Path.Combine(_stateFolderPath, "window-state.json");
    }

    public bool TryLoad(out WindowPlacement placement)
    {
        placement = new WindowPlacement();

        try
        {
            if (!File.Exists(_stateFilePath))
            {
                return false;
            }

            var json = File.ReadAllText(_stateFilePath);
            placement = JsonSerializer.Deserialize<WindowPlacement>(json, SerializerOptions) ?? new WindowPlacement();
            return true;
        }
        catch
        {
            placement = new WindowPlacement();
            return false;
        }
    }

    public bool Save(WindowPlacement placement)
    {
        try
        {
            Directory.CreateDirectory(_stateFolderPath);
            var json = JsonSerializer.Serialize(placement, SerializerOptions);
            File.WriteAllText(_stateFilePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}


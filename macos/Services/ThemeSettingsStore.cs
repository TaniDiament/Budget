using System.IO;
using System.Text.Json;
using Budget.Models;

namespace Budget.Services;

public sealed class ThemeSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _stateFolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Budget");

    private readonly string _stateFilePath;

    public ThemeSettingsStore()
    {
        _stateFilePath = Path.Combine(_stateFolderPath, "theme-settings.json");
    }

    public ThemeSettings Load()
    {
        try
        {
            if (!File.Exists(_stateFilePath))
            {
                return new ThemeSettings();
            }

            var json = File.ReadAllText(_stateFilePath);
            return JsonSerializer.Deserialize<ThemeSettings>(json, SerializerOptions) ?? new ThemeSettings();
        }
        catch
        {
            return new ThemeSettings();
        }
    }

    public void Save(ThemeSettings settings)
    {
        Directory.CreateDirectory(_stateFolderPath);
        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(_stateFilePath, json);
    }
}


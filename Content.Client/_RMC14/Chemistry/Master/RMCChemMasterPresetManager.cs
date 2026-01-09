using System.Linq;
using System.Text.Json;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Chemistry.ChemMaster;
using Robust.Shared.Configuration;

namespace Content.Client._RMC14.Chemistry.Master;

public sealed class RMCChemMasterPresetManager
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    private List<RMCChemMasterPreset> _presets = new();
    private bool _loaded;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public IReadOnlyList<RMCChemMasterPreset> Presets
    {
        get
        {
            EnsureLoaded();
            return _presets;
        }
    }

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        LoadPresets();
    }

    private void EnsureLoaded()
    {
        if (!_loaded)
            LoadPresets();
    }

    public void LoadPresets()
    {
        _loaded = true;
        _presets.Clear();

        try
        {
            var json = _config.GetCVar(RMCCVars.RMCChemMasterPresets);
            if (string.IsNullOrWhiteSpace(json))
                return;

            var presets = JsonSerializer.Deserialize<List<RMCChemMasterPreset>>(json, JsonOptions);
            if (presets != null)
                _presets = presets;
        }
        catch (JsonException)
        {
            // Invalid JSON, reset to empty
            _presets = new List<RMCChemMasterPreset>();
        }
    }

    public void SavePresets()
    {
        try
        {
            var json = JsonSerializer.Serialize(_presets, JsonOptions);
            _config.SetCVar(RMCCVars.RMCChemMasterPresets, json);
            _config.SaveToFile();
        }
        catch (JsonException)
        {
            // Serialization failed, don't save
        }
    }

    public void SavePreset(RMCChemMasterPreset preset)
    {
        EnsureLoaded();

        var existingIndex = _presets.FindIndex(p =>
            string.Equals(p.Name, preset.Name, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
            _presets[existingIndex] = preset;
        else
            _presets.Add(preset);

        SavePresets();
    }

    public bool RemovePreset(string name)
    {
        EnsureLoaded();

        var removed = _presets.RemoveAll(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) > 0;

        if (removed)
            SavePresets();

        return removed;
    }

    public RMCChemMasterPreset? GetPreset(string name)
    {
        EnsureLoaded();
        return _presets.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public bool RenamePreset(string oldName, string newName)
    {
        EnsureLoaded();

        var preset = GetPreset(oldName);
        if (preset == null)
            return false;

        if (GetPreset(newName) != null)
            return false;

        preset.Name = newName;
        SavePresets();
        return true;
    }

    public bool MovePreset(string name, int direction)
    {
        EnsureLoaded();

        var index = _presets.FindIndex(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
            return false;

        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= _presets.Count)
            return false;

        (_presets[index], _presets[newIndex]) = (_presets[newIndex], _presets[index]);
        SavePresets();
        return true;
    }
}

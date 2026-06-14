using System.Globalization;
using System.Linq;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Chemistry.ChemMaster;
using Robust.Shared.Configuration;

namespace Content.Client._RMC14.Chemistry.Master;

/// <summary>
/// Manages ChemMaster presets for the client. Uses a simple text-based serialization format.
/// Format: name|bottleLabel|bottleColor|pillType|usePresetName|quickSlot|quickLabel;...
/// </summary>
public sealed class RMCChemMasterPresetManager
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    private List<RMCChemMasterPreset> _presets = new();
    private bool _loaded;

    private const char PresetDelimiter = ';';
    private const char FieldDelimiter = '|';
    private const string EscapedPipe = "%%PIPE%%";
    private const string EscapedSemicolon = "%%SEMI%%";

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
            var data = _config.GetCVar(RMCCVars.RMCChemMasterPresets);
            if (string.IsNullOrWhiteSpace(data))
                return;

            var presetStrings = data.Split(PresetDelimiter, StringSplitOptions.RemoveEmptyEntries);
            foreach (var presetStr in presetStrings)
            {
                var preset = DeserializePreset(presetStr);
                if (preset != null)
                    _presets.Add(preset);
            }
        }
        catch
        {
            // Invalid data, reset to empty
            _presets = new List<RMCChemMasterPreset>();
        }
    }

    public void SavePresets()
    {
        try
        {
            var serialized = string.Join(PresetDelimiter.ToString(),
                _presets.Select(SerializePreset));
            _config.SetCVar(RMCCVars.RMCChemMasterPresets, serialized);
            _config.SaveToFile();
        }
        catch
        {
            // Serialization failed, don't save
        }
    }

    private static string EscapeField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("|", EscapedPipe)
            .Replace(";", EscapedSemicolon);
    }

    private static string UnescapeField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace(EscapedPipe, "|")
            .Replace(EscapedSemicolon, ";");
    }

    private static string SerializePreset(RMCChemMasterPreset preset)
    {
        // Format: name|bottleLabel|bottleColor|pillType|usePresetName|quickSlot|quickLabel
        var quickSlot = preset.QuickAccessSlot?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

        return string.Join(FieldDelimiter.ToString(),
            EscapeField(preset.Name),
            EscapeField(preset.BottleLabel),
            ((int)preset.BottleColor).ToString(CultureInfo.InvariantCulture),
            preset.PillType.ToString(CultureInfo.InvariantCulture),
            preset.UsePresetNameAsLabel ? "1" : "0",
            quickSlot,
            EscapeField(preset.QuickAccessLabel));
    }

    private static RMCChemMasterPreset? DeserializePreset(string data)
    {
        var fields = data.Split(FieldDelimiter);
        if (fields.Length < 5)
            return null;

        var preset = new RMCChemMasterPreset
        {
            Name = UnescapeField(fields[0]),
            BottleLabel = UnescapeField(fields[1]),
        };

        if (int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var colorInt)
            && Enum.IsDefined(typeof(RMCPillBottleColors), colorInt))
        {
            preset.BottleColor = (RMCPillBottleColors)colorInt;
        }

        if (uint.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var pillType))
        {
            preset.PillType = pillType;
        }

        preset.UsePresetNameAsLabel = fields[4] == "1";

        if (fields.Length > 5 && !string.IsNullOrEmpty(fields[5]))
        {
            if (int.TryParse(fields[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var quickSlot))
            {
                preset.QuickAccessSlot = quickSlot;
            }
        }

        if (fields.Length > 6)
        {
            preset.QuickAccessLabel = UnescapeField(fields[6]);
        }

        return preset;
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

    public void RemovePreset(string name)
    {
        EnsureLoaded();

        var count = _presets.RemoveAll(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        if (count > 0)
            SavePresets();
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client._RMC14.TacticalMap;

public sealed class TacticalMapSettingsManager
{
    private const string SettingsPath = "/rmc_tactical_map_settings.yml";

    [Dependency] private readonly IResourceManager _resourceMan = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    private readonly HashSet<string> _modifiedSettings = new();
    private readonly List<TacticalMapSettingRegistration> _defaultRegistrations = new();
    private readonly Dictionary<string, TacticalMapSettingRegistration> _currentSettings = new();

    private ISawmill _logger = default!;

    public TacticalMapSettingsManager()
    {
        IoCManager.InjectDependencies(this);
        _logger = Logger.GetSawmill("tactical_map_settings");
        Initialize();
    }

    private void Initialize()
    {
        RegisterDefaultSettings();

        var path = new ResPath(SettingsPath);
        if (_resourceMan.UserData.Exists(path))
        {
            try
            {
                LoadSettingsFile(path, false, true);
            }
            catch (Exception e)
            {
                _logger.Error("Failed to load user tactical map settings: " + e);
            }
        }

        if (_resourceMan.ContentFileExists(path))
        {
            LoadSettingsFile(path, true);
        }
    }

    private void RegisterDefaultSettings()
    {
        var defaults = new[]
        {
            new TacticalMapSettingRegistration
            {
                Key = "ZoomFactor",
                Value = 1.0f,
                PlanetId = null
            },
            new TacticalMapSettingRegistration
            {
                Key = "PanOffsetX",
                Value = 0.0f,
                PlanetId = null
            },
            new TacticalMapSettingRegistration
            {
                Key = "PanOffsetY",
                Value = 0.0f,
                PlanetId = null
            },
            new TacticalMapSettingRegistration
            {
                Key = "BlipSizeMultiplier",
                Value = 1.0f,
                PlanetId = null
            },
            new TacticalMapSettingRegistration
            {
                Key = "LineThickness",
                Value = 2.0f,
                PlanetId = null
            },
            new TacticalMapSettingRegistration
            {
                Key = "SelectedColorIndex",
                Value = 0,
                PlanetId = null
            },
            new TacticalMapSettingRegistration
            {
                Key = "SettingsVisible",
                Value = false,
                PlanetId = null
            },
            new TacticalMapSettingRegistration
            {
                Key = "LabelMode",
                Value = (int) TacticalMapControl.LabelMode.Area,
                PlanetId = null
            },
            new TacticalMapSettingRegistration
            {
                Key = "WindowWidth",
                Value = 1000.0f,
                PlanetId = null
            },
            new TacticalMapSettingRegistration
            {
                Key = "WindowHeight",
                Value = 800.0f,
                PlanetId = null
            },
            new TacticalMapSettingRegistration
            {
                Key = "WindowPositionX",
                Value = -1.0f,
                PlanetId = null
            },
            new TacticalMapSettingRegistration
            {
                Key = "WindowPositionY",
                Value = -1.0f,
                PlanetId = null
            }
        };

        foreach (var setting in defaults)
        {
            _defaultRegistrations.Add(setting);
            if (!_modifiedSettings.Contains(GetSettingKey(setting.Key!, setting.PlanetId)))
            {
                _currentSettings[GetSettingKey(setting.Key!, setting.PlanetId)] = setting;
            }
        }
    }

    private void LoadSettingsFile(ResPath file, bool defaultRegistration, bool userData = false)
    {
        TextReader reader;
        if (userData)
        {
            reader = _resourceMan.UserData.OpenText(file);
        }
        else
        {
            reader = _resourceMan.ContentFileReadText(file);
        }

        using var _ = reader;

        try
        {
            var documents = DataNodeParser.ParseYamlStream(reader).First();
            var mapping = (MappingDataNode)documents.Root;

            if (mapping.TryGet("settings", out var settingsNode))
            {
                if (settingsNode == null)
                {
                    _logger.Warning("Settings node is null, skipping settings load");
                    return;
                }

                TacticalMapSettingRegistration[]? settings = null;
                try
                {
                    settings = _serialization.Read<TacticalMapSettingRegistration[]>(settingsNode, notNullableOverride: false);
                }
                catch (Exception parseEx)
                {
                    _logger.Error($"Failed to parse settings array: {parseEx}");
                    return;
                }

                if (settings == null)
                {
                    _logger.Warning("Parsed settings array is null, skipping settings load");
                    return;
                }

                foreach (var setting in settings)
                {
                    if (setting.Key == null || string.IsNullOrEmpty(setting.Key))
                    {
                        _logger.Warning("Skipping setting with null/empty key");
                        continue;
                    }

                    if (setting.Value == null)
                    {
                        _logger.Warning($"Skipping setting '{setting.Key}' with null value");
                        continue;
                    }

                    var settingKey = GetSettingKey(setting.Key, setting.PlanetId);

                    if (defaultRegistration)
                    {
                        _defaultRegistrations.Add(setting);

                        if (_modifiedSettings.Contains(settingKey))
                        {
                            continue;
                        }
                    }

                    _currentSettings[settingKey] = setting;

                    if (!defaultRegistration)
                    {
                        _modifiedSettings.Add(settingKey);
                    }
                }

                if (!defaultRegistration && mapping.TryGet("unsetSettings", out var unsetNode))
                {
                    if (unsetNode != null)
                    {
                        try
                        {
                            var unsetSettings = _serialization.Read<string[]>(unsetNode, notNullableOverride: false);

                            if (unsetSettings != null)
                            {
                                foreach (var settingKey in unsetSettings)
                                {
                                    if (string.IsNullOrEmpty(settingKey))
                                    {
                                        continue;
                                    }

                                    _modifiedSettings.Add(settingKey);
                                    _currentSettings.Remove(settingKey);
                                }
                            }
                        }
                        catch (Exception unsetEx)
                        {
                            _logger.Error($"Failed to parse unsetSettings array: {unsetEx}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to parse settings file {file}: {ex}");
            if (!defaultRegistration)
            {
                _logger.Info("Continuing with default settings due to parse failure");
            }
        }
    }

    private string GetSettingKey(string key, string? planetId)
    {
        if (!string.IsNullOrEmpty(planetId))
        {
            return $"{key}_{planetId}";
        }
        return key;
    }

    public void SaveToUserData()
    {
        var path = new ResPath(SettingsPath);

        try
        {
            using var writer = _resourceMan.UserData.OpenWriteText(path);

            writer.WriteLine("version: \"1\"");
            writer.WriteLine("settings:");

            var modifiedSettings = _modifiedSettings
                .Where(key => _currentSettings.ContainsKey(key))
                .Select(key => _currentSettings[key])
                .Where(setting => setting.Key != null);

            foreach (var setting in modifiedSettings)
            {
                writer.WriteLine("  - Key: \"" + setting.Key + "\"");
                writer.WriteLine("    Value: " + FormatValueForYaml(setting.Value));
                if (!string.IsNullOrEmpty(setting.PlanetId))
                {
                    writer.WriteLine("    PlanetId: \"" + setting.PlanetId + "\"");
                }
                else
                {
                    writer.WriteLine("    PlanetId: null");
                }
            }

            writer.WriteLine("unsetSettings:");
            var unsetSettings = _modifiedSettings.Where(key => !_currentSettings.ContainsKey(key));
            foreach (var unsetKey in unsetSettings)
            {
                writer.WriteLine("  - \"" + unsetKey + "\"");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save tactical map settings: {ex}");
        }
    }

    private string FormatValueForYaml(object? value)
    {
        if (value == null) return "null";

        return value switch
        {
            string str => "\"" + str.Replace("\"", "\\\"") + "\"",
            bool b => b.ToString().ToLower(),
            float f => f.ToString("F6"),
            int i => i.ToString(),
            _ => "\"" + value.ToString() + "\""
        };
    }

    public TacticalMapSettings LoadSettings(string? planetId = null)
    {
        var settings = new TacticalMapSettings
        {
            ZoomFactor = GetSettingValue<float>("ZoomFactor", planetId, 1.0f),
            PanOffset = new Vector2(
                GetSettingValue<float>("PanOffsetX", planetId, 0.0f),
                GetSettingValue<float>("PanOffsetY", planetId, 0.0f)
            ),
            BlipSizeMultiplier = GetSettingValue<float>("BlipSizeMultiplier", planetId, 1.0f),
            LineThickness = GetSettingValue<float>("LineThickness", planetId, 2.0f),
            SelectedColorIndex = GetSettingValue<int>("SelectedColorIndex", planetId, 0),
            SettingsVisible = GetSettingValue<bool>("SettingsVisible", planetId, false),
            LabelMode = (TacticalMapControl.LabelMode)GetSettingValue<int>("LabelMode", planetId, (int) TacticalMapControl.LabelMode.Area),
            WindowSize = new Vector2(
                GetSettingValue<float>("WindowWidth", planetId, 1000.0f),
                GetSettingValue<float>("WindowHeight", planetId, 800.0f)
            ),
            WindowPosition = new Vector2(
                GetSettingValue<float>("WindowPositionX", planetId, -1.0f),
                GetSettingValue<float>("WindowPositionY", planetId, -1.0f)
            )
        };

        if (!string.IsNullOrEmpty(planetId))
        {
            CopyGlobalSettingsToMapIfNeeded(planetId);
        }

        return settings;
    }

    private void CopyGlobalSettingsToMapIfNeeded(string planetId)
    {
        var hasMapSpecificSettings = _modifiedSettings.Any(key => key.Contains($"_{planetId}"));

        if (!hasMapSpecificSettings)
        {
            var globalSettings = _currentSettings.Where(kvp => !kvp.Key.Contains("_")).ToList();
            foreach (var (key, setting) in globalSettings)
            {
                if (setting.Key == null) continue;

                var mapSpecificKey = GetSettingKey(setting.Key, planetId);
                var mapSpecificSetting = new TacticalMapSettingRegistration
                {
                    Key = setting.Key,
                    Value = setting.Value,
                    PlanetId = planetId
                };

                _currentSettings[mapSpecificKey] = mapSpecificSetting;
                _modifiedSettings.Add(mapSpecificKey);
            }

            SaveToUserData();
        }
    }

    private T GetSettingValue<T>(string key, string? planetId, T defaultValue) where T : notnull
    {
        var settingKey = GetSettingKey(key, planetId);

        if (_currentSettings.TryGetValue(settingKey, out var setting))
        {
            if (setting.Value is T typedValue)
                return typedValue;
        }

        if (!string.IsNullOrEmpty(planetId))
        {
            var globalKey = GetSettingKey(key, null);
            if (_currentSettings.TryGetValue(globalKey, out var globalSetting))
            {
                if (globalSetting.Value is T typedValue)
                    return typedValue;
            }
        }

        return defaultValue;
    }

    private void SetSettingValue(string key, object value, string? planetId, bool markModified = true)
    {
        var settingKey = GetSettingKey(key, planetId);

        var registration = new TacticalMapSettingRegistration
        {
            Key = key,
            Value = value,
            PlanetId = planetId
        };

        _currentSettings[settingKey] = registration;

        if (markModified)
        {
            _modifiedSettings.Add(settingKey);
        }
    }

    public void SaveSettings(TacticalMapSettings settings, string? planetId = null)
    {
        SetSettingValue("ZoomFactor", settings.ZoomFactor, planetId);
        SetSettingValue("PanOffsetX", settings.PanOffset.X, planetId);
        SetSettingValue("PanOffsetY", settings.PanOffset.Y, planetId);
        SetSettingValue("BlipSizeMultiplier", settings.BlipSizeMultiplier, planetId);
        SetSettingValue("LineThickness", settings.LineThickness, planetId);
        SetSettingValue("SelectedColorIndex", settings.SelectedColorIndex, planetId);
        SetSettingValue("SettingsVisible", settings.SettingsVisible, planetId);
        SetSettingValue("LabelMode", (int)settings.LabelMode, planetId);
        SetSettingValue("WindowWidth", settings.WindowSize.X, planetId);
        SetSettingValue("WindowHeight", settings.WindowSize.Y, planetId);
        SetSettingValue("WindowPositionX", settings.WindowPosition.X, planetId);
        SetSettingValue("WindowPositionY", settings.WindowPosition.Y, planetId);

        try
        {
            SaveToUserData();
        }
        catch (Exception ex)
        {
            _logger.Error($"TacticalMapSettingsManager: Failed to save to file: {ex}");
        }
    }

    public void SaveViewSettings(float zoomFactor, Vector2 panOffset, string? planetId = null)
    {
        SetSettingValue("ZoomFactor", zoomFactor, planetId);
        SetSettingValue("PanOffsetX", panOffset.X, planetId);
        SetSettingValue("PanOffsetY", panOffset.Y, planetId);

        try
        {
            SaveToUserData();
        }
        catch (Exception ex)
        {
            _logger.Error($"TacticalMapSettingsManager: Failed to save view settings: {ex}");
        }
    }

    public void SaveWindowSizeAndPosition(Vector2 size, Vector2 position, string? planetId = null)
    {
        SetSettingValue("WindowWidth", size.X, planetId);
        SetSettingValue("WindowHeight", size.Y, planetId);
        SetSettingValue("WindowPositionX", position.X, planetId);
        SetSettingValue("WindowPositionY", position.Y, planetId);

        try
        {
            SaveToUserData();
        }
        catch (Exception ex)
        {
            _logger.Error($"TacticalMapSettingsManager: Failed to save window settings: {ex}");
        }
    }

    public void SaveWindowSize(Vector2 size, string? planetId = null)
    {
        SetSettingValue("WindowWidth", size.X, planetId);
        SetSettingValue("WindowHeight", size.Y, planetId);

        try
        {
            SaveToUserData();
        }
        catch (Exception ex)
        {
            _logger.Error($"TacticalMapSettingsManager: Failed to save window size: {ex}");
        }
    }

    public void SaveSingleSetting<T>(string key, T value, string? planetId = null) where T : notnull
    {
        SetSettingValue(key, value, planetId);

        try
        {
            SaveToUserData();
        }
        catch (Exception ex)
        {
            _logger.Error($"TacticalMapSettingsManager: Failed to save single setting: {ex}");
        }
    }

    public void SaveBlipSizeMultiplier(float value, string? planetId = null)
    {
        SaveSingleSetting("BlipSizeMultiplier", value, planetId);
    }

    public void SaveLineThickness(float value, string? planetId = null)
    {
        SaveSingleSetting("LineThickness", value, planetId);
    }

    public void SaveSelectedColorIndex(int value, string? planetId = null)
    {
        SaveSingleSetting("SelectedColorIndex", value, planetId);
    }

    public void SaveLabelMode(TacticalMapControl.LabelMode value, string? planetId = null)
    {
        SaveSingleSetting("LabelMode", (int)value, planetId);
    }

    public void SaveSettingsVisible(bool value, string? planetId = null)
    {
        SaveSingleSetting("SettingsVisible", value, planetId);
    }

    public void ResetSettingsFor(string key, string? planetId = null)
    {
        var settingKey = GetSettingKey(key, planetId);
        _modifiedSettings.Remove(settingKey);
        _currentSettings.Remove(settingKey);

        var defaultSetting = _defaultRegistrations.FirstOrDefault(r =>
            r.Key != null && r.Key == key &&
            ((r.PlanetId == null && planetId == null) ||
             (!string.IsNullOrEmpty(r.PlanetId) && !string.IsNullOrEmpty(planetId) && r.PlanetId == planetId)));

        if (!string.IsNullOrEmpty(defaultSetting.Key))
        {
            _currentSettings[settingKey] = defaultSetting;
        }

        SaveToUserData();
    }

    public void ResetAllSettings()
    {
        var modifiedKeys = _modifiedSettings.ToArray();
        foreach (var key in modifiedKeys)
        {
            var parts = key.Split('_');
            var settingKey = parts[0];
            string? planetId = null;

            if (parts.Length > 1)
            {
                planetId = parts[1];
            }

            ResetSettingsFor(settingKey, planetId);
        }
    }

    public bool IsSettingModified(string key, string? planetId = null)
    {
        return _modifiedSettings.Contains(GetSettingKey(key, planetId));
    }
}

[Serializable, DataDefinition]
public partial struct TacticalMapSettingRegistration
{
    [DataField("Key")] public string? Key { get; set; }
    [DataField("Value")] public object? Value { get; set; }
    [DataField("PlanetId")] public string? PlanetId { get; set; }
}

public struct TacticalMapSettings()
{
    public float ZoomFactor;
    public Vector2 PanOffset;
    public float BlipSizeMultiplier;
    public float LineThickness;
    public int SelectedColorIndex;
    public bool SettingsVisible;
    public TacticalMapControl.LabelMode LabelMode = TacticalMapControl.LabelMode.Area;
    public Vector2 WindowSize;
    public Vector2 WindowPosition;
}
